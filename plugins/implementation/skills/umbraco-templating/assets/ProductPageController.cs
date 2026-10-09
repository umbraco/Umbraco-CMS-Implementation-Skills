using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using <Namespace>.Models;

namespace <Namespace>.Controllers;

/// <summary>
/// Route hijack: Umbraco sends every request for a page whose Document Type alias is "productPage"
/// here instead of to its default RenderController. The class name IS the binding —
/// "[DocumentTypeAlias]Controller" — so rename it to match the alias being hijacked
/// (e.g. "blogPost" → BlogPostController). No registration or route attribute is needed.
/// </summary>
public class ProductPageController : RenderController
{
    private readonly IPublishedValueFallback _publishedValueFallback;

    public ProductPageController(
        ILogger<ProductPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
        => _publishedValueFallback = publishedValueFallback;

    // The base class's synchronous Index() must be taken out of action selection, or MVC finds two
    // "Index" candidates and throws AmbiguousMatchException. Sealing it also stops a subclass
    // reintroducing it by accident.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    // Query-string values bind straight onto action parameters (?page=2).
    public async Task<IActionResult> Index([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        // Stand-in for real async work (an external stock API, a database lookup …). Remove or replace.
        string stockStatus = await GetStockStatusAsync(cancellationToken);

        var model = new ProductPageViewModel(CurrentPage!, _publishedValueFallback)
        {
            PageNumber = Math.Max(1, page),
            StockStatus = stockStatus,
        };

        // CurrentTemplate renders the page's assigned template (or ?altTemplate=…) with this model.
        // The template must declare @inherits UmbracoViewPage<ProductPageViewModel>.
        return CurrentTemplate(model);
    }

    private static Task<string> GetStockStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult("In stock");
}
