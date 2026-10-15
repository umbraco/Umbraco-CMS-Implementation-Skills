# Running these evals

Run all five evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet. A stronger model's
baseline already knows the generic .NET debugger settings (Just My Code, symbol servers), so a single
strong-model run understates what the skill adds; the Haiku run shows the gap, the Sonnet run shows
what is left once the model knows the basics.

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
1. Copy `files/project/` into it. This is a minimal Umbraco 17 site with root namespace `Acme.Web`:
   - the stock Umbraco csproj settings (`net10.0`, `Umbraco.Cms`, Razor runtime compilation);
   - `appsettings.json` with ModelsBuilder in `Nothing` mode and **no** `Umbraco:CMS:Hosting`
     section;
   - `appsettings.Development.json` with the stock Serilog console sink and no `Hosting` section;
   - `Properties/launchSettings.json` with an `Acme.Web` profile that sets
     `ASPNETCORE_ENVIRONMENT=Development`;
   - `Views/_ViewImports.cshtml`, `Views/master.cshtml` (a layout with a `<body>`, so MiniProfiler
     has somewhere to inject) and `Views/home.cshtml`.
2. Overlay the eval's own folder from its `files` list, replacing any file with the same name.
   Eval 2's overlay keeps its VS Code files in `files/vscode-launch/vscode/` because the repo's
   `.gitignore` excludes every `.vscode/` folder; **rename `vscode/` to `.vscode/`** when copying it
   into `project/`, or the run will see no launch.json at all.

| Eval | Overlay | Starting state |
|---|---|---|
| 1 | `files/product-import/` | `Controllers/ProductImportController.cs`, an `[ApiController]` at `api/products/import` that creates products with `IContentService.Create` and calls `_contentService.Save(product)` per row: the F11 target the prompt names. No debugger settings live in the project; Visual Studio's are per machine, so this eval is graded from `answer.md` |
| 2 | `files/vscode-launch/` (its `vscode/` copied in as `.vscode/`) | `.vscode/launch.json` with four planted faults: no `justMyCode` (so the default `true` applies), `searchMicrosoftSymbolServer` at the top level of the configuration instead of inside `symbolOptions` and no NuGet.org server, `sourceLinkOptions` with `"*": { "enabled": false }`, and `program` pointing at `bin/Debug/net8.0/` although the csproj targets `net10.0`. No `suppressJITOptimizations`. The `preLaunchTask`, `serverReadyAction` and Development `env` are correct and must survive. `.vscode/tasks.json` holds the `build` task |
| 3 | — | The project as copied. Rider's settings are per machine; graded from `answer.md`, plus the check that `Acme.Web.csproj` was not given `DebugType`/`EmbedAllSources` as the fix |
| 4 | `files/slow-page/` | `Controllers/ProductListingController.cs`, a `RenderController` hijacking `productListing` that does three expensive things with no timing: a `Root().DescendantsOrSelf()` product query, a per-product same-category `Count` over every product (quadratic), and a 1.2 s `Task.Delay` standing in for a stock API; `Models/ProductListingViewModel.cs`; `Views/productListing.cshtml`; and an `appsettings.json` that **wrongly** sets `Umbraco:CMS:Hosting:Debug: true` in the base file, which applies in production too |
| 5 | `files/stale-news/` | `Components/LatestNewsViewComponent.cs`, which caches the five latest `newsArticle` items in a `private static List<LatestNewsItem>? _latest` built on the first request and never cleared (the real cause of the stale headline); `Views/Shared/Components/LatestNews/Default.cshtml`; and `Views/home.cshtml`, replaced by one that invokes the component |

The fixture mirrors the pinned `Umbraco.Cms 17.5.3` and the repo's central package versions
(`Directory.Packages.props`). The base project and all four overlays build with
`dotnet build -p:RazorCompileOnBuild=true` (checked on 2026-10-15 with dotnet-sdk-10.0, in a scratch
copy with `Directory.Packages.props` beside each project: 0 errors each, and the only warnings were the
18 NU1902/NU1903 package-vulnerability advisories Umbraco 17.5.3's dependency graph raises in any
project). Build every overlay again after editing the fixture. The views are compiled at runtime, as
in the stock template; the build check turns Razor compilation on to catch view errors.

## What the expectations rely on

Each fact an expectation grades was checked against the Umbraco source (17.7.1 checkout) or is
standard debugger behaviour:

- Umbraco publishes symbol packages: the repo's `Directory.Build.props` sets
  `PublishRepositoryUrl`, `IncludeSymbols` and `SymbolPackageFormat=snupkg`, so the PDBs are on the
  NuGet.org symbol server and Source Link points at GitHub.
