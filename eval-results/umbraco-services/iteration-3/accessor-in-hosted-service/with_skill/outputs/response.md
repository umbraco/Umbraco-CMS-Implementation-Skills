# Why `IPublishedContentQueryAccessor` returns false in your nightly job

## What's going on

`IPublishedContentQueryAccessor` doesn't hold a query object of its own. It looks up the **scoped** `IPublishedContentQuery` from the current HTTP request's service provider. Here's the Umbraco 17.5.3 source:

```csharp
// src/Umbraco.Infrastructure/PublishedContentQueryAccessor.cs
public bool TryGetValue(out IPublishedContentQuery publishedContentQuery)
{
    publishedContentQuery = _scopedServiceProvider.ServiceProvider?.GetService<IPublishedContentQuery>();
    return publishedContentQuery is not null;
}

// src/Umbraco.Web.Common/DependencyInjection/ScopedServiceProvider.cs
public IServiceProvider? ServiceProvider => _accessor.HttpContext?.RequestServices;
```

So the accessor only works while an `HttpContext` exists:

- **From a controller (daytime):** a request is in progress, `HttpContext.RequestServices` exists, and `TryGetValue` returns `true`.
- **From your `IHostedService` (night):** no request exists, `HttpContext` is `null`, and `TryGetValue` always returns `false`. This has nothing to do with the time of day. The difference is that one call runs inside a request and the other doesn't.

DI accepted the setup because the accessor is a singleton. That only tells you the service can be constructed. It doesn't tell you it can return anything outside a request. The v17 docs for `IPublishedContentQuery` say so directly: *"In a background task, accessing the content cache using an injected `IPublishedContentQuery` or `IPublishedContentQueryAccessor` will not work, as they rely on `HttpContext`."* The accessor exists for singletons that run **during** a request. It doesn't help when there is no request.

You were right not to use `IContentService` for this. It's a management service, and every read takes a lock and queries the database. Keep reading from the published cache, just through a different service.

## The right fix: `IUmbracoContextFactory`, one context per run

When there's no HTTP request, inject the **singleton** `IUmbracoContextFactory` and create a short-lived Umbraco context with `EnsureUmbracoContext()` inside a `using` block. Read the published cache through `UmbracoContext.Content`. Use `IDocumentNavigationQueryService` (also a singleton, held in memory) to walk the tree without touching the database.

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public record DigestItem(Guid Key, string Name, DateTime CreateDate);

// Singleton-safe: holds only singletons. No UmbracoContext or IPublishedContent is kept in fields.
public class NewsDigestReader
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;

    public NewsDigestReader(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
    }

    public IReadOnlyList<DigestItem> GetLatestNews(int count = 5)
    {
        // Creates a context when none exists (background thread) and disposes it afterwards.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = contextReference.UmbracoContext.Content;
        if (cache is null || !_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return [];
        }

        // Find the published "News" node under a root. Navigation also contains unpublished nodes,
        // so a null from GetById means "not published", which we skip.
        IPublishedContent? news = rootKeys
            .SelectMany(rootKey => _navigation.TryGetChildrenKeys(rootKey, out var keys) ? keys : [])
            .Select(key => cache.GetById(key))
            .FirstOrDefault(c => c is not null && c.Name == "News");
        // Better: match on the document type alias (TryGetChildrenKeysOfType(rootKey, "newsIndex", ...))
        // or store the News node's key in config. Editors can rename a node.

        if (news is null || !_navigation.TryGetChildrenKeys(news.Key, out IEnumerable<Guid> articleKeys))
        {
            return [];
        }

        // Project to plain data before the context is disposed. Don't return IPublishedContent.
        return articleKeys
            .Select(key => cache.GetById(key))
            .OfType<IPublishedContent>()          // published only
            .OrderByDescending(a => a.CreateDate) // or your own publish-date property
            .Take(count)
            .Select(a => new DigestItem(a.Key, a.Name, a.CreateDate))
            .ToList();
    }
}
```

Register it as a singleton. Its lifetime follows from its dependencies, and both are singletons:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class NewsDigestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<NewsDigestReader>();
        builder.Services.AddHostedService<NightlyDigestService>(); // your existing hosted service
    }
}
```

In the hosted service, inject `NewsDigestReader` instead of `IPublishedContentQueryAccessor` and call `GetLatestNews(5)` on each nightly run. That's the only change the hosted service needs.

### Rules to keep

- **Create one `EnsureUmbracoContext()` per run, inside a `using` block.** Never store the `UmbracoContext`, the reference, or any `IPublishedContent` in a field of the singleton. Turn the content into plain data (as `DigestItem` does) before the context is disposed.
- **Don't fall back to `IContentService`** for reading. Keep it for jobs that *change* content.
- **Don't resolve `IPublishedContentQuery` from the root provider.** It's scoped, so a singleton that asks for it directly fails with *"Cannot resolve scoped service … from root provider"*. The context factory is the supported way to read content when there's no request.
- The same symptom has the same cause elsewhere: `IUmbracoContextAccessor.TryGetUmbracoContext` and `UmbracoHelper` also find nothing in a background job.

### Optional, not verified here

If the site ever runs on more than one server, a plain `IHostedService` runs on **every** instance. Umbraco's own background-job abstraction (`IRecurringBackgroundJob`, which can be limited by server role) is worth a look so the digest goes out only once. Check the Umbraco 17 *Scheduling* docs before adopting it. I haven't checked it for this answer.

## Verification status

- **Root cause:** confirmed in the Umbraco 17.5.3 source (`PublishedContentQueryAccessor` → `ScopedServiceProvider` → `IHttpContextAccessor.HttpContext?.RequestServices`) and in the v17 `IPublishedContentQuery` docs.
- **The fix pattern** (singleton + `IUmbracoContextFactory` + `IDocumentNavigationQueryService`, with a disposed context per read) matches a pattern that compiles on 17.5.3 and has been tested at runtime with no HTTP request. The `TryGetChildrenKeys`/`TryGetChildrenKeysOfType` signatures were checked against the 17.5.3 `INavigationQueryService`.
- **The digest code above was written for this answer and has not been compiled or run against your site.** Build it and trigger one run (for example with a short delay on startup) before relying on it.
