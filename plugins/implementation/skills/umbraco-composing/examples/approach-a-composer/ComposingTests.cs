using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Skills.Examples.Composing;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-composing skill, Approach A: the skill's
/// FeatureComposer / FeatureBuilderExtensions / FeatureComponent assets, compiled with
/// &lt;Feature&gt; = ComposingExample and loaded into the reference instance.
///
/// A composer has no output of its own, so these assert what it composed: the composer was
/// type-scanned and ran, each registration landed exactly once, the content finder sits where the
/// template pins it, and that finder is live in the real request pipeline.
/// </summary>
[TestFixture]
public class ComposingTests
{
    private static IServiceProvider Services => ReferenceSiteFixture.Factory.Services;

    [Test]
    public void Content_finder_is_inserted_immediately_before_the_core_url_finder()
    {
        List<Type> finders = Services.GetRequiredService<ContentFinderCollection>()
            .Select(finder => finder.GetType())
            .ToList();

        int ours = finders.IndexOf(typeof(ComposingExampleContentFinder));
        int core = finders.IndexOf(typeof(ContentFinderByUrlNew));

        Assert.That(ours, Is.GreaterThanOrEqualTo(0), "the composer did not register the content finder");
        Assert.That(core, Is.GreaterThanOrEqualTo(0),
            "ContentFinderByUrlNew is no longer registered: the template's anchor is stale for this Umbraco version");
        Assert.That(ours, Is.EqualTo(core - 1),
            "InsertBefore<ContentFinderByUrlNew, T> must place the finder directly before the core URL finder");
        Assert.That(finders.Count(type => type == typeof(ComposingExampleContentFinder)), Is.EqualTo(1));
    }

    [Test]
    public async Task Content_finder_is_live_in_the_request_pipeline()
    {
        HttpClient client = ReferenceSiteFixture.Client;

        HttpResponseMessage home = await client.GetAsync("/");
        HttpResponseMessage probe = await client.GetAsync(ComposingExampleContentFinder.ProbePath);
        string probeBody = await probe.Content.ReadAsStringAsync();

        Assert.That(probe.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "no core finder matches the probe URL, so a 200 means the composed finder handled it");

        string? homeTitle = Title(await home.Content.ReadAsStringAsync());
        Assert.That(homeTitle, Is.Not.Null.And.Not.Empty, "the home page should render a <title>");
        Assert.That(Title(probeBody), Is.EqualTo(homeTitle),
            "the finder routes the probe URL to the site root, so it should render the home page");
    }

    [Test]
    public void Notification_handler_is_registered_exactly_once()
    {
        // Async handlers are not de-duplicated by Umbraco, so a second Add<Feature>() call (composer
        // AND Program.cs) would show up here as a count of 2.
        int count = Services.GetServices<INotificationAsyncHandler<ContentPublishedNotification>>()
            .Count(handler => handler is ComposingExamplePublishedHandler);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Component_is_registered_once_and_gated_to_the_run_level()
    {
        List<IAsyncComponent> components = Services.GetRequiredService<ComponentCollection>()
            .Where(component => component is ComposingExampleComponent)
            .ToList();

        Assert.That(components, Has.Count.EqualTo(1), "the composer did not append the component exactly once");
        Assert.That(components[0], Is.InstanceOf<RuntimeAsyncComponentBase>(),
            "the template's component must use RuntimeAsyncComponentBase so it skips install/upgrade");
    }

    private static string? Title(string html)
    {
        Match match = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
