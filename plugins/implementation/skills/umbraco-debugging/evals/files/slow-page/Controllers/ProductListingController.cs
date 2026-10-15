using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;
using Acme.Web.Models;

namespace Acme.Web.Controllers;

/// <summary>
/// Route hijack for the "productListing" Document Type (/products). Takes 3-4 seconds to render.
/// </summary>
public class ProductListingController : RenderController
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public ProductListingController(
        ILogger<ProductListingController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
        => _publishedValueFallback = publishedValueFallback;

    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IPublishedContent listing = CurrentPage!;

        // Every product on the site, wherever an editor put it.
        IPublishedContent[] products = listing.Root()
            .DescendantsOrSelf()
            .Where(x => x.ContentType.Alias == "product" && x.IsVisible())
            .ToArray();

        var summaries = new List<ProductSummary>();
        foreach (IPublishedContent product in products)
        {
            string category = product.Value<string>("category") ?? string.Empty;

            // "12 more in Outdoor" badge.
            int sameCategory = products.Count(p =>
                p.Id != product.Id
                && string.Equals(p.Value<string>("category"), category, StringComparison.OrdinalIgnoreCase));

            summaries.Add(new ProductSummary(product.Name, product.Url(), category, sameCategory));
        }

        Dictionary<string, int> stock = await GetStockLevelsAsync(
            products.Select(p => p.Value<string>("sku") ?? string.Empty),
            cancellationToken);

        var model = new ProductListingViewModel(listing, _publishedValueFallback)
        {
            Products = summaries,
            StockLevels = stock,
        };

        return CurrentTemplate(model);
    }

    // Stand-in for the warehouse stock API, which is called once per page view.
    private static async Task<Dictionary<string, int>> GetStockLevelsAsync(
        IEnumerable<string> skus,
        CancellationToken cancellationToken)
    {
        await Task.Delay(1200, cancellationToken);
        return skus.Distinct().ToDictionary(sku => sku, _ => 5);
    }
}
