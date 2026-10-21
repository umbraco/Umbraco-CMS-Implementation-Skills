---
name: umbraco-security-external-logins
description: >
  Adds external login providers (single sign-on through OpenID Connect or OAuth: Microsoft Entra
  ID, Google, Okta, Auth0, Keycloak, GitHub) to Umbraco 17+, for backoffice users and for website
  members, with safe auto-linking and group mapping. Use this whenever the user asks to add SSO,
  "Sign in with Microsoft/Google", Entra ID or Azure AD login to the Umbraco backoffice, social or
  external login for members, auto-link or auto-create users or members from an identity
  provider, map identity-provider groups or roles to Umbraco user groups, disable password login,
  or fix an external login that shows no button, fails with "No authentication handler is
  registered", "The Name value cannot be null" or an unlinked-account error, or creates users with
  no sections. SKIP: non-Umbraco projects, Umbraco versions below 17, Umbraco Cloud's built-in
  external login provider feature, and two-factor authentication on its own.
---

# External login providers

Umbraco has two separate identity systems, and an external login is wired into one of them at a
time. **Users** sign in to the backoffice at `/umbraco`; **members** sign in to the website. They
use different builders, scheme prefixes, callback paths and auto-link options, and a provider set
up for one does nothing for the other.

| | **A — Users** (backoffice) | **B — Members** (website) |
|---|---|---|
| Reference | [users.md](references/users.md) | [members.md](references/members.md) |
| Asset | [`BackOfficeExternalLogin.cs`](assets/BackOfficeExternalLogin.cs) + [`umbraco-package.json`](assets/umbraco-package.json) | [`MemberExternalLogin.cs`](assets/MemberExternalLogin.cs) |
| Registered with | `AddBackOfficeExternalLogins`, scheme prefix `Umbraco.` | `AddMemberExternalLogins`, scheme prefix `UmbracoMembers.` |
| Login button | `authProvider` manifest in `App_Plugins` | A form per provider in the site's login view |
| Auto-link creates | A user, in the user groups you map | A member, of one Member Type, in default member groups |
| Coverage tier | **Asserted** | **Asserted** |

### How to decide

Ask who signs in. Editors and administrators → A. Site visitors, customers, extranet members → B.
Both → implement both, as two schemes with different names and callback paths; one app
registration at the provider can serve both if it lists both redirect URIs. A headless front end
that needs signed-in members is B, finished through the Delivery API (see members.md).

The assets use the generic OpenID Connect handler, which works with any OIDC provider. If the user
wants a provider-specific handler (`AddMicrosoftAccount`, `AddGoogle`), keep the Umbraco half of the
asset and swap the handler as the reference describes.

Before writing any code, open the approach's reference and start from its asset: the reference
holds the placeholders, the configuration and the traps the asset already handles.

## Version compatibility

Targets **Umbraco 17+**. Type names, callbacks and defaults were checked against the
`Umbraco.Cms` 17.5.3 binaries. Lightweight external members (`ExternalOnly`) need **17.4+**.

## Best practices

- **Auto-linking matches on email, including existing accounts.** With auto-linking on, the first
  provider sign-in links to any existing user or member with the same email. If the provider will
  assert an email the person doesn't own (an unverified address, or a multi-tenant Entra ID app
  accepting any tenant), that is an account takeover. Pin the authority to your tenant, restrict
  email domains for the backoffice, and refuse `email_verified: false`. Linking also skips
  Umbraco's approval and lockout check, so refuse disabled accounts yourself. The assets do all
  of this.
- **Groups come from the provider, never from a default.** Give the backoffice no default user
  groups, map provider groups or roles to Umbraco group aliases, and sync them on every sign-in.
- **Secrets stay out of source control.** Read `ClientSecret` from configuration backed by user
  secrets, environment variables or a vault.
- **One callback path per scheme**, registered at the provider as an absolute URL per environment,
  and added to `Umbraco:CMS:Global:ReservedPaths`.
- **Treat the scheme name as permanent.** Changing it orphans every existing link (see users.md).
- **Prove it on the wire.** The **Verify** checks in each reference file use requests, not just a
  build.

For two-factor authentication, password rules and other identity hardening, use the
`umbraco-security` skill if it is installed.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Both approaches are compiled from `assets/` and asserted over HTTP by
the `dotnet test` gate on the Clean reference host (`examples/users-backoffice`,
`examples/members-website`).
