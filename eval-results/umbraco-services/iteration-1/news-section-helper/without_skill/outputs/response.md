# "Find the news section under the site root" in Umbraco 17: build a service

**Short answer:** make a service. Don't write a static helper with a static `UmbracoHelper` field.

## Why the static `UmbracoHelper` field is wrong

- **`UmbracoHelper` is request-scoped.** It wraps the `IUmbracoContext` for *one* HTTP request: the published snapshot, culture, preview state and assigned content. If you put it in a static field, the first request's helper lives for the whole lifetime of the app and every later request shares it. You get stale or wrong content, broken preview and wrong-culture results. Once that request's scope is disposed you can also get `ObjectDisposedException` or other odd failures.
- **It isn't thread-safe to share** across concurrent requests.
- **You can't resolve it outside a request.** In background jobs, notification handlers or startup code there is no `UmbracoContext`, so a static helper fails there in confusing ways.
- **It hides dependencies and blocks testing.** You can't mock a static, and nothing in the constructor tells you it needs Umbraco's content cache.
- Since Umbraco 10+ the guidance is to inject what you need. Umbraco 15+ also reworked the published content cache (HybridCache) and navigation (`IDocumentNavigationQueryService`), so old "static helper" patterns age especially badly.

**A service** registered in DI, with injected dependencies, behaves the same in templates (`@inject`), controllers (constructor injection), view components and background code. You can also unit-test it.

## Design notes

- **"Under the site root" depends on the request.** On a multi-site install (or one that will become multi-site) each root has its own news section. So the service takes the **current page** and walks up to *that page's* root. It doesn't use `GetAtRoot().First()`.
- **Don't cache `IPublishedContent` in a field.** Navigation and cache lookups in v15+ are in-memory and cheap. Caching a node in a singleton field brings back the staleness problem the static helper had.
- Use the **v15+ APIs**: `IDocumentNavigationQueryService` for structure and `IPublishedContentCache` for the content. Avoid `UmbracoHelper` and `IUmbracoContext.Content` traversal in services.
- The code assumes a document type alias of `newsSection`. Change it to match yours. If you use ModelsBuilder, `NewsSection.ModelTypeAlias` is better than a string.

## The code

### 1. Interface + implementation

```csharp
// Services/INewsSectionService.cs
using Umbraco.Cms.Core.Models.PublishedContent;

namespace MySite.Services;

public interface INewsSectionService
{
    /// <summary>
    /// Returns the published news section under the site root that <paramref name="current"/> belongs to,
    /// or null if there isn't one.
    /// </summary>
    IPublishedContent? GetNewsSection(IPublishedContent current);

    /// <summary>
    /// Same as above, but uses the page being rendered in the current request.
    /// Returns null outside a front-end request.
    /// </summary>
    IPublishedContent? GetNewsSectionForCurrentRequest();
}
```

```csharp
// Services/NewsSectionService.cs
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public sealed class NewsSectionService : INewsSectionService
{
    // Prefer NewsSection.ModelTypeAlias if you use ModelsBuilder.
    private const string NewsSectionAlias = "newsSection";

    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public NewsSectionService(
        IDocumentNavigationQueryService navigation,
        IPublishedContentCache contentCache,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _navigation = navigation;
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public IPublishedContent? GetNewsSection(IPublishedContent current)
    {
        ArgumentNullException.ThrowIfNull(current);

        Guid rootKey = GetRootKey(current.Key);

        if (_navigation.TryGetChildrenKeysOfType(rootKey, NewsSectionAlias, out IEnumerable<Guid> keys) is false)
        {
            return null;
        }

        // GetById returns null for unpublished nodes, so this yields the first *published* news section.
        return keys
            .Select(key => _contentCache.GetById(key))
            .FirstOrDefault(content => content is not null);
    }

    public IPublishedContent? GetNewsSectionForCurrentRequest()
    {
        if (_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? context) is false)
        {
            return null; // background job, startup, etc.
        }

        IPublishedContent? current = context.PublishedRequest?.PublishedContent;
        return current is null ? null : GetNewsSection(current);
    }

    private Guid GetRootKey(Guid key)
    {
        // Walk up until there is no parent. The root is the node with no parent.
        Guid currentKey = key;
        while (_navigation.TryGetParentKey(currentKey, out Guid? parentKey) && parentKey.HasValue)
        {
            currentKey = parentKey.Value;
        }

        return currentKey;
    }
}
```

