# Main Content Block Grid

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Main Content Block Grid |
| Property editor | `Umbraco.BlockGrid` |
| Editor UI | `Umb.PropertyEditorUi.BlockGrid` |
| Database type | — |
| Folder | [Data Types](../../Data-Types.md) / [Block Grid](../Block-Grid.md) |

## Configuration

Grid columns: 12.

| Group | Content element | Settings element | At root | In areas | Column spans |
|---|---|---|---|---|---|
| — | [Accordion](../../Document-Types/Elements/Accordion.md) | [Accordion Settings](../../Document-Types/Elements/Settings/AccordionSettings.md) | Yes | No | — |
| — | [Rich Text](../../Document-Types/Elements/RichText.md) | — | Yes | No | — |

## Used by

| Content type | Property | Alias |
|---|---|---|
| [Article Page](../../Document-Types/ArticlePage.md) | Main Content | `mainContent` |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Block Grid](../Block-Grid.md) *(data-type-container)* | `data-type-container` | New in this changeset |
| [Accordion](../../Document-Types/Elements/Accordion.md) | `document-type` | New in this changeset |
| [Rich Text](../../Document-Types/Elements/RichText.md) | `document-type` | New in this changeset |
| [Accordion Settings](../../Document-Types/Elements/Settings/AccordionSettings.md) | `document-type` | New in this changeset |
