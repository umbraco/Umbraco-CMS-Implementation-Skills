using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of umbraco-routing Approach B. Every building block is compiled
/// from the skill's assets/ and asserted over HTTP:
///   - SkuUrlSegmentProvider rewrites product URLs, and Umbraco routes the new URL with no finder;
///   - SkuShortLinkContentFinder answers /p/{sku} with a 301 to that URL;
///   - StoreFinderController serves /stores/{city} as a virtual page whose CurrentPage is the landing node.
///
/// Runs on SITE 2 (no starter kit), where the example owns the product and landing types. The
/// *BlankTests.cs suffix routes this file into the blank test assembly.
/// </summary>
[TestFixture]
public class RoutingCustomBlankTests
{
    private const string SwibbleUrl = "/routing-products/swibble-sw-123/";

    private static HttpClient NoRedirectClient() =>
        BlankSiteFixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<(HttpStatusCode Status, string Body, Uri? Location)> GetAsync(string url)
    {
        using HttpClient client = NoRedirectClient();
        HttpResponseMessage response = await client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(), response.Headers.Location);
    }

    [Test]
    public async Task Segment_provider_puts_the_sku_in_the_product_url_and_it_routes()
    {
        var (status, body, _) = await GetAsync(SwibbleUrl);

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
        Assert.That(body, Does.Contain("<h1>Swibble</h1>"));
    }

    [Test]
    public async Task Name_only_product_url_no_longer_routes()
    {
        var (status, _, _) = await GetAsync("/routing-products/swibble/");

        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound),
            "the provider's segment must replace the name-based one, not add a second URL");
    }

    [Test]
    public async Task Product_without_a_sku_keeps_its_name_based_url()
    {
        var (status, body, _) = await GetAsync("/routing-products/gizmo/");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
        Assert.That(body, Does.Contain("<h1>Gizmo</h1>"));
    }

    [TestCase("/p/SW-123")]
    [TestCase("/p/sw-123/")]
    public async Task Short_link_redirects_permanently_to_the_canonical_product_url(string url)
    {
        var (status, _, location) = await GetAsync(url);

        Assert.That(status, Is.EqualTo(HttpStatusCode.MovedPermanently));
        Assert.That(location?.ToString(), Does.EndWith(SwibbleUrl));
    }

    [Test]
    public async Task Unknown_sku_short_link_is_a_404()
    {
        var (status, _, _) = await GetAsync("/p/NOPE-000");

        Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Virtual_route_renders_with_the_landing_node_as_current_page()
    {
        var (status, body, _) = await GetAsync("/stores/london");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
        Assert.That(body, Does.Contain("<h1>Stores in london</h1>"), "the route value must reach the view");
        Assert.That(body, Does.Contain(">Stores</a>"), "CurrentPage must be the storeFinder landing node");
    }

    [Test]
    public async Task Landing_node_still_renders_at_its_own_url()
    {
        var (status, body, _) = await GetAsync("/stores/");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
        Assert.That(body, Does.Contain("<h1>Stores</h1>"));
    }
}
