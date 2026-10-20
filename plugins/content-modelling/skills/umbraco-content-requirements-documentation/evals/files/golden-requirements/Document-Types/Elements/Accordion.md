# Accordion

> **Status:** approved

> **Element type**: used inside block editors; not routable as a page.

## Definition

| Setting | Value |
|---|---|
| Name | Accordion |
| Alias | `accordion` |
| Kind | Element Type |
| Icon | `icon-list` |
| Description | A set of expandable panels, each with a title and content. |
| Folder | [Document Types](../../Document-Types.md) / [Elements](../Elements.md) |
| Allowed at root | No |
| Vary by culture | No |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| — | — | Content | 0 | Title | `title` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | An optional heading shown above the panels. |
| — | — | Content | 0 | Items | `items` | [Accordion Items](../../Data-Types/Block-List/Accordion-Items.md) | `Umbraco.BlockList` | `Umbraco.Cms.Core.Models.Blocks.BlockListModel` | Yes | 200 | The panels, in the order they are shown. At least one is required. |

## Used by

- Registered as a block in: [Main Content Block Grid](../../Data-Types/Block-Grid/Main-Content-Block-Grid.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Accordion Items](../../Data-Types/Block-List/Accordion-Items.md) | `data-type` | New in this changeset |
| Textstring | `data-type` | Exists |
| [Elements](../Elements.md) *(document-type-container)* | `document-type-container` | New in this changeset |
