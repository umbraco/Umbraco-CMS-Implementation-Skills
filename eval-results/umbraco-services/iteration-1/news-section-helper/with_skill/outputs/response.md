# Finding "the news section under the site root" in Umbraco 17

**Short answer:** go with the colleague who suggested a service. Don't build a static helper with a static `UmbracoHelper` field.

## Why the static `UmbracoHelper` field is wrong

- `UmbracoHelper` is **scoped**: Umbraco creates one per HTTP request, tied to that request's `UmbracoContext` and published-cache snapshot. A static field keeps whichever instance it got first and hands it to every later request. Depending on timing, you'll see stale content (the news section doesn't update after a publish), you'll leak one request's state into another, or you'll get `ObjectDisposedException`s because the original request's context has already been disposed.
- You also can't set it up properly. A static class can't receive constructor injection, so the field ends up filled from some view or controller at runtime. That's a hidden ordering dependency.
- You can't test it or swap it out.

This is a well-known Umbraco anti-pattern. The sibling skill `umbraco-common-pitfalls` covers it in more detail.

## Two correct options

The Umbraco 17 docs ([Services and Helpers](https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md)) describe two valid ways to do this. "Find the news section" is actually their own example:

| Option | When it fits |
|---|---|
| **Extension method** on `IPublishedContentQuery` | A small, stateless lookup that needs only the object it extends. Nothing to register. |
| **Custom service** (interface + class + composer) | You'll add more "key page" lookups (contact page, search page…), you want to unit-test or mock it, or it needs other dependencies. |

You have two controllers and several templates that all need it, and there will probably be more "find page X" lookups later. A small scoped **`ISiteService`** is the better long-term home for this. The extension method is also correct and is shown at the end if you prefer it.

## The service

### `Services/ISiteService.cs`

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;

namespace MySite.Services;

// One place for "find the key pages of this site", so controllers and views don't
// each repeat their own tree traversal.
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

// REQUEST-ONLY service: use it from controllers and views, never from a background job.
// It reads the published cache through IPublishedContentQuery, which is SCOPED (one per
// HTTP request), so this class must be registered as scoped too.
public class SiteService : ISiteService
{
    private const string NewsSectionAlias = "newsSection"; // your Document Type alias

    private readonly IPublishedContentQuery _publishedContentQuery;

    public SiteService(IPublishedContentQuery publishedContentQuery)
        => _publishedContentQuery = publishedContentQuery;

    // Multi-site: ContentAtRoot() returns every root. If the install hosts more than one
    // site, pick the root for the current domain instead of the first one.
    public IPublishedContent? GetSiteRoot()
        => _publishedContentQuery.ContentAtRoot().FirstOrDefault();

    public IPublishedContent? GetNewsSection()
        => GetSiteRoot()?.FirstChildOfType(NewsSectionAlias);
}
```

It reads from the **published cache** and doesn't use `IContentService`. `IContentService` is a management service that queries the database on every call, so it's the wrong tool for rendering.

### `SiteServiceComposer.cs`: registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.Services;

namespace MySite;

public class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Scoped because SiteService injects IPublishedContentQuery (scoped).
        // Registering it as a singleton fails at startup with scope validation on
        // ("Cannot consume scoped service ... from singleton"), and with validation
        // off it silently shares the first request's state with every request:
        // the same bug as the static UmbracoHelper field.
        builder.Services.AddScoped<ISiteService, SiteService>();
    }
}
```

Pick the lifetime from what the class **depends on**. Don't pick it from what feels efficient. A scoped service costs almost nothing here.

### Using it from your controllers

Use constructor injection. The pattern is the same for a plain controller, a `RenderController` (route hijacking) or a `SurfaceController`:

```csharp
using Microsoft.AspNetCore.Mvc;
using MySite.Services;

namespace MySite.Controllers;

[Route("api/site")]
public class NewsLinkController : ControllerBase
{
    private readonly ISiteService _siteService;

    public NewsLinkController(ISiteService siteService) => _siteService = siteService;

    [HttpGet("news-url")]
    public IActionResult GetNewsUrl()
    {
        var newsSection = _siteService.GetNewsSection();
        return newsSection is null ? NotFound() : Content(newsSection.Url());
    }
}
```

`Url()` comes from `Umbraco.Extensions`. Add `using Umbraco.Extensions;` if your global usings don't already include it.

### Using it from your templates

```razor
@inject MySite.Services.ISiteService SiteService
@{
    var newsSection = SiteService.GetNewsSection();
}
@if (newsSection is not null)
{
    <a href="@newsSection.Url()">@newsSection.Name</a>
}
```

Always null-check the result. The news section may not exist yet, or it may be unpublished. For more on views, partials, view components and route hijacking, see the sibling skill `umbraco-templating`.

## Alternative: an extension method

If you'd rather not add a service, this is the lightweight option the docs show. You get the same correctness and there's nothing to register:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Extensions; // or your own namespace, imported where it's used

public static class PublishedContentQueryExtensions
{
    public static IPublishedContent? GetNewsSection(this IPublishedContentQuery query)
        => query.ContentAtRoot().FirstOrDefault()?.FirstChildOfType("newsSection");
}
```

Controllers inject `IPublishedContentQuery` and call `query.GetNewsSection()`. Views use `@inject IPublishedContentQuery Query`. This is fine: the extension method is static, but it holds **no state**. The scoped object is passed in on every call. The static *field* is what makes your colleague's version broken.

## One limitation: request-only

`SiteService` is scoped, so it only works **inside an HTTP request**: controllers and views, which covers your case. If you later need the news section from a hosted service, a scheduled job or a notification handler fired from the backoffice, don't inject `ISiteService` there. Instead, write a **singleton** that injects `IUmbracoContextFactory` and opens a short-lived context per call:

```csharp
using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
// read via ctx.UmbracoContext.Content ... and never store ctx or the content in a field
```

## Summary

| Class | Lifetime | Why | Registered in |
|---|---|---|---|
| `ISiteService` / `SiteService` | Scoped | Injects `IPublishedContentQuery`, which is scoped | `SiteServiceComposer` |
| `SiteServiceComposer` | n/a (`IComposer`, picked up automatically) | n/a | n/a |

Controllers get it through constructor injection, and views through `@inject ISiteService SiteService`.

## Verification status

I haven't compiled or run this against your project, because no project was provided. The `ISiteService` / `SiteService` / composer / controller pattern is a skill asset that compiles and has runtime tests against Umbraco 17.5.3. The `GetNewsSection()` method (using `FirstChildOfType`, which exists in `Umbraco.Web.Common` 17.5.3) and the `"newsSection"` alias are additions for your case and haven't been tested. Replace the alias and `MySite` namespace with yours, then build and load a page that uses it.
