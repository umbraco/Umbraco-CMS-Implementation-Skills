# MCP read results: existing site with blocks

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

`get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`get-document-type-tree-search`, `get-document-type-ancestors`,
`get-document-type-composition-references`, `create-document-type`, `create-element-type`,
`update-document-type`, `move-document-type`, `create-document-type-folder`,
`get-document-type-folder`, `get-icons`, `get-all-data-types`, `find-data-type`, `get-data-type`,
`get-references-data-type`, `create-data-type`, `update-data-type`, `create-data-type-folder`,
`get-data-type-folder`, `get-template`, `get-template-root`, `get-template-children`,
`create-template`, `get-language`.

## Document Types tree (aliases from get-document-type-by-id)

```
Document Types/
    Compositions/                         folder
        Open Graph Composition                openGraphComposition
        Page Details Composition              pageDetailsComposition
        SEO Composition                       seoComposition
    Elements/                             folder, id 7b1e0c2a-4d5f-4e6a-8b9c-0d1e2f3a4b5c
        Compositions/                         folder, id 8c2f1d3b-5e6a-4f7b-9cad-1e2f3a4b5c6d
            Anchor Settings Composition           anchorSettingsComposition
            Background Colour Settings Composition backgroundColourSettingsComposition
            Title Alignment Settings Composition  titleAlignmentSettingsComposition
        Settings/                             folder, id 9d3a2e4c-6f7b-4a8c-adbe-2f3a4b5c6d7e
            Block Settings                        blockSettings
            Titled Block Settings                 titledBlockSettings
        Hero                                  hero
        Rich Text                             richText
    Content Page                          contentPage
    Home Page                             homePage            (allowed at root)
```

## Page types and compositions (get-document-type-by-id)

| Type | Compositions | Allowed children | Own properties: alias (data type, tab and sort, sort) |
|---|---|---|---|
| Page Details Composition | — | — | `pageTitle` (Textstring, Page Details 200, 100), `pageSummary` (Textarea, Page Details 200, 200) |
| SEO Composition | — | — | `metaTitle` (Textstring, SEO 600, group Search 0, 100), `metaDescription` (Textarea, SEO 600, group Search 0, 200), `isIndexable` (True/false, SEO 600, group Search 0, 300) |
| Open Graph Composition | — | — | `shareTitle` (Textstring, SEO 600, group Sharing 100, 100), `shareImage` (Image Media Picker, SEO 600, group Sharing 100, 200) |
| Home Page | pageDetailsComposition, seoComposition, openGraphComposition | contentPage | — |
| Content Page | pageDetailsComposition, seoComposition, openGraphComposition | contentPage | `mainContent` (Main Content Block Grid, Content 100, 100) |

Home Page: `icon-home color-light-blue`, `allowedAsRoot: true`, default template `homePage`.
Content Page: `icon-document color-light-blue`, `allowedAsRoot: false`, default template
`contentPage`. Nothing varies by culture.

## Element types (get-document-type-by-id)

| Type | Compositions | Containers | Own properties: alias (container, sort) |
|---|---|---|---|
| Anchor Settings Composition | — | tab Settings (100) | `anchorId` (Settings, 100) |
| Background Colour Settings Composition | — | tab Style (50) | `backgroundColour` (Style, 100) |
| Title Alignment Settings Composition | — | tab Style (50) | `titleAlignment` (Style, 100) |
| Block Settings | anchorSettingsComposition, backgroundColourSettingsComposition | — | — |
| Titled Block Settings | anchorSettingsComposition, backgroundColourSettingsComposition, titleAlignmentSettingsComposition | — | — |
| Hero | — | group Content (0) | `title` (100), `image` (200), `link` (300) |
| Rich Text | — | group Content (0) | `content` (100) |

Descriptions: Block Settings "Settings for blocks without a title: background colour and anchor."
Titled Block Settings "Settings for blocks with a title: background colour, anchor and title
alignment."

## Templates (get-template-root, get-template-children)

| Template | Alias | Children |
|---|---|---|
| Master | `master` | Content Page (`contentPage`), Home Page (`homePage`) |

## Data types (find-data-type)

| Name | Editor | Folder |
|---|---|---|
| Textstring | `Umbraco.TextBox` | built-in |
| Textarea | `Umbraco.TextArea` | built-in |
| Richtext editor | `Umbraco.RichText` | built-in |
| True/false | `Umbraco.TrueFalse` | built-in |
| Image Media Picker | `Umbraco.MediaPicker3` | built-in |
| Multi URL Picker | `Umbraco.MultiUrlPicker` | built-in |
| Single URL Picker | `Umbraco.MultiUrlPicker` (max 1) | `Multi URL Picker/` |
| Background Colour Picker | `Umbraco.ColorPicker` | `Color Picker/` |
| Main Content Block Grid | `Umbraco.BlockGrid` | `Block Grid/` |

**Main Content Block Grid** (get-data-type), `Umbraco.BlockGrid`, grid columns 12, no limits:

| Content element | Settings element | At root | In areas | Column spans |
|---|---|---|---|---|
| hero | titledBlockSettings | Yes | No | 12 |
| richText | blockSettings | Yes | No | 12, 6 |

Used by (get-references-data-type): `contentPage.mainContent`. There is no `Block List/` folder
and no other block editor data type.

## Media types

`Image`, `File`, `Folder`, `Video`.

## get-language

One language: `en-GB` (default).
