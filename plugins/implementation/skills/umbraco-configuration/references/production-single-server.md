# Production, single server

## When to choose this

One instance serves the site and the backoffice (IIS, a Linux VM, a container). More than one
instance → also read [load-balanced.md](load-balanced.md). On Azure App Service →
[azure-app-service.md](azure-app-service.md), which extends this profile.

## Building blocks

- [Runtime modes](https://docs.umbraco.com/umbraco-cms/run-in-production/runtime-modes.md) — what
  Production enforces and its build prerequisites
- [Security settings](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/securitysettings.md)
- [Global settings](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/globalsettings.md)
- [Content version cleanup](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/content-version-cleanup.md)
- [Security overview](https://docs.umbraco.com/umbraco-cms/run-in-production/security.md)

Fetch these before adding a setting that is not already in `assets/production.json`.

## What in the profile is required and what is opinion

| Setting | Why |
|---|---|
| `Runtime:Mode` = `Production`, `Global:UseHttps` = true, `ModelsBuilder:ModelsMode` = `Nothing`, `WebRouting:UmbracoApplicationUrl` set | **Required** — Umbraco refuses to boot in Production mode without them |
| `Hosting:Debug` = false | Guard: already the default, kept explicit because a base `appsettings.json` often carries `true` over from development; debug compilation is slow and leaks detail |
| `Global:VersionCheckPeriod` = 0 | Opinion: stops the backoffice phoning home from a locked-down server (default 7 days) |
| `Security:UserPassword:RequiredLength` = 14 | Opinion: longer than the default 10 — loosen only on request |
| `Content:ContentVersionCleanupPolicy:EnableCleanup` = true | Opinion: stops version history growing without bound (off by default). The day counts are left at Umbraco's defaults (keep everything for 7 days, one per day for 90); add them only to change them |
| `Serilog` minimum levels | Opinion: keeps framework noise out of the logs |

Never add a value to a section just to restate its default — it hides what the site actually
chose to change. `Hosting:Debug` is a deliberate exception here, as is `Global:MainDomLock` in the
load-balanced and Azure profiles.

## Before you switch

Do these in development first, while the site still boots without them:

- If `ModelsBuilder:ModelsMode` is `InMemoryAuto` (the project template's default), move off it
  first. Under `InMemoryAuto` the models exist only at runtime, so views that use them cannot
  compile at build time and the next step breaks the build. Set `ModelsMode` to `SourceCodeAuto`
  (or `SourceCodeManual`), run the site, generate the models (**Settings > Models Builder >
  Generate models**) and commit them. The template adds the `Umbraco.Cms.DevelopmentMode.Backoffice`
  package only for `InMemoryAuto`; it registers nothing outside `BackofficeDevelopment` mode, so it
  can stay for local backoffice work or be removed once no environment uses `InMemoryAuto`.
- Then, in the `.csproj`, remove `<RazorCompileOnBuild>false</RazorCompileOnBuild>` and
  `<RazorCompileOnPublish>false</RazorCompileOnPublish>` so views compile into the assembly, and
  fix any compilation errors that exposes. Skipping this is what turns every page into a 404 once
  Production mode is on.
- Move any unattended-install password and connection string out of `appsettings*.json` into
  user-secrets (`dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>"`).

## Steps

1. Save `assets/production.json` as `appsettings.Production.json` and replace
   `<UmbracoApplicationUrl>` with the site's public HTTPS origin (trailing slash, e.g.
   `https://www.example.com/`). It is used in emails (password reset, notifications), so it must
   be reachable, not a placeholder.
2. Publish with `dotnet publish --configuration Release`. A Debug publish fails the
   JIT-optimisation check.
3. Set `ASPNETCORE_ENVIRONMENT=Production` on the host (machine or app-pool environment — not in a
   committed `web.config`, which would also apply to anyone running the project under IIS locally)
   and supply the connection string
   (`ConnectionStrings__umbracoDbDSN` and `ConnectionStrings__umbracoDbDSN_ProviderName`) as
   environment variables, never in the file.
4. Templates, Models Builder and views are read-only in this mode by design — changes happen in
   development and ship as a new publish.

## Done

Report which settings were required vs opinionated, where the secrets come from, and that the
site was validated against Umbraco's configuration model rather than booted in production.
