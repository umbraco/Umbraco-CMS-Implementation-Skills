# Article

> **Status:** draft

## Definition

| Setting | Value |
|---|---|
| Name | Article |
| Alias | `article` |
| Kind | Document Type |
| Icon | `icon-newspaper` |
| Description | <Description> |
| Folder | Document Types |
| Allowed at root | Yes |
| Vary by culture | No |
| Collection | — |
| Default template | — |
| Allowed templates | — |
| Allowed children | — |
| Compositions | [SEO Composition](Compositions/SeoComposition.md), [Open Graph Composition](Compositions/OpenGraphComposition.md) |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Content | 1 | — | — | SEO Title | `SEOTitle` | Textstring | `Umbraco.TextBox` | `System.String` | yes | 150 | The title. |
| Content | 1 | — | — | Body | `bodyText` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | No | 200 | The body of the article. |
| Content | 1 | — | — | Name | `name` | Textstring | `Umbraco.TextBox` | `System.String` | No | 300 | Who wrote it? |

## Used by

- Allowed as a child of: —

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textstring | `datatype` | Todo |
| [SEO Composition](Compositions/SeoComposition.md) | `document-type` | New in this changeset |
