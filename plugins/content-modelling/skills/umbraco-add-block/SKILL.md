---
name: umbraco-add-block
description: >
  Add one block to an Umbraco 17+ site: the content element editors fill in, an optional settings
  element that controls how it looks, built from settings compositions, and, for a repeater, its
  `...Item` child element and the Block List that holds the children. Can also place the block in
  an existing block area the user names. The change is written as a requirements doc, approved,
  applied through the Umbraco Developer MCP or a manual backoffice walkthrough, and read back.
  Use this whenever the user asks to "add a block", "add an accordion block", "make a carousel
  with slides", "give the block a background colour option", or "add a testimonial block to the
  main content grid".
  SKIP: non-Umbraco projects or Umbraco < 17; one element or one settings composition on its own
  (use umbraco-add-element-type); a new block area (use umbraco-configure-block-editor); page
  compositions (use umbraco-add-composition); the page that uses the block (use
  umbraco-add-page); the block's Razor partial view.
---

# Add Block

Adds **one block**: a set of elements. The content element holds what editors write. A settings
element, which holds how it looks, assembled from small single-purpose settings compositions, is
**optional**: only a block with style options has one. A block that repeats something also has a
child element and the Block List that holds the children. This skill decides which of those parts
the block needs and how they fit together, then writes them up, stops for approval, applies them
and reads them back.

Each element is one element type, and the rules for one element belong to
[`umbraco-add-element-type`](../umbraco-add-element-type/SKILL.md); this skill calls it for each
part and owns the pairing. Where the block can be used is a block area, owned by
[`umbraco-configure-block-editor`](../umbraco-configure-block-editor/SKILL.md): when the user names
an existing area, the block is registered there in the same change; a new area is that skill's
own change. See [the terms](../umbraco-content-model-conventions/SKILL.md#terms).

The rules come from [`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md),
and the requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## The parts of a block

| Part | Needed when | Lives in | Decided by |
|---|---|---|---|
| Content element (`accordion`) | Always | `Elements/` | [umbraco-add-element-type](../umbraco-add-element-type/SKILL.md#used-by-another-skill), role content element |
| Settings model (`accordionSettings`, or a shared one) | Optional: only when the block has style options | `Elements/Settings/` | [settings-models.md](references/settings-models.md); the element by `umbraco-add-element-type`, role settings model |
| Settings composition (`backgroundColourSettingsComposition`) | A style option no existing settings composition gives | `Elements/Compositions/` | [settings-models.md](references/settings-models.md); the element by `umbraco-add-element-type`, role settings composition |
| Child element and its Block List (`accordionItem`, `Accordion Items`) | The block repeats a set of fields | `Elements/`, `Data Types/Block List/` | [nested-items.md](references/nested-items.md); the element by `umbraco-add-element-type`, role repeater item |
| Registration in an existing block area | The user names the area | `Data Types/Block Grid/` | [workflow.md](references/workflow.md#registering-in-a-named-block-area), following [umbraco-configure-block-editor](../umbraco-configure-block-editor/SKILL.md#used-by-another-skill) approach A |

The steps are the same for every block: inspect, decide, write the requirements doc, **stop for
approval**, apply, verify. What a block adds at each step is in
[workflow.md](references/workflow.md).

### How to decide between MCP and manual

Use the Umbraco Developer MCP when it is connected, otherwise a manual walkthrough from the same
requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Used by another skill

[`umbraco-add-page`](../umbraco-add-page/SKILL.md) calls this skill when a page needs a block that
does not exist yet. When called from another skill:

- **Inspect, decide and write only.** Run steps 1 to 3 of [workflow.md](references/workflow.md):
  read what the site has, decide the block's parts, and write their requirements pages.
- **Hand back** the pages written, the dependency flags they carry, and the checklist rows in
  this skill's apply order, so the caller can merge them
  ([combined-changeset.md](../umbraco-add-page/references/combined-changeset.md)).
- **Do not stop for approval and do not apply.** The caller owns the changeset, the single
  approval gate and the apply; the apply and verify notes in this skill's workflow still apply to
  the artefacts it wrote. Registration in the caller's block area is the caller's step, not this
  skill's.

## Version compatibility

Targets **Umbraco 17+**. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **An element is not a block.** A block is a content element registered in a block area. Its
  settings model, settings compositions and repeater items are elements too, but they are parts of
  the block, never blocks of their own.
- **Settings are optional.** A block with no style options gets no settings element and is
  registered with settings `—`. Do not add a settings model "in case"; one can be added later.
- **Content and style stay apart.** Colours, spacing and anchors never go on the element an
  editor fills in; they go on its settings element, built from settings compositions; see
  [compositions.md](../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models).
- **Reuse before you create.** Most blocks are served by one of the site's two shared settings
  models; a bespoke `<block>Settings` is for style options the shared ones lack. Which one a block
  takes is decided in [settings-models.md](references/settings-models.md#which-settings-model-a-block-uses).
- **No tabs on an element**; see
  [tabs-groups-sorts.md](../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use).
- **A repeater is a parent with a Block List of `...Item` children**, not numbered fields and not a
  composition; see [nested-items.md](references/nested-items.md).
- **Leaf first.** Settings compositions, then the settings model, then child elements, then the
  Block List, then the parent, then the block area that registers it: each needs the ids of the
  ones before.
- **Placing the block is an Update of an existing data type.** It registers the content element
  with its settings model, when there is one, and keeps every block the area already offers; see
  [Registering in a named block area](references/workflow.md#registering-in-a-named-block-area).
- "Just build the block" is a request for the block, not approval of a requirements doc the user
  has not yet seen.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site. The eval fixtures under `evals/files/` are this skill's own copies; `umbraco-add-element-type`
and `umbraco-add-page` keep theirs, because eval file paths resolve relative to the skill, so the
copies can drift.
