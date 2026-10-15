# Approach B: website members

Authenticator-app 2FA for visitors who sign in to the website. Choose this when the request is
about members; for backoffice editors, use [users.md](users.md).

## Prerequisites

Member login and a members-only page must already work: a login page, and at least one page behind
public access that uses it as its login page. If they don't, build them first from
[Member registration and login](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/tutorials/members-registration-and-login.md).

## Building blocks (fetch the docs first)

Fetch the "Two-factor authentication for Members" section of
[Two-factor authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/two-factor-authentication.md).
The assets fix that sample and add the two pieces it leaves out: the login step and a safe way to
turn 2FA off.

| Asset | Goes to | Does |
|---|---|---|
| [`UmbracoMemberAppAuthenticator.cs`](../assets/UmbracoMemberAppAuthenticator.cs) | project root or a `Security/` folder | Provider, setup model, and composer registering it on a `MemberIdentityBuilder` |
| [`MemberLogin.cshtml`](../assets/MemberLogin.cshtml) | `Views/Partials/` | Login form, plus the code form shown after a correct password |
| [`TwoFactorSetup.cshtml`](../assets/TwoFactorSetup.cshtml) | `Views/Partials/` | QR code and code entry to turn 2FA on; code entry to turn it off |
| [`MemberTwoFactorController.cs`](../assets/MemberTwoFactorController.cs) | `Controllers/` | Turns 2FA on through the checked service and off only with a valid code; closes Umbraco's own unchecked actions |

## Steps

1. **Discover the project:** root namespace, the login page's template and the partial it renders,
   the members-only page that will host the setup, and whether a backoffice 2FA provider is
   already registered (search for `AddTwoFactorProvider`).
2. **Install the package:** `dotnet add package GoogleAuthenticator`.
3. **Write the provider:** copy the `.cs` asset. Replace `<Namespace>`, and `<IssuerName>` with the
   site name members should see in their app.
4. **Add the code step to the login view.** After a correct password, `UmbLoginController` renders
   the same page again with the member's providers in `ViewData`. The view must check
   `ViewData.TryGetTwoFactorProviderNames(...)` and post a code to
   `UmbTwoFactorLoginController.Verify2FACode`. Without that branch the login form reappears with no
   error, and an enrolled member can never sign in. Either use `MemberLogin.cshtml`, or merge its
   first branch into the existing login partial. Keep the `returnUrl` field: without it a member who
   started on a protected page lands on the login page after verifying.
5. **Add the setup partial** to the members-only page and the controller to the project. Replace
   `<Namespace>` in the partial's `@using`.
6. **Build, then verify end to end:** sign in as a test member, open the members-only page, scan,
   enter a code. Sign out, sign in again: the code form appears; a wrong code is rejected; the right
   one lands on the page originally requested. Check the member's **Two-factor authentication** box
   in the backoffice member editor.

## Docs corrections

- **Users and members share one provider list.** `GetProviderNamesAsync` returns every registered
  `ITwoFactorProvider`, backoffice ones included. The docs view loops over all of them and calls the
  user provider's setup with a member key, which throws. The setup partial keeps only providers in
  the member `IdentityOptions` token map.
- **Umbraco's own setup actions are unchecked.** `UmbTwoFactorLoginController.Disable` needs no code,
  and `ValidateAndSaveSetup` saves another secret even when 2FA is already on, so a hijacked session
  can remove the second factor or add its own. Pointing the forms elsewhere is not enough: Umbraco
  routes its own surface controllers, so both still answer under `/umbraco/surface/UmbTwoFactorLogin/`.
  The asset's controller uses `ValidateAndSaveAsync` and `DisableByCodeAsync`, and its filter returns 404
  for the two built-in actions.
- **The sample provider** dereferences the member without a null check and hard-codes the issuer
  `testingOn15`.
- **A failed setup code shows a new QR code**, because the page asks for fresh setup data. Tell
  members to delete the half-added entry from their app and scan again.

## Decisions to settle with the user

- **Offer or require?** Umbraco only offers 2FA. To require it, check
  `IMemberTwoFactorLoginService.GetProviderNamesAsync` after login and send members with nothing
  enabled to the setup page.
- **Recovery.** An editor can clear a member's 2FA by unticking
  **Two-factor authentication** in the member editor. Agree who handles "I lost my phone" requests and
  how they confirm identity.
- **External logins skip it.** `Umbraco:CMS:Security:MemberBypassTwoFactorForExternalLogins`
  defaults to `true`.
- **Headless front ends** sign members in on an Umbraco-rendered login page, so the same login partial
  applies. External logins through the Delivery API always bypass 2FA in Umbraco 17.
- **Codes by email or SMS:** handle `MemberTwoFactorRequestedNotification`; weaker than an app.

## Done

Tell the user which files were added and where the partials are rendered, the provider name (and
that it must never change), how members enrol and turn 2FA off, and how an editor resets a member.
