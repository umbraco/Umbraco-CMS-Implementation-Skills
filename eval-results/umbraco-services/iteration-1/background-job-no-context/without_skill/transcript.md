# Transcript — background-job-no-context (without_skill baseline)

- Fetched nothing. Answered from model knowledge. No web lookups, and no repo files read, per the baseline rules.
- Diagnosis: `IUmbracoContextAccessor` only exposes the request-scoped context. A background thread has no request, so `TryGetUmbracoContext` returns false.
- The fix I chose: `IUmbracoContextFactory.EnsureUmbracoContext()` inside the job, mainly because URL generation still relies on the context. Content lookups go through the v15+ injectable `IPublishedContentCache` and `IDocumentNavigationQueryService` (`TryGetRootKeys`, `TryGetChildrenKeysOfType`, `TryGetDescendantsKeysOfType`).
- Scheduling: I recommended `IRecurringBackgroundJob` with `AddRecurringBackgroundJob<T>()` (v13+) over a raw `IHostedService` timer. It gives a runtime-level gate and server-role filtering (Single/SchedulingPublisher), and the delay is computed to 02:00.
- Caveats in the answer: absolute URLs need Culture & Hostnames or `UmbracoApplicationUrl`; `IPublishedContent` has no PublishDate, so use `UpdateDate` or a custom date property (IContentService's PublishDate means a DB hit); culture variants; singleton lifetimes.
- Flagged as unverified: the exact namespaces and the existence of the `*OfType` navigation methods in 17. Not compiled.
