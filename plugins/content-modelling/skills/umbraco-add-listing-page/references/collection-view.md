# The listing's collection view

A listing page with dozens or hundreds of items is unusable as a tree. A **collection view**
shows the listing's children as a sortable, searchable table in the backoffice, and the items no
longer crowd the content tree. Each listing gets its own collection view data type, set on the
listing type's `Collection` row; the one exception is listings of the same item type, which may
share one. The rule is in
[allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#listings).

## The data type

| Setting | Rule | Example |
|---|---|---|
| Name | `<Listing type name> Collection View`, or `<Item type name> Collection View` when shared | `Article Listing Page Collection View` |
| Folder | `Collection View/` | |
| Property editor, editor UI | `Umbraco.ListView`, `Umb.PropertyEditorUi.Collection` | |
| Database type | `Nvarchar` | |
| Page size | How many rows per page in the backoffice | 10 to 50 |
| Order by, direction | A date on the item when it has one, otherwise `updateDate`; newest first | `publishDate` descending |
| Columns (`includeProperties`) | What an editor needs to find an item: a date, a status field, who changed it last | `publishDate`, `updateDate`, `creator` |
| Layouts | Table first; Grid only for visual items (images, products) | Table and Grid |
| Icon | The listing's icon | `icon-list` |
| Tab name | The plural of the item, as editors say it | `Articles` |

Start from the template
[collection-view-data-type.md](../../umbraco-content-requirements-documentation/assets/collection-view-data-type.md).
A requirements page for this data type lives in `Data-Types/Collection-View/`, and the linter
checks that a listing page's `Collection` row names one.

## Columns

Columns are either built-in fields of every node (such as `updateDate`, `createDate`, `creator`,
`owner` and `sortOrder`) or property aliases of the item type. A property column must exist on the
item type, or a composition it has, before the collection view is useful. Keep it to three or four
columns: the name is always shown first.

## Ordering

- **Dated items** (articles, news, events): order by the item's own date property, descending for
  news and articles, ascending for upcoming events.
- **Hand-ordered items** (team members, FAQs as pages): order by `sortOrder` ascending, so editors
  control the order.
- **No natural order:** `updateDate` descending, so the most recently edited item is on top.

The backoffice ordering does not decide the order on the website. The listing's template or
controller orders what it renders; say so when you report.

## Why one per listing

An article listing wants `publishDate` and `author`; an event listing wants `eventDate` and
`venue`. A shared collection view would force one set of columns on both, and a change for one
listing would silently change the other. A second listing of the **same** item type (for example
two regional news listings) may share one, named after the item it lists, because the same
columns fit both.

## What it changes for editors

Once a listing has a collection, its children are managed in the collection rather than expanded
in the content tree. Tell the user this when the listing replaces a section editors already use
through the tree.

**Related:** [workflow.md](workflow.md).
