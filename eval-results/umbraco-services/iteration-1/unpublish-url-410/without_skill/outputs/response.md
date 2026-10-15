# Capturing unpublished URLs for a 410 Gone content finder (Umbraco 17)

## The key gotcha: capture the URL *before* the unpublish happens

The obvious approach is to handle `ContentUnpublishedNotification` and ask Umbraco for the page's URL. That doesn't work. By the time that notification fires, the page has already left the published cache, so `IPublishedUrlProvider` has nothing to resolve and returns `"#"` (or null).

So the handler works in two phases on one class:

1. **`ContentUnpublishingNotification`** (before the unpublish). The page and its descendants are still in the published cache. Resolve their URLs here and stash them in `notification.State`.
2. **`ContentUnpublishedNotification`** (after the unpublish). Read the stashed URLs back and persist them. If another handler cancels the unpublish, this phase never runs, so we never record a URL that is still live.

`notification.State` is shared between the "-ing" and "-ed" notifications of the same operation, but only when **the same handler type** handles both. That is why one class implements both interfaces.

Two more details matter:

- **Descendants.** When you unpublish a parent, every page below it becomes unreachable too. Their URLs have to be captured as well, or they will 404 instead of 410.
- **Background execution.** Unpublishing can run with no HTTP request at all, for example from scheduled unpublish. `IPublishedUrlProvider` needs an `UmbracoContext`, so the handler calls `IUmbracoContextFactory.EnsureUmbracoContext()`.

---

## 1. The stored record and store abstraction

```csharp
// GoneUrls/GoneUrl.cs
namespace MySite.GoneUrls;

/// <summary>A URL that used to resolve to a published page.</summary>
public sealed record GoneUrl(
    Guid ContentKey,
    string? Culture,
    string Host,          // "" when the site has no domain assigned
    string Path,         // normalised: lower-case, leading "/", no trailing "/" (except root)
    DateTime UnpublishedUtc);
```

```csharp
// GoneUrls/IGoneUrlStore.cs
namespace MySite.GoneUrls;

public interface IGoneUrlStore
{
    void AddOrUpdate(IEnumerable<GoneUrl> urls);

    /// <summary>Used later by the 410 content finder.</summary>
    GoneUrl? Find(string host, string path);

    /// <summary>Call when a page is re-published so it stops answering 410.</summary>
    void RemoveForContent(Guid contentKey);
}
```

## 2. The notification handler