- `Umbraco:CMS:Hosting:Debug` is read once while the builder is created
  (`UmbracoBuilderExtensions.GetWebProfiler`): `false` registers `NoopProfiler`, whose `Step` returns
  a shared no-op disposable; `true` registers `WebProfiler`. Changing it needs a restart.
- `?umbDebug=true` (also the `X-UMB-DEBUG` header or `UMB-DEBUG` cookie) turns profiling on for a
  request (`WebProfilerRepository`), and `UmbracoViewPage` injects the MiniProfiler markup into the
  `<body>` tag when the request is in debug.
- The expectations deliberately do **not** require a logged-in backoffice user for `?umbDebug=true`.
  In the checked source `WebProfilerService.GetExecutingUserId()` is a `FIXME` that always succeeds
  with `-1`, so no user check gates the profiler there. A run that mentions a login requirement is
  not marked down for it, and one that omits it is not either. This is also why Debug on production
  is unsafe: anyone could append `?umbDebug=true`.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that the .NET SDK is installed and it may run `dotnet build` on the project;
- that it may read the Umbraco documentation;
- that no IDE, debugger or browser is available, so it cannot launch Visual Studio, Rider or VS Code
  or run the site;
- to save its reply to `outputs/answer.md` and a plain log of the steps it took to `transcript.md`.
  Name both files as required deliverables, so a run doesn't skip them as "report files".

Give the with-skill run the path of a **snapshot** of the skill that contains only `SKILL.md`,
`references/` and `assets/`, not the skill folder itself. `evals/` holds the expectations and
`examples/` holds the tests, and a run that can reach them will read them; an instruction not to
read them is not enough. Tell the baseline run not to read `plugins/`, `.claude/` or any Umbraco
source checkout, or it will find the skill by browsing.

Do not tell either side to avoid writing outside the project: see the next section.

## Outside-the-project check

`dotnet add package` in a centrally-managed project edits the nearest `Directory.Packages.props`
above it. The copy in `outputs/` is there to absorb that, so the repo's own file is never touched.
After a run, diff the copy against the repo-root original; a difference means the run reached
outside `project/`.

## Reviewing in the viewer

The evaluator's viewer lists only the files directly inside `outputs/`, not those under
`outputs/project/`. Before generating the report, copy each file the run created or changed up into
`outputs/` (for example as `changed - .vscode__launch.json` or
`changed - Controllers__ProductListingController.cs`), or a reviewer sees only `answer.md` and
will think the code changes are missing.

## Grading notes

Grade code and config expectations from the files under `outputs/project/`, not from `answer.md`
alone.

- **Eval 1** is advice only: Visual Studio's debugger options are per machine, not in the project.
  Grade from `answer.md`. Exact menu paths can drift between VS 2022 updates; the setting names
  (`Enable Just My Code`, `Enable Source Link support`, `NuGet.org Symbol Server`,
  `Suppress JIT optimization on module load`, `Load only specified modules`, Modules window) are
  what is graded.
- **Eval 2**: grade every launch.json expectation from `.vscode/launch.json`, one per planted fault.
  `symbolOptions` must be an object on the configuration holding `searchNuGetOrgSymbolServer: true`;
  the fixture's top-level `searchMicrosoftSymbolServer` left where it was fails expectation 2 even if
  `symbolOptions` is added. Removing `sourceLinkOptions` entirely passes expectation 3.
- **Eval 3** is advice plus one negative file check: `Acme.Web.csproj` must not gain `DebugType` or
  `EmbedAllSources` as the fix. Rider's option labels vary by version; grade on what the setting does
  (symbol server, sources from symbol files / Source Link with remote download, stepping into external
  code), not on its exact label.
- **Eval 4**: grade expectations 1 to 3 from the files. For expectation 3, `appsettings.json` must
  not leave `Umbraco:CMS:Hosting:Debug` true, and `appsettings.Development.json` must hold it under
  `Umbraco` > `CMS` > `Hosting` > `Debug`; a key elsewhere (e.g. `Umbraco:CMS:Global:Debug`, or a
  top-level `Debug`) fails. Expectation 2 needs a separate step per section; one step around the
  whole action fails. Expectation 5 fails on any "turn it on in production briefly and then off".
- **Eval 5**: expectations 1 and 2 are graded from `Components/LatestNewsViewComponent.cs`. A
  `static` field remaining is fine only if something clears it on publish. Expectation 4 may be met
  in `answer.md` or in a `.vscode/launch.json` the run wrote. For expectation 5 either branch passes.
