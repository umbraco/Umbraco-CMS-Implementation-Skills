# Why the header shows the old News title

## What's wrong

**1. The singleton keeps one `IPublishedContent` for the whole life of the app.**
`AddSingleton` builds `SiteService` once, the first time something resolves it. The constructor runs once, looks up the News node and stores that `IPublishedContent` in a field. From then on `GetNewsSection()` hands back that same object on every request.

`IPublishedContent` is a **read-only snapshot** of the node as it was published when it was read. Publishing does not change that object. Umbraco rebuilds the cache entry and gives later lookups a *new* `IPublishedContent`. Your service never looks the node up again, so it keeps returning the object from startup, with the old `Name`, old property values and old URL, until a restart builds a new singleton. That is exactly the symptom: no output cache or CDN is involved, because the stale data is held in your own field.

**2. Constructors in a singleton should not do content lookups.**
- The constructor runs whenever DI first resolves the service. That can be during startup, before content is loaded, or before any root node exists (for example, on a fresh install). Then `rootKeys.First()` throws `InvalidOperationException`, or `_newsSection` is set to `null` and stays `null` forever.
- `TryGetRootKeys` returns `false` and its result is ignored.
- Calling `EnsureUmbracoContext()` in a constructor only to read content is unnecessary in Umbraco 15 and later, because the published content cache can be injected directly.

**3. Culture, preview and other per-request context are lost.**
With variant content, `Name` depends on the current request's culture. A value captured once at startup is in whatever culture was active then, or none. Preview requests also always get the published startup snapshot.

**4. Smaller issues**
- The navigation service holds the full document tree, including unpublished nodes. `rootKeys.First()` may therefore be an unpublished root, and `GetById` returns `null` for it.
- It only looks under the first root. On a multi-root site that may not be the right one.

## Rule of thumb

> Never keep `IPublishedContent`, or anything taken from it, in a field of a singleton or any other long-lived object. Look it up from the cache each time you need it. The published cache is in memory and built for this, so lookups are cheap.

If you want to cache anything, cache the **identity** (key or id), not the content, and still resolve the content per call.

## How it should be written (Umbraco 17)

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;

public interface ISiteService
{
    IPublishedContent? GetNewsSection();
}

public class SiteService : ISiteService
{
    private const string NewsSectionAlias = "newsSection";

    private readonly IPublishedContentCache _contentCache;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(
        IPublishedContentCache contentCache,
        IDocumentNavigationQueryService navigation)
    {
        // Store only services here. No content lookups in the constructor.
        _contentCache = contentCache;
        _navigation = navigation;
    }

    public IPublishedContent? GetNewsSection()
    {
        if (!_navigation.TryGetRootKeys(out var rootKeys))
        {
            return null;
        }

        foreach (var rootKey in rootKeys)
        {
            if (!_navigation.TryGetChildrenKeysOfType(rootKey, NewsSectionAlias, out var childKeys))
            {
                continue;
            }

            foreach (var childKey in childKeys)
            {
                // Returns the CURRENT published version, or null if the node is not published.
                var newsSection = _contentCache.GetById(childKey);
                if (newsSection is not null)
                {
                    return newsSection;
                }
            }
        }

        return null;
    }
}
```

Registration:

```csharp
public class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISiteService, SiteService>();
}
```

Notes:
- **Lifetime.** The service no longer holds any content, so `Singleton` would technically work too, because the injected Umbraco services are themselves singletons. `Scoped` (one instance per request) is the safer default for a service that reads content, so nobody adds a cached field later. Either way, the fix is that **content is resolved inside the method, not in the constructor.**
- **Up to date after publish.** `IPublishedContentCache.GetById` reads the Umbraco 15+ HybridCache-backed published cache. Umbraco refreshes that cache on publish, including across load-balanced servers through cache refreshers, so the header changes right away.
- **Culture.** In a front-end request, the variation context is set per request, so `newsSection.Name` in your view uses the culture being rendered. Outside a request (background jobs and similar), use `newsSection.Name(culture)` or set the variation context yourself.
- **In views,** prefer handing the result straight to the view or view component that renders the header, rather than putting it in another long-lived object.

## Optional: if the tree walk feels too costly

The lookup above is a couple of in-memory dictionary reads, which is negligible. If the real logic is heavier (for example, a deep descendant search), cache only the **key** and clear it when content changes:

```csharp
public class SiteService : ISiteService
{
    private readonly IPublishedContentCache _contentCache;
    private readonly IDocumentNavigationQueryService _navigation;
    private Guid? _newsSectionKey; // identity only, never IPublishedContent

    public SiteService(IPublishedContentCache contentCache, IDocumentNavigationQueryService navigation)
    {
        _contentCache = contentCache;
        _navigation = navigation;
    }

    public IPublishedContent? GetNewsSection()
    {
        _newsSectionKey ??= FindNewsSectionKey();
        return _newsSectionKey is { } key ? _contentCache.GetById(key) : null;
    }

    public void Reset() => _newsSectionKey = null;

    private Guid? FindNewsSectionKey() { /* same tree walk as above, returning the key */ return null; }
}

// Clear the cached key when content structure changes (move/delete/new news section):
public class SiteServiceCacheInvalidator : INotificationHandler<ContentCacheRefresherNotification>
{
    private readonly SiteService _siteService;
    public SiteServiceCacheInvalidator(SiteService siteService) => _siteService = siteService;
    public void Handle(ContentCacheRefresherNotification notification) => _siteService.Reset();
}

// composer
builder.Services.AddSingleton<SiteService>();
builder.Services.AddSingleton<ISiteService>(sp => sp.GetRequiredService<SiteService>());
builder.AddNotificationHandler<ContentCacheRefresherNotification, SiteServiceCacheInvalidator>();
```

Even in this version, a rename shows up right away because the **content** is read fresh on each call. The cached key only matters if the node is moved, deleted or replaced. `ContentCacheRefresherNotification` is used instead of `ContentPublishedNotification` because it also fires on the other servers in a load-balanced setup. For the simple case, the first version, with no caching, is the one to use.

## Summary

| Problem | Fix |
|---|---|
| `IPublishedContent` captured once in a singleton field, so a stale snapshot is kept until restart | Look up content inside the method on each call |
| Content lookup in the constructor (startup timing, `First()` can throw, `null` is kept forever) | Constructor only stores services; handle `TryGet…` returning `false` |
| `EnsureUmbracoContext` used only for content reads | Inject `IPublishedContentCache` (Umbraco 15+) |
| Culture and preview frozen at construction time | Resolving per request picks up the request's variation context |
| Singleton lifetime makes caching content too easy | Register `Scoped`; if you need to cache, cache the key and invalidate it on `ContentCacheRefresherNotification` |

*Written from general Umbraco 15–17 knowledge and not compiled against a 17.x project. Check member names such as `TryGetChildrenKeysOfType` against your installed version.*
