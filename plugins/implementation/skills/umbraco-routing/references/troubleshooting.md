# Troubleshooting routing

Find the symptom, check the cause in order, and confirm the fix with a request. How the pipeline
runs, step by step, is in [Routing in Umbraco](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/request-pipeline.md)
and its inbound and outbound pages.

| Symptom | Likely cause | Fix |
|---|---|---|
| A page's URL shows `#err-1234` or the backoffice Info tab lists a collision | Two published pages produce the same path: siblings with the same name or `umbracoUrlName`, or several roots with no domains while `HideTopLevelNodeFromPath` is `true` | Rename or set a distinct `umbracoUrlName`; give each root a domain (Culture and Hostnames). The first page in sort order keeps the URL. |
| Old URLs 404 after deploying a segment provider | The boot-time rebuild changed every URL and recorded no redirects | A content finder that 301s old URLs to new |
| Page keeps its old URL after changing a provider's logic, `umbracoUrlName` or the char replacements | Segments are stored at publish; no rebuild was triggered, or the new segment collides | Publish with descendants; check `Url()` for `#err` |
| New URL works, old URL 404s | Redirect tracking only records changes to pages that were already published, and only with `DisableRedirectUrlTracking: false` | Set `DisableRedirectUrlTracking: false` (it only covers later changes); for old URLs Umbraco never recorded (unpublished at the time, or from a previous site), add rewrite rules or a content finder |
| Custom `IUrlProvider` URLs 404 | Outbound changed, inbound didn't | Add a content finder for those URLs, or switch to an `IUrlSegmentProvider` |
| Content finder never runs | Not registered, or an earlier finder already matched (a real page or alias at that path), or the path has a file extension | Register in a composer; check the URL isn't a page; see *File extensions* in [custom-routing.md](custom-routing.md) |
| Content finder runs for every request and slows the site | Inserted before `ContentFinderByUrlNew`, or no early prefix check | `Append<T>()` and return `false` on the first line for other paths |
| Virtual-page route returns a bare 404 | `FindContent` returned `null`: wrong Document Type alias, node unpublished, or a hardcoded id from another environment | Find the node by alias; publish it |
| Virtual-page or custom route returns 500 "Wasn't able to get an UmbracoContext" | Path has a file extension | `UmbracoRequestOptions.HandleAsServerSideRequest` for that path |
| `umbracoRedirect` / `umbracoInternalRedirectId` does nothing | Property isn't a Content Picker (e.g. Multi URL Picker), alias misspelled, or the target isn't published | Use a Content Picker with the exact alias; publish the target |
| `umbracoUrlAlias` path 404s | Page not republished since the alias was set, a domain in the value, or a real page at that path (pages match before aliases) | Paths only (`promo/spring`, leading and trailing `/` are ignored), republish, check for clashes |
| A page renders by id or key (`/{guid}`, `?umbPageID=1234`) | `ContentFinderByKeyPath` and `ContentFinderByPageIdQuery`, both on by default; `/1234` no longer routes on 17 | `WebRouting:DisableFindContentByIdentifierPath: true` for `/{guid}`; `builder.ContentFinders().Remove<ContentFinderByPageIdQuery>()` for `?umbPageID=`. The Delivery API serves items by id separately. |
| Wrong site's page on a multi-site install | Request host has no domain assigned, so Umbraco matches the path without a site | Assign domains; `UseStrictDomainMatching: true` stops routing unassigned hosts |
| Headless front end can't resolve a path | The Delivery API doesn't use content finders | Implement `IApiContentPathResolver` (not covered by this skill) |

## Check what Umbraco thinks

- **The page's URLs:** Content → the page → Info tab lists every URL and alias, or flags a collision.
- **Old URLs:** Content → Redirect URL Management lists recorded 301s.
- **A request:** with `Umbraco.Cms.Core.Routing` logging at `Debug`, the log shows each finder tried
  and which matched.
