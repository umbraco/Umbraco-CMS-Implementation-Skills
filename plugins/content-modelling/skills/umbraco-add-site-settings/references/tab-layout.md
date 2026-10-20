# Tab layout

Site settings is the one type whose tabs do not follow the global tab sorts: they are numbered
**0 to 9 in the order an editor reads them**, as
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#global-tab-sorts)
says. The linter rejects a sort above 9 on `siteSettings`.

## A typical layout

Use only the tabs the site needs, keep their relative order, and renumber from 0 with no gaps.

| Sort | Tab | Holds | Typical fields: alias (data type) |
|---|---|---|---|
| 0 | General | Identity of the site | `siteName` (Textstring), `logo` (Image Media Picker) |
| 1 | Header | What appears in every page's header | `mainNavigation` (a multinode tree picker of pages), `headerCallToAction` (Multi URL Picker, maximum 1) |
| 2 | Footer | What appears in every page's footer | `footerLinks` (Multi URL Picker), `copyrightText` (Textstring) |
| 3 | Social | Links to the organisation's profiles | `socialLinks` (a Block List of a `socialLinkItem` element, or a Multi URL Picker) |
| 4 | Contact | Details shown in the footer or a contact block | `contactEmail` (Email Address), `contactPhone` (Textstring), `address` (Textarea) |
| 5 | SEO Defaults | Values pages fall back to | `defaultShareImage` (Image Media Picker), `metaTitleSuffix` (Textstring) |
| 6 | Error Pages | Where not-found and error content lives | `notFoundPage` (Content Picker) |
| 7 | Scripts | Tracking and third-party snippets | `trackingId` (Textstring), `headScripts` (Textarea) |

Tabs 8 and 9 stay free for the project. Groups inside a tab are fine when a tab holds two clearly
different sets of fields (for example `Primary` and `Secondary` navigation in Header).

## Field rules

- **Aliases are area-prefixed where a page has a similar field.** `defaultShareImage`, not
  `shareImage`, so a page's own share image and the site default never read alike
  ([property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md)).
- **Links are pickers.** Navigation and footer links pick pages or use the Multi URL Picker, so
  they follow pages that move; never a text field holding a URL.
- **Repeated structured items are a repeater.** A list of social links with a network, a URL and an
  icon is a Block List of an element, made with
  [umbraco-add-block](../../umbraco-add-block/SKILL.md) and
  [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md).
- **Descriptions say where the value appears** ("Shown in every page footer.") and what the
  fallback is for defaults ("Used when a page has no share image of its own.")
  ([descriptions.md](../../umbraco-content-model-conventions/references/descriptions.md)).
- **Scripts are editor-maintained markup.** Say in the summary that whoever can edit the settings
  node can inject script into every page, so the node's permissions matter.

**Related:** [workflow.md](workflow.md).
