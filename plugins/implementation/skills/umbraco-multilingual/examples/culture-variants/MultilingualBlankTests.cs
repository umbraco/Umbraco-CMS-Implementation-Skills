using System.Net;
using System.Text.RegularExpressions;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-multilingual: the skill's language + dictionary
/// manifest and both partials, installed verbatim on site 2 and rendered over a two-language subtree
/// with relative /en and /da domains (seeded by the example's ExampleHostWiring.cs).
///
/// Runs on SITE 2, with no starter kit, so nothing but the skill's own partials can produce the
/// switcher and hreflang markup asserted here. The *BlankTests.cs suffix routes this file into the
/// blank test assembly.
/// </summary>
[TestFixture]
public class MultilingualBlankTests
{
    private static HttpClient Client => BlankSiteFixture.Client;

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetOkAsync(string url)
    {
        (HttpStatusCode status, string body) = await GetAsync(url);
        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), $"GET {url} should render. Body: {Excerpt(body)}");
        return body;
    }

    private static string Excerpt(string body) => body[..Math.Min(400, body.Length)];

    private static string ElementText(string body, string id)
    {
        Match match = Regex.Match(body, $"""<[a-z0-9]+ id="{id}">(.*?)</""", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, $"no element with id '{id}' in: {Excerpt(body)}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    /// <summary>hreflang → href for every &lt;link rel="alternate"&gt; the page emits.</summary>
    private static Dictionary<string, string> Alternates(string body) =>
        Regex.Matches(body, """<link rel="alternate" hreflang="([^"]+)" href="([^"]+)" />""")
            .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));

    // ---------------------------------------------------------------- routing per culture

    [Test]
    public async Task Each_domain_renders_its_own_culture()
    {
        Assert.That(ElementText(await GetOkAsync("/en/"), "title"), Is.EqualTo("Welcome"));
        Assert.That(ElementText(await GetOkAsync("/da/"), "title"), Is.EqualTo("Velkommen"));
    }

    [Test]
    public async Task Invariant_property_is_shared_across_cultures()
    {
        Assert.That(ElementText(await GetOkAsync("/en/"), "code"), Is.EqualTo("ML-001"));
        Assert.That(ElementText(await GetOkAsync("/da/"), "code"), Is.EqualTo("ML-001"));
    }

    [Test]
    public async Task Url_segments_are_per_culture()
    {
        Assert.That(ElementText(await GetOkAsync("/da/om-os/"), "title"), Is.EqualTo("Om os titel"));
        Assert.That(ElementText(await GetOkAsync("/en/about-us/"), "title"), Is.EqualTo("About us title"));

        (HttpStatusCode status, _) = await GetAsync("/da/about-us/");
        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound),
            "the English segment must not resolve under the Danish domain");
    }

    // ---------------------------------------------------------------- fallback + dictionary

    [Test]
    public async Task Blank_danish_value_falls_back_to_english()
    {
        Assert.That(ElementText(await GetOkAsync("/da/reserveside/"), "title"),
            Is.EqualTo("English fallback title"),
            "da-DK's fallback language is en-US, so Fallback.ToLanguage must render the English value");
    }

    [Test]
    public async Task Dictionary_item_renders_in_the_request_culture()
    {
        Assert.That(ElementText(await GetOkAsync("/en/"), "read-more"), Is.EqualTo("Read more"));
        Assert.That(ElementText(await GetOkAsync("/da/"), "read-more"), Is.EqualTo("Læs mere"),
            "the manifest's da-DK dictionary value must be imported and served on the Danish domain");
    }

    // ---------------------------------------------------------------- language switcher

    [Test]
    public async Task Switcher_marks_the_current_culture_and_links_the_other()
    {
        string body = await GetOkAsync("/en/about-us/");

        Assert.That(body, Does.Contain("""<span aria-current="true" lang="en-US">"""),
            "the current culture must be marked, not linked");
        Assert.That(body, Does.Contain("""<a href="/da/om-os/" hreflang="da-DK" lang="da-DK">"""),
            "the switcher must link to THIS page in Danish, at its Danish URL");
    }

    [Test]
    public async Task Switcher_is_not_rendered_for_a_page_published_in_one_culture()
    {
        string body = await GetOkAsync("/en/english-only/");

        Assert.That(body, Does.Not.Contain("language-switcher"),
            "a page with no Danish version must not offer a Danish link");
    }

    // ---------------------------------------------------------------- hreflang

    [Test]
    public async Task Hreflang_lists_every_published_culture_with_absolute_urls_and_x_default()
    {
        Dictionary<string, string> alternates = Alternates(await GetOkAsync("/da/om-os/"));

        Assert.That(alternates.Keys, Is.EquivalentTo(new[] { "en-US", "da-DK", "x-default" }));
        Assert.That(alternates["en-US"], Does.Match("^https?://[^/]+/en/about-us/$"));
        Assert.That(alternates["da-DK"], Does.Match("^https?://[^/]+/da/om-os/$"));
        Assert.That(alternates["x-default"], Is.EqualTo(alternates["en-US"]),
            "x-default must point at the default language's version");
    }

    [Test]
    public async Task Unpublished_culture_is_not_routed_or_advertised()
    {
        (HttpStatusCode status, _) = await GetAsync("/da/english-only/");
        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound),
            "a page with no Danish version must 404 on the Danish domain");

        Assert.That(Alternates(await GetOkAsync("/en/english-only/")), Is.Empty,
            "a single-culture page must not emit hreflang alternates");
    }

    // ---------------------------------------------------------------- block level variance

    /// <summary>"title|code" for each block in the given list (#blocks or #blocks-fallback), in order.</summary>
    private static List<string> Blocks(string body, string listId)
    {
        Match list = Regex.Match(body, $"""<ul id="{listId}">(.*?)</ul>""", RegexOptions.Singleline);
        Assert.That(list.Success, Is.True, $"no <ul id=\"{listId}\"> in: {Excerpt(body)}");
        return Regex.Matches(list.Groups[1].Value, """<li class="block">(.*?)</li>""", RegexOptions.Singleline)
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim())
            .ToList();
    }

    [Test]
    public async Task Invariant_block_list_renders_every_exposed_block_in_layout_order()
    {
        Assert.That(Blocks(await GetOkAsync("/en/about-us/"), "blocks"),
            Is.EqualTo(new[] { "Block A|A-1", "Block B|B-2" }),
            "both blocks are exposed in English, and the shared layout fixes their order");
    }

    [Test]
    public async Task Block_content_renders_in_the_request_culture()
    {
        Assert.That(Blocks(await GetOkAsync("/da/om-os/"), "blocks"), Has.Member("Blok A|A-1"),
            "block A's blockTitle varies by culture, so the Danish page must render the Danish title "
            + "while the invariant blockCode is shared");
    }

    [Test]
    public async Task Unexposed_block_is_omitted_without_fallback()
    {
        Assert.That(Blocks(await GetOkAsync("/da/om-os/"), "blocks"), Is.EqualTo(new[] { "Blok A|A-1" }),
            "block B is not exposed in da-DK, so a plain Value<BlockListModel>(\"blocks\") must omit it");
    }

    /// <summary>
    /// Umbraco 17.4+: with Fallback.ToLanguage, an unexposed block resolves through da-DK's fallback
    /// language (en-US in this fixture) and renders in that culture, still in layout order.
    /// </summary>
    [Test]
    public async Task Unexposed_block_falls_back_to_the_fallback_language_when_asked()
    {
        Assert.That(Blocks(await GetOkAsync("/da/om-os/"), "blocks-fallback"),
            Is.EqualTo(new[] { "Blok A|A-1", "Block B|B-2" }));
    }
}
