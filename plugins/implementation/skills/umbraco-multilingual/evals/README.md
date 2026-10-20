# Running these evals

Run all five evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet. A stronger model's
baseline already knows some of Umbraco's variant behaviour (the variation context, `Fallback`,
block level variance), so a single strong-model run understates what the skill adds; the Haiku run
shows the gap, the Sonnet run shows what is left once the model knows the basics.

The evaluator expects one workspace laid out as
`<workspace>/<model>/iteration-N/<eval-name>/{with_skill,without_skill}/`, so the two models'
runs sit side by side and are reported separately.

## Project fixture (every eval, both sides)

Each run gets its own folder:

```
<run>/outputs/
  Directory.Packages.props     # copy of the repo-root file — see "Outside-the-project check"
  project/                     # the "user's project": the agent edits here, in place
```

To build `project/`:
1. Copy `files/project/` into it. This is a minimal, English-only Umbraco 17 site with root
   namespace `Acme.Web`:
   - the stock Umbraco csproj settings, plus an `EmbeddedResource` glob for `Migrations/*.xml`
     (harmless when the folder is absent);
   - `appsettings.json` with ModelsBuilder in `Nothing` mode and nothing multilingual;
   - `Views/master.cshtml`, a layout with a `<title>` but no hreflang, a header with the site name
     but no language switcher, and `<html lang="en">`;
   - `Views/home.cshtml`, which renders `title` and `bodyText` with `Model.Value<string>(...)` and
     no fallback.
2. Overlay the eval's own folder from its `files` list, replacing any file with the same name:

| Eval | Overlay | Starting state |
|---|---|---|
| 1 | — | The project as copied: no second language, switcher or hreflang anywhere |
| 2 | `files/culture-context/` | `Controllers/PageTitleApiController.cs`, an `[ApiController]` at `api/page-title` that lowercases `?culture=` (`ToLowerInvariant()`), compares it case-sensitively against `ILanguageService` ISO codes, falls back to the default language on no match and still never sets a variation context or passes the culture to `Value`/`Url`; and `Jobs/ProductFeedJob.cs`, a nightly `IRecurringBackgroundJob` (with its composer) that writes one feed per culture from a hardcoded `new[] { "en-US", "de-DE" }`, fills blank German descriptions with `Value<string>(alias, culture) ?? Value<string>(alias, "en-US")` and finds published products with an Examine query on `$"__Published_{culture}"` (so `__Published_de-DE`) |
| 3 | `files/danish-launch/` | `Migrations/DanishLaunchMigrationPlan.cs` + `DanishLaunchMigration.cs` importing `Migrations/danish-launch-package.xml`, whose Danish `<Value LanguageCultureAlias="da-dk">` is lowercase; `Startup/DanishContentSeeder.cs`, which sets Danish values and calls `PublishBranchAsync(rootKey, ["*"], PublishBranchFilter.IncludeUnpublished, userKey, false)`; and an `appsettings.json` that adds `Umbraco:CMS:Global:DefaultUILanguage: "da-DK"` |
| 4 | `files/block-sections/` | `Views/home.cshtml` replaced by one that renders a `sections` Block List (noted as *varying by culture* in the backoffice) with hand-rolled `languages`/`danishOnly` filtering and a lowercased culture comparison |
| 5 | — | The project as copied |

The fixture mirrors the pinned `Umbraco.Cms 17.5.3` csproj and the repo's central package versions
(`Directory.Packages.props`). All five assembled overlays build with
`dotnet build -p:RazorCompileOnBuild=true` (checked on 2026-10-13 with dotnet-sdk-10.0). The first
build found two Razor traps in the fixture itself, a loop variable named `page` that Razor read as
the `@page` directive and bare `@Model.Value<string>(...)` calls, so build every overlay again after
editing the fixture. The views are compiled at runtime, as in the stock template; the build check
turns Razor compilation on to catch view errors.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that the .NET SDK is installed and it may run `dotnet build` on the project (rounds 1 to 4 had no SDK and
  were told so);
- that it may read the Umbraco documentation (rounds 1 to 4 had no docs at all);
- to save its reply to `outputs/answer.md` and a plain step log to `transcript.md`.

Give the with-skill run the path of a **snapshot** of the skill that contains only `SKILL.md`,
`references/` and `assets/`, not the skill folder itself. `evals/` holds the expectations and
`examples/` holds the tests, and a run that can reach them will read them; an instruction not to
read them is not enough. Tell the baseline run not to read `plugins/`, `.claude/` or any Umbraco
source checkout, or it will find the skill by browsing.

