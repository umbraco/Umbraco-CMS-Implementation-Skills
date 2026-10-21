using System.Collections.Concurrent;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace <Namespace>.Services;

// Records a page's URL before unpublish (e.g. for 410 Gone). Must be the "-ing" notification:
// with ContentUnpublishedNotification the page is already out of the cache (v17 docs are wrong).
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
        // Scheduled unpublish and background code have no request, so ensure a context
        // (inside a backoffice request this reuses the request's own).
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // On the "-ing" notification these are the items about to be unpublished.
        foreach (var item in notification.UnpublishedEntities)
        {
            IPublishedContent? published = contextReference.UmbracoContext.Content?.GetById(item.Key);
            if (published is null)
            {
                continue;
            }

            _store.Record(item.Key, _publishedUrlProvider.GetUrl(published));
        }
    }
}

// In-memory for the example; persist to a DB table in production (restarts, load balancing).
public class UnpublishedUrlStore
{
    private readonly ConcurrentDictionary<Guid, string> _urls = new();

    public void Record(Guid key, string url) => _urls[key] = url;

    public bool TryGetUrl(Guid key, out string? url) => _urls.TryGetValue(key, out url);
}
