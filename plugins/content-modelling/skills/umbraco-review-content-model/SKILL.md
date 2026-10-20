---
name: umbraco-review-content-model
description: >
  Audit an Umbraco 17+ content model against the content-modelling conventions and report what
  departs from them, ranked by severity, with the reason and the skill that would fix each
  finding. Reads the live schema through the Umbraco Developer MCP, or a markdown requirements
  folder, or schema the user provides. Reports only: it never changes the site or the requirements
  doc.
  Use this whenever the user asks to "review our content model", "audit our document types",
  "check our schema against best practice", "what's wrong with our Umbraco setup", "is our content
  model any good", "health check the document types", or "review the requirements folder".
  SKIP: non-Umbraco projects or Umbraco < 17; fixing what the review finds (use the add-* skills,
  after approval); reviewing C#, Razor or query performance (use umbraco-common-pitfalls); linting
  a requirements doc before applying it (use umbraco-content-requirements-documentation);
  explaining one rule (use umbraco-content-model-conventions).
---

# Review Content Model

Audits a content model and writes a **report**: every departure from the conventions, why it
matters, how serious it is, and which skill would fix it. It changes nothing. A review is the
input to a later, approved change, not the change itself.

The rules come from the sibling skill
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and reading
the schema and linting a requirements folder come from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md).
**Both must be installed alongside this one.** If a link into either cannot be read, stop and say
so; do not work from memory.

## What to review from

| Source | How | What it can prove |
|---|---|---|
| The live site, through the Umbraco Developer MCP | Read every type, data type and template with the read tools in [inspect-existing-schema.md](../umbraco-content-requirements-documentation/references/inspect-existing-schema.md) | What the site actually has |
| A requirements folder | Run the requirements linter first, then the judgement checks below on the pages | What the requirements doc says; not that the site matches it |
| Schema the user provides (an export, a read-back, a description) | Review what is given; list what was not provided | Only what was given |

Use the live site when the MCP is connected; it is the only source that shows the real model. The
source is one approach with different inputs, not a choice of method. State the source at the top
of the report, and never present a requirements doc or a description as the live site.

## The checks

| Area | Reference |
|---|---|
| Names, aliases, suffixes, property aliases | [checks-naming.md](references/checks-naming.md) |
| Tree, root, allowed children, templates, listings, settings | [checks-structure.md](references/checks-structure.md) |
| Tab, group and property sorts | [checks-sorts.md](references/checks-sorts.md) |
| Data types: reuse, constraints, folders, block editors | [checks-data-types.md](references/checks-data-types.md) |
| Descriptions and icons | [checks-descriptions.md](references/checks-descriptions.md) |
| Compositions, elements and settings models | [checks-compositions.md](references/checks-compositions.md) |

Write the report from [review-report.md](assets/review-report.md).

## Steps

1. **Gather.** Read everything the source offers. With the MCP, read every document type with
   `get-document-type-by-id` (the only way to learn aliases) and every custom data type with
   `get-data-type`. With a requirements folder, run
   [`lint-requirements.mjs`](../umbraco-content-requirements-documentation/scripts/lint-requirements.mjs)
   and keep its output.
2. **Check.** Go through each checks file. Record every finding with the artefact, the rule, the
   evidence and a severity.
3. **Find the project's own convention.** Where the whole model consistently departs from a rule
   in the same way, it is a project convention, not a list of defects. Report it once, under
   departures, and do not rank it.
4. **Rank.** High first. Within a severity, the finding that touches most types first.
5. **Write the report** from the asset, including what was not checked. Save it as
   `docs/content-model-review-<yyyy-mm-dd>.md`, beside the requirements folder rather than inside
   it: the linter treats every page in the requirements folder as a requirements doc.
6. **Stop.** Offer the fixes as a next step through the named skills, each as its own requirements
   doc and approval. Do not change the site or the requirements doc, however small the fix.

## Severity

| Severity | Meaning | Examples |
|---|---|---|
| High | Breaks behaviour or risks content | A page type with no template and no controller; clashing aliases across compositions; a block editor shared by placements where a block cannot render |
| Medium | Makes the model harder to use or extend | Wrong suffix or acronym casing on a page or composition alias; pages allowed at root; tabs with local sorts; constraints in property validation |
| Low | Consistency and polish | Missing descriptions; default icons; element and settings suffixes; property sorts not in hundreds |

## Version compatibility

Targets **Umbraco 17+**. The MCP read tools were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Report, never fix.** Renaming an alias or moving a property can break templates and lose
  content. Every fix is its own change, specified and approved.
- **Evidence for every finding.** Quote the alias, the sort, the setting. A finding without
  evidence is an opinion.
- **Say what you could not see.** A read-back without data type configurations cannot prove
  constraints; a requirements folder cannot prove the site. The report lists these.
- **Respect a consistent project convention.** Consistency beats purity; report the departure once.
- **Code is out of scope.** A controller or view that queries the model badly belongs to
  umbraco-common-pitfalls; name it as a separate review.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code; its one asset
is a markdown report template. An eval graded the guidance. Nothing ran against a live site.
