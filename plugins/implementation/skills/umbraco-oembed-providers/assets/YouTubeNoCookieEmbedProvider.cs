using System.Net;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Media;

namespace <Namespace>.EmbedProviders;

// Privacy-enhanced YouTube: writes a youtube-nocookie.com iframe itself, so embedding makes no oEmbed
// request to YouTube. Whether this meets your privacy requirements is a legal call, not a code one.
// Registered with Replace<YouTube, ...>() in EmbedProvidersComposer: the built-in YouTube provider
// matches the same URLs and the first matching provider wins, so Append would never reach this one.
public partial class YouTubeNoCookieEmbedProvider : IEmbedProvider
{
    public string ApiEndpoint => string.Empty; // no oEmbed endpoint is called

    // Same URL shapes as the built-in YouTube provider.
    public string[] UrlSchemeRegex =>
    [
        @"^https?:\/\/(www\.)?youtu\.be\/",
        @"^https?:\/\/(www\.)?youtube\.com\/watch",
        @"^https?:\/\/(www\.)?youtube\.com\/shorts\/",
        @"^https?:\/\/(www\.)?youtube\.com\/live\/",
    ];

    public Dictionary<string, string> RequestParams => [];

    public Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken)
    {
        Match match = VideoId().Match(url);
        if (!match.Success)
        {
            return Task.FromResult<string?>(null); // reported to the editor as an invalid result
        }

        string id = match.Groups[1].Value;
        int width = maxWidth is > 0 ? maxWidth.Value : 560;
        int height = maxHeight is > 0 ? maxHeight.Value : 315;

        string markup =
            $"<iframe width=\"{width}\" height=\"{height}\" src=\"https://www.youtube-nocookie.com/embed/{WebUtility.HtmlEncode(id)}\" " +
            "title=\"YouTube video player\" frameborder=\"0\" referrerpolicy=\"strict-origin-when-cross-origin\" " +
            "allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" allowfullscreen></iframe>";

        return Task.FromResult<string?>(markup);
    }

    // youtu.be/ID, watch?v=ID (v may follow other params), shorts/ID, live/ID. Ids are 11 chars.
    [GeneratedRegex(@"(?:youtu\.be\/|[?&]v=|\/shorts\/|\/live\/)([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase)]
    private static partial Regex VideoId();
}
