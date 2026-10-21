using System.Net;
using System.Text.RegularExpressions;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Runtime validation of the umbraco-security-headers skill (Approach A). The skill's composer is
/// compiled from its assets/ and loaded into the Clean reference instance; these tests prove the
/// middleware sends a separate CSP to the backoffice and the website, that the backoffice policy
/// carries the same nonce Umbraco puts on its backoffice import map, and that the headers reach
/// responses Umbraco serves before routing (static files) and error responses.
/// </summary>
[TestFixture]
public class SecurityHeadersTests
{
    private const string Csp = "Content-Security-Policy";

    private static readonly Regex NonceInHeader = new("'nonce-([^']+)'", RegexOptions.Compiled);

    private static HttpClient Client => ReferenceSiteFixture.Client;

    [Test]
    public async Task Website_page_gets_the_site_policy_and_the_other_security_headers()
    {
        HttpResponseMessage response = await Client.GetAsync("/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string policy = SingleHeader(response, Csp);
        Assert.Multiple(() =>
        {
            Assert.That(ScriptSrc(policy), Does.Contain("'nonce-"), "site script-src should allow nonce-tagged inline scripts");
            Assert.That(Directive(policy, "style-src"), Does.Contain("'nonce-"), "site style-src should allow nonce-tagged inline styles");
            Assert.That(policy, Does.Not.Contain("'unsafe-inline'"), "the site must not inherit the backoffice's 'unsafe-inline'");
            Assert.That(policy, Does.Not.Contain("news-dashboard.umbraco.com"), "backoffice-only sources leaked into the site policy");
            Assert.That(SingleHeader(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
            Assert.That(SingleHeader(response, "X-Frame-Options"), Is.EqualTo("SAMEORIGIN"));
            Assert.That(SingleHeader(response, "Referrer-Policy"), Is.EqualTo("strict-origin-when-cross-origin"));
            Assert.That(response.Headers.Contains("X-XSS-Protection"), Is.False);
        });
    }

    /// <summary>
    /// The failure this skill exists to prevent: a CSP whose nonce doesn't match the one Umbraco
    /// renders on the backoffice import map blanks the backoffice.
    /// </summary>
    [Test]
    public async Task Backoffice_gets_its_own_policy_with_the_nonce_Umbraco_renders()
    {
        HttpResponseMessage response = await Client.GetAsync("/umbraco");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string policy = SingleHeader(response, Csp);
        Assert.That(policy, Does.Contain("https://news-dashboard.umbraco.com"), "expected the backoffice policy under /umbraco");

        Match nonce = NonceInHeader.Match(ScriptSrc(policy));
        Assert.That(nonce.Success, Is.True, $"no nonce in script-src: {policy}");

        string html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain($"<script type=\"importmap\" nonce=\"{nonce.Groups[1].Value}\""),
            "the import map must carry the nonce from this response's CSP header");
    }

    /// <summary>
    /// The website half of the same contract: an inline script tagged with asp-csp-nonce in the
    /// site's own (precompiled) view gets the nonce from the site policy. The view is the
    /// example's harness (ExampleHostWiring.cs), not a skill asset.
    /// </summary>
    [Test]
    public async Task Site_inline_script_tagged_with_asp_csp_nonce_gets_the_header_nonce()
    {
        HttpResponseMessage response = await Client.GetAsync("/example/security-headers/nonce-probe");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string policy = SingleHeader(response, Csp);
        Assert.That(policy, Does.Not.Contain("news-dashboard.umbraco.com"), "expected the site policy, not the backoffice one");
        Match nonce = NonceInHeader.Match(ScriptSrc(policy));
        Assert.That(nonce.Success, Is.True, $"no nonce in script-src: {policy}");

        // Razor HTML-encodes '+' in the attribute; the browser decodes it before comparing.
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.That(html, Does.Contain($"<script nonce=\"{nonce.Groups[1].Value}\">window.securityHeadersProbe"),
            "asp-csp-nonce must render the nonce from this response's CSP header (and must not render literally)");
    }

    [Test]
    public async Task Each_request_gets_a_fresh_nonce()
    {
        string first = NonceInHeader.Match(SingleHeader(await Client.GetAsync("/umbraco"), Csp)).Groups[1].Value;
        string second = NonceInHeader.Match(SingleHeader(await Client.GetAsync("/umbraco"), Csp)).Groups[1].Value;

        Assert.That(first, Is.Not.Empty);
        Assert.That(second, Is.Not.EqualTo(first));
    }

    /// <summary>
    /// Static files are served before routing and short-circuit the pipeline, so this only passes
    /// if the middleware sits in PrePipeline (or before app.UseUmbraco()).
    /// </summary>
    [Test]
    public async Task Static_file_gets_the_headers()
    {
        HttpResponseMessage response = await Client.GetAsync("/favicon.ico");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(SingleHeader(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
    }

    [Test]
    public async Task Not_found_response_gets_the_site_policy()
    {
        HttpResponseMessage response = await Client.GetAsync("/security-headers-test-missing-page");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(SingleHeader(response, Csp), Does.Not.Contain("'unsafe-inline'"));
    }

    private static string SingleHeader(HttpResponseMessage response, string name)
    {
        Assert.That(response.Headers.TryGetValues(name, out IEnumerable<string>? values), Is.True, $"{name} header missing");
        List<string> list = values!.ToList();
        Assert.That(list, Has.Count.EqualTo(1), $"expected exactly one {name} header, got: {string.Join(" | ", list)}");
        return list[0];
    }

    private static string ScriptSrc(string policy) => Directive(policy, "script-src");

    private static string Directive(string policy, string name) =>
        policy.Split(';').Select(d => d.Trim()).FirstOrDefault(d => d.StartsWith(name + " ")) ?? string.Empty;
}
