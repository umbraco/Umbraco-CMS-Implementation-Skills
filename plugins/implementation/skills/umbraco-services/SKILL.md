---
name: umbraco-services
description: >
  Umbraco 17/18 services and helpers in C#: published cache vs IContentService, content where
  there is no UmbracoContext (background jobs, notification handlers), custom services, lifetimes,
  composer registration. Use when asked to "get content in a background job", "UmbracoContext is
  null", "use IContentService or UmbracoHelper", "create a custom service",
  "singleton or scoped", "cannot consume scoped service from singleton", "get the root node in
  code", or "react when a page is unpublished". SKIP: views, partials, view components, template
  controllers (umbraco-templating); composer ordering, replacing built-ins, collection builders
  (umbraco-composing); performance audits (umbraco-common-pitfalls); 404 pages
  (umbraco-custom-error-pages); headless clients; non-Umbraco or Umbraco below 17.
---

# Services and helpers

Two families, both via DI: **published cache/helpers** (read-only, in memory) and **management services** (change data, hit the DB). Bugs come from mixing them, or from request-only helpers where there is no request.

## Route

Before writing code, open **one** reference, then the asset(s) its row names, and start from the asset. Open another asset or fetch a doc only when you need an API, registration or case they don't show.

| Task | Open |
|---|---|
| Choose/get a built-in service: controller, notification handler, background job, content finder | [using-built-in-services.md](references/using-built-in-services.md) |
| Own service or extension method, lifetime, registration | [custom-services.md](references/custom-services.md) |
| `@inject` in views/partials/view components, route hijacking, form posts | sibling skill `umbraco-templating` |
| Composer order, `Program.cs` registration, replacing a built-in service | sibling skill `umbraco-composing` |
| Slow pages, statics holding `UmbracoHelper`, services in views | sibling skill `umbraco-common-pitfalls` |

Version: read `Umbraco.Cms` from `.csproj`/`Directory.Packages.props`. Below 17 → skill doesn't apply, say so. No project → ask.

## Best practices

The rules live once, in the references: *Read vs change* and *Where the code runs* (using-built-in-services), *Lifetime = from dependencies* (custom-services). Cross-cutting: dispose every `EnsureUmbracoContext()`; use "-ing" notifications when you need the state before the change.

## Version compatibility

17 and 18. Assets compile on 17.5.3 and 18.2.0 with no errors or obsolete-API warnings (18 adds style warning AV0014, `[ApiController]`, on the example controller). Runtime-verified on 17.5.3 only.
On 18: some `IContentService` members moved to base `IPublishableContentService<IContent>` (callers unaffected); `SaveBlueprint(IContent, int)` removed (obsolete on 17): call `SaveBlueprint(content, createdFromContent, userId)`, `null` when there is no source. `UnpublishedCultures` on both Published/Unpublished notifications from 17.6.0/18.1.0 → using-built-in-services, *Culture unpublish*.

## Coverage

**Asserted.** All `assets/` run in the `dotnet test` gate (`examples/custom-service/`, Clean host): scoped service via controller, singleton reader with no request (both via a context and via the injected cache), URLs needing a context, unpublishing handler still sees the cached page.

## Validation

[`evals/evals.json`](evals/evals.json), run with `umbraco-skill-evaluator`.
