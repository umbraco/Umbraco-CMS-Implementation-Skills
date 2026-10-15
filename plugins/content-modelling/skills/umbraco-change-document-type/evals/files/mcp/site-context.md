# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected document-type, data-type, template and language tools

```
get-all-document-types
get-document-type-by-id
get-document-type-batch
get-document-types-by-id-array
get-document-type-tree-search
get-document-type-ancestors
get-document-type-composition-references
get-document-type-allowed-children
create-document-type
update-document-type
move-document-type
create-document-type-folder
get-document-type-folder
get-icons
get-all-data-types
find-data-type
get-data-type
get-references-data-type
get-template
get-template-root
get-template-children
get-language
```

## get-all-document-types

```json
[
  { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10", "name": "Compositions", "isFolder": true, "isElement": false, "parent": null },
  { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000002", "name": "Page Details Composition", "isFolder": false, "isElement": false, "parent": { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10" } },
  { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000003", "name": "SEO Composition", "isFolder": false, "isElement": false, "parent": { "id": "5d0c8a51-6f0e-4a55-9b2b-1f3c9d8e2a10" } },
  { "id": "6e1d9b62-7a1f-4b66-8c3c-2a4d0e9f3b21", "name": "Elements", "isFolder": true, "isElement": false, "parent": null },
  { "id": "d4e5f6a7-4444-4f5a-b1c2-000000000031", "name": "Rich Text", "isFolder": false, "isElement": true, "parent": { "id": "6e1d9b62-7a1f-4b66-8c3c-2a4d0e9f3b21" } },
  { "id": "d4e5f6a7-4444-4f5a-b1c2-000000000032", "name": "Accordion", "isFolder": false, "isElement": true, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000010", "name": "Content Page", "isFolder": false, "isElement": false, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000011", "name": "Home Page", "isFolder": false, "isElement": false, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000012", "name": "Article Listing Page", "isFolder": false, "isElement": false, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000013", "name": "Article Page", "isFolder": false, "isElement": false, "parent": null },
  { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000014", "name": "Event Page", "isFolder": false, "isElement": false, "parent": null }
]
```

## get-document-type-batch (the non-folder types above)

| Name | Alias | allowedAsRoot | Compositions | allowedDocumentTypes |
|---|---|---|---|---|
| Page Details Composition | `pageDetailsComposition` | false | — | — |
| SEO Composition | `seoComposition` | false | — | — |
| Rich Text | `richText` | false | — | — |
| Accordion | `accordion` | false | — | — |
| Content Page | `contentPage` | false | pageDetailsComposition, seoComposition | contentPage |
| Home Page | `homePage` | true | pageDetailsComposition, seoComposition | contentPage, articleListingPage |
| Article Listing Page | `articleListingPage` | false | pageDetailsComposition, seoComposition | articlePage, articleListingPage |
| Article Page | `articlePage` | false | pageDetailsComposition, seoComposition | — |
| Event Page | `eventPage` | false | pageDetailsComposition, seoComposition | — |

## Composition fields (from get-document-type-by-id on each)

| Composition | Tab (sort) | Group (sort) | Properties (alias, sort) |
|---|---|---|---|
| Page Details Composition | Page Details (200) | — | `pageTitle` 100, `pageSummary` 200 |
| SEO Composition | SEO (600) | — | `metaTitle` 100, `metaDescription` 200, `isIndexable` 300 |

## get-document-type-by-id: Article Page

