using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.Helpers;

namespace Shop.Web.Security;

// Backoffice Google login for editors. Works today; please don't break it.
public class GoogleBackOfficeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<GoogleBackOfficeExternalLoginProviderOptions>();
        builder.Services.ConfigureOptions<GoogleBackOfficeAuthenticationOptions>();

        builder.AddBackOfficeExternalLogins(logins =>
            logins.AddBackOfficeLogin(backOfficeAuthenticationBuilder =>
                backOfficeAuthenticationBuilder.AddGoogle(
                    BackOfficeAuthenticationBuilder.SchemeForBackOffice(GoogleBackOfficeExternalLoginProviderOptions.SchemeName)!,
                    options => { })));
    }
}

public class GoogleBackOfficeExternalLoginProviderOptions : IConfigureNamedOptions<BackOfficeExternalLoginProviderOptions>
{
    public const string SchemeName = "Google";

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
        // Editors link their Google account from their profile; no auto-linking.
        options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(autoLinkExternalAccount: false);
    }
}

public class GoogleBackOfficeAuthenticationOptions(OAuthOptionsHelper helper, IConfiguration configuration)
    : IConfigureNamedOptions<GoogleOptions>
{
    public void Configure(string? name, GoogleOptions options)
    {
        if (name != BackOfficeAuthenticationBuilder.SchemeForBackOffice(GoogleBackOfficeExternalLoginProviderOptions.SchemeName))
        {
            return;
        }

        Configure(options);
    }

    public void Configure(GoogleOptions options)
    {
        options.CallbackPath = "/signin-google";
        options.ClientId = configuration["Google:ClientId"]!;
        options.ClientSecret = configuration["Google:ClientSecret"]!;
        helper.SetDefaultErrorEventHandling(options, GoogleBackOfficeExternalLoginProviderOptions.SchemeName);
    }
}
