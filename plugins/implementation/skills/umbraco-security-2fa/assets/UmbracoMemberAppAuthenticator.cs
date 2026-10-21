using Google.Authenticator;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace <Namespace>;

/// <summary>What the TwoFactorSetup partial needs to show the QR code and post the secret back.</summary>
public class QrCodeSetupData : ISetupTwoFactorModel
{
    public required string Secret { get; init; }

    public required SetupCode SetupCode { get; init; }
}

/// <summary>Authenticator-app (TOTP) two-factor provider for website members.</summary>
public class UmbracoMemberAppAuthenticator : ITwoFactorProvider
{
    /// <summary>
    /// Saved with every enrolment. Never rename it: enrolments under a name no provider claims are
    /// ignored, so those members would silently lose 2FA. Must differ from any backoffice provider's name.
    /// </summary>
    public const string Name = "UmbracoMemberAppAuthenticator";

    /// <summary>The account label shown in the authenticator app.</summary>
    private const string IssuerName = "<IssuerName>";

    /// <summary>One 30-second step either side. The library's default accepts codes up to 5 minutes old.</summary>
    private static readonly TimeSpan ClockDriftTolerance = TimeSpan.FromSeconds(30);

    private readonly IMemberService _memberService;

    public UmbracoMemberAppAuthenticator(IMemberService memberService) => _memberService = memberService;

    public string ProviderName => Name;

    public Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        IMember member = _memberService.GetById(userOrMemberKey)
            ?? throw new InvalidOperationException($"No member with key {userOrMemberKey}.");

        // The QR code is a data: URI drawn on this server, so the secret is never sent to a third party.
        SetupCode setupCode = new TwoFactorAuthenticator().GenerateSetupCode(IssuerName, member.Username, secret, false);

        return Task.FromResult<ISetupTwoFactorModel>(new QrCodeSetupData { Secret = secret, SetupCode = setupCode });
    }

    public bool ValidateTwoFactorPIN(string secret, string token) =>
        new TwoFactorAuthenticator().ValidateTwoFactorPIN(secret, token, ClockDriftTolerance);

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);
}

public class UmbracoMemberAppAuthenticatorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        new MemberIdentityBuilder(builder.Services)
            .AddTwoFactorProvider<UmbracoMemberAppAuthenticator>(UmbracoMemberAppAuthenticator.Name);
}
