# Internal video platform in the Umbraco 17 Embed dialog

## Which kind of provider

Your platform has **no oEmbed API**, so an `OEmbedProviderBase` subclass is the wrong tool. It would try to call an endpoint that doesn't exist. Implement `IEmbedProvider` directly and build the `<iframe>` from the pasted URL instead. There's **no outbound HTTP call** (`ApiEndpoint => string.Empty`).

No built-in provider matches `video.intranet.example.com`, so you add the provider with `Append<T>()`. That's safe here, because `IOEmbedService` uses the **first** provider whose regex matches, and nothing ahead of yours matches this host.

> Replace `MySite` below with your project's root namespace.

## 1. The provider

`EmbedProviders/IntranetVideoEmbedProvider.cs`

```csharp
using System.Net;
using Umbraco.Cms.Core.Media;

namespace MySite.EmbedProviders;

// Internal video platform: no oEmbed API, so the player markup is built from the pasted URL.
// No outbound request is made.
public class IntranetVideoEmbedProvider : IEmbedProvider
{
    public string ApiEndpoint => string.Empty; // nothing is requested

    // Anchored, and requires a non-empty id after /watch/.
    // An unanchored "video\.intranet\.example\.com" would also match that text inside other URLs.
    public string[] UrlSchemeRegex => [@"^https?:\/\/video\.intranet\.example\.com\/watch\/[^\/?#]+"];

    public Dictionary<string, string> RequestParams => [];

    public Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        // Defensive: return null for anything we can't handle. IOEmbedService then reports
        // ProviderReturnedInvalidResult to the dialog instead of throwing.
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            || !uri.Host.Equals("video.intranet.example.com", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(null);
        }

        int width = maxWidth is > 0 ? maxWidth.Value : 640;
        int height = maxHeight is > 0 ? maxHeight.Value : 360;

        // The URL is editor input. Escape it as a query value (the player expects the
        // encoded watch URL in ?src=), then HTML-encode the whole src for the attribute.
        string src = WebUtility.HtmlEncode(
            "https://video.intranet.example.com/player?src=" + Uri.EscapeDataString(url));

        return Task.FromResult<string?>(
            $"<iframe class=\"intranet-video\" src=\"{src}\" width=\"{width}\" height=\"{height}\" " +
            "frameborder=\"0\" allow=\"fullscreen; picture-in-picture\" allowfullscreen></iframe>");
    }
}
```

## 2. Registration

`EmbedProvidersComposer.cs`

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MySite.EmbedProviders;

namespace MySite;

public class EmbedProvidersComposer : IComposer
{
    // Append: no built-in matches video.intranet.example.com, so position doesn't matter.
    // (Append would be silently ignored for URLs a built-in already matches. Use
    // Insert/InsertBefore/Replace in that case.)
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Append<IntranetVideoEmbedProvider>();
}
```

Umbraco discovers composers automatically, so you don't need to change `Program.cs`.

## What it matches

| Pasted URL | Result |
|---|---|
| `https://video.intranet.example.com/watch/abc123` | `<iframe src="https://video.intranet.example.com/player?src=https%3A%2F%2Fvideo.intranet.example.com%2Fwatch%2Fabc123" …>` |
| `http://video.intranet.example.com/watch/abc123?t=30` | matches. The query string is encoded into `src` too |
| `https://VIDEO.intranet.example.com/watch/abc123` | matches (regex is case-insensitive) |
| `https://video.intranet.example.com/watch/` (no id) | not matched, so the dialog shows no result |
| `https://evil.example.com/?x=video.intranet.example.com/watch/1` | not matched, because the pattern is anchored |

## Notes

- **Width/height:** the dialog's width and height are passed through as `maxWidth`/`maxHeight`. The provider falls back to 640×360 when they're not set.
- **Security:** the pasted URL is untrusted. It goes through `Uri.EscapeDataString`, then `WebUtility.HtmlEncode`, so it can't break out of the `src` attribute.
- **Intranet reachability:** Umbraco makes no server-side request. The editor's and visitor's browser load the player directly, so the iframe only plays for people who can reach `video.intranet.example.com`. Check that the platform allows framing from your site's origin (`X-Frame-Options` / CSP `frame-ancestors`). If your site sends a CSP, add the host to `frame-src`.
- **Rich Text Editor / Data Type setup** (enabling the Embed button, Media Picker etc.) is content modelling and out of scope here.

## Verify

1. `dotnet build`.
2. Run the site, open a page with a Rich Text Editor that has Embed enabled (or an Embed property), and paste `https://video.intranet.example.com/watch/<real id>`. The preview should show the player.
3. Alternatively, resolve `IOEmbedService` and call `GetMarkupAsync(url, 640, 360, ct)`. A successful result returns the iframe markup.

**Unverified:** I didn't build or run this against your project (none was provided). The pattern is the skill's `CustomVideoEmbedProvider` asset, which compiles on Umbraco 17.5.3 and is runtime-tested there, adapted to your host and player URL.
