using System.Net;
using System.Text.RegularExpressions;
using Google.Authenticator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Skills.Examples.TwoFactor.Members;

namespace Umbraco_CMS.Skills.TestHost.Blank;

/// <summary>
/// Runtime validation of umbraco-security-2fa Approach B (website members), on site 2: the skill's
/// provider, surface controller and both partials, rendered on a login page and a members-only page
/// behind public access (seeded by examples/members/ExampleHostWiring.cs).
///
/// Everything a member does goes over HTTP through the rendered forms: enrol from the account page,
/// then sign in with password and code. A stand-in backoffice provider is registered too, so the setup
/// page is proven to offer only the member provider on a site that has both.
/// </summary>
[TestFixture]
public class MemberTwoFactorBlankTests
{
    private const string Password = "TwoFactorMember-Pa55word!";

    private Guid _memberKey;

    private static T Service<T>() where T : notnull =>
        BlankSiteFixture.Factory.Services.GetRequiredService<T>();

    [OneTimeSetUp]
    public async Task EnsureTestMember()
    {
        using IServiceScope scope = BlankSiteFixture.Factory.Services.CreateScope();
        var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();

        MemberIdentityUser? member = await members.FindByEmailAsync(TwoFactorMembersExample.MemberEmail);
        if (member is null)
        {
            var created = await members.CreateAsync(
                MemberIdentityUser.CreateNew(TwoFactorMembersExample.MemberEmail, TwoFactorMembersExample.MemberEmail,
                    Umbraco.Cms.Core.Constants.Security.DefaultMemberTypeAlias, isApproved: true, "Two-factor member"),
                Password);
            Assert.That(created.Succeeded, Is.True, string.Join("; ", created.Errors.Select(e => e.Description)));
            member = (await members.FindByEmailAsync(TwoFactorMembersExample.MemberEmail))!;
        }
        else
        {
            string token = await members.GeneratePasswordResetTokenAsync(member);
            var reset = await members.ResetPasswordAsync(member, token, Password);
            Assert.That(reset.Succeeded, Is.True, string.Join("; ", reset.Errors.Select(e => e.Description)));
            await members.SetLockoutEndDateAsync(member, null);
        }

        _memberKey = member.Key;
    }

    [SetUp]
    public Task StartUnenrolled() => Service<ITwoFactorLoginService>().DeleteUserLoginsAsync(_memberKey);

    [OneTimeTearDown]
    public Task Unenrol() => Service<ITwoFactorLoginService>().DeleteUserLoginsAsync(_memberKey);

    [Test]
    public async Task Account_page_enrols_the_member_with_the_member_provider_only()
    {
        using HttpClient client = NewClient();
        await SignInWithoutTwoFactorAsync(client);

        string account = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);
        Assert.That(account, Does.Contain(TwoFactorMembersExample.AccountMarker), "the protected page itself must render");
        Assert.That(account, Does.Contain(UmbracoMemberAppAuthenticator.Name));
        Assert.That(account, Does.Not.Contain(ExampleBackOfficeProvider.Name),
            "users and members share one provider list; the member page must not offer a backoffice provider");
        Assert.That(account, Does.Contain("src=\"data:image/png;base64,"),
            "the QR code must be drawn on the server, never fetched from a third party with the secret in its URL");

