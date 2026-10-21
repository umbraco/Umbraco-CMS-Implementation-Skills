using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Helpers;
using Umbraco.Extensions;

namespace <Namespace>;

// Backoffice (Umbraco users) single sign-on through an OpenID Connect provider: Microsoft Entra ID,
// Okta, Auth0, Keycloak, Google Workspace and so on. Needs the NuGet package
// Microsoft.AspNetCore.Authentication.OpenIdConnect, the umbraco-package.json manifest that puts
// the button on the login screen, and the "ExternalLogins:BackOffice" configuration section.

/// <summary>Bound from the "ExternalLogins:BackOffice" configuration section.</summary>
public class BackOfficeSsoSettings
{
    public const string SectionName = "ExternalLogins:BackOffice";

    /// <summary>
    /// The scheme name without Umbraco's "Umbraco." prefix. Renaming it later makes Umbraco purge
    /// every user's link to the old name on the next boot.
    /// </summary>
    public const string SchemeName = "<SchemeName>";

    /// <summary>Pin it to your own tenant or issuer, never a multi-tenant "common" endpoint.</summary>
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>Keep it out of appsettings.json in source control: user secrets, environment variables or a vault.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Unique per scheme. Register the absolute URL, per environment, with the provider.</summary>
    public string CallbackPath { get; set; } = "<CallbackPath>";

    /// <summary>Email domains allowed to sign in or be auto-linked, for example ["contoso.com"]. Empty allows any.</summary>
    public string[] AllowedEmailDomains { get; set; } = [];

    /// <summary>The claim carrying the provider's groups or app roles. Entra ID "roles" arrive as ClaimTypes.Role.</summary>
    public string GroupClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>Provider group or role value → Umbraco user group alias. Only these groups are managed by sign-in.</summary>
    public Dictionary<string, string> GroupMappings { get; set; } = [];

    /// <summary>Removes password login for everyone. Keep a working break-glass route before turning it on.</summary>
    public bool DenyLocalLogin { get; set; }
}

public class BackOfficeSsoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<BackOfficeSsoSettings>(builder.Config.GetSection(BackOfficeSsoSettings.SectionName));

        // Both registrations are needed. Without the first, the login provider has no auto-link
        // options; without the second, the OpenID Connect handler has no client settings.
        builder.Services.ConfigureOptions<BackOfficeSsoLoginProviderOptions>();
        builder.Services.ConfigureOptions<BackOfficeSsoOpenIdConnectOptions>();

        builder.AddBackOfficeExternalLogins(logins =>
            logins.AddBackOfficeLogin(backOfficeAuthenticationBuilder =>
                backOfficeAuthenticationBuilder.AddOpenIdConnect(
                    BackOfficeAuthenticationBuilder.SchemeForBackOffice(BackOfficeSsoSettings.SchemeName),
                    "<ProviderLabel>",
                    // Left empty on purpose: BackOfficeSsoOpenIdConnectOptions supplies the settings.
                    _ => { })));
    }
}

/// <summary>Configures the OpenID Connect handler for the backoffice scheme.</summary>
public class BackOfficeSsoOpenIdConnectOptions(IOptions<BackOfficeSsoSettings> settings, OAuthOptionsHelper oAuthOptionsHelper)
    : IConfigureNamedOptions<OpenIdConnectOptions>
{
    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != BackOfficeAuthenticationBuilder.SchemeForBackOffice(BackOfficeSsoSettings.SchemeName))
        {
            return;
        }

        BackOfficeSsoSettings sso = settings.Value;
        options.Authority = sso.Authority;
        options.ClientId = sso.ClientId;
        options.ClientSecret = sso.ClientSecret;
        options.CallbackPath = sso.CallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        // Umbraco auto-links on ClaimTypes.Email ("email" is mapped onto it) and refuses to create a
        // user whose Identity.Name is empty. OpenID Connect sends the display name as "name", which
        // is not mapped, so point the name claim at it.
        options.Scope.Add("email");
        options.TokenValidationParameters.NameClaimType = "name";

        // Provider errors (cancelled consent, expired state) go back to the backoffice login screen
        // instead of an unhandled exception. SetDefaultErrorEventHandling only accepts OAuthOptions,
        // which OpenIdConnectOptions is not, so the events are wired by hand.
        options.Events.OnRemoteFailure = context =>
        {
            oAuthOptionsHelper.SetUmbracoRedirectWithFilteredParams(context, BackOfficeSsoSettings.SchemeName, "OnRemoteFailure").HandleResponse();
            return Task.CompletedTask;
        };
        options.Events.OnAccessDenied = context =>
        {
            oAuthOptionsHelper.SetUmbracoRedirectWithFilteredParams(context, BackOfficeSsoSettings.SchemeName, "OnAccessDenied").HandleResponse();
            return Task.CompletedTask;
        };
    }

    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);
}