Do not tell either side to avoid writing outside the project: see the next section.

No Umbraco instance and no Umbraco MCP are running during a run, so every backoffice step
(languages, fallback, vary by culture, Culture and Hostnames, exposing blocks) is graded on the
instructions the run gives, not on anything it did.

## Outside-the-project check

`dotnet add package` in a centrally-managed project edits the nearest `Directory.Packages.props`
above it. The copy in `outputs/` is there to absorb that, so the repo's own file is never touched.
After a run, diff the copy against the repo-root original; a difference means the run reached
outside `project/`.

## Reviewing in the viewer

The evaluator's viewer lists only the files directly inside `outputs/`, not those under
`outputs/project/`. Before generating the report, copy each file the run created or changed up into
`outputs/` (for example as `changed - Views__home.cshtml` or
`created - Views__Partials__languageSwitcher.cshtml`), or a reviewer sees only `answer.md` and
will think the code changes are missing.

## Grading notes

Grade code expectations from the files under `outputs/project/`, not from `answer.md` alone.

- **Eval 1** is mostly backoffice instructions plus two partials and a view change. Grade the
  switcher and hreflang expectations from the files the run wrote (`Views/master.cshtml`,
  `Views/home.cshtml` and any partials). Copying the skill's `languageSwitcher.cshtml` and
  `hreflangLinks.cshtml` verbatim passes those expectations; an inline switcher with the same
  behaviour passes too; the baseline has to write its own.
- **Eval 2**: both files must change. A fix in the controller alone, or advice without an edit,
  fails the "applied in both" expectation. In `Jobs/ProductFeedJob.cs` the hardcoded culture list,
  the `?? Value<string>(..., "en-US")` fallback and the `$"__Published_{culture}"` field must each
  be gone or replaced (expectations 3, 4 and 6 are graded one per bug). A `ToLower()` /
  `ToLowerInvariant()` on a culture left anywhere in either file, including the fixture's own
  line in the controller, fails expectation 5, except a lowercase used only to build the Examine
  `__Published_` field name, which expectation 6 accepts.
- **Eval 3**: four separate causes, four separate fixes. Check the manifest's casing in
  `danish-launch-package.xml`, the cultures passed in `DanishContentSeeder.cs`, where the fallback
  language gets set (backoffice, MCP or code are all acceptable), and what happened to
  `DefaultUILanguage` in `appsettings.json`.
- **Eval 4**: the rewritten `Views/home.cshtml` should contain no culture filtering at all; the
  rest is advice, graded from `answer.md`.
