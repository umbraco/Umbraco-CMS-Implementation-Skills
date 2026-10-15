# Why the handler never records a URL

**Short answer:** you are handling the wrong notification. `ContentUnpublishedNotification` (the "-ed" one) fires **after** the page has already been removed from the published cache. When your handler runs, `contentCache.GetById(item.Id)` returns `null`, the `if` is skipped, and nothing gets added. That matches what you see: the handler runs in the debugger, but `_store.Add` is never called.

The comment in the docs snippet ("item being unpublished will still be in the cache, as unpublishing event fires before the cache is updated") is wrong for v17. The "-ed" notification is published after the unpublish has finished and the cache has been updated. We confirmed this on Umbraco 17.5.3: the handler runs, but `Content.GetById` returns null. The v18 docs use the "-ing" notification instead.

## Fix: handle `ContentUnpublishingNotification`

The "-ing" notification fires **before** the unpublish, while the page is still in the published cache, so you can still look up its URL. The items are still in `notification.UnpublishedEntities`. The property has the same name on the "-ing" notification.

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
                continue;
            }

            _store.Add(_publishedUrlProvider.GetUrl(published));
        }
    }
}
```

The changes compared with the docs snippet:

1. **`ContentUnpublishingNotification`** instead of `ContentUnpublishedNotification`. This is the actual fix.
2. **Fix the registration too.** The notification type in the registration has to match the handler's interface. If you register with the old type, the handler is never called:
   ```csharp
   public class GoneUrlComposer : IComposer
   {
       public void Compose(IUmbracoBuilder builder)
       {
           builder.Services.AddSingleton<IGoneUrlStore, GoneUrlStore>();
           builder.AddNotificationHandler<ContentUnpublishingNotification, HandleUnPublishingHandler>();
       }
   }
   ```
   Register `IGoneUrlStore` as a **singleton** if it is application state such as an in-memory list. If your implementation depends on scoped services (for example a DbContext), it has to be scoped instead. The lifetime follows its dependencies.
3. **Inject `IPublishedUrlProvider`** and call `GetUrl(published)`, so the dependency is explicit and not resolved behind `.Url()`. Keeping `.Url()` is fine if you prefer; it isn't the cause of the bug.
4. **Null-safe `Content?`** and lookup by `Key`. `GetById(item.Id)` also works, so this is a style choice.

## Things to keep in mind

- **The "-ing" notification can be cancelled.** Another handler can cancel the unpublish (`notification.Cancel`). If that happens, your store holds a URL for a page that is still live. Before you answer 410 for a URL, check that the page really is gone. For example, make sure the URL no longer resolves to published content, or remove the entry again when the page is republished (`ContentPublishedNotification`).
- **Persist the store.** An in-memory list is lost on restart and isn't shared across load-balanced servers. Use a database table in production.
- **Variants:** if the site is multilingual, a single-culture unpublish also raises the notification, and `GetUrl(published)` only gives the URL for the current/default culture. Pass `culture:` to `GetUrl` for each culture you need.
- **Serving the 410** is a separate step. A content finder runs only when no published content matches the URL. In it, check the store and set `request.SetResponseStatus(410)`. If you want this to work alongside a custom 404, see the `umbraco-custom-error-pages` skill (`IContentLastChanceFinder`).

## Verification status

The fixed pattern matches the skill's `UnpublishingUrlRecorder` asset. That asset compiles on 17.5.3 and 18.2.0, and on 17.5.3 the runtime test shows the "-ing" handler still finds the cached page. I haven't built or run your code (`IGoneUrlStore`, your composer), because you didn't share your project. Unpublish a page and check that the URL appears in the store.
