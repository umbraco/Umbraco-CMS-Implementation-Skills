# Why `IPublishedContentQueryAccessor.TryGetValue` fails in your nightly job

## What's going on

This has nothing to do with it being night. It comes down to whether an **HTTP request** is in flight.

`IPublishedContentQueryAccessor` is registered as a singleton, but it does not hold an `IPublishedContentQuery` of its own. `IPublishedContentQuery` is a **request-scoped** service. The default accessor gets it from the current request's service provider, roughly like this:

```csharp
// simplified: Umbraco.Cms.Web.Common...PublishedContentQueryAccessor
public bool TryGetValue(out IPublishedContentQuery publishedContentQuery)
{
    publishedContentQuery = _httpContextAccessor.HttpContext?.RequestServices
        .GetService<IPublishedContentQuery>();
    return publishedContentQuery is not null;
}
```

- **From a controller:** a request is running, so `HttpContext` exists and an `UmbracoContext` has already been created for it. `TryGetValue` returns `true`.
- **From an `IHostedService` / timer / background job:** no request is running, so `HttpContext` is `null` and `TryGetValue` returns `false`. Every call does this, whatever the time of day. If you started the hosted service at 14:00 it would still fail.

DI accepts this setup because the accessor is a singleton. It exists precisely so singletons can reach request-scoped services *while a request is running*. It cannot create a request for you. Other "accessor" types behave the same way: `IUmbracoContextAccessor.TryGetUmbracoContext` also returns `false` in a background thread unless you create a context first.

So the docs advice is only half right for your case. It is correct that you shouldn't read content through `IContentService`, because that hits the database and returns unpublished drafts. But the accessor only covers request-bound code. Background code has to set up its own context.

## The right fix

You need two changes:

1. **Use Umbraco's background-job abstraction instead of a raw `IHostedService`.** In Umbraco 10+ that is `IRecurringBackgroundJob`. It runs only once Umbraco has fully booted, and it respects server roles, so in a load-balanced setup it does not run on every node.
2. **Inside the job, create an `UmbracoContext` yourself** with `IUmbracoContextFactory.EnsureUmbracoContext()`, then read from the published content cache. Alternatively, in v15+ you can use the published cache and navigation services directly, since those no longer need an `UmbracoContext`.

### Option A: `EnsureUmbracoContext` (works in both v13 and v17 code)

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.BackgroundJobs;
using Umbraco.Extensions;

