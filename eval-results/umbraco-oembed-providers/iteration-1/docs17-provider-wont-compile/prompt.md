Umbraco 17.5. I copied the DeviantArt embed provider from the Umbraco 17 docs and it won't build:

```csharp
public class DeviantArtEmbedProvider : OEmbedProviderBase
{
    public DeviantArtEmbedProvider(IJsonSerializer jsonSerializer) : base(jsonSerializer) { }

    public override string ApiEndpoint => "https://backend.deviantart.com/oembed?url=";

    public override string[] UrlSchemeRegex => new[] { @"fav\.me/*", @"\w+\.deviantart.com\/art\/*" };

    public override Dictionary<string, string> RequestParams => new();

    public override string? GetMarkup(string url, int maxWidth = 0, int maxHeight = 0)
    {
        return GeOEmbedDataAsync(url, maxWidth, maxHeight, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<string?> GeOEmbedDataAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        var requestUrl = base.GetEmbedProviderUrl(url, maxWidth, maxHeight);
        OEmbedResponseWithStringDimensions? oembed = await base.GetJsonResponseAsync<OEmbedResponseWithStringDimensions>(requestUrl, cancellationToken);
        return oembed?.GetHtml();
    }
}
```

Errors: CS0115 'GetMarkup': no suitable method found to override, CS0115 'GeOEmbedDataAsync': no suitable method found to override, CS0534 does not implement inherited abstract member 'GetMarkupAsync'. How do I fix it?