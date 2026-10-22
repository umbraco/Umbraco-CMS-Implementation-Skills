# Hardening checklist

Work through this list when auditing a site or preparing it for production. Report every item as
**done**, **not applicable** (with the reason), or **needs the user** (a decision or access you
don't have, such as IP ranges, IdP registration or hosting configuration). End with that status
list and the files changed.

## Fetch first

- [Lock down access to the Umbraco folder](https://docs.umbraco.com/umbraco-cms/run-in-production/security/lock-access-to-umbraco-folder.md):
  locking down `/umbraco` by IP on IIS.
- [Umbraco security settings](https://docs.umbraco.com/umbraco-cms/run-in-production/security/security-settings.md):
  pointers to password, routing and Forms settings.
- [Health Check](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md):
  run it first, and treat its results as the starting backlog.

## Checklist

1. **Runtime mode `Production`** (`Umbraco:CMS:Runtime:Mode`). Umbraco then refuses to boot
   unless the build is Release (JIT optimizer on), Models Builder mode is `Nothing`, `UseHttps` is
   `true` and `UmbracoApplicationUrl` is set ([https.md](https.md)). The startup error names the
   failing setting. Production mode also turns off Razor runtime compilation, so views must be
   compiled at build/publish time (no `RazorCompileOnBuild`/`RazorCompileOnPublish` set to `false`
   in the project), or templates return 404.
2. **HTTPS everywhere, HSTS on**: [https.md](https.md).
3. **Security headers and CSP**: the [`umbraco-security-headers`](../../umbraco-security-headers/SKILL.md) skill. Remove `Server` and
   `X-Powered-By`.
4. **Restrict `/umbraco` by IP or VPN** where editors work from known networks. Exempt the public
   endpoints that live under `/umbraco` (see *Docs corrections*), or the front end breaks. For a
   non-IIS host (nginx, Front Door, App Gateway WAF), apply the same rule there.
5. **Routing settings:** set `WebRouting:DisableAlternativeTemplates` and
   `WebRouting:DisableFindContentByIdPath` to `true` unless the site relies on them. Both default
   to `false`, and they let visitors render pages with other templates or enumerate nodes by ID.
6. **Accounts:** user password and lockout rules set explicitly, 2FA or SSO for administrators,
   no shared logins, and API users for integrations
   ([authentication-users.md](authentication-users.md)). Remove or disable the install-time
   admin account if it isn't a named person.
7. **Secrets out of source control:** connection strings, `DeliveryApi:ApiKey`, OAuth client
   secrets and SMTP credentials go in environment variables, user secrets or a vault
   ([Azure Key Vault](https://docs.umbraco.com/umbraco-cms/run-in-production/security/key-vault.md)).
8. **Rate limiting** for login, registration, password reset, form posts and public APIs:
   [API rate limiting](https://docs.umbraco.com/umbraco-cms/run-in-production/security/api-rate-limiting.md).
   It uses an `UmbracoPipelineFilter`'s `PostRouting` slot, so endpoint metadata is available.
   Behind a proxy, partition by the forwarded client IP, not the proxy's.
9. **Uploads and rich text:** register an `IFileStreamSecurityValidator`
   ([Server-side file validation](https://docs.umbraco.com/umbraco-cms/run-in-production/security/serverside-file-validation.md))
   if editors or members can upload. Consider an `IHtmlSanitizer`
   ([Sanitizing the RTE](https://docs.umbraco.com/umbraco-cms/run-in-production/security/serverside-sanitizing.md))
   when untrusted users can author rich text. Restrict `Content:DisallowedUploadedFileExtensions` /
   `AllowedUploadedFileExtensions`.
10. **Members and personal data:** sensitive fields marked, member-only pages protected, media
    exposure checked ([authorization.md](authorization.md)).
11. **Cookies:** the session cookie is `UMB_SESSION`. Umbraco registers it HttpOnly. To force
    `Secure`, add your own `AddSession` call; the last registration wins. Register it on
    `builder.Services` **before** `builder.Build()` (see *Docs corrections*). List the backoffice
    cookies in the site's cookie policy.
12. **Staging and preview environments:** front-end
    [basic authentication](https://docs.umbraco.com/umbraco-cms/run-in-production/security/basic-authentication.md)
    or IP restriction, plus `noindex`. Never reuse the production Delivery API key.
13. **Stay patched:** check the installed version against
    [Umbraco security advisories](https://github.com/umbraco/Umbraco-CMS/security/advisories)
    and the [Trust Center](https://umbraco.com/trust-center/). Upgrade within the major
    version promptly.
14. **Regulated hosting:** follow the
    [FIPS setup](https://docs.umbraco.com/umbraco-cms/run-in-production/security/setup-umbraco-for-a-fips-server.md)
    only if the user says the server must be FIPS-compliant.

## Docs corrections

Where the docs pages and Umbraco 17 disagree, follow this list:

- **[Cookies](https://docs.umbraco.com/umbraco-cms/run-in-production/security/cookies.md)** says to
  add the `AddSession` call "after `Build();`". `Program.cs` has two `Build()` calls: it means the
  Umbraco builder chain's `.Build()`. After `builder.Build()`, services can't be registered and
  the call throws. Register it before `builder.Build()`.
- **[Lock down access to the Umbraco folder](https://docs.umbraco.com/umbraco-cms/run-in-production/security/lock-access-to-umbraco-folder.md)**:
  the IIS rule exempts `umbraco/api`, `umbraco/surface` and `umbraco/delivery` only. It still
  blocks the basic-auth login pages (`/umbraco/basic-auth`) and the Management API
  (`/umbraco/management/api`) that API users call. Add the ones the site uses to the exemption.
