Umbraco 18. We added an embed provider for our partner's site, which supports oEmbed (JSON). Pasting their URLs in the Embed dialog never returns anything. Here it is:

```csharp
public class PartnerEmbedProvider : OEmbedProviderBase
{
    public PartnerEmbedProvider(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }
    public override string ApiEndpoint => "https://partner.example.org/oembed?url=";
    public override string[] UrlSchemeRegex => [@"partner\.example\.org/media/"];
    public override Dictionary<string, string> RequestParams => [];
    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
```

It's registered with Append in a composer. What's wrong?