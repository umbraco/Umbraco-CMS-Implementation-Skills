# Transcript: singleton-cached-content (without skill)

## URLs fetched
None. The answer was written from model knowledge only. No repo files were read, as the baseline requires.

## Key reasoning
1. Symptom: the stale title stays until a restart, and there is no output cache or CDN. So something in process holds the old data, and the obvious culprit is the singleton field `_newsSection`.
2. `IPublishedContent` is an immutable snapshot. Publishing creates new cache entries, and objects you already hold are never updated. The singleton constructor runs once, so the snapshot is frozen.
3. Other issues in the constructor: content lookup at DI resolve time (startup ordering), ignored `TryGetRootKeys` result, `First()` can throw, unnecessary `EnsureUmbracoContext`, and the navigation service includes unpublished nodes.
4. Culture and preview are captured once, which is also wrong for variant sites.
5. Fix: inject `IPublishedContentCache` (Umbraco 15+) and `IDocumentNavigationQueryService`, resolve per call with `TryGetChildrenKeysOfType` and `GetById`, and register Scoped (Singleton is acceptable once it is stateless).
6. Optional variant: cache only the Guid key, and clear it on `ContentCacheRefresherNotification`, which works with load balancing.
7. Caveat noted: the code was not compiled, so API member names should be checked against 17.x.
