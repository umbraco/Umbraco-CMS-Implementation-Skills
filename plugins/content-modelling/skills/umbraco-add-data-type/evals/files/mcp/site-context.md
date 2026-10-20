# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected data-type tools (abbreviated)

`get-all-data-types`, `find-data-type`, `get-data-type`, `get-data-type-root`,
`get-data-type-children`, `get-data-type-ancestors`, `get-references-data-type`,
`create-data-type`, `update-data-type`, `create-data-type-folder`, `get-data-type-folder`, plus the
document-type tools (`get-document-type-by-id`, `update-document-type`, ...).

## Data Types tree (get-data-type-root, get-data-type-children, get-data-type)

| Name | Folder | Editor | Editor UI | Configuration | Used by (get-references-data-type) |
|---|---|---|---|---|---|
| Textstring | root (built-in) | `Umbraco.TextBox` | `Umb.PropertyEditorUi.TextBox` | `maxChars`: none | 14 properties |
| Textarea | root (built-in) | `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` | `maxChars`: none, `rows`: none | articlePage.summary, eventPage.summary, contentPage.summary, author.bio, openGraphComposition.shareDescription |
| Richtext editor | root (built-in) | `Umbraco.RichText` | `Umb.PropertyEditorUi.Tiptap` | default toolbar | 6 properties |
| True/false | root (built-in) | `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` | `default`: false | 2 properties |
| Date Picker | root (built-in) | `Umbraco.DateTime` | `Umb.PropertyEditorUi.DatePicker` | `format`: YYYY-MM-DD | eventPage.eventDate |
| Image Media Picker | root (built-in) | `Umbraco.MediaPicker3` | `Umb.PropertyEditorUi.MediaPicker` | `filter`: Image, `multiple`: false | 4 properties |
| Multi URL Picker | root (built-in) | `Umbraco.MultiUrlPicker` | `Umb.PropertyEditorUi.MultiUrlPicker` | `minNumber`: 0, `maxNumber`: 0 | — |
| Meta Description Text Area | `Text Area/` | `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` | `maxChars`: 160, `rows`: 3 | seoComposition.metaDescription |
| Multi URL Picker (single) | `Multi URL Picker/` | `Umbraco.MultiUrlPicker` | `Umb.PropertyEditorUi.MultiUrlPicker` | `minNumber`: 0, `maxNumber`: 1 | contentPage.callToAction |
| Toggle (default on) | `Toggle/` | `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` | `default`: true | seoComposition.isIndexable |

Folders: `Text Area/`, `Multi URL Picker/`, `Toggle/`, `Block Grid/`, `Block List/`.

## Relevant types (get-document-type-by-id)

| Type | Kind | Own properties: alias (data type, container, sort) |
|---|---|---|
| Article Page (`articlePage`) | page | `summary` (Textarea, Content tab 100, 100), `mainContent` (Main Content Block Grid, Content tab 100, 200) |
| Hero (`hero`) | element in `Elements/` | `title` (Textstring, group Content 0, 100), `image` (Image Media Picker, group Content 0, 200) |

Article Page has 212 published nodes. Several existing `summary` values are longer than 200
characters.

## get-language

One language: `en-GB` (default).
