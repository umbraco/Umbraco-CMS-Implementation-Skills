using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace <Namespace>.Routing;

/// <summary>
/// Short links for printed material: /p/{sku} answers with a 301 to the product's real URL.
///
/// Umbraco runs content finders in order until one returns true. This one is appended after the
/// built-in finders, so it only ever sees URLs that matched no page, alias or tracked redirect, and
/// it bails out on the first line for anything outside its prefix.
/// </summary>
public class SkuShortLinkContentFinder : IContentFinder
{
    private const string Prefix = "/<ShortLinkPrefix>/";
    private const string ProductDocTypeAlias = "<ProductDocTypeAlias>";
    private const string SkuPropertyAlias = "<SkuPropertyAlias>";

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedUrlProvider _urlProvider;

    public SkuShortLinkContentFinder(
        IUmbracoContextAccessor umbracoContextAccessor,
        IDocumentNavigationQueryService navigation,
        IPublishedUrlProvider urlProvider)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _navigation = navigation;
        _urlProvider = urlProvider;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        string path = request.AbsolutePathDecoded;
        if (!path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        string sku = path[Prefix.Length..].Trim('/');
        if (sku.Length == 0 || sku.Contains('/')
            || !_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        IPublishedContent? product = FindProduct(umbracoContext, request, sku);
        if (product is null)
        {
            return Task.FromResult(false); // Unknown SKU: let the 404 handling take over.
        }

        // Redirect rather than render: one canonical URL per page. To render the product at the
        // short URL instead, call request.SetPublishedContent(product).
        string url = _urlProvider.GetUrl(product, UrlMode.Auto, request.Culture, request.Uri);
        request.SetRedirectPermanent(url);
        return Task.FromResult(true);
    }

    private IPublishedContent? FindProduct(IUmbracoContext umbracoContext, IPublishedRequestBuilder request, string sku)
    {
        // On a multi-site install, only search the site whose domain matched the request.
        IEnumerable<Guid> rootKeys = request.Domain is { } domain
            ? umbracoContext.Content!.GetById(domain.ContentId) is { } siteRoot ? [siteRoot.Key] : []
            : _navigation.TryGetRootKeys(out IEnumerable<Guid> keys) ? keys : [];

        foreach (Guid rootKey in rootKeys)
        {
            // An in-memory key lookup, not a content traversal. For a catalogue of many thousands of
            // products, query an Examine index on the SKU instead.
            if (!_navigation.TryGetDescendantsKeysOfType(rootKey, ProductDocTypeAlias, out IEnumerable<Guid> productKeys))
            {
                continue;
            }

            foreach (Guid key in productKeys)
            {
                IPublishedContent? product = umbracoContext.Content!.GetById(key);
                if (string.Equals(product?.Value<string>(SkuPropertyAlias), sku, StringComparison.OrdinalIgnoreCase))
                {
                    return product;
                }
            }
        }

        return null;
    }
}
