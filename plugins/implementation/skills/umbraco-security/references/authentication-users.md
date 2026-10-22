# Authentication: backoffice users

How editors and administrators sign in to `/umbraco`, and how machines authenticate to the
Management API. For website visitors, see [authentication-members.md](authentication-members.md).

SSO and external providers have their own skill, [`umbraco-security-external-logins`](../../umbraco-security-external-logins/SKILL.md), and so does
two-factor authentication, [`umbraco-security-2fa`](../../umbraco-security-2fa/SKILL.md). Use those for either.

## Fetch first (pick the page for the task)

| Task | Docs page |
|---|---|
| Password rules, lockout, session length, concurrent logins | [Security settings](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/securitysettings.md) |
| Check credentials against another store (LDAP, legacy DB) | [Replacing the basic username/password check](https://docs.umbraco.com/umbraco-cms/run-in-production/security/custom-password-check.md) |
| React to login/logout (audit, sign out of the IdP) | [BackOfficeUserManager and events](https://docs.umbraco.com/umbraco-cms/run-in-production/security/backofficeusermanager-and-notifications.md) |
| Machine access to the Management API | [API users](https://docs.umbraco.com/umbraco-cms/manage-and-publish-content/users-and-members/users/api-users.md) and [External access](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/management-api/external-access.md) |
| Locked out, or lost the admin password | [Locking of users and password reset](https://docs.umbraco.com/umbraco-cms/run-in-production/security/password-reset.md), [Reset admin password](https://docs.umbraco.com/umbraco-cms/run-in-production/security/reset-admin-password.md) |
| Password-protect a staging site with backoffice logins | [Basic authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/basic-authentication.md) |

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
