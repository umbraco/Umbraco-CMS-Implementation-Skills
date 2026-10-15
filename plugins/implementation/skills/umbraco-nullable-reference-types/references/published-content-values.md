# Published content values

**When to read this:** reading property values in C#, using `Value<T>()`, Models Builder
properties, pickers or media, and deciding how to handle the `null` they can return.
**Fetch first:** Umbraco's [IPublishedContent property access](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/ipublishedcontent/properties.md)
page (the *Fallback* section) and Microsoft's [null-forgiving operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-forgiving)
reference.

**Symptoms:** CS8602 / CS8600 / CS8604 on property reads; a page that works until an editor clears
a field or unpublishes a picked node, then throws `NullReferenceException`.
**Look for:** `Value<string>(…)!`, `Model.SomePicker!.Url()`, `foreach` straight over a picker
value, `defaultValue:` without `fallback:`.

Return types for each API are listed once in [umbraco-nullable-surface.md](umbraco-nullable-surface.md).
This file covers what to do with them.

## Why `!` is the trap

`Value<T>()` returns `T?`, and Models Builder properties are `[MaybeNull]`. That is because content is
editor-controlled data. Even a property marked *mandatory* can be absent at runtime: the node was
published before the property was added, the value is a picker to a node that was later unpublished
or deleted, or the property is culture-variant and the current culture has no value.

The null-forgiving operator (`!`) only changes what the **compiler** believes. It emits no runtime
check. `Model.Hero!.Url()` builds cleanly and throws `NullReferenceException` on the first page where
the picker is empty. That makes the compiler warning *more* useful in Umbraco than in typical
app code, so don't silence it. The decision rule for when `!` is acceptable is in
[project-settings.md](project-settings.md#decision-rule).

## Strings and scalars

```csharp
// string? -> string: coalesce at the point of use.
string title = content.Value<string>("title") ?? content.Name;

// Or let Umbraco apply the default. defaultValue is IGNORED unless fallback includes
// Fallback.ToDefaultValue, and the return type is still string?, so the compiler wants ?? anyway
// if you dereference it.
string? subtitle = content.Value<string>("subtitle", fallback: Fallback.ToDefaultValue, defaultValue: content.Name);

// Value types: Value<int> returns int, which is 0 when empty. Use int? if 0 is a real value.
int? maxItems = content.Value<int?>("maxItems");
```

`string.IsNullOrWhiteSpace(x)` narrows `x` to not-null in its `false` branch (it is annotated
`[NotNullWhen(false)]` in .NET), so it is a good guard when an empty string should count as missing.

`content.HasValue("alias")` tells you whether a value exists, but it does **not** narrow the type
of a later `Value<T>()` call. The compiler still sees `T?`. Prefer reading once and testing the
result:

```csharp
if (content.Value<IPublishedContent>("relatedPage") is { } related)
{
    string relatedUrl = related.Url();
}
```

## Pickers

```csharp
// Content Picker: IPublishedContent?
IPublishedContent? cta = content.Value<IPublishedContent>("callToAction");
string? ctaUrl = cta?.Url();

// Multinode Treepicker: can be null, not just empty. Normalise once, then iterate.
IEnumerable<IPublishedContent> items =
    content.Value<IEnumerable<IPublishedContent>>("featuredItems") ?? [];
foreach (IPublishedContent item in items)
{
    // item is not-null here
}
```

## Media

```csharp
// Single Media Picker: MediaWithCrops?. GetCropUrl always returns string?.
MediaWithCrops? hero = content.Value<MediaWithCrops>("heroImage");
string? heroUrl = hero?.GetCropUrl("banner");

// Multiple Media Picker: empty when nothing is picked, but Value<T> is still typed T?.
IEnumerable<MediaWithCrops> gallery = content.Value<IEnumerable<MediaWithCrops>>("gallery") ?? [];
```

`MediaWithCrops.Content` is non-null, so once you have a `MediaWithCrops` you can use
`hero.Content.Name` freely.

## Models Builder properties

Generated properties look non-nullable (`MediaWithCrops HeroImage`), but they are marked
`[MaybeNull]`, so the same rules apply: `Model.HeroImage?.GetCropUrl("banner")`,
`Model.FeaturedItems ?? []`. Value-type properties (`int`, `bool`, `DateTime`) are never null but
fall back to `default` when empty, so check for `DateTime.MinValue` before formatting a date.

Don't "fix" the warnings by editing the generated files or wrapping them in partials that add `!`.
The next regeneration loses the edit, and the runtime risk remains. For where computed properties
belong, see the *Models Builder misuse* entry in the `umbraco-common-pitfalls` skill.

Namespaces used above: `Umbraco.Cms.Core.Models` (`MediaWithCrops`),
`Umbraco.Cms.Core.Models.PublishedContent` (`IPublishedContent`, `Fallback`), and `Umbraco.Extensions`
(`Value<T>`, `Url`, `GetCropUrl`).

**Related:** [razor-views.md](razor-views.md) (the same rules in `.cshtml`),
[accessor-try-patterns.md](accessor-try-patterns.md).
