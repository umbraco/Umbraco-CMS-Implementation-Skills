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

// SPLIT sitemap for sites AT or OVER the 50,000 URL / 50 MB limit.
// Serves a <sitemapindex> at /sitemap.xml and paged <urlset> files at /sitemap-1.xml,
// /sitemap-2.xml, … For smaller sites use SitemapController.cs instead. Register only ONE.
//
// Output follows the sitemaps.org protocol and Google Search Central guidance:
//   - <loc> URLs are ABSOLUTE (Url(mode: UrlMode.Absolute)); relative paths are not allowed.
//   - Served as application/xml; charset=utf-8.
//   - Only <loc> + <lastmod> are emitted. Google ignores <priority> and <changefreq>.
//   - Each page holds at most PageSize URLs to stay under the 50,000 URL / 50 MB per-file limit.
//   Refs: https://www.sitemaps.org/protocol.html
//         https://developers.google.com/search/docs/crawling-indexing/sitemaps/large-sitemaps
public class SitemapIndexController : ControllerBase
{
    // URLs per page. The protocol limit is 50,000; 10,000 keeps each file comfortably under 50 MB.
    private const int PageSize = 10000;
    private const string CacheKey = "SitemapUrls";

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly IUmbracoContextFactory _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly IDocumentNavigationQueryService _documentNavigationQueryService;
    private readonly IPublicAccessService _publicAccessService;

    public SitemapIndexController(
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

    // Index: lists one <sitemap> entry per page.
    [HttpGet("sitemap.xml")]
    public IActionResult Index()
    {
        var urls = GetUrls();
        if (urls.Count == 0)
        {
            return NotFound();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var pageCount = (int)Math.Ceiling(urls.Count / (double)PageSize);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(Ns + "sitemapindex",
                Enumerable.Range(1, pageCount).Select(page =>
                    new XElement(Ns + "sitemap",
                        new XElement(Ns + "loc", $"{baseUrl}/sitemap-{page}.xml")))));

        return XmlContent(doc);
    }

    // Page: one <urlset> of up to PageSize URLs.
    [HttpGet("sitemap-{page:int}.xml")]
    public IActionResult Page(int page)
    {
        var urls = GetUrls();
        var pageCount = (int)Math.Ceiling(urls.Count / (double)PageSize);
        if (page < 1 || page > pageCount)
        {
            return NotFound();
        }

        var pageUrls = urls.Skip((page - 1) * PageSize).Take(PageSize);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(Ns + "urlset",
                pageUrls.Select(u => new XElement(Ns + "url",
                    new XElement(Ns + "loc", u.Loc),
                    new XElement(Ns + "lastmod", u.LastMod)))));

        return XmlContent(doc);
    }

    private sealed record SitemapUrl(string Loc, string LastMod);

    // Build the full URL list once and cache it; SitemapCacheInvalidator clears it on content changes.
    private List<SitemapUrl> GetUrls()
    {
        return _cache.GetOrCreate(CacheKey, _ =>
        {
            using var ctx = _contextFactory.EnsureUmbracoContext();

            if (!_documentNavigationQueryService.TryGetRootKeys(out var rootKeys))
            {
                return new List<SitemapUrl>();
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
                return new List<SitemapUrl>();
            }

            return rootPage
                .DescendantsOrSelf()
                .Where(x => !_publicAccessService.IsProtected(x.Path))
                // FILTER: remove this .Where() entirely if no filter property is needed
                .Where(x => !x.HasProperty("<filterAlias>") || !x.Value<bool>("<filterAlias>"))
                .Select(x => new SitemapUrl(
                    x.Url(mode: UrlMode.Absolute),
                    x.UpdateDate.ToString("yyyy-MM-dd")))
                .ToList();
        })!;
    }

    private ContentResult XmlContent(XDocument doc)
    {
        var sw = new System.IO.StringWriter();
        doc.Save(sw);
        // application/xml; charset=utf-8 — required content type/encoding for sitemaps.
        return Content(sw.ToString(), "application/xml", Encoding.UTF8);
    }
}
