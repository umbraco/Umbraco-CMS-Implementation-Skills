# Transcript: readonly-api-contentservice (with_skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md
- references/using-built-in-services.md (the task is "choose a built-in service for a controller", so custom-services.md was not needed)
- assets/SiteServiceController.cs, ISiteService.cs, SiteService.cs, SiteServiceComposer.cs, RootContentReader.cs (patterns: scoped IPublishedContentQuery in a controller; navigation service for keys)
- Did not read evals/ or skill-workspaces (as instructed).

## Fetched (v17 docs only, per Step 0; version 17 given by the user, no project to check)
- https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/templating-and-rendering/querying/ipublishedcontentquery.md: injection pattern; doesn't work in background tasks because it relies on HttpContext
- https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md: "Management Services ... not optimized for displaying data"; use IPublishedContentQuery for reading

## Key reasoning
- This is a read-only, per-request endpoint, so it falls under the skill's "read from cache, write with services" rule. IContentService hits the DB every call.
- Extra problems specific to GetPagedChildren: it returns unpublished/draft children, GetValue defaults to the draft value, and paging adds complexity.
- Recommended: inject IPublishedContentQuery (scoped, fits a per-request controller) plus IDocumentNavigationQueryService (in-memory structure), then Content(key) and Value<decimal>.
- No composer needed. Mentioned the Delivery API as a zero-code alternative.
- Stated that the code was not compiled (no project). TryGetChildrenKeysOfType is flagged as unverified.

## Outputs
- outputs/response.md
- No repository files modified.
