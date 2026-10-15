using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using <Namespace>.Mapping;
using <Namespace>.Models;

namespace <Namespace>.Controllers;

/// <summary>
/// Looks up published content by key and returns it mapped to a view model — written so that every
/// "might be null" Umbraco API is handled explicitly, with no null-forgiving operator (`!`).
/// </summary>
[ApiController]
[Route("<RoutePrefix>/{id:guid}")]
public sealed class NullSafeContentController : ControllerBase
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly NullSafeContentMapper _mapper;

    public NullSafeContentController(IUmbracoContextAccessor umbracoContextAccessor, NullSafeContentMapper mapper)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<NullSafeContentViewModel> Get(Guid id)
    {
        // TryGetUmbracoContext's out parameter is [MaybeNullWhen(false)]: inside this `if (!...)` it
        // may be null, and after the early return the compiler treats `umbracoContext` as non-null.
        // Use the Try* form rather than the throwing GetRequiredUmbracoContext(): there is no Umbraco
        // context while the runtime is installing/upgrading, or on a background thread.
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // IUmbracoContext.Content is non-nullable in v17, but GetById returns IPublishedContent? —
        // null for an unknown key AND for content that exists but is unpublished. Both are a 404.
        IPublishedContent? content = umbracoContext.Content.GetById(id);
        if (content is null)
        {
            return NotFound();
        }

        // `content` is narrowed to non-null from here on; the mapper owns property-level nulls.
        return _mapper.Map(content);
    }
}
