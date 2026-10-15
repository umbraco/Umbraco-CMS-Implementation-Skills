using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Skills.Examples.OEmbedProviders.EmbedProviders;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-oembed-providers skill, fully offline: no test reaches
/// the internet. The providers are compiled from the skill's assets/ and registered by its composer.
///
/// Calls go through IOEmbedService, the same service the backoffice Embed dialog uses, which picks the
/// FIRST provider whose UrlSchemeRegex matches. That makes the YouTube tests the proof that
/// Replace&lt;YouTube, ...&gt;() really took over: with Append the built-in would answer first and try to
/// call youtube.com.
/// </summary>
[TestFixture]
public class OEmbedProvidersTests
{
    private static T Resolve<T>() where T : notnull =>
        ReferenceSiteFixture.Factory.Services.GetRequiredService<T>();

    private static async Task<string> MarkupFor(string url, int? width = null, int? height = null)
    {
        Attempt<string, OEmbedOperationStatus> attempt =
            await Resolve<IOEmbedService>().GetMarkupAsync(new Uri(url), width, height, CancellationToken.None);

        Assert.That(attempt.Status, Is.EqualTo(OEmbedOperationStatus.Success), $"no markup for {url}");
        return attempt.Result!;
    }

    [TestCase("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("https://youtu.be/dQw4w9WgXcQ")]
    [TestCase("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [TestCase("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ")]
    public async Task YouTube_urls_render_the_privacy_enhanced_player(string url)
    {
        string markup = await MarkupFor(url, 400, 225);

        Assert.That(markup, Does.Contain("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"));
        Assert.That(markup, Does.Contain("width=\"400\"").And.Contain("height=\"225\""));
    }

    [Test]
    public void The_builtin_YouTube_provider_was_replaced_not_kept()
    {
        var providers = Resolve<EmbedProvidersCollection>().ToList();

        Assert.That(providers.OfType<YouTube>(), Is.Empty, "built-in YouTube still registered");
        Assert.That(providers.OfType<YouTubeNoCookieEmbedProvider>().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task A_youtube_url_without_a_video_id_is_reported_as_invalid_not_thrown()
    {
        Attempt<string, OEmbedOperationStatus> attempt = await Resolve<IOEmbedService>()
            .GetMarkupAsync(new Uri("https://www.youtube.com/watch?list=only-a-playlist"), null, null, CancellationToken.None);

        Assert.That(attempt.Status, Is.EqualTo(OEmbedOperationStatus.ProviderReturnedInvalidResult));
    }

    [Test]
    public async Task Custom_host_without_oembed_renders_its_own_player_with_encoded_input()
    {
        string markup = await MarkupFor("https://video.example.com/v/123?t=5&lang=da");

        Assert.That(markup, Does.Contain("src=\"https://video.example.com/embed?src=https%3A%2F%2Fvideo.example.com%2Fv%2F123%3Ft%3D5%26lang%3Dda\""));
        Assert.That(markup, Does.Contain("width=\"640\"").And.Contain("height=\"360\""), "defaults when no size is asked");
    }

    [Test]
    public void OEmbed_template_builds_a_single_url_parameter_request()
    {
        OEmbedJsonProviderTemplate provider = Resolve<EmbedProvidersCollection>().OfType<OEmbedJsonProviderTemplate>().Single();
        const string pasted = "https://media.example.com/watch/abc";

        string request = provider.GetEmbedProviderUrl(pasted, 500, 300);

        Assert.That(request, Is.EqualTo(
            "https://media.example.com/oembed?url=https%3A%2F%2Fmedia.example.com%2Fwatch%2Fabc&maxwidth=500&maxheight=300"));
        Assert.That(provider.UrlSchemeRegex.Any(p => Regex.IsMatch(pasted, p, RegexOptions.IgnoreCase)), Is.True);
        Assert.That(provider.UrlSchemeRegex.Any(p => Regex.IsMatch("https://evil.example/?x=media.example.com/watch/", p, RegexOptions.IgnoreCase)),
            Is.False, "anchored pattern must not match the host name inside another URL");
    }
}
