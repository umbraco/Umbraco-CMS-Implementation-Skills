# Startup work and runtime levels

## Composers can't see the runtime level

`Compose()` can't make runtime-level decisions (see SKILL.md's best practices), so register
unconditionally and gate the *work* at startup with one of the two mechanisms below.

(Umbraco 8's `[RuntimeLevel(MinLevel = ...)]` composer attribute doesn't exist in Umbraco 17. Don't
use it.)

## The runtime levels that matter

`Umbraco.Cms.Core.RuntimeLevel`, in the order they're listed in the
[Composing docs](https://docs.umbraco.com/umbraco-cms/model-your-content/content-types-and-structure/composing.md):
`BootFailed`, `Unknown`, `Boot`, `Install`, `Upgrade`, `Run`. Umbraco 17.3 added **`Upgrading`**. It
means an unattended upgrade is running in the background while the HTTP
server already answers health probes. During `Upgrading`, components and startup notifications are
**deferred** until the migrations finish, and then run once at `Run`.

Code that queries content, writes to the database or relies on the published cache needs `Run`.

## Option 1 — a component (`assets/FeatureComponent.cs`)

Use a component when you want start **and** stop hooks as one unit, or ordering among components
(components initialise in collection order and terminate in reverse).

| Base | Runs when | Use for |
|---|---|---|
| `RuntimeAsyncComponentBase` | Level is `Run`, and not a restart | Almost everything: anything touching content or the DB |
| `AsyncComponentBase` | Any level, but not a restart | Work that must also happen during install/upgrade (rare) |
| `IAsyncComponent` directly | Always, and you get `isRestarting` | Full control |

**After an attended install or upgrade, the two base classes don't run until the next process
start.** When the install or upgrade wizard finishes, Umbraco restarts the runtime in-process
(`RestartAsync`), so components get `isRestarting: true`, which both bases skip. They were also
skipped at the first boot, because the level wasn't `Run` then. For work that must be in place
straight after the wizard (warming a cache the site depends on, for example), gate on
`IRuntimeState.Level == RuntimeLevel.Run` only, not on `isRestarting`. To do that, either override
`protected virtual bool CanExecute(bool isRestarting)` on `AsyncComponentBase`, injecting your own
`IRuntimeState` because the base's is private, or implement `IAsyncComponent` directly. Make that
work idempotent, because it then runs again on every restart. Unattended
installs and upgrades don't restart, so they aren't affected.

Register it with `builder.Components().Append<T>()`. That's what `ComponentComposer<T>` does, but
the extension method in `assets/` already has the line. Implement `InitializeAsync(CancellationToken)`
and, optionally, `TerminateAsync(CancellationToken)`.

When migrating an existing `IComponent`, move its body into `InitializeAsync`/`TerminateAsync` on
one of the bases above, and pick the base by the runtime level the old code assumed. Only
`IComponent` is obsolete. `ComponentComposer<T>` isn't: it now requires `T : IAsyncComponent`, so an
existing `ComponentComposer<YourComponent>` can stay as it is once the component is migrated.

## Option 2 — a startup notification handler

`INotificationAsyncHandler<UmbracoApplicationStartingNotification>` runs at the same point as
components (straight after them). The notification carries `RuntimeLevel` and `IsRestarting`, so
check them yourself:

```csharp
if (notification.RuntimeLevel != RuntimeLevel.Run) return;
```

Add `|| notification.IsRestarting` only when the work must not repeat after the install/upgrade
restart described above.

Prefer this when the feature already uses notification handlers and needs no stop hook. Register
it with `builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, T>()`.
`UmbracoApplicationStartedNotification` fires later, once the host has fully started. Use it for
work that must not delay startup. It carries only `IsRestarting`, not `RuntimeLevel`, so gate it on
an injected `IRuntimeState.Level` instead. Fetch the
[Notification Handler docs](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/notification-handler.md)
for the handler shape.

## Outside startup

A long-lived service that needs to know the level at request time (a controller, a background
job) should inject `IRuntimeState` and check `runtimeState.Level == RuntimeLevel.Run`. Don't cache
the level at construction, because it changes during an unattended upgrade.

## Keep it fast

Boot waits for every component and every `UmbracoApplicationStartingNotification` handler in turn.
Hand heavy work to a background job. For the performance side, see
[`umbraco-common-pitfalls`](../../umbraco-common-pitfalls/references/expensive-startup-processing.md).
