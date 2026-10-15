# MCP read results: existing site

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a local Umbraco 17.5.3 site with the
`document-type`, `data-type`, `template` and `language` collections enabled. Treat it as what the
connected server reports. Nothing has been written to the site.

## Connected tools (abbreviated)

Document types: `get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`create-document-type`, `update-document-type`, `get-icons`. Data types: `get-all-data-types`,
`find-data-type`, `get-data-type`, `create-data-type`, `create-data-type-folder`.

## Document Types tree (aliases from get-document-type-by-id)

```
Document Types/
    Compositions/
        SEO Composition              seoComposition
    Content Page                     contentPage
    Home Page                        homePage          (allowed at root)
    Site Settings                    siteSettings      (allowed at root, no template, no children)
```

## Home Page (get-document-type-by-id)

Compositions: seoComposition. Allowed children: contentPage. Tabs: Content (100), Settings (1).

| Property | Alias | Data type | Tab | Sort |
|---|---|---|---|---|
| Hero Title | `heroTitle` | Textstring | Content | 100 |
| Logo | `logo` | Image Media Picker | Settings | 0 |
| Footer Text | `footerText` | Textarea | Settings | 1 |
| Facebook URL | `facebookUrl` | Textstring | Settings | 2 |
| Twitter URL | `twitterUrl` | Textstring | Settings | 3 |

The one Home Page node has values in all four Settings fields.

## Site Settings (get-document-type-by-id)

Allowed at root, no template, no allowed children, no compositions. Tabs: General (0), Footer (1).

| Property | Alias | Data type | Tab | Sort |
|---|---|---|---|---|
| Site Name | `siteName` | Textstring | General | 100 |
| Copyright Text | `copyrightText` | Textstring | Footer | 100 |

One Site Settings content node exists at the content root, beside the one Home Page node.

## Data types (find-data-type)

Built-in only: Textstring, Textarea, Richtext editor, True/false, Image Media Picker, Multi URL
Picker, Content Picker. No custom data types.

## get-language

One language: `en-GB` (default).
