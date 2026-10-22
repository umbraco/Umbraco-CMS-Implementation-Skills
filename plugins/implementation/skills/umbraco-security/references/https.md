# HTTPS

Serve every request over HTTPS and make Umbraco aware of it. HTTPS is set up in three places
that don't know about each other: Umbraco configuration, the ASP.NET Core pipeline, and the
host (IIS, a reverse proxy, a load balancer, a CDN).

## Fetch first

- [HTTPS](https://docs.umbraco.com/umbraco-cms/run-in-production/security/ssl-https.md):
  `Umbraco:CMS:Global:UseHttps`, `app.UseHttpsRedirection()`, the IIS rewrite rule.
- [Health check: HTTPS Configuration](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/httpsconfiguration.md)
- [Health check: Strict-Transport-Security Header](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/stricttransportsecurityheader.md)
- [Fixed Application URL](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/fixedapplicationurl.md)
- Behind a proxy: ASP.NET Core's
  [forwarded headers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer)
  guidance. The Umbraco docs don't cover this.

## What `UseHttps` does and doesn't do

`UseHttps` (which defaults to `true` in Umbraco 17) covers the **backoffice** only. It makes the
backoffice auth and token cookies secure-only, makes the backoffice's OpenID Connect endpoints refuse
plain HTTP, and makes the links in Umbraco's notification emails use `https`. It does **not**
redirect anything, front-end pages included. Front-end redirection needs `app.UseHttpsRedirection()` before
`app.UseUmbraco()`, or a host-level rule (IIS rewrite, proxy, CDN). Pick **one** place to
redirect, because two layers that disagree produce redirect loops.

## Production runtime mode enforces it

A site that won't start after switching to Production usually has `Global:UseHttps` set to
`false` (the default is `true`, but templates and upgraded sites often set it `false` for
development) or no `WebRouting:UmbracoApplicationUrl`. The full list of boot requirements is in
[hardening.md](hardening.md) item 1.

## Behind TLS-terminating proxies (Azure Front Door, App Gateway, Cloudflare, nginx, Docker)

The proxy talks plain HTTP to the app, so ASP.NET Core sees every request as `http`. As a
result, `UseHttpsRedirection()` loops, `UseHttps` denies backoffice authorization, and generated
URLs come out as `http://`. The fix is `ForwardedHeadersOptions` plus
`app.UseForwardedHeaders()`, called **first**: before redirection and before `app.UseUmbraco()`.
Trusting forwarded headers from anywhere lets clients spoof their scheme and IP, so trust only the
real proxy. Either list its addresses in `KnownProxies`/`KnownIPNetworks`, or, for CDNs with large
or changing ranges such as Front Door and Cloudflare, clear those lists **and** make the origin
unreachable except through the proxy. For Front Door that means an App Service access restriction
on the `AzureFrontDoor.Backend` service tag plus the `X-Azure-FDID` header. Clearing the lists
without locking the origin is the common mistake. Set `UmbracoApplicationUrl` to the public
`https://` URL so background work (emails, scheduled publishing) doesn't build URLs from an
internal address.

## HSTS

`app.UseHsts()` belongs in the non-development branch only. Browsers cache HSTS, and ASP.NET Core
excludes `localhost` by default anyway. Start with a short `MaxAge` and raise it once every
subdomain is confirmed HTTPS-only. Add `IncludeSubDomains` or `Preload` only when that's true for
every subdomain of the domain, not just this site: preload lists are slow to leave. If a CDN or
proxy already adds HSTS, don't add it twice with different values.

## Verify

- `curl -I http://<site>/` → one redirect hop to `https://`, no loop.
- `curl -I https://<site>/` → `Strict-Transport-Security` present (non-development only).
- The Health Check dashboard's HTTPS and application-URL checks pass.

**Related:** the [`umbraco-security-headers`](../../umbraco-security-headers/SKILL.md) skill (HSTS sits alongside the other headers),
[hardening.md](hardening.md) (runtime mode).
