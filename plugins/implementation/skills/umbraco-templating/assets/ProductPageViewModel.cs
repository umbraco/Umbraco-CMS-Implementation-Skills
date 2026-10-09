using Umbraco.Cms.Core.Models.PublishedContent;

namespace <Namespace>.Models;

/// <summary>
/// View model for the hijacked ProductPage route. Wraps the current page so every IPublishedContent
/// member (Name, Url(), Value("alias"), Children() …) still works in the template and its layout, and
/// adds the request-specific data the template can't get from content alone.
///
/// Rename "ProductPage" to the PascalCase alias of the Document Type being hijacked.
/// </summary>
public class ProductPageViewModel : PublishedContentWrapped
{
    // Umbraco 17. On Umbraco 18+ PublishedContentWrapped takes only (IPublishedContent content):
    // drop the fallback parameter there or the base call fails to compile (CS1729).
    public ProductPageViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    // The constructor only passes through to base; the controller fills these in, so building the
    // model never triggers traversal or property conversion on its own.
    public int PageNumber { get; init; } = 1;

    public string? StockStatus { get; init; }
}
