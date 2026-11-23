using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
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

namespace Umbraco.Skills.Examples.RoutingBuiltIn;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships. It stands in for what an editor does by hand: import the composition, add it to a page type,
// and fill in the routing properties on a few pages.

/// <summary>Installs the skill's routingProperties composition plus the example's page type.</summary>
public class RoutingBuiltInExamplePlan : PackageMigrationPlan
{
    public RoutingBuiltInExamplePlan()
        : base("Umbraco Routing built-in (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportRoutingBuiltInExample>(new Guid("e8100000-0000-4000-8000-000000000001"));
}

/// <summary>
/// One manifest, because the example's page type composes the skill's type and Umbraco's importer only
/// resolves compositions inside the manifest being installed. InstallCompiledPackageData directly: the
/// inherited ImportPackage builder silently does nothing with an XDocument on 17.5.3.
/// </summary>
public class ImportRoutingBuiltInExample : AsyncPackageMigrationBase
{
    private readonly IPackagingService _packagingService;

    public ImportRoutingBuiltInExample(
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
        var assembly = typeof(ImportRoutingBuiltInExample).Assembly;
        XDocument manifest = EmbeddedManifest.Xml(assembly, "routing-properties-package.xml")
            .MergedWith(EmbeddedManifest.Xml(assembly, "ExampleFixtureContent.xml"));

        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Seeds one page per reserved routing property under site 2's shared root. Idempotent: the test
/// database is reused across boots.
/// </summary>
public class RoutingBuiltInExampleSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _publishingService;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly IDocumentUrlAliasService _documentUrlAliasService;
    private readonly IDatabaseCacheRebuilder _cacheRebuilder;
    private readonly ILogger<RoutingBuiltInExampleSeeder> _logger;

    public RoutingBuiltInExampleSeeder(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService publishingService,
        IDocumentUrlService documentUrlService,
        IDocumentUrlAliasService documentUrlAliasService,
        IDatabaseCacheRebuilder cacheRebuilder,
        ILogger<RoutingBuiltInExampleSeeder> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _publishingService = publishingService;
        _documentUrlService = documentUrlService;
        _documentUrlAliasService = documentUrlAliasService;
        _cacheRebuilder = cacheRebuilder;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        IContent? root = FixtureSite.FindRoot(_contentService);
        if (root is null)
        {
            _logger.LogWarning("Site 2 has no root node, so the routing fixture content was not seeded.");
            return;
        }

        const string page = "routingDemoPage";

        // Only set properties the installed type actually has. A package that misnames an alias must
        // fail this example's assertions, not throw here and take every other blank-host fixture down.
        HashSet<string> aliases = _contentTypeService.Get(page)?.CompositionPropertyTypes
            .Select(p => p.Alias).ToHashSet() ?? [];
        Dictionary<string, object?> Values(string alias, object? value)
        {
            if (aliases.Contains(alias))
            {
                return new Dictionary<string, object?> { [alias] = value };
            }

            _logger.LogWarning("{DocType} has no '{Alias}' property, so it was not seeded.", page, alias);
            return [];
        }

        IContent sale = FixtureSite.EnsureChild(_contentService, _contentTypeService, root, page, "Spring Sale",
            Values("umbracoUrlName", "sale"));

        IContent contact = FixtureSite.EnsureChild(_contentService, _contentTypeService, root, page, "Routing Contact",
            Values("umbracoUrlAlias", "get-in-touch,support/contact"));

        // Content Picker values are stored as document UDIs, which is what the backoffice saves.
        FixtureSite.EnsureChild(_contentService, _contentTypeService, root, page, "Old Offers",
            Values("umbracoRedirect", Udi.Create(Constants.UdiEntityType.Document, sale.Key).ToString()));

        FixtureSite.EnsureChild(_contentService, _contentTypeService, root, page, "Routing Help",
            Values("umbracoInternalRedirectId", Udi.Create(Constants.UdiEntityType.Document, contact.Key).ToString()));

        await FixtureSite.PublishAndRefreshAsync(_publishingService, _documentUrlService, _cacheRebuilder, root);

        // umbracoUrlAlias values are indexed in their own persisted map on 17.5, which the shared
        // helper doesn't rebuild.
        await _documentUrlAliasService.RebuildAllAliasesAsync();
    }
}

/// <summary>Registers the seeder. Picked up by AddComposers() in the blank host's Program.cs.</summary>
public class RoutingBuiltInExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RoutingBuiltInExampleSeeder>();
}
