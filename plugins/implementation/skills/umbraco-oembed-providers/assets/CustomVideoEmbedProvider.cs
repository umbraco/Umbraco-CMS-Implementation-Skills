using System.Net;
using Umbraco.Cms.Core.Media;

namespace <Namespace>.EmbedProviders;

// A host with NO oEmbed API: build the player markup from the pasted URL, no outbound call.
// Change the host in UrlSchemeRegex and the player URL to your platform's.
public class CustomVideoEmbedProvider : IEmbedProvider
{
    public string ApiEndpoint => string.Empty; // nothing is requested

    // Anchor the pattern: an unanchored "video\.example\.com" would also match other hosts' URLs.
    public string[] UrlSchemeRegex => [@"^https?:\/\/video\.example\.com\/v\/"];

    public Dictionary<string, string> RequestParams => [];

    public Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        int width = maxWidth is > 0 ? maxWidth.Value : 640;
        int height = maxHeight is > 0 ? maxHeight.Value : 360;

        // The URL is editor input: escape it for the query string, then for the HTML attribute.
        string src = WebUtility.HtmlEncode("https://video.example.com/embed?src=" + Uri.EscapeDataString(url));

        return Task.FromResult<string?>(
            $"<iframe class=\"custom-video\" src=\"{src}\" width=\"{width}\" height=\"{height}\" frameborder=\"0\" allowfullscreen></iframe>");
    }
}