public sealed class NewsDigestJob : IRecurringBackgroundJob
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILogger<NewsDigestJob> _logger;

    public NewsDigestJob(IUmbracoContextFactory umbracoContextFactory, ILogger<NewsDigestJob> logger)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _logger = logger;
    }

    public TimeSpan Period => TimeSpan.FromDays(1);
    public TimeSpan Delay => TimeUntilNext(new TimeOnly(2, 0)); // first run at 02:00 server time

    // Only run on the scheduling publisher / single server, not on every LB subscriber
    public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

    public event EventHandler PeriodChanged { add { } remove { } }

    public Task RunJobAsync()
    {
        // Creates an UmbracoContext for this background thread; disposing the reference cleans it up.
        using UmbracoContextReference ctxRef = _umbracoContextFactory.EnsureUmbracoContext();
        var contentCache = ctxRef.UmbracoContext.Content!;

        IPublishedContent? news = contentCache.GetAtRoot()
            .SelectMany(root => root.DescendantsOrSelfOfType("newsOverview")) // your News doc type alias
            .FirstOrDefault();

        if (news is null)
        {
            _logger.LogWarning("News digest: no News node found");
            return Task.CompletedTask;
        }

        var latest = news.Children()
            .OrderByDescending(x => x.Value<DateTime?>("publishDate") ?? x.CreateDate)
            .Take(5)
            .ToList();

        // ...build and send the digest...
        return Task.CompletedTask;
    }

    private static TimeSpan TimeUntilNext(TimeOnly at)
    {
        var now = DateTime.Now;
        var next = now.Date + at.ToTimeSpan();
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
```

Register it in a composer, and remove your `AddHostedService<...>()` call:

```csharp
public sealed class NewsDigestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddRecurringBackgroundJob<NewsDigestJob>();
}
```

### Option B: Umbraco 15+ (including 17) without `UmbracoContext`

Since v15 the published content cache runs on HybridCache, and tree structure comes from the navigation services. Both are injectable singletons that work outside a request:

- `IPublishedContentCache`: `GetById(Guid key)` / `GetByIdAsync(...)`
- `IDocumentNavigationQueryService`: `TryGetRootKeys`, `TryGetChildrenKeys`, `TryGetChildrenKeysOfType`, `TryGetDescendantsKeysOfType`, and similar

```csharp
public sealed class NewsDigestJob(
    IPublishedContentCache contentCache,
    IDocumentNavigationQueryService navigation,
    IUmbracoContextFactory umbracoContextFactory) : IRecurringBackgroundJob
{
    // Period / Delay / ServerRoles / PeriodChanged as above

    public async Task RunJobAsync()
    {
        // Still a good idea: some extension methods / value converters (e.g. URL generation,
        // media pickers) expect an UmbracoContext.
        using var _ = umbracoContextFactory.EnsureUmbracoContext();

        if (!navigation.TryGetRootKeys(out var rootKeys)) return;

        Guid? newsKey = rootKeys
            .SelectMany(r => navigation.TryGetDescendantsKeysOfType(r, "newsOverview", out var k) ? k : [])
            .Cast<Guid?>()
            .FirstOrDefault();
        if (newsKey is null || !navigation.TryGetChildrenKeys(newsKey.Value, out var childKeys)) return;

        var latest = new List<IPublishedContent>();
        foreach (var key in childKeys)
        {
            if (await contentCache.GetByIdAsync(key) is { } item) latest.Add(item);
        }

        var top5 = latest
            .OrderByDescending(x => x.Value<DateTime?>("publishDate") ?? x.CreateDate)
            .Take(5);
        // ...
    }
}
```

The navigation structure includes unpublished nodes. In v17, `GetById` on the published cache returns `null` for anything that isn't published, so the `is { } item` check filters those out. If you want to be explicit about it, v16+ also has `IPublishedContentStatusFilteringService` for this. Check the exact method names against your 17.x version: the navigation API was added in v15 and has grown in each minor release since.

## Other things to check

- **Culture:** a background thread has no request culture. On a multilingual site, set the culture explicitly before reading values, either with `IVariationContextAccessor.VariationContext = new VariationContext("en-US")` or by passing `culture:` to `Value(...)` / `Name(...)`.
- **Absolute URLs in the digest email:** with no request, Umbraco cannot infer the host. Assign domains to your root nodes (Culture and Hostnames), or configure `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` so that `Url(mode: UrlMode.Absolute)` returns something sensible.
- **Don't capture the context:** create the `UmbracoContextReference` inside each run and dispose it at the end of that run. Don't create one in the constructor and keep it across nightly runs, because you'd be reading a stale snapshot.
- **Scoped services:** if the job needs other scoped services (your own `DbContext`, `IPublishedContentQuery`, etc.), inject `IServiceScopeFactory`, call `CreateScope()` *after* `EnsureUmbracoContext()`, and resolve them from that scope. `IPublishedContentQuery` resolved this way works once an `UmbracoContext` exists.
- **Don't fall back to `IContentService`:** it would "work" at night, but it reads from the database, returns drafts, and is slow. Keep it for writes.

## Summary

| Context | `IPublishedContentQueryAccessor` | What to use |
|---|---|---|
| Controller / view / request pipeline | Works (or inject `IPublishedContentQuery` directly) | Accessor or `IPublishedContentQuery` |
| `IHostedService`, `IRecurringBackgroundJob`, timers, notification handlers fired off-request | Always `false` | `IUmbracoContextFactory.EnsureUmbracoContext()` and `UmbracoContext.Content`, or on v15+ `IPublishedContentCache` with `IDocumentNavigationQueryService` |

Sources (from memory of the Umbraco docs and source; check them against your exact 17.x version):
- Umbraco docs, "Using IPublishedContentQuery" / "IPublishedContentQueryAccessor" (Reference > Querying)
- Umbraco docs, "Scheduling with IRecurringBackgroundJob" (Reference > Scheduling)
- Umbraco docs, "Accessing published content outside of a HTTP request" (`IUmbracoContextFactory.EnsureUmbracoContext`)
- Umbraco CMS source: `PublishedContentQueryAccessor`, `UmbracoContextFactory`, `IDocumentNavigationQueryService`