- **Eval 5** is advice plus one composer. Grade it from `answer.md` and any `.cs` the run wrote.
- A conditional expectation whose condition never arises (for example "if the agent replaces
  `IIsoCodeValidator`" when it keeps the stock one) is not applicable; do not grade it as a fail.
- "Does not lowercase cultures": a `ToLower()` or `ToLowerInvariant()` applied to a culture code
  anywhere in the written code or in a proposed fix fails it, including one left over from the eval 4
  starting view. Case-insensitive comparison (`StringComparison.OrdinalIgnoreCase`) passes, and so
  does a lowercase used only to build the Examine `__Published_` field name (eval 2, expectation 6).
- The build-honesty expectations: a run that says it could not or did not build passes; a run that
  says it built passes only if its log shows the build and `razor-build.txt` agrees. It fails if it
  claims to have built, run, rendered, validated or verified anything it did not, including presenting
  the skill's own runtime gate as its verification ("this composer has been validated against
  Umbraco 17.5"). Saying whether the code is expected to compile is welcome but not required.
- The razor-build expectation is graded from `razor-build.txt` beside the run's `outputs/`, written after
  the run (not by it) with `dotnet build -p:RazorCompileOnBuild=true` on `outputs/project`. Exit code 0 passes; anything else fails, whatever the run said.

## Results

Run on 2026-10-13 and 2026-10-14 with the method above: all five evals, with and without the skill,
as Claude Code runs on `claude-haiku-5-5` and `claude-sonnet-5-5`, one run per cell, graded by
Haiku 5.5. Six rounds were run. Rounds 1 to 4 had no .NET SDK and no docs; from round 5 every run had
the SDK, permission to build and the Umbraco docs, and every eval carries the razor-build expectation,
so totals are 41 per column against 34 before. Round 6 (the table below) is a straight re-run of
round 5's method under the final wording of the skill and the evals, and no cell in it was regraded:
each run was graded once on the output it produced. Eval 2's four cells come from a second fresh run
(see "Across both models"); the other sixteen are round 6's. In every round neither side had an
Umbraco instance or MCP. None of the 124 runs modified the `Directory.Packages.props` copy. Read
single runs as a range, not a point: baseline scores on the same prompt moved by one or two
expectations between rounds.

### Final round (round 6: builds and docs, graded once)

| Eval | Haiku with skill | Haiku baseline | Sonnet with skill | Sonnet baseline |
|---|---|---|---|---|
| 1. Danish launch setup (languages, fallback, domains, switcher, hreflang) | 10/10 | 10/10 | 10/10 | 10/10 |
| 2. Culture-less contexts (API controller, background job) | 9/9 | 8/9 | 9/9 | 7/9 |
| 3. Scripted launch diagnosis (manifest casing, `"*"` publish, fallback, `DefaultUILanguage`) | 7/7 | 6/7 | 7/7 | 6/7 |
| 4. Block level variance (shared blocks, unexposed blocks, migration) | 7/7 | 7/7 | 7/7 | 6/7 |
| 5. Missing culture `zh-HK` (composer) | 8/8 | 7/8 | 8/8 | 8/8 |
| **Mean pass rate** | **100%** (41/41) | **93%** (38/41) | **100%** (41/41) | **90%** (37/41) |

Every edited project built with Razor compilation on (`razor-build.txt` exit 0 in all 24 runs, the
20 of round 6 and the 4 eval 2 re-runs), so the build column proves the code compiles but does not
separate the two sides. Each run also ran `dotnet build` itself at least once, which is why times
from round 5 on are three to four times round 4's.

**Haiku 5.5**

- The skill reached 100% against 93% without it, and cost less: 111k tokens and 318 s per run with
  the skill, 125k tokens and 323 s without. The with-skill run copies the partials and the composer
  and reads one reference; the baseline reads more docs pages.
- Baseline misses: in the feed job the `?? Value<string>(..., defaultCulture)` fallback kept, with
  no explanation that `Value<string>` returns `""` so `??` never fires; the `"*"` publish explained
  as "publishes all cultures and all pending nodes" (the opposite of what happens on an
  already-published node); and `zh-Hant-HK` offered as a workaround that "already appears in the
  dropdown".
- Haiku is the model the skill helps most, and the one that needed the SKILL.md wording fixes
  below: it works from SKILL.md and reads a reference only when the router points at it.

**Sonnet 5.5**

- The skill reached 100% against 90% without it, at lower cost: 85k tokens and 277 s per run with
  the skill against 94k and 330 s without.
- Baseline misses: a blank-description fallback that works (`string.IsNullOrWhiteSpace`) but no
  explanation of the `""` against `null` bug behind the fixture's `??`; the Examine query replaced
  but the lowercase suffix never named ("a fragile, name-sensitive field"); the `"*"` publish
  explained as "the invariant marker, so the da-DK variants were saved as drafts" rather than as a
  default-culture-only republish; and an eval 4 view that dropped the fixture's filter but kept its
  own culture branch (`isEnglish`) instead of rendering the shared list as is.
- The gap is smaller than Haiku's and sits in what has to be known rather than reasoned out or looked
  up: the `"*"` publish rule, that `Value<string>` is never `null`, and the lowercase Examine suffix.

**Across both models**

- Every with-skill cell in the table is a fresh run graded once. Round 6 as first run scored 40/41 on
  both models: the one miss on each side was eval 2's expectation 5 ("no `ToLower()` on a culture"),
  failed on the line that builds the Examine field name `__Published_{culture.ToLowerInvariant()}`,
  which expectation 6 of the same eval accepts as a fix. Both with-skill runs had done what the skill
  and expectation 6 say; the two expectations contradicted each other. Expectation 5 now scopes the
  rule to a culture that is then used as a culture and names the Examine field-name case as allowed,
  and eval 2's four cells were run again from a clean fixture and graded once: 9/9 and 9/9 with the
  skill, 8/9 and 7/9 without. The first-run eval 2 cells (8/9, 7/9, 8/9, 7/9) were not regraded and
  are not in the table.
- Access to the docs lifted the baselines, not the skill: Haiku's baseline went from 76% (round 4,
  no docs) to 93%, Sonnet's from 85% to 90%. Eval 1 and eval 4 stopped discriminating once the
  baselines could read the docs; Sonnet's eval 5 baseline writes the `IIsoCodeValidator` composer
  from the docs page, Haiku's still offers `zh-Hant-HK` instead.
