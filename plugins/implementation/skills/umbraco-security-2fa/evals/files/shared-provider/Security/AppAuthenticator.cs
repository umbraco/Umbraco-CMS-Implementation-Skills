using System.Runtime.Serialization;
using Google.Authenticator;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace Acme.Web.Security;

// One authenticator for everyone: backoffice editors AND website members.

[DataContract]
public class AppAuthenticatorSetup : ISetupTwoFactorModel
{
    [DataMember(Name = "qrCodeSetupImageUrl")]
    public string? QrCodeSetupImageUrl { get; set; }

    [DataMember(Name = "secret")]
    public string? Secret { get; set; }

    [DataMember(Name = "manualEntryKey")]
    public string? ManualEntryKey { get; set; }
}

public class AppAuthenticator : ITwoFactorProvider
{
    public const string Name = "AppAuthenticator";

    private readonly IUserService _userService;
    private readonly IMemberService _memberService;

    public AppAuthenticator(IUserService userService, IMemberService memberService)
    {
        _userService = userService;
        _memberService = memberService;
    }

    public string ProviderName => Name;

    public async Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        IUser? user = await _userService.GetAsync(userOrMemberKey);
        IMember? member = user is null ? _memberService.GetById(userOrMemberKey) : null;
        string account = user?.Username ?? member!.Username;

        // Skip the package's built-in QR generation (it bloats the response with a base64 PNG)
        // and let a QR service draw it from the otpauth link instead.
        SetupCode setup = new TwoFactorAuthenticator().GenerateSetupCode("Acme", account, secret, false);
        string qrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data="
            + Uri.EscapeDataString($"otpauth://totp/Acme:{account}?secret={setup.ManualEntryKey}&issuer=Acme");

        return new AppAuthenticatorSetup { QrCodeSetupImageUrl = qrUrl, Secret = secret, ManualEntryKey = setup.ManualEntryKey };
    }

    public bool ValidateTwoFactorPIN(string secret, string code) =>
        new TwoFactorAuthenticator().ValidateTwoFactorPIN(secret, code);

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);
}

public class AppAuthenticatorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        new BackOfficeIdentityBuilder(builder.Services).AddTwoFactorProvider<AppAuthenticator>(AppAuthenticator.Name);
        new MemberIdentityBuilder(builder.Services).AddTwoFactorProvider<AppAuthenticator>(AppAuthenticator.Name);
    }
}
