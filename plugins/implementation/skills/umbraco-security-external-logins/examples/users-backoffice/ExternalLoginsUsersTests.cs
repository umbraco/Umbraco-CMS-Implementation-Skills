using System.Net;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Skills.Examples.ExternalLogins.Users;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Runtime validation of the umbraco-security-external-logins skill, users (backoffice) approach:
/// assets/BackOfficeExternalLogin.cs and assets/umbraco-package.json, compiled and loaded into the
/// reference instance. No identity provider is reachable, so the tests stop at the redirect to it,
/// and call the auto-link callbacks directly with the claims a provider would send.
/// </summary>
[TestFixture]
public class ExternalLoginsUsersTests
{
    private static readonly string Scheme = BackOfficeAuthenticationBuilder.SchemeForBackOffice(BackOfficeSsoSettings.SchemeName);

    private HttpClient _noRedirectClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() =>
        _noRedirectClient = ReferenceSiteFixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [OneTimeTearDown]
    public void OneTimeTearDown() => _noRedirectClient?.Dispose();

    [Test]
    public async Task Challenge_redirects_to_the_provider_with_the_configured_client_and_callback()
    {
        HttpResponseMessage response = await _noRedirectClient.GetAsync("/example/external-logins/users/challenge");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Uri location = response.Headers.Location!;
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Multiple(() =>
        {
            Assert.That(location.GetLeftPart(UriPartial.Path), Is.EqualTo(ExampleHostWiringComposer.AuthorizationEndpoint));
            Assert.That(query["client_id"], Is.EqualTo(ExampleHostWiringComposer.ClientId));
            Assert.That(query["redirect_uri"], Does.EndWith("/umbraco-example-sso-signin"));
            Assert.That(query["response_type"], Is.EqualTo("code"));
            Assert.That(query["code_challenge"], Is.Not.Null.And.Not.Empty, "PKCE must be on");
            Assert.That(query["scope"]!.Split(' '), Does.Contain("email"),
                "Umbraco auto-links on the email claim, so the email scope must be requested");
        });
    }

    [Test]
    public async Task A_failed_callback_returns_to_the_backoffice_login_instead_of_throwing()
    {
        // A callback with no valid state is what a cancelled or expired sign-in looks like.
        HttpResponseMessage response = await _noRedirectClient.GetAsync("/umbraco-example-sso-signin?error=access_denied");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        string location = response.Headers.Location!.ToString();
        Assert.That(location, Does.Contain("flow=external-login"));
        Assert.That(location, Does.Contain("provider=" + BackOfficeSsoSettings.SchemeName));
        Assert.That(location, Does.Contain("error=access_denied"));
    }

