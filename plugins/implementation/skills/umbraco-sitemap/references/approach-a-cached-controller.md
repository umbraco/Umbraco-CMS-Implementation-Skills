# Approach A — Cached API controller (custom, no backoffice)

A `/sitemap.xml` route served by a custom controller, with in-memory caching invalidated on
content changes. **No backoffice configuration required** — it is entirely file-based, so an
AI agent can implement it without the Umbraco Developer MCP.

This assembled controller is **not** in the official docs verbatim. It is composed from
individually-documented building blocks (linked below). Where a building block's behaviour is
in question, the linked docs are the source of truth.

## When to choose this approach

- **Headless / Delivery-API** sites, or any site without Razor templates.
- You want a fixed `/sitemap.xml` route and **caching** out of the box.
- You cannot (or do not want to) make backoffice changes / use the MCP.
- You want everything in source control as `.cs` files.

If the project is a traditional Razor site and the team prefers editor-managed, per-page
sitemap settings that match the official documentation, use
[Approach B](approach-b-razor-template.md) instead.

## Building blocks (documentation references)

Every piece of this approach is individually documented:

- **Composer / `IComposer`** — registering services at startup:
  [Composing](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing)
- **Notification handlers** (`ContentPublishedNotification`, etc.) — reacting to content
  changes:
  [Subscribing to Notifications](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/subscribing-to-notifications)
- **`IPublicAccessService`** — detecting member-protected pages:
  [Umbraco Services](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/umbraco-services)
- **`IDocumentNavigationQueryService`** (Umbraco 15+) — finding content root keys:
  [Services and Helpers](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services)
- **Custom controller routing / route hijacking** — serving a fixed route:
  [Custom Routes](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/routing/custom-routes)

## Version compatibility

Requires **Umbraco 17+**. Uses `IDocumentNavigationQueryService`, introduced in Umbraco 15.

## What the implementation does

- Only published pages appear — unpublished content is never in the published cache.
- Member-protected pages are excluded via `IPublicAccessService`.
- An optional boolean property (e.g. `hideFromSitemap`) lets editors exclude individual
  pages; works whether the property is on a Document Type directly or inherited from a
  composition.
- Results are cached in memory and invalidated on every publish, unpublish, or delete.
- Multi-site aware: matches the request host to the correct root node; falls back to the
  first root in dev.

## The three files

Read the templates in [`../assets/`](../assets) as the source:

- `SitemapController.cs` — serves `/sitemap.xml`, builds and caches the XML.
- `SitemapComposer.cs` — registers the notification handlers.
- `SitemapCacheInvalidator.cs` — clears the cache on content changes.

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

1. **Headless?** — if yes, ask for the public front-end base URL.
2. **Filter property alias** — the boolean property to exclude pages (leave blank for none).

## Step 2 — Write the files

Place each file per the folder conventions found in Step 1. Replace in every file:
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
`Url(mode: UrlMode.Absolute)` returns the wrong host in `<loc>` entries. Fix this by adding a
fourth file, `SitemapSettings.cs`, alongside the composer:

- A simple POCO with a `BaseUrl` string property and a `const string SectionName = "Sitemap"`.
- Register it in the composer:
  `builder.Services.Configure<SitemapSettings>(builder.Config.GetSection(SitemapSettings.SectionName))`.
- Inject `IOptions<SitemapSettings>` into the controller and use `BaseUrl` to replace the host
  on each `<loc>` URL.
- Instruct the user to add `"Sitemap": { "BaseUrl": "https://www.mysite.com" }` to
  `appsettings.json`.

For traditional Umbraco none of this is needed — URLs come from Culture & Hostnames on the
root node.

## Done

Tell the user:
- Sitemap is at `/sitemap.xml`.
- Cache invalidates automatically on content changes.
- Add `Sitemap: https://yoursite.com/sitemap.xml` to `wwwroot/robots.txt`.
- Traditional: requires a domain in **Culture & Hostnames** on the root node for absolute
  URLs.
- Headless: set `Sitemap:BaseUrl` in `appsettings.json` to the public front-end domain.
