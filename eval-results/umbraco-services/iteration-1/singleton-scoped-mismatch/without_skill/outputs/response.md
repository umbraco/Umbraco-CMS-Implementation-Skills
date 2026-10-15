# Why `SiteService` fails in Development, and the right fix

## What's wrong

There are two problems, and the error message only tells you about the first one.

### 1. A captive dependency (lifetime mismatch)

`UmbracoHelper` is registered as **scoped**: one instance per HTTP request. Your `SiteService` is a **singleton**: one instance for the whole application lifetime. When the container builds the singleton, it has to inject a `UmbracoHelper` and then keeps that one instance forever. The scoped object is now "captured" by the singleton and outlives the request it belonged to.

### 2. `UmbracoHelper` belongs to a request

`UmbracoHelper` wraps the current `IUmbracoContext`, and that context is created per request by Umbraco's middleware. If a singleton holds on to it, you end up reading through a context from a request that has already finished and been disposed. You may also hit a published-content snapshot that's out of date. And if the singleton gets built outside a request (at startup, or from a background job), there's no Umbraco context at all.

### Why it only fails in Development

ASP.NET Core's default host turns on **scope validation** (`ServiceProviderOptions.ValidateScopes` / `ValidateOnBuild`) only when the environment is `Development`. In Development the container spots the singleton-to-scoped dependency and throws `Cannot consume scoped service ... from singleton ...`.

In Production that check is off, so the app starts. **The bug is still there, though.** The singleton silently keeps the first `UmbracoHelper` it got. Expect intermittent errors such as `ObjectDisposedException` or "no UmbracoContext", stale content after publishing, or behaviour that changes depending on which request touched the service first. Development is doing you a favour here, so don't switch the validation off.

## The options

| Fix | Works in requests | Works in a scheduled job | Verdict |
|---|---|---|---|
| Turn off `ValidateScopes` | No (it only hides the bug) | No | Never |
| Make `SiteService` scoped and keep `UmbracoHelper` | Yes | **No.** A job has no HTTP request, so there's no `UmbracoHelper` or Umbraco context to inject | Fine only for request-only code |
| Keep it a singleton, drop `UmbracoHelper`, and use `IUmbracoContextFactory` plus singleton content/navigation services | Yes | Yes | **Recommended** |
| Resolve `UmbracoHelper` through `IServiceProvider` inside the singleton | No (same captive problem, just hidden) | No | Avoid |

Since you want to call the service from a scheduled job later, **don't depend on `UmbracoHelper` at all**. It's a convenience for views and controllers, not for services. Instead:

- Inject `IUmbracoContextFactory`, which is a singleton. Inside each method call, run `using var ctx = _umbracoContextFactory.EnsureUmbracoContext();`. During a web request this reuses the request's existing context. In a background job, where there's no request, it creates a temporary one and disposes it at the end of the `using`.
- Use `IDocumentNavigationQueryService`, also a singleton, to walk the tree. In Umbraco 15+ the content cache no longer holds the tree structure. Navigation lives in these services, and `GetAtRoot()`-style APIs on the cache are obsolete.
- Never store `IPublishedContent` or the context in fields. Fetch what you need inside each call.

## Recommended implementation (Umbraco 17)

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

public interface ISiteService
{
    IPublishedContent? GetContactPage();
    IPublishedContent? GetNewsSection();
}

public sealed class SiteService : ISiteService
{
    // Adjust to your actual document type aliases
    private const string ContactPageAlias = "contactPage";
    private const string NewsSectionAlias = "newsSection";

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;

    public SiteService(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
    }

    public IPublishedContent? GetContactPage() => FindFirstOfType(ContactPageAlias);

    public IPublishedContent? GetNewsSection() => FindFirstOfType(NewsSectionAlias);

    private IPublishedContent? FindFirstOfType(string contentTypeAlias)
    {
        // Reuses the request's context if there is one; creates (and disposes) one otherwise.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var contentCache = contextReference.UmbracoContext.Content;
        if (contentCache is null)
        {
            return null;
        }

        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        foreach (Guid rootKey in rootKeys)
        {
            if (!_navigation.TryGetDescendantsKeysOfType(rootKey, contentTypeAlias, out IEnumerable<Guid> keys))
            {
                continue;
            }

            foreach (Guid key in keys)
            {
                // Navigation includes unpublished nodes; the published cache returns null for those.
                IPublishedContent? node = contentCache.GetById(key);
                if (node is not null)
                {
                    return node;
                }
            }
        }

        return null;
    }
}
```

Registration doesn't change. A singleton is now correct because every dependency is a singleton:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public sealed class SiteServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddSingleton<ISiteService, SiteService>();
}
```

**Multi-site note:** if you host more than one site, "the first match across all roots" is probably wrong. Pass in the site root (for example `Model.Root()` in a view, or a configured root key in the job) and search below that root only.

**Optional:** if you call this a lot, you can cache the **keys** (`Guid`), never the `IPublishedContent`. Clear that cache with a `ContentCacheRefresherNotification` handler so moving, publishing or deleting nodes is picked up, including in load-balanced setups.

## Using it from a scheduled job later

In Umbraco 17, recurring jobs implement `IRecurringBackgroundJob` and are registered with `builder.Services.AddRecurringBackgroundJob<T>()`. Jobs are effectively **singletons**, so a scoped `SiteService` would hit exactly the same captive-dependency error there. The singleton built on `IUmbracoContextFactory` can just be injected:

```csharp
using Umbraco.Cms.Infrastructure.BackgroundJobs;

public sealed class NewsDigestJob : IRecurringBackgroundJob
{
    private readonly ISiteService _siteService;
    private readonly ILogger<NewsDigestJob> _logger;

    public NewsDigestJob(ISiteService siteService, ILogger<NewsDigestJob> logger)
    {
        _siteService = siteService;
        _logger = logger;
    }

    public TimeSpan Period => TimeSpan.FromHours(1);
    public TimeSpan Delay => TimeSpan.FromMinutes(1);
    public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];
    public event EventHandler PeriodChanged { add { } remove { } }

    public Task RunJobAsync()
    {
        var news = _siteService.GetNewsSection();
        _logger.LogInformation("News section: {Name}", news?.Name ?? "(not found)");
        return Task.CompletedTask;
    }
}
```

`SiteService` creates its own Umbraco context inside the call, so the job works without a request. Do everything you need with the returned `IPublishedContent` **during that run**; don't keep it for the next one. If the job also needs something that really is scoped (an EF Core `DbContext`, for example), inject `IServiceScopeFactory` and create a scope inside `RunJobAsync`.

## Summary

- **Cause:** a singleton is capturing the request-scoped `UmbracoHelper`. Development catches it through scope validation. Production hides it, but the bug is real there too.
- **Quick fix for request-only code:** register the service as scoped. That breaks again as soon as a job uses it.
- **Right fix for your plans:** keep the service a singleton, remove `UmbracoHelper`, and inside each call use `IUmbracoContextFactory.EnsureUmbracoContext()` together with `IDocumentNavigationQueryService` and the published content cache. The same service then works from views, controllers and background jobs.

*Note: this answer is from general knowledge of Umbraco 15–17 and ASP.NET Core DI, without checking against the Umbraco 17 source. Before you rely on them, confirm the exact signatures (for example `TryGetDescendantsKeysOfType` and the `IRecurringBackgroundJob` members) against your installed version.*
