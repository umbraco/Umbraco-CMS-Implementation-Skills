# Running these evals

Run all three evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet. A stronger model's
baseline can often find the Umbraco 17 traps by decompiling the packages, so a single strong-model
run shows little difference between the two sides.

## Project fixture (every eval, both sides)

Each run gets its own sandbox:

```
<run>/outputs/
  Directory.Packages.props     # copy of the repo-root file, so the project restores and builds
  project/                     # the "user's project": the agent edits here, in place
```

To build `project/`:
1. Copy `files/project/` into it. This is a minimal Umbraco 17 site with root namespace `Acme.Web`:
   - the stock Umbraco csproj settings;
   - `Views/master.cshtml`, which reads `siteName` with an ancestors fallback and renders a
     footer partial;
   - ModelsBuilder in `Nothing` mode.
2. Overlay the eval's own folder from its `files` list, replacing any file with the same name:

| Eval | Overlay | Starting state |
|---|---|---|
| 1 | `files/article-list/` | ModelsBuilder `SourceCodeAuto` with generated `ArticleList` / `Article` models, a slow `Views/articleList.cshtml` that renders every article, and a registered `IArticleStatsClient` |
| 2 | `files/newsletter/` | A registered `IMailingListClient` |
| 3 | `files/event-page/` | The colleague's broken `Views/eventPage.cshtml` (`IHtmlString`), and a master layout that renders the footer with `@Html.Partial` |

All three assembled projects build. The views are compiled at runtime, as in the stock template.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that it may run `dotnet build` and fetch public docs;
- to save its reply to `outputs/final-answer.md` and a transcript to `transcript.md`.

Give the with-skill run the path of a **snapshot** of the skill that contains only `SKILL.md`,
`references/` and `assets/`, not the skill folder itself. `evals/` holds the expectations and
`examples/` holds the tests, and a run that can reach them will read them; an instruction not to
read them is not enough. Tell the baseline run not to read `plugins/`, `.claude/` or any Umbraco
source checkout, or it will find the skill by browsing.

## Reviewing in the viewer

The evaluator's viewer lists only the files directly inside `outputs/`. Before generating the
report, copy each file the run created or changed under `outputs/project/` up into `outputs/` (for
example as `changed - Views__articleList.cshtml`), or a reviewer sees only `final-answer.md`.

## Grading notes

Grade code expectations from the files under `outputs/project/`, not from `final-answer.md` alone.
The Razor-compile expectation uses `dotnet build -p:RazorCompileOnBuild=true`. That only works because
every fixture runs ModelsBuilder in `Nothing` or `SourceCodeAuto`. Under `InMemoryAuto` the generated
models exist only at runtime, so a fixture in that mode needs the page-request check instead.

The build-honesty expectation can now be checked: if the run says the build succeeded, the
assembled project should build.
