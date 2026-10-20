# Author

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Author |
| Alias | `author` |
| Kind | Document Type |
| Icon | `icon-user color-green` |
| Description | One person who writes articles, picked from the Authors folder. |
| Folder | [Document Types](../../Document-Types.md) / [Data](../Data.md) |
| Allowed at root | No |
| Vary by culture | No |
| Default template | — |
| Allowed templates | — |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Content | 100 | — | — | Full Name | `fullName` | Textstring | `Umbraco.TextBox` | `System.String` | Yes | 100 | The name shown on articles, for example Jane Smith. |
| Content | 100 | — | — | Biography | `biography` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | No | 200 | A short paragraph shown with the author's articles. |

## Used by

- Allowed as a child of: [Author Folder](AuthorFolder.md)
- Picked by: [Author Picker](../../Data-Types/Content-Picker/Author-Picker.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textstring | `data-type` | Exists |
| Richtext editor | `data-type` | Exists |
| [Data](../Data.md) *(document-type-container)* | `document-type-container` | New in this changeset |
