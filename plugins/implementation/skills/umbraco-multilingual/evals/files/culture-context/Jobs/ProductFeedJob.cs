using System.Text;
using Examine;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.BackgroundJobs;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Acme.Web.Jobs;

/// <summary>
/// Nightly job that writes one product feed per culture (feeds/products-en-US.txt and
/// feeds/products-de-DE.txt) for our partner network. It was ported from Hangfire to Umbraco's own
/// scheduler, so the team still calls it "the Hangfire job". It runs once every 24 hours on the
/// scheduling server.
///
/// Known problems:
/// - products-de-DE.txt is empty most nights, although the same products render in German at /de/.
/// - On the nights it did have rows (before the published check was added) the German descriptions
///   and names were the English text.
/// The English feed has always been fine.
/// </summary>
public class ProductFeedJob : IRecurringBackgroundJob
{
    // The cultures we write a feed for.
    private static readonly string[] FeedCultures = new[] { "en-US", "de-DE" };

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IPublishedContentCache _contentCache;
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IExamineManager _examineManager;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ILogger<ProductFeedJob> _logger;

    public ProductFeedJob(
        IUmbracoContextFactory umbracoContextFactory,
        IPublishedContentCache contentCache,
        IDocumentNavigationQueryService navigation,
        IExamineManager examineManager,
        IWebHostEnvironment hostEnvironment,
        ILogger<ProductFeedJob> logger)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _contentCache = contentCache;
        _navigation = navigation;
        _examineManager = examineManager;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    // Nightly.
    public TimeSpan Period => TimeSpan.FromHours(24);

    public TimeSpan Delay => TimeSpan.FromMinutes(5);

    public ServerRole[] ServerRoles => [ServerRole.Single, ServerRole.SchedulingPublisher];

    // The period never changes at runtime.
    public event EventHandler PeriodChanged { add { } remove { } }

    public async Task RunJobAsync()
    {
        // Running outside a request, so make sure an UmbracoContext exists before touching the cache.
        using UmbracoContextReference _ = _umbracoContextFactory.EnsureUmbracoContext();

        if (!_navigation.TryGetRootKeys(out IEnumerable<Guid> rootKeys))
        {
            _logger.LogWarning("No root content found; no product feeds were written.");
            return;
        }

        IPublishedContent? productsRoot = rootKeys
            .Select(key => _contentCache.GetById(key))
            .Where(root => root is not null)
            .SelectMany(root => root!.Children())
            .FirstOrDefault(node => node.ContentType.Alias == "productList");

        if (productsRoot is null)
        {
            _logger.LogWarning("No productList node found; no product feeds were written.");
            return;
        }

        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            _logger.LogWarning("The external index is not available; no product feeds were written.");
            return;
        }

        string feedsDirectory = Path.Combine(_hostEnvironment.ContentRootPath, "feeds");
        Directory.CreateDirectory(feedsDirectory);

        foreach (string culture in FeedCultures)
        {
            // Only products that are published in this culture belong in its feed. The external index
            // keeps a per-culture published flag, so ask it instead of walking every product. For
            // en-US this returns every product; for de-DE it almost always returns nothing, even
            // though the products are published in German and show up at /de/.
            ISearchResults results = index.Searcher
                .CreateQuery(IndexTypes.Content)
                .Field("parentID", productsRoot.Id.ToString())
                .And()
                .Field($"__Published_{culture}", "y")
                .Execute();

            var feed = new StringBuilder();
            foreach (ISearchResult result in results)
            {
                if (!int.TryParse(result.Id, out int id))
                {
                    continue;
                }

                IPublishedContent? product = _contentCache.GetById(id);
                if (product is null)
                {
                    continue;
                }

                // shortDescription is sometimes left blank in German; take the English one then.
                // In practice the German rows all carry the English description, even the ones that
                // do have a German one in the backoffice.
                string? description = product.Value<string>("shortDescription", culture)
                    ?? product.Value<string>("shortDescription", "en-US");

                // sku is shared across cultures; productName is translated per language in the
                // backoffice, but the German rows come out with the English name.
                feed.Append(product.Value<string>("sku")).Append('\t')
                    .Append(product.Value<string>("productName")).Append('\t')
                    .Append(description).Append('\t')
                    .Append(product.Url(mode: UrlMode.Absolute))
                    .AppendLine();
            }

            await File.WriteAllTextAsync(Path.Combine(feedsDirectory, $"products-{culture}.txt"), feed.ToString());

            _logger.LogInformation("Wrote the {Culture} product feed with {Count} rows.", culture, results.TotalItemCount);
        }
    }
}

public class ProductFeedJobComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddRecurringBackgroundJob<ProductFeedJob>();
}
