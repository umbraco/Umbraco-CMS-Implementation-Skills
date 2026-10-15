using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Extensions;
using Umbraco.Skills.Examples.Fixtures;

namespace Umbraco.Skills.Examples.TwoFactor.Members;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships. It stands in for what a real site already has (a login page, a members-only page behind
// public access) and for the files a user adds by hand (the two partials under Views/Partials/).

/// <summary>Names the fixture shares with MemberTwoFactorBlankTests.</summary>
public static class TwoFactorMembersExample
{
    public const string LoginUrl = "/two-factor-login/";
    public const string AccountUrl = "/two-factor-account/";

    /// <summary>The only member public access lets into the account page. The test creates it.</summary>
    public const string MemberEmail = "two-factor-member@example.com";

    /// <summary>Marker the account template renders, proving the protected page itself was served.</summary>
    public const string AccountMarker = "two-factor-account-page";
}

/// <summary>
/// A second, BACKOFFICE provider registered alongside the member one, as on a site that has both.
/// Users and members share one provider list, so a member page that doesn't filter it offers this
/// provider, and asking it to set up a member throws, just as the docs' user provider does.
/// </summary>
public class ExampleBackOfficeProvider : ITwoFactorProvider
{
    public const string Name = "ExampleBackOfficeAuthenticator";

    public string ProviderName => Name;

    public Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret) =>
        throw new InvalidOperationException(
            "A backoffice 2FA provider was asked to set up a member: the member page must filter providers.");

    public bool ValidateTwoFactorPIN(string secret, string token) => false;

    public bool ValidateTwoFactorSetup(string secret, string token) => false;
}

/// <summary>Installs the login and account page types, with templates that render the skill's partials.</summary>
public class TwoFactorMembersExamplePlan : PackageMigrationPlan
{
    public TwoFactorMembersExamplePlan()
        : base("Umbraco Security 2FA members (example)")
    {
    }

    protected override void DefinePlan()
        => To<ImportTwoFactorMembersExample>(new Guid("2fa00000-0000-4000-8000-0000000000a1"));
}

/// <summary>
/// Calls IPackagingService.InstallCompiledPackageData directly — the inherited
/// ImportPackage.FromXmlDataManifest(...).Do() builder silently does nothing on Umbraco 17.5.3 when
/// given an XDocument (see the sitemap Approach B example for the details).
/// </summary>
public class ImportTwoFactorMembersExample : AsyncPackageMigrationBase
{
    private const string LoginTemplate =
        "@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage\n"
        + "@{ Layout = null; }\n"
        + "<!doctype html><html><body><main>\n"
        + "@await Html.PartialAsync(\"MemberLogin\")\n"
        + "</main></body></html>\n";

    private const string AccountTemplate =
        "@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage\n"
        + "@{ Layout = null; }\n"
        + "<!doctype html><html><body><main id=\"" + TwoFactorMembersExample.AccountMarker + "\">\n"
        + "@await Html.PartialAsync(\"TwoFactorSetup\")\n"
        + "</main></body></html>\n";

    private readonly IPackagingService _packagingService;

    public ImportTwoFactorMembersExample(
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
        XDocument manifest = EmbeddedManifest.Xml(typeof(ImportTwoFactorMembersExample).Assembly, "ExampleFixtureContent.xml")
            .WithTemplateDesign("twoFactorLoginPage", LoginTemplate)
            .WithTemplateDesign("twoFactorAccountPage", AccountTemplate);

        _packagingService.InstallCompiledPackageData(manifest);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Writes the skill's partials to Views/Partials/ (gitignored scratch on the host), seeds the login and
/// account pages under site 2's root, and protects the account page so only the test member gets in.
/// Idempotent, because the test database is reused across boots.
/// </summary>
public class TwoFactorMembersExampleSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly string[] Partials = ["MemberLogin.cshtml", "TwoFactorSetup.cshtml"];

    private readonly IWebHostEnvironment _environment;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _publishingService;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly IDatabaseCacheRebuilder _cacheRebuilder;
    private readonly IPublicAccessService _publicAccessService;
    private readonly ILogger<TwoFactorMembersExampleSeeder> _logger;

    public TwoFactorMembersExampleSeeder(
        IWebHostEnvironment environment,
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService publishingService,
        IDocumentUrlService documentUrlService,
        IDatabaseCacheRebuilder cacheRebuilder,
        IPublicAccessService publicAccessService,
        ILogger<TwoFactorMembersExampleSeeder> logger)
    {
        _environment = environment;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _publishingService = publishingService;
        _documentUrlService = documentUrlService;
        _cacheRebuilder = cacheRebuilder;
        _publicAccessService = publicAccessService;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        foreach (string partial in Partials)
        {
            string markup = EmbeddedManifest.Text(typeof(TwoFactorMembersExampleSeeder).Assembly, partial);
            string path = Path.Combine(_environment.ContentRootPath, "Views/Partials", partial);
            if (!System.IO.File.Exists(path) || await System.IO.File.ReadAllTextAsync(path, cancellationToken) != markup)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await System.IO.File.WriteAllTextAsync(path, markup, cancellationToken);
            }
        }

        IContent? root = FixtureSite.FindRoot(_contentService);
        if (root is null)
        {
            _logger.LogWarning("Site 2 has no root node, so the 2FA members fixture content was not seeded.");
            return;
        }

        IContent login = FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "twoFactorLoginPage", "Two-factor login");
        IContent account = FixtureSite.EnsureChild(_contentService, _contentTypeService, root, "twoFactorAccountPage", "Two-factor account");

        await FixtureSite.PublishAndRefreshAsync(_publishingService, _documentUrlService, _cacheRebuilder, root);

        if ((await _publicAccessService.GetEntryByContentKeyAsync(account.Key)).Result is null)
        {
            await _publicAccessService.CreateAsync(new PublicAccessEntrySlim
            {
                ContentId = account.Key,
                LoginPageId = login.Key,
                ErrorPageId = login.Key,
                MemberUserNames = [TwoFactorMembersExample.MemberEmail],
            });
        }
    }
}

public class TwoFactorMembersExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TwoFactorMembersExampleSeeder>();

        new BackOfficeIdentityBuilder(builder.Services)
            .AddTwoFactorProvider<ExampleBackOfficeProvider>(ExampleBackOfficeProvider.Name);
    }
}
