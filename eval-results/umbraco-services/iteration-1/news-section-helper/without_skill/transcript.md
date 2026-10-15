# Transcript: news-section-helper (without skill, baseline)

- Fetched: nothing. Answered from model knowledge. No repo files were read, per the baseline rules.
- Key reasoning:
  1. A static `UmbracoHelper` field is wrong. The helper is request-scoped (UmbracoContext, culture, preview), so a static field captures one request, is not thread-safe, fails outside requests, and can't be tested.
  2. Recommended a DI service (`INewsSectionService`) registered via an `IComposer` as scoped.
  3. Used the v15+ APIs: `IDocumentNavigationQueryService` (TryGetParentKey walk to root, TryGetChildrenKeysOfType) plus `IPublishedContentCache.GetById(Guid)`. Kept `IUmbracoContextAccessor` only for the "current request" convenience overload.
  4. Multi-site aware: the root is resolved from the current page, not `GetAtRoot().First()`.
  5. Advised against caching `IPublishedContent` in fields.
  6. Showed usage via `@inject` in Razor and constructor injection in a `RenderController`.
- Caveat stated in the answer: API method names were written from memory and not compiled.
