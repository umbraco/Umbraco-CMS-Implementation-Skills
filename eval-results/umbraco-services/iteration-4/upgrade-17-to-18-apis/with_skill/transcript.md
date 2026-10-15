# Transcript: upgrade-17-to-18-apis (with_skill)

## Files read
1. plugins/implementation/skills/umbraco-services/SKILL.md: the "Version compatibility" section says `SaveBlueprint(IContent, int)` was removed on 18 (obsolete on 17), and that `UnpublishedCultures` is on the Published and Unpublished notifications from 17.6.0/18.1.0. It points to using-built-in-services, *Culture unpublish*.
2. references/using-built-in-services.md: the Culture unpublish section. Partial unpublish raises ContentPublishedNotification once per culture; a whole-page unpublish raises ContentUnpublishedNotification; handle both. The Docs corrections section says "-ed" fires after the page leaves the cache.
3. assets/UnpublishingUrlRecorder.cs: the notification handler shape and the EnsureUmbracoContext pattern.
(I did not read evals/ or examples/.)

## Verification (no URLs fetched)
- Grepped Umbraco.Core.xml in ~/.nuget/packages/umbraco.cms.core/{17.5.3,18.0.0,18.1.0,18.2.0}:
  - 17.5.3 has SaveBlueprint(IContent,int) and SaveBlueprint(IContent,IContent,int); 18.2.0 has only the three-argument overload.
  - UnpublishedCultures: 18.0.0 = absent, 18.1.0 = present; on ContentPublished and ContentUnpublished notifications.
  - The doc remarks confirm that partial-culture unpublish is reported on ContentPublishedNotification, that ContentUnpublishedNotification is whole-document only (["*"] for invariant), and that the value is null only without culture info.
- Built a throwaway net10.0 project in the session scratchpad (outside the repo) referencing Umbraco.Cms.Core 18.2.0. It contained the SaveBlueprint calls (null / source / default userId) and a handler typed as IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>?. Build succeeded with no errors or warnings.

## Reasoning
- Q1: the 2-argument overload was removed. Replace it with SaveBlueprint(blueprint, null or source, userId); null keeps the old behaviour. That overload is already on 17, so the change can be made before the upgrade.
- Q2: yes, from 18.1.0 via UnpublishedCultures. Handle both notifications because of the partial vs whole split. Read the dictionary rather than entity dirty-tracking. No UmbracoContext is needed for logging.
- Runtime behaviour was not tested; I flagged this in the answer.
