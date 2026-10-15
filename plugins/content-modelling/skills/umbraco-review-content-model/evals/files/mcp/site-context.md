# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Document Types tree (get-all-document-types, aliases from get-document-type-by-id)

```
Document Types/
    Compositions/
        Page Composition             pageComposition
        SEO Composition              seoComposition
    Hero Block                       heroBlock            (element type, loose at the root)
    Content Page                     contentPage
    Home Page                        homePage             (allowed at root)
    News                             news                 (allowed at root)
```

## Types (get-document-type-by-id)

| Type | Compositions | Allowed at root | Allowed children | Default template | Containers (sort) |
|---|---|---|---|---|---|
| Page Composition | seoComposition | No | — | — | tab Page Details (200) |
| SEO Composition | — | No | — | — | tab SEO (600) |
| Home Page | pageComposition | Yes | contentPage, news | homePage | tab Content (100), tab Settings (1) |
| Content Page | pageComposition | No | contentPage | contentPage | tab Content (100) |
| News | — | Yes | — | none | tab Content (0), tab SEO (1) |
| Hero Block | — (element) | No | — | — | tab Content (0), tab Settings (1) |

Own properties:

| Type | Property: alias (data type, container, sort) |
|---|---|
| Page Composition | `pageTitle` (Textstring, Page Details, 100) |
| SEO Composition | `metaTitle` (Textstring, SEO, 100), `metaDescription` (Textarea, SEO, 200) |
| Home Page | `heroTitle` (Textstring, Content, 100), `logo` (Image Media Picker, Settings, 0), `footerText` (Textarea, Settings, 1) |
| Content Page | `bodyText` (Richtext editor, Content, 100) |
| News | `bodyText` (Richtext editor, Content, 0), `sEOTitle` (Textstring, SEO, 0), `searchEngines` (True/false, SEO, 1) |
| Hero Block | `heroTitle` (Textstring, Content, 0), `image` (Image Media Picker, Content, 1), `backgroundColour` (Textstring, Settings, 0) |

Descriptions: every type and property description is empty.

## Data types (get-data-type, get-references-data-type)

| Name | Editor | Configuration | Used by |
|---|---|---|---|
| Textstring (built-in) | `Umbraco.TextBox` | default | 7 properties |
| Textarea (built-in) | `Umbraco.TextArea` | `maxChars`: 160 (changed from the default) | seoComposition.metaDescription, homePage.footerText |
| Main Content Block Grid | `Umbraco.BlockGrid` | registers heroBlock with no settings element | homePage and contentPage do not use it; no references |

## Templates

`master` with children `homePage`, `contentPage`. There is no `news` template.

## get-language

One language: `en-GB` (default).
