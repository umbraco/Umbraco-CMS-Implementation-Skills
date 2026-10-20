---
name: umbraco-multilingual
description: >
  Set up and debug multilingual Umbraco 17+ Razor sites, where content exists in more than one
  language (culture). Covers languages and fallback, vary-by-culture Document Types, Culture and
  Hostnames (/de or a domain per country) without breaking existing URLs, block level variance for
  Block List/Grid and rich text blocks, dictionary items for hardcoded view text, a language
  switcher and hreflang tags, and cultures such as zh-HK missing from the Languages dropdown. Also
  diagnoses blank translated fields, controllers or background jobs returning the wrong culture,
  and migrations or packages whose imported languages or dictionary items don't go live. Trigger:
  "add Danish to the site", "make the site multilingual", "add a language switcher", "hreflang
  tags", "translate the blocks per language", "the German page shows English text", "zh-HK isn't
  in the languages list". SKIP: non-Umbraco apps, Umbraco 13 and earlier, translating the
  backoffice UI or extension labels, and headless / Content Delivery API front ends.
---

# Multilingual site

One content tree, **culture variants** on the nodes, and a **domain per culture** on the site root.
That is Umbraco's documented model: one node holds every language version, and the domain the request
matches decides which one renders. Build it in this order, since each step depends on the one before.

| # | Step | Reference | Ships |
|---|---|---|---|
| 1 | Languages: default, mandatory, fallback | [languages-and-variants.md](references/languages-and-variants.md) | [`assets/multilingual-package.xml`](assets/multilingual-package.xml) |
| 2 | Vary Document Types, properties and blocks by culture | [languages-and-variants.md](references/languages-and-variants.md) | — |
| 3 | Culture and Hostnames: a domain per culture | [languages-and-variants.md](references/languages-and-variants.md) | — |
| 4 | Render variant values, fallback, dictionary, culture outside a request | [rendering-variants.md](references/rendering-variants.md) | — |
| 5 | Language switcher and hreflang | [language-switcher-and-seo.md](references/language-switcher-and-seo.md) | [`languageSwitcher.cshtml`](assets/languageSwitcher.cshtml), [`hreflangLinks.cshtml`](assets/hreflangLinks.cshtml) |
| — | Only if a culture is missing from the Languages dropdown | [additional-cultures.md](references/additional-cultures.md) | [`AllowAdditionalCulturesComposer.cs`](assets/AllowAdditionalCulturesComposer.cs) |

### How to decide

- Default to one tree with culture variants. Choose a **root per market** only when markets differ
  in *structure*, not just in wording; see the note in
  [languages-and-variants.md](references/languages-and-variants.md).
- For backoffice steps (1–3), prefer the Umbraco Developer MCP. If it's unavailable, walk the user
  through the backoffice one step at a time; the order is in
  [languages-and-variants.md](references/languages-and-variants.md). Don't skip a step because the
  MCP is missing.
- **Headless / Content Delivery API front ends are out of scope for now.** This skill covers Razor
  sites.

## Version compatibility

Targets **Umbraco 17+**. Every API used here (`ILanguageService`, `IDomainService.UpdateDomainsAsync`,
`IIsoCodeValidator`, `ICultureService`) exists on 17, with one exception: **language fallback for
blocks** (`Value<BlockListModel>(..., fallback: Fallback.ToLanguage)` showing a block not exposed in
the current language) needs **17.4+**. On 17.0–17.3 the argument is accepted but unexposed blocks
stay hidden, with no error, so check the site's version before relying on it. Umbraco 13 and earlier
have different language/domain APIs; don't apply this skill there.

## Best practices

- **Never lowercase a culture you pass to Umbraco APIs or emit in markup.** Keep ISO casing
  (`da-DK`) in code and config, and compare with `StringComparison.OrdinalIgnoreCase`. The one
  exception is **Examine field names**, whose culture suffixes are stored lowercase
  (`__Published_da-dk`, `title_da-dk`): build those with `culture.ToLowerInvariant()`, or a query on
  `__Published_da-DK` silently matches nothing.
- **Don't hardcode culture lists.** Read them from `ILanguageService` or `IPublishedContent.Cultures`,
  so adding a language needs no code change.
- **Set a fallback per language and decide mandatory deliberately.** A mandatory language blocks
  publishing every other culture of a node until it has content.
- **Translate blocks with block level variance, not a varying block property.** Three settings, all
  three stated: the Document Type varies by culture, the Block List/Grid property is invariant
  (shared), and its element types vary. Every language then shares the same blocks and order. A
  varying block property gives each language its own list, and the lists drift.
- **Vary only what differs.** Shared values (SKUs, dates, images with no text) belong on invariant
  properties, or editors enter them once per language and they drift apart.
- **`Value<string>` returns `""`, not `null`, for a blank value.** A `?? fallback` never fires; use
  `Fallback.ToLanguage` or `string.IsNullOrWhiteSpace`.
- **`Umbraco:CMS:Global:DefaultUILanguage` is the *backoffice* UI language.** The site's default
  content language is set on the language itself (Settings → Languages → Default language). Changing
  that default is a separate decision from adding a language: the default is what renders when no
  domain matches and what `Fallback.ToDefaultLanguage` reads, so when a launch request implies making
  the new language the default, ask whether that is really intended rather than switching it.
- **Every published culture needs a reachable URL.** If a culture has no domain, its pages can't be
  routed in that culture, and the switcher and hreflang tags have nothing to link to.
- **The runtime gate tests the skill's assets, not your copy of them.** When handing over, say the
  partials or composer were copied from this skill and whether *you* built the project. Don't
  present the gate as your verification ("validated against Umbraco 17.5") unless you ran it.

## Validation

Assertions live in [`evals/evals.json`](evals/evals.json); run them with `umbraco-skill-evaluator`.
[`evals/README.md`](evals/README.md) has the project fixture, the run-prompt rules, and the Haiku and
Sonnet results (with and without the skill).
The runtime gate covers both gated parts:
- `examples/culture-variants/` on the blank host (languages, domains, fallback, dictionary, switcher,
  hreflang, block level variance);
- `examples/additional-cultures/` on the Clean host (the composer).