- Eval 2 discriminates since its prompt was hardened in round 5: baselines 8/9 and 7/9 against 9/9.
  What separates the sides is the explanation, not the fix: both baselines repaired the code but
  could not say why the original fallback never fired, and Sonnet's could not name the lowercase
  suffix.
- Evals 2 and 3 separate the two sides on both models; eval 5 only on Haiku; eval 4 only on Sonnet;
  eval 1 on neither. What is left in the gap is knowledge the docs do not state plainly: the `"*"`
  publish rule, `Value<string>` never being `null`, and the lowercase Examine suffix.
- No with-skill run lowercased a culture that it then used as a culture in any round. The only
  lowercasing in any with-skill run was the Examine field name, which is correct.

### Earlier rounds and what changed between them

| Round | Haiku with / baseline | Sonnet with / baseline | With-skill misses and the fix that followed |
|---|---|---|---|
| 1 | 32/34 / 24/34 | 33/34 / 28/34 | Eval 1 (both models): the package-manifest expectation was unconditional, but neither run used the package; it is now conditional on using or recommending it. Eval 3 (Haiku): never questioned making Danish the default; SKILL.md now says changing the default is a separate decision. |
| 2 | 33/34 / 28/34 | 32/34 / 28/34 | All three misses on the build-honesty expectation. Two were the grading note being read as a requirement to predict whether the code compiles; the note now says a run passes by not claiming a build or verification. One Sonnet run told the user the composer "has been validated against Umbraco 17.5", echoing the skill's own runtime-gate claim; SKILL.md now says the gate tests the skill's assets, not the run's copy. |
| 3 | 33/34 / 27/34 | 34/34 / 30/34 | Eval 4 (Haiku): block level variance described without the Document Type varying. The reference table named all three settings; the SKILL.md bullet named two and now names three. |
| 4 | 34/34 / 26/34 | 34/34 / 29/34 | None. Between rounds 4 and 5 the method changed, not the skill: SDK installed, builds allowed, docs made available, razor-build expectation added to every eval, eval 2 prompt hardened (6 to 8 expectations plus the build one), and the fixture made to compile (see "Project fixture"). |
| 5 | 41/41 / 36/41 | 41/41 / 38/41 | Eval 1 (Haiku): the MCP expectation could not be met as worded when the prompt says no MCP is running; reworded to "uses the MCP when one is available; when none is ... walks the user through them manually". Eval 1's switcher expectation also dropped its "must be a partial" clause, which both baselines had lost their only point on. Three cells (the Haiku with-skill run and both baselines) were regraded on unchanged output to reach these figures, so round 5 was re-run in full as round 6. The block-level-variance reference was also trimmed to what the docs do not say. |
| 6 (final) | 41/41 / 38/41 | 41/41 / 37/41 | Eval 2 (both models) on the first run, 40/41 each: expectation 5 contradicted expectation 6 on the Examine field name (above). Expectation 5 reworded; eval 2's four cells re-run fresh and graded once. No skill change. |

Rounds 1 to 4 ran with no SDK and no docs, so their with-skill 100% means the runs said and wrote
what the expectations ask for; from round 5 the code was also compiled. A single-cell re-run of
Haiku eval 3 between rounds 1 and 2, against the round-1 SKILL.md fix, scored 6/6; the full round 2
confirmed it.

### Notes for the next run

- Sonnet 5.5's safeguards rejected the executor prompt wholesale when its transcript line asked
  for "each step ... and your reasoning", and rejected three runs mid-task even after that was
  reworded. From round 2 both models were asked for "a plain log of the steps you took"; a rejected
  run was reset to a fresh folder and re-run, never patched up.
- One Haiku run (eval 2 re-run) returned its whole answer as its final message and wrote no
  `answer.md`, saying its instructions barred "report .md files". There was nothing to grade, so the
  cell was reset and run again with the two files named as required deliverables. Say so in the
  prompt from the start.
- Run the Razor build after each run, not inside it. With a warm NuGet cache it takes 8 to 47 s per
  project, so build the fixture overlays first.
- Some runs write `transcript.md` inside `outputs/` rather than beside it. Move it before grading.
- Wall time is not work time for every run. One Haiku run in round 3 reported 20 minutes for two
  minutes of work; one round-5 baseline finished and then stalled; one round-6 re-run did its work in
  under five minutes and then sat idle for nearly four hours before handing back. In each case the
  recorded time is first to last action, excluding the single idle gap, and the outputs were graded
  as written. Treat a lone outlier like that as time outside the run.
