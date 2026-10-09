# Surface controllers (form posts)

Handle a form posted from a content page while staying inside Umbraco's page pipeline. On invalid
input, re-render the same page with validation errors; on success, redirect back to it. For page
rendering itself, use a [hijacked RenderController](route-hijacking.md) instead. Surface
controllers aren't for rendering widgets: that's a [view component](view-components.md).

## Building blocks (fetch the docs before implementing)

- **Creating, routing, antiforgery, `ValidateUmbracoFormRouteString`:** [Surface Controllers](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/surface-controllers.md)
- **`CurrentUmbracoPage`, `RedirectToCurrentUmbracoPage`, `RedirectToUmbracoPage`:** [Surface Controller Actions](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/surface-controllers/surface-controllers-actions.md)

## Docs corrections (Umbraco 17)

| Docs snippet | Problem | Use instead |
|---|---|---|
| `[ValidateUmbracoFormRouteString]` with the listed usings | Doesn't compile (CS0246) | Add `using Umbraco.Cms.Web.Common.Filters;` |
| Async action named `SomethingAsync` | "Could not find a Surface controller route" | Drop the `Async` suffix from the action name |

## The files

Verified templates in [`../assets/`](../assets):

- `ContactFormController.cs`: validates, uses Post/Redirect/Get, and passes a TempData success flag.
- `ContactFormModel.cs`: DataAnnotations-validated input.
- `contactForm.cshtml`: the form partial (`Views/Partials/contactForm.cshtml`).

## Steps

1. **Discover project context:** the root namespace and folder conventions. Confirm
   `_ViewImports.cshtml` has `@using Umbraco.Extensions` (for `BeginUmbracoForm`) and the MVC tag
   helpers (for `asp-for`).
2. **Write the model and controller** from the assets, replacing `<Namespace>`. The controller must
   be public, inside a namespace, end in `Controller`, and call the `SurfaceController` base
   constructor. Breaking any of these gives a 404 or a missing-route error.
3. **Write the form partial.** `Html.BeginUmbracoForm<TController>(nameof(TController.Action))`
   posts to the *current page's* URL with an encrypted `ufprt` route token and an antiforgery
   token. Don't add `@Html.AntiForgeryToken()`. If the form is posted by `fetch`/AJAX, send the
   token in a `RequestVerificationToken` header.
4. **Handle the post:**
   - Invalid → `return CurrentUmbracoPage();`. This re-renders in place, keeping ModelState, so
     errors and the typed values show.
   - Valid → do the work, set `TempData`, then `return RedirectToCurrentUmbracoPage();`
     (Post/Redirect/Get, so a refresh can't resubmit).
   - Keep `[ValidateUmbracoFormRouteString]` so the action is reachable only through
     `BeginUmbracoForm`, not through `/umbraco/surface/<controller>/<action>`.
   - Never save an `IContent` per submission. Store submissions in a custom table, or email them
     (see the `umbraco-common-pitfalls` skill).
5. **Render it** from any template: `<partial name="contactForm" model="new ContactFormModel()" />`.
6. **Verify:** submit an invalid form and expect inline errors; submit a valid one and expect a
   redirect plus the success message once.

## Other controller types

| Type | Base class | Use for |
|---|---|---|
| Public API | ASP.NET Core `Controller`/`ControllerBase` with `[Route]` (`UmbracoApiController` was removed in v15) | JSON for your own front-end JS: [Umbraco API Controllers](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/umbraco-api-controllers.md) |
| Backoffice / Management API | `ManagementApiControllerBase` | Backoffice extensions: out of scope for this skill |

## Done

Tell the user where the controller, model and partial live, and the one-line `<partial>` to drop
into a template. Remind them to replace the stand-in work in `Submit`.
