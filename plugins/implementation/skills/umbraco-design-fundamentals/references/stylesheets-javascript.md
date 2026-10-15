# Stylesheets and JavaScript

Stylesheets and scripts are static files under `wwwroot`, referenced once from the master layout. The
layout itself comes from the sibling skill `umbraco-templating`.

**Docs (Umbraco 17, fetch before implementing):** [Stylesheets and JavaScript](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/design/stylesheets-javascript.md) · [RTE Style Menu](https://docs.umbraco.com/umbraco-cms/17.latest/model-your-content/property-editors/built-in-umbraco-property-editors/rich-text-editor/style-menu.md)

## Where the files go

| | Default folder | Served at | Backoffice folder setting |
|---|---|---|---|
| Stylesheets | `wwwroot/css` | `/css/…` | `UmbracoCssPath` |
| Scripts | `wwwroot/scripts` | `/scripts/…` | `UmbracoScriptsPath` |

Any path under `wwwroot` is served, but the backoffice only lists the two folders above.

## Stylesheets

Add one `<link>` in the layout's `<head>`:

```html
<link rel="stylesheet" href="/css/site.css" />
```

`@Url.Content("~/css/site.css")` works too.

## JavaScript

Add one `<script>` just before `</body>`:

```html
<script src="/scripts/site.js"></script>
```

## Both

- **Only some pages need it?** Render a named section in the layout's `<head>`
  (`@RenderSection("Head", false)`) and fill it from that page's template.
- **Third-party files** (Bootstrap, Tailwind output, …): put the built files under `wwwroot`, or link a
  CDN, and reference them from the layout in the same way.
- **Created in the backoffice:** Settings → Stylesheets (or Scripts) → ⋯ → Create → Stylesheet file
  / JavaScript file.
- **Added to the folder outside the backoffice:** they appear after ⋯ → Reload children on that
  folder.
- **Bundling or minifying:** the docs don't cover a built-in option. Use the project's own front-end
  build (npm, Vite, webpack, …) and reference its output files from `wwwroot`.

## Rich Text Editor styles

The editor doesn't pick up site CSS automatically. Editor styles come from the RTE **Style Menu**
configuration (docs link above). The site CSS is still needed for the front end.

## Steps

1. Check for an existing `wwwroot/css`, `wwwroot/scripts` and a layout `<head>`.
2. Add the files and reference them once, in the layout.
3. Build and run if possible. Otherwise say it wasn't verified.

## Done

Give the file paths and the URLs they're served from (`/css/…`, `/scripts/…`).
