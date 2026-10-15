---
name: umbraco-security
description: >
  Secures Umbraco 17+ sites: HTTPS and reverse proxies, security headers and CSP, CORS for
  headless front ends, backoffice user and member authentication, authorization and protected
  content, and production hardening. Use this whenever the user asks to secure, harden or
  security-review an Umbraco site, fix pen-test or security health check findings, add a Content
  Security Policy or security headers, fix a blank backoffice after adding a CSP, force HTTPS or
  fix a redirect loop behind Azure Front Door, Cloudflare or a load balancer, find out why
  Production runtime mode won't start, enable CORS for a Delivery API or SPA front end, protect
  the Delivery API key or preview, add SSO, external login or 2FA, protect member-only pages,
  files or API endpoints, or restrict the backoffice.
  SKIP: non-Umbraco projects, Umbraco versions below 17, and Umbraco Cloud platform settings.
---

# Umbraco Security

An index for security work on an Umbraco site. Each row points at one reference file, which adds
what the docs pages leave out: where the code goes in Umbraco's pipeline, which settings
interact, and the mistakes that make a site look secured without being secured.

## How to use this index

- **Building a feature** (add CSP, enable CORS, add SSO): open the one matching reference file,
  fetch the docs page it names, then implement. The reference files give no full code samples
  on purpose: the docs carry the current API.
- **Auditing or hardening a site:** read [hardening.md](references/hardening.md) first. It is the
  checklist, and it links back to the other files for each item that fails.
- **A health check is failing:** the Health Check dashboard's security checks map to
  [security-headers.md](references/security-headers.md) (headers) and
  [https.md](references/https.md) (HTTPS and application URL).
- **Users or members?** Umbraco has two separate identity systems. *Users* sign in to the
  backoffice; *members* sign in to the website. They share no accounts, cookies, password rules
  or login pages. Settle which one the request is about before opening a reference file.

## Topics

| Goal | Reference |
|---|---|
| Force HTTPS, HSTS, TLS behind a proxy or load balancer, application URL | [https.md](references/https.md) |
| Content Security Policy, X-Frame-Options, nosniff, removing `Server` headers | [security-headers.md](references/security-headers.md) |
| Let a browser app on another origin call the Delivery API or custom APIs | [cors.md](references/cors.md) |
| Backoffice login: SSO and external providers, 2FA, password and lockout rules, API users | [authentication-users.md](references/authentication-users.md) |
| Website login: member registration and login, external providers, 2FA, Delivery API member tokens | [authentication-members.md](references/authentication-members.md) |
| Who can see what: public access, member-only controllers, Delivery API keys, user permissions | [authorization.md](references/authorization.md) |
| Production checklist: runtime mode, locking down `/umbraco`, rate limiting, secrets, uploads | [hardening.md](references/hardening.md) |

## Best practices that apply to every topic

- **Place middleware relative to `app.UseUmbraco()` deliberately.** Umbraco builds most of the
  pipeline itself: static files and media, then routing, then authentication. Middleware added
  in `Program.cs` before `app.UseUmbraco()`, or in an `UmbracoPipelineFilter`'s `PrePipeline`,
  sees every request, media and backoffice included. Each reference file says which slot its
  feature needs.
- **Treat the backoffice and the public site as two surfaces.** Policies such as a strict CSP,
  `frame-ancestors`, or an IP allow-list usually need different values under `/umbraco`. One
  site-wide value either breaks the backoffice or is too loose for the site.
- **Prove it on the wire.** A security change is done when an HTTP request shows it: the header
  is present, the redirect happens, the protected URL returns 401/403. A compiled change is not
  proof. Check `/umbraco` and a media URL as well as a content page.
- **Secrets stay out of `appsettings.json` in source control.** See
  [hardening.md](references/hardening.md) item 7 for what counts and where it goes.
- **Report honestly.** If the site could not be run, say which checks were not made rather than
  describing the configuration as verified.

## Version compatibility

Targets **Umbraco 17+**. The docs links point at the current (17) documentation. Type names,
configuration keys, routes and pipeline ordering were checked against the `Umbraco.Cms` 17.5.3
source. `ICspNonceService`, `UseUmbracoCspNonceInjection()` and the `asp-csp-nonce` tag helper
are recent; confirm they exist in the project's Umbraco version before relying on them.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. This skill ships no `assets/`, so no runtime gate applies: its
coverage tier is **Documented**, meaning an eval graded the guidance and no code was run.
