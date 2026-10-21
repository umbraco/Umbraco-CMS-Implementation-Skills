# Approach A — Custom middleware

## When to choose this approach

When you want the policy in source control as one reviewable file, with a report-only switch in
configuration. For a project already on NWebsec, use [Approach B](approach-b-nwebsec.md).

## Building blocks (fetch before changing the design)

- **The CSP and the backoffice's sources:**
  [Content Security Policy (CSP)](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check/guides/contentsecuritypolicy.md),
  section *Adding a CSP using custom middleware*.
- **Pipeline filters** (`UmbracoPipelineFilter`, `PrePipeline`):
  [Custom Middleware](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/backend-and-custom-logic/routing/custom-middleware.md)
- **Composers:**
  [Composing](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md)

## What the code does

Read the templates in [`../assets/`](../assets):

- `SecurityHeadersMiddleware.cs`: the backoffice policy under `/umbraco`, the site policy
  elsewhere, plus `nosniff`, `SAMEORIGIN`, `Referrer-Policy` and `Permissions-Policy`, and removal
  of `X-XSS-Protection`. Its comments say why the headers are set when they are.
- `SecurityHeadersSettings.cs`: `ReportOnly`, bound from the `SecurityHeaders` section.
- `SecurityHeadersComposer.cs`: registers both and adds the middleware in `PrePipeline`.

## Steps

1. **Discover the project.** Find the root namespace, the folder conventions, and every existing
   source of these headers: `app.Use` blocks, NWebsec, `web.config` `customHeaders`, and the CDN
   or proxy. Remove those, or keep them out of this middleware, so each header has one source.
2. **List what the front end loads.** Open the layout and partials, and note every external
   script, stylesheet, font, image, iframe and `fetch` origin, plus every inline `<script>` and
   `<style>` and `style="…"` attribute (including ones in rich text).
3. **Write the three files,** replacing `<Namespace>`. Put them where the project keeps composers,
   or at the project root.
4. **Edit `SitePolicy`** with the origins from step 2. For inline tags:
   - add `asp-csp-nonce` to each inline `<script>`/`<style>` and register the tag helper (see the
     skill's best practices for the assembly name and for output-cached pages);
   - inline `style="…"` attributes: see the skill's best practices.

   Leave `BackOfficePolicy` as shipped unless a backoffice package needs another source.
5. **Roll out report-only.** Add `"SecurityHeaders": { "ReportOnly": true }` to `appsettings.json`
   (or the environment's settings), deploy, and browse the site and backoffice with the console
   open. Fix each violation, then set `ReportOnly` to `false` and restart the site: output-cached
   pages otherwise keep the old header until their entries expire.
6. **Build and verify.** If a `.csproj` exists, run `dotnet build`, then run the checks in the
   skill's *Verify* section.

## Done

Tell the user:

- which files were added, and that `SitePolicy` is the place to add origins later;
- that `ReportOnly` switches between report-only and enforcing without a code change;
- which header sources were removed, and anything the CDN or proxy still adds;
- that `Server` is removed in Kestrel or IIS settings, not in this middleware.
