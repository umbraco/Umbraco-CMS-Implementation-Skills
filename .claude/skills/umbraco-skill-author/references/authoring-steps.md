# Authoring steps

Follow these when building a new skill. End with the
[conformance checklist](conformance-checklist.md). Copy-paste skeletons are in
[`skill-template.md`](../templates/skill-template.md).

## Step 1 — Clarify scope

Pin down before writing anything (ask if ambiguous):

- **The task** the skill automates, phrased as user goals (these become trigger phrases). If it
  came from a project-board card, read the neighbouring cards too. Their topics become the
  description's **SKIP** lines, so two skills don't compete for the same request. Cards often link
  an old, unversioned docs page; use the `17.latest/…/<page>.md` equivalent.
- **Which plugin** — `content-modelling` vs `implementation`.
- **One approach or several?** An official-docs way *and* a custom/code way → plan a decision table
  and one reference file per approach. **Two is the maximum**, and that's a hard rule rather than a
  style preference: the runtime gate runs one host per approach *kind* (C#/DI on the Clean host,
  Document Types + templates + config on the starter-kit-free host), so two approaches keep it at two
  hosts. See [runtime-validation.md](runtime-validation.md).
- **Complementary building blocks are not approaches.** A toolkit skill (`umbraco-templating`:
  template, partial, view component, hijacked controller, surface controller) routes between parts
  that work *together* and can share one `examples/` project. Only competing implementations of
  the *same* feature count towards the two-approach cap.
- **Version compatibility** — target Umbraco version and any "introduced in vX" APIs.
- **Backoffice vs file-based** — needs the Umbraco Developer MCP / manual backoffice steps, or pure
  `.cs`/`.cshtml`? This drives the MCP→manual→fallback guidance.

## Step 2 — Scaffold the folder

```
plugins/<plugin>/skills/<kebab-case-name>/
├── SKILL.md            # required
├── references/         # one file per approach (optional if single, trivial approach)
├── assets/             # code templates with <Placeholder> tokens (optional)
├── scripts/            # deterministic helpers the skill runs (optional)
├── evals/evals.json    # objective, repeatable validation
└── templates/          # copy-paste skeletons for authoring (optional)
```

Folder name is kebab-case and must match the `name` in frontmatter.

## Step 3 — Write SKILL.md (keep it thin — it routes)

**Frontmatter**
- `name:` kebab-case, matches the folder.
- `description:` must contain both **trigger phrases** (real user wordings) AND explicit **SKIP**
  conditions (e.g. "non-Umbraco projects or Umbraco < 17"). This is what makes the skill fire at
  the right time — treat it as the most important line in the file.

**Body (in this order)**
1. One-paragraph intro.
2. **Decision table** if there's more than one approach (Reference | Source | Backoffice work |
   caching | best-for …), then a short "How to decide" with a default and the MCP→manual→fallback
   order. Directly under the table, add a bold line telling the agent to **open the reference and
   start from its asset before writing any code**, naming one or two of the traps it covers.
   Without it, weaker models answer from SKILL.md alone.
3. **Version compatibility.**
4. **Best practices** — domain guidance, not just restated code.
5. **Validation** — a pointer to `evals/evals.json`.

Don't paste doc links here that already live in a reference file. Push per-approach detail and any
`if/else`-style branching into `references/`.

## Step 4 — Write references/<approach>.md (one per approach)

- Open with "When to choose this approach" and cross-link the alternative.
- List the documented **building blocks** with `.md` doc links (or links to sibling skills).
- Instruct: **fetch the docs before implementing** where the code isn't verbatim in `assets/`.
- Add a **Docs corrections (Umbraco <version>)** table (docs snippet | problem | use instead) for
  every snippet on the linked pages that a build or the gate disproved. Leave the section out when
  there are none.
- Give the step sequence: discover project context → write files (with placeholder substitution) →
  build/verify → "Done" summary for the user.

## Step 5 — assets/ (only if code is needed)

- Code templates with `<Placeholder>` tokens (`<Namespace>`, `<filterAlias>`, …).
- Mark optional/removable lines with a comment.
- The template code itself already follows the best-practices section.
- The placeholder lint only sees string literals and `namespace` lines. For identifiers such as a
  controller named after a Document Type alias, use a concrete example name (`ProductPageController`)
  and tell the agent in the reference to rename it.
