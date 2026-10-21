---
name: umbraco-nullable-reference-types
description: >
  Write and fix Umbraco 17+ C# and Razor code under C# nullable reference types (NRT), which
  Umbraco has enabled since v10. Covers which Umbraco APIs return null (Value<T>() returning T?,
  Parent, GetById, Models Builder properties, media and picker values), the TryGetUmbracoContext /
  TryGetUmbracoHelper accessor pattern with [NotNullWhen] flow analysis, Razor null handling, and
  turning on <Nullable>enable</Nullable> with WarningsAsErrors=nullable without scattering the
  null-forgiving operator. Use this when the user mentions "nullable reference types", "NRT",
  CS8602 / CS8600 / CS8603 / CS8618 warnings in an Umbraco project, "possible null reference",
  "dereference of a possibly null reference", "null-forgiving operator", or asks to make Umbraco
  code null-safe. SKIP: non-Umbraco projects, Umbraco versions before 10, and general C# NRT
  questions with no Umbraco API involved.
---

# Nullable Reference Types in Umbraco

Umbraco has compiled with nullable reference types since v10, and its public API is annotated:
many calls a site developer makes every day can return `null`. Once a site turns on
`<Nullable>enable</Nullable>` — especially with `WarningsAsErrors=nullable`, as Umbraco does for its
own source — the compiler flags every place that ignores that. This skill says which Umbraco APIs
return `null` and *why*, and how to handle each one so the code is correct at runtime, not just
quiet at compile time.

Umbraco's own docs page on this topic exists only for v13 and is a short pointer to Microsoft's
docs. Every Umbraco-specific contract in `references/` was checked against the v17 source instead.

## Index

| Situation | Look for | Reference |
|---|---|---|
| Unsure whether an Umbraco call can return `null` | Any Umbraco API under CS86xx | [umbraco-nullable-surface.md](references/umbraco-nullable-surface.md) |
| Reading property values, pickers, media, Models Builder props | `Value<T>(...)!`, `.Trim()` / `.Length` on a value, `defaultValue:` | [published-content-values.md](references/published-content-values.md) |
| Getting `UmbracoContext` / `UmbracoHelper` outside a view | `TryGetUmbracoContext`, `GetRequiredUmbracoContext()` in a service, hosted service or job | [accessor-try-patterns.md](references/accessor-try-patterns.md) |
| `.cshtml` templates and partials | `@Model.Value<...>()!`, `foreach` / `.Any()` over a picker value | [razor-views.md](references/razor-views.md) |
| Turning NRT on, CS8618, your own helpers | `<Nullable>`, `#nullable disable`, uninitialised view-model strings, `IOptions<T>` classes | [project-settings.md](references/project-settings.md) |

Before writing code, open the reference for each situation the code in front of you touches, and
only those. A compiled, tested example of the controller → mapper → view-model shape is in
[`assets/`](assets/): start from it when you write that shape.

## Best practices

- **A `?` in Umbraco's API describes a real content state**, not compiler noise: an editor left a
  property empty, a picked node was deleted or unpublished, or there's no request and so no
  `UmbracoContext`. Decide what the page should do in that state.
- **Guard, then default, then — rarely — `!`.** Prefer an early return or `is not null` guard; then a
  real default (`?? string.Empty`, an empty collection). Use `!` only where you can prove non-null,
  with a comment saying why. `!` and `#nullable disable` silence the compiler and leave the same
  `NullReferenceException` waiting for the first empty field.
- **`defaultValue:` alone does nothing.** `Value<T>(alias, defaultValue: x)` applies `x` only when you
  also pass `fallback: Fallback.ToDefaultValue`, and the return type stays `T?` either way.
- **A blank text property is `""`, not `null`.** `Value<string>()` on an empty Textstring or Textarea
  returns `string.Empty`, so `Value<string>("title") ?? content.Name` never falls back. Use
  `string.IsNullOrWhiteSpace` or `fallback: Fallback.ToDefaultValue` when blank should mean "use
  something else". `null` comes back when the alias isn't on the Document Type.
- **Use the `Try*` accessors and act on `false`.** `TryGetUmbracoContext` / `TryGetUmbracoHelper`
  are annotated so the `out` value is non-null when they return `true`. Don't call
  `GetRequiredUmbracoContext()` where there may be no request. Instead, inject the singleton
  `IPublishedContentCache` for plain content lookups, or use
  `IUmbracoContextFactory.EnsureUmbracoContext()` in a `using` scoped to one unit of work.
- **Prefer the `Parent()` / `Children()` extensions** over the `[Obsolete]` `IPublishedContent.Parent`
  / `.Children` properties (removal scheduled for v18).
- **Don't assume views are checked.** With InMemoryAuto models, Razor views compile at runtime
  without nullable analysis, so a clean build says nothing about your `.cshtml`. Apply the same rules
  by hand.
- **Never claim a build passed that you didn't run.** If you can't compile, say so, then state
  plainly whether you expect the change to build with no nullable warnings, and name anything you
  couldn't confirm, such as an assumed signature or a missing `using`.
- **Include the `using` directives** in the code you hand back. `UmbracoContextReference` is in
  `Umbraco.Cms.Core`, and `MediaWithCrops` is in `Umbraco.Cms.Core.Models`. Either one missing
  breaks the build even when the null handling is right.

## Version compatibility

Targets **Umbraco 17** (`net10.0`). Contracts were checked against the Umbraco CMS source at
`release-17.5.3` (the version this repo pins), and the `assets/` compile against the repo's pinned Umbraco 17 with
`Nullable=enable` and `WarningsAsErrors=nullable`. Umbraco has shipped NRT annotations since v10, so
the patterns apply from v10, but individual signatures (for example `Parent()` vs `.Parent`) differ
in older versions — check the installed version before relying on one.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. The `assets/` are **Asserted**: compiled and HTTP-asserted on the Clean
reference host by the `dotnet test` gate (`examples/approach-a-nullable-patterns/`).
