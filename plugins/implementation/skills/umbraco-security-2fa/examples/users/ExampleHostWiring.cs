using Microsoft.AspNetCore.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Skills.Examples.TwoFactor.Users;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships. It does the one step a user does by hand: put the skill's manifest in App_Plugins.

/// <summary>
/// Writes the embedded umbraco-package.json to the host's App_Plugins/TwoFactorProviders/ while the
/// application is starting, before any request can make the backoffice read its manifests.
/// Content-comparing; the folder is gitignored on the host.
/// </summary>
public class TwoFactorManifestWriter : INotificationHandler<UmbracoApplicationStartingNotification>
{
    /// <summary>Where the skill's users reference tells users to put the manifest.</summary>
    public const string ManifestPath = "App_Plugins/TwoFactorProviders/umbraco-package.json";

    private readonly IWebHostEnvironment _environment;

    public TwoFactorManifestWriter(IWebHostEnvironment environment) => _environment = environment;

    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        using Stream resource = typeof(TwoFactorManifestWriter).Assembly
            .GetManifestResourceStream("umbraco-package.json")
            ?? throw new InvalidOperationException(
                "umbraco-package.json was not embedded; check plugins/Directory.Build.props.");
        string manifest = new StreamReader(resource).ReadToEnd();

        string path = Path.Combine(_environment.ContentRootPath, ManifestPath);
        if (File.Exists(path) && File.ReadAllText(path) == manifest)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, manifest);
    }
}

public class TwoFactorUsersExampleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationHandler<UmbracoApplicationStartingNotification, TwoFactorManifestWriter>();
}
