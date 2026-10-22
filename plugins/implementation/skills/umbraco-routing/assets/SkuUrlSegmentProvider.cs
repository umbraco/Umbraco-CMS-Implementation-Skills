using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace <Namespace>.Routing;

/// <summary>
/// Appends the product's SKU to its URL segment: a "Swibble" page with SKU "SW-123" gets the segment
/// "swibble-sw-123", so its URL becomes /products/swibble-sw-123/.
///
/// Umbraco stores the segment it gets from here and routes inbound requests by it, so no content
/// finder is needed for the new URLs. Deploying this provider makes Umbraco rebuild every stored
/// segment at the next boot WITHOUT recording redirects: published pages' old URLs need a 301 from you.
/// </summary>
public class SkuUrlSegmentProvider : IUrlSegmentProvider
{
    private const string ProductDocTypeAlias = "<ProductDocTypeAlias>";
    private const string SkuPropertyAlias = "<SkuPropertyAlias>";

    private readonly IShortStringHelper _shortStringHelper;

    // Umbraco's own provider: honours umbracoUrlName and the published/draft name, so the custom
    // segment starts from exactly what the page would have had without this provider.
    private readonly DefaultUrlSegmentProvider _defaultProvider;

    public SkuUrlSegmentProvider(IShortStringHelper shortStringHelper)
    {
        _shortStringHelper = shortStringHelper;
        _defaultProvider = new DefaultUrlSegmentProvider(shortStringHelper);
    }

    public string? GetUrlSegment(IContentBase content, string? culture = null)
        => GetUrlSegment(content, published: true, culture);

    // Umbraco calls this overload: published: true for the live URL, published: false for the draft
    // (to detect a changed URL and record a 301 for the old one). Read the matching values.
    public string? GetUrlSegment(IContentBase content, bool published, string? culture = null)
    {
        if (content.ContentType.Alias != ProductDocTypeAlias)
        {
            return null; // Not a product: the next provider, and finally Umbraco's default, decides.
        }

        // SKUs don't vary by culture. GetValue with a culture on an invariant property returns null,
        // so read it without one.
        string? sku = content.GetValue<string>(SkuPropertyAlias, published: published);
        string? baseSegment = _defaultProvider.GetUrlSegment(content, published, culture);
        if (string.IsNullOrWhiteSpace(sku) || baseSegment is null)
        {
            return null; // No SKU yet: keep the plain name-based segment rather than "name-".
        }

        // ToUrlSegment lowercases and strips unsafe characters, including "/": a segment must never
        // contain one, or it would become an extra level in the path.
        return $"{baseSegment}-{sku}".ToUrlSegment(_shortStringHelper, culture);
    }
}