        string secret = HiddenInput(account, "secret");
        HttpResponseMessage enrol = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, account,
            new() { ["providerName"] = UmbracoMemberAppAuthenticator.Name, ["secret"] = secret, ["code"] = Code(secret) });
        Assert.That(enrol.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), await enrol.Content.ReadAsStringAsync());

        Assert.That(await IsEnrolledAsync(), Is.True);
        Assert.That(await GetOkAsync(client, TwoFactorMembersExample.AccountUrl), Does.Contain("Two-factor authentication is on"));
    }

    [Test]
    public async Task Enrolled_member_needs_a_code_and_then_lands_on_the_page_they_asked_for()
    {
        string secret = await EnrolDirectlyAsync();
        using HttpClient client = NewClient();

        // Public access serves the login page AT the protected URL.
        string loginPage = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);
        HttpResponseMessage password = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, loginPage, Credentials());
        string codePage = await password.Content.ReadAsStringAsync();

        Assert.That(password.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a correct password for an enrolled member must not sign in yet");
        Assert.That(HiddenInput(codePage, "provider"), Is.EqualTo(UmbracoMemberAppAuthenticator.Name),
            "after the password the login view must render the code form");

        HttpResponseMessage wrong = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, codePage,
            new() { ["provider"] = UmbracoMemberAppAuthenticator.Name, ["code"] = WrongCode(Code(secret)) });
        string wrongPage = await wrong.Content.ReadAsStringAsync();
        Assert.That(wrong.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(wrongPage, Does.Contain("Invalid code"));
        Assert.That(wrongPage, Does.Not.Contain(TwoFactorMembersExample.AccountMarker));

        HttpResponseMessage right = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, wrongPage,
            new() { ["provider"] = UmbracoMemberAppAuthenticator.Name, ["code"] = Code(secret) });
        Assert.That(right.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(right.Headers.Location?.OriginalString, Is.EqualTo(TwoFactorMembersExample.AccountUrl),
            "without the returnUrl field the member lands on the login page instead");

        Assert.That(await GetOkAsync(client, TwoFactorMembersExample.AccountUrl), Does.Contain(TwoFactorMembersExample.AccountMarker));
    }

    [Test]
    public async Task Turning_two_factor_off_needs_a_valid_code()
    {
        string secret = await EnrolDirectlyAsync();
        using HttpClient client = NewClient();
        await SignInWithCodeAsync(client, secret);

        string account = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);
        HttpResponseMessage wrong = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, account,
            new() { ["providerName"] = UmbracoMemberAppAuthenticator.Name, ["code"] = WrongCode(Code(secret)) });
        Assert.That(await wrong.Content.ReadAsStringAsync(), Does.Contain("That code is not valid"));
        Assert.That(await IsEnrolledAsync(), Is.True, "a wrong code must leave 2FA on");

        account = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);
        HttpResponseMessage right = await PostFormAsync(client, TwoFactorMembersExample.AccountUrl, account,
            new() { ["providerName"] = UmbracoMemberAppAuthenticator.Name, ["code"] = Code(secret) });
        Assert.That(right.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(await IsEnrolledAsync(), Is.False);
    }

    [Test]
    public async Task Umbracos_codeless_disable_route_is_blocked()
    {
        // Umbraco auto-routes its own surface controllers, so without the skill's filter this request
        // switches 2FA off with nothing but a signed-in session and an antiforgery token.
        string secret = await EnrolDirectlyAsync();
        using HttpClient client = NewClient();
        await SignInWithCodeAsync(client, secret);
        string account = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);

        HttpResponseMessage response = await client.PostAsync("/umbraco/surface/UmbTwoFactorLogin/Disable",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["providerName"] = UmbracoMemberAppAuthenticator.Name,
                ["__RequestVerificationToken"] = HiddenInput(FormContaining(account, "providerName"), "__RequestVerificationToken"),
            }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await IsEnrolledAsync(), Is.True, "Umbraco's own Disable must not switch 2FA off without a code");
    }

    [Test]
    public async Task Umbracos_unchecked_setup_route_is_blocked()
    {
        // UmbTwoFactorLoginController.ValidateAndSaveSetup saves a new secret without checking that one
        // is already enabled, so a signed-in session could add its own authenticator beside the member's.
        await EnrolDirectlyAsync();
        using HttpClient client = NewClient();
        await SignInWithCodeAsync(client, (await SecretAsync())!);
        string account = await GetOkAsync(client, TwoFactorMembersExample.AccountUrl);
        string attackerSecret = Guid.NewGuid().ToString();

        HttpResponseMessage response = await client.PostAsync("/umbraco/surface/UmbTwoFactorLogin/ValidateAndSaveSetup",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["providerName"] = UmbracoMemberAppAuthenticator.Name,
                ["secret"] = attackerSecret,
                ["code"] = Code(attackerSecret),
                ["__RequestVerificationToken"] = HiddenInput(FormContaining(account, "providerName"), "__RequestVerificationToken"),
            }));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- helpers ---------------------------------------------------------------------------------

    /// <summary>Own cookie jar per test; redirects observable rather than followed.</summary>
    private static HttpClient NewClient() =>
        BlankSiteFixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static Dictionary<string, string> Credentials() => new()
    {
        ["loginModel.Username"] = TwoFactorMembersExample.MemberEmail,
        ["loginModel.Password"] = Password,
    };

    private static async Task SignInWithoutTwoFactorAsync(HttpClient client)
    {
        string loginPage = await GetOkAsync(client, TwoFactorMembersExample.LoginUrl);
        HttpResponseMessage response = await PostFormAsync(client, TwoFactorMembersExample.LoginUrl, loginPage, Credentials());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), await response.Content.ReadAsStringAsync());
    }

    private static async Task SignInWithCodeAsync(HttpClient client, string secret)
    {
        string loginPage = await GetOkAsync(client, TwoFactorMembersExample.LoginUrl);
        HttpResponseMessage password = await PostFormAsync(client, TwoFactorMembersExample.LoginUrl, loginPage, Credentials());
        HttpResponseMessage code = await PostFormAsync(client, TwoFactorMembersExample.LoginUrl,
            await password.Content.ReadAsStringAsync(),
            new() { ["provider"] = UmbracoMemberAppAuthenticator.Name, ["code"] = Code(secret) });
        Assert.That(code.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), await code.Content.ReadAsStringAsync());
    }

    /// <summary>Enrols through the service, for tests that are about signing in rather than enrolling.</summary>
    private async Task<string> EnrolDirectlyAsync()
    {
        var twoFactor = Service<IMemberTwoFactorLoginService>();
        var setup = await twoFactor.GetSetupInfoAsync(_memberKey, UmbracoMemberAppAuthenticator.Name);
        string secret = ((QrCodeSetupData)setup.Result!).Secret;
        var saved = await twoFactor.ValidateAndSaveAsync(UmbracoMemberAppAuthenticator.Name, _memberKey, secret, Code(secret));
        Assert.That(saved.Success, Is.True, saved.Result.ToString());
        return secret;
    }

    private Task<string?> SecretAsync() =>
        Service<ITwoFactorLoginService>().GetSecretForUserAndProviderAsync(_memberKey, UmbracoMemberAppAuthenticator.Name);

    private async Task<bool> IsEnrolledAsync() => await Service<ITwoFactorLoginService>().IsTwoFactorEnabledAsync(_memberKey);

    private static string Code(string secret) => new TwoFactorAuthenticator().GetCurrentPIN(secret);

    private static string WrongCode(string code) => ((int.Parse(code) + 500_000) % 1_000_000).ToString("D6");

    private static async Task<string> GetOkAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET {url}: {body}");
        return body;
    }

    /// <summary>
    /// Submits the form on <paramref name="page"/> that carries <paramref name="fields"/>' first field,
    /// as a browser would: every input the form renders (antiforgery token, ufprt, the partial's own
    /// hidden fields), with <paramref name="fields"/> filled in on top.
    /// </summary>
    private static Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, string page, Dictionary<string, string> fields)
    {
        string form = FormContaining(page, fields.Keys.First());
        var values = Regex.Matches(form, "<input[^>]*name=\"([^\"]+)\"[^>]*value=\"([^\"]*)\"")
            .ToDictionary(input => input.Groups[1].Value, input => WebUtility.HtmlDecode(input.Groups[2].Value));
        foreach ((string name, string value) in fields)
        {
            values[name] = value;
        }

        return client.PostAsync(url, new FormUrlEncodedContent(values));
    }

    private static string FormContaining(string page, string fieldName)
    {
        Match form = Regex.Matches(page, "<form.*?</form>", RegexOptions.Singleline)
            .FirstOrDefault(match => match.Value.Contains($"name=\"{fieldName}\""))
            ?? throw new AssertionException($"No form with a '{fieldName}' field on the page:\n{page}");
        return form.Value;
    }

    private static string HiddenInput(string html, string name)
    {
        Match input = Regex.Match(html, $"<input[^>]*name=\"{Regex.Escape(name)}\"[^>]*value=\"([^\"]*)\"");
        Assert.That(input.Success, Is.True, $"no '{name}' input in:\n{html}");
        return WebUtility.HtmlDecode(input.Groups[1].Value);
    }
}
