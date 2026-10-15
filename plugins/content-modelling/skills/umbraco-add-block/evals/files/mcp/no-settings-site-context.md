# MCP read results: site with no block settings

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
        Rich Text                         richText
    Content Page                          contentPage
    Home Page                             homePage
```

There are no `Elements/Settings/` or `Elements/Compositions/` folders, no settings models and no
settings compositions.

## Element types (get-document-type-by-id)

| Type | Compositions | Containers | Own properties: alias (container, sort) |
|---|---|---|---|
| Rich Text | — | group Content (0) | `content` (100) |

## Data types (find-data-type)

| Name | Editor | Folder |
|---|---|---|
| Textstring | `Umbraco.TextBox` | built-in |
| Textarea | `Umbraco.TextArea` | built-in |
| Richtext editor | `Umbraco.RichText` | built-in |
| Image Media Picker | `Umbraco.MediaPicker3` | built-in |
| Multi URL Picker | `Umbraco.MultiUrlPicker` | built-in |
| True/false | `Umbraco.TrueFalse` | built-in |
| Main Content Block Grid | `Umbraco.BlockGrid` | `Block Grid/` (registers richText with no settings element; used by homePage and contentPage as `mainContent`) |

There is no colour picker data type and no `Color Picker/` folder.

## Media types

`Image`, `File`, `Folder`.

## get-language

One language: `en-GB` (default).
