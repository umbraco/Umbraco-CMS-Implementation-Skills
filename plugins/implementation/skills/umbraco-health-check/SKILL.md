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
do not copy API samples from memory, and don't copy the docs' sample either: it doesn't compile on 17.

## Version compatibility (recommended)

This skill and its asset are written for Umbraco 17, and the asset is compiled and run against the
pinned 17.5.3. Earlier versions differ: before 15.3 a check overrides `GetStatus()` instead of
`GetStatusAsync()`. When implementing, confirm the target project's Umbraco package version and the
exact API surface before coding. Umbraco LTS & EoL: https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/

## Choose the implementation surface

| Surface | Choose when | Guidance |
|---|---|---|
| **Backend C#** (default) | The requirement is a dashboard check, configuration comparison, runtime inspection, or safe remediation action | [backend-health-check.md](references/backend-health-check.md): `AbstractSettingsCheck` or `HealthCheck`; keep checks fast and report failures honestly. |
| **Razor frontend** | The user explicitly wants localized health-check guidance or status copy rendered by a view/component | Keep status computation in C#; use the project’s Razor/localization conventions, encode output, and never expose secrets. |

If the request is only for a backoffice check, do not add a Razor surface. If it asks for
Kubernetes, load-balancer, liveness, or readiness probes, skip this skill and use the official
[Health Probes documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/server-setup/health-probes.md).

## Backend C#

Fetch the official
[Health Check documentation](https://docs.umbraco.com/umbraco-cms/run-in-production/infrastructure-and-ops/health-check.md),
and link the official pages you relied on in your answer.

**Before writing any code, open [references/backend-health-check.md](references/backend-health-check.md)
and start from [`assets/RobotsTxtHealthCheck.cs`](assets/RobotsTxtHealthCheck.cs) and its language
file [`assets/en-US.xml`](assets/en-US.xml).** The reference covers choosing between
`AbstractSettingsCheck` and `HealthCheck`, the Umbraco 17 API (`GetStatusAsync`, `ExecuteAction`,
`HealthCheckStatus`), where the language keys go, scheduling, and the docs sample's corrections.
Generate a stable GUID, use an appropriate group, validate action aliases, and keep remediation
idempotent. Always surface write failures as errors (and logs), not swallowed successes.

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

Umbraco doesn't fall back between cultures for you: `Umbraco.GetDictionaryValue(key)` (current
culture) and `GetDictionaryValue(key, culture)` return an **empty string**, not null, when the item
or its translation is missing. So a `??` chain never falls through; test for empty instead, or use
`GetDictionaryValueOrDefault`, which does.

Suggested explicit fallback chain (uncompiled sketch for a view):

```csharp
// Requested (current) culture, then the default culture, then a safe invariant message.
var translation = Umbraco.GetDictionaryValue(alias);
if (string.IsNullOrWhiteSpace(translation))
{
    translation = Umbraco.GetDictionaryValueOrDefault(alias, defaultCulture,
        "Service temporarily unavailable. Please try again later.");
}
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

The backend surface is **Asserted**: the `dotnet test` gate compiles the asset into the reference
host (`examples/robots-txt/`) and checks its statuses, its Fix action, that its messages resolve
from the language file, and that the created robots.txt is served at `/robots.txt`. The Razor
surface is **Documented**: it ships no code, the sketch above is uncompiled, and evals grade the
guidance.
