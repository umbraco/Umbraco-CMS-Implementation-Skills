# SEO Composition

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | SEO Composition |
| Alias | `seoComposition` |
| Kind | Document Type |
| Icon | `icon-settings` |
| Description | Adds meta title, meta description and search engine indexing settings for this page. |
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
| SEO | 600 | Search | 0 | Meta Title | `metaTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | The title shown in search results and the browser tab. Falls back to the page title if this is not set. |
| SEO | 600 | Search | 0 | Meta Description | `metaDescription` | [Meta Description Text Area](../../Data-Types/Text-Area/Meta-Description-Text-Area.md) | `Umbraco.TextArea` | `System.String` | No | 200 | The summary shown in search results. Falls back to the page summary if this is not set. Limited to 160 characters. |
| SEO | 600 | Search | 0 | Indexable | `isIndexable` | [Toggle (default on)](../../Data-Types/Toggle/Toggle-default-on.md) | `Umbraco.TrueFalse` | `System.Boolean` | No | 300 | Turn off to ask search engines not to index this page. On by default. |
| SEO | 600 | Search | 0 | Canonical URL Override | `canonicalUrlOverride` | Textstring | `Umbraco.TextBox` | `System.String` | No | 400 | Full URL, for example `https://example.com/original`. Leave blank to use the address of this page. |

## Used by

- Used as a composition by: [Article Page](../ArticlePage.md), [Article Listing Page](../ArticleListingPage.md), [Home Page](../HomePage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [Meta Description Text Area](../../Data-Types/Text-Area/Meta-Description-Text-Area.md) | `data-type` | New in this changeset |
| Textstring | `data-type` | Exists |
| [Toggle (default on)](../../Data-Types/Toggle/Toggle-default-on.md) | `data-type` | New in this changeset |
| [Compositions](../Compositions.md) *(document-type-container)* | `document-type-container` | New in this changeset |
