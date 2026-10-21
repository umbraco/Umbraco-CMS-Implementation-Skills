# Accessor `Try*` patterns

**When to read this:** code that needs the `UmbracoContext`, `UmbracoHelper` or
`IPublishedContentQuery` outside a Razor view, such as a controller, view component, service,
notification handler or background job.
**Fetch first:** [Nullable static analysis attributes (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/nullable-analysis)
and Umbraco's [Services and Helpers](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/services.md)
page, which covers `EnsureUmbracoContext()` for code that runs outside a request.

**Symptoms:** CS8602 on `umbracoContext.Content` after a `TryGet…` call; an
`InvalidOperationException: Wasn't able to get an UmbracoContext` from a hosted service or
notification handler; `!` scattered after accessor calls.
**Look for:** `TryGetUmbracoContext(out var x);` with the `bool` result ignored,
`out IUmbracoContext? ctx` followed by `ctx!.`, and `GetRequiredUmbracoContext()` in code that isn't
handling an HTTP request.

## Contents

- [How the flow analysis works](#how-the-flow-analysis-works): read when you are calling a `TryGet…` accessor and want the compiler to treat the out value as not-null without `!`.
- [`GetRequiredUmbracoContext()`: when it's right and when it's wrong](#getrequiredumbracocontext-when-its-right-and-when-its-wrong): read when you are deciding whether `GetRequiredUmbracoContext()` is safe, for example in a hosted service or notification handler.
- [Outside a request: `IUmbracoContextFactory.EnsureUmbracoContext()`](#outside-a-request-iumbracocontextfactoryensureumbracocontext): read when code runs without an HTTP request and needs an `UmbracoContext`, or only needs to look content up by key or id.
- [Writing your own `Try*` helper](#writing-your-own-try-helper): read when you are writing your own `Try*` method and want callers to get the same null analysis.

## How the flow analysis works

The three request-scoped accessors declare their out parameter as non-nullable and mark it
`[MaybeNullWhen(false)]`. The exact signatures and source files are in
[umbraco-nullable-surface.md](umbraco-nullable-surface.md). For the compiler,
`[MaybeNullWhen(false)]` on a non-nullable out parameter has the same effect as `[NotNullWhen(true)]`
on a nullable one: inside the branch where the method returned `true`, the variable is *not-null*,
and no `!` is needed. The analysis only works if you **branch on the return value**.

**Bad:** the result is discarded, so the compiler (correctly) still treats `ctx` as maybe-null.

```csharp
_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? ctx);
IPublishedContent? page = ctx!.Content.GetById(key); // NullReferenceException outside a request
```

**Good:** an early-return guard. After it, `umbracoContext` is not-null for the rest of the method.

```csharp
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

public sealed class PageTitleController : Controller
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public PageTitleController(IUmbracoContextAccessor umbracoContextAccessor)
        => _umbracoContextAccessor = umbracoContextAccessor;

    [HttpGet("api/page-title/{key:guid}")]
    public IActionResult Get(Guid key)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext))
        {
            return NotFound();
        }

        IPublishedContent? page = umbracoContext.Content.GetById(key); // GetById returns IPublishedContent?
        if (page is null)
        {
            return NotFound();
        }

        return Ok(page.Name);
    }
}
```

`IUmbracoHelperAccessor.TryGetUmbracoHelper(out UmbracoHelper? helper)` and
`IPublishedContentQueryAccessor.TryGetValue(out IPublishedContentQuery? query)` follow the same
shape. Choose the failure branch for the context: `NotFound()` in a controller, an empty
`Content(string.Empty)` result in a view component, or an early `return` in a handler.

## `GetRequiredUmbracoContext()`: when it's right and when it's wrong

`GetRequiredUmbracoContext()` returns a non-null `IUmbracoContext` and **throws**
`InvalidOperationException` when there is none. That is correct when the code only ever runs
inside an Umbraco front-end request, which is what Umbraco itself does in `RenderController` and
`PluginController` (`Umbraco.Web.Common/Controllers/`). It is wrong anywhere a request may not exist:

- `IHostedService` / `BackgroundService` and Umbraco background jobs
- notification handlers that can be triggered by a backoffice save, an import or a scheduled publish
- services that are called from both a request and something else

Don't "fix" the throw with `TryGet…` plus `!`, which just swaps the exception for a
`NullReferenceException`. Create a context instead.

## Outside a request: `IUmbracoContextFactory.EnsureUmbracoContext()`

`EnsureUmbracoContext()` returns the current context if there is one, otherwise creates one and
sets it on the accessor. It returns an `UmbracoContextReference`, which is `IDisposable`, and
disposing it disposes the context only if this call created it. Always wrap it in `using`.
Both members are non-nullable, so no null handling is needed beyond the lookup itself.

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

public sealed class ContentNameLookup
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public ContentNameLookup(IUmbracoContextFactory umbracoContextFactory)
        => _umbracoContextFactory = umbracoContextFactory;

    public string? GetName(Guid key)
    {
        using UmbracoContextReference reference = _umbracoContextFactory.EnsureUmbracoContext();
        IPublishedContent? content = reference.UmbracoContext.Content.GetById(key);
        return content?.Name;
    }
}
```

### Simpler when you only need content: inject `IPublishedContentCache`

On v17, `IPublishedContentCache` is registered as a **singleton**
(`Umbraco.PublishedCache.HybridCache/DependencyInjection/UmbracoBuilderExtensions.cs`), and its
`GetById` / `GetByIdAsync` overloads need no request or `UmbracoContext`. If a background job only
looks content up by key or id and reads property values, inject it directly. The result is still
`IPublishedContent?`, so the null check stays.

```csharp
IPublishedContent? settings = await _contentCache.GetByIdAsync(settingsKey);
if (settings is null)
{
    _logger.LogWarning("Settings node {Key} is missing or unpublished; skipping run", settingsKey);
    return;
}
```

Use `EnsureUmbracoContext()` when the code needs the context itself, for example helpers that read
the current `UmbracoContext`. Either fix is correct. What matters is that nothing borrows a request
context that doesn't exist.

`IPublishedContentQueryAccessor` warns against using it in background threads. Instead, create a DI
scope and resolve `IPublishedContentQuery` from it after `EnsureUmbracoContext()`
(`Umbraco.Infrastructure/IPublishedContentQueryAccessor.cs`).

## Writing your own `Try*` helper

Annotate your own helpers so callers get the same flow analysis. That is covered in
[project-settings.md](project-settings.md#annotate-your-own-helpers).

**Related:** [umbraco-nullable-surface.md](umbraco-nullable-surface.md),
[published-content-values.md](published-content-values.md). For why request-scoped objects must
never be cached in statics, see the `umbraco-common-pitfalls` skill.
