# Running these evals

Run all three evals in `evals.json` together, with and without the skill, using the
`umbraco-skill-evaluator` skill. Run them once on Haiku and once on Sonnet, and report each model
separately. A strong default model's baseline can find the Umbraco 17 traps by reading the
Umbraco source, so a single strong-model run shows little difference between the two sides.

## Project fixture (every eval, both sides)

Each run gets its own sandbox:

```
<run>/outputs/
  Directory.Packages.props     # copy of the repo-root file, so the project restores and builds
  project/                     # the "user's solution": the agent edits here, in place
    MySite.Web/                # the Umbraco host; build this project
```

To build `project/`:

1. Copy `files/project/` into it: a minimal Umbraco 17 host, `MySite.Web`, with the stock
   `Program.cs` chain (`AddBackOffice`, `AddWebsite`, `AddDeliveryApi`, `AddComposers`) and
   ModelsBuilder in `Nothing` mode.
2. Overlay the eval's own folder from its `files` list, replacing any file with the same name:

| Eval | Overlay | Starting state |
|---|---|---|
| 1 | `files/legacy-products/` | A `MySite.Core` class library referenced by the host, with an `ISkuRepository` (reads the SKU table through `IScopeProvider`) and an HTTP `ISearchServiceClient`, neither registered |
| 2 | `files/acme-package/` | An `Acme.Umbraco.Media` project standing in for the package: a public `IAcmeMediaUrlResolver`, an **internal** composer that registers Acme's default implementation as scoped, and a public `AcmeHealthCheck`. The host also has the user's `Media/CdnMediaUrlResolver.cs`, not yet registered |
| 3 | `files/v13-migration/` | The prompt's v13 code under `MySite.Web/Infrastructure/`, plus the types it uses. It compiles with the two CS0618 warnings the prompt describes, and anchoring to `ContentFinderByUrl` really fails at boot |

3. Restore and build `project/MySite.Web` once before the run. All three assembled projects build.
   A restored project also builds inside a sandbox with `dotnet build --no-restore`, so tell the
   runs to use that.

## Run prompt

Each run is told:
- where the project is, and to edit it in place;
- that it may run `dotnet build --no-restore` on `project/MySite.Web` and fetch public docs;
- to save its reply to `outputs/final-answer.md` and a transcript to `transcript.md`.

Give the with-skill run the path of a **snapshot** of the skill that contains only `SKILL.md`,
`references/` and `assets/`, not the skill folder itself. `evals/` holds the expectations and
`examples/` holds the tests, and a run that can reach them will read them. Tell the baseline run
not to read `plugins/`, `.claude/`, the snapshot or any Umbraco source checkout.

## Grading

After each run, build `outputs/project/MySite.Web` yourself with
`dotnet build --no-restore --no-incremental` and save the output beside the run as `build.txt`.
`--no-incremental` matters: if the run already built, an incremental build skips compilation and
prints no CS warnings, so eval 3's "no CS0618" expectation would pass on missing evidence. Grade the compile expectation (and eval 3's CS0618 expectation) from that file, not
from the transcript. The build-honesty expectation is separate, so a broken build isn't failed twice.

Then boot it, because the traps these evals test fail at startup, not at compile time, and probe it,
so registrations are graded from the running site rather than from reading the code:

```bash
node scripts/boot-check.mjs <run>/outputs/project/MySite.Web --out <run>/boot.txt --probe <run>/probe.json
```

| Eval | Boot |
|---|---|
| 1 | Add `--seed-sql plugins/implementation/skills/umbraco-composing/evals/files/boot/legacy-products-seed.sql`. `MySite.Core`'s SKU repository reads a `legacySku` table a fresh install doesn't have, so an unseeded warm-up throws and takes the site down. That's a fixture gap, not the run's mistake. |
| 2 | As above. |
| 3 | Boot twice. First as above. Then copy the project, add `files/boot/DisableAnalytics.cs` to its `MySite.Web/Infrastructure/`, and boot the copy with `--out <run>/boot-disabled.txt`. A strong `[ComposeAfter]` on `AnalyticsComposer` builds, then fails that second boot with `Broken composer dependency`. |

Grade the boot expectation from those files only: `RESULT: PASS` passes it, anything else fails it.
Grade the runtime expectation from `probe.json`: the content finder order, the components, the
notification handlers and service registrations (with lifetimes) from the solution's own projects,
and the health checks, all as the booted site has them.

Grade code expectations from the files under `outputs/project/`, not from `final-answer.md` alone.
Before generating the viewer, copy each file the run created or changed under `outputs/project/` up
into `outputs/`, because the viewer lists only the files directly inside `outputs/`.
