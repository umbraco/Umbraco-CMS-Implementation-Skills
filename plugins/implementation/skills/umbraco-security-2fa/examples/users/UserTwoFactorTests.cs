using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Authenticator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Skills.Examples.TwoFactor.Users;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Runtime validation of umbraco-security-2fa Approach A (backoffice users): the skill's provider,
/// composer and umbraco-package.json, compiled from its assets and loaded into the Clean host.
///
/// The decisive check is a real sign-in over HTTP through the backoffice login API, the same calls
/// the login screen makes: an enrolled user gets 402 with the provider name, a wrong code is refused,
/// and the right code completes the sign-in.
/// </summary>
[TestFixture]
public class UserTwoFactorTests
{
    private const string LoginUrl = "/umbraco/management/api/v1/security/back-office/login";
    private const string VerifyUrl = "/umbraco/management/api/v1/security/back-office/verify-2fa";

    /// <summary>A dedicated user, reused across runs: users with login history can't be deleted.</summary>
    private const string Email = "two-factor-gate@example.com";
    private const string Password = "TwoFactorGate-Pa55word!";

    private Guid _userKey;
    private string _secret = null!;

    private static T Service<T>() where T : notnull =>
        ReferenceSiteFixture.Factory.Services.GetRequiredService<T>();

    [OneTimeSetUp]
    public async Task EnrolATestUser()
    {
        _userKey = await EnsureUserWithKnownPasswordAsync();

        // Start clean: a previous run left this user enrolled.
        await Service<ITwoFactorLoginService>().DeleteUserLoginsAsync(_userKey);

        // Enrol exactly as the backoffice does: ask for setup data, then confirm with a first code.
        var twoFactor = Service<IUserTwoFactorLoginService>();
        Attempt<ISetupTwoFactorModel, TwoFactorOperationStatus> setup =
            await twoFactor.GetSetupInfoAsync(_userKey, UmbracoUserAppAuthenticator.Name);
        Assert.That(setup.Status, Is.EqualTo(TwoFactorOperationStatus.Success),
            "the provider must be registered on the backoffice identity builder");

        var info = (TwoFactorAuthInfo)setup.Result!;
        Assert.That(info.QrCodeSetupImageUrl, Does.StartWith("data:image/png;base64,"),
            "the QR code must be drawn on the server, never fetched from a third party with the secret in its URL");
        _secret = info.Secret!;

        Attempt<TwoFactorOperationStatus> saved = await twoFactor.ValidateAndSaveAsync(
            UmbracoUserAppAuthenticator.Name, _userKey, _secret, CurrentCode());
        Assert.That(saved.Result, Is.EqualTo(TwoFactorOperationStatus.Success));
    }

    [OneTimeTearDown]
    public async Task UnenrolTheTestUser()
    {
        if (_userKey != Guid.Empty)
        {
            await Service<ITwoFactorLoginService>().DeleteUserLoginsAsync(_userKey);
        }
    }

    [Test]
    public async Task Manifest_declares_an_mfaLoginProvider_for_the_registered_provider()
    {
        // The backoffice lists only providers that have a manifest entry, matched on forProviderName.
        IEnumerable<PackageManifest> manifests = await Service<IPackageManifestService>().GetAllPackageManifestsAsync();
        JsonElement[] extensions = manifests
            .SelectMany(manifest => manifest.Extensions)
            .Select(extension => JsonSerializer.SerializeToElement(extension))
            .Where(extension => extension.TryGetProperty("type", out JsonElement type) && type.GetString() == "mfaLoginProvider")
            .ToArray();

        Assert.That(
            extensions.Select(extension => extension.GetProperty("forProviderName").GetString()),
            Does.Contain(UmbracoUserAppAuthenticator.Name));
    }

    [Test]
    public async Task Password_alone_is_not_enough_for_an_enrolled_user()
    {
        using HttpClient client = NewClient();

        HttpResponseMessage response = await LoginAsync(client);

        Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes402), await response.Content.ReadAsStringAsync());
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            body.GetProperty("enabledTwoFactorProviderNames").EnumerateArray().Select(name => name.GetString()),
            Is.EquivalentTo(new[] { UmbracoUserAppAuthenticator.Name }));
    }

    [Test]
    public async Task Wrong_code_is_refused()
    {
        using HttpClient client = NewClient();
        await LoginAsync(client);

        HttpResponseMessage response = await VerifyAsync(client, WrongCode(CurrentCode()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Code_from_two_minutes_ago_is_refused()
    {
        // The asset narrows the library's default window (five minutes either side) to one 30-second step.
        string stale = new TwoFactorAuthenticator().GetCurrentPIN(_secret, DateTime.UtcNow.AddMinutes(-2));
        Assume.That(stale, Is.Not.EqualTo(CurrentCode()), "codes collided; rerun");

        using HttpClient client = NewClient();
        await LoginAsync(client);

        HttpResponseMessage response = await VerifyAsync(client, stale);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Current_code_completes_the_sign_in()
    {
        using HttpClient client = NewClient();
        await LoginAsync(client);

        HttpResponseMessage response = await VerifyAsync(client, CurrentCode());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
    }

    private const int StatusCodes402 = 402;

    /// <summary>Own cookie jar per test: the 2FA step is tied to the cookie the login call sets.</summary>
    private static HttpClient NewClient() =>
        ReferenceSiteFixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client) =>
        client.PostAsJsonAsync(LoginUrl, new { username = Email, password = Password });

    private static Task<HttpResponseMessage> VerifyAsync(HttpClient client, string code) =>
        client.PostAsJsonAsync(VerifyUrl, new { provider = UmbracoUserAppAuthenticator.Name, code });

    private string CurrentCode() => new TwoFactorAuthenticator().GetCurrentPIN(_secret);

    private static string WrongCode(string code) => ((int.Parse(code) + 500_000) % 1_000_000).ToString("D6");

    private static async Task<Guid> EnsureUserWithKnownPasswordAsync()
    {
        var users = Service<IUserService>();
        IUser? user = users.GetByEmail(Email);
        if (user is null)
        {
            Attempt<UserCreationResult, UserOperationStatus> created = await users.CreateAsync(
                Constants.Security.SuperUserKey,
                new UserCreateModel
                {
                    Email = Email,
                    UserName = Email,
                    Name = "Two-factor gate",
                    UserGroupKeys = new HashSet<Guid> { Constants.Security.EditorGroupKey },
                },
                approveUser: true);
            Assert.That(created.Status, Is.EqualTo(UserOperationStatus.Success), created.Result.Error?.ErrorMessage);
            user = created.Result.CreatedUser!;
        }

        await users.UnlockAsync(Constants.Security.SuperUserKey, user.Key);
        Attempt<PasswordChangedModel, UserOperationStatus> changed = await users.ChangePasswordAsync(
            Constants.Security.SuperUserKey,
            new ChangeUserPasswordModel { UserKey = user.Key, NewPassword = Password });
        Assert.That(changed.Status, Is.EqualTo(UserOperationStatus.Success), changed.Result?.Error?.ErrorMessage);

        return user.Key;
    }
}
