# Runtime validation (the `dotnet test` gate)

`evals/evals.json` grades whether Claude *writes* the right code. This gate proves the code
*compiles and serves* — no LLM, no grader, reproducible pass/fail, run on every PR. Both matter and
neither substitutes for the other: an eval can score full marks on code that doesn't build, and a
green gate says nothing about whether Claude picks the right approach.

To run it: `dotnet test Umbraco-CMS.Skills.sln`. For booting the site by hand, inspecting its
content, and debugging failures, see the `umbraco-reference-instance` skill — this file covers only
what *you* produce as an author.

## What actually gets checked

Adding a skill to the gate buys four distinct things. Know which you're getting, because it's easy
to assume more coverage than exists:

1. **It compiles.** The example project is built as part of the solution, so an API that no longer
   exists fails the build. This is the highest-value, lowest-effort layer — it's what caught
   `umbraco-custom-error-pages` shipping pre-v14 APIs (`FirstChild(alias)`, `GetAtRoot()`) that
   could not compile on Umbraco 17 at all.
2. **It serves.** An NUnit fixture boots the reference instance in-process and asserts real HTTP
   responses — status, content type, rendered body.
3. **It can't have drifted.** The example is projected from your `assets/` into `obj/` at BUILD
   time and nothing is committed, so the code compiled and served IS the code the skill ships. There
   is no second copy, so there's no drift check to run or forget.
4. **The content it needs exists.** Declared in the manifest (below) and asserted centrally.

Note the limit: **only committed `assets/` can be gated.** A reference that says "fetch the tutorial
and copy its code" produces nothing to compile or assert, so that approach is invisible here. If you
want an approach gated, the skill has to ship its artefacts — for a content-shaped approach that
means a `.cshtml` plus a `package.xml`, not just a link.

## Two hosts, split by artefact type

There are two reference sites, and which one your approach targets follows from what the approach IS:

| | **site 1 — `Umbraco-CMS.Skills`** | **site 2 — `Umbraco-CMS.Skills.Blank`** |
|---|---|---|
| Starter kit | Clean | **none** |
| For approaches expressed as | C# that registers into DI | Document Types + templates + config |
| Content comes from | Clean's install | each example's own package migration |
| Declare with | `"host": "clean"` (or omit) | `"host": "blank"` |
| Fixture file name | `*Tests.cs` | `*BlankTests.cs` |
| Generated models | yes (`InMemoryAuto`) | **none** — use non-generic `UmbracoViewPage` + `Value<T>("alias")`, or the template won't compile |

Two reasons the split exists, and neither is about tidiness:

- **Clean is itself a competing implementation.** It ships an `xMLSitemap` Document Type with its own
  `Views/xMLSitemap.cshtml`, and an `error` type with its own view. A content-shaped approach asserted
  on the Clean host can pass on *Clean's* implementation while the skill's guidance is broken.
- **Some approaches can never share a process.** `SetContentLastChanceFinder` is
  `AddUnique<IContentLastChanceFinder, T>()`, and what it replaces is Umbraco's own
  `ContentFinderByConfigured404` — which is the config-based 404 approach's entire implementation. No
  naming scheme dodges that.

Because there is one host per approach kind, **a skill may document at most two approaches**. That cap
is what keeps the host count at two as skills are added.

## What you add

```
plugins/<plugin>/skills/<skill>/
├── assets/                                  # the source of truth (already yours)
└── examples/<approach>/                     # one folder PER APPROACH, not per skill
    ├── .generate.json                       # the recipe (see below)
    ├── <Skill>.Example.csproj               # Microsoft.NET.Sdk.Razor for C#; plain Sdk otherwise
    ├── <Skill>Tests.cs                      # your fixture, beside the code it tests
    └── ExampleHostWiring.cs                 # only if the skill needs Program.cs/config changes
```

No generated files here: they're written into `obj/` during the build. Everything committed in this
folder is hand-written.

Then one `<ProjectReference>` from whichever host the approach targets.

Three things about this layout are load-bearing:

