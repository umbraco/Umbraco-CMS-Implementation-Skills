using System.Net;
using System.Text.Json;
using static Umbraco_CMS.Skills.TestHost.Shared.ContentPreconditions;

namespace Umbraco_CMS.Skills.TestHost.Shared;

/// <summary>
/// Asserts the CONTENT SHAPE the skill examples navigate to, independently of the skills themselves.
///
/// The examples resolve nodes structurally — "child of the site root whose Document Type alias is X" —
/// so they only prove something while that shape actually exists. Without these checks, losing or
/// renaming a node surfaces as (for instance) CustomErrorPagesTests failing on "expected body to
/// contain 'Page not found'": true, but pointing at the skill rather than at the content. A failure
/// here names the real cause.
///
/// One concrete subclass per test assembly, because each assembly boots a different host and must only
/// assert the requirements declared for that host. Requirements are read through the Delivery API
/// rather than the database: it's a public, versioned contract, it needs no authentication, it reports
/// each node's contentType as its ALIAS, and it serves only PUBLISHED content — the same visibility the
/// skills' own lookups have.
/// </summary>
public abstract class ContentPreconditionsTestsBase
{
    /// <summary>Which host this assembly boots — one of ContentPreconditions.KnownHosts.</summary>
    protected abstract string Host { get; }

    /// <summary>That host's shared client, from the assembly's [SetUpFixture].</summary>
    protected abstract HttpClient Client { get; }

    /// <summary>Direct children of the site root, as (name, contentTypeAlias) pairs.</summary>
    private async Task<List<(string Name, string ContentType)>> GetRootChildrenAsync()
    {
        HttpResponseMessage response = await Client.GetAsync(RootChildrenQuery);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "the Delivery API must answer — it's how these preconditions are read");

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return ParseRootChildren(doc.RootElement);
    }

    [Test]
    public void Example_manifests_are_discoverable()
    {
        // Guards the discovery: if the glob broke, the requirement cases would silently become an empty
        // set and gate nothing at all.
        Assert.That(ExampleManifests(), Is.Not.Empty,
            "expected at least one plugins/**/examples/*/.generate.json — discovery is broken");
    }

    [Test]
    public void Every_manifest_targets_a_known_host()
    {
        // A typo'd host ("blanc") would match no assembly, so its requirements would never be asserted
        // anywhere and the example would look gated while being ignored.
        foreach ((string skill, string host) in DeclaredHosts())
        {
            Assert.That(KnownHosts, Has.Member(host),
                $"{skill} declares host '{host}', which no test assembly claims. Its preconditions "
                + $"would silently never run. Known hosts: {string.Join(", ", KnownHosts)}");
        }
    }

    [Test]
    public async Task Site_root_has_published_children()
    {
        List<(string Name, string ContentType)> children = await GetRootChildrenAsync();

        // Applies to every example that renders or enumerates content: a sitemap has nothing to emit
        // without these, and an empty <urlset> looks like a broken controller rather than empty input.
        Assert.That(children, Is.Not.Empty,
            $"host '{Host}' has no published children under the site root, which is what the "
            + "content-driven examples work against");
    }

    /// <summary>
    /// Shared assertion body. Each concrete fixture supplies its own [TestCaseSource] — NUnit needs a
    /// static source, and only the subclass knows which host to filter for.
    /// </summary>
    protected async Task AssertRequirementAsync(ContentRequirement requirement)
    {
        Assert.That(requirement.Value, Does.Not.StartWith("<"),
            $"{requirement.Skill}: requires '{requirement.Value}' but no such key exists in that "
            + "manifest's placeholders map");

        List<(string Name, string ContentType)> children = await GetRootChildrenAsync();
        string available = string.Join(", ", children.Select(c => $"{c.Name} [{c.ContentType}]"));

        switch (requirement.Kind)
        {
            case "documentTypeAliasAtRoot":
                // Examples navigate root -> child of type X, so the node must be a DIRECT child: one
                // nested a level deeper resolves to null with no error.
                Assert.That(children.Select(c => c.ContentType), Has.Member(requirement.Value),
                    $"{requirement.Skill}: no published DIRECT child of the site root on host "
                    + $"'{Host}' has Document Type alias '{requirement.Value}', so its example cannot "
                    + $"resolve that node. Available: {available}");
                break;

            case "nodeNameAtRoot":
                // For fixtures that assert on a node by NAME (the sitemap example checks a known page
                // appears). A renamed node would otherwise fail inside the skill's own test.
                Assert.That(children.Select(c => c.Name), Has.Member(requirement.Value),
                    $"{requirement.Skill}: no published DIRECT child of the site root on host "
                    + $"'{Host}' is named '{requirement.Value}'. Available: {available}");
                break;

            default:
                // Better to fail than to skip: an unrecognised kind means a manifest declared a
                // precondition this fixture doesn't check, which would otherwise pass vacuously.
                Assert.Fail(
                    $"{requirement.Skill}: unknown requirement kind '{requirement.Kind}'. Add a case "
                    + "for it here, or correct the manifest.");
                break;
        }
    }
}
