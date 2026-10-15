# Transcript — singleton-cached-content (with_skill)

Files read:
1. plugins/implementation/skills/umbraco-services/SKILL.md (route table: own service + lifetime → references/custom-services.md)
2. references/custom-services.md (Lifetime = from dependencies table; singleton row → RootContentReader.cs; registration → SiteServiceComposer.cs)
3. assets/RootContentReader.cs ("Never store a context or content read from it in a field"; IPublishedContentCache singleton; TryGetRootKeys pattern)
4. assets/ISiteService.cs, assets/SiteService.cs (scoped IPublishedContentQuery alternative), assets/SiteServiceComposer.cs

No URLs fetched. Did not open evals/, examples/, eval-results/.

Key reasoning:
- Root cause: content resolved once in the singleton constructor and held in a field. Publishing swaps in a new IPublishedContent snapshot, so the held instance is stale until restart. It was also read through a disposed UmbracoContext.
- Lifetime is fine (all dependencies are singletons); storing content is the bug. Fix: keep AddSingleton, inject IPublishedContentCache + IDocumentNavigationQueryService, look up per call.
- Also flagged: First() throws or null gets cached when nothing is published, multi-site root choice, URLs outside a request need a per-call EnsureUmbracoContext.
- Used navigation TryGetChildrenKeys instead of the Children() extension to stay on the APIs the asset uses. TryGetChildrenKeys itself is not in the asset, so it is marked unverified.
- Offered a scoped IPublishedContentQuery alternative (per the custom-services table) with the captive-dependency warning.
