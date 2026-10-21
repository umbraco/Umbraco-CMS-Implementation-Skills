---
name: umbraco-security-headers
description: >
  Adds security headers to an Umbraco 17.3+ site: a Content Security Policy using Umbraco's
  per-request nonce, with separate backoffice and website policies, plus X-Frame-Options,
  X-Content-Type-Options, Referrer-Policy and Permissions-Policy. Two approaches: custom
  middleware (default) or NWebsec. Use this whenever the user asks to add a CSP or security headers
  to Umbraco, fix the Health Check security group (CSP, click-jacking, MIME sniffing,
  X-XSS-Protection, excessive headers), fix a blank backoffice or "Refused to execute inline
  script" errors after adding a CSP, put a nonce on an inline script (asp-csp-nonce), weigh
  'strict-dynamic', make a CSP work with output caching, roll out report-only, or act on pen-test
  findings about headers.
  SKIP: non-Umbraco projects, Umbraco below 17.3, and other security work (HTTPS/HSTS, CORS,
  login, authorization).
---

# Security Headers

Two supported ways to send a CSP and the other security headers:

| | **A — Custom middleware** (default) | **B — NWebsec** |
|---|---|---|
| Reference | [approach-a-middleware.md](references/approach-a-middleware.md) | [approach-b-nwebsec.md](references/approach-b-nwebsec.md) |
| Source | Verified code in `assets/` | The docs' NWebsec sample, scoped per surface |
| Dependencies | None | `NWebsec.AspNetCore.Middleware` |
| Wiring | A composer adds it to Umbraco's pipeline | Program.cs, before `app.UseUmbraco()` |
| Best for | New CSP work, packages, sites without NWebsec | Sites that already use NWebsec |

### How to decide

Default to A. Choose B when the project already configures headers with NWebsec: extend that
setup rather than adding a second source of the same headers. Never run both.

**Before writing any code, open the reference for the approach you choose,** and for A start from
the files in `assets/`. The steps, and the order the headers must be set in, are only there.

## Version compatibility

Targets **Umbraco 17.3+**. `ICspNonceService`, `UseUmbracoCspNonceInjection()` and the
`asp-csp-nonce` tag helper first ship in 17.3.0, so check the `Umbraco.Cms` version first. On 17.0
to 17.2 there is no nonce to use, so recommend upgrading before adding a CSP.

## Best practices

- **Two surfaces, two policies.** The backoffice under `/umbraco` needs `script-src 'self'` plus
  the nonce, `style-src 'self' 'unsafe-inline'`, `img-src 'self' blob: data:
  https://news-dashboard.umbraco.com` and `frame-src 'self' https://marketplace.umbraco.com`. The
  website should get none of the Umbraco hosts and no `'unsafe-inline'`, except in `style-src`
  when inline `style="…"` attributes (which can't take a nonce) can't be moved to CSS. Browsers
  ignore `'unsafe-inline'` in a directive that also has a nonce, so in that case drop the nonce
  from `style-src`. Its own sources (analytics, fonts, CDN) don't belong in the backoffice's.
- **The nonce is per request.** Umbraco tags its backoffice import map with it. For your own
  inline `<script>`/`<style>`, add `asp-csp-nonce` and register the tag helper with
  `@addTagHelper *, Umbraco.Web.Common`. Note the assembly is `Umbraco.Web.Common`, not
  `Umbraco.Cms.Web.Common`: with the wrong name the attribute renders literally and the script is
  blocked. Never hard-code a nonce, and never add `'unsafe-inline'` to `script-src` to fix a
  violation.
- **Keep `'self'` in the backoffice `script-src`, and don't add `'strict-dynamic'`.** In 17.5.3
  the backoffice entry script carries no nonce (it registers the tag helper under the wrong
  assembly name too) and loads only because of `'self'`, which `'strict-dynamic'` disables.
- **Output caching.** Umbraco's website output cache replays the header and HTML it stored, so a
  cached page reuses one nonce for every visitor until the entry expires. On cached pages, prefer a
  hash or an external file for inline scripts.
- **Send one of each header.** A second `Content-Security-Policy` (from `Append` next to NWebsec,
  a second middleware, or a CDN rule) is enforced as well, and it has no nonce. Check what the
  CDN or proxy adds before adding the same header in the app.
- **Roll out with `Content-Security-Policy-Report-Only`,** read the violations in the browser
  console, then switch to enforcing. The CSP health check only passes on the enforcing header.
- **Framing.** Use `X-Frame-Options: SAMEORIGIN` with `frame-ancestors 'self'`. Backoffice preview
  shows the page in a same-origin iframe, so `DENY` or `'none'` breaks preview.
- **Don't send `X-XSS-Protection`.** It is deprecated, and the health check flags it with any
  value, `0` included.
- **`Server` and `X-Powered-By` come from the host,** so middleware can't remove them. Follow
  [Excessive Headers](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/excessiveheaders.md)
  (Kestrel `AddServerHeader = false`, or `web.config` on IIS).

## Verify

A header change is done when a response shows it, not when it compiles:

- `curl -sI https://<site>/` and `curl -s -D - -o /dev/null https://<site>/umbraco` (GET, because
  `HEAD /umbraco` returns 404): each response has one CSP, its own policy, and the other headers.
  Check a media or static file and a 404 too.
- The nonce in the `/umbraco` header equals the `nonce` on `<script type="importmap">` in its HTML.
  On a site page, any `asp-csp-nonce` tag renders as `nonce="…"` with the header's value.
- Sign in to the backoffice with the console open, open a document and preview it. Expect no CSP
  violations.
- The Health Check security group needs `UmbracoApplicationUrl` set, because it requests that
  URL.

If the site can't be run, say which of these checks weren't made.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. [`evals/README.md`](evals/README.md) has the inputs, the run-prompt rules
and the grading notes. Approach A is **Asserted** by the `dotnet test` gate
(`examples/approach-a-middleware/`). Approach B ships no code, so its tier is **Documented**.