```csharp
// GoneUrls/GoneUrlCaptureHandler.cs
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace MySite.GoneUrls;

/// <summary>
/// Records the public URL(s) of pages as they are unpublished, so a content finder
/// can later answer 410 Gone for them.
/// </summary>
public sealed class GoneUrlCaptureHandler :
    INotificationHandler<ContentUnpublishingNotification>,
    INotificationHandler<ContentUnpublishedNotification>
{
    private const string StateKey = "MySite.GoneUrls.Captured";

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IDocumentNavigationQueryService _navigationQueryService;
    private readonly IGoneUrlStore _store;
    private readonly ILogger<GoneUrlCaptureHandler> _logger;

    public GoneUrlCaptureHandler(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedUrlProvider publishedUrlProvider,
        IDocumentNavigationQueryService navigationQueryService,
        IGoneUrlStore store,
        ILogger<GoneUrlCaptureHandler> logger)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _publishedUrlProvider = publishedUrlProvider;
        _navigationQueryService = navigationQueryService;
        _store = store;
        _logger = logger;
    }

    // Phase 1: BEFORE unpublish. Content is still in the published cache, so URLs resolve.
    public void Handle(ContentUnpublishingNotification notification)
    {
        var captured = new List<GoneUrl>();
        var now = DateTime.UtcNow;

        // Works with or without an HTTP request (e.g. scheduled unpublish).
        using UmbracoContextReference contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        IPublishedContentCache? cache = contextRef.UmbracoContext.Content;
        if (cache is null)
        {
            return;
        }

        foreach (IContent content in notification.UnpublishedEntities)
        {
            CaptureUrls(cache, content.Key, now, captured);

            // Unpublishing a parent takes its whole published subtree offline.
            if (_navigationQueryService.TryGetDescendantsKeys(content.Key, out IEnumerable<Guid> descendantKeys))
            {
                foreach (Guid key in descendantKeys)
                {
                    CaptureUrls(cache, key, now, captured);
                }
            }
        }

        notification.State[StateKey] = captured;
    }

    // Phase 2: AFTER unpublish. Only reached if nobody cancelled it, so persist now.
    public void Handle(ContentUnpublishedNotification notification)
    {
        if (notification.State.TryGetValue(StateKey, out object? value) is false
            || value is not List<GoneUrl> captured
            || captured.Count == 0)
        {
            return;
        }

        try
        {
            _store.AddOrUpdate(captured);
            _logger.LogInformation("Recorded {Count} gone URL(s) after unpublish.", captured.Count);
        }
        catch (Exception ex)
        {
            // Never break the editor's unpublish because of the 410 bookkeeping.
            _logger.LogError(ex, "Failed to record gone URLs after unpublish.");
        }
    }

    private void CaptureUrls(IPublishedContentCache cache, Guid key, DateTime now, List<GoneUrl> captured)
    {
        IPublishedContent? published = cache.GetById(key);
        if (published is null)
        {
            return; // not currently published (e.g. an already-unpublished descendant)
        }

        // Variant pages have one URL per culture; invariant pages use culture = null.
        IEnumerable<string?> cultures = published.ContentType.VariesByCulture()
            ? published.Cultures.Keys
            : new string?[] { null };

        foreach (string? culture in cultures)
        {
            string url = _publishedUrlProvider.GetUrl(published, UrlMode.Absolute, culture);
            if (string.IsNullOrWhiteSpace(url) || url == "#")
            {
                continue; // page has no routable URL
            }

            (string host, string path) = Split(url);
            captured.Add(new GoneUrl(key, culture, host, path, now));
        }
    }

    // Absolute when a domain is assigned ("https://example.com/foo/"), else relative ("/foo/").
    private static (string Host, string Path) Split(string url)
    {
        string host = string.Empty;
        string path = url;

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute))
        {
            host = absolute.Authority.ToLowerInvariant();
            path = absolute.AbsolutePath;
        }

        return (host, NormalisePath(path));
    }

    public static string NormalisePath(string path)
    {
        path = path.Trim().ToLowerInvariant();
        if (path.StartsWith('/') is false)
        {
            path = "/" + path;
        }

        return path.Length > 1 ? path.TrimEnd('/') : path;
    }
}
```

## 3. Persistence: custom table with NPoco and a package migration

```csharp
// GoneUrls/Persistence/GoneUrlDto.cs
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace MySite.GoneUrls.Persistence;

[TableName(TableName)]
[PrimaryKey("id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class GoneUrlDto
{
    public const string TableName = "mySiteGoneUrl";

    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("contentKey")]
    public Guid ContentKey { get; set; }

    [Column("culture")]
    [Length(16)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Culture { get; set; }

    [Column("host")]
    [Length(255)]
    public string Host { get; set; } = string.Empty;

    [Column("path")]
    [Length(400)] // keeps the index under SQL Server's 900-byte key limit
    [Index(IndexTypes.NonClustered, Name = "IX_mySiteGoneUrl_path")]
    public string Path { get; set; } = string.Empty;

    [Column("unpublishedUtc")]
    public DateTime UnpublishedUtc { get; set; }
}
```

```csharp
// GoneUrls/Persistence/GoneUrlMigrations.cs
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace MySite.GoneUrls.Persistence;

// PackageMigrationPlans are discovered automatically and run at startup
// (unattended package migrations are on by default).
public sealed class GoneUrlMigrationPlan : PackageMigrationPlan
{
    public GoneUrlMigrationPlan() : base("MySite.GoneUrls") { }

    protected override void DefinePlan()
        => To<CreateGoneUrlTable>("7c1d6a0e-3c1b-4c8e-9a52-2f0b7f1d9e10");
}

public sealed class CreateGoneUrlTable : MigrationBase
{
    public CreateGoneUrlTable(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        if (TableExists(GoneUrlDto.TableName) is false)
        {
            Create.Table<GoneUrlDto>().Do();
        }
    }
}
```

> Recent Umbraco majors have been moving migrations to an async base class (`AsyncMigrationBase` / `MigrateAsync`). If `MigrationBase` shows as obsolete in your 17.x build, switch `CreateGoneUrlTable` to the async base. The body stays the same.

