# Approach B — Razor template + Document Type (official docs)

Follows the official tutorial, [Creating an XML Sitemap](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/creating-an-xml-site-map.md).
That's the source of truth for the code, this file has no local copy, so **fetch it before
implementing**. Don't choose this approach for a headless / Delivery-API-only site — use
[Approach A](approach-a-cached-controller.md) instead.

Choose it when the team wants the official-docs approach and/or per-page editor control
(priority, change frequency, hide) via Document Types.

## Backoffice work

Most of this is backoffice configuration (Document Types, a composition, a content node):
1. Prefer the [Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp) to do it directly.
2. No MCP? Walk the user through the steps below one at a time in the backoffice UI — don't
   drop to Approach A just because the MCP is missing.
3. Only fall back to [Approach A](approach-a-cached-controller.md) if backoffice access isn't
   possible at all, or the tutorial can't be fetched.

## Steps (fetch the tutorial for exact clicks/code)

1. **`XmlSiteMap` Document Type** (with template) — an `excludedDocumentTypes` TextString
   property, allowed as a child of the site root; add a content node and exclude itself.
2. **`XmlSiteMapSettings` composition** — priority slider, change-frequency dropdown, hide
   toggle.
3. **Apply the composition** to every content Document Type, so editors can set
   priority / change frequency / hide per page.
4. **`XmlSiteMap.cshtml` template** — copy the Razor code from the tutorial's template
   section verbatim; don't retype it from memory.
5. **Filtering** — add `hideFromXmlSiteMap`, `excludedDocumentTypes`, and the optional
   `maxSiteMapDepth` filters, per the tutorial's "Filter the sitemap content" section.

## Notes

- Sitemap URL is the **XmlSiteMap content node's URL** (e.g. `/xmlsitemap`), not a fixed
  `/sitemap.xml` route. Reference it in `wwwroot/robots.txt`: `Sitemap: https://www.yoursite.com/xmlsitemap`.
- No caching — rendered per request (Approach A caches). Per-page editor control is this
  approach's strength (Approach A has none).
- Apply the same sitemaps.org / Google rules the tutorial follows: absolute `<loc>` URLs,
  `application/xml; charset=utf-8`, and stay under the **50,000 URL / 50 MB per-file limit**
  (split into a `<sitemapindex>` beyond that). Google ignores `<priority>`/`<changefreq>`, so the
  tutorial's per-page priority/change-frequency are editor conveniences, not required output.
