# Conformance checklist (self-audit)

Run this on the skill you're building before you ship it. Check each item against the actual files;
fix anything that fails. A skill is ready only when every applicable item passes.

**Placement & naming**
- [ ] Folder is kebab-case in the correct plugin (`content-modelling` vs `implementation`), or in
  `.claude/skills/` for authoring tooling
- [ ] `name` in frontmatter matches the folder name exactly

**Description (triggering)**
- [ ] `description` has concrete **trigger phrases** (real user wordings, not just "use for X")
- [ ] `description` has explicit **SKIP** conditions
- [ ] `description` is at most 1024 characters (`validate-skills.mjs` fails above that)
- [ ] Doesn't collide with an existing skill's triggers (or the overlap is intentional and noted)

**SKILL.md body**
- [ ] Thin/routing — doesn't inline detail or `if/else` branching that belongs in references/assets
- [ ] Tells the agent to open the reference and start from its asset **before writing code**
- [ ] Decision table + "how to decide" present when there's more than one approach
- [ ] Version compatibility stated
- [ ] Best-practices section is domain guidance specific to this skill, not restated code. A rule a hub
  skill owns (`umbraco-content-model-conventions`, `umbraco-content-requirements-documentation`) is
  linked with an anchor, never restated: a copy drifts, and the agent then reads whichever it finds first
- [ ] Validation section points at `evals/evals.json`
- [ ] No doc link duplicated between SKILL.md and a reference file

