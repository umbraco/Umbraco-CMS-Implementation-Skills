# Route hijacking (custom RenderController)

Replace Umbraco's default `RenderController` for every page of one Document Type, so you control
what model the template gets. Use it when the page needs data that isn't on the node: an external
API, server-side paging from the query string, per-request logic, or a different action per
template. If the view only needs content, a [template](views-and-partials.md) or
[view component](view-components.md) can query it without one.

## Building blocks (fetch the docs before implementing)

- **Convention, custom view models, alt templates:** [Custom MVC Controllers (Umbraco Route Hijacking)](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/custom-controllers.md)
- **Replacing the default controller for all pages:** [Controller & Action Selection](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/application-code/backend-and-custom-logic/routing/controller-selection.md)

## Docs corrections (Umbraco 17)

| Docs snippet | Problem | Use instead |
|---|---|---|
| Query-string `Index(int page, …)` added next to the inherited `Index()` | **`AmbiguousMatchException`** at request time (verified) | `[NonAction] public sealed override IActionResult Index()`, then your own `Index(…)` |
| `new PublishedValueFallback(_serviceContext, _variationContextAccessor)` | Hand-builds a service | Inject `IPublishedValueFallback` |
| "Controller Injection" constructor with no `: base(...)` | Doesn't compile | Always chain `: base(logger, compositeViewEngine, umbracoContextAccessor)` |
| `Model.Value<IHtmlString>(…)` in the template | Type doesn't exist | `IHtmlEncodedString` |
| `IUserComposer` | Removed | `IComposer` |

## The files

Verified templates in [`../assets/`](../assets):

- `ProductPageController.cs`: hijacks Document Type alias `productPage`. It has an async `Index`,
  query-string binding, and builds the view model.
- `ProductPageViewModel.cs`: `PublishedContentWrapped`, plus the controller-computed properties.
- `productPage.cshtml`: the template, typed to the view model.

## Steps

1. **Discover project context:**
   - The root namespace and the Document Type alias to hijack.
   - The template alias(es) assigned to that Document Type.
   - The ModelsBuilder mode.
   - How the master layout is typed.
   - Folder conventions (`Controllers/`, `Models/`).
2. **Name the controller after the alias.** `productPage` → `ProductPageController`, `blogPost` →
   `BlogPostController`. The class name is the whole binding: no route attribute, no registration.
   A mismatched name silently does nothing, and Umbraco's default controller keeps rendering the page.
3. **Write the view model.** Derive from `PublishedContentWrapped`. Or, when ModelsBuilder
   generates the type, derive from the generated model (e.g. `ProductPage`) so the template keeps
   its typed properties. The constructor only passes through to `base`. The controller sets the extra properties.
4. **Write the controller** from the asset, replacing `<Namespace>` and renaming `ProductPage`.
   Keep the `[NonAction] sealed override Index()`: any `Index` with a different signature needs it.
   `RenderController` has no `Logger` property, so to log, store the `ILogger<YourController>`
   you pass to `base` in your own field.
   An action named after a template alias (e.g. `ProductAmpPage()`) handles requests rendered with
   that template, including `?altTemplate=productAmpPage`.
5. **Update the template's `@inherits`** to `UmbracoViewPage<ProductPageViewModel>`. If the master
   layout is typed to a specific model, retype it (see *Best practices* in SKILL.md).
6. **Optional: replace the default for all pages.** Configure
   `UmbracoRenderingDefaultsOptions.DefaultControllerType` in an `IComposer`. This is rarely needed;
   prefer per-type hijacks.
7. **Verify:** request a page of that type and confirm the controller-only values render.

When website output caching is enabled, hijacked controllers inherit it. Add
`[OutputCache(NoStore = true)]` to the action if the output varies per request (e.g. it reads
the query string).

## Done

Tell the user:

- Which Document Type is hijacked.
- Where the controller, view model and template live.
- That the template's `@inherits` changed, plus the layout change if one was needed.
- A URL to test, e.g. `/products/x/?page=2`.
