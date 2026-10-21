# Using built-in services

Getting Umbraco services where code runs, and which to read with. Own services: [custom-services.md](custom-services.md).

**Answer from this file and the matching asset(s).** Both are verified (compiled on 17.5.3 + 18.2.0, runtime-tested on 17.5.3). Fetch a doc below when you need an API or case not shown here or in the asset; say if something stays unverified.

Docs (v18 links; for v17 insert `/17.latest` after `/umbraco-cms`):
[services.md](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services.md) ·
[umbracohelper.md](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/umbracohelper.md) ·
[ipublishedcontentquery.md](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/ipublishedcontentquery.md) ·
[management.md](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/management.md) ·
[icontentfinder.md](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/routing/request-pipeline/icontentfinder.md)

## Read vs change

| Doing | Use | Never |
|---|---|---|
| Read published content | Cache: `IPublishedContentQuery`, `UmbracoHelper` (in request), `IPublishedContentCache` (injectable singleton, 15+), `UmbracoContext.Content` via `IUmbracoContextFactory`, `IDocumentNavigationQueryService` (root keys, children) | `IContentService.GetById` etc.: read lock + **DB query every call** (17 and 18) |
| Change content/media/members/relations | `IContentService`, `IMediaService`, `IMemberService`, `IRelationService` | From a view or per request (→ `umbraco-common-pitfalls`) |

## Where the code runs

| Context | Get services by | Open asset |
|---|---|---|
| **Controller** (Render/Surface/plain) | Constructor injection: `IPublishedContentQuery` to read, management services to write. Controller shape → `umbraco-templating` | [`SiteServiceController.cs`](../assets/SiteServiceController.cs) |
| **Notification handler** | Inject management services directly. To read the cache: inject `IUmbracoContextFactory`, `using var r = factory.EnsureUmbracoContext();`. Don't rely on `IUmbracoContextAccessor`: a backoffice save has a context (the request middleware opens one for every non-file request), but scheduled publishing and code in jobs raise the same notifications with none. `EnsureUmbracoContext()` reuses the request's context when there is one | [`UnpublishingUrlRecorder.cs`](../assets/UnpublishingUrlRecorder.cs) + [`SiteServiceComposer.cs`](../assets/SiteServiceComposer.cs) (registration) |
| **No HTTP request** (hosted service, scheduled job) | Singleton (+ `IDocumentNavigationQueryService`). Read via **B** injected `IPublishedContentCache` (no context) or **A** `IUmbracoContextFactory` + `EnsureUmbracoContext()` in `using` per read. **Building URLs needs A**: `IPublishedUrlProvider` throws *"Wasn't able to get an UmbracoContext"* without one. Never store context/content in fields: a read in the constructor is a one-time snapshot from a context disposed right after, so later publishes are never seen | [`RootContentReader.cs`](../assets/RootContentReader.cs) + [`SiteServiceComposer.cs`](../assets/SiteServiceComposer.cs) (registration) |
| **Content finder / URL provider** | Inject `IUmbracoContextAccessor`, `TryGetUmbracoContext` (pipeline has a context). 404 → `umbraco-custom-error-pages` | — |
| **View** | → `umbraco-templating` (`@inject`). No management services in views (→ `umbraco-common-pitfalls`) | — |

No-request symptoms: `TryGetUmbracoContext` returns false; DI throws *"Cannot resolve scoped service … from root provider"* when a singleton asks for `IPublishedContentQuery`/`UmbracoHelper`.

## Culture unpublish (17.6.0+ / 18.1.0+)

`UnpublishedCultures` (per content key) exists on **both** `ContentPublishedNotification` and `ContentUnpublishedNotification`, from 17.6.0 and 18.1.0 (not on 17.5.x or 18.0). Unpublishing **some** languages while others stay live raises `ContentPublishedNotification`, **once per culture**; unpublishing the whole page (or the last/mandatory language) raises `ContentUnpublishedNotification`. To log every culture unpublish, handle both. (Source-verified at `release-18.2.1`, not runtime-tested.)

## Docs corrections

v17 docs (*Accessing Published Content outside of a HTTP Request*) handle **`ContentUnpublishedNotification`** and claim the item is still cached. Wrong: "-ed" fires **after** the page leaves the cache (v17 source); on 17.5.3 the handler runs but `Content.GetById` returns null (verified by the runtime gate). Use **`ContentUnpublishingNotification`** (fires before; v18 docs agree). Its items are in `UnpublishedEntities` (name kept on the "-ing" notification).

## Steps

1. Version from `Umbraco.Cms` package.
2. Pick context (table above) and read vs change.
3. Open that row's asset(s); replace `<Namespace>`.
4. Build; exercise the path if a site runs, else say unverified.

**Done:** per class, the service used and why (cache vs management) and its registration.
