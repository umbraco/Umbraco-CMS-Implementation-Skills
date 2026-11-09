# CMS Editor MCP read results: Campaigns section

Hand-written fixture shaped after the documented CMS Editor MCP tool outputs (content and
blueprints modes) for a fictional Umbraco 17 site, Example Co, language `en-US`. Treat it as what
the connected server reports. Nothing has been written to the site.

## Parent (search-content "Campaigns")

| Name | Path | Type | Allowed children |
|---|---|---|---|
| Campaigns | /campaigns/ | `campaignFolder` | `campaignPage` |

## Page type (get-document-type `campaignPage`)

| Property | Alias | Editor | Tab | Description |
|---|---|---|---|---|
| SEO title | `seoTitle` | Textstring | SEO | "Shown in search results. Keep it under 60 characters." |
| Meta description | `metaDescription` | Textarea | SEO | "Shown in search results." |
| Hide from navigation | `umbracoNaviHide` | True/false | Settings | |
| Content | `contentGrid` | Block Grid | Content | Allowed blocks: `heroBanner`, `richText`, `featureCards`, `testimonial`, `callToAction` |

Field limits for block properties are not exposed by the read tools.

## Blueprints (list-blueprints)

| Blueprint | Type |
|---|---|
| Standard campaign | `campaignPage` (hero, rich text, call to action, pre-filled) |

## Reference pages (get-page, inspect-blocks)

**Spring Offer** (/campaigns/spring-offer/, published 12-03-2026), blocks in order:

| # | Block | Fields (values abbreviated) |
|---|---|---|
| 1 | `heroBanner` | heading "Spring into online booking", intro (38 words), image (media picker), buttonText "Start free trial", buttonLink /signup/ |
| 2 | `richText` | body (210 words, two H2 headings) |
| 3 | `featureCards` | cards: 3 items, each title (3 to 5 words) and text (15 to 25 words) |
| 4 | `testimonial` | quote, name, role, company, photo |
| 5 | `callToAction` | heading, buttonText, buttonLink |

**Webinar 2022** (/campaigns/webinar-2022/, published 04-05-2022), blocks in order:

| # | Block | Fields |
|---|---|---|
| 1 | `videoEmbed` | url, caption |
| 2 | `richText` | body |

Note: `videoEmbed` is not in the current allowed blocks for `contentGrid`.
