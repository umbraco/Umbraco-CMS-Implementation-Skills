using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace Acme.Web.Migrations;

/// <summary>
/// Imports the embedded package manifest (danish-launch-package.xml, embedded as
/// Acme.Web.Migrations.package.xml by the csproj) — the da-DK language and the dictionary items.
/// </summary>
public class DanishLaunchMigration : AsyncPackageMigrationBase
{
    public DanishLaunchMigration(
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
    {
    }

    protected override Task MigrateAsync()
    {
        ImportPackage.FromEmbeddedResource<DanishLaunchMigration>().Do();
        return Task.CompletedTask;
    }
}
