# Transcript: singleton-scoped-mismatch (without skill)

- Fetched nothing. Answered from model knowledge only; read no repo files (no-skill baseline).
- Diagnosis: a singleton `SiteService` captures the scoped `UmbracoHelper` (captive dependency). ASP.NET Core sets `ValidateScopes`/`ValidateOnBuild` only in Development, which explains why it fails there and starts in Production, where the bug is latent (disposed or stale UmbracoContext).
- Considered the fixes: disabling validation (rejected), scoped service (works for requests but not for background jobs, since UmbracoHelper needs a request context and recurring jobs are singletons), service locator inside the singleton (rejected), and singleton plus `IUmbracoContextFactory.EnsureUmbracoContext()` per call (chosen).
- Umbraco 15+ specifics: the content tree is in `IDocumentNavigationQueryService` (`TryGetRootKeys`, `TryGetDescendantsKeysOfType`) and the published cache resolves by key. Advised against caching `IPublishedContent`; cache keys only, invalidated through `ContentCacheRefresherNotification`.
- Added an `IRecurringBackgroundJob` example registered via `AddRecurringBackgroundJob<T>()`, plus `IServiceScopeFactory` guidance for truly scoped dependencies.
- Hedged: API signatures are not verified against the Umbraco 17 source.
