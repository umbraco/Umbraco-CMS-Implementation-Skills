# Nested items: repeaters

Use this when a block repeats a set of fields an editor can add, remove and reorder: accordion
panels, carousel slides, pricing rows, team members, FAQ questions.

## The shape

```
accordion              parent element in Elements/      title, items
  items  ->  Accordion Items     Block List data type in Data Types/Block List/
               accordionItem     child element in Elements/   title, content
```

| Part | What it is | Example |
|---|---|---|
| Parent element | The block an editor picks | `accordion` |
| Its repeating property | `items`, a Block List | `items` |
| Child element | One entry in the list, suffixed `...Item` or `...Row`; one element in the role repeater item ([element-roles.md](../../umbraco-add-element-type/references/element-roles.md)) | `accordionItem`, `pricingRow` |
| Block List data type | The plural of the child, in `Block List/` | `Accordion Items` |
| How many | Minimum and maximum in the Block List's amount setting | min 1, max unlimited |

The names follow [naming.md](../../umbraco-content-model-conventions/references/naming.md); the
child sits beside its parent in `Elements/`, where alphabetical order keeps them together
([tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md)).

## Rules

- **Not numbered fields.** `panel1Title`, `panel2Title` limits the editor and cannot be reordered.
- **Not a composition**; see
  [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models).
- **A Block List, not a nested Block Grid.** Items are a list, not a layout.
- **The constraint lives in the data type.** "At least one panel" is the Block List's minimum
  ([naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types)); do not
  rely on the parent property being mandatory. Setting it mandatory as well is fine.
- **The child has no settings model** unless each item is styled on its own, which is rare. The
  parent's settings model styles the whole block.
- **One Block List per repeater**, even when two look alike; see
  [naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types).
- **Short generic aliases on both.** The parent has `title` and `items`, the child `title` and
  `content`, not `accordionTitle` or `itemTitle`
  ([property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md)).

## The Block List requirements page

Write it from
[block-data-type.md](../../umbraco-content-requirements-documentation/assets/block-data-type.md):
property editor `Umbraco.BlockList`, the amount as the configuration sentence, and one row whose
content element is the child and whose settings element is `—`. It is used by the parent
element's `items` property. Configuring a Block List beyond this, such as a block that appears in
several lists or has its own settings, is covered by
[approach B of umbraco-configure-block-editor](../../umbraco-configure-block-editor/references/approach-b-block-list-repeater.md).

## Order

The child element, then the Block List that registers it, then the parent that uses the Block
List. A parent element cannot be created before its Block List exists, because its `items`
property needs the data type's id.

**Related:** [workflow.md](workflow.md), [settings-models.md](settings-models.md).