> If you prefer the extension-method style, `current.Root()` / `AncestorOrSelf(1)` also work in templates. In v15+ several of those traversal extensions want the navigation and cache services passed in, or they fall back to the static service locator. Inside a service, the explicit navigation walk above is clearer and testable.

### 2. Register it (composer, so `Program.cs` stays untouched)

```csharp
// Composers/NewsSectionComposer.cs
using MySite.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace MySite.Composers;

public sealed class NewsSectionComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<INewsSectionService, NewsSectionService>();
}
```

**Lifetime:** scoped is the safe default. It resolves fine in controllers and views, and the service holds no state. Its dependencies (`IDocumentNavigationQueryService`, `IPublishedContentCache`, `IUmbracoContextAccessor`) are app-wide, and the accessor reads the *current* request's context on each call. Singleton would also work, but scoped stops anyone accidentally adding per-request state later. Don't inject a scoped service into a singleton (for example a hosted service). There, create a scope or depend on the singleton-safe services directly.

You can also call `builder.Services.AddScoped<...>()` in `Program.cs` before `.Build()` if that's your convention. The composer just keeps it self-contained.

### 3. Use it in templates

Add the namespace once in `Views/_ViewImports.cshtml`:

```cshtml
@using MySite.Services
```

Then in any template or partial:

```cshtml
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@inject INewsSectionService NewsSections

@{
    var newsSection = NewsSections.GetNewsSection(Model);
}

@if (newsSection is not null)
{
    <a href="@newsSection.Url()">@newsSection.Name</a>
}
```

In a partial whose model isn't the page, use `NewsSections.GetNewsSectionForCurrentRequest()` instead.

### 4. Use it in controllers

**Route hijacking (`RenderController`):**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using MySite.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace MySite.Controllers;

public class ArticleController : RenderController
{
    private readonly INewsSectionService _newsSections;

    public ArticleController(
        ILogger<ArticleController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        INewsSectionService newsSections)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
        => _newsSections = newsSections;

    public override IActionResult Index()
    {
        var newsSection = _newsSections.GetNewsSection(CurrentPage!);
        ViewData["NewsSection"] = newsSection; // or build a view model
        return CurrentTemplate(CurrentPage);
    }
}
```

**Surface or API controller:** inject it the same way. With no page model, call `GetNewsSectionForCurrentRequest()` (surface controllers posting back from a page have a published request). For a headless or API controller, pass in a node you resolved yourself, such as one looked up by key.

## Summary

| | Static helper with static `UmbracoHelper` | DI service |
|---|---|---|
| Correct per request / culture / preview | No, captures one request | Yes |
| Thread-safe | No | Yes |
| Works outside a request | No | Yes (navigation + cache) |
| Multi-site aware | Usually not | Yes, resolved from the current page's root |
| Unit-testable | No | Yes, mock the interface or the Umbraco services |

So your second colleague is right. If you want a one-liner in Razor, wrap the service call in a small extension or view component. The lookup itself belongs in the injected service.

---
*Notes / to verify against your build:* method names are from Umbraco 15–17's `IDocumentNavigationQueryService` (`TryGetParentKey`, `TryGetChildrenKeysOfType`) and `IPublishedContentCache.GetById(Guid)`, written from memory and not compiled here. Confirm them against your installed version, and swap the `newsSection` alias for your document type alias.
