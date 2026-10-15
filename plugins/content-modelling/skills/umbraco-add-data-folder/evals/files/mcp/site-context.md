# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

Document types: `get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`create-document-type`, `update-document-type`, `create-document-type-folder`,
`get-document-type-folder`, `get-icons`. Data types: `get-all-data-types`, `find-data-type`,
`get-data-type`, `create-data-type`, `create-data-type-folder`.

## Document Types tree (aliases from get-document-type-by-id)

```
Document Types/
    Compositions/
        SEO Composition              seoComposition
    Elements/
        Rich Text                    richText
    Article Page                     articlePage
    Contact Page                     contactPage
    Content Page                     contentPage
    Home Page                        homePage            (allowed at root)
```

There is no `Data/` folder. Only Home Page is allowed at root.

## Types (get-document-type-by-id)

| Type | Compositions | Allowed children | Own properties: alias (data type, tab, sort) |
|---|---|---|---|
| Home Page | seoComposition | contentPage, contactPage | — |
| Article Page | seoComposition | — | `authorName` (Textstring, Content 100, 100), `bodyText` (Richtext editor, Content 100, 200) |
| Contact Page | seoComposition | — | `introText` (Textarea, Content 100, 100) |
| Content Page | seoComposition | contentPage | `bodyText` (Richtext editor, Content 100, 100) |

There are 300 published Article Page nodes, each with an `authorName` typed by hand (about 25
distinct names, with spelling variations).

## Data types (find-data-type)

Built-in only: Textstring, Textarea, Richtext editor, True/false, Image Media Picker, Multi URL
Picker, Content Picker, Tags. No custom data types and no data type folders.

## get-language

One language: `en-GB` (default).
