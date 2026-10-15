# Why the partner embed provider returns nothing

## The bug: `?url=` appears twice

`ApiEndpoint` should be the bare endpoint, with no query string. `OEmbedProviderBase.GetEmbedProviderUrl` adds the `url` parameter itself. Here is the code from Umbraco's `OEmbedProviderBase.cs`:

```csharp
fullUrl.Append(ApiEndpoint);
fullUrl.Append("?url=" + WebUtility.UrlEncode(url));
foreach (var param in RequestParams) fullUrl.Append($"&{param.Key}={param.Value}");
if (maxWidth > 0)  fullUrl.Append("&maxwidth="  + maxWidth);
if (maxHeight > 0) fullUrl.Append("&maxheight=" + maxHeight);
```

With your `ApiEndpoint` of `https://partner.example.org/oembed?url=`, the request goes to:

```
https://partner.example.org/oembed?url=?url=https%3A%2F%2Fpartner.example.org%2Fmedia%2F123&maxwidth=...
```

The partner's server sees `url` = `?url=https://...`. That isn't a valid URL, so it returns a 400 or 404, or a non-oEmbed body. The base class then either can't download it or can't deserialize it into an `OEmbedResponse`. `GetHtml()` ends up null or empty, and the Embed dialog shows nothing.

## The fix

Remove `?url=` from `ApiEndpoint`:

```csharp
public class PartnerEmbedProvider : OEmbedProviderBase
{
    public PartnerEmbedProvider(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }

    public override string ApiEndpoint => "https://partner.example.org/oembed";

    public override string[] UrlSchemeRegex => [@"partner\.example\.org/media/"];

    // Extra query params only (key/value). Don't put url, maxwidth or maxheight here,
    // because the base class adds those itself.
    // Add e.g. { "format", "json" } if the partner requires it.
    public override Dictionary<string, string> RequestParams => [];

    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
```

The rest of your provider is fine:
- The `UrlSchemeRegex` will match `https://partner.example.org/media/...`.
- `GetJsonBasedMarkupAsync` is the right helper for a JSON endpoint.
- Registering it with `builder.EmbedProviders().Append<PartnerEmbedProvider>()` in a composer is correct.

## How to check it

1. Request the endpoint directly. It should return JSON with `type` and `html` (or `url` for `photo`):
   `curl "https://partner.example.org/oembed?url=https%3A%2F%2Fpartner.example.org%2Fmedia%2F123"`
2. If you still get nothing after the fix, check these:
   - **`format` parameter:** some providers default to XML. If so, add `{ "format", "json" }` to `RequestParams`. Values there are **not** URL-encoded by the base class, so keep them simple.
   - **Response `type`:** `OEmbedResponse.GetHtml()` returns the `html` field for `video`/`rich` and builds an `<img>` for `photo`. A `link` type has no markup.
   - **Matching order:** the first registered provider whose regex matches handles the URL. Make sure no earlier provider also matches the partner's URLs.
   - **Logs:** check the Umbraco log for HTTP or deserialization errors from the embed request, which is made server-side.

**Source:** `src/Umbraco.Core/Media/EmbedProviders/OEmbedProviderBase.cs` on the Umbraco-CMS `main` branch on GitHub. I didn't check the v18 tag specifically. This URL-building logic has been the same since v8, so I'm assuming v18 matches.
