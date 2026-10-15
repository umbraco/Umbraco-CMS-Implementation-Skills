# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

Document types: `get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`create-document-type`, `update-document-type`, `get-document-type-allowed-children`, `get-icons`.
Data types: `get-all-data-types`, `find-data-type`, `get-data-type`, `create-data-type`,
`create-data-type-folder`, `get-data-type-folder`. Templates: `get-template-root`,
`get-template-children`, `get-template`, `create-template`.

## Document Types tree (with aliases from get-document-type-by-id)

```
Document Types/
    Compositions/
        Page Details Composition      pageDetailsComposition   (pageTitle, pageSummary)
        SEO Composition               seoComposition           (metaTitle, metaDescription, isIndexable)
    Data/
        Author                        author                   (data item)
        Author Folder                 authorFolder             (allowed at root, allows author)
    Article Listing Page              articleListingPage       (collection: Article Listing Page Collection View)
    Article Page                      articlePage
    Content Page                      contentPage
    Home Page                         homePage                 (allowed at root)
    News Page                         newsPage
```

## Structure (get-document-type-by-id)

| Type | Compositions | Allowed children | Default template |
|---|---|---|---|
| Home Page | pageDetailsComposition, seoComposition | contentPage, articleListingPage, newsPage | homePage |
| Content Page | pageDetailsComposition, seoComposition | contentPage | contentPage |
| Article Listing Page | pageDetailsComposition, seoComposition | articlePage, articleListingPage | articleListingPage |
| Article Page | pageDetailsComposition, seoComposition | — | articlePage |
| News Page | pageDetailsComposition, seoComposition | — | newsPage |

News Page own properties: `publishDate` (Date Picker, Content tab 100, sort 100), `bodyText`
(Richtext editor, Content 100, 200). There are 140 published News Page nodes, all created directly
under the Home Page node.

## Data types (find-data-type)

| Name | Folder | Editor | Notes |
|---|---|---|---|
| Textstring, Textarea, Richtext editor, True/false, Image Media Picker, Date Picker, Content Picker | root (built-in) | | |
| Article Listing Page Collection View | `Collection View/` | `Umbraco.ListView` | pageSize 10, orderBy `updateDate` desc, columns `updateDate`, `creator`, tabName `Articles` |
| Author Picker | `Content Picker/` | `Umbraco.MultiNodeTreePicker` | start node Author Folder, allowed type `author`, max 1 |

## Templates (get-template-root, get-template-children)

`master` with children `homePage`, `contentPage`, `articleListingPage`, `articlePage`, `newsPage`.

## get-language

One language: `en-GB` (default).
