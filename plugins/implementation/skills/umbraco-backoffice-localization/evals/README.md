# Running these evals

Run all five evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet: a stronger model's
baseline already knows the `#area_key` syntax, so one strong-model run understates what the skill adds.

The evaluator expects one workspace laid out as
`<workspace>/<model>/iteration-N/<eval-name>/{with_skill,without_skill}/`.

## Project fixture

Each run gets its own folder:

```
<run>/outputs/
  Directory.Packages.props     # copy of the repo-root file, so `dotnet add package` cannot touch the repo's
  project/                     # the "user's project": the agent edits here, in place
```

Copy `files/project/` into `project/`. It is a minimal Umbraco 17 site with root namespace `Acme.Web` and
one hardcoded-English backoffice package, `App_Plugins/Acme.Welcome` (a dashboard whose label and
element text are plain English). Then overlay the eval's own folder:

| Eval | Overlay | Starting state |
|---|---|---|
| 1 `translate-dashboard-and-doctypes` | none | the dashboard is English only; the Document Type exists only in the user's backoffice |
| 2 `rename-core-wording` | none | nothing localized |
| 3 `localize-welcome-email` | `files/welcome-email/` | `Services/WelcomeEmailSender.cs` asks `ILocalizedTextService` for area `welcome` with the thread's culture; no XML, no csproj entry |
| 4 `fix-broken-manifest` | `files/broken-manifest/` (replace `App_Plugins`) | a localization manifest in a nested `Localization/` folder with culture `da_DK`, ungrouped inline keys, and key names that do not match the dashboard's requests |
| 5 `visitor-labels-out-of-scope` | none | `Views/home.cshtml` has a hardcoded English link; the right answer is dictionary items, not either localization system |

Every assembled overlay builds with `dotnet build`.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that the .NET SDK is installed and it may run `dotnet build`;
- that it may read the Umbraco documentation;
- to save its reply to `outputs/answer.md` and a plain step log to `transcript.md`.

Give the with-skill run the path of a **snapshot** of the skill that holds only `SKILL.md`,
`references/` and `assets/`. `evals/` holds the expectations and `examples/` holds the tests, and a
run that can reach them will read them. Tell the baseline run not to read `plugins/`, `.claude/` or any
Umbraco source checkout, or it will find the skill by browsing.

No Umbraco instance is running during a run, so everything seen in the backoffice is graded on the
instructions the run gives, not on anything it did.

## Reading the results

Expectations that a baseline run also passes (evals 4 and 5 are near ceiling for a model with the docs)
are kept because they guard against regressions and, for eval 5, against the skill claiming work that
belongs to another skill. The expectations that separate the two sides are the ones that need facts the
docs pages omit: the exact lowercase core key and its weight (eval 2), the culture-explicit overload,
`%0%` tokens and the English-only fallback (eval 3), and the restart note.
