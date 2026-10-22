---
name: umbraco-routing
description: >
  Front-end URL routing for Umbraco 17+ sites: how page URLs are built and matched, and how to
  change either. Covers the no-code options (umbracoUrlName, umbracoUrlAlias, umbracoRedirect,
  umbracoInternalRedirectId, automatic 301s, HideTopLevelNodeFromPath, AddTrailingSlash,
  WebRouting settings, rewrite rules) and routing code (IUrlSegmentProvider, IContentFinder,
  IUrlProvider, IVirtualPageController routes). Also diagnoses #err- URLs, URL collisions, pages
  reachable by id, and old URLs that 404. Trigger: "change a page's URL", "put the
  SKU in product URLs", "vanity URL", "short link", "redirect old URLs", "keep old .html links
  working", "remove the trailing slash", "map /x/{id} to a page", "custom route with the site
  layout", "write a content finder". SKIP: hijacking a Document Type's route or surface
  controllers (umbraco-templating), 404 pages (umbraco-custom-error-pages), Culture and Hostnames
  (umbraco-multilingual), sitemaps (umbraco-sitemap), headless/Delivery API, non-Umbraco or
  Umbraco < 17.
---

# Routing

Umbraco routes in two directions. **Outbound**, it builds each page's URL from the tree: a segment
per node, joined into a path, turned into a URL. **Inbound**, it matches a request back to a node:
domain, then content finders in order, then redirects, then template and controller.

| | **A — Built-in routing** (default) | **B — Custom routing code** |
|---|---|---|
| Reference | [built-in-routing.md](references/built-in-routing.md) | [custom-routing.md](references/custom-routing.md) |
| Ships | [`routing-properties-package.xml`](assets/routing-properties-package.xml) | `SkuUrlSegmentProvider.cs`, `SkuShortLinkContentFinder.cs`, `RoutingComposer.cs`, `StoreFinderController.cs`, `storeFinderCity.cshtml` |
| Work | Properties on Document Types, `appsettings.json`, rewrite rules | C# registered in a composer |

## Pick the building block

| Need | Use | Approach |
|---|---|---|
| Editor sets a page's slug | `umbracoUrlName` | A |
| Extra URLs that show a page (campaign, vanity) | `umbracoUrlAlias` | A |
| Page that sends visitors to another page | `umbracoRedirect` | A |
| Same content shown at a second place in the tree | `umbracoInternalRedirectId` | A |
| Old URLs keep working after a rename or move | Built-in redirect tracking (automatic 301) | A |
| Trailing slash, `/home` in URLs, https/www, pattern redirects | Config and rewrite rules | A |
| Segment derived from data (SKU, date, code) for one Document Type | `IUrlSegmentProvider` | B |
| URL pattern with no node behind it, answered by a node (short links, legacy ids) | `IContentFinder` | B |
| URL pattern rendered by your controller with the site's layout and a `CurrentPage` | `IVirtualPageController` route | B |
| Whole URL differs from the tree, not just one segment | `IUrlProvider` plus a matching `IContentFinder` | B |
| Something routes wrong: `#err-1234`, a 404 after a change, the wrong page | [troubleshooting.md](references/troubleshooting.md) | — |

### How to decide

Default to A: it needs no deployment and editors can see and change it. Reach for B only when a
built-in can't express the rule. Within B, prefer `IUrlSegmentProvider` over `IUrlProvider`: Umbraco
stores the segment and routes it inbound by itself, while an `IUrlProvider` needs a content finder to
match every URL it invents. A missing Umbraco Developer MCP is no reason to pick B:
[built-in-routing.md](references/built-in-routing.md) covers the backoffice steps without it.

**Open the reference before writing anything.** Several Umbraco 17 behaviours that break routing
are only covered there, including a config key the docs misname.

## Version compatibility

Targets **Umbraco 17+**; the assets compile and serve on `Umbraco.Cms` 17.5.3. Since 15, Umbraco stores each
page's URL segment when it is published; on 17.5, `umbracoUrlAlias` values are stored the same way. Umbraco 13
and earlier route differently (`IPublishedContentCache.GetByRoute`, `ContentFinderByIdPath`); don't
apply this skill there.

## Best practices

- **Outbound and inbound must agree.** A URL Umbraco generates must route back to the same page, or
  it 404s. Test both: the page's `Url()` and a request to that URL.
- **One canonical URL per page.** Short links and old URLs should 301 to it, not render a copy.
  Aliases render in place, so add a canonical link tag when the page also has aliases.
- **Never hardcode node ids.** Find nodes by Document Type alias or by key; ids differ per
  environment.
- **Redirects follow publishes, not deploys.** Umbraco records a 301 when an editor's publish or
  move changes a URL, so don't hand-roll those. A deploy that changes URLs (a new segment provider)
  records none: published pages' old URLs need redirects from you.
- **Report what you verified.** Request the URLs you changed. If no runnable site is available, say
  so rather than claiming the routing works.

## Validation

Assertions live in [`evals/evals.json`](evals/evals.json); run them with `umbraco-skill-evaluator`.
[`evals/README.md`](evals/README.md) has the project fixture, the run-prompt rules and the results.
Both approaches are **Asserted** by the runtime gate on the blank host:
- `examples/built-in-routing/`: the composition's four routing properties, each over HTTP;
- `examples/custom-routing/`: the segment provider, the short-link finder and the virtual route.
