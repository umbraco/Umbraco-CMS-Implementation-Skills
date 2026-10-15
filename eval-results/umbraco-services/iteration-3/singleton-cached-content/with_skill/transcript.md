# Transcript

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md (route: own service / lifetime -> custom-services.md)
- plugins/implementation/skills/umbraco-services/references/custom-services.md
- assets/RootContentReader.cs, assets/SiteService.cs, assets/ISiteService.cs, assets/SiteServiceComposer.cs

No URLs fetched; I didn't read the evals/ and examples/ folders. I didn't modify any repo files.

## Key reasoning
- Root cause: a singleton reads content in its constructor and stores an `IPublishedContent` snapshot in a field, so the content is never re-read after publish. It's not an output caching or CDN problem.
- Secondary issues: the content outlives its disposed `UmbracoContext` (the asset comment says never store the context or content read from it). The `TryGetRootKeys` result is ignored and `First()` can throw. A `null` from early construction sticks until restart. `First()` root isn't multi-site aware.
- Fix: the "Lifetime = from dependencies" table. A request-only header uses scoped `IPublishedContentQuery` (SiteService asset). Work outside a request uses a singleton with a per-call `EnsureUmbracoContext` (RootContentReader asset). Composer registration follows the SiteServiceComposer asset.
- Optional caching advice: store the key, not content; clear derived caches on `ContentPublishedNotification`.
- I didn't compile anything because there's no project. The answer says it's unverified against the user's code.
