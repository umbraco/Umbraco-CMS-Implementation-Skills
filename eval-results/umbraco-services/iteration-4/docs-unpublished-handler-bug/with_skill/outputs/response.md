# Why the handler never records a URL

## The cause: wrong notification

The handler subscribes to **`ContentUnpublishedNotification`**, the "-ed" notification. In Umbraco 17 that notification fires **after** the unpublish has finished and the page has already been removed from the published cache. So:

- the handler runs (which is why the breakpoint is hit),
- `contentCache.GetById(item.Id)` returns `null` because the page is no longer in the cache,
- the `if (soonToBeUnPublishedItem != null)` guard skips the item, and nothing is ever added.

The comment in the docs sample is wrong for v17. It says *"item being unpublished will still be in the cache, as unpublishing event fires before the cache is updated"*. That would be true of the "-ing" notification, but the sample handles the "-ed" one. On 17.5.3 this has been verified at runtime: the handler runs and `Content.GetById` returns null. The v18 docs use the "-ing" notification.

## The fix: handle `ContentUnpublishingNotification`

`ContentUnpublishingNotification` fires **before** the unpublish, while the page is still published and still in the cache. On this notification the items are also in `UnpublishedEntities`, so the loop doesn't change much:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

public class HandleUnPublishingHandler : INotificationHandler<ContentUnpublishingNotification>
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IGoneUrlStore _store;

    public HandleUnPublishingHandler(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedUrlProvider publishedUrlProvider,
        IGoneUrlStore store)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _publishedUrlProvider = publishedUrlProvider;
        _store = store;
    }

    public void Handle(ContentUnpublishingNotification notification)
    {
        // Unpublishing is a backoffice action with no front-end request, so open (and dispose) a context.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        foreach (var item in notification.UnpublishedEntities)
        {
            // "-ing" fires before the cache is updated, so the page is still here.
            IPublishedContent? published = contextReference.UmbracoContext.Content?.GetById(item.Key);
            if (published is null)
            {
                continue; // was never published
            }

            _store.Add(_publishedUrlProvider.GetUrl(published));
        }
    }
}
```

**Change the registration as well.** If you only change the class, the handler is still registered against the old notification type and won't compile. In your composer (or in `Program.cs`):

```csharp
builder.AddNotificationHandler<ContentUnpublishingNotification, HandleUnPublishingHandler>();
```

Notes on the changes:

- **`EnsureUmbracoContext()` inside `using`.** Keep this. There is no front-end request during a backoffice unpublish, so `IUmbracoContextAccessor` wouldn't find a context. Building a URL also needs one: both `.Url()` and `IPublishedUrlProvider` throw *"Wasn't able to get an UmbracoContext"* without it. The context must be disposed, which the `using` does.
- **`GetById(item.Key)` and `IPublishedUrlProvider`.** Both are optional tidy-ups. Your `GetById(item.Id)` and `.Url()` also work inside the `using`. Injecting `IPublishedUrlProvider` just avoids the static extension method.
- **Don't cancel.** `ContentUnpublishingNotification` can be cancelled. Only read from it, and don't call `notification.Cancel...`, or you'll block the unpublish.
- **Store lifetime.** `IGoneUrlStore` is shared app state, so register it as a singleton. Back it with a database table rather than memory, so the list survives restarts and works across load-balanced servers.

## Things to check afterwards

1. **Test it.** Unpublish a page in the backoffice and confirm the URL is recorded, then request that URL and confirm your 410 logic picks it up. I couldn't run this against your site, so it isn't verified for your project.
2. **Multilingual sites.** If you unpublish only *some* languages while others stay live, Umbraco raises a publish notification for the remaining cultures, not `ContentUnpublishedNotification`. Whether `ContentUnpublishingNotification` fires in that case hasn't been verified here. If you need 410s per culture, test that scenario. From 17.6.0, `ContentPublishedNotification` and `ContentUnpublishedNotification` expose `UnpublishedCultures` for this.
3. **Republishing.** If a page is published again, remove its URL from the store (for example in a `ContentPublishedNotification` handler) so it doesn't keep returning 410.
4. **Other ways a page disappears.** Deleting or moving a page to the recycle bin is a separate case. Add handlers for those if they should also return 410.
