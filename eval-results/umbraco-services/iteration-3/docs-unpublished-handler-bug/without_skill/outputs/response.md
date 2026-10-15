# Why the "410 Gone" handler never records a URL (Umbraco 17)

## The short answer

You subscribed to the wrong notification. `ContentUnpublishedNotification` (past tense, "-ed") is
published **after** the unpublish has happened. By the time your handler runs, the published content
cache has already been updated (or is updated as part of the same completed operation), so:

```csharp
IPublishedContent soonToBeUnPublishedItem = contentCache.GetById(item.Id); // -> null
```

returns `null`. The `if (... != null)` guard then skips the item every time, so `_store.Add(...)` never
runs. The handler *executes* (which is why you see it in the debugger), it just never finds anything.

The code comment you copied ("item being unpublished will still be in the cache, as unpublishing event
fires before the cache is updated") describes the **"-ing"** notification, not the "-ed" one. It is a
leftover from the old v7/v8 sample, where the timing between the event and the cache refresh happened
to work out. In Umbraco 15+ (the HybridCache-based published cache, which 17 uses) you can't rely on that.

**Fix:** handle `ContentUnpublishingNotification` instead. It fires before the content is unpublished,
while the item is still in the published cache and still has a URL.

## Minimal fix

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;

public class HandleUnPublishingHandler : INotificationHandler<ContentUnpublishingNotification>
{
    private readonly IPublishedContentCache _contentCache;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IGoneUrlStore _store;

    public HandleUnPublishingHandler(
        IPublishedContentCache contentCache,
        IPublishedUrlProvider urlProvider,
        IGoneUrlStore store)
    {
        _contentCache = contentCache;
        _urlProvider = urlProvider;
        _store = store;
    }

    public void Handle(ContentUnpublishingNotification notification)
    {
        foreach (var item in notification.UnpublishedEntities)
        {
            // "-ing" notification: the item is still published, so it is still in the cache.
            IPublishedContent? published = _contentCache.GetById(item.Key);
            if (published is null)
            {
                continue;
            }

            string url = _urlProvider.GetUrl(published);
            if (!string.IsNullOrEmpty(url) && url != "#") // "#" = no routable URL
            {
                _store.Add(url);
            }
        }
    }
}
```

Register it (if you haven't already — you clearly have, since it hits the breakpoint, but you need to
change the notification type in the registration too):

```csharp
builder.AddNotificationHandler<ContentUnpublishingNotification, HandleUnPublishingHandler>();
```

Changes compared to the docs sample:

- `ContentUnpublishingNotification` instead of `ContentUnpublishedNotification` — the actual bug.
- In Umbraco 15+ `IPublishedContentCache` can be injected directly, so you don't need
  `IUmbracoContextFactory.EnsureUmbracoContext()` just to reach the cache. (Keeping the
  `EnsureUmbracoContext` approach also works if you prefer it; it is not the cause of the problem.)
- `IPublishedUrlProvider.GetUrl(...)` instead of the `.Url()` extension method. The extension resolves
  the URL provider through a static service locator; injecting the provider is explicit and testable.
- Guard against `"#"`, which is what Umbraco returns when a node has no routable URL.

## Things the docs sample also misses (worth handling for a real 410 feature)

1. **Descendants go away too.** Unpublishing a page makes its whole subtree unroutable, but
   `UnpublishedEntities` contains only the node(s) the editor unpublished. If you want 410 for the
   children as well, walk the descendants while they are still in the cache, e.g. with
   `IDocumentNavigationQueryService.TryGetDescendantsKeys(published.Key, out var keys)` and
   `_contentCache.GetById(key)` for each, adding each URL.

2. **Multilingual sites.** `GetUrl(published)` gives you the URL for the current/default culture. For
   variant content, loop the cultures (`published.Cultures.Keys`) and call
   `_urlProvider.GetUrl(published, culture: culture)` for each.
   Also note: unpublishing **a single culture** does *not* raise `ContentUnpublishing/Unpublished` at
   all — Umbraco treats it as a publish of the remaining cultures. To catch that case you need a
   `ContentPublishingNotification` handler using `notification.IsUnpublishingCulture(node, culture)`
   (see the ContentService notifications docs).

3. **The "-ing" notification can be cancelled.** Another handler may call `notification.Cancel = true`,
   in which case the page stays published and you'd have recorded a URL that isn't actually gone. If
   that matters, use the stateful pattern: implement both `INotificationHandler<ContentUnpublishingNotification>`
   and `INotificationHandler<ContentUnpublishedNotification>` on the **same** class, capture the URLs
   in the "-ing" handler into `notification.State["goneUrls"]`, and only write them to the store in the
   "-ed" handler (Umbraco shares the `State` dictionary between the two for the same handler type).

4. **Re-publishing.** Remove the URL from your store when the page (or a new page at the same URL) is
   published again, typically in a `ContentPublishedNotification` handler; otherwise a republished page
   keeps answering 410 if your 410 check runs before normal routing.

5. **Moving to the recycle bin / deleting.** Trashing a published page raises `ContentMovingToRecycleBin…`
   / `ContentDeleting…`, not the unpublish notifications. Handle those too if "deleted" should also be 410.

6. **Multiple servers / load balancing.** Notification handlers run only on the server where the editor
   performed the action, so `IGoneUrlStore` should be backed by shared storage (database), not memory.

## Verify

Set a breakpoint after `GetById` in the new handler and unpublish a page: `published` should now be
non-null and `url` should be the page's route (e.g. `/about-us/`). Then request that URL on the front
end and check your 410 logic returns `410 Gone`.

## Sources

- Umbraco docs, ContentService notifications (Unpublishing vs Unpublished, single-variant unpublish via
  `ContentPublishingNotification` + `IsUnpublishingCulture`):
  https://docs.umbraco.com/umbraco-cms/reference/notifications/contentservice-notifications
- The rest (cache timing in the HybridCache-based published cache, `IPublishedContentCache` being
  injectable since v15, `IDocumentNavigationQueryService`, notification `State`) is from my knowledge of
  the Umbraco 15–17 APIs and was not re-verified against the v17 source in this answer — check exact
  method signatures against your installed version.
