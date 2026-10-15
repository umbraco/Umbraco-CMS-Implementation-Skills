# Transcript: docs17-provider-wont-compile (without skill)

1. Read prompt.md. The user's code overrides `GetMarkup` and `GeOEmbedDataAsync` and gets CS0115 x2 and CS0534 (missing `GetMarkupAsync`). My hypothesis was that this is pre-async API code and v17 needs only `GetMarkupAsync`.
2. WebFetch https://raw.githubusercontent.com/umbraco/Umbraco-CMS/v17/main/src/Umbraco.Core/Media/EmbedProviders/OEmbedProviderBase.cs returned 404 (wrong branch name). The IEmbedProvider.cs fetch on the same branch also returned 404.
3. `gh api search/code` located `OEmbedProviderBase.cs`. The tag list showed release-17.x tags.
4. `gh api repos/umbraco/Umbraco-CMS/contents/...?ref=release-17.5.3` fetched IEmbedProvider.cs and OEmbedProviderBase.cs. Confirmed: only `GetMarkupAsync(string, int?, int?, CancellationToken)` exists and it is abstract. There is no `GetMarkup` and no `GeOEmbedDataAsync`. `GetEmbedProviderUrl` appends `"?url="` itself.
5. Listed the EmbedProviders folder (no built-in DeviantArt in 17). Fetched LottieFiles.cs, OEmbedResponseWithStringDimensions.cs and Youtube.cs, which use a bare endpoint with no `?url=`, plus `AddEmbedProvider<T>` in UmbracoBuilder.CollectionBuilders.cs.
6. WebFetch https://docs.umbraco.com/umbraco-cms/extending/embedded-media-provider returned a 404 page pointing to the new URL.
7. WebFetch https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers.md showed the current sample already uses `GetMarkupAsync`, but it still has `ApiEndpoint => ".../oembed?url="`. That produces a double `?url=` at runtime.
8. Conclusions: delete `GetMarkup`, rename `GeOEmbedDataAsync` to `GetMarkupAsync`, drop `?url=` from `ApiEndpoint`, and register via `builder.EmbedProviders().Append<T>()`. Nothing was compiled; there is no project.
