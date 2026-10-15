# Page Details Composition

> **Status:** approved

## Definition

| Setting | Value |
|---|---|
| Name | Page Details Composition |
| Alias | `pageDetailsComposition` |
| Kind | Document Type |
| Icon | `icon-settings` |
| Description | Adds the page title and summary shown at the top of the page and in listings. |
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
| Page Details | 200 | — | — | Page Title | `pageTitle` | Textstring | `Umbraco.TextBox` | `System.String` | Yes | 100 | The main heading of the page, for example "Our services". Falls back to the node name if this is not set. |
| Page Details | 200 | — | — | Page Summary | `pageSummary` | Textarea | `Umbraco.TextArea` | `System.String` | No | 200 | One or two sentences shown under the title and on listing cards. Leave blank to show no summary. |

## Used by

- Used as a composition by: [Article Page](../ArticlePage.md), [Article Listing Page](../ArticleListingPage.md), [Home Page](../HomePage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textarea | `data-type` | Exists |
| Textstring | `data-type` | Exists |
| [Compositions](../Compositions.md) *(document-type-container)* | `document-type-container` | New in this changeset |
