# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected document-type, template and language tools

```
get-all-document-types
get-document-type-by-id
get-document-types-by-id-array
get-document-type-tree-search
get-document-type-ancestors
get-document-type-composition-references
get-document-type-allowed-children
create-document-type
update-document-type
move-document-type
create-document-type-folder
get-icons
get-all-data-types
find-data-type
get-data-type
get-template
get-template-root
get-template-children
create-template
get-language
get-language-items
```

## get-all-document-types

```json
[
  { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10", "name": "Compositions", "isFolder": true, "isElement": false, "parent": null },
  { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000001", "name": "Open Graph Composition", "isFolder": false, "isElement": false, "parent": { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10" } },
  { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000002", "name": "Page Details Composition", "isFolder": false, "isElement": false, "parent": { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10" } },
  { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000003", "name": "SEO Composition", "isFolder": false, "isElement": false, "parent": { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10" } },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000010", "name": "Content Page", "isFolder": false, "isElement": false, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000011", "name": "Home Page", "isFolder": false, "isElement": false, "parent": null }
]
```

## get-document-type-batch (the five types above)

| Name | Alias | allowedAsRoot | Compositions |
|---|---|---|---|
| Open Graph Composition | `openGraphComposition` | false | — |
| Page Details Composition | `pageDetailsComposition` | false | — |
| SEO Composition | `seoComposition` | false | — |
| Content Page | `contentPage` | false | pageDetailsComposition, seoComposition, openGraphComposition |
| Home Page | `homePage` | true | pageDetailsComposition, seoComposition, openGraphComposition |

## Composition fields (from get-document-type-by-id on each)

| Composition | Tab (sort) | Group (sort) | Properties (alias, sort) |
|---|---|---|---|
| Page Details Composition | Page Details (200) | — | `pageTitle` 100, `summary` 200 |
| SEO Composition | SEO (600) | Search (0) | `metaTitle` 100, `metaDescription` 200, `isIndexable` 300 |
| Open Graph Composition | SEO (600) | Sharing (100) | `shareTitle` 100, `shareImage` 200 |

## get-document-type-by-id: Home Page (abbreviated)

```json
{
  "id": "b2e3c4d5-2222-4d3e-9fa0-000000000011",
  "alias": "homePage",
  "name": "Home Page",
  "icon": "icon-home color-light-blue",
  "allowedAsRoot": true,
  "variesByCulture": false,
  "compositions": [
    { "documentType": { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000002" }, "compositionType": "Composition" },
    { "documentType": { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000003" }, "compositionType": "Composition" },
    { "documentType": { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000001" }, "compositionType": "Composition" }
  ],
  "allowedDocumentTypes": [
    { "documentType": { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000010" }, "sortOrder": 0 }
  ],
  "defaultTemplate": { "id": "c3f4d5e6-3333-4e4f-a0b1-000000000021" },
  "allowedTemplates": [ { "id": "c3f4d5e6-3333-4e4f-a0b1-000000000021" } ],
  "properties": [],
  "containers": []
}
```

## Templates (get-template-root, get-template-children)

| Template | Alias | Children |
|---|---|---|
| Master | `master` | Content Page (`contentPage`), Home Page (`homePage`) |

## Data types (find-data-type)

| Name | Editor | Id |
|---|---|---|
| Textstring | `Umbraco.TextBox` | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` |
| Textarea | `Umbraco.TextArea` | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` |
| Richtext editor | `Umbraco.RichText` | `ca90c950-0aff-4e72-b976-a30b1ac57dad` |
| Email Address | `Umbraco.EmailAddress` | `8c2f5a3e-6d1b-4a7c-9e0f-2b4d6a8c0e11` |

## get-language

```json
{ "items": [ { "isoCode": "en-US", "name": "English (United States)", "isDefault": true } ], "total": 1 }
```