- **Debug in production** ("does not advise ... in production"): any instruction to set
  `Umbraco:CMS:Hosting:Debug` true on the live site, or to leave it in the base `appsettings.json`,
  fails, whatever caveats come with it.
- The build-honesty expectations: no run has an IDE, debugger or browser, so a run passes by not
  claiming to have stepped into Umbraco, loaded symbols, opened the profiler or seen timings. A
  `dotnet build` the run really ran and logged may be reported. It fails if it presents the skill's
  own runtime gate as verification of its own edit.
- The razor-build expectation is graded from `razor-build.txt` beside the run's `outputs/`, written
  after the run (not by it) with `dotnet build -p:RazorCompileOnBuild=true` on `outputs/project`.
  Exit code 0 passes; anything else fails, whatever the run said. For evals 1 and 3, where the
  project may be untouched, it still catches a run that broke the csproj.

## Results

Run on 2026-10-15 with the method above: all five evals, as Claude Code runs on `claude-haiku-5-5`
and `claude-sonnet-5-5`, one run per cell, graded by Haiku 5.5. Every run had the .NET SDK,
permission to build and the Umbraco docs; neither side had an Umbraco instance, an IDE or an MCP.
The harness then built every edited project with `dotnet build -p:RazorCompileOnBuild=true` and
wrote `razor-build.txt`: all 50 builds succeeded. None of the 50 runs modified the
`Directory.Packages.props` copy.

Four rounds were run. The baselines don't depend on the skill, so the round 1 baselines stand for
every round; each later round re-ran only the with-skill side, under the skill's wording at the
time. The table is round 4, the skill as committed. Read single runs as a range, not a point: one
expectation either way between rounds is normal run-to-run variation.

| Eval | Haiku with | Haiku without | Sonnet with | Sonnet without |
|---|---|---|---|---|
| 1 Visual Studio 2022, F11 steps over `Save` | 9/9 | 9/9 | 9/9 | 9/9 |
| 2 VS Code `launch.json` fix | 9/9 | 7/9 | 9/9 | 9/9 |
| 3 Rider shows decompiled code | 10/10 | 8/10 | 10/10 | 10/10 |
| 4 Slow page, MiniProfiler, Debug in base config | 10/10 | 7/10 | 10/10 | 6/10 |
| 5 Stale news box, both approaches | 8/8 | 6/8 | 8/8 | 6/8 |
| **Total** | **46/46 (100%)** | **37/46 (80%)** | **46/46 (100%)** | **40/46 (87%)** |
| Mean time per run | 212 s | 307 s | 151 s | 464 s |
| Mean tokens per run | 87k | 76k | 79k | 82k |

What the baselines missed:
- **Debug mode in production (eval 4, both models).** Both baselines left `Hosting:Debug: true` in
  the base `appsettings.json` and offered to switch it on in production for a short window. Neither
  knew that starting a profile has no login check, or that `IProfiler.Step` calls can stay in code.
- **Mixed approach (eval 5, both models).** Both baselines found and fixed the static cache, but gave
  no Source Link launch settings and no Debug/MiniProfiler route for staging, falling back on logs.
- **Haiku without the skill** also left the symbol-server key at the top level of `launch.json` with
  no NuGet.org server and no `suppressJITOptimizations` (eval 2), and offered cloning Umbraco as a
  fallback in Rider (eval 3).

Evals 1 to 3 don't separate the two sides on Sonnet: a strong model already knows the IDE settings.
There the skill's value is speed, a mean of 151 s against 464 s across all five evals.

### Round history

| Round | Haiku with | Sonnet with | What changed before the round |
|---|---|---|---|
| 1 | 45/46 | 46/46 | First draft |
| 2 | 42/46 | 44/46 | Production rule made absolute (no "temporary" switch-on); the probe made opt-in |
| 3 | 45/46 | 45/46 | A reply checklist in each reference; one profiler step per suspect |
| 4 | 46/46 | 46/46 | Parse `launch.json` before calling it valid; say plainly the steps can stay |

Round 2 dropped because answers left out points the references already made: the content was there,
but it didn't reach the reply. The reply checklists in round 3 fixed that. The two misses left in
round 3 were Sonnet writing single backslashes into the `launch.json` regex, which broke the file,
and Haiku saying the profiler steps "won't show in production" instead of saying they can stay.
Round 4 addressed both.

Harness notes from these rounds:
- The first round's eval 2 fixture had single backslashes in its `serverReadyAction` regex, which
  isn't valid JSON. It was fixed before round 2, and no round 1 run was marked down for it.
- Haiku graders sometimes report a pass count in their reply that doesn't match their own
  `grading.json`. Count from the file, and check it holds one entry per assertion.
