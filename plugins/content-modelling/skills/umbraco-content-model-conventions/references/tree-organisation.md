# Tree organisation

**Applies to:** the Document Types tree and the Data Types tree in Settings.
**Look for:** pages inside folders, compositions or elements loose at the root, data
types all in one flat list, items in creation order.
**Why:** a fresh install gives one flat node. Left alone it becomes pages, elements, compositions
and data items mixed together, and nobody can find anything.

## Document Types

```
Document Types/
    Compositions/           compositions applied to pages (seoComposition)
    Data/                   containers and items that are not pages (authorFolder, author)
    Elements/
        Compositions/       settings compositions used only by settings models
        Settings/           block settings models (accordionSettings)
        accordion, accordionItem, richText ...
    articleListingPage, articlePage, contentPage, homePage, siteSettings
```

| Rule | Reason |
|---|---|
| Pages (types with a template) sit at the tree root, never in a folder | Pages are the main deliverable; they should be findable without drilling down |
| Exactly three top-level folders: `Compositions`, `Data`, `Elements` | Each non-page kind has one obvious home |
| `Elements` holds its own `Compositions` and `Settings` folders | Settings compositions and models belong to blocks, not pages |
| Folders sort above loose types | The tree reads the same way at every level |
| Alphabetical within a level | The order does not depend on when something was created |

Child elements (`accordionItem`) sit beside their parent in `Elements/`; alphabetical order keeps
them together. Add a sub-folder only when the flat list becomes unwieldy, and then apply it to
every feature, not one.

A multi-site install may wrap the whole structure in one folder per site. The rules then apply
inside each site folder.

## Data Types

Custom data types are grouped in **folders by editor kind**, one folder per property editor:

```
Data Types/
    Block Grid/             Main Content Block Grid
    Block List/             Accordion Items
    Collection View/        Article Listing Page Collection View
    Text Area/              Meta Description Text Area
    Toggle/                 Toggle (default on)
```

| Rule | Reason |
|---|---|
| One folder per editor kind, named after the editor | You look for a data type by what it edits |
| Create the folder when the first custom data type of that kind appears | No empty folders |
| Umbraco's built-in data types stay where the installer put them | They are shared defaults, not project artefacts |

Data type names are covered in [naming.md](naming.md#data-types).

**Related:** [Names, aliases and suffixes](naming.md), [Compositions](compositions.md).
