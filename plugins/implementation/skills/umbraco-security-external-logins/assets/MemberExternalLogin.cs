using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;

namespace <Namespace>;

// Website (Umbraco members) sign-in through an OpenID Connect provider: Microsoft Entra ID or
// External ID, Auth0, Okta, Keycloak, Google and so on. Needs the NuGet package
// Microsoft.AspNetCore.Authentication.OpenIdConnect, the "ExternalLogins:Members" configuration
// section, and a login view that renders a button per provider (Umbraco's "Login" partial view
// snippet does). Members get no backoffice manifest: that is for users only.

/// <summary>Bound from the "ExternalLogins:Members" configuration section.</summary>
public class MemberSsoSettings
{
    public const string SectionName = "ExternalLogins:Members";

    /// <summary>The scheme name without Umbraco's "UmbracoMembers." prefix.</summary>
    public const string SchemeName = "<MemberSchemeName>";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>Keep it out of appsettings.json in source control: user secrets, environment variables or a vault.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Unique per scheme, so never the backoffice scheme's path. Register the absolute URL with the provider.</summary>
    public string CallbackPath { get; set; } = "<MemberCallbackPath>";

    /// <summary>Where a failed or cancelled provider sign-in lands, with ?externalLoginError=... appended.</summary>
    public string LoginPagePath { get; set; } = "<LoginPagePath>";

    /// <summary>The Member Type new members are created as. It must exist.</summary>
    public string MemberTypeAlias { get; set; } = Constants.Security.DefaultMemberTypeAlias;

    /// <summary>Member groups every auto-linked member joins, for example the group public access checks. They must exist.</summary>
    public string[] DefaultMemberGroups { get; set; } = [];
}

public class MemberSsoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<MemberSsoSettings>(builder.Config.GetSection(MemberSsoSettings.SectionName));
        builder.Services.ConfigureOptions<MemberSsoLoginProviderOptions>();
        builder.Services.ConfigureOptions<MemberSsoOpenIdConnectOptions>();

        builder.AddMemberExternalLogins(logins =>
            logins.AddMemberLogin(memberAuthenticationBuilder =>
                memberAuthenticationBuilder.AddOpenIdConnect(
                    memberAuthenticationBuilder.SchemeForMembers(MemberSsoSettings.SchemeName),
                    // The Login snippet renders "Sign in with {display name}": without one the button is blank.
                    "<MemberProviderLabel>",
                    // Left empty on purpose: MemberSsoOpenIdConnectOptions supplies the settings.
                    _ => { })));
    }
}

/// <summary>Configures the OpenID Connect handler for the member scheme.</summary>
public class MemberSsoOpenIdConnectOptions(IOptions<MemberSsoSettings> settings) : IConfigureNamedOptions<OpenIdConnectOptions>
{
    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != Constants.Security.MemberExternalAuthenticationTypePrefix + MemberSsoSettings.SchemeName)
        {
            return;
        }

        MemberSsoSettings sso = settings.Value;
        options.Authority = sso.Authority;
        options.ClientId = sso.ClientId;
        options.ClientSecret = sso.ClientSecret;
        options.CallbackPath = sso.CallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        // Umbraco auto-links on ClaimTypes.Email ("email" is mapped onto it) and refuses to create a
        // member whose Identity.Name is empty. OpenID Connect sends "name", which is not mapped.
        options.Scope.Add("email");
        options.TokenValidationParameters.NameClaimType = "name";

        // Members have no equivalent of the backoffice's error redirect: without these events a
        // visitor who cancels at the provider, or whose sign-in state expired, gets an unhandled
        // exception page.
        options.Events.OnRemoteFailure = context => RedirectToLoginPage(context, sso, "failed");
        options.Events.OnAccessDenied = context => RedirectToLoginPage(context, sso, "access_denied");
    }

    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);

    private static Task RedirectToLoginPage(HandleRequestContext<RemoteAuthenticationOptions> context, MemberSsoSettings sso, string error)
    {
        context.Response.Redirect(sso.LoginPagePath + QueryString.Create("externalLoginError", error));
        context.HandleResponse();
        return Task.CompletedTask;
    }
}

/// <summary>Configures what Umbraco does with a successful provider sign-in: auto-linking new members.</summary>
public class MemberSsoLoginProviderOptions(IOptions<MemberSsoSettings> settings)
    : IConfigureNamedOptions<MemberExternalLoginProviderOptions>
{
    public void Configure(string? name, MemberExternalLoginProviderOptions options)
    {
        if (name != Constants.Security.MemberExternalAuthenticationTypePrefix + MemberSsoSettings.SchemeName)
        {
            return;
        }

        MemberSsoSettings sso = settings.Value;
        options.AutoLinkOptions = new MemberExternalSignInAutoLinkOptions(
            autoLinkExternalAccount: true,
            // Auto-linked members must be approved, or they are created and then can't sign in.
            defaultIsApproved: true,
            defaultMemberTypeAlias: sso.MemberTypeAlias,
            defaultMemberGroups: sso.DefaultMemberGroups)
        {
            // Runs once, before a new member is saved, and before an existing member with the same
            // email is linked. Throwing stops the link and shows the message on the login form.
            OnAutoLinking = (member, loginInfo) =>
            {
                // Linking signs the member in without Umbraco's usual approval and lockout check,
                // so an unapproved or locked-out member must be stopped here.
                if (member.HasIdentity && (!member.IsApproved || member.IsLockedOut))
                {
                    throw new InvalidOperationException("This member account is not approved or is locked out.");
                }

                string? refusal = MemberSsoRules.Check(loginInfo);
                if (refusal is not null)
                {
                    throw new InvalidOperationException(refusal);
                }
            },

            // Runs on every sign-in. Returning false refuses it.
            OnExternalLogin = (member, loginInfo) => MemberSsoRules.Check(loginInfo) is null,
        };
    }

    public void Configure(MemberExternalLoginProviderOptions options) => Configure(Options.DefaultName, options);
}

public static class MemberSsoRules
{
    /// <summary>Returns why the sign-in is refused, or null to allow it.</summary>
    public static string? Check(ExternalLoginInfo loginInfo)
    {
        // Umbraco links to an existing member whose email matches the claim, so an address the
        // provider has not verified could take over that member's account.
        string? email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return "The provider did not send an email address.";
        }

        string? emailVerified = loginInfo.Principal.FindFirstValue("email_verified");
        if (emailVerified is not null && !string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "The provider has not verified this email address.";
        }

        return null;
    }
}
