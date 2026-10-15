# Authentication: backoffice users

How editors and administrators sign in to `/umbraco`, and how machines authenticate to the
Management API. For website visitors, see [authentication-members.md](authentication-members.md).

## Fetch first (pick the page for the task)

| Task | Docs page |
|---|---|
| SSO / external provider (Entra ID, Google, any OpenID Connect) | [External login providers](https://docs.umbraco.com/umbraco-cms/run-in-production/security/external-login-providers.md), then the [Entra ID](https://docs.umbraco.com/umbraco-cms/run-in-production/tutorials/add-microsoft-entra-id-authentication.md) or [Google](https://docs.umbraco.com/umbraco-cms/run-in-production/tutorials/add-google-authentication.md) tutorial |
| Two-factor authentication | [Two-factor authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/two-factor-authentication.md), "Users" section |
| Password rules, lockout, session length, concurrent logins | [Security settings](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/securitysettings.md) |
| Check credentials against another store (LDAP, legacy DB) | [Replacing the basic username/password check](https://docs.umbraco.com/umbraco-cms/run-in-production/security/custom-password-check.md) |
| React to login/logout (audit, sign out of the IdP) | [BackOfficeUserManager and events](https://docs.umbraco.com/umbraco-cms/run-in-production/security/backofficeusermanager-and-notifications.md) |
| Machine access to the Management API | [API users](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/users/api-users.md) and [External access](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/management-api/external-access.md) |
| Locked out, or lost the admin password | [Locking of users and password reset](https://docs.umbraco.com/umbraco-cms/run-in-production/security/password-reset.md), [Reset admin password](https://docs.umbraco.com/umbraco-cms/run-in-production/security/reset-admin-password.md) |
| Password-protect a staging site with backoffice logins | [Basic authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/basic-authentication.md) |

## External login providers: what trips people up

- **Two halves.** The server registers the provider in C#: a composer calling
  `AddBackOfficeExternalLogins`, with the scheme built through
  `BackOfficeAuthenticationBuilder.SchemeForBackOffice(...)`. The backoffice login screen only
  shows a button if an `umbraco-package.json` also declares an `authProvider` extension.
  Its `forProviderName` is the scheme name **with `Umbraco.` prepended**. If the button is
  missing, check the manifest before the C#.
- **Callback paths** (`/signin-oidc`, `/signin-google`, ...) go in `Umbraco:CMS:Global:ReservedPaths`
  so Umbraco doesn't try to route them as content. Register the same absolute callback URL, per
  environment, in the identity provider.
- **Auto-linking** creates Umbraco users on first SSO login and assigns user groups in
  `OnAutoLinking`. Map groups from IdP claims rather than giving everyone a fixed group, and
  never default to administrators. Auto-linked users have no local password. `DenyLocalLogin`
  then removes password login entirely, so keep one break-glass local admin, or the documented
  admin reset, available before turning it on.
- Keep claims copied into the backoffice identity small. They live in the auth cookie, and
  tokens belong in storage (the docs show how).
- Basic authentication with external providers or 2FA needs `BasicAuth:RedirectToLoginPage: true`.
  The browser's native prompt can't complete those flows.

## Two-factor authentication

2FA for users is an `ITwoFactorProvider` registered with
`AddTwoFactorProvider<T>(name)` on the backoffice identity builder, plus the client-side
activation UI the docs describe. Requiring 2FA for a group, as opposed to offering it, is a
separate decision. Settle it with the user, because the docs' sample only offers it.

## Settings worth setting explicitly

`Umbraco:CMS:Security:UserPassword` (length, character classes,
`MaxFailedAccessAttemptsBeforeLockout`), `UserDefaultLockoutTimeInMinutes` (defaults to 30 days),
`AllowPasswordReset`, `KeepUserLoggedIn`, `UserAllowConcurrentLogins`. If several sites share a
hostname (for example `localhost` on different ports), they share cookies. Give each site a unique
`AuthCookieName` **and** `BackOfficeTokenCookie:SiteName`, or signing in to one signs you out of
the other.

## Machines are API users, not people

A script, CI job or integration calling the Management API should use an **API user** with
Client Credentials, in a user group scoped to only what it needs. Never use a human editor's login.
Reuse the access token until it expires. Requesting one per call floods the token table.

**Related:** [authorization.md](authorization.md) (user groups and permissions),
[hardening.md](hardening.md).
