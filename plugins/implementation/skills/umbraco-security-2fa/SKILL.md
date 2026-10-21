---
name: umbraco-security-2fa
description: >
  Adds two-factor authentication (2FA/MFA) with an authenticator app (TOTP, Google or Microsoft
  Authenticator) to an Umbraco 17+ site, for backoffice users, website members, or both. Use this
  whenever the user asks to add, enable or require 2FA, MFA, two-step verification or an
  authenticator app for Umbraco editors or members, or to fix 2FA that is missing from the
  backoffice user menu, a member login that loops back to the login form after 2FA was enabled, a
  member profile page that crashes listing 2FA providers, or a site that won't start with "An item
  with the same key has already been added" after adding 2FA. Also use it when the user wants
  editors or members to confirm sign-in with a code from their phone, or mentions TOTP or one-time
  codes, even if they never say "2FA".
  SKIP: Umbraco Cloud's Umbraco ID MFA, MFA enforced by an external identity provider (SSO),
  non-Umbraco projects and Umbraco versions below 17.
---

# Two-factor authentication

Umbraco has two separate identity systems, and each gets 2FA its own way. Both use an
`ITwoFactorProvider` that you implement and register; what differs is the UI around it.

| | **A: Backoffice users** | **B: Website members** |
|---|---|---|
| Reference | [users.md](references/users.md) | [members.md](references/members.md) |
| Who signs in | Editors and admins at `/umbraco` | Visitors on the public site |
| Registered on | `BackOfficeIdentityBuilder` | `MemberIdentityBuilder` |
| UI you write | A JSON manifest; the backoffice supplies the screens | The login view's code step and a setup page |

### How to decide

Settle which identity system the request is about before writing code: "editors", "admins",
"backoffice" and "CMS login" mean A; "members", "customers", "my account" and "website login" mean
B. If the user wants both, do both: they coexist. Open the reference for each one you build and
start from its assets. The docs sample has mistakes that the references list and the assets fix.

## Version compatibility

Targets **Umbraco 17+**, checked against the `Umbraco.Cms` 17.5.3 source. The assets use the
third-party `GoogleAuthenticator` NuGet package, version 3.x (tested on 3.2.0).

## Best practices

- **The provider name is permanent.** It is stored with every enrolment. If it changes, existing
  enrolments no longer match a provider and are ignored, so those accounts sign in with a password
  alone and nobody is told.
- **One provider per identity system.** Users and members share a single provider list. Registering
  the same provider name on both builders fails at startup with a duplicate-key error.
- **Tighten the code window.** `GoogleAuthenticator` accepts codes up to five minutes either side by
  default. The assets pass 30 seconds.
- **The secret stays on the server.** The QR code is a `data:` URI generated in-process. Never build it
  with an online QR or chart service: the URL would carry the secret. A Content Security Policy on the
  setup page needs `img-src data:`.
- **Plan recovery before go-live.** There are no recovery codes. Agree who can reset a locked-out
  account; each reference says how.
- **Prove it with a sign-in.** It is done when an enrolled account is asked for a code and a wrong
  code is refused. A build that compiles proves neither. If the site can't be run, say which checks
  were not made.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Both approaches are **Asserted** by the runtime gate: `examples/users/`
(Clean host) signs a user in through the backoffice login API and its 2FA step;
`examples/members/` (blank host) enrols a member through the setup page, then signs in with a code.
