# The Umbraco null surface (v17)

**When to read this:** before writing or fixing code that touches Umbraco APIs under nullable
reference types, to know which calls can hand back `null` and which cannot.
**Fetch first:** [Nullable reference types (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types)
for the language rules. Umbraco's own page exists only for
[v13](https://docs.umbraco.com/umbraco-cms/13.latest/implementation/nullable-reference-types.md)
and says only that NRT has been enabled in Umbraco since v10.

Umbraco builds its own source with `<Nullable>enable</Nullable>` and
`<WarningsAsErrors>nullable</WarningsAsErrors>` (repository `Directory.Build.props`), so its public
signatures carry real annotations. Trust the signature: a `?` means a real `null` at runtime, not a
theoretical one. Paths below are relative to the Umbraco CMS `src/` folder, checked at the
`release-17.5.3` tag.

## Content and properties

| API | Returns | Null when | Source |
|---|---|---|---|
| `content.Value<T>(alias, …)` | `T?` | Alias not on the Document Type, or the value can't convert to `T`. A blank value gives the converter's own "no value", which is `null` only for some editors: Textstring/Textarea give `""` (`Umbraco.Core/PropertyEditors/TextStringValueConverter.cs`), a multiple Media Picker an empty collection, single pickers `null`. For value types `T?` is just `T`, so you get `default` (`0`, `false`, `DateTime.MinValue`) | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs`, `Umbraco.Core/Extensions/PublishedContentExtensions.cs` |
| `content.Value(alias, …)` | `object?` | As above | same |
| `content.HasValue(alias, …)` | `bool` | n/a | same |
| `element.Value<T>(alias, …)` (block items) | `T?` | As above | `Umbraco.Web.Common/Extensions/FriendlyPublishedElementExtensions.cs` |
| `content.Name` / `content.Name(culture)` | `string` | Never. A variant node with no name for the culture gives `string.Empty` | `Umbraco.Core/Models/PublishedContent/IPublishedContent.cs`, `Umbraco.Core/Extensions/PublishedContentExtensions.cs` |
| `content.Url(culture, mode)` | `string` | Never. An unroutable node gives `"#"` (`Constants.Routing.Unroutable`) | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs`, `Umbraco.Core/Routing/UrlProvider.cs` |
| `content.Parent()` / `content.Parent<T>()` | `IPublishedContent?` / `T?` | At a root node, or `T` doesn't match | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs` |
| `content.Parent` (property) | `IPublishedContent?` | As above. `[Obsolete]`, scheduled for removal in v18: use `Parent()` | `Umbraco.Core/Models/PublishedContent/IPublishedContent.cs` |
| `content.Children()` / `Children<T>()` | `IEnumerable<…>` | Never; empty when there are none | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs` |
| `content.Children` (property) | `IEnumerable<IPublishedContent>` | Never. `[Obsolete]` for v18: use `Children()` | `Umbraco.Core/Models/PublishedContent/IPublishedContent.cs` |
| `content.FirstChild()`, `Ancestor()`, `Ancestor<T>()`, `Root<T>()` | `…?` | Nothing matches | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs` |
| `content.Root()` | `IPublishedContent` | Never (non-nullable signature) | same |

## Looking content up

| API | Returns | Source |
|---|---|---|
| `UmbracoHelper.Content(id)` / `.Media(id)` (single id: `int`, `Guid`, `string`, `Udi`, `object`) | `IPublishedContent?` | `Umbraco.Web.Common/UmbracoHelper.cs` |
| `UmbracoHelper.Content(ids…)` / `.Media(ids…)` (many ids) | `IEnumerable<IPublishedContent>`, never null; missing ids are skipped | same |
| `IPublishedContentQuery.Content(id)` / `.Media(id)` | `IPublishedContent?` | `Umbraco.Infrastructure/IPublishedContentQuery.cs` |
| `IUmbracoContext.Content` / `.Media` | `IPublishedContentCache` / `IPublishedMediaCache`, non-null | `Umbraco.Core/Web/IUmbracoContext.cs` |
| `IPublishedCache.GetById(int / Guid)` | `IPublishedContent?` | `Umbraco.Core/PublishedCache/IPublishedCache.cs` |
| `IPublishedContentCache.GetByIdAsync(…)` | `Task<IPublishedContent?>` | `Umbraco.Core/PublishedCache/IPublishedContentCache.cs` |

## Request context and accessors

| API | Contract | Source |
|---|---|---|
| `IUmbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext)` | `bool`, out parameter is `[MaybeNullWhen(false)]` | `Umbraco.Core/Web/IUmbracoContextAccessor.cs` |
| `IUmbracoHelperAccessor.TryGetUmbracoHelper(out UmbracoHelper)` | `bool`, `[MaybeNullWhen(false)]` | `Umbraco.Web.Common/IUmbracoHelperAccessor.cs` |
| `IPublishedContentQueryAccessor.TryGetValue(out IPublishedContentQuery)` | `bool`, `[MaybeNullWhen(false)]` | `Umbraco.Infrastructure/IPublishedContentQueryAccessor.cs` |
| `accessor.GetRequiredUmbracoContext()` | `IUmbracoContext`, non-null. Throws `InvalidOperationException("Wasn't able to get an UmbracoContext")` when there is none | `Umbraco.Core/Extensions/UmbracoContextAccessorExtensions.cs` |
| `IUmbracoContextFactory.EnsureUmbracoContext()` | `UmbracoContextReference` (`IDisposable`); its `.UmbracoContext` is non-null | `Umbraco.Core/Web/IUmbracoContextFactory.cs`, `Umbraco.Core/UmbracoContextReference.cs` |
| `IUmbracoContext.PublishedRequest` | `IPublishedRequest?` | `Umbraco.Core/Web/IUmbracoContext.cs` |
| `IPublishedRequest.PublishedContent` / `.Culture` / `.Domain` | all nullable | `Umbraco.Core/Routing/IPublishedRequest.cs` |
| `IVariationContextAccessor.VariationContext` | `VariationContext?` | `Umbraco.Core/Models/PublishedContent/IVariationContextAccessor.cs` |
| `UmbracoPageController.CurrentPage` (and so `RenderController`) | `IPublishedContent?`; throws `InvalidOperationException` if the request has no published content | `Umbraco.Web.Common/Controllers/UmbracoPageController.cs` |
| `SurfaceController.CurrentPage` | `IPublishedContent?` | `Umbraco.Web.Website/Controllers/SurfaceController.cs` |
| `UmbracoViewPage.Umbraco` | `UmbracoHelper`, non-null | `Umbraco.Web.Common/Views/UmbracoViewPage.cs` |
| `UmbracoViewPage.UmbracoContext` (protected) | `IUmbracoContext?` | same |

How to consume the `Try*` methods: [accessor-try-patterns.md](accessor-try-patterns.md).

## Fallback and media

| API | Contract | Source |
|---|---|---|
| `IPublishedValueFallback.TryGetValue<T>(…, out T? value)` | `bool`, but the out value carries **no** `[NotNullWhen]`, so it stays `T?` even when the method returns `true` | `Umbraco.Core/Models/PublishedContent/IPublishedValueFallback.cs` |
| `defaultValue:` argument of `Value<T>` | Only used when `fallback:` includes `Fallback.ToDefaultValue`; ignored otherwise | `Umbraco.Core/Models/PublishedContent/PublishedValueFallback.cs`, `Umbraco.Core/Models/PublishedContent/Fallback.cs` |
| Media Picker value (single) | `MediaWithCrops?`; `null` when empty | `Umbraco.Infrastructure/PropertyEditors/ValueConverters/MediaPickerWithCropsValueConverter.cs` |
| Media Picker value (multiple) | `IEnumerable<MediaWithCrops>`; empty when nothing is picked | same |
| `MediaWithCrops.Content` / `.LocalCrops` | non-null | `Umbraco.Infrastructure/Models/MediaWithCrops.cs` |
| `GetCropUrl(…)` (every overload, on `IPublishedContent`, `MediaWithCrops` and `ImageCropperValue`) | `string?` | `Umbraco.Web.Common/Extensions/FriendlyImageCropperTemplateExtensions.cs`, `Umbraco.Web.Common/Extensions/ImageCropperTemplateCoreExtensions.cs` |
| `content.MediaUrl(…)` | `string` | `Umbraco.Web.Common/Extensions/FriendlyPublishedContentExtensions.cs` |
| Content Picker value | `IPublishedContent?` | `Umbraco.Core/PropertyEditors/ValueConverters/ContentPickerValueConverter.cs` |
| Multinode Treepicker value | `IEnumerable<IPublishedContent>?` (or `IPublishedContent?` when limited to one item). Can be `null`, not just empty: an empty source or no `UmbracoContext` returns `null` | `Umbraco.Core/PropertyEditors/ValueConverters/MultiNodeTreePickerValueConverter.cs` |

## Models Builder generated properties

Models Builder writes each property with its plain CLR type, **without** `?`, and adds
`[global::System.Diagnostics.CodeAnalysis.MaybeNull]` to every reference-type property. Value-type
properties get no attribute. Generated files carry no `#nullable` directive, so the consuming
project's setting applies (`Umbraco.Infrastructure/ModelsBuilder/Building/TextBuilder.cs`).
The generated shape is (abbreviated; the `GeneratedCode` attribute is left out):

```csharp
[global::System.Diagnostics.CodeAnalysis.MaybeNull]
[ImplementPropertyType("heroImage")]
public virtual global::Umbraco.Cms.Core.Models.MediaWithCrops HeroImage => this.Value<global::Umbraco.Cms.Core.Models.MediaWithCrops>(_publishedValueFallback, "heroImage");
```

So `Model.HeroImage` reads as *maybe-null* to the compiler: dereferencing it raises CS8602 even
though the type has no `?`. What to do with these values: [published-content-values.md](published-content-values.md).

**Related:** [accessor-try-patterns.md](accessor-try-patterns.md),
[published-content-values.md](published-content-values.md), [razor-views.md](razor-views.md).
