# Transcript: upgrade-17-to-18-apis (with_skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md. Its Version compatibility section says that on 18 `SaveBlueprint(IContent,int)` is removed and `ContentUnpublishedNotification` gains `UnpublishedCultures`.
- references/using-built-in-services.md: the notification-handler row and the -ing vs -ed correction.
- assets/UnpublishingUrlRecorder.cs and assets/SiteServiceComposer.cs: handler shape and registration.
- Did not read evals/ or examples/. Fetched no URLs.

## Verification against local NuGet cache (~/.nuget/packages/umbraco.cms.core)
- Grepped Umbraco.Core.xml for 17.5.3 and 18.2.0 (SaveBlueprint, UnpublishedCultures).
  - **Key finding:** the 18.2.0 remarks say that unpublishing an individual culture of a variant document is done as a PUBLISH. So it is reported on `ContentPublishedNotification.UnpublishedCultures`, NOT on `ContentUnpublishedNotification`, which is raised only when the whole document is unpublished.
  - Unpublishing several cultures raises one ContentPublishedNotification per culture.
  - The skill only mentions `ContentUnpublishedNotification.UnpublishedCultures`. On its own that would have produced a handler that misses the user's exact scenario. This is a gap in the skill.
- Built a scratch probe console app in the session scratchpad (not in the repo). It references Umbraco.Cms.Core with a local cache feed.
  - Reflection on 18.2.0:
    - Both notifications expose `UnpublishedCultures` as `IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>`.
    - IContentService has only `SaveBlueprint(IContent, IContent?, int = -1)`, and `createdFromContent` is Nullable.
    - ContentService class keeps the 2-arg overload, obsolete "Scheduled for removal in Umbraco 20".
  - Reflection on 17.5.3:
    - IContentService.SaveBlueprint(IContent,int) is obsolete with "Scheduled for removal in Umbraco 18".
    - No UnpublishedCultures on either notification.
  - Compiled the proposed handler, composer and `SaveBlueprint(blueprint, null, userId)` call: 18.2.0 built with 0 warnings; 17.5.3 failed with CS1061 (expected).
- Not runtime-verified. Said so in the response.

## Answer summary
1. Use `_contentService.SaveBlueprint(blueprint, null, userId)`, or pass the source content.
2. Handle BOTH ContentPublishedNotification (partial, one notification per culture) and ContentUnpublishedNotification (whole page). Read `UnpublishedCultures` keyed by content Key and null-check it. Register via an IComposer.

## Repo
No repository files were modified.
