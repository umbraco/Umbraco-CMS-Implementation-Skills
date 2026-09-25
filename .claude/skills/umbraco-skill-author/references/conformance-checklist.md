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
- [ ] Doesn't collide with an existing skill's triggers (or the overlap is intentional and noted)

**SKILL.md body**
- [ ] Thin/routing — doesn't inline detail or `if/else` branching that belongs in references/assets
- [ ] Decision table + "how to decide" present when there's more than one approach
- [ ] Version compatibility stated
- [ ] Best-practices section is domain guidance, not just restated code
- [ ] Validation section points at `evals/evals.json`
- [ ] No doc link duplicated between SKILL.md and a reference file

**references/**
- [ ] One file per approach, each with "when to choose" + cross-link to the alternative
- [ ] All fetch-me doc links use the `.md` version (or link a sibling skill instead of raw docs)
- [ ] Says "fetch the docs first" wherever the code isn't verbatim in `assets/`

**assets/ (if present)**
- [ ] Templates use `<Placeholder>` tokens with optional/removable lines marked
- [ ] Template code follows the skill's own best-practices section

**scripts/ (if present)**
- [ ] Deterministic, repeatable work lives in a script the skill points at, not re-derived in prose

**evals/**
- [ ] `evals/evals.json` present with realistic, multi-step prompts (one per meaningful scenario)
- [ ] `expectations[]` are objective/checkable, including a build-honesty expectation

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
- [ ] At least one approach is **Asserted**. A skill where nothing runs isn't gated.
- [ ] **Documented** is only legal when the approach ships no code at all. "Committed but ungated" is
  not a tier.
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

**Runtime gate (skip only if the skill ships nothing in `assets/` — and say so)**
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
- [ ] Passes `umbraco-skill-validator` and `umbraco-skill-code-analyzer` (if available)
- [ ] Eval'd against a baseline with `umbraco-skill-evaluator`
