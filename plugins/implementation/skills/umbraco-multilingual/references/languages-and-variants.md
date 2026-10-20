# Languages, culture variants and domains (steps 1–3)

This is the setup half of the official tutorial,
[Creating a Multilingual Site](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/multilanguage-setup.md).
Fetch it before walking the user through the backoffice, because it has the current screen names. For
what editors see once variants are on, fetch
[Language Variants](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/backoffice/variants.md).

Order of preference for every backoffice step:
1. the [Umbraco Developer MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp);
2. a manual walk-through, one step at a time;
3. for languages and dictionary items only, importing [`assets/multilingual-package.xml`](../assets/multilingual-package.xml).

## Contents

- [1. Languages](#1-languages): read when adding languages, or choosing the default and fallbacks.
- [2. Vary by culture](#2-vary-by-culture): read when making Document Types and properties vary,
  including blocks.
- [3. Culture and Hostnames](#3-culture-and-hostnames): read when assigning a domain per culture, or
  keeping or redirecting existing URLs.
- [Publishing variants from code](#publishing-variants-from-code): read when a migration, import or
  seeding job publishes variant content.
- [Note: a root per market](#note-a-root-per-market): read when markets differ in structure, not just
  wording.
- [Done](#done): read before reporting back to the user.

## 1. Languages

Settings → Languages → Create, once per language. For each one, decide:

| Setting | What it does | Guidance |
|---|---|---|
| **Default language** | The culture used when a request matches no domain, and the source for `Fallback.ToDefaultLanguage` | Exactly one. Usually the language the site launched in. |
| **Mandatory language** | A node can't be published in *any* culture until this culture has content | Only for languages that must never be missing. It blocks every other language's publishing. |
| **Fall back to language** | `Value(..., fallback: Fallback.ToLanguage)` reads this culture when the requested one is blank | Set it for every non-default language, normally to the default. |

**The package manifest only creates the language.** On import, Umbraco reads `CultureAlias` and
`FriendlyName` and nothing else, so default, mandatory and fallback must still be set afterwards, in the
backoffice or MCP or in code (`ILanguageService.UpdateAsync` after setting `FallbackIsoCode`). Say so
whenever you use the package. Otherwise fallback silently never happens.

The manifest's `<DictionaryItems>` values name their language with `LanguageCultureAlias`. That value
is matched **case-sensitively** against installed languages, and a value for a missing language is
dropped without an error. Import languages first, which the same package does, and keep ISO casing
exact.

If a language the user needs is missing from the dropdown, see
[additional-cultures.md](additional-cultures.md).

## 2. Vary by culture

- **Document Type:** Settings tab → *Allow vary by culture*. For Element Types used in blocks, see
  [Blocks](#blocks-same-blocks-translated-content) below.
- **Property:** each property on a varying type is variant by default. Mark it *Shared across
  cultures* (invariant) when the value is the same in every language, such as SKUs, dates, toggles and
  images with no text.
- A node's **name** is variant, so its **URL segment is per culture** (`/en/about-us/`, `/da/om-os/`).

Switching an existing property to vary by culture copies its current value into the **default
language**. Other languages start empty, so plan fallback. The reverse is destructive: switching a
property back to shared keeps **only the default language's** value and discards every other
language's. Warn before doing that, and try both on a copy of the database for large sites.

### Blocks: same blocks, translated content

Fetch [Block Level Variance](https://docs.umbraco.com/umbraco-cms/model-your-content/property-editors/built-in-umbraco-property-editors/block-editor/block-level-variance.md)
first. Block List, Block Grid and Rich Text blocks can vary in two ways:

| | **Block level variance** (default) | Block property varies by culture |
|---|---|---|
| Configure | Document Type varies; the **block property does not**; its **Element Types (and their text properties) do** | The block property itself varies |
| Structure | One shared list: same blocks, same order in every language | A separate list per language |
| Best for | The same page layout in every language, with translated content | Markets whose pages genuinely differ block by block |
| Risk | — | Lists drift apart: missing and reordered blocks |

Default to block level variance whenever pages should have the same blocks. The docs page covers
how it behaves (unexposed blocks, shared structure, invariant properties inside variant blocks); what
it does not say:

- A block added in one language is **unexposed** in the others until an editor edits it there, and
  unexposed blocks are left out of that language's published output. That is how a block stays
  English-only: add it in English and never edit it in Danish.
- **Rich Text blocks** work the same way: the markup stays shared and only the block content varies.
- **Who can change structure.** By default, editors who can only edit non-default languages can't
  change the shared structure. Set `Umbraco:CMS:Content:AllowEditInvariantFromNonDefault` to `true`
  to let them.
- **Migrating from a varying block property** means switching the property to shared, and that keeps
  **only the default language's** block list: every other language's blocks are discarded (see the
  warning above). Plan the translations back in as block variants, and rehearse on a copy of the
  database.

Rendering needs no extra code; see [rendering-variants.md](rendering-variants.md#blocks).

## 3. Culture and Hostnames

On the site root: Content → root node → **⋯ → Culture and Hostnames** → add one domain per culture.

| Domain form | Example | Use when |
|---|---|---|
| Absolute host | `example.com` → en-US, `example.dk` → da-DK | Each language has its own domain |
| Host + path | `example.com/en`, `example.com/da` | One domain, language in the path |
| Relative path | `/en`, `/da` | The same prefixes on every host (staging, local, production) |

Relative names like `/da` are valid and are resolved against the current request's host, which keeps
one configuration working across environments. A domain only routes when its culture is **published**
on that node. When no domain matches, Umbraco renders the default language.

### Existing URLs: keep them, or redirect them

Adding domains to a live site can move pages. If English moves from `/about-us/` to `/en/about-us/`,
every indexed URL, backlink and bookmark breaks. Umbraco's built-in redirect tracking won't catch
this, because it only records URL changes caused by publishing, renaming or moving content, not by
domain changes. Decide before adding domains:

- **Keep the default language at the root (preferred for an existing site).** Give the default culture
  the bare host and the new language a path: `example.com` → en-US, `example.com/da` → da-DK. Umbraco
  tries domains longest-first, so `/da/…` resolves as Danish and everything else stays English at its
  current URL. Nothing needs redirecting.
- **Move every language under a prefix** (`/en`, `/da`) only when the user wants symmetrical URLs.
  Then add permanent (301) redirects from each old unprefixed URL to its `/en/` equivalent,
  excluding `/umbraco` and static files. Use the URL Rewriting Middleware; fetch
  [URL Rewrites in Umbraco](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/routing/iisrewriterules.md)
  first. Update canonical tags, the sitemap and `robots.txt` to the new URLs.

A brand-new site has no URLs to preserve, so either layout works.

Domains are **not** part of a package manifest. Configure them in the backoffice or MCP, or in code
with `IDomainService.UpdateDomainsAsync(contentKey, new DomainsUpdateModel { Domains = [...] })`,
which **replaces** all of a node's domains. Fetch the docs for that service before writing code
against it.

## Publishing variants from code

Editors publish each culture separately, and a culture that was never published isn't routed. When a
migration, import or seeding job publishes variant content, name the cultures explicitly
(`["en-US", "da-DK"]`, or each node's `AvailableCultures`). Don't pass `"*"`: on a node that is
**already published**, a branch publish with `"*"` republishes the **default culture only**, so new
Danish edits silently stay unpublished. (On a never-published node, `"*"` does publish every culture,
which is why it looks fine the first time.)

## Note: a root per market

Some sites need a separate root per market (`/uk`, `/de`), each with its own domains and often still
with culture variants beneath. Choose it only when markets differ in **structure**: different pages,
navigation or product ranges. For the same pages in different wording, one tree is simpler for editors
and avoids duplicate content. With several roots, **every root needs domains**. Without them, Umbraco
can't tell which root a URL belongs to, and URLs change shape.

## Done

Tell the user:
- which languages exist, which is default, and each one's fallback;
- which Document Types and properties now vary, and which were left shared;
- the domain per culture and the URL each culture's home page now has;
- that editors must publish each culture, since an unpublished culture isn't routed.
