# Open Graph Composition

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Open Graph Composition |
| Alias | `openGraphComposition` |
| Kind | Document Type |
| Icon | `icon-settings` |
| Description | Adds the title and image shown when this page is shared on social networks. |
| Folder | [Document Types](../../Document-Types.md) / [Compositions](../Compositions.md) |
| Allowed at root | No |
| Vary by culture | No |
| Default template | — |
| Allowed templates | — |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| SEO | 600 | Sharing | 100 | Share Title | `shareTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | The title shown when the page is shared. Falls back to the meta title, then the page title. |
| SEO | 600 | Sharing | 100 | Share Image | `shareImage` | Image Media Picker | `Umbraco.MediaPicker3` | `Umbraco.Cms.Core.Models.MediaWithCrops` | No | 200 | The image shown when the page is shared. Falls back to the site's default share image. |

## Used by

- Used as a composition by: [Article Page](../ArticlePage.md), [Home Page](../HomePage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textstring | `data-type` | Exists |
| Image Media Picker | `data-type` | Exists |
| [Compositions](../Compositions.md) *(document-type-container)* | `document-type-container` | Exists |
