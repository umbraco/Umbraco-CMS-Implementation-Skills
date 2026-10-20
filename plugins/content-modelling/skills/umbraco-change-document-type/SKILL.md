---
name: umbraco-change-document-type
description: >
  Change a document type, composition or element type that already exists on an Umbraco 17+ site:
  add, remove or reorder a property, make one mandatory, change its data type, rename a type or
  property, change allowed children or compositions, or move a type into a folder. Says which
  changes lose content. The change is written as a requirements doc, approved, applied through
  the Umbraco Developer MCP or a manual backoffice walkthrough, and read back.
  Use this whenever the user asks to "add a field to the article page", "make the summary
  mandatory", "remove the old banner property", "rename this property", "change this field to a
  picker", "let the home page allow event pages", or "move this type into a folder".
  SKIP: non-Umbraco projects or Umbraco < 17; creating a new type (use umbraco-add-page,
  umbraco-add-document-type, umbraco-add-element-type or umbraco-add-block); applying a
  composition (use umbraco-add-composition); reconfiguring a data type (use
  umbraco-add-data-type); editing content nodes.
---

# Change Document Type

Changes **one type that already exists**: a page, composition, element or data item. It works
requirements-first: read the type, write the change down as an Update page, stop for the user's
approval, apply it by reading, modifying and writing back the whole type, then read it back.

Everything an existing type carries is content somebody has entered. The skill's job, beyond the
change itself, is to say **before approval** which part of the request loses stored values and
which does not, and to offer the safe alternative where one exists.

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and
[the six-step workflow](../umbraco-content-requirements-documentation/references/change-workflow.md)
from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md);
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Read the type in full, what composes it, what uses it, and whether it has content | [workflow.md](references/workflow.md#1-inspect) |
| 2. Classify the change | Safe, needs confirmation, or loses content; the alternative for each | [safe-changes.md](references/safe-changes.md) |
| 3. Decide the details | The new property's place and sorts, the new children, the new folder | [workflow.md](references/workflow.md#2-decide) |
| 4. Write and lint | The type's Update page, any new data type, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset, having seen what it loses | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Read, modify, write back whole; through the MCP or a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read it back, compare the property count, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

One approach, two mechanisms: the Umbraco Developer MCP when it is connected, otherwise a manual
walkthrough generated from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Version compatibility

Targets **Umbraco 17+**. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **A full read before any write.** `update-document-type` replaces the whole type, and a body
  that leaves out a property deletes it and its values. Never build the body from the requirements
  doc; start from the read-back and change only what the page says.
- **An alias is never renamed in place.** Templates, Models Builder classes and queries read it,
  and Umbraco keeps the stored values under the old alias. Rename the *name* freely; for an alias,
  propose a new property plus a migration, see
  [safe-changes.md](references/safe-changes.md#renaming).
- **Removing is deleting.** A removed property takes every stored value with it, on every node of
  the type and of every type that composes it. Say how many nodes, name the migration, and get
  the user's explicit yes in the approval turn.
- **Additions keep the type's conventions.** A new field joins the existing tab with the next
  sort in hundreds, follows the type's alias prefixes, and reuses a data type where one fits; the
  rules are the same as for a new type.
- **Mandatory is retroactive.** Existing nodes with the field empty cannot be saved until it is
  filled; say so and suggest a default where the data type supports one.
- **"Just add the field" is a request for the change, not approval** of a requirements doc the
  user has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site.
