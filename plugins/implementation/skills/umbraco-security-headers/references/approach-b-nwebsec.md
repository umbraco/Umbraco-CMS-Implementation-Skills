# Approach B — NWebsec

## When to choose this approach

When `Program.cs` already configures NWebsec. Otherwise use [Approach A](approach-a-middleware.md).

## Building blocks (fetch the docs before implementing)

- **CSP with NWebsec and the nonce injection:**
  [Content Security Policy (CSP)](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/contentsecuritypolicy.md),
  section *Adding a CSP using NWebsec*. Its sample is a single site-wide policy; split it as below.
- **Framing and MIME sniffing with NWebsec:**
  [Click-Jacking Protection](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/clickjackingprotection.md)
  and
  [Content/MIME Sniffing Protection](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/contentsniffingprotection.md)

## Steps

1. **Discover the project.** Read the existing NWebsec calls and find every other source of the
   same headers: `app.Use` blocks that `Append` headers, `web.config`, the CDN. Each header should
   come from NWebsec only.
2. **Split the CSP by surface** with two `UseWhen` branches, both before `app.UseUmbraco()`:
   - `context.Request.Path.StartsWithSegments("/umbraco")`: `UseCsp` with the backoffice sources
     listed in the skill's best practices, and `ScriptSources(s => s.Self())`;
   - the negation: `UseCsp` with the site's own sources.

   Don't add a nonce or `'unsafe-inline'` to either `ScriptSources`. The injection adds the nonce.
3. **Add the nonce once.** Call `app.UseUmbracoCspNonceInjection()` after both branches, as the
   docs show. It inserts the nonce into `script-src` of whichever header is present, enforcing or
   report-only.
4. **Other headers:** `UseXfo(o => o.SameOrigin())` and `UseXContentTypeOptions()`, applied to
   both surfaces. Remove any `UseXXssProtection` call.
5. **Inline tags in views:** add `asp-csp-nonce` and register the tag helper (see the skill's
   best practices for the assembly name).
6. **Roll out** with NWebsec's report-only CSP (`UseCspReportOnly`), then switch to `UseCsp`.
7. **Build and verify.** Run `dotnet build` if there is a project, then the checks in the skill's
   *Verify* section.

## Done

Tell the user which `UseCsp` branch serves which path, that `UseUmbracoCspNonceInjection()` adds
the nonce to both, and which duplicate header sources were removed.
