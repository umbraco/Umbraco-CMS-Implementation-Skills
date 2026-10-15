---
name: umbraco-add-data-type
description: >
  Decide whether a property in Umbraco 17+ needs a new data type or can reuse one, pick the
  property editor, and name and file it in the Data Types tree. The change is written as a
  requirements doc, approved, applied through the Umbraco Developer MCP or a manual backoffice
  walkthrough, and read back.
  Use this whenever the user asks to "add a data type", "create a dropdown for", "limit this field
  to 160 characters", "make a toggle that defaults to on", "which property editor should I use",
  "can I reuse this data type", "tidy up our data types", or "change this data type's settings".
  SKIP: non-Umbraco projects or Umbraco < 17; adding or changing a property on an existing type
  (use umbraco-change-document-type); Block List and Block Grid data types (use
  umbraco-configure-block-editor); a listing's collection view (use umbraco-add-listing-page);
  shared data items and the picker that chooses them (use umbraco-add-data-folder).
---

# Add Data Type

Decides the **data type behind a property**: reuse an existing one or create a new one, which
property editor, what it is called, which folder it goes in and how it is configured. It works
requirements-first: write the change down, stop for the user's approval, apply it, then read it
back.

Most of the time the answer is "reuse". This skill exists so the site does not collect a dozen
near-identical text areas, and so a constraint such as a character limit lives in one place.

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Data types with the editor, their configuration and references; the editor-kind folders; the property that will use it | [workflow.md](references/workflow.md#1-inspect) |
| 2. Reuse or create | Reuse as is, a generic variant with a qualifier, or a purpose-specific data type | [reuse-or-create.md](references/reuse-or-create.md) |
| 3. Decide the details | Property editor and editor UI, value type, name, folder, configuration | [workflow.md](references/workflow.md#2-decide) |
| 4. Write and lint | The data type, any type whose property moves to it, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Through the MCP if connected, otherwise a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read everything back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

MCP when connected, otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Used by another skill

[`umbraco-add-page`](../umbraco-add-page/SKILL.md) calls this skill for a page field nothing on
the site fits, [`umbraco-add-block`](../umbraco-add-block/SKILL.md) and
[`umbraco-add-element-type`](../umbraco-add-element-type/SKILL.md) for a block field (a colour
palette, a dropdown of sizes), and [`umbraco-add-composition`](../umbraco-add-composition/SKILL.md)
for a composition field such as a toggle that defaults on. When called from another skill:

- **Inspect, decide and write only.** Run steps 1 to 3 of [workflow.md](references/workflow.md):
  read the candidate data types' configurations, decide reuse or create
  ([reuse-or-create.md](references/reuse-or-create.md)) and, for a new one, write its page and
  the editor-kind folder index if the folder is new.
- **Hand back** the decision (reuse, by name, or the new page), its dependency flags and the
  checklist row, which the caller places first in its apply order
  ([combined-changeset.md](../umbraco-add-page/references/combined-changeset.md)).
- **Do not stop for approval and do not apply.** The caller owns the changeset, the single
  approval gate and the apply; the apply and verify notes in this skill's workflow still apply to
  the data type it wrote. Reconfiguring an existing data type is never part of a call from
  another skill.

## Version compatibility

Targets **Umbraco 17+**. The property editor and editor UI aliases in
[editor-selection.md](references/editor-selection.md) were read from the Umbraco 17.5.3
`Umbraco.Cms.Core` and `Umbraco.Cms.StaticAssets` packages; Umbraco 17 added separate date-only,
time-only, unspecified and time-zone-aware date editors. MCP tool names were checked as described
in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Reuse first.** A new data type is justified by a configuration no existing one has, not by a
  new property. Read the candidate's configuration before deciding; its name is not proof of what
  it does.
- **A shared data type is shared.** Changing its configuration changes every property that uses
  it. Read its references before touching it, and create a qualified variant rather than bend a
  shared one for one field.
- **Constraints belong in the data type**, not in property validation; see
  [naming.md](../umbraco-content-model-conventions/references/naming.md#data-types).
- **Changing a property's editor can lose content.** Moving a property to a data type with a
  different value format (text to picker, single to multiple) leaves stored values unreadable.
  Stop and say so; never do it to existing content silently.
- **Built-in data types stay as installed.** Do not rename, move or reconfigure them; a variant
  is a new, qualified data type in its editor's folder.
- **"Just make it 160 characters" is a request for the data type, not approval** of a
  requirements doc the user has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran
against a live site.
