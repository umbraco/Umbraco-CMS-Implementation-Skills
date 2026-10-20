# View components

A reusable widget that owns its own logic: a C# class deriving from `ViewComponent` plus a Razor
view. Choose this over a [partial view](views-and-partials.md) when the piece needs to query,
compute or inject services. A partial should only format the model it is handed. View components
replace MVC5 child actions and `[ChildActionOnly]` surface-controller actions.

## Building blocks (fetch the docs before implementing)

- **Umbraco's guide:** [Using View Components in Umbraco](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/templating/mvc/viewcomponents.md)
- **Invocation options, tag-helper syntax:** [View components in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/views/view-components)

## The files

Verified templates in [`../assets/`](../assets):

- `RelatedPagesViewComponent.cs`: lists visible, same-type siblings of the page passed in.
- `RelatedPages.Default.cshtml`: install at `Views/Shared/Components/RelatedPages/Default.cshtml`.

## Steps

1. **Discover project context:** the root namespace and the folder convention for components
   (`Components/`, `ViewComponents/`, or the project root).
2. **Write the class.** The name ends in `ViewComponent`, and the view folder is the name without
   that suffix. Replace `<Namespace>` and rename `RelatedPages` throughout. Inject services through
   the constructor. Use `InvokeAsync` returning `Task<IViewComponentResult>` when the work is async.
3. **Pass the content in.** Take an `IPublishedContent` (or a view model) as an `Invoke`
   parameter, rather than reading the current page from `IUmbracoContextAccessor` inside the
   component. The component then works in a template, a layout, or a Block List/Grid item view, and
   always renders the content it was given.
4. **Write the view** at `Views/Shared/Components/<Name>/Default.cshtml`. Type it with
   `@inherits UmbracoViewPage<T>`, where `T` is the type passed to `View(...)`. A bare `@model`
   fails to compile (CS1061) when `_ViewImports.cshtml` declares a non-generic base page.
   Add `@inject Umbraco.Cms.Web.Common.UmbracoHelper Umbraco` if it needs the helper.
5. **Invoke it:** `@await Component.InvokeAsync(typeof(RelatedPagesViewComponent), new { page = Model, maxItems = 3 })`.
   Invoking by type turns a rename into a compile error. Using a string name fails only at runtime.
   The `<vc:related-pages>` tag-helper form also works, but only after `@addTagHelper *, <AssemblyName>`
   is added to `_ViewImports.cshtml`.

## Done

Tell the user where the class and view live, and give them the one-line invocation to paste into
any template.
