# Why the partner embed returns nothing

## The bug: `ApiEndpoint` ends with `?url=`

```csharp
public override string ApiEndpoint => "https://partner.example.org/oembed?url=";
```

`OEmbedProviderBase.GetEmbedProviderUrl(...)` adds `?url=<encoded pasted URL>` (then `RequestParams`, `maxwidth`, `maxheight`) to `ApiEndpoint` itself. Because your endpoint already ends with `?url=`, the request becomes:

```
https://partner.example.org/oembed?url=?url=https%3A%2F%2Fpartner.example.org%2Fmedia%2F...
```

The partner's oEmbed endpoint gets `url` = `?url=https://...` (or a malformed query), so it answers with a 404/400 or no usable JSON. `GetJsonBasedMarkupAsync` then has no `html` to return, and the Embed dialog shows nothing.

The docs' DeviantArt example uses the same `…/oembed?url=` pattern (on both the v17 and v18 pages), which is probably where this came from. The built-in providers use the bare endpoint, for example `https://vimeo.com/api/oembed.xml`.

## Fix

Use the bare endpoint. While you're editing the class, anchor the regex too:

```csharp
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

public class PartnerEmbedProvider : OEmbedProviderBase
{
    public PartnerEmbedProvider(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }

    // Bare endpoint: the base class appends "?url=<encoded url>" itself.
    public override string ApiEndpoint => "https://partner.example.org/oembed";

    // Anchored so it only matches URLs on the partner's host, not that text inside another URL.
    public override string[] UrlSchemeRegex => [@"^https?:\/\/(www\.)?partner\.example\.org\/media\/"];

    // Add extra query params only if the partner needs them, e.g. ["format"] = "json".
    public override Dictionary<string, string> RequestParams => [];

    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
```

The rest of your code is fine.
- `GetMarkupAsync(string, int?, int?, CancellationToken)` is the correct override on Umbraco 17 and 18.
- `GetJsonBasedMarkupAsync` is the right call for a JSON oEmbed API.
- `Append<PartnerEmbedProvider>()` is the right registration because no built-in provider matches `partner.example.org`. `Append` only fails when a built-in already matches the same URLs, since the first matching provider wins. In that case you would need `Replace` or `Insert`/`InsertBefore`.

```csharp
public class EmbedProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.EmbedProviders().Append<PartnerEmbedProvider>();
}
```

## The regex (secondary)

`partner\.example\.org/media/` has no anchor, so it matches that text anywhere in a URL, including inside another site's query string. That doesn't cause the "nothing returned" problem, but it does mean an unrelated URL could be routed to the partner's API. Anchor it as shown above. Matching is case-insensitive.

## Other things to check if it still returns nothing

These aren't confirmed causes, just things to rule out after the fix.
- **Width/height as strings:** if the partner's JSON has `"width": "640"` instead of `640`, deserialization can fail. In that case call `GetJsonResponseAsync<OEmbedResponseWithStringDimensions>(...)` and return its `GetHtml()`.
- **Required format parameter:** some hosts need `format=json`. Add it through `RequestParams`, for example `new() { ["format"] = "json" }`, not in `ApiEndpoint`.
- **Test the endpoint directly:** `curl "https://partner.example.org/oembed?url=<encoded media URL>"` should return JSON that includes an `html` field.

## Verification

I haven't compiled or run this against your project, so it's unverified there. The bare-endpoint pattern matches the skill's JSON oEmbed template, which compiles on 17.5.3 and 18.2.0. That template is also runtime-tested on 17.5.3, where a test asserts that it builds a request with a single `?url=`. After deploying, paste a partner media URL into the Embed dialog to confirm.
