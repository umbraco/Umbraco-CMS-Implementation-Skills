---
name: umbraco-health-check
description: >
  Create and localize custom Umbraco 17 Health Checks for backend C# and Razor frontend
  experiences, including multilingual Dictionary items and safe fallback handling. Use this skill
  when the user asks to add a custom Umbraco Health Check, validate an appsettings value, show
  health-check guidance in Razor, localize a check, organise its Dictionary items, or handle a
  missing translation safely. SKIP: non-Umbraco projects, Umbraco versions below 17, existing
  built-in checks that only need configuration, and liveness/readiness or orchestrator probe
  endpoints.
---

# Umbraco Health Check

Use this skill for the Settings > Health Check dashboard and any explicitly requested Razor
presentation of its guidance or status text. Fetch the official documentation before implementing;
do not copy API samples from memory.

## Version compatibility (recommended)

This skill and examples are written for Umbraco 17 and verified against Umbraco 17 documentation. The underlying patterns (DI-era health checks, localization via Dictionary items, safe Razor rendering) generally apply to Umbraco v9+ (the DI-era releases), but only Umbraco 17 is explicitly verified here. When implementing, confirm the target project's Umbraco package version and the exact API surface before coding.

Helpful refs:
- Health Check docs: https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md
- Health Probes guidance (for Kubernetes/readiness): https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-probes.md
- Umbraco LTS & EoL: https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/

## Choose the implementation surface

| Surface | Choose when | Guidance |
|---|---|---|
| **Backend C#** (default) | The requirement is a dashboard check, configuration comparison, runtime inspection, or safe remediation action | Use the current Umbraco 17 `AbstractSettingsCheck` or `HealthCheck` API; keep checks fast and report failures honestly. |
| **Razor frontend** | The user explicitly wants localized health-check guidance or status copy rendered by a view/component | Keep status computation in C#; use the project’s Razor/localization conventions, encode output, and never expose secrets. |

If the request is only for a backoffice check, do not add a Razor surface. If it asks for
Kubernetes, load-balancer, liveness, or readiness probes, skip this skill and use the official
[Health Probes documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/health-probes.md).

## Backend C# and official source

Start with the official
[Health Check documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md).
Use `AbstractSettingsCheck` for one configuration key and accepted values; use `HealthCheck` for
custom runtime logic, multiple statuses, or an explicit `HealthCheckAction`. Generate a stable GUID,
use an appropriate group, validate action aliases, and keep remediation idempotent. Check the
project’s actual Umbraco 17 package and namespace conventions before writing code.

For scheduling and notification delivery, consult the separate
[Health checks configuration documentation](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/configuration/healthchecks.md);
do not conflate scheduled notifications with dashboard execution.

### Minimal HealthCheck outline (adapt to your project/API)

Below is a minimal outline to show the shape of a dashboard check — adapt namespaces, base classes,
and helper calls to the project's actual Umbraco 17 API rather than copying.

```csharp
// Minimal outline — adapt to the project's Umbraco 17 API and namespaces.
[HealthCheck("00000000-0000-0000-0000-000000000000", Name = "Use HTTPS", Description = "Checks that Umbraco:CMS:Global:UseHttps is true", Group = HealthCheckGroup.Security)]
public class UseHttpsHealthCheck : HealthCheck
{
    public UseHttpsHealthCheck(/* inject config/services as needed */) { }

    public override HealthCheckStatus Execute(HealthCheckContext context)
    {
        // Read configuration carefully; adapt to your config access pattern
        var useHttps = context.Configuration?["Umbraco:CMS:Global:UseHttps"];
        if (string.Equals(useHttps, "true", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckStatus.Success("HTTPS is enabled.");
        }

        return HealthCheckStatus.Warning("HTTPS is not configured. See documentation link for remediation.");
    }

    public override HealthCheckAction[] ExecuteAction(HealthCheckActionRequest request)
    {
        // Provide idempotent remediation when appropriate, with validation and logging.
        throw new NotImplementedException();
    }
}
```

Always validate action inputs and surface write failures as errors (and logs), not swallowed successes.

## Razor, multilingual Dictionary items, and fallbacks

When Razor is requested, inspect the existing view/component and localization conventions first.
Keep user-facing text in Dictionary items rather than hard-coded translated strings. Organise items
by feature and message purpose, use stable aliases, and create values deliberately for every
supported culture. Follow the project’s established culture-selection mechanism.

Define fallback behavior before implementation: requested culture, configured/default culture, then
a safe non-sensitive invariant message. A missing Dictionary item or translation must produce a
visible controlled fallback or an explicit failure state—not a blank success, leaked configuration
value, swallowed exception, or silent default. Encode Razor output and never render secrets or raw
configuration values.

Suggested explicit fallback chain (pseudocode):

```csharp
// Pseudocode: safe Dictionary lookup with explicit fallback
var translation =
    Dictionary.GetValue(alias, requestedCulture) ??
    Dictionary.GetValue(alias, configuredDefaultCulture) ??
    "Service temporarily unavailable. Please try again later.";
// In Razor: use the normal @translation rendering (it is encoded by default).
// Avoid Html.Raw(translation) unless you have a verified, sanitized HTML value.
```

Important: by default Razor's `@variable` rendering is HTML-encoded. Do not use `Html.Raw` for localized
strings unless the content is known-safe and has been sanitized — `Html.Raw` can introduce XSS risk.
Never render configuration values or secrets directly into the view.

## Validation

Objective scenarios and build-honesty requirements live in
[`evals/evals.json`](evals/evals.json). Build a real project when one exists and state clearly when
verification was not possible.