```csharp
// GoneUrls/Persistence/GoneUrlStore.cs
using Umbraco.Cms.Infrastructure.Scoping;

namespace MySite.GoneUrls.Persistence;

public sealed class GoneUrlStore : IGoneUrlStore
{
    private readonly IScopeProvider _scopeProvider;

    public GoneUrlStore(IScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public void AddOrUpdate(IEnumerable<GoneUrl> urls)
    {
        using IScope scope = _scopeProvider.CreateScope();
        foreach (GoneUrl url in urls)
        {
            // One row per host+path: the latest unpublish wins.
            scope.Database.Execute(
                $"DELETE FROM {GoneUrlDto.TableName} WHERE host = @0 AND path = @1", url.Host, url.Path);

            scope.Database.Insert(new GoneUrlDto
            {
                ContentKey = url.ContentKey,
                Culture = url.Culture,
                Host = url.Host,
                Path = url.Path,
                UnpublishedUtc = url.UnpublishedUtc,
            });
        }

        scope.Complete();
    }

    public GoneUrl? Find(string host, string path)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        GoneUrlDto? dto = scope.Database.FirstOrDefault<GoneUrlDto>(
            $"SELECT * FROM {GoneUrlDto.TableName} WHERE path = @0 AND (host = @1 OR host = '')",
            path, host.ToLowerInvariant());

        return dto is null
            ? null
            : new GoneUrl(dto.ContentKey, dto.Culture, dto.Host, dto.Path, dto.UnpublishedUtc);
    }

    public void RemoveForContent(Guid contentKey)
    {
        using IScope scope = _scopeProvider.CreateScope();
        scope.Database.Execute(
            $"DELETE FROM {GoneUrlDto.TableName} WHERE contentKey = @0", contentKey);
        scope.Complete();
    }
}
```

## 4. Registration

```csharp
// GoneUrls/GoneUrlComposer.cs
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using MySite.GoneUrls.Persistence;

namespace MySite.GoneUrls;

public sealed class GoneUrlComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IGoneUrlStore, GoneUrlStore>();

        // Register the SAME handler type for both, so they share notification.State.
        builder.AddNotificationHandler<ContentUnpublishingNotification, GoneUrlCaptureHandler>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, GoneUrlCaptureHandler>();
    }
}
```

If you prefer explicit registration in `Program.cs` over an `IComposer`, put the same three lines in an `IUmbracoBuilder` extension method and chain it after `.AddComposers()`.

---

## Things to decide before step two

- **Re-publishing.** If an editor publishes the page again, its row should go away. Add a `ContentPublishedNotification` handler that calls `_store.RemoveForContent(key)` for each published entity. Even without it, a live page wins: Umbraco's normal URL finder runs before any last-chance finder. The cleanup just keeps the table honest.
- **Culture-only unpublish.** Unpublishing one culture of a multilingual page, while others stay live, is saved as a publish operation. It does not raise `ContentUnpublishingNotification`, so this handler won't see it. If you need 410s per culture, handle `ContentPublishingNotification` as well and check for cultures being unpublished.
- **Deleted and trashed pages.** Moving a page to the recycle bin takes it offline through a different notification flow (`ContentMovingToRecycleBinNotification`). Decide whether trashed pages should also return 410. If so, capture their URLs in the "moving" notification with the same `CaptureUrls` logic.
- **The content finder (step two).** Implement `IContentFinder` and look up `request.Uri.Authority` plus the normalised path via `IGoneUrlStore.Find`. On a match, call `request.SetResponseStatus(410)` and point it at a "Gone" page with `request.SetPublishedContent(...)`. Register it with `builder.ContentFinders().InsertBefore<ContentFinderByUrl, GoneContentFinder>()` or `.Append<...>()`, or use it as the last-chance finder. Cache lookups (for example with `IAppPolicyCache`) so you don't hit the database on every 404.

### Verification status

I wrote this from knowledge of the Umbraco 14–17 APIs and did not compile it against a 17.x build. Before relying on it, check these signatures against your installed version:

- `IDocumentNavigationQueryService.TryGetDescendantsKeys`
- `IPublishedContentCache.GetById(Guid)`
- the `MigrationBase` / `AsyncMigrationBase` split
