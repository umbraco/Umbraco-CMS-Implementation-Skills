---
name: umbraco-dictionary-items
description: >
  Render translatable labels and static text in Umbraco 17+ Razor templates
  with dictionary items - the per-language strings editors manage in the
  backoffice Translation section. Use whenever the user asks to translate
  labels, button text or static text in templates, to "use dictionary items",
  calls GetDictionaryValue / GetDictionaryValueOrDefault, wants to translate
  dropdown options, or needs multilingual labels. SKIP: non-Umbraco projects,
  Umbraco < 17, translating *content* properties - that is language variants,
  not dictionary items - full multilingual site setup (adding languages,
  vary-by-culture Document Types, Culture and Hostnames, language switcher),
  which is `umbraco-multilingual`, and backoffice extension/localization files,
  which belong to the backoffice skills.
---

# Dictionary items

A dictionary item is a keyed string with one value per language, managed by
editors under Translation > Dictionary. Use it for static UI text - labels,
buttons, headings - not for content: text that varies per language *as content*
belongs in a language-variant property on the Document Type.

## Workflow

1. **Decide the keys** using the dotted naming convention: one parent group item
   (e.g. `Site`), children named `Group.Thing` (`Site.ReadMore`,
   `Site.Footer.Copyright`). Keys are identifiers referenced from code, so treat
   them like constants and keep one casing; lookups are case-insensitive on
   Umbraco's default databases, but don't rely on that across environments.
   Grouping keeps the Translation tree navigable for editors.
2. **Create the items** in the Translation section - via the Umbraco Developer
   MCP if available, otherwise by hand following the official
   [Dictionary items guide](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/publishing-and-workflow/editorial-tools/dictionary-items.md)
   (create, group, edit; Export/Import of `.udt` files to move items between
   sites).
   If the request is backoffice-only (an editor, no code), fetch that guide and
   walk the user through its steps dialog by dialog - on import, for example,
   reviewing the items and choosing where they go - and link the guide in your
   answer.
3. **Render them** - open the
   [rendering reference](references/rendering-dictionary-items.md) before writing
   any view code: the exact lookup overloads, the explicit-culture form, null-safe
   multi-value rendering, and culture handling.
4. **Verify, then report** - finish with the reference's Verify and Done
   sections. Don't skip the build check; see the first best practice below for
   why "it looks right" is not evidence.

## Version compatibility

Umbraco 17+. `GetDictionaryValue`, `GetDictionaryValueOrDefault` and
`CultureDictionary` are available on `Umbraco` in any view that inherits
`UmbracoViewPage`, with or without ModelsBuilder. The older `ILocalizationService`
is obsolete and must not be used.

## Best practices

- **Always have a fallback - with the right overload.** `GetDictionaryValue(key)`
  returns an empty string for a missing key or translation; use
  `GetDictionaryValueOrDefault(key, fallback)` wherever blank UI is unacceptable.
  It is the only fallback mechanism: a language's configured fallback language
  applies to content properties, not to dictionary values. Beware the look-alike:
  `GetDictionaryValue(key, "fallback")` reads naturally but does not compile -
  that second parameter is a `CultureInfo`.
- **Let the request culture drive lookups.** On a routed front-end request Umbraco
  sets `CurrentUICulture` from the matched domain; use the explicit `CultureInfo`
  overload only when you deliberately want a fixed culture.
- **Single-key lookups are cached; `GetChildren` is not.** `CultureDictionary[key]`
  and the `GetDictionaryValue*` calls are the safe read path for views.
  `CultureDictionary.GetChildren(key)` runs a database query per call and drops
  children with no translation, so keep it out of templates - and never call
  `IDictionaryItemService` (the management API) from a view.
- **Dictionary = chrome, variants = content.** If the text is part of the page's
  editorial content, it is a language variant, not a dictionary item.

## Validation

Documented - this skill ships no code and nothing ran: the Razor snippets in the
reference are illustrative and uncompiled, and the
[Multilanguage setup tutorial](references/rendering-dictionary-items.md#fetch-these-first)
is the source for the real thing. Assertions live in
[`evals/evals.json`](evals/evals.json); run them with the `umbraco-skill-evaluator`
skill.
