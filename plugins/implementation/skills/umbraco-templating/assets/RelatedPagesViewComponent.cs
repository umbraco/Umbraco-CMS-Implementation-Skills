using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace <Namespace>.Components;

/// <summary>
/// Reusable "related pages" widget: lists published, visible siblings of the same Document Type.
///
/// A view component rather than a partial because it owns logic (and can take constructor-injected
/// services); a partial should only format the model it is handed. The page is passed IN by the
/// caller instead of being looked up from IUmbracoContextAccessor, so the component works anywhere —
/// a template, a layout, a block — and always renders against the content it was given.
///
/// View: Views/Shared/Components/RelatedPages/Default.cshtml
/// Invoke: @await Component.InvokeAsync(typeof(RelatedPagesViewComponent), new { page = Model, maxItems = 3 })
/// </summary>
public class RelatedPagesViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IPublishedContent page, int maxItems = 3)
    {
        IPublishedContent[] related = page
            .Siblings()
            .Where(x => x.ContentType.Alias == page.ContentType.Alias && x.IsVisible())
            .Take(maxItems)
            .ToArray();

        // Default.cshtml is typed UmbracoViewPage<IPublishedContent[]>; an empty array lets the view decide
        // whether to render nothing.
        return View(related);
    }
}
