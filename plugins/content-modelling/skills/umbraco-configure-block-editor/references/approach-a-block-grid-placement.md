# Approach A: a Block Grid per placement

Follow [the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md),
starting with
[the requirements folder](../../umbraco-content-requirements-documentation/references/change-workflow.md#before-step-1-the-requirements-folder).
This file adds only what a Block Grid placement needs at each step.

## When to choose this approach

Choose this when editors build an **area of a page** from several kinds of block: the main body of
a content page, a sidebar, a landing page's sections. If editors only add more of one kind of item,
use [approach B, a Block List per repeater](approach-b-block-list-repeater.md) instead.

A **placement** is one property on one or more types (usually through a composition) where blocks
are laid out. Each placement has its own Block Grid data type.

## Building blocks

| Part | Rule | Example |
|---|---|---|
| The data type | `Umbraco.BlockGrid`, editor UI `Umb.PropertyEditorUi.BlockGrid`, named `<Placement> Block Grid`, in `Block Grid/` | `Main Content Block Grid` |
| The property | A page-level alias naming the area, on the tab the global table gives that area: `Content` (100) for the main area, `Sidebar` (500) for a sidebar | `mainContent` on Content, `sidebarContent` on Sidebar |
| Grid columns | 12, unless the project's front end uses another grid | 12 |
| Each registered block | Content element, its settings element, allowed at root, allowed in areas, column spans | `accordion` with `titledBlockSettings`, spans 12 or 6 |
| Block groups | Only when the catalogue is long enough to need them: Content, Media, Layout | `Content` |
| Layout blocks | An element whose block has **areas** (for example two columns), holding other blocks | `twoColumnLayout` with areas `left` and `right` |
| Limits | Optional minimum and maximum number of blocks | minimum 1 on a page that must have content |

Names and folders come from
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types) and
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types);
the property's tab and sort from
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md).

### Rules

- **One Block Grid per placement.** A sidebar that allows fewer blocks than the main area is its
  own data type, even though the lists overlap; see
  [naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types).
- **Register each block with its settings model** (the site's shared ones, or the block's bespoke
  one). A block registered without its settings element loses its style options in that placement.
  A block with no settings model is registered with settings `—`.
- **Column spans say where a block may sit.** A full-width block offers 12 only; a card-like block
  offers 12, 6 and 4. Leave spans empty only for blocks that take any width.
- **Layout blocks are opt-in.** Add areas only when editors need to put blocks side by side. A
  block allowed only inside areas is "allowed in areas" and not "allowed at root".
- **Adding a placement property to types is an Update of those types.** If the property belongs
  on every page, it goes on a composition, not on each type
  ([`umbraco-add-composition`](../../umbraco-add-composition/SKILL.md)).

## Adding a block to an existing placement

The most common request. Read the data type with `get-data-type`, add one entry to its blocks with
the content element, the settings element, root/areas and spans, keep every existing entry
unchanged, and write it back. Pages that already use the placement are unaffected; editors simply
see a new block in the catalogue. The requirements page for the data type becomes an `Update`.

**Removing** a block, or tightening a limit, affects content already on pages. Check
`get-references-data-type` for the properties using the grid and ask the user whether any page
uses the block before planning its removal. Never remove a block that content uses.

## 1. Inspect

Read the placement's existing Block Grid and its blocks (`get-data-type`), the elements and
settings models to register (`get-document-type-by-id`), the types that have or will have the
property, and the `Block Grid/` folder. A block that does not exist yet is made first with
[`umbraco-add-block`](../../umbraco-add-block/SKILL.md), or a single element with
[`umbraco-add-element-type`](../../umbraco-add-element-type/SKILL.md).

## 2. Decide

Work through the Building blocks table above, in order.

## 3. Write the requirements doc

The data type page comes from
[block-data-type.md](../../umbraco-content-requirements-documentation/assets/block-data-type.md), at
`Data-Types/Block-Grid/<Name-Slug>.md`, with "Grid columns: 12." as the configuration sentence and
one table row per block. Existing elements with no requirements page are plain text flagged
`Exists`. Each type or composition that gains the property, and a Block Grid that gains a block,
is an **Update** page, written as
[requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
describes.

## 4. Stop for approval

Call out any existing content affected (a removed block, a tightened limit) and every existing
type that gains the property, then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps without the MCP, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a Block Grid, also:

- **Order.** Elements and settings models first, then the Block Grid, then the property on its
  type or composition.
- **The data type's `values`.** Build them by reading an existing Block Grid on the site with
  `get-data-type` and copying its shape: the `blocks` entries with `contentElementTypeKey`,
  `settingsElementTypeKey`, `allowAtRoot`, `allowInAreas` and `columnSpanOptions`; `blockGroups`;
  `gridColumns`; `validationLimit`. Look up every element id at apply time.
- **An update.** Send the whole configuration back with `update-data-type`, every existing block
  entry included.

## 6. Verify and report

For a Block Grid, also read the data type back and compare every block row, settings element,
span and limit; read each type that gained the property and confirm its property count grew by
one.

In the report, what is left is: the Razor partial for each new block, the grid layout stylesheet
if the site uses one, and anything in the Apply log.

**Alternative:** [approach B, a Block List per repeater](approach-b-block-list-repeater.md).
