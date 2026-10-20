# MCP read results: existing site with no SEO composition

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

`get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`get-document-type-tree-search`, `get-document-type-ancestors`,
`get-document-type-composition-references`, `create-document-type`, `update-document-type`,
`move-document-type`, `create-document-type-folder`, `get-document-type-folder`, `get-icons`,
`get-all-data-types`, `find-data-type`, `get-data-type`, `create-data-type`,
`create-data-type-folder`, `get-data-type-folder`, `get-template`, `get-template-root`,
`get-template-children`, `create-template`, `get-language`.

## Document Types tree (aliases from get-document-type-by-id)

```
Document Types/
    Compositions/                folder, id 5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10
        Page Details Composition     pageDetailsComposition
    Content Page                     contentPage
    Home Page                        homePage            (allowed at root)
```

There is no SEO composition, no Open Graph composition, no `Data/` folder and no `Elements/`
folder.

## Types (get-document-type-by-id)

| Type | Compositions | Allowed children | Own properties: alias (data type, tab and sort, sort) |
|---|---|---|---|
| Page Details Composition | — | — | `pageTitle` (Textstring, Page Details 200, 100), `pageSummary` (Textarea, Page Details 200, 200) |
| Home Page | pageDetailsComposition | contentPage | — |
| Content Page | pageDetailsComposition | contentPage | `bodyText` (Richtext editor, Content 100, 100) |

Home Page: `icon-home color-light-blue`, `allowedAsRoot: true`, default template `homePage`.
Content Page: `icon-document color-light-blue`, `allowedAsRoot: false`, default template
`contentPage`. Both `variesByCulture: false`.

`get-document-type-composition-references` on Page Details Composition: Home Page, Content Page.

## Templates (get-template-root, get-template-children)

| Template | Alias | Children |
|---|---|---|
| Master | `master` | Content Page (`contentPage`), Home Page (`homePage`) |

## Data types (find-data-type)

Built-in only, no custom data types and no data type folders:

| Name | Editor | Id |
|---|---|---|
| Textstring | `Umbraco.TextBox` | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` |
| Textarea | `Umbraco.TextArea` | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` |
| Richtext editor | `Umbraco.RichText` | `ca90c950-0aff-4e72-b976-a30b1ac57dad` |
| True/false | `Umbraco.TrueFalse` | `92897bc6-a5f3-4ffe-ae27-f2e7e33dda49` |
| Image Media Picker | `Umbraco.MediaPicker3` | `ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3` |
| Multi URL Picker | `Umbraco.MultiUrlPicker` | `b4e3535a-1753-47e2-aab8-a5b8a4d6b3dc` |

The True/false data type has its default configuration (off by default); there is no toggle that
defaults on.

## get-language

```json
{ "items": [ { "isoCode": "en-GB", "name": "English (United Kingdom)", "isDefault": true } ], "total": 1 }
```
