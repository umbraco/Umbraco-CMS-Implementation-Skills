# Approach A — Built-in routing (no code)

Change URLs with what Umbraco already reads: four reserved property aliases on pages, a handful of
settings in `appsettings.json`, automatic redirect tracking, and ASP.NET Core rewrite rules. Choose
this whenever the rule can be stated per page or site-wide. If the URL has to be computed from data,
or a URL pattern has no page behind it, use [custom routing code](custom-routing.md) instead.

## Building blocks (fetch the docs before implementing)

- **Reserved property aliases:** [Special Property Type Aliases for Routing](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/routing-properties.md)
- **Automatic 301s:** [URL Redirect Management](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/url-tracking.md)
- **Settings:** [Web Routing](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/webroutingsettings.md),
  [Request Handler](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/requesthandlersettings.md),
  [Global](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/globalsettings.md)
- **Rewrite rules:** [URL Rewrites in Umbraco](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/iisrewriterules.md)

## Docs corrections (Umbraco 17)

| Docs say | Problem | Use instead |
|---|---|---|
| `WebRouting:DisableFindContentByIdPath` | No such setting on 17; the key is silently ignored | `DisableFindContentByIdentifierPath` |
| `/1234` finds the page with id 1234 | 17 finds pages by **key**: `/{guid}` | Expect `/{guid}` to route; disable it with the setting above |
| `HideTopLevelNodeFromPath`: "if you are running multiple sites, you don't want the top level node in your URL" | It hides the root from **every** URL; with several roots and no domains, their children's URLs collide | Give each root a domain (`umbraco-multilingual` covers Culture and Hostnames) |

## The reserved properties

Shipped as a composition in [`../assets/routing-properties-package.xml`](../assets/routing-properties-package.xml).
The **alias and the property editor** both matter; Umbraco reads these by alias from any page that has them.

| Alias | Editor | Effect | Notes |
|---|---|---|---|
| `umbracoUrlName` | Textstring | Replaces this page's segment | One segment: no `/`. Descendants' URLs change too. Varies by culture if the property does. |
| `umbracoUrlAlias` | Textstring | Extra paths render this page in place (no redirect) | Comma-separated, no domain: `offers,promo/spring`. |
| `umbracoRedirect` | **Content Picker** | **302** to the picked page | The router reads an id or a document UDI. A Multi URL Picker value is neither, so nothing happens. Use rewrite rules for a 301 or an external URL. |
| `umbracoInternalRedirectId` | **Content Picker** | Renders the picked page at this URL (a rewrite) | The picked page's template renders, not this page's. |

Steps:

1. **Discover:** which Document Types need which properties (rarely all four on every type), and
   whether the Umbraco Developer MCP is connected.
2. **Create the properties** as a composition, using the XML as the spec: names, aliases, editors.
   The 17 backoffice has no package upload, so build it with the MCP or give the user one backoffice
   step at a time (Settings → Document Types → create a composition, add each property, unlock and
   type the alias). Only a developer can install the XML directly, through a package migration.
   Leave out the properties the site doesn't need, then add the composition to the page types.
3. **Publish** a page with a value set. Segment and alias changes apply when the page is published.
4. **Verify** each URL with a request, see *Verify* below.

## Automatic redirects for renames and moves

When a published page's URL changes (rename, move, `umbracoUrlName`, or a republish after a segment
provider's logic changed), Umbraco records the old URL for it and every descendant, and answers it
with a **301** to the new URL. Editors manage them in Content → Redirect URL Management. Don't write
redirects for these. Deploying a new segment provider records none: see
[custom-routing.md](custom-routing.md).

It doesn't cover URLs Umbraco never generated, such as an old site's paths. For those use rewrite
rules (pattern-based) or a content finder (data-based, see [custom-routing.md](custom-routing.md)).

## Settings

All under `Umbraco:CMS` in `appsettings.json`. Defaults shown.

| Setting | Default | Use it to |
|---|---|---|
| `Global:HideTopLevelNodeFromPath` | `true` | Keep the root's own segment out of URLs (`/about/`, not `/home/about/`). |
| `RequestHandler:AddTrailingSlash` | `true` | Set `false` to generate `/about` instead of `/about/`. Both forms still route; add a rewrite rule to redirect one to the other. |
| `RequestHandler:ConvertUrlsToAscii`, `UserDefinedCharCollection` | `try`, built-in list | Control how names become segments (`æ`, `+`, `&`). Changes apply as pages are republished. |
| `WebRouting:UrlProviderMode` | `Auto` | Force `Absolute` or `Relative` URLs from `Url()`. |
| `WebRouting:UseStrictDomainMatching` | `false` | Stop routing requests on hostnames not assigned to a site (e.g. the `*.azurewebsites.net` name). |
| `WebRouting:DisableAlternativeTemplates` | `false` | Stop `?altTemplate=alias` from switching a page's template. |
| `WebRouting:DisableFindContentByIdentifierPath` | `false` | Stop `/{guid}` from rendering a page. `?umbPageID=1234` has no setting: remove `ContentFinderByPageIdQuery` in a composer. |
| `WebRouting:DisableRedirectUrlTracking` | `false` | Turn off the automatic 301s above. |
| `WebRouting:InternalRedirectPreservesTemplate` | `false` | Keep the alt template across an `umbracoInternalRedirectId`. |

## Rewrite rules

For site-wide or pattern redirects (https, www, trailing slash, `.aspx` URLs, a whole old section),
use ASP.NET Core's URL Rewriting Middleware: `app.UseRewriter(...)` in `Program.cs` **before**
`app.UseUmbraco()`. The doc page has the rule examples. Two things to keep:
- Exclude `/umbraco` (the backoffice) from every rule, as the doc's examples do.
- Use the `...Permanent()` shortcuts (`AddRedirectToHttpsPermanent()`, `AddRedirectToWwwPermanent()`)
  or `AddRedirect(regex, replacement, 301)` for moves that are permanent: the plain shortcuts and
  `AddRedirect` send temporary redirects.

## Verify

Request each changed URL and check the status and which page rendered: the new segment answers 200,
the old segment 404 (or 301 if the page was already published under it), each alias 200,
`umbracoRedirect` 302 with the right `Location`.

## Done

Tell the user which properties were added to which Document Types, what each editor field does, any
settings changed, and the URLs to test.
