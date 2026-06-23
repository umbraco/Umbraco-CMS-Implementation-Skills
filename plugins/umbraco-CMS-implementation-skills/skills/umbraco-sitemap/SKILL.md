---
name: umbraco-sitemap
description: >
  Add an XML sitemap to an Umbraco 17+ site using a cached API controller, a composer, 
  and a cache invalidator. No backoffice setup required. Use this skill whenever 
  the user asks to add, create, build, implement, or enable an XML sitemap for 
  an Umbraco site — including related goals like "improve SEO", "let search 
  engines index the site", or "set up robots.txt". SKIP: non-Umbraco projects or Umbraco < 17.
---

# Sitemap

Three files: a controller that serves `/sitemap.xml`, a composer that wires up cache invalidation, and the invalidator itself. No backoffice configuration needed.

## What the implementation does

- Only published pages appear — unpublished content is never in the published cache
- Member-protected pages are excluded via `IPublicAccessService`
- An optional boolean property (e.g. `hideFromSitemap`) lets editors exclude individual pages; works whether the property is on a document type directly or inherited from a composition
- Results are cached in memory and invalidated on every publish, unpublish, or delete
- Multi-site aware: matches the request host to the correct root node; falls back to the first root in dev

## Version compatibility

This skill requires **Umbraco 17+**. It uses `IDocumentNavigationQueryService` which was introduced in Umbraco 15.

## Headless

For headless setups where the Umbraco host and public front-end domain differ, the standard `Url(mode: UrlMode.Absolute)` will return the wrong host in `<loc>` entries. Fix this by adding a fourth file, `SitemapSettings.cs`, alongside the composer:

- A simple POCO with a `BaseUrl` string property and a `const string SectionName = "Sitemap"`
- Register it in the composer: `builder.Services.Configure<SitemapSettings>(builder.Config.GetSection(SitemapSettings.SectionName))`
- Inject `IOptions<SitemapSettings>` into the controller and use `BaseUrl` to replace the host on each `<loc>` URL
- Instruct the user to add `"Sitemap": { "BaseUrl": "https://www.mysite.com" }` to `appsettings.json`

For traditional Umbraco none of this is needed — URLs come from Culture & Hostnames on the root node.

---

## Step 1 — Discover project context

Search for:
- Root namespace (`.csproj` or any `.cs` file)
- Umbraco version (`<PackageReference Include="Umbraco.Cms"` in `.csproj`)
- Whether the Delivery API is enabled or a separate front-end framework is present (signals headless)
- Any existing boolean "hide from sitemap" property on a document type or composition
- Folder conventions — look for `Controllers/`, `Composers/`, `Core/`, `Infrastructure/` to decide where each file belongs; follow the project pattern, fall back to `Controllers/` for the controller and project root for the rest. In flat projects with no subfolders, create the files at the project root but **still use `<RootNamespace>.Controllers` as the controller namespace** — the `.Controllers` suffix is a namespace convention, not tied to whether a physical folder exists

Ask before writing:

1. **Headless?** — if yes, ask for the public front-end base URL
2. **Filter property alias** — the boolean property to exclude pages (leave blank for none)

---

## Step 2 — Write the files

Read the templates in `assets/` as the source. Place each file per the folder conventions found in Step 1. Replace in every file:
- `<Namespace>` → confirmed namespace (controller uses `<Namespace>.Controllers`, others use `<Namespace>`)
- `<filterAlias>` → confirmed alias; if none, remove the filter `.Where()` line entirely

For headless, also write `SitemapSettings.cs` as described above.

---

## Done

Tell the user:
- Sitemap is at `/sitemap.xml`
- Cache invalidates automatically on content changes
- Add `Sitemap: https://yoursite.com/sitemap.xml` to `wwwroot/robots.txt`
- Traditional: requires a domain in **Culture & Hostnames** on the root node for absolute URLs
- Headless: set `Sitemap:BaseUrl` in `appsettings.json` to the public front-end domain
