# Transcript – internal-video-no-oembed (without skill)

## Files read
- prompt.md (task)

## URLs fetched
1. https://raw.githubusercontent.com/umbraco/Umbraco-CMS/v17/main/src/Umbraco.Core/Media/IEmbedProvider.cs → 404
2. https://raw.githubusercontent.com/umbraco/Umbraco-CMS/main/src/Umbraco.Core/Media/IEmbedProvider.cs → interface: ApiEndpoint, UrlSchemeRegex, RequestParams, `Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken)` (no sync GetMarkup)
3. https://docs.umbraco.com/umbraco-cms/extending/embedded-media-provider → moved; pointed to new URL
4. https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers.md → summary: two approaches (OEmbedProviderBase vs custom GetMarkupAsync for non-oEmbed), register via `builder.EmbedProviders().Append<T>()` in an IComposer
5. https://raw.githubusercontent.com/umbraco/Umbraco-CMS/release-17.0.0/src/Umbraco.Core/Media/EmbedProviders/OEmbedProviderBase.cs → confirmed base class needs IJsonSerializer and is built around ApiEndpoint fetch

## Key reasoning
- No oEmbed API means there's nothing to fetch, so implement IEmbedProvider directly: ApiEndpoint empty, RequestParams empty, and build the iframe in GetMarkupAsync.
- The player URL takes the url-encoded watch URL (Uri.EscapeDataString). HTML-encode the attribute. Use an anchored, strict regex so the provider can't frame arbitrary hosts. Return null on no match.
- Honour maxWidth/maxHeight and keep 16:9 when only one is given.
- Register via a composer. Mention Insert vs Append for ordering.
- Non-Umbraco concerns: X-Frame-Options/frame-ancestors on the video platform, site CSP frame-src, and intranet reachability for public visitors.
- The Management API oembed route is quoted from memory and flagged in the response for checking in Swagger.
- Nothing was compiled; no buildable project was provided.
