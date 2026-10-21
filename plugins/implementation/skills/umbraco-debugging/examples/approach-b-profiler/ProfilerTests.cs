using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of umbraco-debugging's Approach B controller
/// (assets/ProfiledController.cs), compiled from the skill's asset and loaded into the Clean host.
///
/// Proves the nested IProfiler.Step calls don't get in the way of the work they wrap: the route
/// answers, and the counts it reports come from the real content tree rather than an empty or
/// short-circuited loop.
/// </summary>
[TestFixture]
public class ProfilerTests
{
    private static readonly Regex Body = new(@"^Roots: (\d+), descendants: (\d+)$");

    private static HttpClient Client => ReferenceSiteFixture.Client;

    private static IServiceProvider Services => ReferenceSiteFixture.Factory.Services;

    [Test]
    public async Task Get_profiled_returns_ok_and_plain_text()
    {
        HttpResponseMessage response = await Client.GetAsync("/debugging-example/profiled");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
    }

    [Test]
    public async Task Get_profiled_reports_the_content_tree_counts()
    {
        HttpResponseMessage response = await Client.GetAsync("/debugging-example/profiled");
        string body = await response.Content.ReadAsStringAsync();

        Match match = Body.Match(body);
        Assert.That(match.Success, Is.True, $"body should read 'Roots: N, descendants: M'. Got: {body}");

        int roots = int.Parse(match.Groups[1].Value);
        int descendants = int.Parse(match.Groups[2].Value);

        Assert.That(roots, Is.GreaterThan(0), "Clean installs root content, so the root count must be > 0");
        Assert.That(descendants, Is.GreaterThan(0), "Clean's Home has children, so descendants must be > 0");

        // Cross-check against a different service than the one the controller reads, so a loop that
        // counts the wrong thing can't agree with itself.
        int expectedRoots = Services.GetRequiredService<IContentService>().GetRootContent().Count();
        Assert.That(roots, Is.EqualTo(expectedRoots), "root count should match IContentService.GetRootContent()");
    }
}
