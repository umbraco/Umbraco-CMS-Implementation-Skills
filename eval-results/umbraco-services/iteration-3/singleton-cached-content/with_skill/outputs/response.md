# Why the header shows the old News title

## What's wrong

The cause is not caching. The service holds onto one copy of the News page and never fetches it again.

1. **It's a singleton, and the lookup runs in its constructor.** `AddSingleton` builds `SiteService` once per app lifetime, the first time something resolves it. The constructor reads the News section once and saves it in `_newsSection`. After that, `GetNewsSection()` returns the same object on every request until the app restarts. That matches what your editors see.
2. **It stores an `IPublishedContent` in a field.** A published content item is a read-only snapshot of the page as it was when you read it. When an editor publishes, Umbraco refreshes the published cache with a new item. Your field still points at the old snapshot, so the old `Name` stays visible. Even if publishing changed the old object, you'd still have the next problem.
3. **The content outlives its `UmbracoContext`.** `using var reference = EnsureUmbracoContext()` disposes the context at the end of the constructor. The content you read through it is kept long after that. The rule is one disposed context per read, and never store the context or content read from it in a field.
4. **Smaller bugs in the same code:**
   - The `bool` from `TryGetRootKeys` is ignored. `rootKeys.First()` throws if there's no root yet, for example on a fresh install or if the service is first resolved before content exists. Because it's a singleton, that failure then breaks resolving it for good.
   - If the singleton is built when the News page isn't published yet, `_newsSection` stays `null` until a restart.
   - `First()` root isn't domain-aware. On a multi-site install every site gets the first root's News section.

**Rule of thumb:** pick the lifetime from the dependencies, and never cache published content in a long-lived object. Look it up on every call. The published cache is in memory, so this is cheap.

## How to write it

The header runs inside a request, so the simplest correct version is a **scoped** service that uses `IPublishedContentQuery`. If you also need it outside a request (background job, notification handler), use the **singleton** version below.

### Option A (recommended for a site header): scoped, request-only

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace MySite.Services;

public interface ISiteService
{
    IPublishedContent? GetSiteRoot();
    IPublishedContent? GetNewsSection();
}

// Injects scoped IPublishedContentQuery, so it's registered AddScoped.
// Nothing is stored in fields; every call reads the current published cache.
public class SiteService : ISiteService
{
    private readonly IPublishedContentQuery _publishedContentQuery;

    public SiteService(IPublishedContentQuery publishedContentQuery)
        => _publishedContentQuery = publishedContentQuery;

    // Multi-site: pick the root for the current domain instead of the first one.
    public IPublishedContent? GetSiteRoot()
        => _publishedContentQuery.ContentAtRoot().FirstOrDefault();

    public IPublishedContent? GetNewsSection()
        => GetSiteRoot()?.Children().FirstOrDefault(x => x.ContentType.Alias == "newsSection");
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.Services;

namespace MySite;

public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISiteService, SiteService>();
}
```

Use it from a view or partial with `@inject ISiteService SiteService`, or through constructor injection in a controller or view component. Null-check the result:

```cshtml
@inject MySite.Services.ISiteService SiteService
@{ var news = SiteService.GetNewsSection(); }
@if (news is not null) { <a href="@news.Url()">@news.Name</a> }
```

Don't inject a scoped `ISiteService` into a singleton. With scope validation on (Development), startup fails with *"Cannot consume scoped service … from singleton"*. With it off, one request's data leaks into others.

### Option B: singleton that also works outside a request

Keep `AddSingleton` only if the service depends on singletons alone. Inject the factory and navigation service, and open a new, disposed context **inside each method call**:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

// Singleton: depends only on singletons. One disposed UmbracoContext per read.
// Never store the context or content read from it in a field.
public class SiteService : ISiteService
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(IUmbracoContextFactory umbracoContextFactory, IDocumentNavigationQueryService navigation)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
    }

    public IPublishedContent? GetSiteRoot()
    {
        using UmbracoContextReference reference = _umbracoContextFactory.EnsureUmbracoContext();
        return FindRoot(reference);
    }

    public IPublishedContent? GetNewsSection()
    {
        using UmbracoContextReference reference = _umbracoContextFactory.EnsureUmbracoContext();
        return FindRoot(reference)?.Children().FirstOrDefault(x => x.ContentType.Alias == "newsSection");
    }

    private IPublishedContent? FindRoot(UmbracoContextReference reference)
    {
        // Tree structure is in memory, so this doesn't query the database.
        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        return rootKeys
            .Select(key => reference.UmbracoContext.Content?.GetById(key))
            .FirstOrDefault(content => content is not null);
    }
}

// composer
builder.Services.AddSingleton<ISiteService, SiteService>();
```

Use the returned content right away, in the same request or job step. Don't put it in a field or a static.

### If you really want to cache something

Cache the **key** (`Guid`) of the News section, not the `IPublishedContent`. Resolve it per call with `GetById(key)`. The published cache lookup is cheap, and the result is always the latest published version. If you also cache derived values, such as a rendered header, clear them on publish from a `ContentPublishedNotification` handler (register it with `builder.AddNotificationHandler<…>()`). Simply not storing content usually makes this unnecessary.

## Summary

| | Before | After |
|---|---|---|
| Lifetime | Singleton | Scoped (A) or singleton with per-call reads (B) |
| When content is read | Once, in the constructor | On every call |
| What's stored in fields | An `IPublishedContent` snapshot | Only injected services |
| `UmbracoContext` | Disposed, but its content is kept | Opened and disposed per read (B) |
| Missing root | Throws in `First()` | Returns `null`; callers null-check |

**Verification:** I haven't compiled or run this code against your project. The patterns match the `umbraco-services` skill assets, which are compiled on 17.5.3 and 18.2.0 and runtime-tested on 17.5.3. The `Children()` and `newsSection` lookup comes from your code unchanged. Build it and publish a rename of the News page to confirm the header updates without a restart.
