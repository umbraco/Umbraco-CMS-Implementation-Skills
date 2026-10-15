using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Acme.Web.Models;

public class ProductListingViewModel : PublishedContentWrapped
{
    public ProductListingViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    public IReadOnlyList<ProductSummary> Products { get; init; } = [];

    public IReadOnlyDictionary<string, int> StockLevels { get; init; } = new Dictionary<string, int>();
}

public record ProductSummary(string Name, string Url, string Category, int SameCategoryCount);
