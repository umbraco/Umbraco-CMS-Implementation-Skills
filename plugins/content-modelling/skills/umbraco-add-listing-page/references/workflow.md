# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a listing pair needs at each step. Each of the two pages is decided the
way [`umbraco-add-page`](../../umbraco-add-page/SKILL.md) decides one page, called once per page;
read its [workflow](../../umbraco-add-page/references/workflow.md) for the page-level points
(template stub, parent update, SEO is mandatory) rather than repeating them here. The schema rows
of each page come from [`umbraco-add-document-type`](../../umbraco-add-document-type/references/workflow.md#2-decide)
with the role page.

## 1. Inspect

For a listing pair, establish:

| Question | Why it matters here |
|---|---|
| Does a listing or item for this noun already exist? | Extend it instead of making a second |
| Is there a home page, and which content pages exist? | The listing is allowed under the home page (and under content pages) |
| Which page compositions exist? | Both pages take the usual ones; do not repeat their fields |
| Is there a master template? | Both templates go under it |
| Which data types exist for the item's fields, and is there a `Collection View/` folder? | Reuse before creating |
| Where do the items' shared data come from (authors, categories)? | Those are data folders, made separately |

## 2. Decide

| Decision | For a listing pair | Rule |
|---|---|---|
| Names and aliases | `<Noun> Listing Page` / `<noun>ListingPage` lists `<Noun> Page` / `<noun>Page`; singular noun in both | [naming.md](../../umbraco-content-model-conventions/references/naming.md#suffixes) |
| Schema of each page: folder, icon, description, tabs, culture | Decided by `add-document-type` with the role page: both at the Document Types root; listing `icon-list`, item a noun that fits, both the page colour; listing described by what it lists, item by what it is | [add-document-type workflow](../../umbraco-add-document-type/references/workflow.md#2-decide) |
| Compositions | The SEO composition on both, always; the site's other page compositions by kind | [SEO is mandatory](../../umbraco-add-page/references/workflow.md#seo-is-mandatory), [compositions.md](../../umbraco-content-model-conventions/references/compositions.md) |
| Listing's own fields | Usually none; an intro or featured items at most, on the `Content` tab | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md) |
| Item's own fields | What each item holds: a date to order by, the body; `Content` tab | [property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md) |
| Templates | One each, named after the alias, under the master | [templates.md](../../umbraco-content-model-conventions/references/templates.md) |
| Allowed children | Listing: the item **and itself**. Item: nothing | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-children) |
| Parent | The home page (and content pages, if the site nests sections); never allowed at root | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root) |
| Collection view | Its own data type, unless a listing of the same item type already has one | [collection-view.md](collection-view.md) |

The listing allows itself so a large section can be divided into sub-listings of the same kind
(news by year, events by region) without a new type.

## 3. Write the requirements doc

A listing pair is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| Any new data type for an item field | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The SEO composition, only if the site has none | [composition.md](../../umbraco-content-requirements-documentation/assets/composition.md) | `Document-Types/Compositions/SeoComposition.md` | Create |
| The collection view | [collection-view-data-type.md](../../umbraco-content-requirements-documentation/assets/collection-view-data-type.md) | `Data-Types/Collection-View/<Name-Slug>.md` | Create |
| The master template, only if the site has none | [template.md](../../umbraco-content-requirements-documentation/assets/template.md) | `Templates/Master.md` | Create |
| The item's template | [template.md](../../umbraco-content-requirements-documentation/assets/template.md) | `Templates/<MasterPascalCase>/<ItemAliasPascalCase>.md` | Create |
| The listing's template | [template.md](../../umbraco-content-requirements-documentation/assets/template.md) | `Templates/<MasterPascalCase>/<ListingAliasPascalCase>.md` | Create |
| The item page type | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/<ItemAliasPascalCase>.md` | Create |
| The listing page type, with its `Collection` row | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/<ListingAliasPascalCase>.md` | Create |
| The parent, with the listing added to Allowed children | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/HomePage.md` | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to a listing pair:

- Keep the `Collection` row on the listing page and delete it on the item page. The linter requires
  it on any alias ending `ListingPage`.
- The item's `Used by` names the listing; the listing's `Used by` names the parent and itself.
- **The parent** is an existing type the change edits. Its page is written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes.

The changeset's checklist, in apply order:

```
- [ ] 1. Create any new data type for an item field                              (only if needed)
- [ ] 2. Create composition `seoComposition`, then fix-up                             (only if new)
- [ ] 3. Create data type folder `Collection View` and data type `<Listing name> Collection View`
- [ ] 4. Create template `master`                                               (only if new)
- [ ] 5. Create template `<itemAlias>` under `master`
- [ ] 6. Create template `<listingAlias>` under `master`
- [ ] 7. Create document type `<itemAlias>`, then fix-up (sorts, mandatory, descriptions, template)
- [ ] 8. Create document type `<listingAlias>`, then fix-up (template, collection view)
- [ ] 9. Set allowed children on `<listingAlias>`: `<itemAlias>`, `<listingAlias>`
- [ ] 10. Set allowed children on `homePage`: add `<listingAlias>`, keeping the existing entries
- [ ] 11. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

For a listing pair, call out the **existing** parent that will change, and any shared data
(authors, categories) left for a separate change. Then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a listing pair, also:

- **The collection view.** Create it with `create-data-type`; its `values` are `pageSize`,
  `orderBy`, `orderDirection`, `includeProperties`, `layouts`, `icon` and `tabName`, in the shape
  read from an existing collection view as
  [apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#tools)
  describes.
- **The item first, then the listing.** The listing's allowed children need the item's id.
- **The listing's fix-up** sets `collection` to `{ "id": <collection view id> }` along with the
  template, and, in the allowed-children step, `allowedDocumentTypes` to the item and itself.
- **The parent** is updated last by read-modify-write, keeping every existing allowed child; see the
  page [workflow](../../umbraco-add-page/references/workflow.md#5-apply).

## 6. Verify and report

For a listing pair, also confirm that the listing's `collection` is the new collection view, that
each type's allowed children are exactly as specified, and that the parent lists the listing.

In the report, what is left (item 3) names: the listing template's rendering of its children
(paging, ordering on the website), the item template's markup, and any data folders for shared
item data (with [umbraco-add-data-folder](../../umbraco-add-data-folder/SKILL.md)).
