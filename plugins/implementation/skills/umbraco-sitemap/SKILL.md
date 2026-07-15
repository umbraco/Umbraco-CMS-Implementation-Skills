---
name: umbraco-sitemap
description: >
  Add an XML sitemap to an Umbraco 17+ site. Offers two approaches: the official docs
  Razor-template + Document Type approach, and a custom cached API controller that needs
  no backoffice setup. Use this skill whenever the user asks to add, create, build,
  implement, or enable an XML sitemap for an Umbraco site, including related goals like
  "improve SEO", "let search engines index the site", or "set up robots.txt".
  SKIP: non-Umbraco projects or Umbraco < 17.
---

# Sitemap

Two supported ways to add an XML sitemap:

| | **A — Cached controller** (default) | **B — Razor template** |
|---|---|---|
| Reference | [approach-a-cached-controller.md](references/approach-a-cached-controller.md) | [approach-b-razor-template.md](references/approach-b-razor-template.md) |
| Source | Custom code from documented building blocks | Official [tutorial](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/creating-an-xml-site-map.md), fetched live (no local copy) |
| Backoffice work | None — file-based only | Required: Document Types, composition, content node |
| Sitemap URL | Fixed `/sitemap.xml` | The XmlSiteMap content node (e.g. `/xmlsitemap`) |
| Caching | In-memory, auto-invalidated | None — rendered per request |
| Best for | Headless/Delivery-API sites, no backoffice access | Traditional Razor sites, per-page editor control |

### How to decide

Default to A. Choose B when the team wants the official-docs approach or per-page editor
control (priority / change frequency / hide). For B's backoffice steps, prefer the
[Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp); if
unavailable, walk the user through the steps manually in the backoffice — don't drop to A
just because the MCP is missing. Only fall back to A if backoffice access isn't possible at
all, or the tutorial can't be fetched.

If ambiguous, briefly offer both and recommend A.

## Version compatibility

Both target **Umbraco 17+**. Approach A uses `IDocumentNavigationQueryService` (Umbraco 15+).

## Validation

Assertions for both approaches live in [`evals/evals.json`](evals/evals.json); run them with the `umbraco-skill-evaluator` skill.
