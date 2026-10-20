using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.Multilingual.CultureVariants;

/// <summary>
/// Installs umbraco-multilingual's assets into the blank reference host, standing in for the package
/// import and file copies a user would otherwise do.
/// </summary>
public class MultilingualPlan : PackageMigrationPlan
{
    public MultilingualPlan()
        : base("Umbraco Multilingual (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportMultilingual>(new Guid("d5000000-0000-4000-8000-000000000001"));
}

/// <summary>
/// Installs, as ONE manifest: the skill's multilingual-package.xml (languages + dictionary), the
/// skill's two partials as partial views, and this example's culture-variant page type + template.
///
/// The partials are spliced in as &lt;PartialViews&gt;&lt;View path="…"&gt; entries, which Umbraco's
/// package importer writes to Views/Partials — so the markup rendered is the asset file itself, not a
/// copy pasted into XML.
///
/// Calls IPackagingService.InstallCompiledPackageData directly rather than the inherited
/// ImportPackage.FromXmlDataManifest(...).Do() builder, which silently does nothing on Umbraco 17.5.3
/// when the manifest is supplied as an XDocument (see the sitemap Approach B example).
/// </summary>
public class ImportMultilingual : AsyncPackageMigrationBase
{
    private readonly IPackagingService _packagingService;

    public ImportMultilingual(
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
        var assembly = typeof(ImportMultilingual).Assembly;

        XDocument manifest = EmbeddedManifest.Xml(assembly, "multilingual-package.xml")
            .MergedWith(EmbeddedManifest.Xml(assembly, "ExampleFixtureContent.xml"));

        XElement partialViews = manifest.Root!.Element("PartialViews")
            ?? new XElement("PartialViews");
        if (partialViews.Parent is null)
        {
            manifest.Root.Add(partialViews);
        }

        foreach (string partial in new[] { "languageSwitcher.cshtml", "hreflangLinks.cshtml" })
        {
            partialViews.Add(new XElement("View",
                new XAttribute("path", partial),
                new XCData(EmbeddedManifest.Text(assembly, partial))));
        }

        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}
