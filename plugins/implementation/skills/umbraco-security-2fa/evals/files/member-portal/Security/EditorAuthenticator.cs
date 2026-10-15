using System.Runtime.Serialization;
using Google.Authenticator;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace Acme.Web.Security;

// Added last year so editors could use an authenticator app in the backoffice. Works fine.
[DataContract]
public class EditorTwoFactorInfo : ISetupTwoFactorModel
{
    [DataMember(Name = "qrCodeSetupImageUrl")]
    public string? QrCodeSetupImageUrl { get; set; }

    [DataMember(Name = "secret")]
    public string? Secret { get; set; }
}

public class EditorAuthenticator : ITwoFactorProvider
{
    public const string Name = "EditorAuthenticator";

    private readonly IUserService _userService;

    public EditorAuthenticator(IUserService userService) => _userService = userService;

    public string ProviderName => Name;

    public async Task<ISetupTwoFactorModel> GetSetupDataAsync(Guid userOrMemberKey, string secret)
    {
        IUser? user = await _userService.GetAsync(userOrMemberKey);
        ArgumentNullException.ThrowIfNull(user);

        SetupCode setup = new TwoFactorAuthenticator().GenerateSetupCode("Acme CMS", user.Username, secret, false);
        return new EditorTwoFactorInfo { QrCodeSetupImageUrl = setup.QrCodeSetupImageUrl, Secret = secret };
    }

    public bool ValidateTwoFactorPIN(string secret, string code) =>
        new TwoFactorAuthenticator().ValidateTwoFactorPIN(secret, code, TimeSpan.FromSeconds(30));

    public bool ValidateTwoFactorSetup(string secret, string token) => ValidateTwoFactorPIN(secret, token);
}

public class EditorAuthenticatorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        new BackOfficeIdentityBuilder(builder.Services).AddTwoFactorProvider<EditorAuthenticator>(EditorAuthenticator.Name);
}
