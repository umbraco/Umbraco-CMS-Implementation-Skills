# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected document-type tools (abbreviated)

`get-all-document-types`, `get-document-type-by-id`, `get-document-type-batch`,
`get-document-types-by-id-array`, `get-document-type-tree-search`, `get-document-type-ancestors`,
`get-document-type-composition-references`, `get-document-type-available-compositions`,
`create-document-type`, `create-element-type`, `update-document-type`, `move-document-type`,
`create-document-type-folder`, `get-document-type-folder`, `get-icons`, `get-all-data-types`,
`find-data-type`, `get-data-type`, `create-data-type`, `create-data-type-folder`, `get-template`,
`get-template-root`, `get-language`.

## Document Types tree (get-all-document-types, with aliases and `allowedAsRoot` from get-document-type-batch)

```
Document Types/
    Compositions/            folder, id 5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10
        SEO Composition              seoComposition
    Data/                    folder
        Author                       author            (data item)
        Author Folder                authorFolder      (allowed at root)
    Elements/                folder
        Hero                         hero              (element type)
    Article Listing Page             articleListingPage
    Article Page                     articlePage
    Content Page                     contentPage
    Event Page                       eventPage
    Home Page                        homePage          (allowed at root)
    Search Page                      searchPage
    Site Settings                    siteSettings      (allowed at root)
```

## Types and their compositions and own properties (get-document-type-by-id on each)

| Type | Compositions | Own properties: alias (tab, sort) |
|---|---|---|
| SEO Composition | — | `metaTitle` (SEO 600, no group, 100), `metaDescription` (SEO 600, no group, 200), `isIndexable` (SEO 600, no group, 300) |
| Home Page | seoComposition | — |
| Content Page | seoComposition | `bodyText` (Content 100, 100) |
| Article Listing Page | seoComposition | — |
| Article Page | seoComposition | `bodyText` (Content 100, 100), `shareImage` (Content 100, 200) |
| Event Page | seoComposition | `eventDate` (Content 100, 100), `bodyText` (Content 100, 200) |
| Search Page | seoComposition | — |
| Author Folder | — | — |
| Author | — | `jobTitle` (Content 100, 100), `photo` (Content 100, 200) |
| Site Settings | — | `siteName` (General 0, 100) |
| Hero | — (element type) | `title` (group Content, 100), `image` (group Content, 200) |

`get-document-type-composition-references` on SEO Composition: Home Page, Content Page, Article
Listing Page, Article Page, Event Page, Search Page.

Allowed children: Home Page allows Content Page, Article Listing Page, Search Page. Article
Listing Page allows Article Page. Content Page allows Content Page and Event Page.

## Data types (find-data-type)

| Name | Editor |
|---|---|
| Textstring | `Umbraco.TextBox` |
| Textarea | `Umbraco.TextArea` |
| Richtext editor | `Umbraco.RichText` |
| True/false | `Umbraco.TrueFalse` |
| Image Media Picker | `Umbraco.MediaPicker3` |
| Multi URL Picker | `Umbraco.MultiUrlPicker` |
| Date Picker | `Umbraco.DateTime` |

## get-language

One language: `en-GB` (default).
