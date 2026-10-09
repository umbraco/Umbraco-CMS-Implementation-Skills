---
name: umbraco-templating
description: >
  Server-rendered Umbraco 17 front-end: Razor templates and layouts, partial views, view
  components, route hijacking (RenderController + view model) and surface-controller form posts.
  Use when asked to "create a template", "write a Razor view for a document type", "render a
  property", "add a partial view", "build a view component" or reusable widget, "hijack a route",
  "add a custom controller for a document type", "pass a custom view model to a template",
  "server-side paging on a page", "handle a form post", "use BeginUmbracoForm", "create a surface
  controller", or "cache a partial" — or when a Razor view won't compile after copying a docs
  snippet (`IHtmlString`, `Html.Partial`, `Members.`), or a page 404s after adding a template.
  SKIP: headless / Content Delivery API front-ends; backoffice extensions and Management API
  controllers; custom routes outside the content tree or `IContentFinder`; ModelsBuilder
  configuration; member login/registration; non-Umbraco projects or Umbraco < 17.
---

# Templating

Umbraco renders every content page through ASP.NET Core MVC: a request is matched to a content
node, routed to a controller (Umbraco's `RenderController` unless you hijack it), and rendered by
the node's Template, a Razor view.

## Pick the building block

| Need | Use | Reference |
|---|---|---|
| Markup for a Document Type's pages, shared page shell | Template + master layout | [views-and-partials.md](references/views-and-partials.md) |
| Reusable markup that only formats the model it's given | Partial view | [views-and-partials.md](references/views-and-partials.md) |
| Reusable widget with its own logic or injected services | View component | [view-components.md](references/view-components.md) |
| Page needs data that isn't on the node (external API, paging, per-request logic), or a different view per template | Route hijacking: `RenderController` + view model | [route-hijacking.md](references/route-hijacking.md) |
| Accept a form POST from a content page | Surface controller | [surface-controllers.md](references/surface-controllers.md) |
| JSON / REST endpoint | Not a templating concern: plain ASP.NET Core controller | *Other controller types* in [surface-controllers.md](references/surface-controllers.md) |

### How to decide

Use the lightest block that does the job: template → partial → view component → hijacked
controller. Querying other content doesn't need a hijack: a template or view component can do it.
A single page often combines several blocks:
[`assets/productPage.cshtml`](assets/productPage.cshtml) is a hijacked template that invokes a view
component and renders a form partial.

A new template renders only once it's registered in Umbraco and assigned to the Document Type,
otherwise the page 404s: see step 3 of [views-and-partials.md](references/views-and-partials.md).

Finish every building block with `dotnet build` and a request to a page that exercises it; if no
runnable project is available, say so rather than claiming it works.

## Version compatibility

Targets **Umbraco 17**. The `assets/` compile and render on `Umbraco.Cms` 17.5.3. On Umbraco 18,
`PublishedContentWrapped` takes only `(IPublishedContent)`, so drop the fallback argument from the
view model.

Several snippets on the Umbraco 17 docs pages are v8-era and **do not compile or run** on 17.
References whose docs pages have broken snippets list them under *Docs corrections*. Trust the
assets over the docs where they disagree.

## Best practices

- **Views render, they don't fetch.** No `IContentService`/`IMediaService` in a view. Move logic
  into a view component or a hijacked controller. For performance traps (deep traversal, repeated
  `Root()`), see the sibling skill `umbraco-common-pitfalls`.
- **Async rendering only:** `<partial>`, `await Html.PartialAsync`, `await Component.InvokeAsync`.
  Never `Html.Partial`/`Html.RenderPartial`, which are sync-over-async and can deadlock (analyzer MVC1000).
- **Typed values.** Use `Value<T>("alias")`, or the generated ModelsBuilder property. Rich text
  converts to `IHtmlEncodedString`, so render it directly. Never `Html.Raw` an editor string.
- **Keep the master layout generically typed** (`UmbracoViewPage` or a shared interface, not `UmbracoViewPage<Home>`),
  or a hijacked page with a custom view model will throw when the layout binds.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Every asset is **Asserted** by the runtime gate
(`examples/render-pipeline/`, blank host): the hijacked route, query binding, property fallback,
the view component and both form-post paths are checked over HTTP.
