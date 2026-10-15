# Why `SiteService` won't start, and which fix to pick

## What's wrong

The problem is the service's **lifetime**, not its code.

- `UmbracoHelper` (and `IPublishedContentQuery`) is **scoped**: Umbraco creates one per HTTP request. The Umbraco 17 docs say both are "scoped to the current `HttpRequest`", and that injecting one into a class resolved outside a request, such as a singleton, makes Umbraco report an error at startup.
- You registered `SiteService` with `AddSingleton`. A singleton is created **once** for the whole application, so it would hold on to a single `UmbracoHelper` for good.
- **Why only Development fails:** ASP.NET Core turns on scope validation (`ValidateScopes`) by default only in the Development environment. That check finds the singleton → scoped dependency at startup and throws `Cannot consume scoped service ... from singleton`.
- **Production isn't fine, just quiet.** With validation off, the container still builds the singleton once and gives it whatever `UmbracoHelper` it resolved first. Every later request then reuses that one helper and the context it was tied to. You get data from the wrong request, stale results, or errors under load. The Development error is warning you about a real bug that is also in production.

The rule: **a service's lifetime follows its dependencies.** If it injects anything scoped, it must be scoped too, or it must stop injecting that scoped thing.

## The two fixes

| | Fix A: make it scoped | Fix B: singleton + `IUmbracoContextFactory` |
|---|---|---|
| Change | `AddScoped<ISiteService, SiteService>()` (and inject `IPublishedContentQuery` rather than the whole `UmbracoHelper`) | Inject only singletons and open a short-lived Umbraco context for each lookup |
| Works in controllers, views, view components | Yes | Yes |
| Works in a scheduled job, hosted service or notification handler | **No.** There's no request and no `UmbracoContext` there | **Yes** |

**Because you'll call it from a scheduled job, Fix B is the right one.** Fix A would make the startup error go away, but the service could still only be used during a request, so you'd hit the problem again when you add the job. The Umbraco 17 docs give the same pattern for code that runs outside a request: `IUmbracoContextFactory.EnsureUmbracoContext()` in a `using`, plus `IDocumentNavigationQueryService` for tree lookups. Both are singletons, so a singleton can safely inject them.

## Fix B: the code

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public interface ISiteService
{
    IPublishedContent? GetContactPage();
    IPublishedContent? GetNewsSection();
}

// SINGLETON: injects only singletons, so it works in requests AND in background work.
public class SiteService : ISiteService
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
    }

    // Replace with your Document Type aliases.
    public IPublishedContent? GetContactPage() => FindFirstOfType("contactPage");
    public IPublishedContent? GetNewsSection() => FindFirstOfType("newsSection");

    private IPublishedContent? FindFirstOfType(string contentTypeAlias)
    {
        // Inside a request this reuses the current context; in a job it creates one.
        // Always in a `using`; never store the context in a field.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // The navigation service keeps the tree structure in memory, so this doesn't query the database.
        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        IPublishedContent? root = rootKeys
            .Select(key => contextReference.UmbracoContext.Content?.GetById(key))
            .FirstOrDefault(content => content is not null);
        if (root is null)
        {
            return null;
        }

        if (!_navigation.TryGetDescendantsKeysOfType(root.Key, contentTypeAlias, out IEnumerable<Guid> keys))
        {
            return null;
        }

        return keys
            .Select(key => contextReference.UmbracoContext.Content?.GetById(key))
            .FirstOrDefault(content => content is not null);
    }
}
```

Registration in your composer:

```csharp
public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<ISiteService, SiteService>();
}
```

The `AddSingleton` call you already have is correct now that the class has no scoped dependencies.

### Using it

- **Controller / view:** inject `ISiteService` through the constructor, or use `@inject ISiteService SiteService` in a view, and null-check the result.
- **Scheduled job:** inject `ISiteService` into the job's constructor, which is allowed because the job is also a singleton. In the job, read what you need, such as the name, URL or key, right away. Don't cache the returned `IPublishedContent` in a field. It's a snapshot of the cache, so it goes stale after the next publish.

### Notes

- **Multi-site:** `TryGetRootKeys` returns every root, and the code above takes the first. If you host more than one site, choose the root for the current domain or pass it in.
- **Circular dependency at startup:** if you ever see one involving `IUmbracoContextFactory` (often caused by third-party code), inject `Lazy<IUmbracoContextFactory>` and use `.Value` only when you need it.
- **If the service never needs to run outside a request:** Fix A is the simpler option. Use `AddScoped`, inject `IPublishedContentQuery`, and use `ContentAtRoot()`. You've said you'll need the job, though.

## Verification status

- The pattern (a singleton with `IUmbracoContextFactory`, `EnsureUmbracoContext()` in a `using`, and `IDocumentNavigationQueryService.TryGetRootKeys` + `GetById`) is the same as a sample that is compiled and run against Umbraco 17.5.3 in this skill's test gate.
- The `TryGetDescendantsKeysOfType` lookup, the `contactPage`/`newsSection` aliases and the exact class above were **not built or run**, because there was no project to build against. Build it in your solution and call both methods from a page and from the job before relying on it.

Sources: Umbraco 17 docs, *Services and Helpers* (`docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services`); ASP.NET Core default host behaviour (scope validation in Development only).
