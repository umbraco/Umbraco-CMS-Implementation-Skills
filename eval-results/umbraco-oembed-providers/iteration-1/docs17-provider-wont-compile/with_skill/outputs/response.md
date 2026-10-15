# Fixing the DeviantArt embed provider from the Umbraco 17 docs

The Umbraco 17 docs example is wrong for 17.5. It is not something in your project. On Umbraco 17 (and 18), `OEmbedProviderBase` / `IEmbedProvider` has **one** abstract markup method:

```csharp
Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken);
```

The docs sample overrides two methods that don't exist in 17.5.3:

| Error | Cause |
|---|---|
| CS0115 `GetMarkup` | There is no synchronous `GetMarkup(string, int, int)` on the base class any more |
| CS0115 `GeOEmbedDataAsync` | No such method exists (the name is also a typo of "GetOEmbedData") |
| CS0534 `GetMarkupAsync` | The one abstract member you do have to implement is never overridden |

(I checked against the `Umbraco.Cms.Core` 17.5.3 assembly: `GetMarkupAsync`, `GetEmbedProviderUrl`, `GetJsonResponseAsync`, `GetJsonBasedMarkupAsync`, `OEmbedResponseWithStringDimensions` and `GetHtml` are all there. `GetMarkup` and `GeOEmbedDataAsync` are not.)

There is also a second docs bug that compiles fine but breaks at runtime. `ApiEndpoint` ends in `?url=`, but `GetEmbedProviderUrl` adds `?url=<encoded url>` on its own, so the request goes out as `https://backend.deviantart.com/oembed?url=?url=https%3A...`. Use the bare endpoint. The built-in providers do the same.

## Fixed provider

```csharp
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace MySite.EmbedProviders;

public class DeviantArtEmbedProvider : OEmbedProviderBase
{
    public DeviantArtEmbedProvider(IJsonSerializer jsonSerializer)
        : base(jsonSerializer)
    {
    }

    // Bare endpoint. GetEmbedProviderUrl appends "?url=<encoded url>" itself.
    public override string ApiEndpoint => "https://backend.deviantart.com/oembed";

    // Anchored so the patterns don't also match these hosts when they appear inside other URLs.
    public override string[] UrlSchemeRegex =>
    [
        @"^https?:\/\/(www\.)?fav\.me\/",
        @"^https?:\/\/[\w-]+\.deviantart\.com\/art\/",
        @"^https?:\/\/(www\.)?deviantart\.com\/[\w-]+\/art\/",
    ];

    public override Dictionary<string, string> RequestParams => [];

    // The only markup method on 17/18. DeviantArt returns width/height as strings, so
    // deserialize into OEmbedResponseWithStringDimensions (as the docs intended).
    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        var requestUrl = GetEmbedProviderUrl(url, maxWidth, maxHeight);
        OEmbedResponseWithStringDimensions? oembed =
            await GetJsonResponseAsync<OEmbedResponseWithStringDimensions>(requestUrl, cancellationToken);

        // null => IOEmbedService reports ProviderReturnedInvalidResult instead of throwing.
        return oembed?.GetHtml();
    }
}
```

Summary of the changes:
- I deleted `GetMarkup` and `GeOEmbedDataAsync`, and put their body into `GetMarkupAsync`. This keeps the async flow, so there's no `.GetAwaiter().GetResult()` blocking any more.
- `ApiEndpoint` no longer ends in `?url=`.
- The regexes are anchored to `^https?://`. The docs' `fav\.me/*` is unanchored, and its `/*` means "zero or more slashes", so it matches `fav.me` anywhere in any URL. I also added a pattern for the current `www.deviantart.com/<user>/art/...` URL format. That third pattern is my own addition based on how DeviantArt URLs look today, and I haven't verified it against their oEmbed endpoint.
- If DeviantArt ever returned numeric width/height, `GetMarkupAsync => GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken)` would be enough. The string-dimensions response type is the safe choice here, and the docs picked it for that reason.

## Register it

DeviantArt is **not** one of the built-in providers in 17.5.3, so `Append` is the right call: no built-in matches these URLs. (If you were taking over URLs a built-in already matches, `Append` would do nothing because the first matching provider wins. In that case you'd use `Replace`/`InsertBefore`.)

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.EmbedProviders;

namespace MySite;

public class EmbedProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Append<DeviantArtEmbedProvider>();
}
```

Replace `MySite` with your project's namespace.

## Check it

1. `dotnet build`. The CS0115/CS0534 errors should be gone.
2. Run the site, open the Embed dialog (e.g. from a Rich Text Editor with the embed button), and paste a URL such as `https://www.deviantart.com/<artist>/art/<title>-123456789` or a `https://fav.me/...` short link. You should get the DeviantArt preview.

**Not verified here:** you didn't share a project, so I haven't compiled or run this exact class. The API it uses (`GetMarkupAsync`, `GetEmbedProviderUrl`, `GetJsonResponseAsync<OEmbedResponseWithStringDimensions>`, `GetHtml`) is the same set used by a provider template that compiles on 17.5.3 and 18.2.0, and the member names were checked against the 17.5.3 assembly. Whether DeviantArt's live endpoint still answers this way also hasn't been checked.

This code works unchanged on Umbraco 18. 18 also adds a `protected virtual HttpClient GetHttpClient()` you can override if you need a proxy or a test client.
