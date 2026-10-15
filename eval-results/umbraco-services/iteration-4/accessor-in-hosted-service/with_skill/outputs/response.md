# Why `IPublishedContentQueryAccessor` is empty in your nightly job, and what to use instead

## What's going on

The DI wiring is fine. The problem is that **`IPublishedContentQueryAccessor` only has a value while an HTTP request is in flight**.

- `IPublishedContentQuery` is a **scoped** (per-request) service, so a singleton can't take it directly. Umbraco gives you the accessor so a singleton can reach the instance that belongs to the *current request*.
- The default implementation (`PublishedContentQueryAccessor`) takes an `IScopedServiceProvider`. In Umbraco's XML docs that type "provides access to a **request scoped** service provider when available". `TryGetValue` resolves `IPublishedContentQuery` from that provider, and returns `false` when there isn't one.
- **From the controller:** the code runs inside a request. There is an `HttpContext.RequestServices`, so `TryGetValue` returns `true`.
- **At night:** your `IHostedService` runs on a background thread with no request, so there is no request-scoped provider and `TryGetValue` returns `false` every time.

Umbraco says this itself in the 17.5.3 XML docs on `IPublishedContentQueryAccessor`:

> Not intended for use in background threads where you should make use of `IUmbracoContextFactory.EnsureUmbracoContext` and instead resolve `IPublishedContentQuery` from a `IServiceScope` …

So you were right not to read with `IContentService`. That service hits the database on every call and is for changing content. But the accessor is the wrong replacement in a background job. Use the published cache directly.

## The fix

A hosted service has no request, so the reader should be a **singleton** that uses the singleton-safe cache APIs. There are two verified options:

| | Inject | Use when |
|---|---|---|
| **A** | `IUmbracoContextFactory`, then open `using var ctx = factory.EnsureUmbracoContext();` **for each run** | The digest builds **URLs** to the articles. `IPublishedUrlProvider` throws *"Wasn't able to get an UmbracoContext"* without a context. |
| **B** | `IPublishedContentCache` (an injectable singleton since Umbraco 15) | You only need names, dates and properties. No context needed. |

In both cases, use `IDocumentNavigationQueryService` to walk the tree. It lives in memory and costs no DB query. A digest email almost always contains links, so the example uses **A**, which also gives you the cache.

```csharp
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace MySite.Services;

public sealed record DigestItem(string Name, string? Url, DateTime PublishDate);

// Singleton: every dependency is a singleton, and nothing request-bound is held in fields.
public class NewsDigestReader
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedUrlProvider _urlProvider;

    public NewsDigestReader(
        IUmbracoContextFactory umbracoContextFactory,
        IDocumentNavigationQueryService navigation,
        IPublishedUrlProvider urlProvider)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _navigation = navigation;
        _urlProvider = urlProvider;
    }

    public IReadOnlyList<DigestItem> GetLatestNews(int count = 5)
    {
        // A: one context per run, disposed at the end. Never store it (or content read from it) in a field.
        using UmbracoContextReference reference = _umbracoContextFactory.EnsureUmbracoContext();
        var cache = reference.UmbracoContext.Content;
        if (cache is null) return [];

        // Find the 'News' node: root -> children of type "news" (adjust aliases to your doc types).
        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys)) return [];

        Guid? newsKey = rootKeys
            .SelectMany(root => _navigation.TryGetChildrenKeysOfType(root, "news", out var keys) ? keys : [])
            .Cast<Guid?>()
            .FirstOrDefault();
        if (newsKey is null) return [];

        if (!_navigation.TryGetChildrenKeysOfType(newsKey.Value, "article", out IEnumerable<Guid> articleKeys))
            return [];

        return articleKeys
            .Select(key => cache.GetById(key))       // navigation includes unpublished nodes; the cache returns null for those
            .OfType<IPublishedContent>()
            .OrderByDescending(a => a.CreateDate)     // or a custom "publishDate" property: a.Value<DateTime>("publishDate")
            .Take(count)
            .Select(a => new DigestItem(a.Name, _urlProvider.GetUrl(a, UrlMode.Absolute), a.CreateDate))
            .ToList();
    }
}
```

Register it as a singleton in a composer, and call it from your hosted service:

```csharp
using Umbraco.Cms.Core.Composing;

public class NewsDigestComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<NewsDigestReader>();
        builder.Services.AddHostedService<NightlyDigestService>(); // if not registered already
    }
}
```

In `NightlyDigestService`, replace `IPublishedContentQueryAccessor` with `NewsDigestReader` and call `GetLatestNews()` on each run. Don't cache the results between runs. Build plain DTOs, as shown, and send those.

**Option B (no URLs needed):** inject `IPublishedContentCache` instead of `IUmbracoContextFactory` and `IPublishedUrlProvider`. Then use `_publishedContentCache.GetById(key)` in the `Select`, with no `using` block.

### Other route (from the Umbraco XML docs, not the skill's verified path)

If you really want `IPublishedContentQuery`, for example for its Examine search helpers, Umbraco's own remark shows this pattern:

```csharp
using UmbracoContextReference _ = _umbracoContextFactory.EnsureUmbracoContext();
using IServiceScope scope = _serviceProvider.CreateScope();
var query = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
```

It works, but you don't need it to get the "5 newest children of a node" result. Options A and B are simpler and need no extra scope.

## Things to check

- **Doc type aliases:** `"news"` and `"article"` are placeholders. If the site has several roots, or News isn't a direct child of a root, use `TryGetDescendantsKeysOfType` instead.
- **"Newest":** `CreateDate` is when the node was created. If editors set a publish-date property, sort by that instead.
- **Absolute URLs without a request:** Umbraco needs a hostname to build them, either from Culture and Hostnames on the site root or from `Umbraco:CMS:WebRouting:UmbracoApplicationUrl`. Otherwise you may get relative URLs. I haven't verified this for your setup.
- **Load-balanced setup:** if you run several servers, run the job only on the scheduling server (`IServerRoleAccessor.CurrentServerRole`), or the digest goes out once per server.

## Verification status

- The fact that the accessor resolves from the request-scoped provider, and the "not intended for background threads" remark, both come from the Umbraco **17.5.3** package XML docs (`Umbraco.Infrastructure.xml`, `Umbraco.Core.xml`) in the local NuGet cache.
- Patterns A and B (a singleton reading with no request, plus `EnsureUmbracoContext` for URLs) match the skill's assets, which are compiled on 17.5.3/18.2.0 and runtime-tested on 17.5.3.
- The `NewsDigestReader` code above is adapted from those assets and has **not** been compiled or run against your project. Build it and run it once by hand (for example, call it from a temporary controller **and** from a background `Task.Run`) before relying on the nightly run.
