using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace <Namespace>.Services;

// Request-only: injects scoped IPublishedContentQuery, so it is registered AddScoped.
// Outside a request (jobs) use the RootContentReader pattern instead.
public class SiteService : ISiteService
{
    private readonly IPublishedContentQuery _publishedContentQuery;

    public SiteService(IPublishedContentQuery publishedContentQuery)
        => _publishedContentQuery = publishedContentQuery;

    // Multi-site: pick the root for the current domain, not the first.
    public IPublishedContent? GetSiteRoot() => _publishedContentQuery.ContentAtRoot().FirstOrDefault();
}
