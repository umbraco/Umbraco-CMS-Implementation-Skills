---
name: umbraco-security
description: >
  Router for securing an Umbraco 17+ site: sends each security task to the skill that owns it
  (umbraco-security-headers, umbraco-security-external-logins, umbraco-security-2fa,
  umbraco-configuration). Use this whenever the user asks to secure, harden or security-review an
  Umbraco site, fix pen-test or security health check findings, or asks a security question that
  spans several areas, such as a pre-launch audit. Also use it to force HTTPS or fix a redirect
  loop behind Azure Front Door, Cloudflare or a load balancer, enable CORS for a Delivery API or
  SPA, protect the Delivery API key or preview, protect member-only pages, files or endpoints, or
  restrict the backoffice. SKIP: a task that is only a CSP/header, SSO, 2FA or appsettings change
  (use that skill), non-Umbraco projects, Umbraco below 17, and Umbraco Cloud settings.
---

# Umbraco Security

## Route first

Split the request into topics, then open the skill or file for each one before writing anything:
the routed skills ship runtime-tested code, and the reference files here add what the docs pages
leave out. An audit or "secure the site" request usually touches several rows. For a routed skill, read the
reference for its approach too, and carry its pitfalls into your answer: a plan that only names
the skill leaves the reader to rediscover them.

| Topic | Go to |
|---|---|
| Content Security Policy, X-Frame-Options, nosniff, Referrer-Policy, removing `Server`/`X-Powered-By`, a blank backoffice after adding a CSP | The [`umbraco-security-headers`](../umbraco-security-headers/SKILL.md) skill |
| SSO and external login providers (Entra ID, Google, Okta, any OpenID Connect), for users or members | The [`umbraco-security-external-logins`](../umbraco-security-external-logins/SKILL.md) skill |
| Two-factor authentication, for users or members | The [`umbraco-security-2fa`](../umbraco-security-2fa/SKILL.md) skill |
| Production runtime mode and why it won't boot, `UmbracoApplicationUrl`, secrets in environment variables or Key Vault | The [`umbraco-configuration`](../umbraco-configuration/SKILL.md) skill |
| HTTPS redirection, what `UseHttps` does and doesn't cover, HSTS, TLS terminated by a proxy, load balancer or CDN (forwarded headers, origin lockdown) | [https.md](references/https.md) |
| A browser app on another origin calling the Delivery API or custom APIs | [cors.md](references/cors.md) |
| Member login, member-only pages, files and endpoints, Delivery API keys, preview and member tokens, backoffice permissions and API users | [authorization.md](references/authorization.md) |
| Auditing or hardening a site before launch | [hardening.md](references/hardening.md): the checklist, which routes each item onward |

*Users* sign in to the backoffice; *members* sign in to the website. They share no accounts,
cookies, password rules or login pages, so settle which one a login question is about first.

## Rules that apply to every topic

- **Place middleware relative to `app.UseUmbraco()` deliberately.** Umbraco builds most of the
  pipeline itself: static files and media, then routing, then authentication. Middleware added
  before `app.UseUmbraco()`, or in an `UmbracoPipelineFilter`'s `PrePipeline`, sees every
  request, media and backoffice included.
- **Treat the backoffice and the public site as two surfaces.** A CSP, `frame-ancestors` or an
  IP allow-list usually needs a different value under `/umbraco`, and public endpoints such as
  the Delivery API also live under `/umbraco`.
- **Prove it on the wire.** A security change is done when an HTTP request shows it: the header
  is present, the redirect happens, the protected URL returns 401/403. If the site could not be
  run, say which checks were not made instead of calling the configuration verified.

## Version compatibility

Targets **Umbraco 17+**. Type names, configuration keys, routes and pipeline ordering were checked
against the `Umbraco.Cms` 17.5.3 source.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. [`evals/README.md`](evals/README.md) has the inputs, the run-prompt
rules and the grading notes. This skill ships no `assets/`, so no runtime gate applies: its
coverage tier is **Documented**, meaning an eval graded the guidance and no code was run. The
skills it routes to carry their own runtime gates.
