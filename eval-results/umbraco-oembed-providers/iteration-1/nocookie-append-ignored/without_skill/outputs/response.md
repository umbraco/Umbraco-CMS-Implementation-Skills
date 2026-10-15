# Why your YouTube nocookie provider is ignored

## Short answer

The order of providers is the problem. Umbraco's embed provider collection (`EmbedProvidersCollection`) is **ordered**. When an editor pastes a URL, Umbraco goes through the providers in order and uses the **first** one whose `UrlSchemeRegex` matches the URL. The built-in `YouTube` provider (`Umbraco.Cms.Core.Media.EmbedProviders.YouTube`) is already in the collection and matches every `youtube.com` and `youtu.be` URL. `Append<T>()` puts your provider **after** it, so your provider is never reached.

## Fix: replace the built-in provider

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Media.EmbedProviders;

public class YouTubeNoCookieComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders()
                  .Replace<YouTube, YouTubeNoCookieProvider>();
}
```

You can also do this, which gives the same result:

```csharp
builder.EmbedProviders()
       .Remove<YouTube>()
       .Append<YouTubeNoCookieProvider>();
```

Prefer `Replace` or `Remove` over `Insert<YouTubeNoCookieProvider>()`. With `Insert`, the stock YouTube provider stays in the collection, so it could start matching again if your regex ever drifts. Your DPO needs a guarantee, and removing the stock provider gives one.

## A provider that outputs nocookie markup

YouTube's oEmbed endpoint always returns an iframe pointing at `https://www.youtube.com/embed/...`. Your provider therefore has to **rewrite the returned HTML**. Changing the request URL is not enough. Here is a minimal version based on `OEmbedProviderBase`:

```csharp
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

public class YouTubeNoCookieProvider : OEmbedProviderBase
{
    public YouTubeNoCookieProvider(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }

    public override string ApiEndpoint => "https://www.youtube.com/oembed";

    // Match youtube.com, youtu.be and (already) youtube-nocookie.com URLs
    public override string[] UrlSchemeRegex => new[]
    {
        @"youtu(?:\.be|be\.com|be-nocookie\.com)/(?:(.*)v(/|=)|(.*/)?)([a-zA-Z0-9-_]+)",
    };

    public override Dictionary<string, string> RequestParams => new() { { "format", "json" } };

    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        // YouTube's oEmbed endpoint does not accept nocookie URLs as input
        url = url.Replace("youtube-nocookie.com", "youtube.com", StringComparison.OrdinalIgnoreCase);

        var requestUrl = base.GetEmbedProviderUrl(url, maxWidth, maxHeight);
        var oembed = await base.GetJsonResponseAsync<OEmbedResponse>(requestUrl, cancellationToken);
        var html = oembed?.GetHtml();

        return html is null
            ? null
            : Regex.Replace(html, @"https?://(www\.)?youtube\.com/embed/", "https://www.youtube-nocookie.com/embed/", RegexOptions.IgnoreCase);
    }
}
```

*Unverified:* check the exact base-class member names against your 17.x build: `GetMarkupAsync` vs the older synchronous `GetMarkup`, `GetEmbedProviderUrl`, and `GetJsonResponseAsync`. The async signature replaced the sync one in recent majors, and the method you need to override is whichever one your version's `IEmbedProvider` exposes. If your class compiles but is never called, an override of the wrong (obsolete) member is the next thing to check.

## Other things to check

1. **Existing content is not changed.** The embed's HTML is generated when the editor inserts it and saved with the content, in the RTE markup or the embed value. Fixing the provider only affects **new** embeds. Existing ones still point at `youtube.com` until they are re-embedded. If your DPO needs everything compliant now, you have two options:
   - Re-insert the existing embeds, or run a one-off content migration that rewrites `youtube.com/embed/` to `youtube-nocookie.com/embed/`.
   - Rewrite the markup when the page renders, as a safety net. For example, post-process the RTE value, or use a response middleware. This also covers the Delivery API output if you render headlessly.
2. **The composer must actually run.** It has to be a public, non-abstract `IComposer` in an assembly the site loads. A breakpoint or log line in `Compose` confirms this quickly.
3. **Test with a fresh insert.** Test in a new browser session with a new paste. An embed already in the editor's RTE won't be fetched again.
4. **Not a full privacy guarantee.** `youtube-nocookie.com` only delays cookies until playback starts. YouTube can still set local storage and identifiers once the video plays. Your DPO may still want a consent gate (click-to-load) under GDPR/ePrivacy. Please have them confirm this. Don't treat the nocookie domain alone as compliant.

## Summary

| Cause | Fix |
|---|---|
| `Append` puts your provider after the built-in `YouTube` provider, which matches first | `Replace<YouTube, YouTubeNoCookieProvider>()` (or `Remove<YouTube>()` + `Append`) |
| oEmbed returns `youtube.com` iframe HTML | Rewrite the `src` to `youtube-nocookie.com` in `GetMarkupAsync` |
| Old embeds are saved as HTML | Re-embed or migrate existing content; optionally rewrite at render time |
