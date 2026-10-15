# News Summary Text Area

> **Status:** applied 2026-09-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | News Summary Text Area |
| Property editor | `Umbraco.TextArea` |
| Editor UI | `Umb.PropertyEditorUi.TextArea` |
| Database type | `Ntext` |
| Folder | [Data Types](../../Data-Types.md) / [Text Area](../Text-Area.md) |

## Configuration

Maximum characters: 200.
Rows: 3.

## Used by

| Content type | Property | Alias |
|---|---|---|
| [News Page](../../Document-Types/NewsPage.md) | Summary | `summary` |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Text Area](../Text-Area.md) *(data-type-container)* | `data-type-container` | Exists |
