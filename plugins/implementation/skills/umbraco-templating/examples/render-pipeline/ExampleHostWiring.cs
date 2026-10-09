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
using Umbraco.Extensions;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.Templating;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships. It stands in for the steps a user does by hand: create the Document Type and Template in the
// backoffice, drop the partial and view-component view into Views/, create some pages.

/// <summary>Installs the productPage Document Type and its Template (markup = the skill's asset).</summary>
public class TemplatingExamplePlan : PackageMigrationPlan
{
    public TemplatingExamplePlan()
        : base("Umbraco Templating (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportTemplatingExample>(new Guid("d4000000-0000-4000-8000-000000000001"));
}

/// <summary>
/// Calls IPackagingService.InstallCompiledPackageData directly — the inherited
/// ImportPackage.FromXmlDataManifest(...).Do() builder silently does nothing on Umbraco 17.5.3 when
/// given an XDocument (see the sitemap Approach B example for the details).
/// </summary>
public class ImportTemplatingExample : AsyncPackageMigrationBase
{
    private readonly IPackagingService _packagingService;

    public ImportTemplatingExample(
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
        var assembly = typeof(ImportTemplatingExample).Assembly;
        XDocument manifest = EmbeddedManifest.Xml(assembly, "ExampleFixtureContent.xml")
            .WithTemplateDesign("productPage", EmbeddedManifest.Text(assembly, "productPage.cshtml"));

        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Copies the skill's partial and view-component view to the paths its reference tells users to use,
/// then seeds two productPage nodes under site 2's shared root.
///
/// Files are (re)written on every boot because Views/ is gitignored scratch on the host and a fresh
/// checkout has none; the write is content-comparing so runtime compilation isn't invalidated for
/// nothing. Content seeding is idempotent because the test database is reused across boots.
/// </summary>
public class TemplatingExampleSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    /// <summary>Asset name → path under the content root, as given in the skill's reference files.</summary>
    private static readonly (string Resource, string RelativePath)[] ViewFiles =
    [
        ("contactForm.cshtml", "Views/Partials/contactForm.cshtml"),
        ("RelatedPages.Default.cshtml", "Views/Shared/Components/RelatedPages/Default.cshtml"),
    ];

    private readonly IWebHostEnvironment _environment;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _publishingService;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly IDatabaseCacheRebuilder _cacheRebuilder;
    private readonly ILogger<TemplatingExampleSeeder> _logger;

    public TemplatingExampleSeeder(
        IWebHostEnvironment environment,
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService publishingService,
        IDocumentUrlService documentUrlService,
        IDatabaseCacheRebuilder cacheRebuilder,
        ILogger<TemplatingExampleSeeder> logger)
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
        var assembly = typeof(TemplatingExampleSeeder).Assembly;
        foreach ((string resource, string relativePath) in ViewFiles)
        {
            string path = Path.Combine(_environment.ContentRootPath, relativePath);
            string markup = EmbeddedManifest.Text(assembly, resource);
            if (System.IO.File.Exists(path) && await System.IO.File.ReadAllTextAsync(path, cancellationToken) == markup)
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await System.IO.File.WriteAllTextAsync(path, markup, cancellationToken);
        }

        IContent? root = FixtureSite.FindRoot(_contentService);
        if (root is null)
        {
            _logger.LogWarning("Site 2 has no root node, so the templating fixture content was not seeded.");
            return;
        }

        FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "productPage", "Product page",
            new Dictionary<string, object?> { ["tagline"] = "Built to last" });

        // No tagline: exercises the template's Fallback.ToDefaultValue branch.
        FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "productPage", "Related product");

        await FixtureSite.PublishAndRefreshAsync(_publishingService, _documentUrlService, _cacheRebuilder, root);
    }
}

/// <summary>Registers the seeder. Picked up by AddComposers() in the blank host's Program.cs.</summary>
public class TemplatingExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TemplatingExampleSeeder>();
}
