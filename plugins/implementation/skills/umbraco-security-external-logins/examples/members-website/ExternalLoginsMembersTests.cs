using System.Net;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Skills.Examples.ExternalLogins.Members;

namespace Umbraco_CMS.Skills.TestHost;

/// <summary>
/// Runtime validation of the umbraco-security-external-logins skill, members (website) approach:
/// assets/MemberExternalLogin.cs, compiled and loaded into the reference instance. No identity
/// provider is reachable, so the tests stop at the redirect to it, and call the auto-link
/// callbacks directly with the claims a provider would send.
/// </summary>
[TestFixture]
public class ExternalLoginsMembersTests
{
    private static readonly string Scheme = ExampleHostWiringComposer.Scheme;

    private HttpClient _noRedirectClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() =>
        _noRedirectClient = ReferenceSiteFixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [OneTimeTearDown]
    public void OneTimeTearDown() => _noRedirectClient?.Dispose();

    [Test]
    public async Task Challenge_redirects_to_the_provider_with_the_member_callback()
    {
        HttpResponseMessage response = await _noRedirectClient.GetAsync("/example/external-logins/members/challenge");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Uri location = response.Headers.Location!;
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Multiple(() =>
        {
            Assert.That(location.GetLeftPart(UriPartial.Path), Is.EqualTo(ExampleHostWiringComposer.AuthorizationEndpoint));
            Assert.That(query["client_id"], Is.EqualTo(ExampleHostWiringComposer.ClientId));
            Assert.That(query["redirect_uri"], Does.EndWith("/example-member-sso-signin"),
                "members need a callback path of their own, not the backoffice scheme's");
            Assert.That(query["code_challenge"], Is.Not.Null.And.Not.Empty, "PKCE must be on");
            Assert.That(query["scope"]!.Split(' '), Does.Contain("email"));
        });
    }

    [Test]
    public async Task A_failed_callback_returns_to_the_login_page_instead_of_throwing()
    {
        HttpResponseMessage response = await _noRedirectClient.GetAsync("/example-member-sso-signin?error=access_denied");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
            "without OnRemoteFailure handling, a cancelled member sign-in is an unhandled exception");
        Assert.That(response.Headers.Location!.ToString(), Is.EqualTo("/login?externalLoginError=failed"));
    }

    [Test]
    public async Task The_provider_is_listed_for_the_login_form_with_a_display_name()
    {
        // Umbraco's Login partial view snippet renders "Sign in with {DisplayName}" per provider.
        IMemberExternalLoginProviders providers = ReferenceSiteFixture.Factory.Services.GetRequiredService<IMemberExternalLoginProviders>();
        MemberExternalLoginProviderScheme? provider = (await providers.GetMemberProvidersAsync())
            .FirstOrDefault(p => p.ExternalLoginProvider.AuthenticationType == Scheme);

        Assert.That(provider, Is.Not.Null, "the scheme is not registered as a member login provider");
        Assert.That(provider!.AuthenticationScheme.DisplayName, Is.EqualTo("Example Member SSO"));

        MemberExternalSignInAutoLinkOptions? autoLink = provider.ExternalLoginProvider.Options.AutoLinkOptions;
        Assert.That(autoLink?.AutoLinkExternalAccount, Is.True);
        Assert.That(autoLink!.DefaultIsApproved, Is.True, "an unapproved auto-linked member can't sign in");
    }

    [Test]
    public void A_verified_email_is_allowed()
    {
        bool allowed = AutoLink().OnExternalLogin!(NewMember(), LoginInfo(emailVerified: true));

        Assert.That(allowed, Is.True);
    }

    [Test]
    public void An_unverified_email_cannot_be_auto_linked_to_an_existing_member()
    {
        Assert.Throws<InvalidOperationException>(() => AutoLink().OnAutoLinking!(NewMember(), LoginInfo(emailVerified: false)));
        Assert.That(AutoLink().OnExternalLogin!(NewMember(), LoginInfo(emailVerified: false)), Is.False);
    }

    [Test]
    public void An_unapproved_existing_member_cannot_be_auto_linked()
    {
        // Linking skips Umbraco's approval check, so it would otherwise bypass member approval.
        MemberIdentityUser pending = MemberIdentityUser.CreateNew("member@example.test", "member@example.test", "Member", false, "Example Member");
        pending.Id = "42";

        Assert.Throws<InvalidOperationException>(() => AutoLink().OnAutoLinking!(pending, LoginInfo(emailVerified: true)));
    }

    private static MemberExternalSignInAutoLinkOptions AutoLink() =>
        ReferenceSiteFixture.Factory.Services
            .GetRequiredService<IOptionsMonitor<MemberExternalLoginProviderOptions>>()
            .Get(Scheme)
            .AutoLinkOptions!;

    private static MemberIdentityUser NewMember() =>
        MemberIdentityUser.CreateNew("member@example.test", "member@example.test", "Member", true, "Example Member");

    private static ExternalLoginInfo LoginInfo(bool emailVerified)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "member@example.test"),
            new("name", "Example Member"),
            new("email_verified", emailVerified ? "true" : "false"),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", ClaimTypes.Role));
        return new ExternalLoginInfo(principal, Scheme, "provider-key", MemberSsoSettings.SchemeName);
    }
}
