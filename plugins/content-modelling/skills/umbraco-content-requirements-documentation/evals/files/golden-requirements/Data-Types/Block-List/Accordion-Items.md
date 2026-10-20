# Accordion Items

> **Status:** approved

## Definition

| Setting | Value |
|---|---|
| Name | Accordion Items |
| Property editor | `Umbraco.BlockList` |
| Editor UI | `Umb.PropertyEditorUi.BlockList` |
| Database type | — |
| Folder | [Data Types](../../Data-Types.md) / [Block List](../Block-List.md) |

## Configuration

Amount (min/max): 1 / unlimited.

| Group | Content element | Settings element | At root | In areas | Column spans |
|---|---|---|---|---|---|
| — | [Accordion Item](../../Document-Types/Elements/AccordionItem.md) | — | No | No | — |

## Used by

| Content type | Property | Alias |
|---|---|---|
| [Accordion](../../Document-Types/Elements/Accordion.md) | Items | `items` |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Block List](../Block-List.md) *(data-type-container)* | `data-type-container` | New in this changeset |
| [Accordion Item](../../Document-Types/Elements/AccordionItem.md) | `document-type` | New in this changeset |
