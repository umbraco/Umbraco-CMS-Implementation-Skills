---
name: umbraco-configure-block-editor
description: >
  Configure a block editor data type in Umbraco 17+ and the property that uses it: a Block Grid
  for each placement where editors lay out content (approach A), or a Block List for each
  repeater of one kind of item (approach B). Covers allowed blocks, settings models,
  column spans, limits, and adding a block to an existing placement. The change is written as a
  requirements doc, approved, applied through the Umbraco Developer MCP or a manual backoffice
  walkthrough, and read back.
  Use this whenever the user asks to "add a block grid", "set up the main content area", "add this
  block to the page", "make the new block available in the grid", "configure the block list",
  "which blocks can editors use here", or "set column spans".
  SKIP: non-Umbraco projects or Umbraco < 17; creating blocks or elements (use umbraco-add-block
  or umbraco-add-element-type); other data types (use umbraco-add-data-type); a listing's
  collection view (use umbraco-add-listing-page); Block Grid stylesheets and Razor partials.
---

# Configure Block Editor

Configures **one block editor data type**: which blocks it offers, with which settings, where and
how many, and the property that uses it. It works requirements-first: write the change down, stop
for the user's approval, apply it, then read it back.

The blocks themselves (content elements, with a settings model when they have style options, and
child items) are made by
[`umbraco-add-block`](../umbraco-add-block/SKILL.md), and a single element by
[`umbraco-add-element-type`](../umbraco-add-element-type/SKILL.md). This skill registers them in a
block area; see [the terms](../umbraco-content-model-conventions/SKILL.md#terms).

The rules come from [`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md),
and the requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Approaches

| | A: Block Grid per placement | B: Block List per repeater |
|---|---|---|
| Reference | [approach-a-block-grid-placement.md](references/approach-a-block-grid-placement.md) | [approach-b-block-list-repeater.md](references/approach-b-block-list-repeater.md) |
| What it is | An area of a page where editors combine different blocks | An ordered list of one kind of item inside a block or a page |
| Examples | `mainContent` on content pages, a sidebar | Accordion panels, carousel slides, key facts |
| Editor | `Umbraco.BlockGrid` | `Umbraco.BlockList` |
| Name | `<Placement> Block Grid` | The plural of the child: `Accordion Items` |
| Blocks allowed | Many, grouped, each with its settings model | Usually one |
| Layout | Columns, spans, optional layout blocks with areas | A single column |
| Limits | Optional minimum and maximum | Minimum and maximum, usually set |
| Coverage tier | **Documented** | **Documented** |

### How to decide

- **Editors choose what goes where, from several kinds of block:** A. This is the default for any
  page area, even one whose blocks always stack full width today. A Block List placement cannot
  later become a Block Grid without migrating the content, while a grid can start with full-width
  blocks only.
- **Editors add more of the same thing:** B.
- **Both:** a placement (A) offers a parent block whose items are a repeater (B), as the accordion
  does. Configure each data type with its own approach.
- **The project already uses Block Lists for page areas:** follow the project and say which
  convention it departs from.

### How to decide between MCP and manual

Not a third approach: both approaches use the Umbraco Developer MCP when it is connected,
otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Used by another skill

Two skills call this one, both for approach A:

- [`umbraco-add-page`](../umbraco-add-page/SKILL.md), for the block area a new page has: the
  Block Grid for that placement, created if the site has none, or an Update that keeps every
  existing entry if it has one, and the `mainContent` (or sidebar) property on the page.
- [`umbraco-add-block`](../umbraco-add-block/SKILL.md), to place one new block in an existing
  block area the user names: an Update of that Block Grid adding one entry with the block's
  settings model (or `—` when it has none), keeping every existing entry.

When called from another skill:

- **Inspect, decide and write only.** Run steps 1 to 3 of
  [approach A](references/approach-a-block-grid-placement.md): read the placement's grid and its
  blocks, decide the registration (settings element, root or areas, column spans) and write the
  data type page as Create or Update.
- **Hand back** the page, its dependency flags and its checklist row, which the caller places
  after the elements it registers and before the page that uses it
  ([combined-changeset.md](../umbraco-add-page/references/combined-changeset.md)).
- **Do not stop for approval and do not apply.** The caller owns the changeset, the single
  approval gate and the apply; the apply and verify notes in approach A still apply to the grid
  it wrote. Removing a block or tightening a limit is never part of a call from another skill.

## Version compatibility

Targets **Umbraco 17+**. The editor and editor UI aliases (`Umbraco.BlockGrid`,
`Umb.PropertyEditorUi.BlockGrid`, `Umbraco.BlockList`, `Umb.PropertyEditorUi.BlockList`) were read
from the Umbraco 17.5.3 packages. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Two places that allow different blocks get two data types**, so changing one never changes
  the other; see [naming.md](../umbraco-content-model-conventions/references/naming.md#data-types).
- **Register blocks with their settings models, when they have one.** A block's styling is chosen
  where it is registered; register the content element and its settings element together.
  Settings are optional: a block with no style options is registered with settings `—`.
- **Changing a block editor changes existing content's options.** Adding a block is safe.
  Removing one, or tightening a limit, affects pages that already use it: read what uses the data
  type first, and never remove a block that content still uses.
- **Limits live in the data type**, not in the property's validation; see
  [naming.md](../umbraco-content-model-conventions/references/naming.md#data-types).
- "Just add it to the grid" is a request for the change, not approval of a requirements doc the
  user has not yet seen.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented** for both approaches. This skill ships no
code and no assets. The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran
against a live site.
