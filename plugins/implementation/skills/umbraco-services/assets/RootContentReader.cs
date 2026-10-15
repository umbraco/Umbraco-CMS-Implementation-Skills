using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace <Namespace>.Services;

// No HTTP request (jobs, hosted services): a singleton. Two verified ways to read the cache; keep one.
// Never store a context or content read from it in a field.
public class RootContentReader
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IDocumentNavigationQueryService _documentNavigationQueryService;

    public RootContentReader(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedContentCache publishedContentCache,
        IDocumentNavigationQueryService documentNavigationQueryService)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _publishedContentCache = publishedContentCache;
        _documentNavigationQueryService = documentNavigationQueryService;
    }

    // A: open a context per read. Needed when the work also builds URLs (IPublishedUrlProvider throws without one).
    public string? GetSiteRootName()
    {
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        return FindRoot(key => contextReference.UmbracoContext.Content?.GetById(key))?.Name;
    }

    // B (Umbraco 15+): the published content cache is itself an injectable singleton; no context needed.
    public string? GetSiteRootNameFromCache() => FindRoot(key => _publishedContentCache.GetById(key))?.Name;

    // Tree structure is in memory: root keys without a DB query.
    private IPublishedContent? FindRoot(Func<Guid, IPublishedContent?> getById) =>
        _documentNavigationQueryService.TryGetRootKeys(out IEnumerable<Guid> rootKeys)
            ? rootKeys.Select(getById).FirstOrDefault(content => content is not null)
            : null;
}
