# Why your nocookie provider is ignored

Your provider is fine. The problem is the `Append`.

## Cause: the first matching provider wins

When an editor pastes a URL into the Embed dialog, `IOEmbedService` goes through the registered providers in order. It uses the **first** one whose `UrlSchemeRegex` matches the URL, and the match ignores case.

The built-in `Umbraco.Cms.Core.Media.EmbedProviders.YouTube` provider is already in that list and matches `youtube.com/watch`, `youtu.be/…` and similar URLs. `Append<YouTubeNoCookieProvider>()` puts your provider at the **end**, after the built-in. So for every YouTube URL:

1. The built-in YouTube provider matches first.
2. It calls YouTube's oEmbed API and returns the normal `youtube.com/embed/…` iframe.
3. Umbraco never asks your provider.

`Append` only helps for URLs that no built-in matches. For URLs a built-in already handles, it has no effect.

## Fix: use `Replace` instead of `Append`

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Media.EmbedProviders;

public class EmbedProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders()
            .Replace<YouTube, YouTubeNoCookieProvider>();
}
```

`Replace<TBuiltIn, TNew>()` takes the built-in out of the list and puts your provider in its place. Your provider then handles all YouTube URLs. Because the built-in is gone, Umbraco also stops calling YouTube's oEmbed endpoint when an editor embeds a video. That is likely what your DPO wants, as long as your provider builds the iframe itself and doesn't call YouTube.

Other options, and why they fit less well here:

| Call | Effect |
|---|---|
| `Insert<T>()` / `InsertBefore<YouTube, T>()` | Your provider wins, but the built-in stays registered. Only useful if you want the built-in to handle some URLs your regex skips. |
| `Remove<YouTube>()` + `Append<T>()` | Works, but `Replace` does the same in one call and keeps the provider in the same position. |

## Check your provider while you're in there

- **Match the same URL shapes as the built-in.** Otherwise the URLs your regex misses will fail (with `Replace`) or still go to youtube.com (with `Insert`). Use anchored patterns such as:
  ```csharp
  public string[] UrlSchemeRegex =>
  [
      @"^https?:\/\/(www\.)?youtu\.be\/",
      @"^https?:\/\/(www\.)?youtube\.com\/watch",
      @"^https?:\/\/(www\.)?youtube\.com\/shorts\/",
      @"^https?:\/\/(www\.)?youtube\.com\/live\/",
  ];
  ```
  Without the `^https?:\/\/` anchor, a pattern like `youtube\.com` would also match that text inside other URLs.
- **Return `null` from `GetMarkupAsync` when you can't extract a video ID.** `IOEmbedService` then reports an invalid result to the editor instead of throwing an exception.
- **Treat the pasted URL as untrusted input.** HTML-encode the video ID before putting it in the `src` attribute (`WebUtility.HtmlEncode`), or limit it to the 11-character ID format `[A-Za-z0-9_-]{11}`.
- **Use the v17 signature:** `Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)`. The `GetMarkup(string, int, int)` override in the v17 docs example doesn't exist on 17.x and won't compile.

## Two things the code change won't do

1. **Existing embeds keep their youtube.com iframe.** The Embed dialog saves the generated markup into the content when the editor inserts it. Your provider only affects new embeds. Videos already embedded must be re-embedded, or the stored HTML updated, before they use youtube-nocookie.com. I haven't verified this against a running site: it follows from the markup being saved at insert time.
2. **youtube-nocookie.com isn't automatically GDPR-compliant.** It reduces tracking until the visitor plays the video, but the player still loads from Google and contacts Google's servers. Whether that's enough, or whether you also need consent gating (for example a click-to-load placeholder), is for your DPO to decide. It isn't something the code can guarantee.

## Verify

Build, restart the site, then paste a fresh link such as `https://www.youtube.com/watch?v=dQw4w9WgXcQ` or `https://youtu.be/dQw4w9WgXcQ` into the Embed dialog. The generated iframe `src` should be `https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ`.
