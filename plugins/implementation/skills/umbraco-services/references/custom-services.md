# Custom services

Writing your own service or extension method, choosing its lifetime, registering it. Which built-in services to inject: [using-built-in-services.md](using-built-in-services.md).

**Answer from this file and the matching asset(s).** Both are verified (compiled on 17.5.3 + 18.2.0, runtime-tested on 17.5.3). Fetch a doc below when you need an API or case not shown here or in the asset; say if something stays unverified.

Docs (v18 links; for v17 insert `/17.latest` after `/umbraco-cms`):
[services.md](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services.md) (*Customizing Services and Helpers*) ·
[composing.md](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md) ·
[circular-dependencies.md](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services/circular-dependencies.md)

## Extension method or service

| Choose | When |
|---|---|
| Extension method on `IPublishedContentQuery` / `UmbracoHelper` | Small stateless lookup needing only the extended object (e.g. news section under root). No registration. |
| Service (interface + class + composer) | Needs other dependencies, testable/swappable logic, or must also run **outside** a request. |

## Lifetime = from dependencies

| Service injects | Register | Open asset |
|---|---|---|
| Anything scoped (`IPublishedContentQuery`, `UmbracoHelper`) | `AddScoped`; request-only (controllers, views) | [`SiteService.cs`](../assets/SiteService.cs) + [`ISiteService.cs`](../assets/ISiteService.cs) |
| Only singletons; reads via `IPublishedContentCache` or `IUmbracoContextFactory` | `AddSingleton`; works in requests **and** background work (URLs need the factory's context) | [`RootContentReader.cs`](../assets/RootContentReader.cs) |
| `IPublishedContentQueryAccessor` (singleton), `TryGetValue` per use | `AddSingleton`; `TryGetValue` resolves the query from the current request's services (`HttpContext.RequestServices`), so it returns false in jobs and hosted services | — (v18 docs pattern; accessor exists on 17) |
| App-wide state | `AddSingleton`, thread-safe | `UnpublishedUrlStore` in [`UnpublishingUrlRecorder.cs`](../assets/UnpublishingUrlRecorder.cs) |
| Nothing stateful or expensive | `AddTransient` (new per injection) | — |

Wrong-lifetime symptoms: startup *"Cannot consume scoped service … from singleton"* (scope validation on, e.g. Development); with validation off, one request's data leaks into others (same bug family as `UmbracoHelper` statics → `umbraco-common-pitfalls`).

## Registration

One `IComposer` per feature: `builder.Services.Add…` for services, `builder.AddNotificationHandler<TNotification, THandler>()` for handlers → [`SiteServiceComposer.cs`](../assets/SiteServiceComposer.cs).
Composer order, `Program.cs` registration, replacing a built-in service or a collection item: sibling skill [`umbraco-composing`](../../umbraco-composing/SKILL.md).
Circular dependency at startup (often `IUmbracoContextFactory` + third-party code): inject `Lazy<IUmbracoContextFactory>`, use `.Value` when needed.

## Using it

Controller: constructor injection. View: `@inject ISiteService SiteService` (view itself → `umbraco-templating`).

## Steps

1. Version from `Umbraco.Cms` package.
2. Extension method vs service; for a service, list dependencies → lifetime row.
3. Open that row's asset (and `SiteServiceComposer.cs` for registration); replace `<Namespace>` (`<routePrefix>` for the example controller). Return `IPublishedContent?`, null-check callers.
4. Build; request a page/endpoint using it if a site runs, else say unverified.

**Done:** per class, lifetime and why, where registered, how to call from controller/view.
