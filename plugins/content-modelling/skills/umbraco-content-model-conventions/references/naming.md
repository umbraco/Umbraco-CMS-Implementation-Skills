# Names, aliases and suffixes

**Applies to:** every document type, element type, composition and data container.
**Look for:** a page type with no `Page` suffix, a composition with no `Composition` suffix,
an acronym left in capitals inside an alias (`SEOComposition`, `xMLSitemapPage`).
**Why:** on a project with forty or more types, the suffix is the fastest way to tell what a thing
is. "Everything ending in `Composition` is a composition" is learned in thirty seconds.

## Casing

- **Name**: Title Case, with spaces, as an editor reads it: `Article Page`, `SEO Composition`.
  Acronyms stay upper case in the name.
- **Alias**: camelCase with acronyms collapsed to a single capital: `articlePage`,
  `seoComposition`, `xmlSitemapPage`. Never two capitals in a row. Type the alias yourself;
  Umbraco's generated alias for "XML Sitemap Page" is `xMLSitemapPage`.

## Suffixes

| Kind | Suffix | Example alias |
|---|---|---|
| Page (has a template) | `...Page` | `articlePage`, `homePage` |
| Listing page and its item | `...ListingPage`, item `...Page` | `articleListingPage` lists `articlePage` |
| Composition for document types | `...Composition` | `seoComposition` |
| Composition for block settings | `...SettingsComposition` | `backgroundColourSettingsComposition` |
| Block settings model | `<block>Settings` | `accordionSettings` |
| Data container | `...Folder` | `authorFolder` |
| Data item | `...Item`, or the plain singular noun when it is a real-world thing | `reusableContentItem`, `author` |
| Child element of a repeater | `...Item` or `...Row` | `accordionItem`, `pricingRow` |
| Block element | plain noun | `accordion`, `richText` |
| Block element whose noun is taken | `...Block` | `imageBlock` (when `image` clashes) |

`...Block` is a last resort for a clash with a media type, a data type or a reserved word. It is
not the default suffix for elements.

The site settings singleton is `siteSettings`; it is the one root type without a `Page` suffix,
because it is not a page (it has no template).

## Data types

Data types have a name only, written in Title Case.

| Case | Pattern | Example |
|---|---|---|
| A generic type reused everywhere, varied by one setting | `<Editor> (<qualifier>)` | `Toggle (default on)` |
| A type built for one purpose | `<Subject> <Editor kind>` | `Meta Description Text Area` |
| A Block List behind a repeater | the plural of its child | `Accordion Items` for `accordionItem` |
| A Block Grid or block catalogue | `<Placement> Block Grid` | `Main Content Block Grid` |
| A listing's collection view | `<Type> Collection View` | `Article Listing Page Collection View` |

Reuse an existing data type when its configuration already matches; create a new one only when
the configuration differs. A constraint such as a character limit or a minimum and maximum number
of items belongs in the data type configuration, not in property validation, so that every
property using it behaves the same.

There is one block editor data type per placement. Two places that allow different blocks get two
data types, even if they overlap.

## Aside: the `HasSeo` prefix

Some teams name compositions `HasSeo` so that Models Builder produces `IHasSeo`, and an interface
check reads like English (`if (Model is IHasSeo page)`). It is a legitimate technique. It was
rejected here because `Has Seo` looks odd in the backoffice beside everything else, and the
convention is not obvious unless you already know how Models Builder surfaces compositions.
`seoComposition` still gives `ISeoComposition`. Mention the alternative only if the user asks
about Models Builder interfaces.

**Related:** [Property aliases](property-aliases.md), [Tree organisation](tree-organisation.md).
