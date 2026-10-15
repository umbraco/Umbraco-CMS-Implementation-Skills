# Internal video platform in the Umbraco 17 Embed dialog

The Embed dialog (used by the Rich Text Editor and the Embed button) asks every registered
`IEmbedProvider` whether it can handle the pasted URL. Each provider checks its `UrlSchemeRegex`
patterns, and the first provider that matches has its `GetMarkupAsync(...)` called. Its return value
is the HTML that gets inserted.

Your platform has no oEmbed endpoint, so there's nothing to fetch. The provider should implement
`IEmbedProvider` directly and **build the `<iframe>` markup itself**. Don't use
`OEmbedProviderBase`. Its helpers (`GetJsonBasedMarkupAsync`, `GetEmbedProviderUrl`) all expect an
`ApiEndpoint` to call, and its constructor requires an `IJsonSerializer` that you'd never use.

## 1. The provider

`EmbedProviders/InternalVideoEmbedProvider.cs`

```csharp
using System.Net;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Media;

namespace MySite.EmbedProviders;

/// <summary>
/// Embed provider for the internal video platform (no oEmbed API).
/// Turns https://video.intranet.example.com/watch/{id} into an iframe pointing at
/// https://video.intranet.example.com/player?src={url-encoded watch url}.
/// </summary>
public class InternalVideoEmbedProvider : IEmbedProvider
{
    private const string PlayerBaseUrl = "https://video.intranet.example.com/player";
    private const int DefaultWidth = 640;
    private const int DefaultHeight = 360;

    // Strict pattern: https only, exact host, /watch/{id}, optional query/fragment.
    private static readonly Regex WatchUrl = new(
        @"^https://video\.intranet\.example\.com/watch/(?<id>[A-Za-z0-9_-]+)/?(?:[?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // No oEmbed endpoint – not used.
    public string ApiEndpoint => string.Empty;

    // Patterns the Embed dialog uses to pick this provider.
    public string[] UrlSchemeRegex => new[]
    {
        @"^https://video\.intranet\.example\.com/watch/[A-Za-z0-9_-]+",
    };

    // No oEmbed request parameters.
    public Dictionary<string, string> RequestParams => new();

    public Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        Match match = WatchUrl.Match(url.Trim());
        if (match.Success == false)
        {
            // Returning null lets the dialog report that the URL can't be embedded.
            return Task.FromResult<string?>(null);
        }

        // Normalise to the canonical watch URL (drops tracking query strings etc.).
        var watchUrl = $"https://video.intranet.example.com/watch/{match.Groups["id"].Value}";

        var playerUrl = $"{PlayerBaseUrl}?src={Uri.EscapeDataString(watchUrl)}";

        (int width, int height) = GetSize(maxWidth, maxHeight);

        var markup =
            $"<iframe src=\"{WebUtility.HtmlEncode(playerUrl)}\" " +
            $"width=\"{width}\" height=\"{height}\" " +
            "frameborder=\"0\" " +
            "allow=\"autoplay; fullscreen; picture-in-picture\" " +
            "allowfullscreen " +
            "title=\"Internal video\"></iframe>";

        return Task.FromResult<string?>(markup);
    }

    // Respect the dialog's width/height, keeping 16:9 when only one is constrained.
    private static (int Width, int Height) GetSize(int? maxWidth, int? maxHeight)
    {
        int width = maxWidth is > 0 ? maxWidth.Value : DefaultWidth;
        int height = maxHeight is > 0 ? maxHeight.Value : DefaultHeight;

        if (maxWidth is > 0 && maxHeight is not > 0)
        {
            height = (int)Math.Round(width * 9 / 16.0);
        }
        else if (maxHeight is > 0 && maxWidth is not > 0)
        {
            width = (int)Math.Round(height * 16 / 9.0);
        }

        return (width, height);
    }
}
```

Notes on the code:

- **`Uri.EscapeDataString`** produces the "encoded watch URL" the player needs:
  `https%3A%2F%2Fvideo.intranet.example.com%2Fwatch%2Fabc123`.
- **`WebUtility.HtmlEncode`** escapes the `src` attribute value, so the `&`/`"` characters can't
  break out of the attribute.
- The **regex only allows a safe id character set** (`[A-Za-z0-9_-]`). Change it if your ids use
  other characters, such as GUIDs with braces or dots. Keep it anchored to the exact host so the
  provider can't be used to frame arbitrary URLs.
- `GetMarkupAsync` is the one method on the Umbraco 17 `IEmbedProvider` interface. The old
  synchronous `GetMarkup(...)` has been removed.

## 2. Register it

`EmbedProviders/InternalVideoEmbedComposer.cs`

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace MySite.EmbedProviders;

public class InternalVideoEmbedComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Append<InternalVideoEmbedProvider>();
}
```

`EmbedProviders()` is the `EmbedProvidersCollectionBuilder` extension in
`Umbraco.Cms.Core.DependencyInjection`. Composers are discovered automatically, so `Program.cs`
doesn't need to change. If another provider's regex could ever match the same host, use
`.Insert<InternalVideoEmbedProvider>()` instead to put yours first.

## 3. Try it

1. Build and restart the site.
2. In the backoffice, open a Rich Text Editor property, click **Embed**, and paste
   `https://video.intranet.example.com/watch/abc123`.
3. The preview should show the player, and the inserted markup should be:

   ```html
   <iframe src="https://video.intranet.example.com/player?src=https%3A%2F%2Fvideo.intranet.example.com%2Fwatch%2Fabc123"
           width="640" height="360" frameborder="0" allow="autoplay; fullscreen; picture-in-picture"
           allowfullscreen title="Internal video"></iframe>
   ```
4. If you change width/height in the dialog, it calls `GetMarkupAsync` again with
   `maxWidth`/`maxHeight`, and the iframe size updates.

To test without the backoffice UI, you can call the Management API endpoint the dialog uses:
`GET /umbraco/management/api/v1/oembed/query?url=...&maxWidth=...&maxHeight=...`. It requires an
authenticated backoffice token. The route is from memory, so check it in Swagger at
`/umbraco/swagger`.

## Things outside Umbraco to check

- **Framing headers on the video platform.** The player must allow framing by both the backoffice
  host (for the preview) and the public site host. Check its `X-Frame-Options` and
  `Content-Security-Policy: frame-ancestors` settings.
- **Your site's own CSP.** If the Umbraco site sends a CSP, add
  `frame-src https://video.intranet.example.com` (or equivalent) for both the front end and the
  backoffice.
- **Intranet reachability.** The iframe loads in the visitor's browser. If the site is public but
  the platform is intranet-only, outside visitors will get a broken frame. Decide whether that's
  acceptable, or show a fallback message in the template.
- **Server-side calls.** This provider never calls the video platform from the server, so the
  Umbraco server doesn't need network access to it. That's one benefit of not faking an oEmbed
  call.

## Optional: Block-based or property-editor usage

The same provider is used anywhere the Embed dialog appears. If editors should add videos as a
block rather than inside rich text, a simple alternative is a Text Box property holding the watch
URL. A Razor view then builds the same iframe using the same regex and encoding logic. You could
move that logic into a shared static helper.
