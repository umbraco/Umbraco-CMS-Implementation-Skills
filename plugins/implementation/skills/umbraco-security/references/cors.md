# CORS

Let a browser app on another origin, such as a Next.js/Nuxt front end or a SPA on its own domain,
call the Content Delivery API, Media Delivery API or your own API controllers.

## Fetch first

- ASP.NET Core [Enable CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors) is the
  source for the policy API. **Umbraco has no front-end CORS setting and no CORS docs page**, so
  don't invent `Umbraco:CMS:*` keys for it.
- [Content Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api.md)
  lists the request headers a client sends.

## What Umbraco already does

Umbraco registers a CORS policy in exactly one case: when `Umbraco:CMS:Security:BackOfficeHost`
is set, because the backoffice client is served from elsewhere (a Vite dev server, a proxied host).
Umbraco applies it with an `UmbracoPipelineFilter` whose `PrePipeline` calls `app.UseCors(<policy>)`
with **no path scoping**, and the policy allows any header, any method and credentials. So setting
`BackOfficeHost` to a front end's origin does make its CORS errors go away, but only by giving that
origin credentialed cross-origin access to every path, the Management API included, and by
registering it as a backoffice login host. Never use it as a front-end CORS setting. Copy the
filter pattern for your own policy instead, scoped as below. A public site should use the Delivery
API, and a server-side integration should use an API user (see
[authentication-users.md](authentication-users.md)).

## Building the policy

1. `services.AddCors(o => o.AddPolicy("<name>", ...))` in a composer or `Program.cs`.
2. Apply it in `PrePipeline` (or before `app.UseUmbraco()`) so preflight `OPTIONS` requests are
   answered before Umbraco's routing.
3. **Scope it to the API paths.** Wrap it in
   `app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/umbraco/delivery/api"), b => b.UseCors("<name>"))`,
   adding your own API prefix. Otherwise every Razor page and the backoffice answer cross-origin
   requests too.
4. **Origins:** list exact origins (`https://www.example.com`): scheme and host, no path, no
   trailing slash. Read them from configuration so staging and production can differ. Keep a
   `http://localhost:3000`-style origin for development in `appsettings.Development.json`, not in
   the production list.
5. **Headers:** list the headers the browser actually sends in `WithHeaders(...)`. Usually that is
   `Accept-Language`, `Accept-Segment` and `Start-Item`, plus `Authorization` for member access
   tokens and `Content-Type` for JSON bodies. A missing header shows up as a preflight failure and
   is commonly misread as an auth bug. Leave out `Api-Key` and `Preview`. They belong to
   server-side callers, and allowing them from the browser invites someone to put the key there.
   To show private or preview content, call the API from the front end's server side and keep
   the key there (see [authorization.md](authorization.md)).
6. **Credentials:** `AllowCredentials()` only if the browser must send cookies. ASP.NET Core
   rejects it combined with `AllowAnyOrigin()`. Bearer tokens in `Authorization` don't need it.
   Don't work around that rejection with `SetIsOriginAllowed(_ => true)` plus `AllowCredentials()`:
   that lets any site read responses made with the member's cookies.

## Misconceptions to correct

- **CORS is not access control.** It only governs what *browsers* let scripts read. `curl`,
  server-side code and attackers ignore it. Protecting content is the job of the Delivery API key
  or member authorization ([authorization.md](authorization.md)).

## Verify

```bash
curl -i -X OPTIONS https://<site>/umbraco/delivery/api/v2/content \
  -H "Origin: https://www.example.com" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: authorization,accept-language"
```

The response should include `Access-Control-Allow-Origin: https://www.example.com` and the
requested headers. Repeat with a non-allowed origin: there should be no
`Access-Control-Allow-Origin`. Repeat against a content page: it should have none either.

**Related:** [authorization.md](authorization.md), [authentication-members.md](authentication-members.md)
(token flows from a SPA).