**references/**
- [ ] One file per approach, each with "when to choose" + cross-link to the alternative
- [ ] All fetch-me doc links use the `.md` version (or link a sibling skill instead of raw docs)
- [ ] Says "fetch the docs first" wherever the code isn't verbatim in `assets/`
- [ ] Every docs snippet a build or the gate disproved is in a *Docs corrections* table
- [ ] (implementation skills) The skill links the Umbraco docs it's built on at least once, in SKILL.md
  or a reference. A skill with no docs link is teaching from memory
- [ ] A reference over 100 lines starts with a `## Contents` index: links to its own sections, each
  saying when to read it (`- [Fallback](#fallback): read when a value is missing in this culture`), so
  the agent loads only the part it needs. `scripts/check-skill-docs.mjs` checks both rules

**assets/ (if present)**
- [ ] Templates use `<Placeholder>` tokens with optional/removable lines marked
- [ ] Template code follows the skill's own best-practices section
- [ ] Views use `@inherits UmbracoViewPage<T>` (never a bare `@model`) and wrap generic calls in `@(...)`
- [ ] Assets that install to a nested path carry their install path in a first-line comment
- [ ] Markdown assets (requirements page templates, report templates) are documents, not code. They still
  use `<Placeholder>` tokens, but an optional row or line is named in the reference file that
  describes the format, because a comment inside a markdown table breaks the table

**scripts/ (if present)**
- [ ] Deterministic, repeatable work lives in a script the skill points at, not re-derived in prose

**evals/**
- [ ] `evals/evals.json` present with realistic, multi-step prompts (one per meaningful scenario)
- [ ] `expectations[]` are objective/checkable, including a build-honesty expectation
  (for views: `dotnet build -p:RazorCompileOnBuild=true` under ModelsMode `Nothing`/`SourceCode*`,
  a page request under `InMemoryAuto`)
- [ ] Expectations accept every valid fix, not one implementation, and check the feature is wired in

**Approaches (count is NOT the constraint — coverage is)**
- [ ] **At most two approaches per skill.** This is load-bearing, not editorial: one host exists per
  approach *kind* (below), so two approaches keep the gate at exactly two hosts however many skills
  are added. A third approach means a third host.
- [ ] Each approach declares a coverage tier, and the tier is true:
  | Tier | Requires | What you may then claim |
  |---|---|---|
  | **Asserted** | `examples/<approach>/` + a `<ProjectReference>` from a host + a fixture on rendered output | "this runs on the pinned Umbraco" |
  | **Compiled** | `examples/<approach>/` in the .sln, referenced by no host | "this builds against the pinned Umbraco" |
  | **Documented** | no example; ships no code anywhere in the repo | "an eval graded the guidance; nothing ran" |
- [ ] At least one approach is **Asserted**. A skill where nothing runs isn't gated. The one
  exception is a **Documented-only** skill, below.
- [ ] **Documented** is only legal when the approach ships no code at all. "Committed but ungated" is
  not a tier. Markdown in `assets/` is not code: a skill whose assets are all `.md` ships no code.
- [ ] Complementary building blocks sharing one example are counted as one approach, not several.
- [ ] **Documented-only skills.** A skill that ships no code (no `assets/`, or markdown assets only)
  is tier **Documented** throughout, with no Asserted approach and no `examples/`. That is legal only
  when all of these hold:
  - its `## Validation` section states the tier in words ("Documented ... nothing ran"), so nobody
    assumes runtime coverage that isn't there;
  - it ships no compiled code. An inline snippet a reference needs for context (a two-line Razor
    layout stub) is allowed only when it is marked as uncompiled in `## Validation` and points at the
    implementation plugin or the Umbraco docs for the real thing;
  - it is validated by `evals/evals.json`, with one MCP-connected and one MCP-absent prompt when the
    skill touches the backoffice. A recorded fixture of MCP read results (`evals/files/mcp/*.md`)
    satisfies the MCP-connected prompt: evals cannot reach a live server;
  - if it writes or applies schema requirements docs, the requirements docs it produces pass
    `plugins/content-modelling/skills/umbraco-content-requirements-documentation/scripts/lint-requirements.mjs`, which CI
    runs against that skill's golden requirements and broken fixture;
  - Umbraco Developer MCP versus manual backoffice steps is one approach with two mechanisms, not an
    A/B pair, so it does not count towards the two-approach limit.
- [ ] Variants *inside* an approach carry their own tier. The two-host split does not fix
  intra-approach collisions — sitemap's split-index controller still maps the same `/sitemap.xml` as
  the single-file one, so it stays **Compiled**.

**Which host does the approach target?**
- [ ] Decided by ARTEFACT TYPE, not by the A/B letter:
  - C# that registers into DI (controllers, `IContentFinder`, composers) → **site 1**,
    `Umbraco-CMS.Skills` (Clean). Clean is harmless there — ambient content to traverse and render.
  - Document Types + templates + config → **site 2**, `Umbraco-CMS.Skills.Blank` (no starter kit).
    Clean must be absent, because Clean is itself a competing implementation of these features
    (`xMLSitemap` type + view, `error` type + view). Asserting on the Clean host would prove Clean
    works, not that the skill's guidance works.
- [ ] Declared as `"host": "clean" | "blank"` in `.generate.json` (absent means `clean`).
- [ ] Before assuming two approaches can share a host, check for **registration conflicts**: the same
  route template, or any `AddUnique`/`Set*` builder extension. `SetContentLastChanceFinder` REPLACES
  Umbraco's own `ContentFinderByConfigured404` — so a content-finder 404 approach and a config-based
  404 approach can never both be live in one process, whatever they're named.

**Runtime gate (skip only if the skill ships no code in `assets/`: nothing, or markdown only — and say so)**
- [ ] `.generate.json` maps **every** placeholder the assets carry. Enforced by
  `scripts/generate-examples.mjs --lint`, because a missed one survives substitution as a literal
  string, still compiles, and silently never matches.
- [ ] Nothing generated is committed — examples are projected from `assets/` into `obj/` at build
  time, so there is no second copy that can drift.
- [ ] Content the example depends on is declared via `requires`, not asserted by hand.
- [ ] Fixture uses its host's shared client (`ReferenceSiteFixture.Client` or
  `BlankSiteFixture.Client`), restores any content it mutates, and asserts rendered content rather
  than just a status code.
- [ ] Site-2 fixtures are named `*BlankTests.cs` — that suffix is what routes them into the blank test
  assembly, and therefore into their own process.
- [ ] The test was **proven able to fail** — broken deliberately, seen red, reverted.

**Before shipping**
- [ ] `node scripts/validate-skills.mjs --check-index` passes (run `--write-index` first)
- [ ] `node scripts/check-skill-links.mjs` passes — every relative link, including links into a
  sibling skill, resolves, and every `#anchor` names a heading
- [ ] `node scripts/check-skill-frontmatter.mjs` passes — `name` matches the folder, only `name` and
  `description` keys, description at most 1024 characters (the Agent Skills limit; Claude Code allows
  1536), and every file an eval names exists
- [ ] README skills table for the skill's plugin has the skill's row
- [ ] No `.claude/.cc-writes` leftovers under `plugins/*/skills/`
- [ ] Eval'd against a baseline with `umbraco-skill-evaluator`
