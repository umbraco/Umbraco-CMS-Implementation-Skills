# Querying published content from a background job in Umbraco 17

## Why `TryGetUmbracoContext` returns false

`IUmbracoContextAccessor` doesn't create anything. It only hands back the `IUmbracoContext` that Umbraco's middleware set up for the **current HTTP request**. A timer or hosted service runs on a thread-pool thread with no request behind it, so the accessor has nothing to give you and `TryGetUmbracoContext` returns `false`. This is expected behaviour, not a bug.

In a background job you have two options:

1. **Create a context yourself** with `IUmbracoContextFactory.EnsureUmbracoContext()`. This is the documented pattern for code that runs outside a request. It is also the safest option for URL generation, because `IPublishedUrlProvider` still uses the Umbraco context internally.
2. **Skip `UmbracoContext` for content lookups.** Since v15 the published cache (`IPublishedContentCache`) and the navigation structure (`IDocumentNavigationQueryService`) are injectable services that don't need a request.

The example below does both. It creates a context with `EnsureUmbracoContext()` so that URL generation works, and it reads the content through the injected services.

## Use Umbraco's recurring background job, not a raw `IHostedService`

A plain `IHostedService` with a `Timer` works, but since v13 Umbraco provides `IRecurringBackgroundJob`. Register it with `AddRecurringBackgroundJob<T>()`. Umbraco's host then:

- waits until the runtime level is `Run` before it starts the job, so it won't run during install or upgrade
- runs the job only on the server roles you choose. The default is `Single` and `SchedulingPublisher`, so the job won't run on every load-balanced subscriber
- handles the period and initial delay for you

## The job

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace MySite.Jobs;

public class LatestNewsLoggerJob : IRecurringBackgroundJob
{
    private const string NewsDocTypeAlias = "news";        // adjust to your aliases
    private const string ArticleDocTypeAlias = "article";

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedContentCache _contentCache;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly ILogger<LatestNewsLoggerJob> _logger;

    public LatestNewsLoggerJob(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedContentCache contentCache,
        IDocumentNavigationQueryService navigation,
        IPublishedUrlProvider urlProvider,
        ILogger<LatestNewsLoggerJob> logger)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _contentCache = contentCache;
        _navigation = navigation;
        _urlProvider = urlProvider;
        _logger = logger;
    }

    // Run once a day...
    public TimeSpan Period => TimeSpan.FromDays(1);

    // ...first run at the next 02:00 server time.
    public TimeSpan Delay => GetDelayUntil(new TimeOnly(2, 0));

    // Default anyway; stated explicitly so only one server in a load-balanced setup runs it.
    public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

    public event EventHandler PeriodChanged { add { } remove { } }

    public Task RunJobAsync()
    {
        // No HTTP request here → create (or reuse) an UmbracoContext for the duration of the job.
        using UmbracoContextReference _ = _umbracoContextFactory.EnsureUmbracoContext();

        IPublishedContent? newsPage = FindNewsPage();
        if (newsPage is null)
        {
            _logger.LogWarning("LatestNewsLoggerJob: no published '{Alias}' page found", NewsDocTypeAlias);
            return Task.CompletedTask;
        }

        if (!_navigation.TryGetDescendantsKeysOfType(newsPage.Key, ArticleDocTypeAlias, out IEnumerable<Guid> articleKeys))
        {
            return Task.CompletedTask;
        }

        IEnumerable<IPublishedContent> latest = articleKeys
            .Select(key => _contentCache.GetById(key))       // returns null for unpublished items
            .OfType<IPublishedContent>()
            .OrderByDescending(a => a.UpdateDate)            // see note on "publish date" below
            .Take(5);

        foreach (IPublishedContent article in latest)
        {
            string url = _urlProvider.GetUrl(article, UrlMode.Absolute);
            _logger.LogInformation("Latest news: {Name} — {Url} (updated {Date:dd-MM-yyyy HH:mm})",
                article.Name, url, article.UpdateDate);
        }

        return Task.CompletedTask;
    }

    private IPublishedContent? FindNewsPage()
    {
        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return null;
        }

        foreach (Guid rootKey in rootKeys)
        {
            // News page directly under a root (Home) node; switch to TryGetDescendantsKeysOfType if it lives deeper.
            if (_navigation.TryGetChildrenKeysOfType(rootKey, NewsDocTypeAlias, out IEnumerable<Guid> newsKeys))
            {
                foreach (Guid key in newsKeys)
                {
                    if (_contentCache.GetById(key) is { } news)
                    {
                        return news;
                    }
                }
            }
        }

        return null;
    }

    private static TimeSpan GetDelayUntil(TimeOnly runAt)
    {
        DateTime now = DateTime.Now;
        DateTime next = now.Date.Add(runAt.ToTimeSpan());
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }
}
```

### Registration

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.DependencyInjection;

namespace MySite.Jobs;

public class LatestNewsLoggerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddRecurringBackgroundJob<LatestNewsLoggerJob>();
}
```

(You can also call `builder.Services.AddRecurringBackgroundJob<LatestNewsLoggerJob>()` directly in `Program.cs` before `.Build()`.)

## Things to watch

- **Lifetimes.** Recurring jobs are singletons. `IUmbracoContextFactory`, `IPublishedContentCache`, `IDocumentNavigationQueryService` and `IPublishedUrlProvider` are all safe to inject into a singleton. If you later need something scoped, such as your own `DbContext`, inject `IServiceScopeFactory` and create a scope inside `RunJobAsync`.
- **Absolute URLs need a hostname.** There's no request to take the host from. If the job logs relative URLs (`/news/my-article/`) or something unexpected, set a domain on the site root under **Culture and Hostnames**, or set `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` in `appsettings.json`.
- **"Most recently published".** `IPublishedContent` has no publish date. It only has `CreateDate` and `UpdateDate`, and `UpdateDate` is the date of the published version. That's normally close enough. If editors can republish old articles, use a dedicated date property instead (for example `article.Value<DateTime>("articleDate")`). The real `PublishDate` is on `IContent` through `IContentService`, but that queries the database rather than the cache. Avoid it unless you really need it.
- **Variants.** On a multilingual site, `Name` and `GetUrl` use the default culture unless you pass one: `article.Name(culture)`, `_urlProvider.GetUrl(article, UrlMode.Absolute, culture)`.
- **Many articles.** `TryGetDescendantsKeysOfType` reads the in-memory navigation structure, and `GetById` per key is cheap. This is fine for a nightly job, even with a few thousand articles. If you only need children (articles directly under News), use `TryGetChildrenKeysOfType`.
- **Exceptions.** Unhandled exceptions in `RunJobAsync` are logged by Umbraco's job runner, and the job runs again next period. Add your own try/catch if you want specific handling.

## Notes on accuracy

I wrote this from my knowledge of the Umbraco 13–17 APIs, without compiling it against 17. Please check these points:

- the namespaces for `IRecurringBackgroundJob` and `AddRecurringBackgroundJob` (`Umbraco.Cms.Infrastructure.BackgroundJobs` and `Umbraco.Cms.Infrastructure.DependencyInjection`)
- that the `*OfType` methods exist on `IDocumentNavigationQueryService`. They were added in v15. If they're missing, use `TryGetDescendantsKeys` and filter on `ContentType.Alias`.

Docs:
- Scheduling / recurring background jobs: https://docs.umbraco.com/umbraco-cms/reference/scheduling
- Accessing the published cache outside a request (`IUmbracoContextFactory.EnsureUmbracoContext`): https://docs.umbraco.com/umbraco-cms/reference/querying/umbraco-context
