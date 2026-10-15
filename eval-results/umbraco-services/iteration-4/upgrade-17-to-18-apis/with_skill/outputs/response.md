# Umbraco 17 → 18: `SaveBlueprint` and culture-unpublish notifications

## 1. `SaveBlueprint(blueprint, userId)` no longer compiles

On 17, `IContentService.SaveBlueprint(IContent, int)` was already marked obsolete. Umbraco 18 removed it. The only overload left is the three-argument one, which also takes the content the blueprint was created from:

```csharp
// IContentService (18.x)
void SaveBlueprint(IContent content, IContent? createdFromContent, int userId = Constants.Security.SuperUserId);
```

Change the call to:

```csharp
// The blueprint wasn't created from an existing page:
_contentService.SaveBlueprint(blueprint, null, userId);

// The blueprint was created from a page (for example, after CreateBlueprintFromContent):
_contentService.SaveBlueprint(blueprint, sourceContent, userId);
```

If you have the source page, pass it. Umbraco uses it to link the blueprint to the content it came from. If you don't have it, pass `null`; that is the same behaviour as the old overload. The three-argument overload already exists on 17.5.3, so you can make this change before you switch the package version.

On 18, some other `IContentService` members moved to the new base interface `IPublishableContentService<IContent>`. Code that calls them through `IContentService` is not affected.

## 2. Logging which cultures were unpublished

Yes, if you're on **18.1.0 or later**. The notifications now have an `UnpublishedCultures` property: a dictionary keyed by content `Key` whose values are the cultures that were taken down. It isn't on 18.0.x. On the 17 line it arrives in 17.6.0.

There is a catch: one notification isn't enough, because Umbraco raises different notifications depending on how much was unpublished:

| What the editor does | Notification raised | Where the cultures are |
|---|---|---|
| Unpublishes **some** languages, and others stay live | `ContentPublishedNotification`. Umbraco treats this as a re-publish with those cultures cleared, and raises it once per culture | `notification.UnpublishedCultures` |
| Unpublishes the **whole** page, or the last or mandatory language | `ContentUnpublishedNotification` | `notification.UnpublishedCultures` (all cultures that went down; `["*"]` for invariant content) |

To log every culture unpublish, handle **both**:

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace MySite.Notifications;

public class CultureUnpublishLogger :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>
{
    private readonly ILogger<CultureUnpublishLogger> _logger;

    public CultureUnpublishLogger(ILogger<CultureUnpublishLogger> logger) => _logger = logger;

    // Some languages unpublished while others stay live (raised once per culture).
    public void Handle(ContentPublishedNotification notification)
        => Log(notification.UnpublishedCultures, notification.PublishedEntities, partial: true);

    // Whole page unpublished (or its last/mandatory language).
    public void Handle(ContentUnpublishedNotification notification)
        => Log(notification.UnpublishedCultures, notification.UnpublishedEntities, partial: false);

    private void Log(
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? unpublishedCultures,
        IEnumerable<IContent> items,
        bool partial)
    {
        // null only if the notification was raised without culture information.
        if (unpublishedCultures is null)
        {
            return;
        }

        foreach (IContent content in items)
        {
            if (unpublishedCultures.TryGetValue(content.Key, out IReadOnlyCollection<string>? cultures)
                && cultures.Count > 0)
            {
                _logger.LogInformation(
                    "Unpublished culture(s) {Cultures} of {Name} ({Key}){Scope}",
                    string.Join(", ", cultures),
                    content.Name,
                    content.Key,
                    partial ? "; other cultures remain published" : "; page is no longer published");
            }
        }
    }
}
```

Register the handler for both notifications, either in an `IComposer` or on the builder in `Program.cs`:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

public class CultureUnpublishLoggerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishedNotification, CultureUnpublishLogger>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, CultureUnpublishLogger>();
    }
}
```

Notes:
- On ordinary publishes, the `TryGetValue`/`Count > 0` check skips the published handler, so you only log real unpublishes.
- Read the cultures from `UnpublishedCultures`, not from the entities. Umbraco fills the dictionary when it raises the notification, because the entity's change tracking has been reset by the time the "-ed" handler runs.
- This handler only logs, so it needs no `UmbracoContext`. If you later need the page's **URL** or other published-cache data, the "-ed" notification is too late: the page has already left the cache. Use `ContentUnpublishingNotification` with `IUmbracoContextFactory.EnsureUmbracoContext()` instead.
- The handler only compiles on 18.1.0 or later, so check `Umbraco.Cms` in your `.csproj` or `Directory.Packages.props`.

## Verification status
- Both snippets (the `SaveBlueprint(blueprint, null/source, userId)` calls and the handler) **compiled against `Umbraco.Cms.Core` 18.2.0 with no errors or warnings** in a throwaway project.
- I checked the API XML docs for 17.5.3, 18.0.0, 18.1.0 and 18.2.0 in the local NuGet cache:
  - 17.5.3 has both `SaveBlueprint` overloads; 18.2.0 has only the three-argument one.
  - `UnpublishedCultures` is missing in 18.0.0 and present from 18.1.0.
- **Not run against a live site.** The "raised once per culture" behaviour of `ContentPublishedNotification` comes from source and API docs, not from a runtime test. Unpublish one language and then a whole page on a dev site, and check the log, before you rely on it.
