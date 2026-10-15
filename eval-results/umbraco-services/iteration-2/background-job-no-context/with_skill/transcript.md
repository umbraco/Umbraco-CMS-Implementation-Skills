# Transcript

## Files read
1. plugins/implementation/skills/umbraco-services/SKILL.md (plus directory listing; evals/ and examples/ not opened)
2. plugins/implementation/skills/umbraco-services/references/using-built-in-services.md (route: "background job")
3. plugins/implementation/skills/umbraco-services/assets/RootContentReader.cs (the asset that the "No HTTP request" row points to)

No URLs fetched. The reference says to answer from it and the matching asset.

## Reasoning
- No project was given, so I took the Umbraco version (17) from the user's message.
- Context = no HTTP request (hosted service). The reference's "Where the code runs" table says: singleton + IUmbracoContextFactory (+ IDocumentNavigationQueryService), EnsureUmbracoContext() in a `using` block for each read, and never store the context or content in fields. It lists "TryGetUmbracoContext returns false" as a symptom of having no request.
- Read vs change: this is a read, so use the published cache, not IContentService (DB query on every call).
- Adapted RootContentReader: root keys -> children -> "News" by name -> descendant keys -> filter by article alias -> order by UpdateDate -> take 5 -> absolute URL via IPublishedUrlProvider (singleton). Plain values are projected before the context is disposed.
- Hosted service: a BackgroundService with a delay-until-02:00 loop and an IRuntimeState.Level == Run guard. Registered with a composer; the reader is a singleton because all its dependencies are singletons.
- Flagged as unverified: absolute URLs without a request (need domains or UmbracoApplicationUrl), IRecurringBackgroundJob as the load-balancing option, and the fact that the adapted code hasn't been compiled.
