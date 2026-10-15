# Vimeo in the Umbraco 17 Embed dialog

**You don't need to add one. Umbraco already ships a Vimeo embed provider and turns it on by default.**

Umbraco registers it in core, in `Umbraco.Core/DependencyInjection/UmbracoBuilder.Collections.cs`:

```csharp
builder.EmbedProviders()
    .Append<YouTube>()
    .Append<Vimeo>()        // <-- already there
    .Append<DailyMotion>()
    // ... Flickr, Slideshare, Kickstarter, GettyImages, Ted, Soundcloud,
    //     Issuu, Hulu, Giphy, LottieFiles, X
```

The provider is `Umbraco.Cms.Core.Media.EmbedProviders.Vimeo`:

```csharp
public override string ApiEndpoint => "https://vimeo.com/api/oembed.xml";
public override string[] UrlSchemeRegex => [@"^https?:\/\/(www\.)?vimeo\.com\/"];
```

If you added a second Vimeo provider, you would have two competing providers for the same URLs and twice the code to maintain. Don't do it.

## What editors should do

1. In the Rich Text Editor, open **Embed**, or use a property that offers the embed dialog.
2. Paste the normal Vimeo page URL, for example `https://vimeo.com/76979871`.
3. The preview should appear. Insert it.

## If it "doesn't work", check these first

| Symptom | Likely cause / fix |
|---|---|
| URL like `https://player.vimeo.com/video/76979871` is not recognised | The built-in regex only matches `vimeo.com/` and `www.vimeo.com/`, not `player.vimeo.com`. Ask editors to paste the page URL `https://vimeo.com/<id>` instead. |
| Private/unlisted video gives no preview | Vimeo's oEmbed endpoint only returns markup for videos whose privacy/embed settings allow it. For unlisted videos, paste the full URL including the hash, e.g. `https://vimeo.com/<id>/<hash>`. Also check the video's "Where can this be embedded?" setting in Vimeo. |
| Nothing works for any provider | The server must be able to make outbound HTTPS calls to `vimeo.com` (check firewall/proxy). Check the Umbraco log for the failed oEmbed request. |
| Someone removed it | Search the solution for `EmbedProviders().Remove<Vimeo>()` or `.Clear()` in a composer. |

## Only if you really need `player.vimeo.com` URLs

This is optional. You could replace the built-in provider with a subclass that also accepts player URLs. The Vimeo oEmbed API accepts player URLs. I haven't compiled or tested this against a running site, so test it before you rely on it:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace MySite.Embeds;

public class VimeoWithPlayerUrls : Vimeo
{
    public VimeoWithPlayerUrls(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }

    public override string[] UrlSchemeRegex =>
    [
        @"^https?:\/\/(www\.)?vimeo\.com\/",
        @"^https?:\/\/player\.vimeo\.com\/video\/",
    ];
}

public class VimeoEmbedComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Replace<Vimeo, VimeoWithPlayerUrls>();
}
```

Use `Replace`, not `Append`, so only one Vimeo provider is registered.

## Sources
- Umbraco source, Vimeo provider: https://github.com/umbraco/Umbraco-CMS/blob/main/src/Umbraco.Core/Media/EmbedProviders/Vimeo.cs
- Umbraco source, default registration: https://github.com/umbraco/Umbraco-CMS/blob/main/src/Umbraco.Core/DependencyInjection/UmbracoBuilder.Collections.cs
- Umbraco docs, Embedded Media Providers (lists Vimeo as a default provider): https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers
