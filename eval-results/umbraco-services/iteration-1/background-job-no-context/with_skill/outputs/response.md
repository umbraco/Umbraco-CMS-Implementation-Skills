# Reading published content in a nightly job (Umbraco 17)

## Why `TryGetUmbracoContext` returns false

`IUmbracoContextAccessor` only gives you the `UmbracoContext` that already exists for the current front-end request. A hosted service or timer runs with **no HTTP request**, so there's nothing for it to find. Content finders and URL providers can use the accessor because they run inside the routing pipeline. A background job can't.

You can't inject `IPublishedContentQuery` or `UmbracoHelper` into the job either. Both are **scoped** services. A singleton or hosted service that asks for one gets *"Cannot resolve scoped service … from root provider"* or *"Cannot consume scoped service from singleton"*.

Don't switch to `IContentService` as a workaround. It's a management service that queries the **database** on every call. Read from the published cache instead.

## The fix: `IUmbracoContextFactory.EnsureUmbracoContext()`

1. Inject `IUmbracoContextFactory` into a **singleton**.
2. Open a short-lived context around each read, inside a `using`.
3. Get the tree structure (root, News page, its children) from `IDocumentNavigationQueryService`. It's held in memory, so there's no database query.
4. Turn everything into plain values before the `using` ends. Don't keep the context, or any `IPublishedContent` read from it, in a field.

This is the pattern from the Umbraco 17 docs: [Services and Helpers](https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md), "Accessing the Published Content Cache".

For the scheduling, I'd use Umbraco's own **`IRecurringBackgroundJob`** (via `RecurringBackgroundJobBase`) rather than a hand-made `IHostedService` + timer. Umbraco runs it only on the scheduling server: the single server, or the `SchedulingPublisher` in a load-balanced setup. That way the digest isn't logged once per instance. If you'd rather keep your `IHostedService`, the reader below works unchanged. Inject it and call `GetLatest(5)` from your timer callback.

### `NewsDigest/LatestNewsReader.cs`

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.NewsDigest;

public record NewsItem(string Name, string Url, DateTime UpdateDate);

// Singleton: depends only on singletons, so it works with no HTTP request (background jobs).
public class LatestNewsReader
{
    // Adjust to your Document Type aliases.
    private const string NewsPageAlias = "newsPage";
    private const string ArticleAlias = "article";

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedUrlProvider _urlProvider;

    public LatestNewsReader(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation,
        IPublishedUrlProvider urlProvider)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
        _urlProvider = urlProvider;
    }

    public IReadOnlyList<NewsItem> GetLatest(int count = 5)
    {
        // Opens a short-lived UmbracoContext (there is none in a background job). Dispose it when done.
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        IPublishedContentCache? cache = contextReference.UmbracoContext.Content;
        if (cache is null || !_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return [];
        }

        // Navigation keys include unpublished nodes; GetById on the published cache returns null for those.
        var news = new List<IPublishedContent>();
        foreach (Guid rootKey in rootKeys)
        {
            if (_navigation.TryGetChildrenKeysOfType(rootKey, NewsPageAlias, out IEnumerable<Guid> newsKeys))
            {
                news.AddRange(newsKeys.Select(k => cache.GetById(k)).OfType<IPublishedContent>());
            }
        }

        IPublishedContent? newsPage = news.FirstOrDefault();
        if (newsPage is null
            || !_navigation.TryGetChildrenKeysOfType(newsPage.Key, ArticleAlias, out IEnumerable<Guid> articleKeys))
        {
            return [];
        }

        return articleKeys
            .Select(k => cache.GetById(k))
            .OfType<IPublishedContent>()
            .OrderByDescending(a => a.UpdateDate)
            .Take(count)
            // Materialise to plain values before the context is disposed.
            .Select(a => new NewsItem(a.Name, _urlProvider.GetUrl(a, UrlMode.Absolute), a.UpdateDate))
            .ToList();
    }
}
```

### `NewsDigest/LatestNewsJob.cs`

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace MySite.NewsDigest;

// Umbraco's recurring background job: runs once a day, only on the scheduling server
// (single server, or the SchedulingPublisher in a load-balanced setup).
public class LatestNewsJob : RecurringBackgroundJobBase
{
    private readonly LatestNewsReader _reader;
    private readonly ILogger<LatestNewsJob> _logger;

    public LatestNewsJob(LatestNewsReader reader, ILogger<LatestNewsJob> logger)
        : base(TimeSpan.FromDays(1))
    {
        _reader = reader;
        _logger = logger;
    }

    // First run at the next 02:00 server time, then every 24 hours.
    public override TimeSpan Delay
    {
        get
        {
            DateTime now = DateTime.Now;
            DateTime next = now.Date.AddHours(2);
            if (next <= now)
            {
                next = next.AddDays(1);
            }

            return next - now;
        }
    }

    public override Task RunJobAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<NewsItem> items = _reader.GetLatest(5);
        _logger.LogInformation("Nightly news digest: {Count} article(s)", items.Count);
        foreach (NewsItem item in items)
        {
            _logger.LogInformation("News: {Name} {Url} (updated {UpdateDate})", item.Name, item.Url, item.UpdateDate);
        }

        return Task.CompletedTask;
    }
}
```

