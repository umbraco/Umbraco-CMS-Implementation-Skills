# Transcript: unpublish-url-410 (without skill, baseline)

## Fetched
- Nothing. I read no repo files and made no web fetches. I answered from my own knowledge of Umbraco 14–17 APIs.

## Key reasoning steps
1. **Which notification?** In `ContentUnpublishedNotification` the page is already gone from the published cache, so its URL can't be resolved there. I capture the URLs in `ContentUnpublishingNotification`, hand them over via `notification.State`, and persist them in `ContentUnpublishedNotification`. One handler class implements both, because `State` is only shared within the same handler type. Persisting in the "-ed" phase also covers cancelled unpublishes.
2. **Descendants.** Unpublishing a parent takes its whole subtree offline, so I walk `IDocumentNavigationQueryService.TryGetDescendantsKeys` and capture each descendant's published URL.
3. **No request context.** Scheduled unpublish runs in the background, so the handler calls `IUmbracoContextFactory.EnsureUmbracoContext()` before using `IPublishedUrlProvider`.
4. **Variants and domains.** I capture one URL per culture, use `UrlMode.Absolute`, split the result into host and normalised path, and skip `"#"`.
5. **Persistence.** I used a custom NPoco table created by an auto-discovered `PackageMigrationPlan`. The store uses `IScopeProvider` (upsert, find, remove-for-content). The path column is 400 characters long so the index stays under SQL Server's 900-byte key limit. I flagged that `MigrationBase` may be superseded by `AsyncMigrationBase` in 17.
6. **Registration.** An `IComposer` that registers the store as a singleton and calls `AddNotificationHandler` for both notifications with the same handler type.
7. **Caveats for step two:** removing rows on republish, culture-only unpublish (no Unpublishing notification), recycle-bin moves, and a sketch of the content finder with `SetResponseStatus(410)`.
8. **Verification.** Nothing was compiled. I listed the signatures to check against 17.x.
