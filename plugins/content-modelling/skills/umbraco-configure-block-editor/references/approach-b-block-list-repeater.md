# Approach B: a Block List per repeater

Follow [the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md),
starting with
[the requirements folder](../../umbraco-content-requirements-documentation/references/change-workflow.md#before-step-1-the-requirements-folder).
This file adds only what a Block List repeater needs at each step.

## When to choose this approach

Choose this when editors add, remove and reorder **more of one kind of item**: the panels of an
accordion, the slides of a carousel, the rows of a pricing table, a page's list of key facts. If
editors combine different kinds of block into a layout, use
[approach A, a Block Grid per placement](approach-a-block-grid-placement.md) instead.

A repeater usually lives inside a parent block (`accordion.items`), and is created with that block
by [`umbraco-add-block`](../../umbraco-add-block/SKILL.md) (see its
[nested items](../../umbraco-add-block/references/nested-items.md)). Use this file when the
repeater is configured on its own: a page-level list, a second list of the same items, a changed
limit, or a repeater that allows a second kind of item.

## Building blocks

| Part | Rule | Example |
|---|---|---|
| The data type | `Umbraco.BlockList`, editor UI `Umb.PropertyEditorUi.BlockList`, named as the plural of its child, in `Block List/` | `Accordion Items` |
| The property | `items` on a parent block; an area-prefixed alias on a page (`keyFacts`) | `items` |
| Registered blocks | Usually one child element, with no settings element | `accordionItem` |
| Amount | Minimum and maximum in the data type (`validationLimit`) | minimum 1, no maximum |
| Single block mode | Only for a property that holds exactly one block | off |
| Inline editing | On for small items edited in place (a title and a line of text) | off by default |

Names and folders come from
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types) and
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types).

### Rules

- **One Block List per repeater.** Two repeaters that allow different items get two data types
  ([naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types)). Two
  properties that hold exactly the same items may share one.
- **The limit is the data type's amount**
  ([naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types)). Making
  the property mandatory as well is fine; relying on it alone is not.
- **Items have no settings element** unless each one is styled on its own, which is rare. The
  parent block's settings model styles the whole list.
- **A second kind of item** (a carousel that takes image slides and video slides) is a second block
  in the same Block List; name the data type after what the items have in common (`Carousel
  Slides`).
- **A list of the same item across the whole site** (for example key facts on several page types)
  is one Block List used by a property on a composition, not a property on each type.

## Changing an existing repeater

Read the data type with `get-data-type` and `get-references-data-type`. Raising a limit or adding
an item type is safe. Lowering a maximum, raising a minimum or removing an item type affects
content that already has more, fewer or those items: say which, and ask before planning it.

## 1. Inspect

Read the existing Block List if any (`get-data-type`), what uses it (`get-references-data-type`),
the child element(s) (`get-document-type-by-id`) and the `Block List/` folder.

## 2. Decide

Work through the Building blocks table above, in order.

## 3. Write the requirements doc

The data type page comes from
[block-data-type.md](../../umbraco-content-requirements-documentation/assets/block-data-type.md), at
`Data-Types/Block-List/<Name-Slug>.md`, with the amount as the configuration sentence ("Amount
(min/max): 1 / 6.") and one table row per item element, settings element `—`. The type or
element that gains the property, and a Block List that changes, is an **Update** page, written as
[requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
describes.

## 4. Stop for approval

Call out any existing content affected (a lowered maximum, a raised minimum, a removed item type)
and the existing type or element that gains the property, then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps without the MCP, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a Block List, also:

- **Order.** The child element first, then the Block List, then the property that uses it.
- **The data type's `values`.** Copy the shape from an existing Block List read with
  `get-data-type`: `blocks` with `contentElementTypeKey`, `validationLimit`,
  `useSingleBlockMode`, `useInlineEditingAsDefault`. Look up every element id at apply time.
- **An update.** Send the whole configuration back with `update-data-type`, every existing block
  entry included.

## 6. Verify and report

For a Block List, also read the data type back and compare the registered items and the amount;
read the type or element that uses it and confirm its property count.

In the report, what is left is: the Razor that renders the items, and anything in the Apply log.

**Alternative:** [approach A, a Block Grid per placement](approach-a-block-grid-placement.md).
