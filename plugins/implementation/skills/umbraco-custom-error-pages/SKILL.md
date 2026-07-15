---
name: umbraco-custom-error-pages
description: >
  Implement custom 404 and 500 error pages in Umbraco 16.1+. Offers two 404 approaches: a
  multi-site-aware IContentLastChanceFinder (default) and the official docs' zero-code
  Error404Collection config; 500s use a shared MVC controller. Trigger: user asks to "add
  custom error pages", "set up 404 page", "handle 500 errors", "create page not found",
  "implement error handling", "customize error pages in Umbraco". SKIP: non-Umbraco projects,
  and maintenance/upgrade pages (use umbraco-custom-maintenance-page instead).
---

# Custom Error Pages

Covers all three scenarios from the official tutorial: **404s have two supported approaches;
500s have exactly one** (unhandled exceptions bypass Umbraco's routing pipeline, so only a
plain MVC controller works — see
[500-error-controller.md](references/500-error-controller.md), shared by both approaches);
**Boot Failed has one trivial method** (see below).

Looking for a **maintenance page** (shown during Umbraco upgrades)? That's not an error page —
use the separate `umbraco-custom-maintenance-page` skill instead.

## Choose a 404 approach

| | **A — Content finder** (default) | **B — Config-based** |
|---|---|---|
| Reference | [approach-a-content-finder.md](references/approach-a-content-finder.md) | [approach-b-config-based.md](references/approach-b-config-based.md) |
| Source | Custom improvement on the docs' "Advanced" sample | Official [tutorial](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page)'s "Recommended" method, fetched live (no local copy) |
| Custom code | One `IContentLastChanceFinder` + composer (local asset) | None — backoffice + `appsettings.json` only |
| Multi-site (per domain) | Yes — resolves root via `request.Domain` | No — per-culture only |
| Per-environment config | None — resolves by Document Type alias | 404 page GUID in config per environment |

Default to **A**. Offer **B** when the team wants zero custom C# and doesn't need per-domain
multi-site resolution — it's the docs' officially recommended method, so don't hide it. Both
need a backoffice-created Document Type + published content node either way — prefer the
[Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp) for
those steps if available, else guide the user through the backoffice manually.

Ask the user before choosing: multi-site by domain? comfortable with a small C# file in the
project? Then read the matching reference and follow it. For 500s, always use
[500-error-controller.md](references/500-error-controller.md).

## Boot Failed errors

If the user also wants to handle failed startups (blank screen / `500.30` / `502.5`), there's
one documented method, no code or backoffice needed: add a static
`wwwroot/config/errors/BootFailed.html` with custom markup, and ensure
`Umbraco:CMS:Hosting:Debug` is `false` in `appsettings.json` (the page only shows with
debugging off). Note: if the app crashes before ASP.NET Core's pipeline initializes, only the
web server (IIS/NGINX/Apache) can serve a fallback page — suggest configuring one there for
full coverage. Offer this proactively when setting up 500 pages; don't wait to be asked.

## Version compatibility

Targets **Umbraco 16.1+** (16.0 has a 404-handling regression, fixed in 16.1 — see the
tutorial's warning).

## Documentation references

- [Implement Custom Error Pages](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/custom-error-page) — source of truth for Approach B's steps/snippets and the underlying reference for A and the 500 controller
- [Custom Routing (IContentLastChanceFinder)](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/custom-routing)
