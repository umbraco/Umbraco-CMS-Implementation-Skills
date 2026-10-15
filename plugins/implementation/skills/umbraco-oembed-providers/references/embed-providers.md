# Embed providers

C# providers that turn a URL pasted into the backoffice Embed dialog into embed markup. Editor-side setup (Rich Text Editor toolbar, Data Types) is content modelling and out of scope.

**Answer from this file and the matching asset(s).** Assets compile on 17.5.3 + 18.2.0 and are runtime-tested offline on 17.5.3. Fetch a doc below when you need an API or case not shown here; say if something stays unverified.

Docs (v18 links; for v17 insert `/17.latest` after `/umbraco-cms`):
[embedded-media-providers.md](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers.md) ·
[OEmbedProviderBase API](https://apidocs.umbraco.com/v18/csharp/api/Umbraco.Cms.Core.Media.EmbedProviders.OEmbedProviderBase.html)

## Choose the provider kind

| Case | Build | Open asset |
|---|---|---|
| Site already supported (YouTube, Vimeo, X, Flickr, SoundCloud, Giphy, LottieFiles, …) | Nothing. Check the pasted URL matches the built-in's regex (built-ins: `Umbraco.Cms.Core.Media.EmbedProviders`) | — |
| Site has an **oEmbed API** (JSON) | `OEmbedProviderBase`; `GetMarkupAsync => GetJsonBasedMarkupAsync(...)` (XML: `GetXmlBasedMarkupAsync`). Width/height as strings: `GetJsonResponseAsync<OEmbedResponseWithStringDimensions>(...)` + `GetHtml()` | [`OEmbedJsonProviderTemplate.cs`](../assets/OEmbedJsonProviderTemplate.cs) |
| **No oEmbed API** | Implement `IEmbedProvider`; build markup from the URL; `ApiEndpoint => string.Empty`. URL is editor input: `Uri.EscapeDataString` for query values, `WebUtility.HtmlEncode` for attributes | [`CustomVideoEmbedProvider.cs`](../assets/CustomVideoEmbedProvider.cs) |
| **Change a built-in** (e.g. privacy-enhanced YouTube, own markup) | Own provider + `Replace<BuiltIn, Mine>()` | [`YouTubeNoCookieEmbedProvider.cs`](../assets/YouTubeNoCookieEmbedProvider.cs) |

Every case registers in [`EmbedProvidersComposer.cs`](../assets/EmbedProvidersComposer.cs).

## Registration and ordering

`builder.EmbedProviders()` in an `IComposer`. `IOEmbedService` uses the **first** provider whose `UrlSchemeRegex` matches (case-insensitive).

| Call | Use |
|---|---|
| `Append<T>()` | URLs no built-in matches |
| `Replace<TBuiltIn, T>()` | take over a built-in's URLs, keep its position |
| `Insert<T>()` / `InsertBefore<TExisting, T>()` | win over a built-in for some URLs while keeping it for others |
| `Remove<T>()` | disable a built-in |

Trap: `Append` for URLs a built-in already matches does nothing. The built-in answers (and calls its API). Verified by the runtime gate.

## Regex rules

- Anchor: `^https?:\/\/host\/path`. An unanchored `host\.com` also matches that text inside other URLs.
- Return `null` from `GetMarkupAsync` when the URL can't be handled: `IOEmbedService` then reports `ProviderReturnedInvalidResult` instead of an exception (runtime-verified).

## Docs corrections (Umbraco 17 / 18)

| Docs snippet | Problem | Use instead |
|---|---|---|
| v17 page, DeviantArt example: overrides `GetMarkup(string, int, int)` and `GeOEmbedDataAsync(...)` | Neither exists on 17.5.3: **CS0115** (×2) and **CS0534** (missing `GetMarkupAsync`), verified by compiling | Override `GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken)` (same on 17 and 18) |
| v17 and v18 pages, DeviantArt `ApiEndpoint => "…/oembed?url="` | `GetEmbedProviderUrl` appends `?url=<encoded>` itself → `…oembed?url=?url=…` | Bare endpoint, as built-ins do (e.g. `https://vimeo.com/api/oembed.xml`) |
| Both pages: list of built-in providers | Predates LottieFiles | Check `Umbraco.Cms.Core.Media.EmbedProviders` for the current set |

## Steps

1. Version from `Umbraco.Cms` package.
2. Built-in already covers the URL? Then stop and say so.
3. Pick the row above; open its asset + the composer; replace `<Namespace>`, host, endpoint and patterns.
4. Build. If a site runs, paste a URL into the Embed dialog or call `IOEmbedService.GetMarkupAsync`; else say unverified.

**Done:** provider kind and why, registration call (and why that ordering), sample URLs it matches.
