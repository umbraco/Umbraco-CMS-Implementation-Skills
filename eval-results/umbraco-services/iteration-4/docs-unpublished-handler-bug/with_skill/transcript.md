# Transcript

## Files read
1. plugins/implementation/skills/umbraco-services/SKILL.md. Route table: notification handler, so open using-built-in-services.md.
2. plugins/implementation/skills/umbraco-services/references/using-built-in-services.md. The "Docs corrections" section covers this exact docs sample: v17 docs handle ContentUnpublishedNotification and say the item is still cached, which is wrong. "-ed" fires after the page leaves the cache, and on 17.5.3 GetById returns null (runtime-verified). The fix is ContentUnpublishingNotification, whose items are still in UnpublishedEntities. Also used the "Where the code runs" row (handler: EnsureUmbracoContext in using, no accessor) and the "Culture unpublish" section.
3. assets/UnpublishingUrlRecorder.cs and assets/SiteServiceComposer.cs, the verified handler and its registration (AddNotificationHandler<ContentUnpublishingNotification, ...>, store as a singleton).

No URLs fetched. Did not open evals/, examples/, or eval-results/.

## Reasoning
- The symptom (handler runs, nothing recorded) matches the reference: "-ed" notification, page already out of cache, GetById returns null, the null guard skips it.
- Fix: switch to the "-ing" notification, update the registration, keep the disposed context (needed for URL building with no request).
- Added caveats: do not cancel the notification; store as a persistent singleton; partial-culture unpublish behaviour for "-ing" not verified; clear the URL on republish; delete/trash are separate cases.
- Not built or run against the user's project (none provided), and the response says so.
