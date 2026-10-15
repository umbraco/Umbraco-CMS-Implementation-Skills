using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Serialization;

namespace <Namespace>.EmbedProviders;

// A host that HAS an oEmbed API returning JSON. Replace the endpoint and patterns with the host's.
public class OEmbedJsonProviderTemplate : OEmbedProviderBase
{
    public OEmbedJsonProviderTemplate(IJsonSerializer jsonSerializer)
        : base(jsonSerializer)
    {
    }

    // Bare endpoint: GetEmbedProviderUrl appends "?url=<encoded url>" itself. Ending this with "?url="
    // (as the docs' DeviantArt example does) produces a malformed "...?url=?url=..." request.
    public override string ApiEndpoint => "https://media.example.com/oembed";

    public override string[] UrlSchemeRegex => [@"^https?:\/\/media\.example\.com\/watch\/"];

    // Extra query parameters the host needs, e.g. ["format"] = "json". Appended after url, before maxwidth/maxheight.
    public override Dictionary<string, string> RequestParams => [];

    // Standard JSON oEmbed: the base class requests GetEmbedProviderUrl(...) and returns the "html"
    // field. If the host sends width/height as strings, use GetJsonResponseAsync<OEmbedResponseWithStringDimensions>
    // and return its GetHtml(). For XML hosts use GetXmlBasedMarkupAsync.
    public override async Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
        => await GetJsonBasedMarkupAsync(url, maxWidth, maxHeight, cancellationToken);
}
