# Approach A — Cached API controller (custom, no backoffice)

A `/sitemap.xml` route served by a custom controller, with in-memory caching invalidated on
content changes. **No backoffice configuration required** — entirely file-based, so it can be
implemented without the Umbraco Developer MCP.

## When to choose this approach

- **Headless / Delivery-API** sites, or any site without Razor templates.
- You want a fixed `/sitemap.xml` route and **caching** out of the box.
- You cannot (or do not want to) make backoffice changes / use the MCP.
- You want everything in source control as `.cs` files.

If the project is a traditional Razor site and the team prefers editor-managed, per-page
sitemap settings that match the official documentation, use
[Approach B](approach-b-razor-template.md) instead.

## Building blocks (source of truth for each piece)

- **Composer / `IComposer`** — registering services at startup:
  [Composing](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md)
- **Notification handlers** (`ContentPublishedNotification`, etc.) — reacting to content
  changes:
  [Subscribing to Notifications](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/subscribing-to-notifications.md)
- **`IPublicAccessService`** — detecting member-protected pages:
  [Umbraco Services](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/umbraco-services.md)
- **`IDocumentNavigationQueryService`** (Umbraco 15+) — finding content root keys:
  [Services and Helpers](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services.md)
- **Custom controller routing / route hijacking** — serving a fixed route:
  [Custom Routes](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/routing/custom-routes.md)

## What the implementation does

- Only published pages appear — unpublished content is never in the published cache.
- Member-protected pages are excluded via `IPublicAccessService`.
- An optional boolean property (e.g. `hideFromSitemap`) lets editors exclude individual
  pages; works whether the property is on a Document Type directly or inherited from a
  composition.
- Results are cached in memory and invalidated on every publish, unpublish, or delete.
- Multi-site aware: matches the request host to the correct root node; falls back to the
  first root in dev.

## The files

Read the templates in [`../assets/`](../assets) as the source.

Pick **one** controller based on how many URLs the site has (or will grow to):

- `SitemapController.cs` — **single-file** sitemap at `/sitemap.xml`. Use for sites **under
  50,000 URLs**.
- `SitemapIndexController.cs` — **split** sitemap: a `<sitemapindex>` at `/sitemap.xml` plus paged
  `<urlset>` files at `/sitemap-1.xml`, `/sitemap-2.xml`, … (10,000 URLs per page). Use for sites
  **at or over 50,000 URLs** (the protocol's per-file limit is 50,000 URLs / 50 MB).

Register only one of them — never both (they'd collide on `/sitemap.xml`). The other two files are
shared by either choice:

- `SitemapComposer.cs` — registers the notification handlers.
- `SitemapCacheInvalidator.cs` — clears the cache on content changes (handles either controller's
  cache key).

## Step 1 — Discover project context

Search for:
- Root namespace (`.csproj` or any `.cs` file).
- Umbraco version (`<PackageReference Include="Umbraco.Cms"` in `.csproj`) — must be 17+.
- Whether the Delivery API is enabled or a separate front-end framework is present (signals
  headless).
- Any existing boolean "hide from sitemap" property on a Document Type or composition.
- Folder conventions — look for `Controllers/`, `Composers/`, `Core/`, `Infrastructure/` to
  decide where each file belongs; follow the project pattern, fall back to `Controllers/` for
  the controller and project root for the rest. In flat projects with no subfolders, create
  the files at the project root but **still use `<RootNamespace>.Controllers` as the
  controller namespace** — the `.Controllers` suffix is a namespace convention, not tied to
  whether a physical folder exists.

Ask before writing:

1. **Roughly how many pages?** — under 50,000 → `SitemapController.cs`; 50,000+ (or expected to
   grow there) → `SitemapIndexController.cs`. If unsure, default to the single-file controller.
2. **Headless?** — if yes, ask for the public front-end base URL.
3. **Filter property alias** — the boolean property to exclude pages (leave blank for none).

## Step 2 — Write the files

Write the **chosen** controller (single-file or index — not both), plus `SitemapComposer.cs` and
`SitemapCacheInvalidator.cs`. Place each file per the folder conventions found in Step 1. Replace
in every file:
- `<Namespace>` → confirmed namespace (controller uses `<Namespace>.Controllers`, others use
  `<Namespace>`).
- `<filterAlias>` → confirmed alias; if none, remove the filter `.Where()` line entirely.

For headless, also write `SitemapSettings.cs` as described below.

If a real `.csproj` is present, run `dotnet build` after writing the files and fix any
compile errors before finishing. If no buildable project exists (e.g. a sandbox with no
solution to build against), say so explicitly and state that the code compiles against the
documented Umbraco 17 APIs it uses — don't stay silent on build correctness either way.

## Headless

For headless setups where the Umbraco host and public front-end domain differ, the standard
`Url(mode: UrlMode.Absolute)` returns the wrong host in `<loc>` entries. Fix this by adding an
extra file, `SitemapSettings.cs`, alongside the composer:

- A simple POCO with a `BaseUrl` string property and a `const string SectionName = "Sitemap"`.
- Register it in the composer:
  `builder.Services.Configure<SitemapSettings>(builder.Config.GetSection(SitemapSettings.SectionName))`.
- Inject `IOptions<SitemapSettings>` into the chosen controller and use `BaseUrl` to replace the
  host on each `<loc>` URL. With `SitemapIndexController`, also build the index/page `<loc>` URLs
  from `BaseUrl` (e.g. `{BaseUrl}/sitemap-{page}.xml`) instead of `Request.Scheme`/`Request.Host`.
- Instruct the user to add `"Sitemap": { "BaseUrl": "https://www.mysite.com" }` to
  `appsettings.json`.

For traditional Umbraco none of this is needed — URLs come from Culture & Hostnames on the
root node.

## Done

Tell the user:
- Sitemap is at `/sitemap.xml` (the split version also exposes `/sitemap-1.xml`, `/sitemap-2.xml`,
  … which the index points at — you only submit `/sitemap.xml`).
- Cache invalidates automatically on content changes.
- Add `Sitemap: https://yoursite.com/sitemap.xml` to `wwwroot/robots.txt`.
- Traditional: requires a domain in **Culture & Hostnames** on the root node for absolute
  URLs.
- Headless: set `Sitemap:BaseUrl` in `appsettings.json` to the public front-end domain.
