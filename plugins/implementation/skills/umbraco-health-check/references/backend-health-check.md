# Backend C# Health Check (Umbraco 17)

Fetch the official
[Health Check documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md)
first, then start from [`assets/RobotsTxtHealthCheck.cs`](../assets/RobotsTxtHealthCheck.cs) and
[`assets/en-US.xml`](../assets/en-US.xml). The docs' sample doesn't compile on 17 as written; the
corrections are at the end of this page.

## When to choose which base class

| Base class | Choose when |
|---|---|
| `Umbraco.Cms.Core.HealthChecks.Checks.AbstractSettingsCheck` | One configuration key compared with accepted values, and nothing to fix automatically |
| `Umbraco.Cms.Core.HealthChecks.HealthCheck` | Custom runtime logic, several statuses, or a **Fix** action (the asset) |

Before writing a new check, check the built-in ones. `HttpsCheck` (Security group) already reports
whether `Umbraco:CMS:Global:UseHttps` is enabled when the site runs on HTTPS, so a custom UseHttps
check only makes sense when it checks something that one doesn't (for example, regardless of the
current scheme).

## The API on 17

- **Registration is automatic.** Umbraco adds every `HealthCheck` subclass it finds by type scanning
  (`builder.HealthChecks().Add(() => builder.TypeLoader.GetTypes<HealthCheck>())`), so no composer
  is needed. Every check needs a `[HealthCheck]` attribute, or its constructor throws.
- **`[HealthCheck("<guid>", "<name>", Description = ..., Group = ...)]`**: the GUID and the name are
  constructor arguments (`Name` has no setter). `Group` is a plain string such as `"Security"`,
  `"Configuration"` or `"SEO"`; an existing group name puts the check in that group, a new one adds
  a group. There is no `HealthCheckGroup.Security` constant: `HealthCheckGroup` is the API's group
  model.
- **`HealthCheck`**: override `Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()`, which runs
  every time the dashboard loads, so keep it fast. Override
  `HealthCheckStatus ExecuteAction(HealthCheckAction action)` for **Fix** buttons; the backoffice
  calls `ExecuteActionAsync`, which calls `ExecuteAction` unless you override it too.
- **`HealthCheckStatus`** is built with `new HealthCheckStatus(message) { ResultType = StatusResultType.Success }`.
  There are no `Success(...)`/`Warning(...)` factory methods. `ResultType` is `Success`, `Warning`,
  `Error` or `Info`; `Actions` lists the Fix buttons; `ReadMoreLink` adds a documentation link.
- **`HealthCheckAction(alias, healthCheckId)`** with `Name` (the button text) and `Description`.
  `ExecuteAction` receives the alias back: switch on it, reject anything unknown, and make the action
  idempotent (clicking Fix twice must not do harm).
- **`AbstractSettingsCheck`**: the constructor takes an `ILocalizedTextService`; implement
  `ItemPath` (the configuration key), `Values` (`AcceptableConfiguration` items, one with
  `IsRecommended = true` for `ShouldEqual`), `CurrentValue`, `ValueComparisonType` and
  `ReadMoreLink`. Its `ExecuteAction` throws `NotSupportedException`: a settings check can't fix
  anything.

## Localized messages

Use `ILocalizedTextService.Localize("<area>", "<key>", culture)` for every message, button name and
description, and ship the keys in a language file:

- a site: `~/config/lang/<culture>.xml` (for example `en-US.xml`; `en-US.user.xml` also works);
- a package: `App_Plugins/<package>/lang/<culture>.xml`.

The file name is the culture. A key that resolves nowhere comes back as `[key]`, so add the keys
for every backoffice culture the site supports.

## Write remediation safely

- Validate the action alias (and that the action targets this check's `Id`) before doing anything.
- Report write failures as `StatusResultType.Error` and log them: never swallow an exception and
  return success.
- A file that must be reachable over HTTP goes in the **web root** (`IWebHostEnvironment.WebRootPath`,
  normally `wwwroot`). ASP.NET Core serves static files from there, not from the content root.

## Scheduling and notifications

Scheduled runs and notification methods are configured separately from the dashboard; see the
[Health checks configuration documentation](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/healthchecks.md).
The same checks run in both places.

## Docs corrections (Umbraco 17)

The robots.txt sample on the Health Check page, checked against the 17.5.3 source and the gate:

| Docs say | On 17 |
|---|---|
| `public override Task<IEnumerable<HealthCheckStatus>> GetStatus()` | `GetStatus()` was replaced by `GetStatusAsync()` in 15.3. On 17 there is no `GetStatus()` to override, so the sample doesn't compile. |
| `File.WriteAllText(_hostEnvironment.MapPathContentRoot("~/robots.txt"), ...)` | The content root isn't served, so the file never appears at `/robots.txt`. Write to the web root (`IWebHostEnvironment.WebRootPath`), as the asset does. |