    [Test]
    public async Task The_login_button_manifest_is_public_and_names_the_registered_scheme()
    {
        // The login screen loads before anyone is signed in, so it can only use public manifests.
        HttpResponseMessage response = await ReferenceSiteFixture.Client.GetAsync("/umbraco/management/api/v1/manifest/manifest/public");
        string body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("\"authProvider\""));
        Assert.That(body, Does.Contain($"\"forProviderName\":\"{Scheme}\""),
            "forProviderName must be the scheme name with Umbraco's 'Umbraco.' prefix");
    }

    [Test]
    public async Task The_provider_is_registered_with_auto_linking_and_no_default_groups()
    {
        IBackOfficeExternalLoginProviders providers = ReferenceSiteFixture.Factory.Services.GetRequiredService<IBackOfficeExternalLoginProviders>();
        BackOfficeExternaLoginProviderScheme? provider = await providers.GetAsync(Scheme);

        Assert.That(provider, Is.Not.Null, "the scheme is not registered as a backoffice login provider");
        ExternalSignInAutoLinkOptions? autoLink = provider!.ExternalLoginProvider.Options.AutoLinkOptions;
        Assert.That(autoLink?.AutoLinkExternalAccount, Is.True);
        Assert.That(autoLink!.DefaultUserGroups, Is.Empty, "groups must come from mapped provider claims only");
    }

    [Test]
    public void A_sign_in_from_an_allowed_domain_gets_the_mapped_group()
    {
        // "editor" is also mapped from a second provider group the user doesn't have
        // (ExampleHostWiringComposer.SecondProviderGroupForEditor): that must not remove it.
        BackOfficeIdentityUser user = NewUser("editor@example.test");

        bool allowed = AutoLink().OnExternalLogin!(user, LoginInfo("editor@example.test", ExampleHostWiringComposer.MappedProviderGroup));

        Assert.That(allowed, Is.True);
        Assert.That(user.Roles.Select(role => role.RoleId), Is.EquivalentTo(new[] { "editor" }));
    }

    [Test]
    public void Losing_the_provider_group_removes_the_mapped_group_but_keeps_hand_assigned_ones()
    {
        BackOfficeIdentityUser user = NewUser("editor@example.test");
        user.AddRole("editor");
        user.AddRole("writer"); // assigned by hand in the backoffice, not in GroupMappings

        bool allowed = AutoLink().OnExternalLogin!(user, LoginInfo("editor@example.test"));

        Assert.That(allowed, Is.True);
        Assert.That(user.Roles.Select(role => role.RoleId), Is.EquivalentTo(new[] { "writer" }));
    }

    [Test]
    public void A_sign_in_with_no_mapped_group_is_refused()
    {
        bool allowed = AutoLink().OnExternalLogin!(NewUser("someone@example.test"), LoginInfo("someone@example.test", "Unmapped.Group"));

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void Another_email_domain_is_refused()
    {
        bool allowed = AutoLink().OnExternalLogin!(NewUser("editor@elsewhere.test"), LoginInfo("editor@elsewhere.test", ExampleHostWiringComposer.MappedProviderGroup));

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void An_unverified_email_cannot_be_auto_linked_to_an_existing_user()
    {
        // Umbraco links a provider sign-in to an existing user by email, so this is the account
        // takeover path the rule closes.
        BackOfficeIdentityUser existingAdmin = NewUser("admin@example.test");
        existingAdmin.AddRole("admin");

        Assert.Throws<InvalidOperationException>(() =>
            AutoLink().OnAutoLinking!(existingAdmin, LoginInfo("admin@example.test", ExampleHostWiringComposer.MappedProviderGroup, emailVerified: false)));
    }

    [Test]
    public void A_new_auto_linked_user_is_approved()
    {
        // Created unapproved, a user would get in once and be refused from the second sign-in on.
        BackOfficeIdentityUser user = NewUser("editor@example.test");
        Assert.That(user.IsApproved, Is.False, "precondition: Umbraco creates the user unapproved");

        AutoLink().OnAutoLinking!(user, LoginInfo("editor@example.test", ExampleHostWiringComposer.MappedProviderGroup));

        Assert.That(user.IsApproved, Is.True);
    }

    [Test]
    public void A_disabled_existing_user_cannot_be_auto_linked()
    {
        // Linking skips Umbraco's approval check, so it would otherwise let a disabled user back in.
        BackOfficeIdentityUser disabled = NewUser("editor@example.test");
        disabled.Id = "42";
        disabled.IsApproved = false;

        Assert.Throws<InvalidOperationException>(() =>
            AutoLink().OnAutoLinking!(disabled, LoginInfo("editor@example.test", ExampleHostWiringComposer.MappedProviderGroup)));
    }

    private static ExternalSignInAutoLinkOptions AutoLink() =>
        ReferenceSiteFixture.Factory.Services
            .GetRequiredService<IOptionsMonitor<BackOfficeExternalLoginProviderOptions>>()
            .Get(Scheme)
            .AutoLinkOptions!;

    private static BackOfficeIdentityUser NewUser(string email) =>
        BackOfficeIdentityUser.CreateNew(new GlobalSettings(), email, email, "en-US", "Example User");

    private static ExternalLoginInfo LoginInfo(string email, string? providerGroup = null, bool? emailVerified = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Email, email), new("name", "Example User") };
        if (providerGroup is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, providerGroup));
        }

        if (emailVerified is not null)
        {
            claims.Add(new Claim("email_verified", emailVerified.Value ? "true" : "false"));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role));
        return new ExternalLoginInfo(principal, Scheme, "provider-key", BackOfficeSsoSettings.SchemeName);
    }
}
