using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-routing Approach A: the reserved routing property
/// aliases, installed through the skill's routingProperties composition with the property editors it
/// specifies, change routing with no code at all. Each page's template prints the rendered node's name,
/// so every assertion checks WHICH node answered, not just that something did.
///
/// Runs on SITE 2 (no starter kit). The *BlankTests.cs suffix routes this file into the blank test
/// assembly.
/// </summary>
[TestFixture]
public class RoutingBuiltInBlankTests
{
    private static HttpClient NoRedirectClient() =>
        BlankSiteFixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> GetOkAsync(string url)
    {
        using HttpClient client = NoRedirectClient();
        HttpResponseMessage response = await client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET {url}:\n{body}");
        return body;
    }

    [Test]
    public async Task UmbracoUrlName_replaces_the_name_based_segment()
    {
        Assert.That(await GetOkAsync("/sale/"), Does.Contain(">Spring Sale</h1>"));

        using HttpClient client = NoRedirectClient();
        HttpResponseMessage nameBased = await client.GetAsync("/spring-sale/");
        Assert.That(nameBased.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "the node's name must no longer route once umbracoUrlName is set");
    }

    [TestCase("/get-in-touch/")]
    [TestCase("/support/contact/")]
    [TestCase("/routing-contact/")]
    public async Task UmbracoUrlAlias_serves_the_page_at_each_alias_and_at_its_own_url(string url)
    {
        Assert.That(await GetOkAsync(url), Does.Contain(">Routing Contact</h1>"));
    }

    [Test]
    public async Task UmbracoRedirect_with_a_content_picker_answers_302_to_the_picked_page()
    {
        using HttpClient client = NoRedirectClient();
        HttpResponseMessage response = await client.GetAsync("/old-offers/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.ToString(), Does.EndWith("/sale/"),
            "the redirect must target the picked page's real URL, umbracoUrlName included");
    }

    [Test]
    public async Task UmbracoInternalRedirectId_renders_the_picked_page_without_redirecting()
    {
        // GetOkAsync uses a non-redirecting client, so a 200 here is a rewrite, not a followed redirect.
        string body = await GetOkAsync("/routing-help/");

        Assert.That(body, Does.Contain(">Routing Contact</h1>"));
        Assert.That(body, Does.Not.Contain(">Routing Help</h1>"));
    }
}
