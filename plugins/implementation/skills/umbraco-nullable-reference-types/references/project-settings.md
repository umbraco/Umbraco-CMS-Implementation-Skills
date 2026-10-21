# Project settings and your own types

**When to read this:** turning nullable checking on (or stricter) in an Umbraco site or package,
adopting it gradually in an existing codebase, fixing CS8618 on view models and options classes, or
annotating your own helpers.
**Fetch first:** Microsoft's [nullable migration strategies](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/update-applications/nullable-migration-strategies)
and [nullable static analysis attributes](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/nullable-analysis).

## Contents

- [Turn it on the way Umbraco does](#turn-it-on-the-way-umbraco-does): read when you are enabling nullable checking, or making nullable warnings fail the build.
- [Adopting it in an existing site](#adopting-it-in-an-existing-site): read when you are moving an existing codebase to nullable checking in stages.
- [CS8618: view models and options classes](#cs8618-view-models-and-options-classes): read when you are fixing CS8618 on view models or on options classes bound from configuration.
- [Annotate your own helpers](#annotate-your-own-helpers): read when you are adding `[NotNullWhen]` or `[MemberNotNull]` to your own helpers.
- [Decision rule](#decision-rule): read when the compiler says a value may be null and you need to choose between a guard, a default and `!`.

## Turn it on the way Umbraco does

Umbraco's own repository sets both properties in its root `Directory.Build.props`. The project
template (`templates/UmbracoProject/UmbracoProject.csproj`) sets only `<Nullable>enable</Nullable>`,
so a new site gets the warnings but can still build with them. To make nullable warnings break the
build, add the second property to the site `.csproj`, or to a `Directory.Build.props` at the
solution root so every project picks it up:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

`nullable` is a warning group, so this promotes only the CS86xx nullable warnings, not every
warning. Razor views only get this checking when they compile at build time. See
[razor-views.md](razor-views.md#do-views-get-nullable-checking-at-all).

## Adopting it in an existing site

Don't flip the whole solution and then bury the errors under `!`. Pick one route:

- **Project on, legacy files opted out:** set `<Nullable>enable</Nullable>`, and put
  `#nullable disable` at the top of files you haven't migrated yet. Delete the directive as each
  file is fixed. New files are checked from day one.
- **Project off, migrated files opted in:** leave the project setting off and add `#nullable enable`
  to each file as you migrate it. Switch the project setting on when most files carry the directive.

Either way, add `<WarningsAsErrors>nullable</WarningsAsErrors>` only once the error count is
manageable. Start with the code that touches Umbraco APIs, since that's where the real runtime nulls
are (see [umbraco-nullable-surface.md](umbraco-nullable-surface.md)).

## CS8618: view models and options classes

CS8618 means "a non-nullable property is left null when the constructor exits". The right fix depends
on who creates the object.

**View models you construct yourself:** use `required` so every creation site must set the value,
and use `?` for values that are genuinely optional:

```csharp
public sealed class ArticleViewModel
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

// Creation sites must satisfy `required`. The nullable values are resolved here, once.
// A blank title is "", not null, so the fallback goes through Fallback.ToDefaultValue, not `??`.
var model = new ArticleViewModel
{
    Title = content.Value<string>("title", fallback: Fallback.ToDefaultValue, defaultValue: content.Name)
        ?? content.Name,
    Url = content.Url(),
    Summary = content.Value<string>("summary"),
};
```

**Options classes bound from configuration** (`IOptions<T>`): the binder creates the object
through reflection, so `required` gives no runtime guarantee there. Give non-nullable properties a
safe initial value and validate at startup:

```csharp
public sealed class SearchOptions
{
    [Required]
    public string IndexName { get; set; } = string.Empty;

    public int PageSize { get; set; } = 10;
}

// In Program.cs, or an IComposer:
builder.Services.AddOptions<SearchOptions>()
    .BindConfiguration("Search")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

(`[Required]` is from `System.ComponentModel.DataAnnotations`. See Microsoft's
[options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) page for validation.)

## Annotate your own helpers

Give your own `Try*` methods the same contract as Umbraco's accessors, so callers get flow analysis
without `!` ([accessor-try-patterns.md](accessor-try-patterns.md) shows the calling side):

```csharp
using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

public static class SiteSettingsExtensions
{
    public static bool TryGetSiteSettings(
        this IPublishedContent content,
        [NotNullWhen(true)] out IPublishedContent? settings)
    {
        settings = content.Root().FirstChild(x => x.ContentType.Alias == "siteSettings");
        return settings is not null;
    }
}
```

For a class whose fields are filled in by an initialisation method rather than the constructor,
mark that method `[MemberNotNull(nameof(_field))]` instead of declaring the field nullable and
adding `!` at every use.

## Decision rule

When the compiler says a value may be null, choose in this order:

1. **Guard:** `if (x is null) return …;`, `is { } x`, or `TryGet…` with an early return. Use this
   when "missing" changes what happens: a 404, skipping a block, not rendering a partial.
2. **Default:** `?? string.Empty`, `?? []`, `fallback: Fallback.ToDefaultValue`, or
   `string.IsNullOrWhiteSpace(x) ? content.Name : x`. Use this when a missing value has a sensible
   substitute. For text, `??` only catches `null`: a blank Textstring is `""`, so a substitute
   like `content.Name` needs one of the last two.
3. **`!`:** only when you can prove the value is non-null and the compiler can't see it. Add a
   comment that states the proof, for example
   `// set in OnActionExecuting, which always runs before this action`. "The editor always
   fills this in" is not proof (see [published-content-values.md](published-content-values.md#why--is-the-trap)).

Never use `#pragma warning disable CS8602` or `<NoWarn>` to clear a build. That has the same
runtime risk as `!`, with no record of why it's there.

**Related:** [umbraco-nullable-surface.md](umbraco-nullable-surface.md),
[accessor-try-patterns.md](accessor-try-patterns.md), the
[nullable warnings reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings)
and the [`required` modifier](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/required).
