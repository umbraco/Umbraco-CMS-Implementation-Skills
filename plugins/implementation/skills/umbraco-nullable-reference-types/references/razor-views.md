# Null handling in Razor views

**When to read this:** writing or fixing `.cshtml` templates, partials and view-component views
that render Umbraco content.
**Fetch first:** Microsoft's [nullable warnings reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)
to decode a CS86xx code. The value-handling rules are in
[published-content-values.md](published-content-values.md) and apply unchanged in views.

**Symptoms:** a template throws `NullReferenceException` on one page but not others; nullable
warnings show up for `.cs` files but never for views.
**Look for:** `@Model.Hero.Url()` with no `?.`, `@foreach (var x in Model.Items)` over a picker,
`!` inside `@( … )`, and partials that assume their model is non-null.

## Contents

- [Do views get nullable checking at all?](#do-views-get-nullable-checking-at-all): read when you are unsure why a `.cshtml` file shows no nullable warnings.
- [What `UmbracoViewPage` gives you](#what-umbracoviewpage-gives-you): read when you are using `Umbraco` or `UmbracoContext` inside a view.
- [Patterns](#patterns): read when you are writing view markup that reads values, pickers or media that may be null.
- [Partials and view components with nullable data](#partials-and-view-components-with-nullable-data): read when you are passing possibly-null data to a partial or view component.
- [Things to avoid in views](#things-to-avoid-in-views): read when you want a quick check for habits that cause null errors in views.

## Do views get nullable checking at all?

Razor views are compiled with the project's nullable setting **only when they are compiled at build
time**. Check what the project does:

- The Umbraco project template (`templates/UmbracoProject/UmbracoProject.csproj` in the CMS repo)
  sets `<Nullable>enable</Nullable>`. But when Models Builder is in `InMemoryAuto` mode it also sets
  `<RazorCompileOnBuild>false</RazorCompileOnBuild>`, and views are then compiled at runtime by
  `Umbraco.Cms.DevelopmentMode.Backoffice`.
- That runtime compiler builds its `CSharpCompilationOptions` without a nullable context
  (`Umbraco.Cms.DevelopmentMode.Backoffice/InMemoryAuto/CompilationOptionsProvider.cs`), so **no
  nullable warnings are reported for views in that mode**.
- In `SourceCodeAuto` / `SourceCodeManual` (or `Nothing`) mode with build-time Razor compilation,
  views compile with the project and `WarningsAsErrors` for nullable applies to them.

So don't take a quiet build as proof a view is null-safe. Write views defensively either way.

## What `UmbracoViewPage` gives you

`@inherits UmbracoViewPage<T>` exposes `Umbraco` (an `UmbracoHelper`, never null) and a protected
`UmbracoContext` that is `IUmbracoContext?` (`Umbraco.Web.Common/Views/UmbracoViewPage.cs`).
Use `UmbracoContext?.…`, or inject `IUmbracoContextAccessor` and follow
[accessor-try-patterns.md](accessor-try-patterns.md).

## Patterns

Razor needs parentheses around generic calls, so write `@(Model.Value<string>("title"))`, not
`@Model.Value<string>("title")`. Writing `null` to the output renders nothing, so plain output
needs no guard. Only **dereferences** need one. For a fallback, remember a blank text property is
`""`, not `null`: `?? Model.Name` never fires, so use `Fallback.ToDefaultValue` (as for the `<h1>`
below) or `string.IsNullOrWhiteSpace`.

```cshtml
@inherits UmbracoViewPage
@using Umbraco.Cms.Core.Models
@using Umbraco.Cms.Core.Models.PublishedContent
@{
    string? intro = Model.Value<string>("intro");
    MediaWithCrops? hero = Model.Value<MediaWithCrops>("heroImage");
    IEnumerable<IPublishedContent> related =
        Model.Value<IEnumerable<IPublishedContent>>("related") ?? [];
}

<h1>@(Model.Value<string>("title", fallback: Fallback.ToDefaultValue, defaultValue: Model.Name))</h1>

@if (!string.IsNullOrWhiteSpace(intro))
{
    <p class="intro">@intro</p>
}

@if (hero?.GetCropUrl("banner") is string heroUrl)
{
    <img src="@heroUrl" alt="@hero.Name" />
}

@if (related.Any())
{
    <ul>
        @foreach (IPublishedContent item in related)
        {
            <li><a href="@item.Url()">@item.Name</a></li>
        }
    </ul>
}
```

With a Models Builder model (`@inherits UmbracoViewPage<ContentModels.HomePage>`) the shape is the
same: `Model.HeroImage?.GetCropUrl("banner")`, `Model.Related ?? []`. The properties are
`[MaybeNull]` (see [umbraco-nullable-surface.md](umbraco-nullable-surface.md#models-builder-generated-properties)).

## Partials and view components with nullable data

Guard **before** rendering a partial rather than passing it something that might be null and
hoping the partial copes. That keeps the partial's model non-nullable and its body free of checks:

```cshtml
@{ IPublishedContent? cta = Model.Value<IPublishedContent>("callToAction"); }
@if (cta is not null)
{
    @await Html.PartialAsync("_CallToAction", cta)
}
```

```cshtml
@* _CallToAction.cshtml: the model is required, so no null checks inside *@
@inherits UmbracoViewPage<IPublishedContent>
<a class="cta" href="@Model.Url()">@Model.Name</a>
```

If a partial really should render something for "no item", declare that in its model type
(for example, a view model with a nullable property) rather than relying on a null model.

## Things to avoid in views

- `@Model.Hero!.Url()`. Same trap as in C#: [published-content-values.md](published-content-values.md#why--is-the-trap).
- `@inject IContentService` to look things up. That's a pitfall for reasons beyond nullability:
  see *Services in Razor views* in the `umbraco-common-pitfalls` skill.
- Using `Model.Value("alias").ToString()`. `Value()` returns `object?`, so use
  `Model.Value<string>("alias")`.

**Related:** [published-content-values.md](published-content-values.md),
[project-settings.md](project-settings.md). The `umbraco-templating` skill covers views, partials
and view components in general.
