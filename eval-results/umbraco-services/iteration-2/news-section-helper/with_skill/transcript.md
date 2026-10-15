# Transcript

## Files read
1. plugins/implementation/skills/umbraco-services/SKILL.md (plus a file listing of the skill folder, excluding evals/ and examples/)
2. references/custom-services.md. Routed here from the SKILL.md table: "Own service or extension method, lifetime, registration".
3. assets/ISiteService.cs, assets/SiteService.cs, assets/SiteServiceComposer.cs. This is the asset named in the "Anything scoped (IPublishedContentQuery, UmbracoHelper) → AddScoped" row, plus the registration asset.

No URLs fetched (the reference says to answer from the file and its asset). Did not open using-built-in-services.md or the other assets.

## Reasoning
- Version: the user stated Umbraco 17. No project was provided, so nothing was built and the answer says it is unverified.
- Static UmbracoHelper field: UmbracoHelper/IPublishedContentQuery are scoped, so a static field captures one request's context and leaks it to other requests. This matches the reference's wrong-lifetime symptom and the umbraco-common-pitfalls bug family.
- Extension method vs service: the reference table lists "news section under root" as an extension-method example. Because there are several consumers (templates and controllers) and testability matters, I recommended the service as primary and showed the stateless extension method as a lighter alternative.
- Lifetime: the service depends on scoped IPublishedContentQuery, so it is AddScoped and request-only. I mentioned the IUmbracoContextFactory singleton pattern for background work.
- Adapted the asset: added GetNewsSection() using Children() + ContentType.Alias. Return IPublishedContent? and null-check callers. Composer reduced to the single AddScoped line.
- Kept the multi-site caveat from the asset comment.
