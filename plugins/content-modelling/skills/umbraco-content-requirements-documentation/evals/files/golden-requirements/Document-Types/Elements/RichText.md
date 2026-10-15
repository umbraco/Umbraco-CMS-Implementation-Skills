# Rich Text

> **Status:** approved

> **Element type**: used inside block editors; not routable as a page.

## Definition

| Setting | Value |
|---|---|
| Name | Rich Text |
| Alias | `richText` |
| Kind | Element Type |
| Icon | `icon-edit` |
| Description | A block of formatted text. |
| Folder | [Document Types](../../Document-Types.md) / [Elements](../Elements.md) |
| Allowed at root | No |
| Vary by culture | No |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| — | — | Content | 0 | Content | `content` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | Yes | 100 | The formatted text to display. |

## Used by

- Registered as a block in: [Main Content Block Grid](../../Data-Types/Block-Grid/Main-Content-Block-Grid.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Richtext editor | `data-type` | Exists |
| [Elements](../Elements.md) *(document-type-container)* | `document-type-container` | New in this changeset |
