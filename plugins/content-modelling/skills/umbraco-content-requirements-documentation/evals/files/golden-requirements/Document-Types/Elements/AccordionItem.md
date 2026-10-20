# Accordion Item

> **Status:** approved

> **Element type**: used inside block editors; not routable as a page.

## Definition

| Setting | Value |
|---|---|
| Name | Accordion Item |
| Alias | `accordionItem` |
| Kind | Element Type |
| Icon | `icon-blockquote` |
| Description | One expandable panel inside an Accordion, with its own title and content. |
| Folder | [Document Types](../../Document-Types.md) / [Elements](../Elements.md) |
| Allowed at root | No |
| Vary by culture | No |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| — | — | Content | 0 | Title | `title` | Textstring | `Umbraco.TextBox` | `System.String` | Yes | 100 | The heading shown when the panel is closed. |
| — | — | Content | 0 | Content | `content` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | Yes | 200 | The text shown when the panel is open. |

## Used by

- Registered as a block in: [Accordion Items](../../Data-Types/Block-List/Accordion-Items.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Richtext editor | `data-type` | Exists |
| Textstring | `data-type` | Exists |
| [Elements](../Elements.md) *(document-type-container)* | `document-type-container` | New in this changeset |
