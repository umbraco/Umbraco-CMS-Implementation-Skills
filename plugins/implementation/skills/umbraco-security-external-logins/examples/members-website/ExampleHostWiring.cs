using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Skills.Examples.ExternalLogins.Members;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the skill
// ships to users.
//
// The skill tells users to put an "ExternalLogins:Members" section in appsettings.json. The
// reference instance is shared by every skill example, so the test values are applied through
// PostConfigure instead. No identity provider is reachable from the test run, so the OpenID Connect
// handler is given a static discovery document instead of fetching one from the authority.
public class ExampleHostWiringComposer : IComposer
{
    public const string AuthorizationEndpoint = "https://members-idp.example.test/oauth2/authorize";
    public const string ClientId = "example-member-client";

    public static readonly string Scheme = Constants.Security.MemberExternalAuthenticationTypePrefix + MemberSsoSettings.SchemeName;

    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.PostConfigure<MemberSsoSettings>(settings =>
        {
            settings.Authority = "https://members-idp.example.test";
            settings.ClientId = ClientId;
            settings.ClientSecret = "example-secret";
        });

        builder.Services.PostConfigure<OpenIdConnectOptions>(Scheme, options =>
        {
            var configuration = new OpenIdConnectConfiguration
            {
                Issuer = "https://members-idp.example.test",
                AuthorizationEndpoint = AuthorizationEndpoint,
                TokenEndpoint = "https://members-idp.example.test/oauth2/token",
            };
            options.Configuration = configuration;
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        });
    }
}

/// <summary>
/// Starts the member scheme's challenge, as UmbExternalLoginController.ExternalLogin does when the
/// login form's provider button is posted, so a test can read the redirect to the identity
/// provider without an antiforgery token. Validation-only.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class ExampleMemberChallengeController : Controller
{
    [HttpGet]
    [Route("example/external-logins/members/challenge")]
    public IActionResult Start() =>
        Challenge(new AuthenticationProperties { RedirectUri = "/" }, ExampleHostWiringComposer.Scheme);
}
