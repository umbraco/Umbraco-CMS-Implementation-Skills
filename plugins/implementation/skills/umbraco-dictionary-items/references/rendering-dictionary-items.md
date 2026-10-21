# Rendering dictionary items in Razor

Dictionary items are created in the backoffice (Translation > Dictionary) and read in
templates via `UmbracoHelper`, which resolves the value for the request's culture.

## Contents

- [Fetch these first](#fetch-these-first): read when you need the docs pages to follow for the basic lookups and the request culture.
- [The exact overloads](#the-exact-overloads): read when writing a lookup call, to pick the right overload and fallback behaviour.
- [Explicit culture](#explicit-culture): read when a fixed language is wanted, or there is no routed request to set the culture.
- [Multi-value property editors](#multi-value-property-editors): read when dropdown or checkbox option values are dictionary keys that need translating.
- [Culture](#culture): read when the code runs outside normal front-end routing (API controllers, background jobs) or you need to know how the culture is set.
- [Verify](#verify): read before the final answer, to state build correctness and troubleshoot a key that renders empty.
- [Done](#done): read when wrapping up, for what to tell the user.
- [Runtime coverage](#runtime-coverage): read when you need to know what was checked against the Umbraco source.

## Fetch these first

- [UmbracoHelper: fetching dictionary values](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/umbracohelper.md#fetching-dictionary-values)
  documents the two basic lookups, `GetDictionaryValue(key)` and
  `GetDictionaryValueOrDefault(key, altText)`. The `CultureInfo` overloads and
  `Umbraco.CultureDictionary` are not on that page; they are covered below.
- [Multilanguage setup tutorial](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/multilanguage-setup.md):
  read "Using Dictionary Items", "Translating Multi-Value Property Editors", and
  "Adding Culture and Hostnames" (how the request culture is resolved).

The calls those pages show (`GetDictionaryValue(key)`, `GetDictionaryValueOrDefault(key,
altText)`, the multi-value loop) are not repeated here - fetch and link. The sections
below cover only what the docs leave out.

## The exact overloads

`UmbracoHelper` has four dictionary *value* lookups (plus `CultureDictionary` and
`GetCultureDictionary(CultureInfo)`, which return the `ICultureDictionary` behind them):

| Call | Returns when the key/translation is missing |
| --- | --- |
| `GetDictionaryValue(string key)` | `""` |
| `GetDictionaryValue(string key, CultureInfo culture)` | `""` |
| `GetDictionaryValueOrDefault(string key, string fallback)` | `fallback` |
| `GetDictionaryValueOrDefault(string key, CultureInfo culture, string fallback)` | `fallback` |

There is **no** `GetDictionaryValue(key, "fallback")` - a string second argument does not
compile, because that position is the `CultureInfo`. A fallback always means the
`OrDefault` name. The `OrDefault` string is also the *only* fallback: a language's
configured fallback language applies to content properties, not to dictionary values.
The lookup picks one installed language - the one matching the request culture, or its
parent culture (`da` for `da-DK`) only when no language matches exactly - and returns
that language's translation or `""`; it never tries a second language.

Write the calls inline in the site's own layout and templates, where the hard-coded text
currently sits - don't add a separate labels partial unless the user asks for one.

## Explicit culture

The one form the docs don't show. Use it when a fixed language is wanted regardless of
the request, or where there is no routed request to set the culture (see Culture below):

```cshtml
@using System.Globalization
@Umbraco.GetDictionaryValueOrDefault("Site.Search", new CultureInfo("da-DK"), "Search")
```

`Umbraco.CultureDictionary.GetChildren(key)` also exists but is **not cached** - it is a
database query per call and silently omits children with no translation for the
culture - so don't use it in views.

## Multi-value property editors

Dropdown/checkbox option *values* can be dictionary keys rather than display text, so
editors translate the option labels without touching the Document Type; the tutorial's
"Translating Multi-Value Property Editors" section shows the loop. Two things it doesn't
show:

- The stored value can be empty, and the key parameter is non-nullable. A null or empty
  key doesn't throw, it just renders `""` - so guard it, and fall back to the raw value
  so an untranslated option still shows something:

  ```cshtml
  @{ var category = Model.Value<string>("category"); }
  @if (!string.IsNullOrEmpty(category))
  {
      @Umbraco.GetDictionaryValueOrDefault("Site.Category." + category, category)
  }
  ```

  With ModelsBuilder on, prefer the typed property (`Model.Category`) over `Value<string>`.
- Multi-select editors store `IEnumerable<string>`: loop and apply the same guard per item.

## Culture

On a routed front-end request, `CurrentUICulture` is set from the domain's Culture &
Hostnames mapping (else the default language), so the parameterless overloads already
follow the visitor. Anything that does not go through Umbraco's content routing - API
controllers, surface-less endpoints, background jobs - gets whatever the thread's culture
happens to be, so pass a `CultureInfo` explicitly there; never assume the default
language. With no `UmbracoHelper` (a hosted service, say), inject
`ICultureDictionaryFactory` and call `CreateDictionary(culture)`, then index it by key;
don't swap `CultureInfo.CurrentUICulture` around a parameterless `CreateDictionary()`.
(If the job also reads content, `umbraco-multilingual` covers setting the variation
context.)

## Verify

Your final answer must contain one of these two statements about build correctness -
silence on it is the failure mode to avoid:

1. **A `.csproj` is present:** run `dotnet build` after writing the files, fix any
   errors, and report the result. If the build cannot run (no SDK, no package feed,
   restore fails), say so and quote the error rather than implying success. A clean
   build does not prove the views compile - `.cshtml` is compiled at runtime - so also
   say that the views were not exercised and recommend loading a page that renders them.
2. **No buildable project:** say so explicitly, and state that every dictionary call
   uses one of the four overloads in the table above, so you expect it to compile.

If a key renders empty at runtime: the item is missing (check the spelling; casing
rarely matters, since lookups are case-insensitive on SQLite and default SQL Server
collations), or the request culture has no translation and no
`GetDictionaryValueOrDefault` fallback was given.

## Done

Tell the user which items exist and where editors manage them (Translation > Dictionary),
which templates render them, and that values follow the request's culture - and how to
add more keys under the shared group prefix. Include the build-correctness statement
from Verify, and link the docs page(s) you followed so the user can check the steps
against the source.

## Runtime coverage

Documented - this skill ships no code. The four overloads, the `""` on a miss, the
single-language lookup (no configured-fallback step) and the uncached `GetChildren` were
checked against `UmbracoHelper` and `DefaultCultureDictionary` in the Umbraco 17.5.3
source; the snippets are illustrative, not assets.
