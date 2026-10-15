# Transcript: accessor-in-hosted-service (with_skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md (routing: background job / built-in service → using-built-in-services.md)
- plugins/implementation/skills/umbraco-services/references/using-built-in-services.md ("No HTTP request" row → singleton + IUmbracoContextFactory + IDocumentNavigationQueryService; asset RootContentReader.cs)
- plugins/implementation/skills/umbraco-services/assets/RootContentReader.cs
- (directory listing of the skill only; evals/ and examples/ not opened)

## External sources
- WebFetch https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/querying/ipublishedcontentquery.md. The page confirms: "In a background task, accessing the content cache using an injected IPublishedContentQuery or IPublishedContentQueryAccessor will not work, as they rely on HttpContext."
- WebFetch raw.githubusercontent (wrong path, 404)
- gh api: umbraco/Umbraco-CMS src/Umbraco.Infrastructure/PublishedContentQueryAccessor.cs @ release-17.5.3 (TryGetValue → _scopedServiceProvider.ServiceProvider?.GetService<IPublishedContentQuery>())
- gh api code search → src/Umbraco.Web.Common/DependencyInjection/ScopedServiceProvider.cs @ release-17.5.3 (ServiceProvider => _accessor.HttpContext?.RequestServices)
- gh api: src/Umbraco.Core/Services/Navigation/INavigationQueryService.cs @ release-17.5.3 (confirmed TryGetRootKeys, TryGetChildrenKeys, TryGetChildrenKeysOfType)
- Checked for local Umbraco source (none) and ~/.nuget package versions (17.0.0–18.2.1 cached, not inspected)

## Key reasoning
- The accessor is a singleton wrapper over HttpContext.RequestServices. With no request (hosted service) it returns null, so TryGetValue is false. In a controller it works. The time of day is a coincidence.
- The skill's rule for "No HTTP request": singleton + IUmbracoContextFactory.EnsureUmbracoContext() in a using block per read, plus IDocumentNavigationQueryService for the tree. Never store context/content in fields, and don't use IContentService for reads.
- Adapted RootContentReader into NewsDigestReader: find "News" under the roots, take its children from the published cache (a null from GetById means unpublished), order by CreateDate desc, take 5, and project to a record before disposal.
- Mentioned IRecurringBackgroundJob for load-balanced setups as an optional, unverified pointer.
- Marked the digest code as not compiled; it isn't covered by the gate.
