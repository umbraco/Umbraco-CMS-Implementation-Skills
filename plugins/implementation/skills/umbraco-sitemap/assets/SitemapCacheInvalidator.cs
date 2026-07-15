using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace <Namespace>;

public class SitemapCacheInvalidator :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>,
    INotificationHandler<ContentDeletedNotification>
{
    private readonly IMemoryCache _cache;

    public SitemapCacheInvalidator(IMemoryCache cache) => _cache = cache;

    // Clears both cache keys so this works whether SitemapController ("SitemapXml") or
    // SitemapIndexController ("SitemapUrls") is registered — removing an absent key is a no-op.
    private void Invalidate()
    {
        _cache.Remove("SitemapXml");
        _cache.Remove("SitemapUrls");
    }

    public void Handle(ContentPublishedNotification notification) => Invalidate();
    public void Handle(ContentUnpublishedNotification notification) => Invalidate();
    public void Handle(ContentDeletedNotification notification) => Invalidate();
}
