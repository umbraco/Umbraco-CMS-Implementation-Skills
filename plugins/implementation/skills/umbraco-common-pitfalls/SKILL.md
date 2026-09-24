---
name: umbraco-common-pitfalls
description: >
  Reference guide to anti-patterns, performance traps, and common mistakes in Umbraco
  development. Covers DI vs. statics, static references to request-scoped instances,
  DescendantsOrSelf() on large trees, over-querying, services in Razor views, volatile content
  nodes, startup processing, Examine N+1, RenderTemplateAsync misuse, constructor logic, eager
  loading, missing cache, memory pressure, and Models Builder misuse.
  Use this whenever the user asks about "common pitfalls", "performance issues", "anti-patterns",
  "memory leaks", "best practices for Umbraco code", "avoid mistakes in Umbraco",
  "why is my Umbraco site slow", or asks to review or audit Umbraco code for correctness.
  SKIP: non-Umbraco projects.
---

# Common Pitfalls & Anti-Patterns

An index of the most impactful mistakes in Umbraco development — issues that cause memory leaks,
instability, N+1 queries, and poor performance. Source of truth: the
[official Umbraco docs](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/application-code/common-pitfalls.md).

The docs explain each pitfall. This skill adds what the docs don't: how to **spot** it in real
code (*Look for*), what it looks like **from the outside** (*Symptoms*), and which pitfalls
**travel together** (*Related*). Each reference file keeps a short summary and example, and names
its section in the docs page (*Docs section*).

## How to use this index

- **Reviewing or auditing code:** scan the code for the signatures in the *Look for* column, then
  open only the reference files that match. Several pitfalls often appear together (for example
  `DescendantsOrSelf()` and repeated `Model.Root()` calls), so check the *Related* links at the
  end of each file.
- **Diagnosing a symptom** (slow page, memory growth, data bleed): match the symptom to the
  category below, then open the pitfalls in that category.
- **Writing new code:** check the pitfalls in the categories the code touches, and apply the fix
  before claiming correctness.
- **When to fetch the docs:** if the reference file's example covers the fix, answer from it;
  there is no need to fetch anything. Fetch the docs page only when the fix depends on something
  the reference file doesn't show, such as a different API, an edge case, or a context the example
  doesn't cover. Then read the named section instead of filling the gap from memory.

## Dependency injection & lifetimes

| Pitfall | Look for | Reference |
|---|---|---|
| Singletons and statics | `static` service fields, Service Locator calls | [singletons-and-statics.md](references/singletons-and-statics.md) |
| Static references to request-scoped instances | `static UmbracoHelper`, `static UmbracoContext` | [static-request-scoped-instances.md](references/static-request-scoped-instances.md) |

## Content querying & traversal

| Pitfall | Look for | Reference |
|---|---|---|
| `DescendantsOrSelf()` on large trees | `.DescendantsOrSelf()` + `.Where(x => x.Level == n)` | [descendantsorself-large-trees.md](references/descendantsorself-large-trees.md) |
| Over-querying (repeated traversals) | `Model.Root()` / `.Ancestor()` called repeatedly | [over-querying.md](references/over-querying.md) |
| Services layer in Razor views | `@inject IContentService` (or other `I*Service`) in `.cshtml` | [services-in-razor-views.md](references/services-in-razor-views.md) |
| Not caching expensive lookups | Traversal to find a site-wide settings or nav node | [missing-lookup-cache.md](references/missing-lookup-cache.md) |

## Data storage

| Pitfall | Look for | Reference |
|---|---|---|
| Volatile data stored as content nodes | `IContentService.Save()` per request, submission, or import row | [volatile-data-as-content.md](references/volatile-data-as-content.md) |

## Startup & indexing

| Pitfall | Look for | Reference |
|---|---|---|
| Expensive processing during startup | Heavy work in `UmbracoApplicationStartingNotification` | [expensive-startup-processing.md](references/expensive-startup-processing.md) |
| Rebuilding Examine indexes unnecessarily | Scheduled or code-triggered index rebuilds | [unnecessary-examine-rebuilds.md](references/unnecessary-examine-rebuilds.md) |
| Service lookups inside Examine events | Service calls in `TransformingIndexValues` / `DocumentWriting` | [service-lookups-in-examine-events.md](references/service-lookups-in-examine-events.md) |

## Rendering

| Pitfall | Look for | Reference |
|---|---|---|
| `RenderTemplateAsync` for content rendering | `RenderTemplateAsync` used for on-page output | [rendertemplateasync-misuse.md](references/rendertemplateasync-misuse.md) |

## Models & object lifecycle

| Pitfall | Look for | Reference |
|---|---|---|
| Logic in constructors | Traversal or LINQ inside a model constructor | [logic-in-constructors.md](references/logic-in-constructors.md) |
| Eager loading | Property values assigned in the constructor; resolved content stored on models | [eager-loading.md](references/eager-loading.md) |
| Memory pressure from object allocation | `.Select(x => new Model(x))` over large collections | [memory-pressure-allocations.md](references/memory-pressure-allocations.md) |
| Models Builder misuse | ModelsBuilder partials that traverse content or build view models | [models-builder-misuse.md](references/models-builder-misuse.md) |

## Version compatibility

The official docs page covering these pitfalls targets **Umbraco 17 and 18** — the only versions
for which it is currently published. The underlying patterns apply to any DI-era Umbraco (v9+),
but the documentation source is only verified against 17/18.

The C# APIs used in the reference examples were compiled against `Umbraco.Cms` 17.5.3 and 18.2.0
(checked 23-09-2026). Razor snippets were checked through their C# equivalents. On both versions
they compile with no obsolete-API (CS0618) warnings, with one difference: the
`PublishedContentWrapped` constructor, which is noted in
[logic-in-constructors.md](references/logic-in-constructors.md).

Currently active supported versions (as of 2026-08-05):

| Version | Type | End-of-Life |
|---|---|---|
| Umbraco 18 | STS | 25-06-2027 |
| Umbraco 17 | LTS | 27-11-2028 |
| Umbraco 13 | LTS | 14-12-2026 |

Versions 10–12 and 14–16 are end-of-life. Umbraco 13 remains supported until 14-12-2026. Source:
[Umbraco LTS & End-of-Life](https://umbraco.com/products/knowledge-center/long-term-support-and-end-of-life/).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`.
