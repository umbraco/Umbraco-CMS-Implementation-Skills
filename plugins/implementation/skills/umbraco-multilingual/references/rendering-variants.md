# Rendering variant content (step 4)

Fetch [Language Variation](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/language-variation.md)
and the fallback section of
[Rendering Content](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/design/rendering-content.md)
before writing rendering code.

## Contents

- [In a template: the request's culture is already set](#in-a-template-the-requests-culture-is-already-set):
  read when rendering a page, or a language switcher for another culture.
- [Fallback](#fallback): read when a value is missing in the current culture.
- [Outside page rendering: controllers, jobs, notification handlers](#outside-page-rendering-controllers-jobs-notification-handlers):
  read when reading variant content outside a routed page.
- [Dictionary items: fixed UI strings](#dictionary-items-fixed-ui-strings): read when translating
  labels and other fixed text.
- [Blocks](#blocks): read when rendering block content in a variant site.
- [Done](#done): read before reporting back to the user.

## In a template: the request's culture is already set

When a request matches a domain, Umbraco sets the **variation context** to that domain's culture.
`Model.Value("title")` and `Model.Name` then return that culture's values, and `Model.Url()` returns
that culture's URL. Pass a culture explicitly only when rendering a *different* one, for example in a
language switcher:

```cshtml
@using Umbraco.Cms.Core.Models.PublishedContent
@Model.Value("title", "da-DK")
```

## Fallback

A blank value in a non-default culture renders as empty unless you ask for fallback:

```cshtml
@Model.Value("title", fallback: Fallback.ToLanguage)
```

| Fallback | Reads |
|---|---|
| `Fallback.ToLanguage` | The language's configured *Fall back to language*, chained (set it in step 1) |
| `Fallback.ToDefaultLanguage` | The default language |
| `Fallback.ToAncestors` | The same property on ancestors (recursive) |
| `Fallback.ToDefaultValue` | The `defaultValue:` argument |

`Fallback.To(Fallback.Language, Fallback.Ancestors)` combines them in order.

**`Value<string>` returns `""` for a blank value, not `null`**, so `Model.Value<string>("title") ?? "…"`
never falls back. Use `Fallback` or `string.IsNullOrWhiteSpace`. The same applies to multi-item
pickers, which return an empty collection rather than `null`. Single-item Content and Media pickers
*do* return `null` when empty, so keep the null check on those.

## Outside page rendering: controllers, jobs, notification handlers

Which culture `IPublishedContent` reads in depends on where the code runs:

- **Any HTTP request** (Surface and API controllers included) gets a variation context from Umbraco's
  request middleware. On a routed page it's the culture of the matched domain; on an unrouted API
  call it's the **default** language, whatever culture the caller meant.
- **Outside a request** (background jobs, notification handlers fired from a job) there is no
  variation context. Culture lookups fall back to an empty string, so culture-variant values come
  back **empty**, not in the default language, with no error.

So don't rely on the ambient culture in either case. **Inside a request**, pass the culture to each
call: `content.Value("title", culture)`, `content.Url(culture)`. Changing the request's variation
context leaks into the rest of it: a SurfaceController POST on `/en/contact` that switches to `da-DK`
and returns `CurrentUmbracoPage()` re-renders the English URL with Danish values and dictionary items.

**Outside a request**, where many reads need one culture, set the variation context and restore the
previous one when done:

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;

public class MyService(IVariationContextAccessor variationContextAccessor)
{
    public void RenderFor(string culture)
    {
        VariationContext? previous = variationContextAccessor.VariationContext;
        variationContextAccessor.VariationContext = new VariationContext(culture);
        try
        {
            // IPublishedContent reads here resolve in `culture`.
        }
        finally
        {
            variationContextAccessor.VariationContext = previous;
        }
    }
}
```

Either way, take the culture from the request (route, domain, query string) or from the site's
languages (`ILanguageService.GetAllAsync()`), never from a hardcoded list.

## Dictionary items: fixed UI strings

Labels that aren't content ("Read more", form labels, footer text) belong in **Translation →
Dictionary**, one item per string, with a value per language. In Razor:

```cshtml
@Umbraco.GetDictionaryValue("ReadMore")
@Umbraco.GetDictionaryValueOrDefault("ReadMore", "Read more")
```

Both read the current request's culture. Prefer `GetDictionaryValueOrDefault` so a missing key or
translation renders readable text rather than an empty string. For dropdown/checkbox property editors,
use dictionary keys as the option values and translate them at render time (the tutorial shows the
loop). The package manifest in [`assets/`](../assets/multilingual-package.xml) shows the dictionary
XML shape. For depth - the full overload set, the `GetDictionaryValue(key, "fallback")` compile
trap, key grouping, null-safe multi-value rendering and culture outside routed requests - load
the `umbraco-dictionary-items` skill.

## Blocks

With block level variance (an invariant Block List/Grid property whose element types vary; see
[languages-and-variants.md](languages-and-variants.md#blocks-same-blocks-translated-content)),
rendering needs no filtering or culture code:

```cshtml
@using Umbraco.Cms.Core.Models.Blocks
@foreach (var block in Model.Value<BlockListModel>("blocks") ?? BlockListModel.Empty)
{
    <h2>@block.Content.Value("title")</h2>  @* already the request's culture *@
}
```

- Blocks **not exposed** in the request's culture are left out.
- `block.Content.Value(...)` returns the request's culture, and shared (invariant) block properties
  return their single value.
- **Language fallback for blocks (Umbraco 17.4+):** pass a fallback to show a block that isn't exposed
  in this language, rendered in the fallback language, in its usual position:
  `Model.Value<BlockListModel>("blocks", fallback: Fallback.ToLanguage)`. Without the fallback
  argument it stays hidden. Use it only when "show the English block until it's translated" is what
  the site wants.
- Outside page rendering (controllers, jobs), blocks resolve in the **variation context's** culture,
  not a culture passed to `Value`. To read blocks in another culture, set the variation context and
  restore it in a `finally`, as above, even inside a request.

## Done

Tell the user which properties use fallback and to which language, where dictionary items were added,
and, for any controller or job, how it sets the culture.
