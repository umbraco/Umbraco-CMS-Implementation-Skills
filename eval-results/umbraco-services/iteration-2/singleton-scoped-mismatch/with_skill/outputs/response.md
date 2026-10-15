# Why `SiteService` breaks in Development, and which fix to use

## What's wrong

A service's lifetime has to match what it depends on. `UmbracoHelper` is **scoped**: you get one per request, and it is tied to that request's `UmbracoContext`. You registered `SiteService` as a **singleton**, so one instance is created and kept for the life of the app. That instance would hold on to the `UmbracoHelper` from whichever scope created it first, and keep it forever.

- **Development:** ASP.NET Core turns on scope validation by default, so it catches this at startup and throws *"Cannot consume scoped service UmbracoHelper from singleton ISiteService"*.
- **Production:** scope validation is off by default, so the site starts. The bug is still there, just not reported. The singleton keeps using one stale, request-bound helper for every later request. That can lead to wrong or leaked data, or errors once that context is gone. Treat Development's error as the correct answer. It's the same kind of bug as storing `UmbracoHelper` in a static field.

## The two possible fixes

| Fix | Registration | Works in controllers/views | Works in a scheduled job |
|---|---|---|---|
| A. Keep `UmbracoHelper` and make the service scoped | `AddScoped<ISiteService, SiteService>()` | Yes | **No.** `UmbracoHelper` needs a request, and a job doesn't have one |
| B. Drop `UmbracoHelper`, inject only singletons, read through `IUmbracoContextFactory` | `AddSingleton<ISiteService, SiteService>()` | Yes | **Yes** |

**Since you want to call it from a scheduled job, use B.** A is fine for a service that only ever runs inside a request. A job has no HTTP request, though, so a request-bound helper won't work there. B gives you a single class that works both in requests and in background work.

## Fix B: a singleton that works with or without a request

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public interface ISiteService
{
    IPublishedContent? GetContactPage();
    IPublishedContent? GetNewsSection();
}

// Singleton: depends only on singletons. Opens (and disposes) its own UmbracoContext
// per call, so it works in requests AND in scheduled jobs / hosted services.
public class SiteService : ISiteService
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
    }

    public IPublishedContent? GetContactPage() => FindFirstOfType("contactPage");

    public IPublishedContent? GetNewsSection() => FindFirstOfType("newsSection");

    private IPublishedContent? FindFirstOfType(string contentTypeAlias)
    {
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextReference.UmbracoContext.Content;

        // The tree structure is held in memory, so this lookup doesn't query the database.
        if (content is null || !_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        foreach (Guid rootKey in rootKeys)
        {
            if (_navigation.TryGetDescendantsKeysOfType(rootKey, contentTypeAlias, out IEnumerable<Guid> keys))
            {
                IPublishedContent? match = keys
                    .Select(key => content.GetById(key))
                    .FirstOrDefault(c => c is not null);

                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }
}
```

Replace `"contactPage"` and `"newsSection"` with your actual Document Type aliases, and `MySite` with your namespace.

The composer stays a singleton, which is now correct:

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
        builder.Services.AddSingleton<ISiteService, SiteService>();
    }
}
```

### Rules that keep this safe

- **Dispose every `EnsureUmbracoContext()`.** The `using` above handles that. Open one context per call.
- **Don't store the context, or any `IPublishedContent` you read from it, in a field.** Look it up again each time. The cache is in memory, so this is cheap. If you cache anything, cache the **key** (`Guid`), not the content object.
- **Return `IPublishedContent?` and null-check in callers.** The page might not be published yet.
- **Writing data from the job?** Writes go through management services like `IContentService`, which hit the database. The published cache is read-only.
- If you see a circular-dependency error at startup involving `IUmbracoContextFactory` (sometimes caused by third-party packages), inject `Lazy<IUmbracoContextFactory>` and use `.Value` when you need it.

## Calling it

- **Controller:** inject `ISiteService` through the constructor.
- **View:** `@inject ISiteService SiteService`.
- **Scheduled job (`IRecurringBackgroundJob`, hosted service):** inject `ISiteService` through the constructor. Both the job and the service are singletons, so the scope check is satisfied, and the service creates its own `UmbracoContext`.

## If you went with fix A instead

Change the registration to `builder.Services.AddScoped<ISiteService, SiteService>()` and keep `UmbracoHelper`. Startup will work again, but only request-time code (controllers, views) can use the service. Don't inject it into a singleton job, because that brings the same startup error back.

## Verification status

I haven't compiled or run this snippet against your project, because no project was provided. The pattern (singleton + `IUmbracoContextFactory` + `IDocumentNavigationQueryService.TryGetRootKeys`, and scoped registration for `UmbracoHelper`-based services) comes from the skill's assets, which have been verified on Umbraco 17.5.3. `TryGetDescendantsKeysOfType` is my addition for finding pages by type, and it isn't in those verified assets. To check it, build the project and request a page that uses the service. It's also worth starting once in Development, since the scope check there is what caught the original bug.
