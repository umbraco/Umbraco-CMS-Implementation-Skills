# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

Document types: `get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`update-document-type`, `create-document-type`, `create-element-type`, `move-document-type`.
Data types: `get-all-data-types`, `find-data-type`, `get-data-type`, `get-references-data-type`,
`create-data-type`, `update-data-type`, `create-data-type-folder`, `get-data-type-folder`,
`get-data-type-ancestors`.

## Element types in Elements/ (get-document-type-by-id)

| Element | Alias | Settings model used where registered |
|---|---|---|
| Accordion | `accordion` | `titledBlockSettings` |
| Accordion Item | `accordionItem` | — (child of accordion, in Block List `Accordion Items`) |
| Hero | `hero` | `titledBlockSettings` |
| Promo | `promo` | `blockSettings` |
| Quote | `quote` | `blockSettings` |
| Rich Text | `richText` | `blockSettings` |
| Speaker Item | `speakerItem` | — (not registered anywhere yet) |

Settings models in `Elements/Settings/`: `blockSettings`, `titledBlockSettings`. There is no
newsletter signup element.

## Block editor data types (get-data-type)

**Main Content Block Grid** (`Block Grid/`), `Umbraco.BlockGrid`, grid columns 12:

| Content element | Settings element | At root | In areas | Column spans |
|---|---|---|---|---|
| hero | titledBlockSettings | Yes | No | 12 |
| richText | blockSettings | Yes | No | 12, 6 |
| quote | blockSettings | Yes | No | 12, 6 |

Used by (get-references-data-type): `contentPage.mainContent`, `articlePage.mainContent`,
`eventPage.mainContent`.

A content search the user ran earlier: 37 published pages contain at least one Quote block in
`mainContent`.

**Accordion Items** (`Block List/`), `Umbraco.BlockList`: registers `accordionItem`, amount 1 / no
maximum. Used by `accordion.items`.

There is no other Block Grid or Block List.

## Page types (get-document-type-by-id)

| Type | Tabs (sort) | Own properties: alias (tab, sort) |
|---|---|---|
| Content Page (`contentPage`) | Content (100) | `mainContent` (Content, 100) |
| Article Page (`articlePage`) | Content (100) | `mainContent` (Content, 100) |
| Event Page (`eventPage`) | Content (100) | `eventDate` (Content, 100), `mainContent` (Content, 200) |

## get-language

One language: `en-GB` (default).
