# News Page

> **Status:** applied 2026-09-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | News Page |
| Alias | `newsPage` |
| Kind | Document Type |
| Icon | `icon-document` |
| Description | News page. |
| Folder | [Document Types](../Document-Types.md) |
| Allowed at root | No |
| Vary by culture | No |
| Default template | [News Page](../Templates/Master/NewsPage.md) |
| Allowed templates | [News Page](../Templates/Master/NewsPage.md) |
| Allowed children | — |
| Compositions | [SEO Composition](Compositions/SeoComposition.md) |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Content | 100 | — | — | Summary | `summary` | [News Summary Text Area](../Data-Types/Text-Area/News-Summary-Text-Area.md) | `Umbraco.TextArea` | `System.String` | No | 1 | — |
| Content | 100 | — | — | Body | `bodyText` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | No | 2 | — |
| SEO | 1 | — | — | Meta Title | `metaTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 3 | — |

## Used by

- Allowed as a child of: —

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| [News Summary Text Area](../Data-Types/Text-Area/News-Summary-Text-Area.md) | `data-type` | Exists |
| Richtext editor | `data-type` | Exists |
| Textstring | `data-type` | Exists |
| [SEO Composition](Compositions/SeoComposition.md) | `document-type` | Exists |
| [News Page](../Templates/Master/NewsPage.md) | `template` | Exists |
