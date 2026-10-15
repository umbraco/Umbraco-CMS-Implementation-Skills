# Recording the URL of unpublished pages (step one toward 410 Gone)

There's no project attached, so I'm assuming **Umbraco 17** as you said. Please check `Umbraco.Cms` in your `.csproj` or `Directory.Packages.props`. The code also works on 18. Replace `MySite` with your root namespace.

## The key decision: use `ContentUnpublishingNotification`, not `...Unpublished`

To get a page's URL you need the page from the **published cache**. Once it has been unpublished it is gone from the cache:

- **`ContentUnpublishedNotification`** ("-ed") fires **after** the page has left the cache. In the handler, `UmbracoContext.Content.GetById(...)` returns `null`, so there's no URL to record. The v17 docs page *"Accessing the Published Content Cache from a Content Finder / UrlProvider"* says the item is still cached at that point. That's wrong for 17.5.x. The v18 docs use the "-ing" notification.
- **`ContentUnpublishingNotification`** ("-ing") fires **before** the page leaves the cache, so you can still resolve its URL. Its items are in `notification.UnpublishedEntities`.

## Where the code runs, and which services it uses

An unpublish is a backoffice action, so there is **no front-end request** and no ambient `UmbracoContext`:

- `IUmbracoContextAccessor.TryGetUmbracoContext` finds nothing here. Inject **`IUmbracoContextFactory`** instead and call `EnsureUmbracoContext()` inside a `using`, so the context is disposed.
- **Reading** uses the published cache (`UmbracoContext.Content`) plus `IPublishedUrlProvider`. That's an in-memory read. Don't use `IContentService`, which queries the database on every call and can't produce a routed URL anyway.
- No content is changed, so no management service is needed.

## `UnpublishingUrlRecorder.cs`

```csharp
using System.Collections.Concurrent;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

// Records a page's URL before it is unpublished, so a content finder can answer 410 Gone later.
// This must be the "-ing" notification: with ContentUnpublishedNotification the page is
// already out of the published cache and GetById returns null.
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
        // Backoffice action: there is no front-end request, so open (and dispose) a context.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // On the "-ing" notification these are the items about to be unpublished.
        foreach (var item in notification.UnpublishedEntities)
        {
            IPublishedContent? published = contextReference.UmbracoContext.Content?.GetById(item.Key);
            if (published is null)
            {
                continue; // was not published, nothing to record
            }

            string url = _publishedUrlProvider.GetUrl(published);
            if (string.IsNullOrEmpty(url) || url == "#")
            {
                continue; // page had no routable URL
            }

            _store.Record(item.Key, url);
        }
    }
}

// In-memory store so the example is self-contained. In production, persist this to a database
// table (for example with a package migration and IScopeProvider), because an in-memory
// dictionary is lost on restart and isn't shared across load-balanced servers.
public class UnpublishedUrlStore
{
    private readonly ConcurrentDictionary<Guid, string> _urlsByKey = new();

    public void Record(Guid key, string url) => _urlsByKey[key] = url;

    public bool TryGetUrl(Guid key, out string? url) => _urlsByKey.TryGetValue(key, out url);

    // What the content finder will call in step two.
    public bool IsGone(string path) =>
        _urlsByKey.Values.Any(u => string.Equals(
            u.TrimEnd('/'), path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

    // Call this when the page is published again, so it stops answering 410.
    public void Remove(Guid key) => _urlsByKey.TryRemove(key, out _);
}
```

## Registration: `UnpublishedUrlComposer.cs`

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using MySite.Services;

namespace MySite.Composers;

public class UnpublishedUrlComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Singleton: it holds the shared state and has no scoped dependencies.
        builder.Services.AddSingleton<UnpublishedUrlStore>();

        builder.AddNotificationHandler<ContentUnpublishingNotification, UnpublishingUrlRecorder>();
    }
}
```

`IComposer` classes are discovered automatically, so you don't need to change `Program.cs`. The handler only depends on singletons (`IUmbracoContextFactory`, `IPublishedUrlProvider`, `UnpublishedUrlStore`), so injecting the singleton store is safe.

## Things to decide before step two

1. **Republishing.** If an editor publishes the page again, it should stop returning 410. Add a `ContentPublishedNotification` handler that calls `_store.Remove(entity.Key)` for each item in `notification.PublishedEntities`.
2. **Multilingual and multi-domain sites.** `GetUrl(published)` returns the URL for the default culture. On a variant site, either loop over `published.Cultures.Keys` and call `GetUrl(published, culture: c)`, or on 18 read `UnpublishedCultures`. With more than one hostname, use `UrlMode.Absolute` (or store host and path separately) so the content finder matches on both.
3. **Unpublishing a parent.** Descendants drop out of routing too, but they aren't all listed in `UnpublishedEntities`. If their URLs should also return 410, walk the descendants in the published cache in the same handler, while they are still cached.
4. **Storage.** Replace the in-memory dictionary with a database table before going live.

## The content finder (step two)

In a content finder there *is* a request pipeline. Inject `UnpublishedUrlStore`, check `IsGone(request.Uri.AbsolutePath)`, and set `request.SetResponseStatus(410)`. Register it in the same composer, for example with `builder.ContentFinders().InsertBefore<ContentFinderByUrl, GoneContentFinder>()`. For the 404 fallback itself, see the `umbraco-custom-error-pages` skill.

## Verification

Nothing has been compiled or tested for this answer, because no project was provided. The handler pattern above matches the skill's asset, which compiles on 17.5.3 and 18.2.0 and was runtime-tested on 17.5.3. In that test, the "-ing" handler still found the cached page. The extra store methods (`IsGone`, `Remove`), the URL filtering and the composer are new code that hasn't been built. Run `dotnet build`, then unpublish a page in the backoffice and check that its URL is recorded.
