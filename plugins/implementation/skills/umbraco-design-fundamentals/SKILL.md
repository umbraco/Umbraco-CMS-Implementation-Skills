---
name: umbraco-design-fundamentals
description: >
  The look of a server-rendered Umbraco 17 site: CSS/JS in wwwroot, rendering content (typed
  values, fallbacks, child pages) and media (images, crops, resizing, video) in Razor, and a guided
  path for a whole design. Use when asked to "add CSS/JS", "add Bootstrap/Tailwind", "show an
  image", "render a crop", "resize an image", "list child pages", "fall back to the parent's
  value", or by Umbraco newcomers: "create a design for my Umbraco site", "style my site", "use my
  HTML template/theme", "change the colours/fonts", "same header and footer on every page", "build
  a demo site". Generic phrases need Umbraco context (an Umbraco project, or Umbraco mentioned).
  SKIP: templates, layouts, partials or controllers on their own (use umbraco-templating);
  non-Umbraco projects; mockups with no Umbraco site (Figma, slides); Umbraco < 17; headless
  frontends; sitemaps (umbraco-sitemap); 404/500 pages (umbraco-custom-error-pages).
---

# Design Fundamentals

How an Umbraco site gets its look: **stylesheets and JavaScript** under `wwwroot`, and Razor that
**renders content and media** from the backoffice. Page structure (templates, master layouts,
partial views) belongs to the sibling skill `umbraco-templating`. This skill links to it and
doesn't repeat it, and it also walks newcomers through building a whole design.

## Load only what the task needs

- **Don't read the whole `references/` folder.** Open only the reference for the step you're on,
  when you start it. For example, "add a stylesheet" needs only `stylesheets-javascript.md`.
- Inside a reference, **fetch only the docs page for the step you're on.**
- Each reference stands alone. Its cross-links are "see also", not prerequisites.
- Ask the docs agent (below) only when the reference and the versioned doc don't answer the question.

## Step 0: check the version

This skill targets **Umbraco 17**, and its doc links are the `17.latest` pages. Read the
`Umbraco.Cms` package version from the `.csproj` or `Directory.Packages.props`. If the site is below
17, this skill doesn't apply: say so. If no project or version is available, ask the user rather
than guessing.

## Routing table

| Need | Where |
|---|---|
| Stylesheets, scripts, front-end libraries, RTE styles | [stylesheets-javascript.md](references/stylesheets-javascript.md) |
| Property values, typed values, fallbacks, child pages / traversal | [rendering-content.md](references/rendering-content.md) |
| Images, crops, resizing, video, files | [rendering-media.md](references/rendering-media.md) |
| Templates, master layouts, named sections, partial views, view components, controllers | Sibling skill **`umbraco-templating`**. If it isn't installed, fetch [Working with Templates](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/templates.md). |

## Building a whole design (for people new to Umbraco)

When someone asks for "a design", "a demo site" or "the same header and footer on every page", do
it in this order, opening each piece only when you reach it:

1. **Page structure.** Build a master layout with `@RenderBody()`, a page template, and header and
   footer partials, following `umbraco-templating` (or the docs page above).
2. **Look.** Add the stylesheet and reference it once, from the layout:
   [stylesheets-javascript.md](references/stylesheets-javascript.md).
3. **Content and images.** Render what editors type and pick:
   [rendering-content.md](references/rendering-content.md),
   [rendering-media.md](references/rendering-media.md).
4. **Backoffice steps code can't do.** Tell the user to create the Document Types, register each
   template (alias = file name) and allow it on its Document Type, and publish content.

Explain each Umbraco term in one plain sentence the first time it comes up:

| They say | Umbraco term |
|---|---|
| "the page design / how a page looks" | Template |
| "same header and footer on every page" | Master layout + partial views |
| "a reusable block of HTML", "a menu" | Partial view |
| "my CSS / colours / fonts / scripts" | Stylesheet / script in `wwwroot` |
| "the form editors fill in" | Document Type |
| "show what I typed in the backoffice" | Rendering a property value |

## Docs first, and the docs agent

Don't write Umbraco API code from memory. Fetch the versioned `.md` page linked in the reference
first. If that doesn't answer the question, query the GitBook docs agent on the same page:

```
GET <versioned page>.md?ask=<specific question>&goal=<what the user is trying to achieve>
```

## Best practices

- **IDE and source control over backoffice edits.** The backoffice is fine for quick local
  changes, but ongoing work belongs in files under source control, and edits on a deployed site can
  be overwritten by the next deploy. See
  [Managing Views and Assets](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/managing-views-and-assets.md).
- **Reference site-wide CSS/JS once, in the master layout,** not per template.
- **Prefer ModelsBuilder typed properties** over `Value("alias")` strings when ModelsBuilder is on.
  Null-check media and picked content.
- **Keep views thin.** Render; don't query services or do heavy traversal in Razor.

## Coverage

**Documented.** This skill ships no code (no `assets/`, no `examples/`), so nothing runs in the
`dotnet test` gate. Evals grade the guidance only.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json). Run them with
`umbraco-skill-evaluator`.
