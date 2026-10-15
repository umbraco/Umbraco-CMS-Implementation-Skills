# Why the header keeps showing the old News title

## What's wrong

1. **The content is read once and kept forever.** All the work happens in the constructor. A singleton is built once, the first time something asks for it, so `_newsSection` is filled once and never read again.
2. **`IPublishedContent` is a snapshot, not a live view.** When an editor publishes, Umbraco puts a *new* `IPublishedContent` object into the published cache. It does not change the one you already hold. Your field keeps pointing at the old object, with the old `Name`, until the process restarts. That's the bug your editors see. Output caching and CDN aren't involved, which matches what you've seen.
3. **The content came from a context that's already disposed.** `EnsureUmbracoContext()` is disposed when the constructor returns (`using var`), but the content read through it lives on in a field. Rule: dispose every `EnsureUmbracoContext()`, and **never store a context, or content read from it, in a field**.
4. **Smaller problems:**
   - `rootKeys.First()` throws if nothing is published yet, and the return value of `TryGetRootKeys` is ignored. If the app starts before the root is published, the singleton fails to build, or (if the lookup returns null) it caches `null` until restart.
   - The news section will also be missing after it's moved, unpublished or deleted and recreated.

The lifetime itself isn't the mistake. A singleton is fine here because everything it injects is a singleton. The mistake is **storing content in the singleton**. The published cache is in memory, so looking things up on each call is cheap. There's nothing worth caching yourself.

## How to write it

Keep `AddSingleton`, but inject only singletons and read the cache **on every call**. In Umbraco 15+ (so in 17), `IPublishedContentCache` can be injected as a singleton and needs no `UmbracoContext`. `IDocumentNavigationQueryService` holds the tree structure in memory, so getting root and child keys doesn't hit the database. Because of that, this version also works outside a request (background jobs, notification handlers).

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;

namespace MySite.Services;

public interface ISiteService
{
    IPublishedContent? GetSiteRoot();
    IPublishedContent? GetNewsSection();
}

// Singleton: depends only on singletons. Stores services, never content.
public class SiteService : ISiteService
{
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(
        IPublishedContentCache publishedContentCache,
        IDocumentNavigationQueryService navigation)
    {
        _publishedContentCache = publishedContentCache;
        _navigation = navigation;
    }

    public IPublishedContent? GetSiteRoot() =>
        _navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys)
            ? rootKeys.Select(key => _publishedContentCache.GetById(key))
                      .FirstOrDefault(content => content is not null)
            : null;

    // Read fresh on every call, so a rename shows up as soon as it's published.
    public IPublishedContent? GetNewsSection()
    {
        IPublishedContent? root = GetSiteRoot();
        if (root is null || !_navigation.TryGetChildrenKeys(root.Key, out IEnumerable<Guid> childKeys))
        {
            return null;
        }

        return childKeys
            .Select(key => _publishedContentCache.GetById(key))
            .FirstOrDefault(content => content?.ContentType.Alias == "newsSection");
    }
}
```

Composer (one `IComposer` per feature):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.Services;

namespace MySite;

public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<ISiteService, SiteService>();
}
```

Using it in the header:

```cshtml
@inject MySite.Services.ISiteService SiteService
@{
    var news = SiteService.GetNewsSection();
}
@if (news is not null)
{
    <a href="@news.Url()">@news.Name</a>
}
```

Callers must handle `null`, because the section may not be published.

**If you need URLs outside a request** (for example in a background job), `IPublishedUrlProvider` throws without an `UmbracoContext`. In that case, open a context for each call inside the method: `using var reference = _umbracoContextFactory.EnsureUmbracoContext();`. Read and build URLs inside that `using` block, and still keep nothing in fields. The header runs in a request, so it doesn't need this.

### Alternative: scoped, request-only

If the service is only ever used during requests, you can inject the scoped `IPublishedContentQuery` (for example `ContentAtRoot().FirstOrDefault()`) instead and register with **`AddScoped`**. You can't keep `AddSingleton` with a scoped dependency. With scope validation on (for example in Development), startup fails with *"Cannot consume scoped service … from singleton"*. With validation off, one request's data leaks into others. The singleton version above works in both settings, so I'd use it.

### If you want to "cache" something

Cache the **key** (`Guid`), not the `IPublishedContent`, and still call `_publishedContentCache.GetById(key)` on each call. Then a rename is picked up straight away. Moving the section or recreating it gets a new key, so you would still need to look it up again. It's usually not worth it: the navigation and cache lookups above are already in memory.

## Multi-site note

`GetSiteRoot()` returns the first root. If you run more than one site, choose the root for the current domain instead of the first one.

## Verification status

- The singleton pattern is checked against the skill's verified `RootContentReader` asset. It compiles on 17.5.3 and 18.2.0 and passed runtime tests on 17.5.3: `IPublishedContentCache.GetById(Guid)` plus `IDocumentNavigationQueryService.TryGetRootKeys`.
- `TryGetChildrenKeys` and the code above as written were **not built or run** here, because no project was provided. Build it and publish a rename to confirm the header updates without a restart.