```json
{
  "id": "b2e3c4d5-2222-4d3e-9fa0-000000000013",
  "alias": "articlePage",
  "name": "Article Page",
  "icon": "icon-newspaper color-light-blue",
  "description": "A single article, news item or story.",
  "allowedAsRoot": false,
  "variesByCulture": false,
  "isElement": false,
  "compositions": [
    { "documentType": { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000002" }, "compositionType": "Composition" },
    { "documentType": { "id": "a1f2b3c4-1111-4c2d-8e9f-000000000003" }, "compositionType": "Composition" }
  ],
  "allowedDocumentTypes": [],
  "defaultTemplate": { "id": "c3f4d5e6-3333-4e4f-a0b1-000000000023" },
  "allowedTemplates": [ { "id": "c3f4d5e6-3333-4e4f-a0b1-000000000023" } ],
  "containers": [
    { "id": "e5f6a7b8-5555-4a6b-c2d3-000000000041", "parent": null, "name": "Content", "type": "Tab", "sortOrder": 100 }
  ],
  "properties": [
    { "id": "f6a7b8c9-6666-4b7c-d3e4-000000000051", "container": { "id": "e5f6a7b8-5555-4a6b-c2d3-000000000041" }, "alias": "mainContent", "name": "Main Content", "description": "The body of the article, built from blocks.", "dataType": { "id": "a9b8c7d6-7777-4c8d-e4f5-000000000061" }, "variesByCulture": false, "sortOrder": 100, "validation": { "mandatory": false } },
    { "id": "f6a7b8c9-6666-4b7c-d3e4-000000000052", "container": { "id": "e5f6a7b8-5555-4a6b-c2d3-000000000041" }, "alias": "author", "name": "Author", "description": "", "dataType": { "id": "a9b8c7d6-7777-4c8d-e4f5-000000000062" }, "variesByCulture": false, "sortOrder": 200, "validation": { "mandatory": false } },
    { "id": "f6a7b8c9-6666-4b7c-d3e4-000000000053", "container": { "id": "e5f6a7b8-5555-4a6b-c2d3-000000000041" }, "alias": "legacyBanner", "name": "Legacy Banner", "description": "Old hero image, no longer rendered.", "dataType": { "id": "a9b8c7d6-7777-4c8d-e4f5-000000000063" }, "variesByCulture": false, "sortOrder": 300, "validation": { "mandatory": false } }
  ]
}
```

## get-document-type-by-id: Home Page (abbreviated)

```json
{
  "id": "b2e3c4d5-2222-4d3e-9fa0-000000000011",
  "alias": "homePage",
  "allowedAsRoot": true,
  "allowedDocumentTypes": [
    { "documentType": { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000010" }, "sortOrder": 0 },
    { "documentType": { "id": "b2e3c4d5-2222-4d3e-9fa0-000000000012" }, "sortOrder": 1 }
  ],
  "properties": [],
  "containers": []
}
```

## get-document-type-composition-references

| Composition | Used by |
|---|---|
| `seoComposition` | contentPage, homePage, articleListingPage, articlePage, eventPage |
| `pageDetailsComposition` | contentPage, homePage, articleListingPage, articlePage, eventPage |

## Data types (find-data-type)

| Name | Editor | Id |
|---|---|---|
| Textstring | `Umbraco.TextBox` | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` |
| Textarea | `Umbraco.TextArea` | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` |
| Richtext editor | `Umbraco.RichText` | `ca90c950-0aff-4e72-b976-a30b1ac57dad` |
| Image Media Picker | `Umbraco.MediaPicker3` | `a9b8c7d6-7777-4c8d-e4f5-000000000063` |
| Main Content Block Grid | `Umbraco.BlockGrid` | `a9b8c7d6-7777-4c8d-e4f5-000000000061` |
| Author Picker | `Umbraco.ContentPicker` | `a9b8c7d6-7777-4c8d-e4f5-000000000062` |
| Multinode Treepicker | `Umbraco.MultiNodeTreePicker` | `1f4b9a7c-8888-4d9e-f5a6-000000000064` |

## Templates (get-template-root, get-template-children)

| Template | Alias | Children |
|---|---|---|
| Master | `master` | Content Page, Home Page, Article Listing Page, Article Page, Event Page |

## get-language

```json
{ "items": [ { "isoCode": "en-US", "name": "English (United States)", "isDefault": true } ], "total": 1 }
```

## Content (user's word, not read: the `document` collection is not connected)

About 140 published Article Page nodes; 9 of them have a Legacy Banner image set.
