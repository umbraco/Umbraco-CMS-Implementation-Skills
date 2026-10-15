# Umbraco 17 → 18: `SaveBlueprint` and per-culture unpublish logging

Both answers were checked by reflecting over and compiling against `Umbraco.Cms.Core` **17.5.3** and **18.2.0**. Neither has been run on a live site (see "Verification" at the end).

## 1. `SaveBlueprint(blueprint, userId)` no longer compiles

On 17, `IContentService.SaveBlueprint(IContent, int)` was already marked obsolete. The message was *"Please use the method taking all parameters. Scheduled for removal in Umbraco 18."* On 18 that overload has been removed from `IContentService`. The only one left is:

```csharp
void SaveBlueprint(IContent content, IContent? createdFromContent, int userId = -1);
```

Change your call to:

```csharp
// Not created from an existing page:
_contentService.SaveBlueprint(blueprint, null, userId);

// Created from an existing page (lets Umbraco record the source):
_contentService.SaveBlueprint(blueprint, sourceContent, userId);
```

`createdFromContent` is nullable, so passing `null` gives the same behaviour as the old 2-argument call. Pass the source `IContent` if you have it.

Note: the concrete `ContentService` class on 18 still has `SaveBlueprint(IContent, int)`, marked `[Obsolete]` "Scheduled for removal in Umbraco 20". Don't cast to `ContentService` to reach it. Inject `IContentService` and use the 3-argument overload.

Other changes you may notice on 18: some `IContentService` members moved to the base interface `IPublishableContentService<IContent>`, for example `Save` and `SaveAndPublish`. Existing calls through `IContentService` still compile, so you don't need to change anything for those.

## 2. Logging which cultures were unpublished

Yes, on 18 the notification gives you this directly. The catch is that **which notification you get depends on how the page was unpublished**:

| What the editor did | Notification raised | Where the cultures are |
|---|---|---|
| Unpublished **some** languages; page stays published in others | `ContentPublishedNotification`. A partial unpublish is done as a re-publish with those cultures cleared. | `notification.UnpublishedCultures` |
| Unpublished the **whole** page (all languages) | `ContentUnpublishedNotification` | `notification.UnpublishedCultures` (every culture taken down; `["*"]` for invariant content) |

Both properties are new on 18 and both have the type `IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>?`, keyed by the content's `Key`. Neither exists on 17.

Things to watch for (from the 18.2.0 API docs):
- If you only handle `ContentUnpublishedNotification`, you will **miss** the case you described ("one or more languages of a variant page"), because unpublishing a single culture does not raise it.
- If an editor unpublishes several cultures at once, Umbraco handles them **one culture at a time**. You get **one `ContentPublishedNotification` per culture**, each with a single entry, not one notification listing them all.
- On `ContentPublishedNotification`, `UnpublishedCultures` is `null` when nothing was unpublished. This is the normal case for an ordinary publish, so check for null.
- These values are filled in when the notification is raised. You can't work them out afterwards from `IContent` dirty tracking, because tracking is reset once the content is saved.

### Handler

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace MySite.Notifications;

// Logs which cultures were unpublished (Umbraco 18+).
public class CultureUnpublishLogger :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>
{
    private readonly ILogger<CultureUnpublishLogger> _logger;

    public CultureUnpublishLogger(ILogger<CultureUnpublishLogger> logger) => _logger = logger;

    // Partial unpublish: some cultures taken down, page still live in others.
    // Raised once per culture when several are unpublished together.
    public void Handle(ContentPublishedNotification notification)
    {
        if (notification.UnpublishedCultures is null)
        {
            return; // ordinary publish, nothing taken down
        }

        foreach (IContent content in notification.PublishedEntities)
        {
            if (notification.UnpublishedCultures.TryGetValue(content.Key, out IReadOnlyCollection<string>? cultures)
                && cultures.Count > 0)
            {
                _logger.LogInformation(
                    "Culture(s) {Cultures} unpublished on {Name} ({Key}); page still published in other cultures",
                    string.Join(", ", cultures), content.Name, content.Key);
            }
        }
    }

    // Whole page unpublished: all its cultures went down.
    public void Handle(ContentUnpublishedNotification notification)
    {
        foreach (IContent content in notification.UnpublishedEntities)
        {
            IReadOnlyCollection<string>? cultures = null;
            notification.UnpublishedCultures?.TryGetValue(content.Key, out cultures);

            _logger.LogInformation(
                "Whole page {Name} ({Key}) unpublished; cultures taken down: {Cultures}",
                content.Name, content.Key,
                cultures is null ? "(unknown)" : string.Join(", ", cultures));
        }
    }
}
```

### Registration

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace MySite.Notifications;

public class CultureUnpublishLoggerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishedNotification, CultureUnpublishLogger>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, CultureUnpublishLogger>();
    }
}
```

Why it's built this way:
- The handler only needs `ILogger`. It doesn't read the published cache, so it doesn't need `IUmbracoContextFactory` or `EnsureUmbracoContext()`.
- The "-ed" notifications are the right choice here because you're logging what already happened.
- If you later want the page's **URL** before it disappears, for example for a 410 Gone, use `ContentUnpublishingNotification` instead. It fires before the page leaves the cache. On `ContentUnpublishedNotification` the page is already gone from the cache, whatever the v17 docs say.

### On 17 (if you need both versions during the upgrade)

There is no `UnpublishedCultures` on 17. The handler above doesn't compile there; I checked (CS1061). On 17 you would have to work out the cultures yourself in a "-ing" handler, before the save resets change tracking. Since you're moving to 18, use the built-in properties.

## Verification

- **Compiled with no warnings** against `Umbraco.Cms.Core` 18.2.0: the handler, the composer, and `SaveBlueprint(blueprint, null, userId)`.
- **Signatures checked by reflection** on 17.5.3 and 18.2.0:
  - The `SaveBlueprint` overloads and their obsolete messages.
  - The `UnpublishedCultures` property type.
  - `createdFromContent` is nullable.
- **Behaviour comes from the 18.2.0 XML API docs and was not checked at runtime.** That covers:
  - a partial unpublish being reported on `ContentPublishedNotification`
  - one notification per culture
  - `["*"]` for invariant content

  To confirm, run it on your site: unpublish one culture, then two, then the whole page, and check the log entries.
