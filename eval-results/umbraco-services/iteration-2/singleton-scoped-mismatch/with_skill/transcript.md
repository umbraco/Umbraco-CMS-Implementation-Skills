# Transcript

## Files read
1. plugins/implementation/skills/umbraco-services/SKILL.md: route table. The task (lifetime, registration, singleton/scoped mismatch) maps to references/custom-services.md.
2. plugins/implementation/skills/umbraco-services/references/custom-services.md: the "Lifetime = from dependencies" table, the wrong-lifetime symptoms (scope validation in Development; data leaks when validation is off), registration via IComposer, and the Lazy<IUmbracoContextFactory> circular-dependency note.
3. assets/RootContentReader.cs: the asset for the "only singletons; reads via IUmbracoContextFactory; works in requests and background work" row. Chosen because the user wants to call the service from a scheduled job.
4. assets/SiteServiceComposer.cs: the registration asset named in the reference's Registration section.

(I did a directory listing of the skill folder to find the files. I did not open the evals/ or examples/ contents. No URLs were fetched, because the reference covered the case.)

## Reasoning
- Root cause: UmbracoHelper is scoped and SiteService is a singleton, so it captures the helper. Development has ValidateScopes on and throws. Production has it off, so the bug stays hidden and a stale helper gets reused across requests.
- Two fixes: (A) AddScoped, which only works inside requests; (B) a singleton using IUmbracoContextFactory plus IDocumentNavigationQueryService, which works in requests and in jobs. The scheduled-job requirement points to B.
- Adapted the RootContentReader pattern to find the contact page and news section by type. TryGetDescendantsKeysOfType isn't in the verified assets, so I flagged it as unverified.
- No project was provided: version assumed to be 17 as the user stated, and nothing was built or run.
