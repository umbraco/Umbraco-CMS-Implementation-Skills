# Approach A: backoffice users

Authenticator-app 2FA for editors and administrators signing in to `/umbraco`. Choose this when
the request is about the backoffice; for website visitors, use [members.md](members.md).

## Building blocks (fetch the docs first)

Fetch the "Two-factor authentication for Users" section of
[Two-factor authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/two-factor-authentication.md).
The assets in this skill are that sample with the fixes listed under *Docs corrections*.

- **Provider + composer:** [`assets/UmbracoUserAppAuthenticator.cs`](../assets/UmbracoUserAppAuthenticator.cs)
  implements `ITwoFactorProvider` and registers it on a `BackOfficeIdentityBuilder`.
- **Backoffice manifest:** [`assets/umbraco-package.json`](../assets/umbraco-package.json) declares an
  `mfaLoginProvider` extension. The backoffice lists only providers that have one. Without it the
  avatar menu has no **Configure Two-Factor** option at all, even though the server provider works.

## Steps

1. **Discover the project:** root namespace, Umbraco version, and whether any external login
   provider sets `DenyLocalLogin`. If one does, the backoffice hides 2FA setup for everyone, and MFA
   belongs in that identity provider instead.
2. **Install the package:** `dotnet add package GoogleAuthenticator`. It works with Google, Microsoft and other TOTP authenticator apps.
3. **Write the provider:** copy the `.cs` asset. Replace `<Namespace>`, and `<IssuerName>` with the
   name editors should see in their app, for example "Acme CMS".
4. **Write the manifest** to `App_Plugins/TwoFactorProviders/umbraco-package.json` (or `wwwroot/App_Plugins/...`).
   `forProviderName` must equal the provider's `Name` exactly. A mismatch shows the provider with a red
   "not configured on the server" badge, and enabling it fails.
5. **Build and restart.**
6. **Verify.** In the backoffice: avatar → **Configure Two-Factor** → enable, scan, enter a code;
   sign out and in again, and the code prompt appears. Without a browser, enrol a test user, then
   `POST /umbraco/management/api/v1/security/back-office/login` with `{"username","password"}`.
   An enrolled user gets `402` and the provider in `enabledTwoFactorProviderNames`. Then
   `POST .../back-office/verify-2fa` with `{"provider","code"}` on the same cookie session returns `200`.

## Decisions to settle with the user

- **Offer or require?** Umbraco only offers 2FA to users. Nothing forces an editor to enrol. If it
  must be mandatory, put the backoffice behind an identity provider that enforces MFA:
  [External login providers](https://docs.umbraco.com/umbraco-cms/run-in-production/security/external-login-providers.md).
- **Recovery.** A user with access to the Users section can switch off
  another user's provider (the user's **Configure 2FA** action, no code needed). Make sure more than
  one person can do that.
- **SSO users skip it.** `Umbraco:CMS:Security:UserBypassTwoFactorForExternalLogins` defaults to
  `true`, so users signing in through an external provider are never asked for an Umbraco code.

## Beyond the authenticator app

- **Codes by email or SMS:** handle `UserTwoFactorRequestedNotification` and send a code. Weaker than
  an app, but better than nothing; the docs page covers it.
- **Custom activation or login screens** (for providers that aren't a typed code): the docs page's
  "Customizing the 2FA experience" section, `element` in the manifest and `IBackOfficeTwoFactorOptions`.

## Docs corrections

- The "final form" `umbraco-package.json` sample contains a `// This line is the only change`
  comment. Umbraco's manifest parser rejects comments, so that file fails to load. Ship plain JSON.
- The composer sample omits `using Umbraco.Cms.Core.DependencyInjection;` and `using Umbraco.Extensions;`.
  It compiles only where `Umbraco.Cms` adds them as global usings, not in a class library.

## Done

Tell the user which files were added, the provider name (and that it must never change), that
each editor enrols from the avatar menu, and who can reset an editor who loses their phone.
