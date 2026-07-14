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

    public void Handle(ContentPublishedNotification notification) => _cache.Remove("SitemapXml");
    public void Handle(ContentUnpublishedNotification notification) => _cache.Remove("SitemapXml");
    public void Handle(ContentDeletedNotification notification) => _cache.Remove("SitemapXml");
}
