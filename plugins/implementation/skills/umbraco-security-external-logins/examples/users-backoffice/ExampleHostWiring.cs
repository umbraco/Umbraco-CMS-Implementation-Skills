using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Infrastructure.Manifest;

namespace Umbraco.Skills.Examples.ExternalLogins.Users;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships to users.
//
// The skill tells users to put an "ExternalLogins:BackOffice" section in appsettings.json and the
// umbraco-package.json manifest in App_Plugins. The reference instance is shared by every skill
// example, so this does the equivalent from code: test settings through PostConfigure, and the
// skill's own manifest (embedded by the csproj) served through an IPackageManifestReader.
//
// No identity provider is reachable from the test run, so the OpenID Connect handler is given a
// static discovery document instead of fetching one from the authority.
public class ExampleHostWiringComposer : IComposer
{
    public const string AuthorizationEndpoint = "https://idp.example.test/oauth2/authorize";
    public const string ClientId = "example-backoffice-client";
    public const string AllowedDomain = "example.test";
    public const string MappedProviderGroup = "Umbraco.Editors";
    public const string SecondProviderGroupForEditor = "Umbraco.ContentTeam";

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.PostConfigure<BackOfficeSsoSettings>(settings =>
        {
            settings.Authority = "https://idp.example.test";
            settings.ClientId = ClientId;
            settings.ClientSecret = "example-secret";
            settings.AllowedEmailDomains = [AllowedDomain];
            settings.GroupMappings = new Dictionary<string, string>
            {
                [MappedProviderGroup] = "editor",
                [SecondProviderGroupForEditor] = "editor",
            };
        });

        builder.Services.PostConfigure<OpenIdConnectOptions>(
            BackOfficeAuthenticationBuilder.SchemeForBackOffice(BackOfficeSsoSettings.SchemeName),
            options =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = "https://idp.example.test",
                    AuthorizationEndpoint = AuthorizationEndpoint,
                    TokenEndpoint = "https://idp.example.test/oauth2/token",
                };
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

        builder.Services.AddSingleton<IPackageManifestReader, EmbeddedSkillManifestReader>();
    }
}

/// <summary>Serves the skill's umbraco-package.json as if it were in App_Plugins.</summary>
public class EmbeddedSkillManifestReader(IJsonSerializer jsonSerializer) : IPackageManifestReader
{
    public async Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        await using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("umbraco-package.json")
            ?? throw new InvalidOperationException("umbraco-package.json was not embedded; check the example csproj.");
        using var reader = new StreamReader(stream);
        PackageManifest manifest = jsonSerializer.Deserialize<PackageManifest>(await reader.ReadToEndAsync())
            ?? throw new InvalidOperationException("umbraco-package.json did not deserialize to a package manifest.");
        return [manifest];
    }
}

/// <summary>
/// Starts the backoffice scheme's challenge, as the login button does through Umbraco's authorize
/// endpoint, so a test can read the redirect to the identity provider. Validation-only.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class ExampleBackOfficeChallengeController : Controller
{
    [HttpGet]
    [Route("example/external-logins/users/challenge")]
    public IActionResult Start() =>
        Challenge(
            new AuthenticationProperties { RedirectUri = "/umbraco" },
            BackOfficeAuthenticationBuilder.SchemeForBackOffice(BackOfficeSsoSettings.SchemeName));
}
