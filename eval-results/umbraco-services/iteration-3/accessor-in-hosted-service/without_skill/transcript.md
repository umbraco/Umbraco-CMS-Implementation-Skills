# Transcript: accessor-in-hosted-service (without_skill baseline)

## URLs fetched
None. I answered from my own knowledge. No web fetches, and no repo files were read.

## Key reasoning
1. `IPublishedContentQuery` is request-scoped. The singleton `IPublishedContentQueryAccessor` gets it from `IHttpContextAccessor.HttpContext.RequestServices`. With no HTTP request, `HttpContext` is null, so `TryGetValue` returns false.
2. Time of day doesn't matter. The difference is controller (request) versus hosted service (no request).
3. Fix: use `IRecurringBackgroundJob` (scheduling, server roles, runs after boot) plus `IUmbracoContextFactory.EnsureUmbracoContext()` inside each run, then read from `UmbracoContext.Content`.
4. v15+/17 alternative: inject `IPublishedContentCache` with `IDocumentNavigationQueryService` (HybridCache plus navigation), which work outside requests. Navigation includes unpublished nodes, and the published cache filters them out.
5. Caveats: culture (VariationContext), absolute URLs (domains / UmbracoApplicationUrl), don't hold the context across runs, create a scope after EnsureUmbracoContext for scoped services, and don't fall back to IContentService.
6. I flagged the exact v17 navigation method names as needing checking, because I haven't confirmed them against the source.
