# Approach B — Website members

Visitors sign in to the website with an external provider ("Sign in with Google"), and new members
are created on first sign-in. Existing members with the same email are linked. For backoffice
editors, use [users.md](users.md) instead.

## Contents

- [Fetch first](#fetch-first): read when you need the docs pages to check before writing any member sign-in code.
- [Steps](#steps): read when you are adding external sign-in for website members, from discovery to configuration.
- [Verify](#verify): read when the code is in place and you need to check that it works.
- [What trips people up](#what-trips-people-up): read when something fails (blank button, members who can't sign in, error pages) or when you need the security pitfalls of auto-linking.
- [Swapping the handler](#swapping-the-handler): read when the provider uses `AddGoogle` or another OAuth handler instead of OpenID Connect.
- [Headless front ends](#headless-front-ends): read when a Delivery API front end must sign members in with the same provider.
- [Done](#done): read when you are ready to report back to the user.

## Fetch first

- [External login providers](https://docs.umbraco.com/umbraco-cms/run-in-production/security/external-login-providers.md),
  the "Member Authentication" tabs: auto-link options and the member builder.
- [Member registration and login](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/members-registration-and-login.md):
  the **Login** partial view snippet, which already renders a button per external provider and
  shows their errors. Use **Edit Profile** for members linking a provider to an existing account.
- When the provider owns the profile:
  [Lightweight external members](https://docs.umbraco.com/umbraco-cms/run-in-production/security/lightweight-external-members.md)
  (17.4+).
- For a headless front end:
  [Protected content in the Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/protected-content-in-the-delivery-api.md),
  section "How to use external identity providers".

## Steps

1. **Discover.** Find the root namespace and the Umbraco version (17+), and the login view
   (often a partial from the Login snippet). Check for existing schemes and callback paths
   (`AddMemberExternalLogins` and `AddBackOfficeExternalLogins`), and for the Member Types and
   member groups the site uses. Ask whether new visitors may self-register through the provider,
   and which group protected pages check.
2. **Add the package:** `dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect`.
3. **Write the code.** Copy [`../assets/MemberExternalLogin.cs`](../assets/MemberExternalLogin.cs)
   and replace the placeholders:

   | Placeholder | Example | Rule |
   |---|---|---|
   | `<Namespace>` | `MySite.Security` | The project's namespace |
   | `<MemberSchemeName>` | `Google` | No `UmbracoMembers.` prefix |
   | `<MemberProviderLabel>` | `Google` | Button text: "Sign in with Google" |
   | `<MemberCallbackPath>` | `/signin-google-members` | Unique: never a backoffice scheme's path |
   | `<LoginPagePath>` | `/login` | The page holding the login form |

4. **Configure.** The secret goes in user secrets locally and in environment variables or a vault
   elsewhere: `ExternalLogins__Members__ClientSecret`.

   ```json
   "ExternalLogins": {
     "Members": {
       "Authority": "https://accounts.google.com",
       "ClientId": "<client-id>",
       "MemberTypeAlias": "customer",
       "DefaultMemberGroups": [ "Customers" ]
     }
   }
   ```

5. **Render the button.** If the login view came from the Login snippet, the button appears on
   its own. Otherwise post `Html.BeginUmbracoForm<UmbExternalLoginController>("ExternalLogin")` with
   a `provider` field set to `UmbracoMembers.<MemberSchemeName>`. Read errors with
   `ViewData.GetExternalSignInProviderErrors()`, and show a message when the query string has
   `externalLoginError`, which the asset adds on a cancelled or failed sign-in.
6. **Reserve the callback path** in `Umbraco:CMS:Global:ReservedPaths`, keeping the existing
   entries, and **register the redirect URI** at the provider as an absolute `https://` URL per
   environment.
7. **Verify** (next section).

## Verify

- The login page shows "Sign in with <label>". Clicking it redirects to the provider with
  `redirect_uri` ending in the member callback path.
- Browsing the callback path directly lands on `<LoginPagePath>?externalLoginError=failed`, not an
  exception page.
- A first sign-in creates an approved member of the configured type and groups, and a page
  protected by that group opens. A second sign-in reuses the member.

## What trips people up

- **A backoffice provider doesn't sign in members.** Members use `AddMemberExternalLogins`, and
  `SchemeForMembers` is an *instance* method on the member builder, unlike the static
  `SchemeForBackOffice`. Options classes therefore build the name from
  `Constants.Security.MemberExternalAuthenticationTypePrefix` (`UmbracoMembers.`). Members need
  no `umbraco-package.json`.
- **Two schemes on one callback path.** Each handler claims its own path. When a backoffice
  provider already uses `/signin-google` or `/signin-oidc`, the member scheme needs another.
- **A blank button.** The Login snippet prints `Sign in with @DisplayName`, which is the
  `displayName` argument of `AddOpenIdConnect`/`AddGoogle`. Without it the label is empty.
- **Account takeover by email.** Auto-linking links to an *existing* member with a matching email.
  For a provider that lets people claim addresses they don't own, that hands over the account.
  Refuse `email_verified: false` (the asset does) and don't accept multi-tenant Entra ID tokens
  for linking existing members.
- **Pre-registered accounts.** If the site lets people register without confirming their email,
  someone can register a customer's address first. When the real customer later signs in with
  the provider, auto-linking attaches them to that account, and the person who registered it
  keeps the password. Confirm emails at registration, or don't auto-link to members that have a
  password and let them link from their profile instead.
- **Approval is bypassed.** Linking skips Umbraco's approval and lockout check, so an existing
  member awaiting approval, or locked out, is signed in by their first provider sign-in. Refuse
  them in `OnAutoLinking` (the asset does).
- **Members created but can't sign in.** Keep `defaultIsApproved: true`. The Member Type alias
  must exist. Default member groups should be the ones the site's public access rules check, or
  the new member can't reach protected pages.
- **Cancelled sign-in shows an exception page.** Umbraco handles provider errors for the
  backoffice only. The asset's `OnRemoteFailure` and `OnAccessDenied` send members back to the
  login page.
- **Name or email missing.** As for users, auto-linking needs `ClaimTypes.Email` and a non-empty
  `Identity.Name`. Request the `email` scope and set `NameClaimType = "name"` for OpenID Connect.
- **Umbraco's 2FA is skipped.** `MemberBypassTwoFactorForExternalLogins` defaults to `true`.
- **Existing members who don't want auto-linking.** Turn auto-linking off and let signed-in
  members link the provider from a profile page built on the Edit Profile snippet
  (`UmbExternalLoginController.LinkLogin`).
- **Lightweight external members** (`ExternalOnly = true`): no password, no Member Type
  properties, and only group-based public access. Assign groups through `defaultMemberGroups`,
  and later through `IExternalMemberService`, not `Roles`. Read the docs' trade-off table first.

## Swapping the handler

To use `AddGoogle` or another OAuth handler, replace `AddOpenIdConnect` and
`IConfigureNamedOptions<OpenIdConnectOptions>` with that handler's options type. Keep the asset's
`OnRemoteFailure`/`OnAccessDenied` redirects: `SetDefaultErrorEventHandling` returns to the
backoffice login, not the site's. Keep `MemberSsoLoginProviderOptions` and `MemberSsoRules` as
they are.

## Headless front ends

The Delivery API reuses the same member scheme. Enable
`DeliveryApi:MemberAuthorization:AuthorizationCodeFlow` and restart Umbraco. The client then adds
`identity_provider=UmbracoMembers.<MemberSchemeName>` to the authorize request at
`/umbraco/delivery/api/v1/security/member/authorize`. For login redirect URLs, CORS and the
content index rebuild, use the `umbraco-security` skill if it is installed, or the docs page above.

## Done

Tell the user where the code and configuration went, and which redirect URI to register per
environment. Name the Member Type and groups new members get, what happens to existing members
with the same email, and where the button and errors render. List the checks from **Verify** that
were and weren't made.
