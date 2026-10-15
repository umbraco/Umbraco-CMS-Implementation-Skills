# Taxonomy: categories and other controlled lists

Categories, topics, sectors, regions: lists of values that pages are tagged with and that the
site filters or groups by. There are two ways to hold them. Choose by who controls the list.

| | Content-driven taxonomy (data items) | Free-form tags |
|---|---|---|
| Who controls the list | Whoever may create items in the folder | Every editor, as they type |
| Stored as | Content nodes in a data folder, picked by id | Text values on each page |
| Extra fields per value | Yes: a description, an icon, a landing page, a colour | No |
| Renaming a value | Once, everywhere | On every page that uses it |
| Typos and duplicates | Prevented | Common ("News", "news", "Newss") |
| Made with | This skill | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) (the Tags editor) |

Use **data items** when the site filters, groups or builds pages by the value, when values need
their own fields, or when the list is agreed rather than invented per page. Use **tags** for
loose, editor-invented keywords used only for search or a tag cloud.

## The shape of a content-driven taxonomy

| Part | Rule | Example |
|---|---|---|
| Folder type | `<noun>Folder`, allowed at root, allows the item | `categoryFolder` |
| Item type | The plain singular noun, allows nothing | `category` |
| Item fields | Only what the value needs beyond its name: a description, an image | `description` |
| Picker data type | `Umbraco.MultiNodeTreePicker`, start node the folder, only the item type allowed, the maximum the use needs | `Category Picker`, max 3 |
| The pages' property | An area-prefixed alias on the pages or a composition that needs it | `categories` |

The node **name** is the value editors see. Do not add a `title` field that repeats it unless the
display label genuinely differs.

## Groups of values

Most taxonomies are flat. When values need grouping (regions within countries, topics within
themes), let the folder allow itself so an editor can create sub-folders, and keep the item a
leaf. The conventions make a data item a leaf
([allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md));
if the user needs values that are themselves parents of values (a category with sub-categories
that are also selectable), that departs from the convention. Say so, and let the user choose it
knowingly.

## Several taxonomies

Each list is its own folder and item type (`categoryFolder` with `category`, `regionFolder` with
`region`). Do not make one generic `taxonomyItem` with a "type" field: it loses the picker's
filtering and every consumer has to check the type.

## Where the picker goes

If every article and news item is categorised, the picker property goes on a composition both
types share ([umbraco-add-composition](../../umbraco-add-composition/SKILL.md)). If one type uses
it, it goes on that type, on its `Content` tab.

**Related:** [workflow.md](workflow.md).
