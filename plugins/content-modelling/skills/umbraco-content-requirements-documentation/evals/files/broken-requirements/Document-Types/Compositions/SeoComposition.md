# SEO Composition

> **Status:** proposed

## Definition

| Setting | Value |
|---|---|
| Name | SEO Composition |
| Alias | `seoComposition` |
| Kind | Document Type |
| Description | Adds meta title and meta description. |
| Folder | Document Types / Compositions |
| Allowed at root | No |
| Vary by culture | No |
| Default template | SEO Composition |
| Allowed templates | SEO Composition |
| Allowed children | [Article](../Article.md) |
| Compositions | Page Details Composition |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| SEO | 600 | — | — | Meta Title | `metaTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | The title shown in search results. |

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textstring | `data-type` | Exists |
