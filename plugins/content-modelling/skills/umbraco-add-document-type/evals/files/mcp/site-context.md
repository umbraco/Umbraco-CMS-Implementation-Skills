# MCP read results: existing site

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
        Open Graph Composition       openGraphComposition
        Page Details Composition     pageDetailsComposition
        SEO Composition              seoComposition
    Data/                        folder, id 6e1d9b62-7a1f-4b66-8c3c-2a4d0e9f3b21
        Author                       author
        Author Folder                authorFolder        (allowed at root)
    Elements/                    folder
        Rich Text                    richText
    Contact Page                     contactPage
    Content Page                     contentPage
    Home Page                        homePage            (allowed at root)
```

## Types (get-document-type-by-id)

| Type | Compositions | Allowed children | Own properties: alias (data type, tab and sort, sort) |
|---|---|---|---|
| Page Details Composition | — | — | `pageTitle` (Textstring, Page Details 200, 100), `pageSummary` (Textarea, Page Details 200, 200) |
| SEO Composition | — | — | `metaTitle` (Textstring, SEO 600, group Search 0, 100), `metaDescription` (Textarea, SEO 600, group Search 0, 200), `isIndexable` (True/false, SEO 600, group Search 0, 300) |
| Open Graph Composition | — | — | `shareTitle` (Textstring, SEO 600, group Sharing 100, 100), `shareImage` (Image Media Picker, SEO 600, group Sharing 100, 200) |
| Home Page | pageDetailsComposition, seoComposition, openGraphComposition | contentPage, contactPage | — |
| Content Page | pageDetailsComposition, seoComposition, openGraphComposition | contentPage | `bodyText` (Richtext editor, Content 100, 100) |
| Contact Page | pageDetailsComposition, seoComposition, openGraphComposition | — | `address` (Textarea, Content 100, 100), `phone` (Textstring, Content 100, 200), `email` (Email Address, Content 100, 300) |
| Author Folder | — | author | — |
| Author | — | — | `bio` (Richtext editor, Content 100, 100), `photo` (Image Media Picker, Content 100, 200) |
| Rich Text | — (element type) | — | `content` (Richtext editor, group Content 0, 100) |

Home Page: `icon-home color-light-blue`, `allowedAsRoot: true`, default template `homePage`.
Content Page: `icon-document color-light-blue`, default template `contentPage`. Contact Page:
`icon-phone color-light-blue`, default template `contactPage`. Author Folder:
`icon-folder color-green`, `allowedAsRoot: true`, no template. Author: `icon-user color-green`,
no template. Nothing varies by culture.

`get-document-type-composition-references` on SEO Composition: Home Page, Content Page, Contact
Page.

## Templates (get-template-root, get-template-children)

| Template | Alias | Children |
|---|---|---|
| Master | `master` | Contact Page (`contactPage`), Content Page (`contentPage`), Home Page (`homePage`) |

## Data types (find-data-type)

| Name | Editor | Folder |
|---|---|---|
| Textstring | `Umbraco.TextBox` | built-in |
| Textarea | `Umbraco.TextArea` | built-in |
| Richtext editor | `Umbraco.RichText` | built-in |
| True/false | `Umbraco.TrueFalse` | built-in |
| Email Address | `Umbraco.EmailAddress` | built-in |
| Image Media Picker | `Umbraco.MediaPicker3` | built-in |
| Multi URL Picker | `Umbraco.MultiUrlPicker` | built-in |
| Content Picker | `Umbraco.ContentPicker` | built-in |
| Author Picker | `Umbraco.MultiNodeTreePicker` (start node: the Authors folder, max 1) | `Content Picker/` |

## get-language

One language: `en-GB` (default).
