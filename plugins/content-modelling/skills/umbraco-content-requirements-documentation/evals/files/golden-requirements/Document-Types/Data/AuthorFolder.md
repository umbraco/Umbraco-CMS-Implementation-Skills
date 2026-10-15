# Author Folder

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Author Folder |
| Alias | `authorFolder` |
| Kind | Document Type |
| Icon | `icon-folder color-green` |
| Description | Holds the authors that articles can pick from. |
| Folder | [Document Types](../../Document-Types.md) / [Data](../Data.md) |
| Allowed at root | Yes |
| Vary by culture | No |
| Default template | — |
| Allowed templates | — |
| Allowed children | [Author](Author.md) |
| Compositions | — |

## Properties

No own properties: a folder holds its items and needs no fields of its own.

## Used by

- Allowed as a child of: —
- Content node `Authors` at the content root, sorted after the home page; the start node of [Author Picker](../../Data-Types/Content-Picker/Author-Picker.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Author](Author.md) | `document-type` | New in this changeset |
| [Data](../Data.md) *(document-type-container)* | `document-type-container` | New in this changeset |
