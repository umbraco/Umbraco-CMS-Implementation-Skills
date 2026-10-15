using System.Net;
using System.Text.Json;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-nullable-reference-types skill. The example
/// project already proves at BUILD time that the assets compile with nullable diagnostics as errors;
/// this fixture proves the guards also behave correctly against real Clean content: the
/// null-returning lookups become a 404 or a safe default, never a 500.
///
/// Nodes are addressed by key. Clean installs its content from a package with fixed keys, so they
/// are the same on every install (the test DB is a separate install from a manually booted site).
/// </summary>
[TestFixture]
public class NullableReferenceTypesTests
{
    private const string Route = "/api/nullable-example/";

    /// <summary>Clean article "Podcasts and Videos": every optional property is filled in.</summary>
    private const string FullyPopulatedArticle = "d7556247-610e-459f-a999-06cfb3176cf8";

    /// <summary>Clean article "Join the Umbraco Community on Mastodon": title and subtitle are BLANK.</summary>
    private const string ArticleWithBlankText = "e80ead52-b504-4c58-af3b-771083be8806";

    /// <summary>Clean category "Community": its Document Type has NONE of the mapped properties.</summary>
    private const string CategoryWithoutProperties = "13ea3528-c98a-4b0f-bb84-e036408a092a";

    /// <summary>Clean "Home": the site root, so it has no parent.</summary>
    private const string SiteRoot = "dcf18a51-6919-4cf8-89d1-36b94ce4d963";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static HttpClient Client => ReferenceSiteFixture.Client;

    private sealed record ViewModel(
        Guid Key,
        string? Name,
        string? Title,
        string? Subtitle,
        string? ParentName,
        string? ImageUrl,
        string? PickedContentName);

    private static async Task<ViewModel> GetOkAsync(string key)
    {
        HttpResponseMessage response = await Client.GetAsync(Route + key);
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET {Route}{key} → {body}");
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));

        ViewModel? model = JsonSerializer.Deserialize<ViewModel>(body, JsonOptions);
        Assert.That(model, Is.Not.Null, body);
        return model ?? throw new AssertionException("unreachable");
    }

    [Test]
    public async Task Known_content_returns_its_real_name_and_property_values()
    {
        ViewModel model = await GetOkAsync(FullyPopulatedArticle);

        Assert.Multiple(() =>
        {
            Assert.That(model.Key, Is.EqualTo(Guid.Parse(FullyPopulatedArticle)));
            Assert.That(model.Name, Is.EqualTo("Podcasts and Videos"));
            Assert.That(model.Title, Is.EqualTo("Podcasts and Videos"), "title is set on this node");
            Assert.That(model.Subtitle, Is.EqualTo("From the Umbraco Community"),
                "a filled-in property must come through, not the default");
            Assert.That(model.ParentName, Is.EqualTo("Blog"), "Parent() must resolve the real parent");
            Assert.That(model.ImageUrl, Does.Contain("podcast-coffee"),
                "the single media picker must convert to IPublishedContent and resolve a URL");
            Assert.That(model.PickedContentName, Is.EqualTo("Paul Seal"),
                "the single content picker must convert to IPublishedContent");
        });
    }

    [Test]
    public async Task Unknown_key_returns_404_not_500()
    {
        HttpResponseMessage response = await Client.GetAsync(Route + Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "GetById returns null for an unknown key; the controller must turn that into a 404");
    }

    [Test]
    public async Task Blank_optional_properties_fall_back_instead_of_failing()
    {
        ViewModel model = await GetOkAsync(ArticleWithBlankText);

        Assert.Multiple(() =>
        {
            Assert.That(model.Name, Is.EqualTo("Join the Umbraco Community on Mastodon"));
            Assert.That(model.Title, Is.EqualTo(model.Name), "a blank title must fall back to the node name");
            Assert.That(model.Subtitle, Is.EqualTo(string.Empty), "a blank subtitle must be \"\", never null");
            Assert.That(model.ParentName, Is.EqualTo("Blog"));
        });
    }

    [Test]
    public async Task Properties_missing_from_the_document_type_fall_back_instead_of_failing()
    {
        ViewModel model = await GetOkAsync(CategoryWithoutProperties);

        Assert.Multiple(() =>
        {
            Assert.That(model.Name, Is.EqualTo("Community"));
            Assert.That(model.Title, Is.EqualTo("Community"), "no 'title' property at all → node name");
            Assert.That(model.Subtitle, Is.EqualTo(string.Empty), "no 'subtitle' property at all → \"\"");
            Assert.That(model.ImageUrl, Is.Null, "no media property → null, not an empty or broken URL");
            Assert.That(model.PickedContentName, Is.Null, "no picker property → null");
        });
    }

    [Test]
    public async Task Root_content_has_a_null_parent_instead_of_failing()
    {
        ViewModel model = await GetOkAsync(SiteRoot);

        Assert.Multiple(() =>
        {
            Assert.That(model.Name, Is.EqualTo("Home"));
            Assert.That(model.ParentName, Is.Null, "Parent() is null at the root");
        });
    }
}
