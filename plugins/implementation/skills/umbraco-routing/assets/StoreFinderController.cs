using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace <Namespace>.Controllers;

/// <summary>
/// A virtual page: /stores/{city} has no node per city. The data comes from elsewhere, but the page
/// still renders inside Umbraco, with the "Stores" landing node as its CurrentPage, so the layout,
/// navigation and the landing page's own properties all work.
///
/// The [Route] attribute is the whole registration: Umbraco maps attribute-routed controllers, and
/// IVirtualPageController makes it call FindContent before the action runs. If FindContent returns
/// null the request ends in a plain 404 and the action never runs.
/// </summary>
[Route("<VirtualRoutePrefix>/{city}")]
public class StoreFinderController : UmbracoPageController, IVirtualPageController
{
    private const string LandingDocTypeAlias = "<LandingDocTypeAlias>";

    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public StoreFinderController(
        ILogger<UmbracoPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IDocumentNavigationQueryService navigation,
        IPublishedUrlProvider urlProvider,
        IPublishedValueFallback publishedValueFallback)
        : base(logger, compositeViewEngine)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _navigation = navigation;
        _urlProvider = urlProvider;
        _publishedValueFallback = publishedValueFallback;
    }

    /// <summary>Finds the landing node by Document Type, never by a hardcoded id.</summary>
    public IPublishedContent? FindContent(ActionExecutingContext actionExecutingContext)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext)
            || umbracoContext.Content is null
            || !_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        List<IPublishedContent> landingPages = [];
        foreach (Guid rootKey in rootKeys)
        {
            if (_navigation.TryGetDescendantsKeysOfType(rootKey, LandingDocTypeAlias, out IEnumerable<Guid> keys))
            {
                landingPages.AddRange(keys.Select(umbracoContext.Content.GetById).OfType<IPublishedContent>());
            }
        }

        // Multi-site: prefer the landing page on the site whose domain the request came in on.
        string host = actionExecutingContext.HttpContext.Request.Host.Host;
        return landingPages.FirstOrDefault(page =>
                   Uri.TryCreate(_urlProvider.GetUrl(page, UrlMode.Absolute), UriKind.Absolute, out Uri? url)
                   && url.Host.Equals(host, StringComparison.OrdinalIgnoreCase))
               ?? landingPages.FirstOrDefault();
    }

    [HttpGet]
    public IActionResult Index(string city)
    {
        // CurrentPage is the node FindContent returned. Fetch the city's data from your own service
        // here and return NotFound() for a city it doesn't know.
        var model = new StoreFinderCityPage(CurrentPage!, _publishedValueFallback)
        {
            City = city,
        };

        // Resolved from Views/storeFinderCity.cshtml.
        return View("storeFinderCity", model);
    }
}

/// <summary>The landing node plus the values only this route knows.</summary>
public class StoreFinderCityPage : PublishedContentWrapped
{
    public StoreFinderCityPage(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    public required string City { get; init; }
}
