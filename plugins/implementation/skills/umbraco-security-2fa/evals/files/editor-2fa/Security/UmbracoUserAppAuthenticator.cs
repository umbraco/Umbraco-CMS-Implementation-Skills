using System.Runtime.Serialization;
using Google.Authenticator;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace Acme.Web.Security;

// Copied from the Umbraco docs by Sam before they left. Deployed to staging three weeks ago.

[DataContract]
public class TwoFactorAuthInfo : ISetupTwoFactorModel
{
    [DataMember(Name = "qrCodeSetupImageUrl")]
    public string? QrCodeSetupImageUrl { get; set; }

    [DataMember(Name = "secret")]
    public string? Secret { get; set; }
}

public class UmbracoUserAppAuthenticator : ITwoFactorProvider
{
    public const string Name = "GoogleAuthenticator";

    private readonly IUserService _userService;

    public UmbracoUserAppAuthenticator(IUserService userService)
    {
        _userService = userService;
    }

    public string ProviderName => Name;

    public async Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        IUser? user = await _userService.GetAsync(userOrMemberKey);

        ArgumentNullException.ThrowIfNull(user);

        var applicationName = "testingOn15";
        var twoFactorAuthenticator = new TwoFactorAuthenticator();
        SetupCode setupInfo = twoFactorAuthenticator.GenerateSetupCode(applicationName, user.Username, secret, false);
        return new TwoFactorAuthInfo()
        {
            QrCodeSetupImageUrl = setupInfo.QrCodeSetupImageUrl,
            Secret = secret
        };
    }

    public bool ValidateTwoFactorPIN(string secret, string code)
    {
        var twoFactorAuthenticator = new TwoFactorAuthenticator();
        return twoFactorAuthenticator.ValidateTwoFactorPIN(secret, code);
    }

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);
}
