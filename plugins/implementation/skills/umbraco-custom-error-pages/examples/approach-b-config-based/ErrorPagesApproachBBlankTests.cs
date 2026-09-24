using System.Net;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-custom-error-pages APPROACH B — the config-based 404.
///
/// Proves the three parts actually work together: the ErrorPage404 Document Type and template the skill
/// ships, a published node of that type, and the Error404Collection entry naming that node's GUID.
/// Umbraco's own ContentFinderByConfigured404 does the resolving, so what's under test is the skill's
/// guidance rather than any code it wrote.
///
/// Runs on SITE 2 out of necessity, not preference. Approach A's SetContentLastChanceFinder is
/// AddUnique&lt;IContentLastChanceFinder, T&gt;(), which REPLACES ContentFinderByConfigured404 — so on the
/// Clean host, where Approach A is loaded, this approach has no implementation left to test. Clean also
/// ships its own `error` type and view, which would let these assertions pass on Clean's implementation
/// while the skill's guidance was broken.
/// </summary>
[TestFixture]
public class ErrorPagesApproachBBlankTests
{
    private static HttpClient Client => BlankSiteFixture.Client;

    /// <summary>A URL that matches no content, which is the only way to reach the 404 finder.</summary>
    private const string MissingUrl = "/no-such-page-exists-here/";
    private const string PageTitle = "Page not found (Approach B)";

    [Test]
    public async Task Unknown_url_returns_404()
    {
        HttpResponseMessage response = await Client.GetAsync(MissingUrl);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "a URL matching no content must answer 404, not 200 and not a redirect");
    }

    /// <summary>
    /// The status code alone is worthless here: Umbraco answers 404 for an unmatched URL whether or not
    /// the configured error page was found. Only the rendered body distinguishes "the skill's 404 page
    /// was served" from "Umbraco's built-in not-found page was served", which is the actual claim.
    /// </summary>
    [Test]
    public async Task Unknown_url_renders_the_configured_error_page()
    {
        HttpResponseMessage response = await Client.GetAsync(MissingUrl);
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain(PageTitle),
            "expected the ErrorPage404 node's own content, proving Error404Collection resolved the node "
            + $"by GUID and its template rendered. Got: {body[..Math.Min(300, body.Length)]}");
    }

    /// <summary>
    /// The template sets the status code itself. Without that, a content-rendered error page returns 200
    /// with error markup — which looks fine to a human and is wrong for every crawler.
    /// </summary>
    [Test]
    public async Task Error_page_is_not_indexable_and_is_html()
    {
        HttpResponseMessage response = await Client.GetAsync(MissingUrl);
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
        Assert.That(body, Does.Contain("noindex"),
            "a 404 page must not invite indexing");
    }

    /// <summary>
    /// Guards against the fix that breaks everything else: a last-chance finder that answers for URLs
    /// which DO match content would replace the whole site with the error page.
    /// </summary>
    [Test]
    public async Task Existing_content_is_unaffected()
    {
        HttpResponseMessage response = await Client.GetAsync("/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(),
            Does.Not.Contain(PageTitle),
            "the 404 finder must only run when nothing else matched");
    }
}