- **One folder per approach.** Two approaches often can't coexist in one host — the sitemap skill's
  two controllers both map `GET /sitemap.xml`, and only one `IContentLastChanceFinder` can be
  registered at all (it's a setter). Separate projects let each be compiled even when only one can
  be loaded.
- **Tests live here, but compile elsewhere.** Each test host globs its own fixtures out of
  `**/examples/*/` — `*Tests.cs` into the Clean assembly, `*BlankTests.cs` into the blank one —
  because Umbraco keeps process-wide static state and every fixture must share one host per process.
  A test project per skill would boot an Umbraco each; two assemblies is the minimum that lets two
  hosts coexist. `UmbracoHostSentinel` fails loudly if they ever land in one process.
- **Host wiring ships inside the example.** If your skill tells users to edit `Program.cs` or
  `appsettings.json`, do the equivalent from an `IComposer`/`IUmbracoPipelineFilter` in the example
  rather than editing the shared instance. See `umbraco-custom-error-pages`, which applies
  `UseExceptionHandler` and a `ReservedPaths` entry that way.

## How `.generate.json` works

The generator (`scripts/generate-examples.mjs`) copies your `assets/` files into the example,
substituting placeholders. `assets/` stays the single source of truth; the committed example is a
mechanical projection of it, which is what lets the gate prove that *the code users are told to copy*
is the code that was tested.

```json
{
  "_comment": "Free-text note. JSON has no comments; nothing reads this key.",
  "host": "clean",
  "namespace": "Umbraco.Skills.Examples.<Skill>",
  "placeholders": { "<ErrorPageAlias>": "error" },
  "assets": ["PageNotFoundContentFinder.cs", "ErrorController.cs"],
  "requires": { "documentTypeAliasAtRoot": ["<ErrorPageAlias>"] }
}
```

- **`host`** — `"clean"` (site 1) or `"blank"` (site 2). Optional; absent means `"clean"`. A value no
  test assembly claims is caught by `Every_manifest_targets_a_known_host`, because otherwise the
  example's preconditions would silently never run.
- **`namespace`** — what `<Namespace>` becomes. Required.
- **`assets`** — which of the skill's asset files this example compiles. Required. List only ONE
  approach's files; mutually exclusive assets in one project give you two endpoints on one route.
- **`placeholders`** — every *other* `<Token>` your assets carry, mapped to a value that exists in
  the instance. **Miss one and it silently survives into the generated code** as a literal string:
  it still compiles, so nothing complains, and the code can simply never find what it's looking for.
  You no longer have to catch this by eye — `scripts/generate-examples.mjs` fails the build on any
  `<Token>` in a string literal or `namespace` declaration that the manifest doesn't declare, and on
  any declared placeholder no asset uses. It found exactly this bug in the sitemap skill, where an
  unmapped `<filterAlias>` had made the hide-from-sitemap filter a permanent no-op.
- **`requires`** — content the example needs in order to prove anything, asserted by
  `ReferenceContentPreconditionsTests`. A `<Token>` entry is resolved through your own
  `placeholders` map, so an alias is declared once rather than repeated into a test. Omit the key
  entirely if the skill needs no particular node.

Generation happens automatically during the build. To check a manifest without building, run
`node scripts/generate-examples.mjs --lint`. Skills whose `assets/` aren't on the current branch are
skipped, so both are safe before your skill PR merges.

Two behaviours worth knowing: an asset listed in `assets` but missing from disk is an error, while
an unknown top-level key (a typo'd `requires`, say) is silently ignored — so if a precondition
never seems to run, check the spelling first.

## Writing the fixture

Model it on `SitemapTests.cs` or `CustomErrorPagesTests.cs`. The essentials:

- Use your host's shared client: `ReferenceSiteFixture.Client` or `BlankSiteFixture.Client`.
  **Never** construct a factory yourself — a second host in the same process leaves whichever fixture
  runs later resolving services from a disposed provider, and the symptom is a fixture that passes
  alone and fails in a full run.
- Assert what the skill promises, over HTTP: status code, content type, and a marker proving the
  right *content* rendered — not merely that something responded.
- If you mutate content, restore it in a `finally`. Other fixtures share the instance.
- If you mutate content and then assert on output, wait for the source the code under test actually
  reads before asserting. Umbraco's published cache updates asynchronously, and reading too early
  can rebuild stale output *and cache it*. `SitemapCacheInvalidationTests` documents this trap in
  detail; the short version is that a key lookup or the Delivery API are not stand-ins for the
  traversal your code performs.

**Then prove the test can fail.** Break the thing it checks — point a `requires` alias at a
nonexistent Document Type, or change a cache key — confirm it goes red, and revert. A test that
passes either way is worse than no test, because it reports coverage you don't have.

When you break something to check this, restore it with `touch` on the file afterwards, or verify
the rebuilt binary: `mv file.bak file` restores an older mtime, MSBuild then skips recompiling, and
the broken build silently persists into later runs. That cost hours once.
