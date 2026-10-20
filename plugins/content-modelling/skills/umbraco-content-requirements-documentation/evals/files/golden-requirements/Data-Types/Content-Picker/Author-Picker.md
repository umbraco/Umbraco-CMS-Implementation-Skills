# Author Picker

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Author Picker |
| Property editor | `Umbraco.ContentPicker` |
| Editor UI | `Umb.PropertyEditorUi.DocumentPicker` |
| Database type | `Nvarchar` |
| Folder | [Data Types](../../Data-Types.md) / [Content Picker](../Content-Picker.md) |

## Configuration

Start node: the `Authors` content node (an [Author Folder](../../Document-Types/Data/AuthorFolder.md)), looked up at apply time, so this data type is created after that node exists.

| Setting | Value |
|---|---|
| `startNodeId` | the `Authors` node |
| `ignoreUserStartNodes` | `true` |

## Used by

| Content type | Property | Alias |
|---|---|---|
| [Article Page](../../Document-Types/ArticlePage.md) | Author | `author` |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Content Picker](../Content-Picker.md) *(data-type-container)* | `data-type-container` | New in this changeset |
| [Author Folder](../../Document-Types/Data/AuthorFolder.md) | `document-type` | New in this changeset |
