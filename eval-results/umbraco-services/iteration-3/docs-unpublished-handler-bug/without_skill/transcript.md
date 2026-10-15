# Transcript — docs-unpublished-handler-bug (without skill)

## URLs fetched
1. https://docs.umbraco.com/umbraco-cms/reference/notifications/contentservice-notifications
   - Confirmed: Unpublishing/Unpublished use `UnpublishedEntities`; single-variant unpublish raises
     ContentPublishing/Published (use `IsUnpublishingCulture`), not Unpublishing/Unpublished.

No repo files read (baseline run).

## Key reasoning
- Handler is registered for `ContentUnpublishedNotification` ("-ed", post-operation). The item is no
  longer in the published cache, so `GetById` returns null and the null guard skips `_store.Add`.
- Docs comment ("fires before the cache is updated") describes the "-ing" notification; leftover from
  v8-era sample. Fix: subscribe to `ContentUnpublishingNotification`.
- Modernised for v17: inject `IPublishedContentCache` + `IPublishedUrlProvider` instead of
  `IUmbracoContextFactory` + `.Url()`; guard `"#"`.
- Extra caveats: descendants, multilingual/culture-only unpublish, cancellation (stateful ing/ed
  pattern via `notification.State`), republish cleanup, recycle bin/delete, load-balanced storage.
- Flagged unverified API details (navigation service, State sharing) as from memory.
