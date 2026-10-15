---
name: umbraco-content-requirements-documentation
description: >
  Write an Umbraco 17+ schema change as markdown content requirements documentation, get it
  approved, then apply it through the Umbraco Developer MCP or a manual backoffice walkthrough and
  verify it. Owns the requirements page format, the changeset, the status lifecycle, the six-step
  change workflow, and a linter for the requirements folder. Use this whenever the user asks to
  "write the content requirements", "document this document type before creating it", "write up
  the content model requirements", "apply the approved requirements", "apply this changeset",
  "lint the requirements docs", or "check the site matches the requirements", or when another
  content-modelling skill reaches its write, approve, apply or verify step.
  SKIP: non-Umbraco projects or Umbraco < 17; deciding what a type should be called or contain
  (use umbraco-content-model-conventions); uSync, Umbraco Deploy or package.xml output; editing
  content nodes rather than schema.
---

# Content Requirements Documentation

Schema changes are written down before they are made. A change is a set of markdown requirements
pages, one per artefact, grouped by a changeset. The user approves the changeset; only then is it
applied, and only after reading the result back is it marked applied.

**Write the requirements doc, stop for approval, apply, verify.** Never create or change schema on a
site from a requirements doc whose status is still `proposed`.

The rules for *what* to put in a requirements doc (names, folders, tabs, sorts) live in the sibling
skill [`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md). It must
be installed alongside this one. If that link cannot be read, stop and say so; do not work from
memory.

## Steps

The six steps every schema change follows, with what is shared between skills written once, are
in [change-workflow.md](references/change-workflow.md). Each step's detail:

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Read the live schema so the requirements doc fits what exists | [inspect-existing-schema.md](references/inspect-existing-schema.md) |
| 2. Write | One page per artefact from the templates in `assets/`, plus a changeset | [requirements-format.md](references/requirements-format.md) |
| 3. Lint | `node scripts/lint-requirements.mjs <requirements-root>` until it reports no errors | [requirements-format.md](references/requirements-format.md#linting) |
| 4. Approve | Stop. The user reviews the changeset and says to go ahead | [requirements-lifecycle.md](references/requirements-lifecycle.md) |
| 5. Apply | Through the MCP if it is connected, otherwise a manual walkthrough | [apply-via-mcp.md](references/apply-via-mcp.md), [apply-manually.md](references/apply-manually.md) |
| 6. Verify | Read every artefact back, compare with the requirements doc, set the status lines | [verify.md](references/verify.md) |

### How to decide between MCP and manual

This is one approach with two mechanisms, tried in order. Use the Umbraco Developer MCP when its
tools are in the connected tool list. If it is not connected, or the site is not running, generate
the manual walkthrough from the same requirements doc; do not abandon the requirements doc or invent
another route. There is no uSync, Deploy or `package.xml` output.

## Version compatibility

Targets **Umbraco 17+**. The MCP tool names and behaviour in
[apply-via-mcp.md](references/apply-via-mcp.md) were checked against `@umbraco-cms/mcp-dev`
17.6.8 (run against Umbraco 17.5.3) and read from the package source of 18.1.7. Tool names drift
between versions, so the connected tool list is the authority, not this skill.

## Best practices

- One changeset per change a user would describe in one sentence. Its tick-list is what makes an
  interrupted apply resumable.
- Reference what already exists instead of re-specifying it. A requirements page is written for
  something this change creates or alters.
- Every create through the MCP is followed by a fix-up pass. The create tools cannot set sort
  orders, mandatory, property descriptions, templates or culture variation.
- A status line is a claim about the site. `applied` is written only after the artefact has been
  read back, or the user confirms they did the manual steps.
- Run the linter rather than checking conventions by eye. It is deterministic and it is the same
  check CI runs on this skill's golden requirements.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. The assets are markdown templates, not
code, so there is no `dotnet test` example; the requirements doc format is enforced by
[`scripts/lint-requirements.mjs`](scripts/lint-requirements.mjs), which CI runs against
`evals/files/golden-requirements/` (must pass) and `evals/files/broken-requirements/` (must fail).
