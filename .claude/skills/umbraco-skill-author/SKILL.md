---
name: umbraco-skill-author
description: >
  A framework for authoring Umbraco skills in THIS marketplace: how to structure, scaffold, write,
  and audit a skill so it matches the house conventions. Use this whenever the user wants to start
  or build a new skill for the Umbraco marketplace, e.g. "make a new skill for X", "how should I
  structure this skill", "turn these docs into a skill", "scaffold a skill", or "get this skill
  ready to ship". Ends with a self-audit checklist, then hands off to `umbraco-skill-evaluator`
  for the eval loop — this skill covers authoring, not evaluation.
  SKIP: non-skill work, and general skill-building unrelated to this Umbraco marketplace (use the
  generic skill-creator for those).
---

# Umbraco Skill Author

A framework, checklist, and guide for building Umbraco skills. Follow it to go from an idea to a
shippable skill that matches the house conventions, then self-audit before handing off.

The shape of every skill: a **thin** SKILL.md that routes, detail in `references/`, code templates
in `assets/`, deterministic helpers in `scripts/`, and objective assertions in `evals/evals.json`.

**Golden-standard example:** [`umbraco-sitemap`](../../../plugins/implementation/skills/umbraco-sitemap)
is the current reference skill. When in doubt, open it and copy its shape.

## How to use this

1. **Build** — walk [`references/authoring-steps.md`](references/authoring-steps.md): scope →
   scaffold → SKILL.md → references → assets → evals. Copy-paste skeletons are in
   [`templates/skill-template.md`](templates/skill-template.md).
2. **Prove it runs** — if the skill ships `assets/*.cs`, add it to the `dotnet test` gate:
   [`references/runtime-validation.md`](references/runtime-validation.md).
3. **Audit** — before shipping, self-check against
   [`references/conformance-checklist.md`](references/conformance-checklist.md).
4. **Hand off** — pass the finished skill to `umbraco-skill-evaluator` to run with-skill vs.
   baseline and prove it earns its keep, then iterate from the results.

Steps 2 and 4 answer different questions and neither covers for the other: the gate proves the code
**compiles and serves**, the evals grade whether Claude **writes** it. A skill can pass one and fail
the other.

## Core principles (what the checklist enforces)

- **Thin SKILL.md.** It routes; it doesn't teach everything. Push detail, per-approach steps, and
  `if/else`-style branching into `references/` or `assets/`, in separate files, not inline. A router
  only works if the agent follows it, so SKILL.md must tell the agent to open the reference and
  start from its asset **before writing code**. Weaker models otherwise read SKILL.md alone and
  never see the traps the references cover. Adding that one line took Haiku from 56% to 100% on
  `umbraco-templating`'s evals.
- **Docs are the source of truth, but verify them.** Don't reproduce Umbraco API code from memory;
  link the docs and tell the agent to fetch them first. Ship verbatim code in `assets/` only when
  it's genuinely not in the docs (and say so). The docs pages also carry snippets that don't
  compile or throw on the pinned version (v8-era types, missing `using`s, overloads that are
  ambiguous at runtime). When a build or the gate disproves one, record it in the reference's
  *Docs corrections* table and ship the working version in `assets/`. Never forward a snippet
  you haven't checked.
- **`.md` doc links.** Fetch-me doc links point at the `.md` page (e.g. `.../composing.md`), or link
  a sibling skill instead of raw docs — the `.md` endpoint is what the agent can actually fetch as
  source, and a sibling-skill link keeps discovery progressive (lazy-loading) rather than dumping
  everything up front.
- **Don't duplicate.** A doc link in a reference file isn't repeated in SKILL.md — a fact stated in
  two places drifts out of sync, and the copy the agent reads is then a coin toss.
- **Prefer a script over prose for deterministic work.** If a step is a fixed, repeatable operation
  (scaffolding, validation, a lint check), bundle it in `scripts/` and point at it, rather than
  asking the agent to re-derive it every run. This is also what lets the conformance checklist be
  enforced by a deterministic CI check on the PR, not just a self-audit.
- **Build honesty.** Never claim a verified build you didn't run.
- **Right place.** content-modelling → `plugins/content-modelling/skills/`; build-out/delivery →
  `plugins/implementation/skills/`; authoring tooling → `.claude/skills/`.
