using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using <Namespace>.Models;

namespace <Namespace>.Controllers;

/// <summary>
/// Surface controller handling the contact form posted from any Umbraco page.
///
/// Rendered with Html.BeginUmbracoForm&lt;ContactFormController&gt;(nameof(Submit)), so the form posts
/// back to the CURRENT page's URL with an encrypted "ufprt" route token, and Umbraco dispatches it here.
/// Surface controllers validate the antiforgery token automatically.
///
/// Rules that cause silent 404s or "Could not find a Surface controller route" errors:
///   - the class must be public, inside a namespace, and end in "Controller";
///   - don't suffix the action with "Async" (MVC strips it from action names by default).
/// </summary>
public class ContactFormController : SurfaceController
{
    public ContactFormController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
    }

    public const string SuccessKey = "ContactFormSuccess";

    [HttpPost]
    // Only reachable through BeginUmbracoForm, not via the auto-route /umbraco/surface/contactform/submit.
    [ValidateUmbracoFormRouteString]
    public async Task<IActionResult> Submit(ContactFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Re-renders the current page WITH ModelState, so the form shows the validation errors and
            // keeps what the visitor typed. No redirect — a redirect would lose both.
            return CurrentUmbracoPage();
        }

        // Do the real work here (send email, store in a custom table …) — never IContentService.Save
        // per submission. Replace this stand-in.
        await Task.CompletedTask;

        // Post/Redirect/Get: TempData survives exactly one redirect, so refresh can't resubmit.
        TempData[SuccessKey] = true;
        return RedirectToCurrentUmbracoPage();
    }
}
