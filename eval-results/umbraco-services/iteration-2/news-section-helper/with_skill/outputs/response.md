# "News section under the site root": static helper or service?

**Short answer:** your second colleague is right. Use a service: an interface, a class, and a composer, registered as **Scoped** and injected wherever you need it. Do **not** store an `UmbracoHelper` in a static field.

## Why the static `UmbracoHelper` field is wrong

- `UmbracoHelper` (and the `IPublishedContentQuery` it wraps) is a **scoped** service. You get a new one for each request, tied to that request's `UmbracoContext`.
- A static field keeps whichever instance it got first (usually from the first request) and holds it for the life of the app. Every later request then reads content through **another request's** context. The result is stale or wrong content, data leaking between requests (culture, preview state), and errors once the original scope has been disposed.
- It also hides the dependency, so you can't unit test it or swap it out.

The same applies to any static that holds a scoped Umbraco object, such as `UmbracoContext`, `IPublishedContentQuery` or `UmbracoHelper`. The `umbraco-common-pitfalls` skill covers this bug family.

A static class **is** fine if it holds no state, meaning plain extension methods that take the helper as a parameter (see the alternative at the end). The bug is the static **field**, not the static keyword.

## The right way: a scoped service

The lifetime follows from the dependencies. This service injects `IPublishedContentQuery`, which is scoped, so the service must be registered `AddScoped`. That means it runs inside requests only (controllers and views), which matches your use case.

### `Services/ISiteService.cs`

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;

namespace MySite.Services;

// Key-page lookups in one place. Add methods per page (news section, contact…).
public interface ISiteService
{
    IPublishedContent? GetSiteRoot();
    IPublishedContent? GetNewsSection();
}
```

### `Services/SiteService.cs`

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace MySite.Services;

// Request-only: injects scoped IPublishedContentQuery, so it is registered AddScoped.
// Outside a request (background jobs, some notification handlers) use an
// IUmbracoContextFactory-based singleton instead (see below).
public class SiteService : ISiteService
{
    private const string NewsSectionAlias = "newsSection"; // your Document Type alias

    private readonly IPublishedContentQuery _publishedContentQuery;

    public SiteService(IPublishedContentQuery publishedContentQuery)
        => _publishedContentQuery = publishedContentQuery;

    // Multi-site: pick the root for the current domain, not the first.
    public IPublishedContent? GetSiteRoot()
        => _publishedContentQuery.ContentAtRoot().FirstOrDefault();

    public IPublishedContent? GetNewsSection()
        => GetSiteRoot()?
            .Children()
            .FirstOrDefault(x => x.ContentType.Alias == NewsSectionAlias);
}
```

If you use Models Builder, `.Children<NewsSection>().FirstOrDefault()` gives you a strongly typed result instead of matching on the alias.

### `SiteServiceComposer.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.Services;

namespace MySite;

public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISiteService, SiteService>();
}
```

Umbraco discovers composers on its own, so you don't need to change `Program.cs`.

### Using it in a controller

Inject it through the constructor. Its return type is `IPublishedContent?`, so always null-check the result, because the page may not exist or may not be published.

```csharp
public class NewsWidgetController : Controller // or your RenderController / SurfaceController
{
    private readonly ISiteService _siteService;

    public NewsWidgetController(ISiteService siteService) => _siteService = siteService;

    public IActionResult Index()
    {
        var news = _siteService.GetNewsSection();
        if (news is null)
        {
            return NotFound();
        }

        // ...
    }
}
```

(The class declaration and base type of a template or route-hijacking controller belong to the `umbraco-templating` skill. The only change here is the constructor injection.)

### Using it in templates and partials

```cshtml
@using MySite.Services
@inject ISiteService SiteService

@{
    var newsSection = SiteService.GetNewsSection();
}
@if (newsSection is not null)
{
    <a href="@newsSection.Url()">@newsSection.Name</a>
}
```

You can put `@using MySite.Services` in `Views/_ViewImports.cshtml` so you don't repeat it in every view.

## Summary

| Class | Lifetime | Why | Registered in | Called from |
|---|---|---|---|---|
| `SiteService` / `ISiteService` | Scoped | Depends on scoped `IPublishedContentQuery` | `SiteServiceComposer` | Controller constructor, `@inject` in views |

## Notes and alternatives

- **Lighter alternative (no registration).** For a small lookup that needs nothing except the object it extends, a **static extension method** is also fine. It holds no state; the current request's helper is passed in each time:

  ```csharp
  using Umbraco.Cms.Core;
  using Umbraco.Cms.Core.Models.PublishedContent;
  using Umbraco.Extensions;

  namespace MySite.Extensions;

  public static class PublishedContentQueryExtensions
  {
      public static IPublishedContent? GetNewsSection(this IPublishedContentQuery query)
          => query.ContentAtRoot().FirstOrDefault()?
              .Children()
              .FirstOrDefault(x => x.ContentType.Alias == "newsSection");
  }
  ```

  In a view you call it as `@Umbraco.ContentAtRoot()…`, or inject `IPublishedContentQuery` and call `.GetNewsSection()`. I'd still pick the service here, because you have several consumers (templates and controllers), it's easy to test and swap, and it gives you one place to add the next key page (contact, search…).
- **Lifetime trap.** Don't register `SiteService` as a singleton. With scope validation on (e.g. Development), startup fails with *"Cannot consume scoped service … from singleton"*. With validation off, you get back the same cross-request bug as the static field.
- **Outside a request** (background jobs, scheduled tasks): this scoped service doesn't work there. Use a singleton that injects `IUmbracoContextFactory` and calls `using var ctx = factory.EnsureUmbracoContext();` for each call. Dispose it every time.
- **Multi-site:** `ContentAtRoot().FirstOrDefault()` returns the first root. With several sites, resolve the root from the current page instead, e.g. `currentPage.Root()`, or by domain.
- **Verification:** I had no project to build against, so this code is **unverified** in your solution. The service, interface and composer follow a pattern that has been compiled and runtime-tested on Umbraco 17.5.3. The `GetNewsSection()` addition, the controller and the view snippets have not been compiled. Replace `MySite` with your namespace and `newsSection` with your Document Type alias, then build and load a page that uses it.
