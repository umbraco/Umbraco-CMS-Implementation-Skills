# Templates, layouts and partial views

Markup for content pages. Choose a **template** for a Document Type's page, a **master layout**
for the shared shell, and a **partial view** for markup reused across templates. If the reusable
piece needs logic or services, use a [view component](view-components.md). If the page needs
data that isn't on the node, [hijack the route](route-hijacking.md).

## Building blocks (fetch the docs before implementing)

- **Templates, master templates, named sections:** [Working with Templates](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/templates.md)
- **View base class, `@Model`/`@Umbraco`, member data:** [Working with MVC Views](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/templating/mvc/views.md)
- **Rendering values, typed values, fallbacks:** [Rendering Content](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/design/rendering-content.md)
- **Partial views, locations, `CachedPartialAsync`:** [Using MVC Partial Views](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/templating/mvc/partial-views.md)

## Docs corrections (Umbraco 17)

| Docs snippet | Problem | Use instead |
|---|---|---|
| `Model.Value<IHtmlString>("bodyContent")` | `IHtmlString` doesn't exist in ASP.NET Core | `Model.Value<IHtmlEncodedString>(…)` (`@using Umbraco.Cms.Core.Strings`) |
| `Members.IsLoggedIn()`, `Members.GetCurrentMemberProfileModel()` | The `Members` helper was removed | `@inject IMemberManager` (`Umbraco.Cms.Core.Security`) |
| `@Html.Partial("ChildItem", page)` | Synchronous; analyzer MVC1000 | `<partial name="ChildItem" model="page" />` or `@await Html.PartialAsync(…)` |
| `@Model` "of type `Umbraco.Web.Mvc.ContentModel`" | v8 type | The view's `UmbracoViewPage<T>` model is the content (or your view model) |
| Partial with `@model MyModel` | Loses `@Umbraco` / `@UmbracoContext` | `@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<MyModel>` |

## Steps

1. **Discover project context.**
   - `Umbraco.Cms` version: 17.x.
   - ModelsBuilder mode (`Umbraco:CMS:ModelsBuilder:ModelsMode`). With `Nothing`, there are no
     generated types: use `UmbracoViewPage` + `Value<T>("alias")`. Otherwise use
     `UmbracoViewPage<GeneratedType>`.
   - `Views/_ViewImports.cshtml`: it must have `@using Umbraco.Extensions` and
     `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`.
   - The existing master layout's file name and how it's typed.
2. **Write the template** at `Views/<templateAlias>.cshtml`:
   - `@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<T>`.
   - `Layout = "<master>.cshtml"` (or `null` for a standalone page). Child templates fill the
     master's `@RenderBody()`, and `@section Name { }` fills `@RenderSection("Name", required: false)`.
   - Property values: `@Model.PropertyName`, or `@(Model.Value<T>("alias"))`. Use
     `fallback: Fallback.ToDefaultValue, defaultValue: …` for empty values, `Fallback.ToAncestors`
     to inherit from the tree, and `Fallback.ToLanguage` for variants.
3. **Register the template.** Umbraco only uses templates it has in its database. The `.cshtml`
   file name must equal the template alias, and the template must be **allowed** on the Document
   Type and set as its **default** (or chosen on the node). Use the
   [Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp) if it's
   connected. Otherwise tell the user: *Settings → Templates → Create* with the same name (Umbraco
   picks up the existing file's content), then *Document Type → Templates → allow + set default*.
   Without this step the page returns 404, or keeps rendering its old template.
4. **Partials** go in `Views/Partials/<name>.cshtml` and are rendered with
   `<partial name="<name>" model="…" />`. Type them with `@inherits UmbracoViewPage<T>`, where `T`
   is the model passed in. Pass an `IPublishedContent` to reuse a partial across items. Use
   `CachedPartialAsync(name, model, TimeSpan, cacheByPage, cacheByMember, viewData,
   contextualKeyBuilder)` only for output that is identical for every request sharing a key. The
   first render's output is served to every later request with that key, whatever their model.
   Pass `cacheByPage`/`cacheByMember` whenever the output varies by page or member.
   Caching is disabled in debug and cleared on publish.

## Done

Tell the user which files were added, the backoffice/MCP step that registers or assigns the
template (if it wasn't done for them), and which URL to load to see it.