- Assets are projected **flat, by file name**. A file that installs to a nested path gets a flattened
  name (`RelatedPages.Default.cshtml`) and a first-line comment giving its real install path
  (`Views/Shared/Components/RelatedPages/Default.cshtml`).
- **Razor assets:**
  - type every view with `@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<T>`, never a bare
    `@model T`. A bare `@model` fails to compile (CS1061) in any project whose `_ViewImports.cshtml`
    declares a non-generic base page, which many real projects do.
  - wrap generic calls in `@(...)`, e.g. `@(Model.Value<string>("alias"))`. Razor reads a bare
    `@Model.Value<T>(...)` as an HTML tag.

## Step 6 — Draft evals/evals.json

- Realistic, multi-step prompts (see `umbraco-skill-evaluator` for what makes a good prompt).
- One prompt per meaningful scenario: each approach, plus edge cases (headless, MCP-absent, …).
- `expectations[]` are **objective, checkable assertions** — not "the output is good".
- Include a **build-honesty** expectation. If the skill ships views, how to verify depends on
  `Umbraco:CMS:ModelsBuilder:ModelsMode`:
  - **`Nothing` or `SourceCode*`:** build with `dotnet build -p:RazorCompileOnBuild=true`. The
    stock Umbraco csproj sets `RazorCompileOnBuild=false`, so a plain build never compiles a view
    and passes when they're broken.
  - **`InMemoryAuto` (the default):** that build fails with CS0246 on a healthy site, because the
    generated models exist only at runtime. Verify with a plain build and a request to a page that
    renders the view.

  The skill itself should tell the agent the same, including never changing `ModelsMode` or
  rewriting views to make the Razor build pass.
- Write expectations as **outcomes, not one implementation**. If there are two valid fixes (a
  `[NonAction]` override or a template-named action; renaming the method or `[ActionName]`), accept
  both, or the grader fails correct work.
- Check the feature is **wired in**, not only written: a widget that is never invoked on the page,
  or a partial that is never rendered, should fail an expectation.
- How to run the evals so they measure anything (models, project fixture, isolation) is in
  `umbraco-skill-evaluator`.
- See the schema in [`skill-template.md`](../templates/skill-template.md), and `umbraco-sitemap`'s
  `evals/evals.json` for a worked example.

## Step 7 — Add it to the runtime gate (if it ships `assets/*.cs`)

Compiling the skill's own code and asserting its HTTP behaviour catches what evals can't: APIs that
no longer exist, and output that never renders. Full detail — the `examples/<approach>/` layout, how
`.generate.json` substitutes placeholders, and how to write a fixture that can actually fail — is in
[`runtime-validation.md`](runtime-validation.md).

A config-only or backoffice-only approach is NOT automatically exempt any more. If it can be expressed
as a `package.xml` (Document Types, templates, content) plus a template file, it can ship those as
assets and be asserted on the starter-kit-free host — that's how umbraco-sitemap's Approach B is
gated. Skip the gate only when the approach ships nothing at all, and then say so explicitly rather
than leaving it ambiguous, so nobody assumes coverage that isn't there.

## Step 8 — Audit and hand off

- Run `node scripts/validate-skills.mjs` (frontmatter, spec compliance) and
  `node scripts/generate-examples.mjs --lint` (placeholders), from the repo root.
- Self-audit against the [conformance checklist](conformance-checklist.md).
- Hand off to `umbraco-skill-evaluator` to prove value against a baseline; iterate from results.

## Step 9 — Ship

- Run `node scripts/validate-skills.mjs --write-index` so `AGENTS.md` lists the skill, then
  `--check-index`, which is what CI runs.
- Add the skill's row to the README's *Available skills* table.
- Run the whole gate once more on the final commit (both hosts). After a rebase onto `main`, run it
  again, plus the validators: `main` may have changed the tooling underneath you.
- Run commands from the repo root, not from inside a skill folder. Claude Code leaves an empty
  `.claude/.cc-writes` folder in whatever directory it works in. Inside `plugins/*/skills/` that
  folder fails `validate-skills.mjs` as a skill with no SKILL.md. Delete any you find with `rmdir`.
- Open the PR the way earlier skill PRs did:
  - a plain title, `Add <skill-name> skill`;
  - a body with Summary, How it is validated (tier plus local results for both hosts), Changes
    outside the skill (flag any edit to a shared host or tooling for review), Evals (a table per
    model), and Not covered;
  - no plugin version bump.
