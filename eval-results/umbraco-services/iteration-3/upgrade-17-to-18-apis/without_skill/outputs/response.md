# Umbraco 17 to 18: `SaveBlueprint` and logging unpublished cultures

I checked both answers against the Umbraco-CMS source at the release tags (`release-17.x`, `release-18.0.0`, `release-18.1.0`, `release-18.2.1`).

## 1. `SaveBlueprint(blueprint, userId)` no longer compiles

In v17, `IContentService` had two overloads:

```csharp
[Obsolete("Please use the method taking all parameters. Scheduled for removal in Umbraco 18.")]
void SaveBlueprint(IContent content, int userId = Constants.Security.SuperUserId);

void SaveBlueprint(IContent content, IContent? createdFromContent, int userId = Constants.Security.SuperUserId);
```

v18 removed the obsolete one, as announced. Only this overload is left:

```csharp
void SaveBlueprint(IContent content, IContent? createdFromContent, int userId = Constants.Security.SuperUserId);
```

So your call is now trying to pass `userId` into the `IContent? createdFromContent` slot. The fix is to pass the source content as the second argument, or `null` if the blueprint wasn't created from an existing page:

```csharp
// Blueprint not derived from an existing content item:
_contentService.SaveBlueprint(blueprint, null, userId);

// Blueprint created from an existing page (lets Umbraco record where it came from):
_contentService.SaveBlueprint(blueprint, sourceContent, userId);

// Or with a named argument, which reads clearer:
_contentService.SaveBlueprint(blueprint, createdFromContent: null, userId: userId);
```

This is exactly what the v17 obsolete overload did internally: the default implementation of the 3-arg method forwarded to it, and the obsolete one behaved as if `createdFromContent` were `null`. So `null` keeps your existing behaviour.

**Optional, longer-term:** for new code, the recommended API is `IContentBlueprintEditingService` (`CreateAsync`, `CreateFromContentAsync(contentKey, name, key, userKey)`, `UpdateAsync`, `DeleteAsync`, `MoveAsync`, `GetScaffoldedAsync`). It works with keys and user keys (`Guid`) and runs validation. Another v17 API in the same area, `IContentService.CreateContentFromBlueprint`, was also marked "Scheduled for removal in Umbraco 18" with the replacement `IContentBlueprintEditingService.GetScaffoldedAsync()`. If you use it, expect the same kind of break.

## 2. Logging which cultures were unpublished

Yes. Starting with **Umbraco 18.1.0** (backported to 17.6.0), the notifications include the cultures:

- `ContentUnpublishedNotification.UnpublishedCultures`
- `ContentPublishedNotification.UnpublishedCultures` (and `.PublishedCultures`)

Both are `IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>?`, keyed by the content item's `Key`. **18.0.0 does not have these properties**, so you need 18.1 or later.

### The important catch: which notification fires depends on what is unpublished

In `PublishableContentServiceBase.CommitDocumentChangesInternal` (v18.2.1):

| Editor action on a variant page | Notification raised | Where the cultures are |
|---|---|---|
| Unpublishes some languages while **at least one other stays published** | `ContentPublishedNotification`. This is technically a "publish" that removes cultures. `ContentUnpublishedNotification` does **not** fire. | `notification.UnpublishedCultures[content.Key]` (`PublishedCultures` is empty for this item) |
| Unpublishes the **last** published language | `ContentUnpublishedNotification` | `notification.UnpublishedCultures[content.Key]` |
| Unpublishes a **mandatory** language, which takes the whole page offline | `ContentUnpublishedNotification` | `UnpublishedCultures` lists the culture(s) the editor unpublished. Other languages also go offline as a side effect, but they are not listed. |
| Invariant (non-variant) page unpublished | `ContentUnpublishedNotification` | `["*"]` |
| Published page deleted / content type deleted | `ContentUnpublishedNotification` | all previously published cultures (or `["*"]`) |

So if you only handle `ContentUnpublishedNotification`, you will **miss the most common case**: an editor taking one language offline while the others stay live. To cover everything, handle both notifications.

