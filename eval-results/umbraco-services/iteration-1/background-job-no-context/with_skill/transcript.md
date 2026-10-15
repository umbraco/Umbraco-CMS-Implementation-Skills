# Transcript: background-job-no-context (with_skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md
- references/using-built-in-services.md: "With no HTTP request (hosted service, scheduled job)" section, the read-vs-change table
- references/custom-services.md: lifetime table, composer registration, Lazy<IUmbracoContextFactory> note
- assets/RootContentReader.cs: the verified singleton + EnsureUmbracoContext pattern
- assets/SiteServiceComposer.cs: registration style
- examples/custom-service/*.csproj (header only, for the package reference)
- Did not read evals/ or skill-workspaces/.

## Fetched
- Umbraco 17 docs services.md (v17 row only, as the skill says). It confirms the IUmbracoContextFactory/EnsureUmbracoContext pattern, the singleton registration, and IDocumentNavigationQueryService.TryGetRootKeys.

## Checked locally (NuGet cache XML docs, 17.5.3)
- IRecurringBackgroundJob, RecurringBackgroundJobBase(TimeSpan), and ServiceCollectionExtensions.AddRecurringBackgroundJob<T> exist.
- IPublishedUrlProvider.GetUrl(IPublishedContent, UrlMode, ...) exists.
- IPublishedContent has no PublishDate, so the code sorts by UpdateDate and the response notes that caveat.
- INavigationQueryService.TryGetChildrenKeysOfType and TryGetDescendantsKeysOfType exist.

## Key reasoning
- Version: the user said Umbraco 17, so only the v17 docs were fetched.
- The accessor fails because there's no request. The fix is a singleton with IUmbracoContextFactory, opening the context per read in a `using` and materialising results to a record before it's disposed. Read from the cache, not from IContentService.
- Recommended IRecurringBackgroundJob over a raw IHostedService (it respects server roles), but the reader also works from their own IHostedService.
- Absolute URLs without a request depend on domains or UmbracoApplicationUrl. This is flagged as not runtime-verified.

## Compile check
- A scratch project in the session scratchpad referenced Umbraco.Cms.Web.Website 17.5.3.
- The first build failed: RunJobAsync() isn't virtual, and the abstract member is RunJobAsync(CancellationToken). After fixing that, the build succeeded with no CS warnings.
- No repo files were modified. Runtime behaviour was not verified.
