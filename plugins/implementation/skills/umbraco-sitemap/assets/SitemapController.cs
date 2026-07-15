using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace <Namespace>.Controllers;

// SINGLE-FILE sitemap for sites UNDER the 50,000 URL / 50 MB limit.
// If the site has (or will grow past) 50,000 URLs, use SitemapIndexController.cs instead — it
// splits URLs across paged <urlset> files behind a <sitemapindex>. Register only ONE of the two.
//
// Sitemap output follows the sitemaps.org protocol and Google Search Central guidance:
//   - <loc> URLs are ABSOLUTE (Url(mode: UrlMode.Absolute)); relative paths are not allowed.
//   - Served as application/xml; charset=utf-8.
//   - Only <loc> + <lastmod> are emitted. Google ignores <priority> and <changefreq>, so we
//     don't produce them; <lastmod> is derived automatically from the node's UpdateDate.
//   Refs: https://www.sitemaps.org/protocol.html
//         https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap
[Route("sitemap.xml")]
public class SitemapController : ControllerBase
{
    private readonly IUmbracoContextFactory _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly IDocumentNavigationQueryService _documentNavigationQueryService;
    private readonly IPublicAccessService _publicAccessService;

    public SitemapController(
        IUmbracoContextFactory contextFactory,
        IMemoryCache cache,
        IDocumentNavigationQueryService documentNavigationQueryService,
        IPublicAccessService publicAccessService)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _documentNavigationQueryService = documentNavigationQueryService;
        _publicAccessService = publicAccessService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        const string cacheKey = "SitemapXml";
        if (_cache.TryGetValue(cacheKey, out string? cachedXml))
        {
            // application/xml; charset=utf-8 — required content type/encoding for sitemaps.
            return Content(cachedXml!, "application/xml", Encoding.UTF8);
        }

        using var ctx = _contextFactory.EnsureUmbracoContext();

        if (!_documentNavigationQueryService.TryGetRootKeys(out var rootKeys))
        {
            return NotFound();
        }

        var rootPages = rootKeys
            .Select(k => ctx.UmbracoContext.Content?.GetById(k))
            .Where(p => p != null);

        // Multi-site: match the request host to the correct root node.
        // Falls back to the first root on localhost / dev environments.
        var requestHost = Request.Host.Host;
        var rootPage = rootPages.FirstOrDefault(p =>
            new Uri(p!.Url(mode: UrlMode.Absolute)).Host
                .Equals(requestHost, StringComparison.OrdinalIgnoreCase))
            ?? rootPages.FirstOrDefault();

        if (rootPage == null)
        {
            return NotFound();
        }

        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        var nodes = rootPage
            .DescendantsOrSelf()
            .Where(x => !_publicAccessService.IsProtected(x.Path))
            // FILTER: remove this .Where() entirely if no filter property is needed
            .Where(x => !x.HasProperty("<filterAlias>") || !x.Value<bool>("<filterAlias>"))
            // Only <loc> (absolute) and <lastmod> — Google ignores <priority>/<changefreq>.
            .Select(x => new XElement(ns + "url",
                new XElement(ns + "loc", x.Url(mode: UrlMode.Absolute)),
                new XElement(ns + "lastmod", x.UpdateDate.ToString("yyyy-MM-dd"))))
            .ToList();

        var xmlDoc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", nodes));

        var sw = new System.IO.StringWriter();
        xmlDoc.Save(sw);
        var xmlString = sw.ToString();

        _cache.Set(cacheKey, xmlString);
        return Content(xmlString, "application/xml", Encoding.UTF8);
    }
}
