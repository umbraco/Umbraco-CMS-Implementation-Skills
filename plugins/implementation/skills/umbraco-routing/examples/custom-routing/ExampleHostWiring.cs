using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.RoutingCustom;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships. It stands in for what already exists on a real site: the product and landing Document
// Types, some content, and the view file copied into Views/.

/// <summary>Installs the example's product, product-list and store-finder types.</summary>
public class RoutingCustomExamplePlan : PackageMigrationPlan
{
    public RoutingCustomExamplePlan()
        : base("Umbraco Routing custom (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportRoutingCustomExample>(new Guid("e8200000-0000-4000-8000-000000000001"));
}

/// <summary>InstallCompiledPackageData directly: the inherited ImportPackage builder silently does
/// nothing with an XDocument on 17.5.3.</summary>
public class ImportRoutingCustomExample : AsyncPackageMigrationBase
{
    private readonly IPackagingService _packagingService;

    public ImportRoutingCustomExample(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings)
        : base(packagingService, mediaService, mediaFileManager, mediaUrlGenerators,
            shortStringHelper, contentTypeBaseServiceProvider, context, packageMigrationsSettings)
        => _packagingService = packagingService;

    protected override Task MigrateAsync()
    {
        XDocument manifest = EmbeddedManifest.Xml(typeof(ImportRoutingCustomExample).Assembly, "ExampleFixtureContent.xml");
        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Writes the skill's virtual-page view to Views/, then seeds products and the store-finder landing
/// node under site 2's shared root. Idempotent: the test database is reused across boots, and the
/// view write is content-comparing so runtime compilation isn't invalidated for nothing.
/// </summary>
public class RoutingCustomExampleSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IWebHostEnvironment _environment;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _publishingService;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly IDatabaseCacheRebuilder _cacheRebuilder;
    private readonly ILogger<RoutingCustomExampleSeeder> _logger;

    public RoutingCustomExampleSeeder(
        IWebHostEnvironment environment,
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService publishingService,
        IDocumentUrlService documentUrlService,
        IDatabaseCacheRebuilder cacheRebuilder,
        ILogger<RoutingCustomExampleSeeder> logger)
    {
        _environment = environment;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _publishingService = publishingService;
        _documentUrlService = documentUrlService;
        _cacheRebuilder = cacheRebuilder;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        string viewPath = Path.Combine(_environment.ContentRootPath, "Views", "storeFinderCity.cshtml");
        string view = EmbeddedManifest.Text(typeof(RoutingCustomExampleSeeder).Assembly, "storeFinderCity.cshtml");
        if (!System.IO.File.Exists(viewPath) || await System.IO.File.ReadAllTextAsync(viewPath, cancellationToken) != view)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(viewPath)!);
            await System.IO.File.WriteAllTextAsync(viewPath, view, cancellationToken);
        }

        IContent? root = FixtureSite.FindRoot(_contentService);
        if (root is null)
        {
            _logger.LogWarning("Site 2 has no root node, so the routing fixture content was not seeded.");
            return;
        }

        IContent products = FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "routingProductList", "Routing Products");
        FixtureSite.EnsureChild(_contentService, _contentTypeService, products, "routingProduct", "Swibble",
            new Dictionary<string, object?> { ["sku"] = "SW-123" });
        FixtureSite.EnsureChild(_contentService, _contentTypeService, products, "routingProduct", "Dibble",
            new Dictionary<string, object?> { ["sku"] = "DB-456" });

        // No SKU: exercises the provider's fall-back to the plain name-based segment.
        FixtureSite.EnsureChild(_contentService, _contentTypeService, products, "routingProduct", "Gizmo");

        FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "storeFinder", "Stores");

        await FixtureSite.PublishAndRefreshAsync(_publishingService, _documentUrlService, _cacheRebuilder, root);
    }
}

/// <summary>Registers the seeder. Picked up by AddComposers() in the blank host's Program.cs.</summary>
public class RoutingCustomExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RoutingCustomExampleSeeder>();
}