### Handler

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace MySite.Notifications;

public sealed class LogUnpublishedCulturesHandler :
    INotificationHandler<ContentUnpublishedNotification>,
    INotificationHandler<ContentPublishedNotification>
{
    private readonly ILogger<LogUnpublishedCulturesHandler> _logger;

    public LogUnpublishedCulturesHandler(ILogger<LogUnpublishedCulturesHandler> logger)
        => _logger = logger;

    // Last culture, mandatory culture, or invariant content taken offline.
    public void Handle(ContentUnpublishedNotification notification)
        => LogCultures(notification.UnpublishedEntities, notification.UnpublishedCultures, wholePageOffline: true);

    // Some cultures removed while the page stays published in others.
    public void Handle(ContentPublishedNotification notification)
        => LogCultures(notification.PublishedEntities, notification.UnpublishedCultures, wholePageOffline: false);

    private void LogCultures(
        IEnumerable<IContent> entities,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>? unpublishedCultures,
        bool wholePageOffline)
    {
        if (unpublishedCultures is null || unpublishedCultures.Count == 0)
        {
            return; // nothing culture-specific was unpublished (e.g. a normal publish)
        }

        foreach (IContent content in entities)
        {
            if (!unpublishedCultures.TryGetValue(content.Key, out IReadOnlyCollection<string>? cultures)
                || cultures.Count == 0)
            {
                continue;
            }

            _logger.LogInformation(
                "Content {ContentName} ({ContentKey}) unpublished culture(s): {Cultures}. Whole page offline: {WholePageOffline}",
                content.Name,
                content.Key,
                string.Join(", ", cultures), // "*" means invariant content
                wholePageOffline);
        }
    }
}

public sealed class LogUnpublishedCulturesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentUnpublishedNotification, LogUnpublishedCulturesHandler>();
        builder.AddNotificationHandler<ContentPublishedNotification, LogUnpublishedCulturesHandler>();
    }
}
```

Notes:

- Use a `TryGetValue` lookup by `content.Key`. Don't assume the dictionary has exactly one entry.
- A `"*"` value means invariant content, i.e. the whole item.
- If you need to *react before* the change, for example to cancel it, use `ContentPublishingNotification` with the extension `notification.IsUnpublishingCulture(content, culture)`, or `ContentUnpublishingNotification` for whole-page unpublish. Those "-ing" notifications don't carry a culture dictionary. Instead you check per culture, using `Umbraco.Cms.Core.Notifications.ContentNotificationExtensions`.
- Older alternative, also on 18.0.0: in the "-ed" handlers, loop over `content.AvailableCultures` and call `notification.HasUnpublishedCulture(content, culture)`. This works, but it relies on the content's dirty-property state. The `UnpublishedCultures` dictionary is the explicit, simpler option on 18.1+.
- In the backoffice audit trail, these actions are also recorded as `UnpublishVariant` entries ("Unpublished languages: …"). That can be an alternative if you only need a history.

## Sources

- `IContentService.cs`, `release-17.7.0` vs `release-18.0.0` / `release-18.2.1`: https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Services/IContentService.cs
- `ContentUnpublishedNotification.cs` (property missing in 17.5.0 / 18.0.0, present in 17.6.0 / 18.1.0+): https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Notifications/ContentUnpublishedNotification.cs
- `ContentPublishedNotification.cs`: https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Notifications/ContentPublishedNotification.cs
- `PublishableContentServiceBase.cs` (`CommitDocumentChangesInternal`, `StrategyCanPublish`, `BuildCultureMap`): https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Services/PublishableContentServiceBase.cs
- `ContentNotificationExtensions.cs`: https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Notifications/ContentNotificationExtensions.cs
- `IContentBlueprintEditingService.cs`: https://github.com/umbraco/Umbraco-CMS/blob/release-18.2.1/src/Umbraco.Core/Services/IContentBlueprintEditingService.cs

I read the code above from source but have not compiled or run it against an 18.x site. Verify it on your exact 18.x minor before you rely on it.
