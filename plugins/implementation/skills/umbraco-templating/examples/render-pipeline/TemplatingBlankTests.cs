using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Deterministic runtime validation of the umbraco-templating skill. Every building block the skill
/// ships is compiled from its assets/ and rendered through one page:
///   - ProductPageController hijacks the productPage route (async Index, query-string binding,
///     custom view model built on PublishedContentWrapped) and renders productPage.cshtml;
///   - the template invokes RelatedPagesViewComponent and renders the contactForm partial;
///   - the partial posts to ContactFormController through BeginUmbracoForm.
///
/// Runs on SITE 2 (no starter kit), where the example owns the productPage type outright. The
/// *BlankTests.cs suffix routes this file into the blank test assembly.
/// </summary>
[TestFixture]
public class TemplatingBlankTests
{
    private const string ProductUrl = "/product-page/";
    private const string RelatedUrl = "/related-product/";

    private static HttpClient Client => BlankSiteFixture.Client;

    private static async Task<string> GetOkAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET {url}:\n{body}");
        return body;
    }

    private static string RelatedNav(string body)
    {
        Match nav = Regex.Match(body, "<nav class=\"related-pages\".*?</nav>", RegexOptions.Singleline);
        Assert.That(nav.Success, Is.True, "expected the RelatedPages view component's <nav> in the page");
        return nav.Value;
    }

    private static string HiddenInput(string body, string name)
    {
        Match input = Regex.Match(body, $"<input[^>]*name=\"{name}\"[^>]*value=\"([^\"]*)\"");
        Assert.That(input.Success, Is.True, $"expected a hidden '{name}' input in the rendered form");
        return WebUtility.HtmlDecode(input.Groups[1].Value);
    }

    [Test]
    public async Task Hijacked_route_renders_the_template_with_the_controllers_view_model()
    {
        string body = await GetOkAsync(Client, ProductUrl);

        Assert.That(body, Does.Contain("<h1>Product page</h1>"), "wrapped content's Name must render");
        Assert.That(body, Does.Contain("Built to last"), "the page's own tagline value must render");
        Assert.That(body, Does.Contain("In stock"),
            "StockStatus is set only by ProductPageController — missing means the route was not hijacked");
        Assert.That(body, Does.Contain("Page 1"), "PageNumber defaults to 1");
    }

    [Test]
    public async Task Query_string_binds_to_the_hijacked_action()
    {
        string body = await GetOkAsync(Client, ProductUrl + "?page=3");

        Assert.That(body, Does.Contain("Page 3"));
    }

    [Test]
    public async Task Missing_property_value_falls_back_to_the_default()
    {
        string body = await GetOkAsync(Client, RelatedUrl);

        Assert.That(body, Does.Contain("No tagline yet"));
    }

    [Test]
    public async Task View_component_lists_same_type_siblings_but_not_the_current_page()
    {
        string nav = RelatedNav(await GetOkAsync(Client, ProductUrl));

        Assert.That(nav, Does.Contain(">Related product</a>"));
        Assert.That(nav, Does.Not.Contain(">Product page</a>"), "Siblings() must exclude the page itself");
    }

    [Test]
    public async Task Invalid_form_post_rerenders_the_page_with_validation_errors()
    {
        using HttpClient client = BlankSiteFixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        string page = await GetOkAsync(client, ProductUrl);

        HttpResponseMessage response = await client.PostAsync(ProductUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HiddenInput(page, "__RequestVerificationToken"),
                ["ufprt"] = HiddenInput(page, "ufprt"),
                ["Name"] = "Ada",
                ["Email"] = "not-an-email",
                ["Message"] = "",
            }));
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "CurrentUmbracoPage() re-renders in place; a redirect would lose ModelState");
        Assert.That(body, Does.Contain("<h1>Product page</h1>"), "the Umbraco page itself must re-render");
        Assert.That(body, Does.Contain("field-validation-error"), "validation messages must render");
        Assert.That(body, Does.Contain("value=\"Ada\""), "the visitor's input must be kept");
    }

    [Test]
    public async Task Valid_form_post_redirects_and_shows_the_success_message_once()
    {
        using HttpClient client = BlankSiteFixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        string page = await GetOkAsync(client, ProductUrl);

        HttpResponseMessage post = await client.PostAsync(ProductUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HiddenInput(page, "__RequestVerificationToken"),
                ["ufprt"] = HiddenInput(page, "ufprt"),
                ["Name"] = "Ada",
                ["Email"] = "ada@example.com",
                ["Message"] = "Hello",
            }));

        Assert.That(post.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), "Post/Redirect/Get");
        Assert.That(post.Headers.Location?.ToString(), Does.EndWith(ProductUrl));

        Assert.That(await GetOkAsync(client, ProductUrl), Does.Contain("your message has been sent"),
            "TempData must carry the success flag across the redirect");
        Assert.That(await GetOkAsync(client, ProductUrl), Does.Not.Contain("your message has been sent"),
            "TempData is read once — a refresh shows the form again");
    }
}
