---
name: umbraco-add-element-type
description: >
  Add one element type to an Umbraco 17+ site, in one of four roles: a content element editors
  fill in, the `...Item` child of a repeater, a settings model built from settings compositions,
  or a settings composition for one style concern, named and foldered by role. The change is
  written as a requirements doc, approved, applied through the Umbraco Developer MCP or a manual
  backoffice walkthrough, and read back.
  Use this whenever the user asks to "add an element type", "create an element", "add a settings
  composition", "make a settings model", or "create a testimonial element with no settings".
  SKIP: non-Umbraco projects or Umbraco < 17; a whole block, content plus settings, or a repeater
  with its Block List (use umbraco-add-block); registering the element (use
  umbraco-configure-block-editor); page compositions (use umbraco-add-composition); types that are
  not elements (use umbraco-add-document-type); changing an element that exists (use
  umbraco-change-document-type); the element's Razor partial view.
---

# Add Element Type

Adds **one element type**: the schema of content that lives inside something else and never on
its own in the content tree. An element is one of four things, and the role decides its folder,
its alias, its container and what it may compose; see
[element-roles.md](references/element-roles.md). It works requirements-first: write the change
down, stop for the user's approval, apply it, then read it back.

**An element type is not a block.** A block is a content element registered in a block area,
with an optional settings element and, for a repeater, its items. A repeater item, a settings model
and a settings composition are elements but never blocks, and a content element becomes a block
only when it is registered. Deciding which elements a block needs is
[`umbraco-add-block`](../umbraco-add-block/SKILL.md)'s job, and it calls this skill once for each
part. Where an element can be used is a block area, owned by
[`umbraco-configure-block-editor`](../umbraco-configure-block-editor/SKILL.md). See
[the terms](../umbraco-content-model-conventions/SKILL.md#terms).

The rules come from [`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md),
and the requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Existing elements, folders, settings compositions and models, data types, media types, languages | [workflow.md](references/workflow.md#1-inspect) |
| 2. Decide | The role first (content element, repeater item, settings model or settings composition; [element-roles.md](references/element-roles.md)), then name, alias, folder, container, fields, icon, description, compositions, culture | [workflow.md](references/workflow.md#2-decide) |
| 3. Write and lint | The element, any data type it needs, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 4. Stop for approval | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 5. Apply | Create, move into its folder, fix-up; through the MCP or a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 6. Verify and report | Read it back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

MCP when connected, otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Used by another skill

[`umbraco-add-block`](../umbraco-add-block/SKILL.md) calls this skill for each element a block is
made of, and [`umbraco-add-page`](../umbraco-add-page/SKILL.md) calls it for a single element a
page needs that has no settings and no repeater. When called from another skill:

- **Inspect, decide and write only.** Run steps 1 to 3 of [workflow.md](references/workflow.md):
  read what the site has, decide this element in its role, and write its requirements page.
- **Hand back** the page written, its dependency flags and its checklist rows (create, move,
  fix-up), so the caller can merge them
  ([combined-changeset.md](../umbraco-add-page/references/combined-changeset.md)).
- **Do not stop for approval and do not apply.** The caller owns the changeset, the single
  approval gate and the apply; the apply and verify notes in this skill's workflow still apply to
  the element it wrote.

## Version compatibility

Targets **Umbraco 17+**. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Content and style stay apart.** A content element holds what editors write and never a
  colour, spacing or alignment field; those belong in a settings composition, composed into a
  settings model; see
  [compositions.md](../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models).
- **No tabs on a content element**: one `Content` group, sort 0. Settings compositions are the one
  kind of element that carries a tab (`Style` 50 or `Settings` 100); see
  [tabs-groups-sorts.md](../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use).
- **A plain noun alias**, `...Block` only when the noun clashes with a media type, data type or
  reserved word; see [naming.md](../umbraco-content-model-conventions/references/naming.md#suffixes).
- **One concern per settings composition**, and a settings model has no properties of its own.
  A new settings composition is composed into settings models by
  [`umbraco-add-block`](../umbraco-add-block/SKILL.md) or
  [`umbraco-change-document-type`](../umbraco-change-document-type/SKILL.md), not here; name that
  as the follow-up.
- **Short generic field aliases** on an element: `title`, not `accordionTitle`; see
  [property-aliases.md](../umbraco-content-model-conventions/references/property-aliases.md).
- **Create, move, fix-up.** Elements are created at the tree root and moved into their folder;
  sorts and descriptions are set in the fix-up.
- **"Just create the element" is a request for the element, not approval** of a requirements doc
  the user has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site. The eval fixture under `evals/files/` is this skill's own copy of the
`site-context.md` that `umbraco-add-block` uses, because eval file paths resolve relative to the
skill, so the copies can drift.
