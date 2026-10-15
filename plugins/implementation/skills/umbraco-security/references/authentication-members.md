# Authentication: members

How website visitors register and sign in, on Razor sites and headless front ends. For
backoffice editors, see [authentication-users.md](authentication-users.md).

## Fetch first (pick the page for the task)

| Task | Docs page |
|---|---|
| Registration, login, logout, login status in Razor | [Member registration and login](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/members-registration-and-login.md) |
| Members, member types, member groups | [Members](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/members.md) |
| Current member in code | [IMemberManager](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/templating-and-rendering/querying/imembermanager.md) |
| Social or SSO login for members | [External login providers](https://docs.umbraco.com/umbraco-cms/run-in-production/security/external-login-providers.md), "Members" sections |
| Members who exist only in the IdP | [Lightweight external members](https://docs.umbraco.com/umbraco-cms/run-in-production/security/lightweight-external-members.md) |
| Two-factor authentication | [Two-factor authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/two-factor-authentication.md), "Members" section |
| Headless login (SPA, mobile, Next.js) | [Protected content in the Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/protected-content-in-the-delivery-api/README.md) |
| Server-to-server access to protected content | [Server-to-server access](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api/protected-content-in-the-delivery-api/server-to-server-access.md) |

## Razor sites: what trips people up

- **Use the built-in surface controllers** (`UmbLoginController`, `UmbRegisterController`,
  `UmbLoginStatusController`) through `Html.BeginUmbracoForm`. They bring antiforgery validation,
  lockout and the member sign-in manager. A hand-rolled login action that checks passwords
  through `IMemberService` loses all of that.
- **Only redirect to local URLs** after login. `UmbLoginController` already checks
  `Url.IsLocalUrl` on its `RedirectUrl`. Any custom login or registration action that accepts a
  return URL must do the same, or it becomes an open redirect.
- **New members must be approved** to sign in. Decide whether registration auto-approves or waits
  for an editor, and say which one you implemented.
- **Member password rules are separate from user rules:** `Umbraco:CMS:Security:MemberPassword`
  and `MemberDefaultLockoutTimeInMinutes`. Set them explicitly. The defaults suit editors, not
  necessarily the public.
- **Group assignment on registration** is code (a `MemberSavedNotification` handler in the
  tutorial). Without it, public-access rules that check a group never match a new member.

## Headless sites (Delivery API member authorization)

- Opt in with `DeliveryApi:MemberAuthorization:AuthorizationCodeFlow:Enabled` and list
  **absolute** `LoginRedirectUrls`. Wildcards are not supported. Restart Umbraco after changing it.
- **Rebuild `DeliveryApiContentIndex`** after enabling or disabling member authorization. Until
  then, protected content is still missing from or present in responses as before, and this is
  the commonly missed step.
- The client uses Authorization Code + PKCE with `client_id` `umbraco-member`. The login page is
  an Umbraco-rendered page, at `/Account/Login` by default; the docs show how to move it.
  The docs warn against public OIDC clients in web apps. For a server-rendered front end, prefer
  a confidential, server-side flow or server-to-server access (see the table above).
- A SPA on another origin also needs CORS for the token and API calls
  ([cors.md](cors.md)).

## External and lightweight members

Member external logins are registered with `AddMemberExternalLogins` and
`SchemeForMembers(...)`. This is a different builder from the backoffice one, so a provider set
up for editors does not log in members. Lightweight external members skip content-entity storage
entirely. Read the docs' trade-offs (no backoffice editing, groups held differently) before
choosing it.

**Related:** [authorization.md](authorization.md) (what a signed-in member can then reach).
