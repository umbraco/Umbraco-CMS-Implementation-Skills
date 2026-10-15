# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

`get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`get-document-type-tree-search`, `get-document-type-ancestors`, `create-document-type`,
`create-element-type`, `update-document-type`, `move-document-type`, `create-document-type-folder`,
`get-document-type-folder`, `get-icons`, `get-all-data-types`, `find-data-type`, `get-data-type`,
`create-data-type`, `update-data-type`, `create-data-type-folder`, `get-data-type-folder`,
`get-references-data-type`, `get-language`.

## Document Types tree (with aliases read from get-document-type-by-id)

```
Document Types/
    Compositions/                         folder
        SEO Composition                   seoComposition
    Elements/                             folder, id 7b1e0c2a-4d5f-4e6a-8b9c-0d1e2f3a4b5c
        Compositions/                     folder, id 8c2f1d3b-5e6a-4f7b-9cad-1e2f3a4b5c6d
            Anchor Settings Composition               anchorSettingsComposition
            Background Colour Settings Composition    backgroundColourSettingsComposition
            Title Alignment Settings Composition      titleAlignmentSettingsComposition
        Settings/                         folder, id 9d3a2e4c-6f7b-4a8c-adbe-2f3a4b5c6d7e
            Block Settings                blockSettings
            Titled Block Settings         titledBlockSettings
        Hero                              hero
        Rich Text                         richText
    Article Page                          articlePage
    Home Page                             homePage
```

## Element types (get-document-type-by-id)

| Type | Compositions | Containers | Own properties: alias (container, sort) |
|---|---|---|---|
| Anchor Settings Composition | — | tab Settings (100) | `anchorId` (Settings, 100) |
| Background Colour Settings Composition | — | tab Style (50) | `backgroundColour` (Style, 100) |
| Block Settings | anchorSettingsComposition, backgroundColourSettingsComposition | — | — |
| Title Alignment Settings Composition | — | tab Style (50) | `titleAlignment` (Style, 100) |
| Titled Block Settings | anchorSettingsComposition, backgroundColourSettingsComposition, titleAlignmentSettingsComposition | — | — |
| Hero | — | group Content (0) | `title` (100), `image` (200), `link` (300) |
| Rich Text | — | group Content (0) | `content` (100) |

Descriptions: Block Settings "Settings for blocks without a title: background colour and anchor."
Titled Block Settings "Settings for blocks with a title: background colour, anchor and title
alignment."

## Data types (find-data-type)

| Name | Editor | Folder |
|---|---|---|
| Textstring | `Umbraco.TextBox` | built-in |
| Textarea | `Umbraco.TextArea` | built-in |
| Richtext editor | `Umbraco.RichText` | built-in |
| Image Media Picker | `Umbraco.MediaPicker3` | built-in |
| Multi URL Picker | `Umbraco.MultiUrlPicker` | built-in |
| Single URL Picker | `Umbraco.MultiUrlPicker` (max 1) | `Multi URL Picker/` |
| Background Colour Picker | `Umbraco.ColorPicker` | `Color Picker/` |
| Main Content Block Grid | `Umbraco.BlockGrid` | `Block Grid/` (registers hero and richText, each with blockSettings or titledBlockSettings) |

There is no `Block List/` data type folder yet.

## Media types

`Image`, `File`, `Folder`, `Video`.

## get-language

One language: `en-GB` (default).
