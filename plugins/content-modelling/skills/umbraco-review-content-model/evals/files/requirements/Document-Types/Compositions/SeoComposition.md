# SEO Composition

> **Status:** applied 2026-09-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | SEO Composition |
| Alias | `seoComposition` |
| Kind | Document Type |
| Icon | `icon-settings` |
| Description | SEO composition. |
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
| SEO | 600 | — | — | Meta Title | `metaTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | The title shown in search results. |
| SEO | 600 | — | — | Meta Description | `metaDescription` | Textarea | `Umbraco.TextArea` | `System.String` | No | 200 | The summary shown in search results. |
| SEO | 600 | — | — | Hero Image | `heroImage` | Image Media Picker | `Umbraco.MediaPicker3` | `Umbraco.Cms.Core.Models.MediaWithCrops` | No | 300 | The large image at the top of the page. |

## Used by

- Used as a composition by: [News Page](../NewsPage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Image Media Picker | `data-type` | Exists |
| Textarea | `data-type` | Exists |
| Textstring | `data-type` | Exists |
| [Compositions](../Compositions.md) *(document-type-container)* | `document-type-container` | Exists |