### `NewsDigest/NewsDigestComposer.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace MySite.NewsDigest;

public class NewsDigestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<LatestNewsReader>();
        builder.Services.AddRecurringBackgroundJob<LatestNewsJob>();
    }
}
```

## What each class uses, and why

| Class | Lifetime / registration | Uses | Why |
|---|---|---|---|
| `LatestNewsReader` | `AddSingleton` in `NewsDigestComposer` | `IUmbracoContextFactory`, `IDocumentNavigationQueryService`, `IPublishedUrlProvider` (all singletons) | It reads the **published cache** (no DB hits) and works with no request. A singleton is correct because none of its dependencies are scoped. |
| `LatestNewsJob` | `AddRecurringBackgroundJob<LatestNewsJob>()` in the same composer | `LatestNewsReader`, `ILogger` | Runs every 24 h, first at the next 02:00 server time. It only runs on the scheduling server. |

## Things to adjust or know

- **Aliases.** Set `NewsPageAlias` and `ArticleAlias` to your Document Type aliases. The reader looks for the News page as a direct child of a root node. If your articles are nested under it (for example, year/month folders), use `TryGetDescendantsKeysOfType` instead of `TryGetChildrenKeysOfType` for the articles. If you have several sites, each with a News page, the reader currently takes the first one it finds.
- **"Most recently published".** `IPublishedContent` has no `PublishDate`. `UpdateDate` is the date of the current published version, so re-publishing an old article moves it to the top. If your articles have a date property (e.g. `articleDate`), sort by `a.Value<DateTime>("articleDate")` instead.
- **Unpublished nodes.** The navigation service also returns keys of unpublished nodes. `cache.GetById` returns `null` for those, and `.OfType<IPublishedContent>()` drops them.
- **Absolute URLs with no request.** `UrlMode.Absolute` needs a host. If the News site has a culture/hostname (domain) assigned in the backoffice, that host is used. Without one, the host Umbraco knows about may not be the public one (for example, before the first request has hit the site). Set `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` in `appsettings.json` to your public URL so background work gets the right one. If relative URLs are fine for your log, use `UrlMode.Relative` or `UrlMode.Default`.
- **`Delay` and `Period`.** `Delay` sets when the first run happens (here, the next 02:00 server time). After that the job repeats every `Period` (24 h). An app restart re-runs the `Delay` calculation.
- **Circular dependency at startup** (rare, usually third-party code): inject `Lazy<IUmbracoContextFactory>` and call `.Value` inside `GetLatest`.

## Verification

All three files compile against `Umbraco.Cms.Web.Website` **17.5.3** with no errors and no compiler warnings, which means no obsolete-API warnings. One signature detail I caught while compiling: on 17 the method to override is `RunJobAsync(CancellationToken)`. The parameterless `RunJobAsync()` isn't overridable.

I didn't run it against a live site, so the actual log output (names and absolute URLs) is **not runtime-verified**. To check it, temporarily set the `Delay` to `TimeSpan.FromSeconds(30)`, start the site and watch the log.
