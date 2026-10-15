# Reading published content from a nightly job in Umbraco 17

## Why `TryGetUmbracoContext` returns false

`IUmbracoContextAccessor` only returns a context that already exists. Umbraco creates one for each front-end HTTP request. A hosted service runs on a timer, outside any request, so there is no context for the accessor to return. That's expected behaviour, not a bug.

You'll hit a related error if you try to inject `IPublishedContentQuery` or `UmbracoHelper` instead. Both are request-scoped, so a singleton hosted service can't get them: DI throws *"Cannot resolve scoped service … from root provider"*.

**The fix:** inject `IUmbracoContextFactory` and create a context yourself with `EnsureUmbracoContext()`. Put it in a `using` block for each run so it gets disposed. Use `IDocumentNavigationQueryService` to walk the tree; it's an in-memory structure, so it doesn't query the database.

Two rules:

- **Read from the published cache, not `IContentService`.** `IContentService` is for changing content. Every call takes a read lock and queries the database, and it returns draft data rather than what's live on the site.
- **Never store the context, or any `IPublishedContent` you got from it, in a field.** Create a new context for each run and copy out only the plain values you need (name, URL, date).

## 1. Reader service (singleton, works without a request)

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public sealed record LatestArticle(string Name, string Url, DateTime UpdateDate);

// No HTTP request: singleton + IUmbracoContextFactory, one disposed context per read.
public class LatestNewsReader
{
    private const string NewsPageName = "News";      // or match on a content type alias, see below
    private const string ArticleTypeAlias = "article"; // set to your article Document Type alias

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

    public IReadOnlyList<LatestArticle> GetLatestArticles(int count = 5)
    {
        using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = contextReference.UmbracoContext.Content;
        if (cache is null || !_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            return [];
        }

        // Find the News page under the site root(s), using the in-memory tree (no DB query).
        IPublishedContent? news = rootKeys
            .SelectMany(rootKey => _navigation.TryGetChildrenKeys(rootKey, out IEnumerable<Guid> keys) ? keys : [])
            .Select(key => cache.GetById(key))
            .FirstOrDefault(c => c is not null && c.Name == NewsPageName);

        if (news is null || !_navigation.TryGetDescendantsKeys(news.Key, out IEnumerable<Guid> descendantKeys))
        {
            return [];
        }

        // GetById on the published cache returns null for unpublished items, so only live articles remain.
        return descendantKeys
            .Select(key => cache.GetById(key))
            .OfType<IPublishedContent>()
            .Where(c => c.ContentType.Alias == ArticleTypeAlias)
            .OrderByDescending(c => c.UpdateDate)
            .Take(count)
            .Select(c => new LatestArticle(c.Name, _urlProvider.GetUrl(c, UrlMode.Absolute), c.UpdateDate))
            .ToList(); // copy out plain values before the context is disposed
    }
}
```

## 2. The nightly hosted service

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using MySite.Services;

namespace MySite.Jobs;

public class NightlyLatestNewsJob : BackgroundService
{
    private static readonly TimeSpan RunAt = new(2, 0, 0); // 02:00 server local time

    private readonly LatestNewsReader _reader;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<NightlyLatestNewsJob> _logger;

    public NightlyLatestNewsJob(LatestNewsReader reader, IRuntimeState runtimeState, ILogger<NightlyLatestNewsJob> logger)
    {
        _reader = reader;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DelayUntilNextRun(), stoppingToken);

            // Skip if Umbraco isn't fully running (install/upgrade in progress).
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                continue;
            }

            try
            {
                foreach (LatestArticle article in _reader.GetLatestArticles(5))
                {
                    _logger.LogInformation("Latest news: {Name} {Url} (updated {UpdateDate})",
                        article.Name, article.Url, article.UpdateDate);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Nightly latest-news job failed");
            }
        }
    }

    private static TimeSpan DelayUntilNextRun()
    {
        DateTime now = DateTime.Now;
        DateTime next = now.Date + RunAt;
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
```

## 3. Registration

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.Jobs;
using MySite.Services;

namespace MySite.Composers;

public class LatestNewsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Singleton: every dependency (IUmbracoContextFactory, navigation, URL provider) is a singleton
        // and the class holds no per-request state.
        builder.Services.AddSingleton<LatestNewsReader>();
        builder.Services.AddHostedService<NightlyLatestNewsJob>();
    }
}
```

## Things to check for your site

- **How to find "News".** The code above matches the page by name, which is simple but breaks if an editor renames the page. Matching on the News page's Document Type alias (`c.ContentType.Alias == "newsList"` or whatever yours is called) is more robust. With multiple sites, you may also want to pick a specific root.
- **"Most recently published".** `IPublishedContent` has no publish-date property. `UpdateDate` on the published item is when its current published version was saved, which is usually close enough. If your articles have their own date property (for example `articleDate`), sort on `c.Value<DateTime>("articleDate")` instead.
- **Children or descendants.** `TryGetDescendantsKeys` also picks up articles in sub-folders such as year/month folders. If articles always sit directly under News, use `TryGetChildrenKeys(news.Key, …)` instead.
- **Absolute URLs need a known host.** Outside a request there's no incoming host name to build URLs from. Assign a domain to the site root under *Culture and Hostnames*, or set `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` in `appsettings.json`. Without one of these you may get relative URLs or `#`. I haven't verified this part at runtime, so check the logged URLs on your site.
- **Load-balanced setups.** A plain `IHostedService` runs on every server. If you run more than one instance and only want one log entry per night, use Umbraco's `IRecurringBackgroundJob` instead (register it with `builder.Services.AddRecurringBackgroundJob<T>()`). It only runs on the scheduling-publisher server by default. I haven't verified this API in this answer, so check it against the Umbraco 17 docs before relying on it.

## Verification status

The reading pattern (singleton + `IUmbracoContextFactory.EnsureUmbracoContext()` + `IDocumentNavigationQueryService`) comes from the skill's reference asset. That asset compiles on Umbraco 17.5.3/18.2.0 and is runtime-tested on 17.5.3. The News/article lookup, the URL generation and the hosted-service timer above are adapted from it but haven't been compiled or run against a project. Build it in your solution and check the log output after the first run. To test it without waiting until 02:00, temporarily replace `DelayUntilNextRun()` with a short delay such as `TimeSpan.FromSeconds(30)`.
