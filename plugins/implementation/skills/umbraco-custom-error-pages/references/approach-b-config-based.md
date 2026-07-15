# Approach B — Config-based 404 (official docs, no custom code)

This is the **"Recommended"** method in the official tutorial,
[Implement Custom Error Pages](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page)
([Markdown version](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page.md)).
That tutorial is the source of truth — this file intentionally has no copy of its snippets, so
**fetch it before implementing** and follow its "404 Errors" section directly.

Requires **Umbraco 16.1+** (16.0 has a regression this method trips over — the tutorial calls
this out).

## When to choose this over Approach A

- The team wants **zero custom C#** — it's entirely backoffice + `appsettings.json`.
- Single-site, or multi-**lingual** (it supports per-culture 404 pages). It does **not** do
  per-domain multi-site resolution — for that use
  [Approach A](approach-a-content-finder.md).
- Note: the 404 page's content GUID goes into config, so each environment must either share
  content (e.g. restored DBs) or set its own GUID per environment.

## Shape of the work (fetch the tutorial for exact steps/snippets)

1. Create a *Page Not Found* **Document Type with Template** in the backoffice, add markup to
   the template, and publish a content node of that type (optionally inside an error-pages
   container).
2. Copy the published node's **Id (GUID)** from its Info tab.
3. Add the `Umbraco:CMS:Content:Error404Collection` block to `appsettings.json` with that
   GUID — per-culture entries are supported; copy the exact JSON from the tutorial.

## 500 errors

This approach only covers 404s. For 500s there is no config-based option — use the shared
[500 controller](500-error-controller.md) regardless of which 404 approach was chosen.
