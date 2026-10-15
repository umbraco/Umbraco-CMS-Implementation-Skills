using Umbraco.Cms.Core.Routing;

namespace Acme.Site.Infrastructure;

/// <summary>Serves /campaign/{code} URLs from the redirect cache.</summary>
public class CampaignContentFinder : IContentFinder
{
    private readonly IRedirectCache _cache;

    public CampaignContentFinder(IRedirectCache cache) => _cache = cache;

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        string path = request.Uri.AbsolutePath;
        if (!path.StartsWith("/campaign/", StringComparison.OrdinalIgnoreCase) || !_cache.TryGet(path, out string? target))
        {
            return Task.FromResult(false);
        }

        request.SetRedirect(target!);
        return Task.FromResult(true);
    }
}