/// <summary>Configures what Umbraco does with a successful provider sign-in: auto-linking and groups.</summary>
public class BackOfficeSsoLoginProviderOptions(IOptions<BackOfficeSsoSettings> settings)
    : IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
{
    public void Configure(string? name, BackOfficeExternalLoginProviderOptions options)
    {
        if (name != Constants.Security.BackOfficeExternalAuthenticationTypePrefix + BackOfficeSsoSettings.SchemeName)
        {
            return;
        }

        BackOfficeSsoSettings sso = settings.Value;
        options.DenyLocalLogin = sso.DenyLocalLogin;

        // No default groups: a user gets only the groups their provider claims map to, so a
        // misconfigured mapping can never hand out access, and never the administrators group.
        options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
            autoLinkExternalAccount: true,
            defaultUserGroups: [],
            allowManualLinking: !sso.DenyLocalLogin)
        {
            // Runs once, before a new user is saved, and before an existing user with the same
            // email is linked. Throwing stops the link with the message shown on the login screen.
            OnAutoLinking = (user, loginInfo) =>
            {
                // Linking signs the user in without Umbraco's usual approval and lockout check,
                // so a disabled or locked-out user must be stopped here.
                if (user.HasIdentity && (!user.IsApproved || user.IsLockedOut))
                {
                    throw new InvalidOperationException("This Umbraco user is disabled or locked out.");
                }

                string? refusal = BackOfficeSsoRules.Apply(user, loginInfo, sso);
                if (refusal is not null)
                {
                    throw new InvalidOperationException(refusal);
                }

                // New users are created unapproved, and the backoffice refuses unapproved users
                // from their second sign-in on.
                if (!user.HasIdentity)
                {
                    user.IsApproved = true;
                }
            },

            // Runs on every sign-in. Returning false refuses it; group changes are saved with the
            // user's last-login update.
            OnExternalLogin = (user, loginInfo) => BackOfficeSsoRules.Apply(user, loginInfo, sso) is null,
        };
    }

    public void Configure(BackOfficeExternalLoginProviderOptions options) => Configure(Options.DefaultName, options);
}

public static class BackOfficeSsoRules
{
    /// <summary>Checks the sign-in and syncs mapped groups. Returns why it is refused, or null to allow it.</summary>
    public static string? Apply(BackOfficeIdentityUser user, ExternalLoginInfo loginInfo, BackOfficeSsoSettings sso)
    {
        ClaimsPrincipal principal = loginInfo.Principal;

        // Umbraco links to an existing user whose email matches the claim, so an address the
        // provider has not verified could take over that account.
        string? email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return "The provider did not send an email address.";
        }

        string? emailVerified = principal.FindFirstValue("email_verified");
        if (emailVerified is not null && !string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "The provider has not verified this email address.";
        }

        if (sso.AllowedEmailDomains.Length > 0
            && !sso.AllowedEmailDomains.Any(domain => email.EndsWith("@" + domain.TrimStart('@'), StringComparison.OrdinalIgnoreCase)))
        {
            return "This email domain may not sign in to the backoffice.";
        }

        // Add or remove only the groups named in GroupMappings, so groups an administrator assigned
        // by hand in the backoffice are left alone. Several provider values can map to one Umbraco
        // group: it is kept while any of them is claimed.
        var claimed = principal.FindAll(sso.GroupClaimType).Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var granted = sso.GroupMappings.Where(mapping => claimed.Contains(mapping.Key)).Select(mapping => mapping.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string groupAlias in sso.GroupMappings.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            IdentityUserRole<string>? current = user.Roles.FirstOrDefault(role => string.Equals(role.RoleId, groupAlias, StringComparison.OrdinalIgnoreCase));
            if (granted.Contains(groupAlias) && current is null)
            {
                user.AddRole(groupAlias);
            }
            else if (!granted.Contains(groupAlias) && current is not null)
            {
                user.Roles.Remove(current);
            }
        }

        return user.Roles.Count == 0 ? "None of this user's provider groups are mapped to an Umbraco user group." : null;
    }
}
