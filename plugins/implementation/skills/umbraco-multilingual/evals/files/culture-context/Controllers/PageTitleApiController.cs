using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Acme.Web.Controllers;

/// <summary>
/// Returns a page's title as JSON for the front-end widgets.
/// Called from the German site as GET /api/page-title?key={guid}&amp;culture=de-DE, but the title that
/// comes back is always the English one. Normalising the culture (below) did not change that; the
/// response now also reports which culture was used, and it says en-US for every request.
/// </summary>
[ApiController]
[Route("api/page-title")]
public class PageTitleApiController : ControllerBase
{
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ILanguageService _languageService;

    public PageTitleApiController(
        IPublishedContentQuery publishedContentQuery,
        IUmbracoContextAccessor umbracoContextAccessor,
        ILanguageService languageService)
    {
        _publishedContentQuery = publishedContentQuery;
        _umbracoContextAccessor = umbracoContextAccessor;
        _languageService = languageService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid key, [FromQuery] string? culture)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out _))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Umbraco is not ready.");
        }

        IPublishedContent? content = _publishedContentQuery.Content(key);
        if (content is null)
        {
            return NotFound();
        }

        // Normalise the culture to be safe. The new widget sends "de-DE", the old one sent "de-de" and
        // we have seen "DE-DE" in the logs, so lowercase everything and they all look the same.
        string lowered = (culture ?? string.Empty).ToLowerInvariant();

        // Only accept a culture that is actually set up as a language in the backoffice.
        IEnumerable<ILanguage> languages = await _languageService.GetAllAsync();
        ILanguage? language = languages.FirstOrDefault(l => l.IsoCode == lowered);

        // Fall back to the default language so the endpoint never 400s.
        language ??= languages.First(l => l.IsDefault);

        return Ok(new PageTitleResponse(
            content.Key,
            language.IsoCode,
            content.Name,
            content.Value<string>("title"),
            content.Url()));
    }

    public record PageTitleResponse(Guid Key, string Culture, string? Name, string? Title, string Url);
}
