# Site Settings

> **Status:** applied 2026-10-01 via MCP

## Definition

| Setting | Value |
|---|---|
| Name | Site Settings |
| Alias | `siteSettings` |
| Kind | Document Type |
| Icon | `icon-settings color-green` |
| Description | Holds the settings that apply to the whole site. One per site, next to the home page. |
| Folder | [Document Types](../Document-Types.md) |
| Allowed at root | Yes |
| Vary by culture | No |
| Default template | — |
| Allowed templates | — |
| Allowed children | — |
| Compositions | — |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| General | 0 | — | — | Site Name | `siteName` | Textstring | `Umbraco.TextBox` | `System.String` | Yes | 100 | Shown in the browser tab after each page title. |
| Footer | 1 | — | — | Footer Text | `footerText` | Textarea | `Umbraco.TextArea` | `System.String` | No | 100 | The copyright line at the bottom of every page. |

## Used by

- Allowed as a child of: —
- Content node `Site Settings` at the content root, sorted after the home page

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Textstring | `data-type` | Exists |
| Textarea | `data-type` | Exists |
