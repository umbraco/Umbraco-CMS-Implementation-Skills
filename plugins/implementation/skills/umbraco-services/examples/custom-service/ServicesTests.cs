using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Skills.Examples.Services.Services;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Deterministic runtime validation of the umbraco-services skill. Each test proves one of the three
/// patterns the skill hands out, compiled from its assets/ and loaded into the Clean reference host:
///
///   - a SCOPED custom service consumed by a controller (request path),
///   - a SINGLETON reader that opens its own UmbracoContext (no-request path),
///   - a ContentUnpublishingNotification handler that still sees the page in the published cache.
///
/// The expected root name is read through IContentService (the database), a different path from
/// the published cache the assets use, so the assertions can't pass by comparing a value with itself.
/// </summary>
[TestFixture]
public class ServicesTests
{
    /// <summary>
    /// A Clean node no other fixture asserts on or mutates. Not "About" (the sitemap invalidation
    /// test unpublishes it) and not "Features" (the sitemap tests expect it to be listed).
    /// </summary>
    private const string UnpublishNodeName = "Contact";

    private static HttpClient Client => ReferenceSiteFixture.Client;

    private static T Resolve<T>() where T : notnull =>
        ReferenceSiteFixture.Factory.Services.GetRequiredService<T>();

    private static string ExpectedRootName()
    {
        IContent root = Resolve<IContentService>().GetRootContent().First(c => c.Published);
        return root.Name ?? throw new InvalidOperationException("Clean's published root has no name");
    }

    [Test]
    public async Task Scoped_custom_service_serves_the_site_root_through_a_controller()
    {
        HttpResponseMessage response = await Client.GetAsync("/umbraco-services-example/site-name");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "SiteServiceController did not answer — check that SiteServiceComposer registered ISiteService");
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(ExpectedRootName()));
    }

    [Test]
    public void Singleton_reader_reads_published_content_outside_any_request()
    {
        // Resolved straight from the root provider, with no HttpContext and no UmbracoContext: the
        // situation a hosted service or scheduled job is in. Only EnsureUmbracoContext() makes this work.
        string? name = Resolve<RootContentReader>().GetSiteRootName();

        Assert.That(name, Is.EqualTo(ExpectedRootName()));
    }

    [Test]
    public void Singleton_reader_reads_through_the_injected_published_cache_outside_any_request()
    {
        // Option B: IPublishedContentCache is a singleton (Umbraco 15+), so no UmbracoContext is opened.
        string? name = Resolve<RootContentReader>().GetSiteRootNameFromCache();

        Assert.That(name, Is.EqualTo(ExpectedRootName()));
    }

    /// <summary>
    /// Guards the skill's claim that option B (injected cache, no context) can read content but can't build
    /// URLs: IPublishedUrlProvider needs an UmbracoContext. If a future Umbraco lifts this, the reference
    /// should drop the "URLs need option A" rule, and this test will say so.
    /// </summary>
    [Test]
    public void Url_generation_outside_a_request_needs_an_open_context()
    {
        IPublishedContentCache cache = Resolve<IPublishedContentCache>();
        IPublishedUrlProvider urls = Resolve<IPublishedUrlProvider>();
        Resolve<IDocumentNavigationQueryService>().TryGetRootKeys(out IEnumerable<Guid> keys);
        IPublishedContent root = keys.Select(k => cache.GetById(k)).First(c => c is not null)!;

        Assert.That(() => urls.GetUrl(root), Throws.InvalidOperationException,
            "without a context GetUrl should throw");

        using UmbracoContextReference reference = Resolve<IUmbracoContextFactory>().EnsureUmbracoContext();
        Assert.That(urls.GetUrl(root), Is.Not.Empty, "with a context the same call should return a URL");
    }

    [Test]
    public void Unpublishing_handler_records_the_url_while_the_page_is_still_cached()
    {
        IContentService contentService = Resolve<IContentService>();
        UnpublishedUrlStore store = Resolve<UnpublishedUrlStore>();

        IContent node = contentService.GetRootContent()
            .SelectMany(root => contentService.GetPagedDescendants(root.Id, 0, 100, out _))
            .First(c => c.Name == UnpublishNodeName && c.Published);

        try
        {
            contentService.Unpublish(node);

            // The handler runs synchronously inside Unpublish, before the cache is updated. If it had
            // been wired to ContentUnpublishedNotification instead, GetById would find nothing and no
            // URL would be recorded.
            Assert.That(store.TryGetUrl(node.Key, out string? url), Is.True,
                "no URL recorded — the handler didn't run, or the page had already left the cache");
            Assert.That(url, Does.Contain($"/{UnpublishNodeName.ToLowerInvariant()}"));
        }
        finally
        {
            contentService.Publish(node, ["*"]);
        }
    }
}
