# Security headers

Add response headers that limit what a browser lets a page do: Content Security Policy,
click-jacking protection and MIME-sniffing protection. Also remove headers that reveal the
technology stack. Umbraco's Health Check dashboard reports on each of these.

## Fetch first

- [Content Security Policy (CSP)](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/contentsecuritypolicy.md):
  `ICspNonceService`, `UseUmbracoCspNonceInjection()`, the backoffice sources a CSP must allow.
- [Click-Jacking Protection](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/clickjackingprotection.md)
- [Content/MIME Sniffing Protection](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/contentsniffingprotection.md)
- [X-XSS-Protection](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/crosssitescriptingprotection.md):
  the fix is to **not** send it.
- [Excessive Headers](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/excessiveheaders.md)
- HSTS is covered in [https.md](https.md).

## Where the middleware goes

Headers must be set before the response starts, so add the middleware **before**
`app.UseUmbraco()` in `Program.cs`. For something reusable or packaged, add it in an
`UmbracoPipelineFilter`'s `PrePipeline`, registered from a composer through
`Configure<UmbracoPipelineOptions>`. Middleware added **after** `UseUmbraco()` never sees most requests.

## The rules the docs don't spell out

- **The backoffice needs its own policy.** The docs' CSP example is the minimum the *backoffice*
  needs: a script nonce, `'unsafe-inline'` styles, `blob:`/`data:` images, and the Umbraco news
  dashboard and marketplace hosts. A front end usually needs other sources (analytics, fonts, a
  CDN) and should not inherit the backoffice's `'unsafe-inline'`. Branch on
  `context.Request.Path.StartsWithSegments("/umbraco")` and send a policy for each surface.
- **The nonce is per request, and the backoffice is the only thing tagged automatically.** For
  your own inline `<script>`/`<style>` in Razor views, add the `asp-csp-nonce` attribute (Umbraco's
  `CspNonceTagHelper`, in `Umbraco.Cms.Web.Common`). That gives the tag the same nonce as the
  header. Never hard-code a nonce or cache a page's HTML with a nonce baked in. Output caching
  and a per-request nonce conflict, so decide which pages get which.
- **Send one CSP header.** `Response.Headers.Append` next to NWebsec, a CDN rule or a second
  middleware produces two `Content-Security-Policy` headers. The browser enforces both, so the
  effective policy is the stricter combination and things break unpredictably. With NWebsec, call
  `UseUmbracoCspNonceInjection()` **after** `UseCsp()` so the nonce goes into NWebsec's header.
- **Roll out with `Content-Security-Policy-Report-Only` first.** Run it, read the violations in
  the browser console (or a `report-to` endpoint), and only then switch to the enforcing header.
  `UseUmbracoCspNonceInjection()` handles both header names.
- **Framing:** use `X-Frame-Options: SAMEORIGIN`, as the docs do, or CSP `frame-ancestors 'self'`.
  If you tighten this to `DENY` or `'none'`, test backoffice preview before shipping.
- **Don't add `X-XSS-Protection`.** It is deprecated, and the health check flags it when present.
- **`Server`/`X-Powered-By` are added by the host, not by Umbraco.** Middleware can't remove them.
  Use `builder.WebHost.UseKestrel(o => o.AddServerHeader = false)` on Kestrel, or `web.config` on
  IIS.
- `Referrer-Policy` and `Permissions-Policy` have no health check or docs page, so they're easy to
  forget. Set them in the same middleware (for example `strict-origin-when-cross-origin`).

## Verify

- `curl -I https://<site>/` and `curl -I https://<site>/umbraco` → each shows its own CSP, one
  `Content-Security-Policy` header per response, `X-Content-Type-Options: nosniff`, and the
  framing header.
- Sign in to the backoffice with the browser console open and confirm there are no CSP violations.
- The Health Check dashboard's security group passes.

**Related:** [cors.md](cors.md) (CORS is not a security header and doesn't
replace one).
