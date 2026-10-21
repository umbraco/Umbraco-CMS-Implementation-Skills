using System.Runtime.Serialization;
using Google.Authenticator;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace <Namespace>;

/// <summary>
/// What the backoffice's built-in "Configure Two-Factor" screen reads: it shows the QR code and
/// posts the secret back with the user's first code.
/// </summary>
[DataContract]
public class TwoFactorAuthInfo : ISetupTwoFactorModel
{
    [DataMember(Name = "qrCodeSetupImageUrl")]
    public string? QrCodeSetupImageUrl { get; init; }

    [DataMember(Name = "secret")]
    public string? Secret { get; init; }
}

/// <summary>Authenticator-app (TOTP) two-factor provider for backoffice users.</summary>
public class UmbracoUserAppAuthenticator : ITwoFactorProvider
{
    /// <summary>
    /// Saved with every enrolment and matched by umbraco-package.json's forProviderName. Never rename
    /// it: enrolments under a name no provider claims are ignored, so those users would silently lose 2FA.
    /// </summary>
    public const string Name = "UmbracoUserAppAuthenticator";

    /// <summary>The account label shown in the authenticator app.</summary>
    private const string IssuerName = "<IssuerName>";

    /// <summary>One 30-second step either side. The library's default accepts codes up to 5 minutes old.</summary>
    private static readonly TimeSpan ClockDriftTolerance = TimeSpan.FromSeconds(30);

    private readonly IUserService _userService;

    public UmbracoUserAppAuthenticator(IUserService userService) => _userService = userService;

    public string ProviderName => Name;

    public async Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        IUser? user = await _userService.GetAsync(userOrMemberKey);
        ArgumentNullException.ThrowIfNull(user);

        // The QR code is a data: URI drawn on this server, so the secret is never sent to a third party.
        SetupCode setupCode = new TwoFactorAuthenticator().GenerateSetupCode(IssuerName, user.Username, secret, false);

        return new TwoFactorAuthInfo { QrCodeSetupImageUrl = setupCode.QrCodeSetupImageUrl, Secret = secret };
    }

    public bool ValidateTwoFactorPIN(string secret, string token) =>
        new TwoFactorAuthenticator().ValidateTwoFactorPIN(secret, token, ClockDriftTolerance);

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);
}

public class UmbracoUserAppAuthenticatorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        new BackOfficeIdentityBuilder(builder.Services)
            .AddTwoFactorProvider<UmbracoUserAppAuthenticator>(UmbracoUserAppAuthenticator.Name);
}
