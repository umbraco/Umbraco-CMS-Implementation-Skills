using Microsoft.AspNetCore.Mvc;
using <Namespace>.Services;

namespace <Namespace>.Controllers;

// Constructor injection; same in Render/SurfaceController (umbraco-templating). Views: @inject.
[Route("<routePrefix>")]
public class SiteServiceController : ControllerBase
{
    private readonly ISiteService _siteService;

    public SiteServiceController(ISiteService siteService) => _siteService = siteService;

    [HttpGet("site-name")]
    public IActionResult GetSiteName()
    {
        string? name = _siteService.GetSiteRoot()?.Name;
        return name is null ? NotFound() : Content(name);
    }
}
