# Fixing the DeviantArt embed provider on Umbraco 17.5

## Why it won't build

The code you copied targets the **old** embed provider contract, from before Umbraco went async-only. On Umbraco 17 the contract is different:

- `IEmbedProvider` has **one** markup method: `Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)`.
- `OEmbedProviderBase` declares that method as `abstract`. You must override it, which explains your **CS0534**.
- The synchronous `GetMarkup(string, int, int)` no longer exists, so there is nothing to override (**CS0115**).
- `GeOEmbedDataAsync` (note the typo) also no longer exists on the base class, so you get a second **CS0115**.

Here are the 17.5.3 signatures from the source (`src/Umbraco.Core/Media/IEmbedProvider.cs` and `src/Umbraco.Core/Media/EmbedProviders/OEmbedProviderBase.cs`, tag `release-17.5.3`):

```csharp
public abstract Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken);
public virtual string GetEmbedProviderUrl(string url, int? maxWidth, int? maxHeight);
public virtual Task<T?> GetJsonResponseAsync<T>(string url, CancellationToken cancellationToken) where T : class;
public virtual Task<string?> GetJsonBasedMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken);
```

## The fix

Delete `GetMarkup`. Rename `GeOEmbedDataAsync` to `GetMarkupAsync`, keeping the same `(string, int?, int?, CancellationToken)` signature:

```csharp
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace MyProject.EmbedProviders;

public class DeviantArtEmbedProvider : OEmbedProviderBase
{
    public DeviantArtEmbedProvider(IJsonSerializer jsonSerializer)
        : base(jsonSerializer)
    {
    }

    // No "?url=" here: GetEmbedProviderUrl appends "?url=<encoded url>" itself.
    public override string ApiEndpoint => "https://backend.deviantart.com/oembed";

    public override string[] UrlSchemeRegex =>
    [
        @"fav\.me/*",
        @"\w+\.deviantart\.com\/\w+\/art\/*",
        @"\w+\.deviantart\.com\/art\/*",
        @"sta\.sh/*",
    ];

    public override Dictionary<string, string> RequestParams => [];

    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        var requestUrl = GetEmbedProviderUrl(url, maxWidth, maxHeight);

        // DeviantArt returns width/height as strings, so use the string-dimension response type.
        OEmbedResponseWithStringDimensions? oembed =
            await GetJsonResponseAsync<OEmbedResponseWithStringDimensions>(requestUrl, cancellationToken);

        return oembed?.GetHtml();
    }
}
```

Register it with a composer:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace MyProject.EmbedProviders;

public class RegisterEmbedProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Append<DeviantArtEmbedProvider>();
        // or: builder.AddEmbedProvider<DeviantArtEmbedProvider>();
}
```

## One more bug: remove `?url=` from `ApiEndpoint`

This one won't stop the build, but the embed won't work at runtime. In 17.x, `GetEmbedProviderUrl` builds the request as `ApiEndpoint + "?url=" + WebUtility.UrlEncode(url) + ...`. If `ApiEndpoint` already ends in `?url=` (as yours does, and so does the current docs sample), the request URL comes out as:

```
https://backend.deviantart.com/oembed?url=?url=https%3A%2F%2F...
```

DeviantArt won't get a valid `url` parameter from that. The built-in providers (for example `YouTube` and `LottieFiles`) all use a bare endpoint such as `"https://www.youtube.com/oembed"`. Do the same here.

Two smaller notes:

- The regex `deviantart.com` had an unescaped `.`. I escaped it (`deviantart\.com`). It's harmless either way, but the escaped version is stricter.
- If the provider returns standard integer width/height, you can replace the whole method body with `=> GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);`. This is what the built-in `YouTube` provider does. Keep `OEmbedResponseWithStringDimensions` for DeviantArt, because its oEmbed response returns dimensions as strings.

## Quick check

1. Run `dotnet build`. The three errors should be gone.
2. Paste a DeviantArt art URL (`https://www.deviantart.com/<user>/art/<slug>`) into an Embed in the Rich Text Editor (or Block editor). It should resolve to markup.
3. If it doesn't resolve, set a breakpoint in `GetMarkupAsync` and inspect `requestUrl`. A double `?url=` means the endpoint fix is missing. If the method is never hit, the composer isn't registered or `UrlSchemeRegex` doesn't match the pasted URL.

## Sources

- Umbraco CMS source, tag `release-17.5.3`: `src/Umbraco.Core/Media/IEmbedProvider.cs`, `src/Umbraco.Core/Media/EmbedProviders/OEmbedProviderBase.cs`, `Youtube.cs`, `LottieFiles.cs`, `OEmbedResponseWithStringDimensions.cs`, `src/Umbraco.Core/DependencyInjection/UmbracoBuilder.CollectionBuilders.cs` (`AddEmbedProvider<T>`). Fetched via the GitHub API.
- Umbraco docs, Embedded Media Providers: https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers.md (the current sample already uses `GetMarkupAsync`, but it still keeps `?url=` in `ApiEndpoint`).
