# Language switcher and hreflang (step 5)

Two partials ship in `assets/`. Copy them **verbatim** into `Views/Partials/`. Both are covered by the
runtime gate, which renders them on a two-language site and asserts their output.

| File | Renders | Where |
|---|---|---|
| [`assets/languageSwitcher.cshtml`](../assets/languageSwitcher.cshtml) | `<ul>` of links to the *current page* in each other published culture; the current one marked `aria-current` | Header / footer |
| [`assets/hreflangLinks.cshtml`](../assets/hreflangLinks.cshtml) | `<link rel="alternate" hreflang="…">` per published culture plus `x-default` | Inside `<head>` |

```cshtml
<head>
    @await Html.PartialAsync("hreflangLinks")
</head>
<body>
    @await Html.PartialAsync("languageSwitcher")
```

Both take the current page as their model (the default when called from a template) and inherit the
non-generic `UmbracoViewPage`, so they work with or without ModelsBuilder.

## Why not copy the tutorial's switcher

The [tutorial](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/multilanguage-setup.md)
switcher is a starting point with four defects. These partials fix all of them:

1. It throws when no home node of the named alias exists (`cultures.Count` on null).
2. It compares cultures with `ToLower()`; the partials use `OrdinalIgnoreCase`.
3. It lists every culture on the home page, and falls back to the home page URL when the current page
   isn't translated. The partials link each culture only where **this page** is published in it, so a
   visitor is never sent to a page that doesn't exist in their language.
4. It emits `hreflang` on `<a>` only. Search engines read `<link rel="alternate" hreflang>` in `<head>`
   with **absolute** URLs and an `x-default`, which the second partial provides.

## hreflang rules the partial follows

- One `<link>` per culture the page is **published** in, including the current page itself.
- Absolute URLs. With relative domains (`/da`) these come from the request host, so check the
  production host serves the page.
- `x-default` points at the default language's version (the language marked default in step 1). If the
  page isn't published in the default language, `x-default` is omitted rather than guessed.
- Culture codes are emitted as `da-DK` (BCP 47). Casing is normalised from `CultureInfo`, not taken
  from whatever case the cache stored.
- Invariant pages (types that don't vary by culture) emit nothing. There is only one version.

## Done

Tell the user where the partials were copied and which layout calls them. Explain that a page only
appears in a culture's switcher and hreflang once it's published in that culture and that culture has
a domain.
