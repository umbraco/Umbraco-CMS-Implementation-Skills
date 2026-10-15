# Recording the URL of unpublished pages (Umbraco 17)

To record the URL a page had, you need to read it **before** it leaves the published cache. That decides which notification you handle:

- **`ContentUnpublishingNotification`** ("-ing") fires *before* the cache updates. The page is still published, so its URL can be resolved. **Use this one.**
- **`ContentUnpublishedNotification`** ("-ed") fires *after* the page has left the cache. `Content.GetById` returns `null` there, so you'd get no URL.

> **Docs correction.** The Umbraco 17 *Services and Helpers* page ("Accessing the Published Content Cache from a Content Finder / UrlProvider") handles `ContentUnpublishedNotification` and says the item "will still be in the cache". That's wrong on 17. The handler runs, but the page is already gone. The Umbraco 18 docs use the "-ing" notification. On Umbraco 17.5.3 the code below was checked with the skill's runtime test gate, which confirms the handler still finds the page in the cache.

A notification handler runs from a backoffice action with no front-end request, so `IUmbracoContextAccessor` has no context to give you. The handler injects **`IUmbracoContextFactory`** instead and opens a context with `EnsureUmbracoContext()` inside a `using`.

## The handler and the store

`Services/UnpublishingUrlRecorder.cs`. Replace `MySite` with your namespace.

```csharp
using System.Collections.Concurrent;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

// Remembers the URL a page had before it was unpublished, so a content finder can answer 410 Gone.
//
// Handles ContentUnpublishingNotification, NOT ContentUnpublishedNotification: "Unpublishing" fires
// BEFORE the page leaves the published cache, so its URL can still be read. "Unpublished" fires AFTER.
public class UnpublishingUrlRecorder : INotificationHandler<ContentUnpublishingNotification>
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly UnpublishedUrlStore _store;

    public UnpublishingUrlRecorder(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedUrlProvider publishedUrlProvider,
        UnpublishedUrlStore store)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _publishedUrlProvider = publishedUrlProvider;
        _store = store;
    }

    public void Handle(ContentUnpublishingNotification notification)
    {
        // Backoffice actions have no front-end request, so open a context to read the cache.
        // Dispose it straight away. Never keep it, or IPublishedContent read from it, in a field.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // Despite the name, UnpublishedEntities on the "-ing" notification are the items ABOUT to be
        // unpublished. They are still in the cache at this point.
        foreach (var item in notification.UnpublishedEntities)
        {
            IPublishedContent? published = contextReference.UmbracoContext.Content?.GetById(item.Key);
            if (published is null)
            {
                continue;
            }

            string url = _publishedUrlProvider.GetUrl(published);
            if (string.IsNullOrEmpty(url) || url == "#") // "#" = page had no routable URL
            {
                continue;
            }

            _store.Record(item.Key, url);
        }
    }
}

// In-memory for now. Indexed by URL so the content finder can look up a request path directly.
public class UnpublishedUrlStore
{
    private readonly ConcurrentDictionary<string, Guid> _keysByUrl = new(StringComparer.OrdinalIgnoreCase);

    public void Record(Guid key, string url) => _keysByUrl[Normalize(url)] = key;

    public bool IsGone(string url) => _keysByUrl.ContainsKey(Normalize(url));

    public void Forget(string url) => _keysByUrl.TryRemove(Normalize(url), out _);

    private static string Normalize(string url) => url.TrimEnd('/') is { Length: > 0 } trimmed ? trimmed : "/";
}
```

## Registration

`SiteComposer.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using MySite.Services;

namespace MySite;

public class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // One store for the whole application, and it injects nothing scoped → singleton.
        builder.Services.AddSingleton<UnpublishedUrlStore>();

        builder.AddNotificationHandler<ContentUnpublishingNotification, UnpublishingUrlRecorder>();
    }
}
```

## Which service does what, and why

| Class | Uses | Why | Registered as |
|---|---|---|---|
| `UnpublishingUrlRecorder` | `IUmbracoContextFactory` + `IPublishedUrlProvider` (published cache) | It only *reads* the page's current URL, so the cache is right. `IContentService` would hit the database and can't give you a routed URL anyway. There's no request, so the context comes from the factory, not the accessor. | `builder.AddNotificationHandler<ContentUnpublishingNotification, UnpublishingUrlRecorder>()` |
| `UnpublishedUrlStore` | none | It holds state for the whole app and has no scoped dependencies. | `AddSingleton` |

## Before you rely on it

- **Persist the store.** An in-memory dictionary is lost on restart, and in a load-balanced setup each server only knows about the unpublishes it handled itself. For production, back `UnpublishedUrlStore` with a custom database table (or the Redirect URL Management tables, if you want them in the backoffice). Its public methods can stay the same.
- **Republishing.** When the page is published again, its URL should stop returning 410. Handle `ContentPublishedNotification` and call `Forget` for the page's URL, or have the content finder check the cache first. Umbraco's normal routing already serves a published page before a custom finder runs, so checking the cache first mostly happens without extra code.
- **Descendants.** Unpublishing a parent takes its children offline too, but `UnpublishedEntities` contains only the item(s) the editor acted on. If you want 410s for the whole branch, walk the descendants through `IDocumentNavigationQueryService` in the same handler, while they are still cached. I haven't verified this part.
- **Multilingual sites.** `GetUrl(published)` returns the URL for the default/current culture. On culture-variant sites, call `GetUrl(published, UrlMode.Relative, culture)` for each published culture. How culture-level unpublishes surface in this notification was not verified here.
- **Next step, the content finder.** Inject `UnpublishedUrlStore`. If `IsGone(request.Uri.AbsolutePath)` matches, set `request.SetResponseStatus(410)` and return the content to render (for example a "Gone" page). Register it with `builder.ContentFinders().InsertBefore<ContentFinderByUrl, GoneContentFinder>()`, or as the last-chance finder. Inside a finder, get the cache via `IUmbracoContextAccessor`, because a context exists in the routing pipeline.

**Verification:** No project was provided, so I haven't compiled or run this here. The handler and the composer registration are the same as the skill's asset, which compiles against Umbraco.Cms 17.5.3 and is runtime-tested there. The URL-keyed store (`IsGone`/`Forget`) and the `"#"` guard are my additions and weren't tested.
