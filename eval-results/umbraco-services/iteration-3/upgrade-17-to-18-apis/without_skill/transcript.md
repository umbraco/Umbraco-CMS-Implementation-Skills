# Transcript: baseline (no skill), upgrade-17-to-18-apis

No files under the Umbraco-AI-skills directory were read. All research went to Umbraco-CMS source on GitHub via `gh api` (no WebFetch/WebSearch calls were made).

## GitHub API calls (all repos/umbraco/Umbraco-CMS)
- `branches` and `tags` listings, to find release tags (latest: release-18.2.1, 18.3.0-rc; 17.7.x)
- `contents/src/Umbraco.Core/Services/IContentService.cs` at release-18.2.1, release-17.7.0, release-17.0.0, 17.3.0, 17.4.0, 17.5.0, 17.6.0, release-18.0.0
- `contents/src/Umbraco.Core/Notifications/ContentUnpublishedNotification.cs` at release-18.2.1, 17.7.0, 17.0.0, 17.3.0, 17.4.0, 17.5.0, 17.6.0, 18.0.0, 18.1.0, 18.2.0
- `contents/src/Umbraco.Core/Notifications/ContentUnpublishingNotification.cs` at 18.2.1, 17.7.0
- `contents/src/Umbraco.Core/Notifications/ContentPublishedNotification.cs` at 18.2.1, 17.7.0
- `search/code?q=ContentUnpublishedNotification` (to locate where it is raised)
- `contents/src/Umbraco.Core/Services/ContentService.cs` at 18.2.1
- `contents/src/Umbraco.Core/Services/PublishableContentServiceBase.cs` at 18.2.1
- `contents/src/Umbraco.Core/Notifications/ContentNotificationExtensions.cs` at 18.2.1
- `contents/src/Umbraco.Core/Services/IContentBlueprintEditingService.cs` at 18.2.1

## Key reasoning
1. In v17, `SaveBlueprint(IContent, int userId)` is `[Obsolete(... "Scheduled for removal in Umbraco 18.")]`. In 18.0.0 it is gone, and only `SaveBlueprint(IContent, IContent? createdFromContent, int userId)` remains. In v17, the 3-arg default implementation forwarded to the 2-arg one, so `SaveBlueprint(blueprint, null, userId)` keeps the same behaviour. Also mentioned `IContentBlueprintEditingService` as the modern API.
2. `ContentUnpublishedNotification.UnpublishedCultures` and `ContentPublishedNotification.UnpublishedCultures`/`PublishedCultures` (a dictionary keyed by content Key) are present in 17.6.0+ and 18.1.0+, but absent in 18.0.0.
3. In `CommitDocumentChangesInternal`, a partial culture unpublish (other cultures stay published) goes down the *publishing* path (`StrategyCanPublish` returns SuccessUnpublishCulture) and raises `ContentPublishedNotification` with `UnpublishedCultures`. Only the last culture, a mandatory culture, or invariant content raises `ContentUnpublishedNotification`. So the handler must subscribe to both. Invariant content gives `["*"]`. `BuildCultureMap` returns null for null input, and an empty dictionary for an empty list.
4. Mentioned the older `HasUnpublishedCulture`/`IsUnpublishingCulture` extensions as a fallback for 18.0.0 and for the "-ing" notifications.
5. The code was not compiled or run (no buildable project for this task).
