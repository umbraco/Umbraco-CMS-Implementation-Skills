# Transcript

1. Listed the skill folder (skipped evals/) and read SKILL.md.
2. Routed to references/custom-services.md (lifetime question). Read it and all assets: ISiteService.cs, SiteService.cs (scoped), RootContentReader.cs (singleton + IUmbracoContextFactory), SiteServiceComposer.cs, SiteServiceController.cs, UnpublishingUrlRecorder.cs.
3. Version: the user said Umbraco 17 and there's no project, so I used the v17 doc row only.
4. Fetched https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md. It confirms that UmbracoHelper/IPublishedContentQuery are request-scoped, that injecting them into a singleton errors at startup, and that IUmbracoContextFactory.EnsureUmbracoContext + IDocumentNavigationQueryService is the pattern outside requests.
5. Reasoning:
   - Dev-only failure = ASP.NET Core ValidateScopes is on only in Development.
   - Production silently captures one helper (a captive dependency).
   - The future scheduled job rules out AddScoped, so the answer is a singleton based on RootContentReader.
   - Explained the trade-off in a table and flagged what wasn't verified (the TryGetDescendantsKeysOfType call and the aliases; no build possible).
6. Wrote response.md. No repo files were modified.
