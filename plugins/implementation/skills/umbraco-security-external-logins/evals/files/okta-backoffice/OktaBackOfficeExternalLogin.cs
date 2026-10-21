using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;

namespace Contoso.Cms.Security;

public class OktaBackOfficeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<OktaBackOfficeExternalLoginProviderOptions>();
        builder.Services.ConfigureOptions<OktaOpenIdConnectOptions>();

        builder.AddBackOfficeExternalLogins(logins =>
            logins.AddBackOfficeLogin(backOfficeAuthenticationBuilder =>
                backOfficeAuthenticationBuilder.AddOpenIdConnect(
                    BackOfficeAuthenticationBuilder.SchemeForBackOffice(OktaBackOfficeExternalLoginProviderOptions.SchemeName),
                    "Okta",
                    options => { })));
    }
}

public class OktaOpenIdConnectOptions(IConfiguration configuration) : IConfigureNamedOptions<OpenIdConnectOptions>
{
    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != OktaBackOfficeExternalLoginProviderOptions.SchemeName)
        {
            return;
        }

        options.Authority = configuration["Okta:Authority"];
        options.ClientId = configuration["Okta:ClientId"];
        options.ClientSecret = configuration["Okta:ClientSecret"];
        options.CallbackPath = "/signin-oidc";
        options.ResponseType = "code";
        options.Scope.Add("email");
        options.Scope.Add("groups");
        options.SaveTokens = true;
    }

    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);
}

public class OktaBackOfficeExternalLoginProviderOptions : IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
{
    public const string SchemeName = "Okta";

    public void Configure(string? name, BackOfficeExternalLoginProviderOptions options)
    {
        if (name != Constants.Security.BackOfficeExternalAuthenticationTypePrefix + SchemeName)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(BackOfficeExternalLoginProviderOptions options)
    {
        options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
            autoLinkExternalAccount: true,
            defaultUserGroups: new[] { "Editors" },
            allowManualLinking: false)
        {
            OnAutoLinking = (user, loginInfo) =>
            {
                // Temporary: the agency needs full access while they set up the site.
                user.AddRole("admin");
                user.IsApproved = true;
            },
        };

        // Passwords are a security risk, so everyone must use Okta.
        options.DenyLocalLogin = true;
    }
}
