---
name: umbraco-composing
description: >
  Register custom code into an Umbraco 17+ site at startup: composers, IUmbracoBuilder extension
  methods, collection builders (content finders, URL providers, health checks), composer ordering,
  disabling or replacing built-in implementations, and startup components gated by runtime level.
  Use this whenever the user asks to "register a service or notification handler in Umbraco",
  "create a composer", "add my content finder before Umbraco's", "replace or remove a built-in
  Umbraco implementation", "make my composer run after another one", "disable a package's composer",
  "run code when Umbraco starts", "only run this once Umbraco is installed / not during an upgrade",
  "migrate our composers and components from Umbraco 13", or reports that "IComponent is obsolete"
  or a startup InvalidOperationException from the CreateUmbracoBuilder() chain. SKIP: non-Umbraco
  projects, Umbraco < 17, client-side backoffice extensions registered through umbraco-package.json,
  and plain ASP.NET Core middleware.
---

# Composing

Registrations can live in two supported places. Both call the same `IUmbracoBuilder` extension
method, so switching between them later only changes where it's called from.

| | **A — Composer** (default) | **B — `Program.cs`** |
|---|---|---|
| Reference | [approach-a-composer.md](references/approach-a-composer.md) | [approach-b-program-cs.md](references/approach-b-program-cs.md) |
| How it is found | Type-scanned `IComposer`, runs inside `.AddComposers()` | Explicit call in the `CreateUmbracoBuilder()` chain |
| Runs | In an unspecified order among composers unless `[ComposeAfter]`/`[ComposeBefore]` is used | After **every** composer, when placed after `.AddComposers()` |
| Works in a class library / package / RCL | Yes, and it's the only option there | No, because a package can't edit the host's `Program.cs` |
| Best for | Self-contained features, packages, multi-project solutions | Site code that must override what a composer (including a third-party package) registered |

**Before writing any code, open the reference for the approach you choose and start from its
assets.** The Umbraco 17 traps are covered only there. For example, `ContentFinderByUrl` still
compiles but is no longer registered, so anchoring to it fails at boot, and `AddUnique` silently
turns a scoped service into a singleton.

### How to decide

Default to A. Choose B when the user owns `Program.cs` and needs a registration to win over one
made by a composer they can't edit, or explicitly wants all registrations visible in `Program.cs`.
If ambiguous, recommend A and mention B in one line.

Both approaches share two topics. Read whichever the task touches:

- [collections-and-ordering.md](references/collections-and-ordering.md) covers adding to, inserting
  into, replacing or removing items in Umbraco's collections, replacing a single service, composer
  order, and disabling a composer.
- [startup-and-runtime-levels.md](references/startup-and-runtime-levels.md) covers code that runs
  when Umbraco starts or stops (components or notification handlers), and gating it by runtime
  level.

## Version compatibility

Targets **Umbraco 17+**. `IComponent` (sync `Initialize`/`Terminate`) is obsolete in 17 and
scheduled for removal in 18, so use `IAsyncComponent` or one of its base classes instead.

## Best practices

- **One `Add<Feature>()` extension per feature, called from exactly one place.** Calling it from both
  a composer and `Program.cs` isn't harmless. Collections de-duplicate, but service registrations and
  async notification handlers don't, so they get registered twice and handlers fire twice.
- **`Compose()` only registers.** Runtime level isn't known yet, and composers run at every level
  (install, upgrade, boot-failed). So inside `Compose()`, don't resolve services, call
  `BuildServiceProvider()`, touch the database or do I/O. Startup work belongs in a component or a
  startup notification handler.

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Approach A is **Asserted**: its assets are compiled and asserted by the
`dotnet test` gate (`examples/approach-a-composer/`). Approach B is **Compiled**:
`examples/approach-b-program-cs/` builds its assets and the `Program.cs` wiring after `.AddComposers()`,
but no host runs it, because the shared reference host has its own `Program.cs`. Evals grade the placement.
