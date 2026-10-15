# Transcript: news-section-helper (with_skill)

## Files read
- plugins/implementation/skills/umbraco-services/SKILL.md
- references/custom-services.md (the task is "write your own service / extension, pick lifetime")
- assets/ISiteService.cs, SiteService.cs, SiteServiceComposer.cs, SiteServiceController.cs, RootContentReader.cs
- Did not read evals/ or skill-workspaces/.

## Fetched
- https://docs.umbraco.com/umbraco-cms/17.latest/extend-your-project/server-side-extensions/services.md (v17 row only, as the skill says). It confirmed the extension-method example `GetNewsSection(this IPublishedContentQuery)`, the custom-service option, and the warning not to inject IPublishedContentQuery into a singleton.
- Checked the local NuGet XML docs for Umbraco.Web.Common 17.5.3: `FriendlyPublishedContentExtensions.FirstChildOfType(IPublishedContent, string, string)` exists.

## Key reasoning
- Version: the user stated Umbraco 17, and there's no project, so I used the 17 docs.
- Static UmbracoHelper field: rejected. UmbracoHelper is scoped per request, so a static field captures the first request (stale data, cross-request leakage, disposed context) and can't be injected or tested.
- Extension method vs service, following the skill's decision table: the extension method suits exactly this lookup. A service fits better with two controllers plus several templates and likely future "key page" lookups. I recommended a scoped ISiteService (based on the verified assets) and showed the extension method as an equally correct alternative.
- Lifetime: scoped, because it injects IPublishedContentQuery. I noted the singleton + IUmbracoContextFactory pattern for use outside a request.
- I flagged that the code wasn't built. The asset pattern is verified on 17.5.3, but the GetNewsSection addition isn't.
