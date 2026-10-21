# Approach B: Publish your own notification

Define an `INotification` and publish it from your own service, controller or background job so
other code (yours, or a consumer of your package) can react without a direct dependency. Choose
this when the trigger is something *you* do; if the trigger is an Umbraco operation, use
[Approach A](approach-a-handle-builtin.md). Handling your own notification uses Approach A's
handler shape unchanged.

## Contents

- [Docs: usually nothing to fetch](#docs-usually-nothing-to-fetch): read when deciding whether any docs page is needed, for example before writing a recurring job from scratch.
- [The files](#the-files): read when you want the shipped notification and handler assets and where the publish call goes.
- [Step 1: Discover project context](#step-1-discover-project-context): read when starting, to find the namespace, registration style and the code that triggers the operation.
- [Step 2: Decide how to publish](#step-2-decide-how-to-publish): read when choosing between publishing inside a scope or immediately, or when letting observers veto with a before/after pair, async vetoes or carried state.
- [Step 3: Write the files](#step-3-write-the-files): read when writing the notification, the publish call and the registration, then building.
- [Done](#done): read when wrapping up, for what to tell the user.
- [Runtime coverage](#runtime-coverage): read when you need to know what the reference host actually proves for this approach.

## Docs: usually nothing to fetch

The notification class and handler are shipped as assets, the publish call is in Step 2, and the
veto pattern is below. If the project already has the service or job that will publish, fetch
nothing. Only if you must write a recurring job from scratch, fetch this one page, which contains
a complete `IRecurringBackgroundJob` that publishes both ways:
[Creating and Publishing Notifications](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/creating-and-publishing-notifications.md).
Handler shape and registration are the same as Approach A's assets.

## The files

- `assets/ContentImportCompletedNotification.cs`: a payload-carrying `INotification`
  (`ItemsImported`, `Source`). Rename class and properties to the real event; keep it an
  immutable record of *what happened*, not a bag of services.
- `assets/ContentImportCompletedHandler.cs`: a minimal handler that logs the payload, to show the
  consuming side. Replace the body; register it with
  `builder.AddNotificationHandler<ContentImportCompletedNotification, ContentImportCompletedHandler>()`
  in the project's composer (see Approach A for where that lives).

The publishing call itself is written into the user's existing service/job, not shipped as an
asset; Step 2 shows where it belongs.

## Step 1: Discover project context

- Root namespace, Umbraco version (17+), composer / `Program.cs` registration style, folder
  conventions, as in Approach A.
- Where the triggering operation lives (service, controller, `IRecurringBackgroundJob`), and
  whether it already opens an `ICoreScope`.

## Step 2: Decide how to publish

- **Must the notification only go out if the operation succeeded** (DB writes, content changes
  that could roll back)? Publish inside the scope with `scope.Notifications.Publish(...)` and
  call `scope.Complete()`. Without `Complete()` the notification is silently dropped along with
  the work. Inject `ICoreScopeProvider`; use `CreateCoreScope()`.
- **Should it always go out, immediately** (e.g. "job started")? Inject `IEventAggregator` and
  call `Publish(...)` (or `PublishAsync`).
- Both in one method is normal:

```csharp
_eventAggregator.Publish(new ImportStartedNotification(source));          // immediate

using ICoreScope scope = _scopeProvider.CreateCoreScope();                 // Umbraco.Cms.Core.Scoping
// ... do the work ...
scope.Notifications.Publish(new ContentImportCompletedNotification(source, count));
scope.Complete();                                                          // no Complete(): the scope rolls back and the queued notification goes with it
```

Namespaces: `IEventAggregator` in `Umbraco.Cms.Core.Events`, `ICoreScopeProvider` / `ICoreScope` in
`Umbraco.Cms.Core.Scoping`.

Await all import work inside the scope before calling `Complete()`; never complete in `finally`
or use auto-completion when failures must roll back. Nested scopes share the outer notification
queue: completing an inner scope does not deliver success until the outermost scope also
completes and is disposed. Do not detach the scope to force early delivery.

Name notifications after the event in past tense (`ImportCompleted`).

### Letting observers veto: a before/after pair

If handlers must be able to stop your operation, model it the way Umbraco does (all types in
`Umbraco.Cms.Core.Notifications` unless noted):

- **"Starting" notification** derives from `CancelableNotification`. Its constructor needs an
  `EventMessages` (`Umbraco.Cms.Core.Events`); handlers veto with
  `notification.CancelOperation(new EventMessage("category", "reason", EventMessageType.Error))`,
  which sets `Cancel` and records the message in `notification.Messages`.
- **Publish it before any work** with `_eventAggregator.PublishCancelable(starting)`: returns
  `true` if a handler cancelled (async: `PublishCancelableAsync`). On `true`, abort and log the
  reasons from `starting.Messages.GetAll()`.
- **Async vetoes:** await the publish before starting the operation.
  `PublishCancelableAsync(starting)` has no cancellation-token parameter. To propagate one,
  `await _eventAggregator.PublishAsync(starting, cancellationToken)` and then check
  `starting.Cancel`. Register async veto handlers with `AddNotificationAsyncHandler`.
- **Carry context to the "completed" notification.** Both `CancelableNotification` and
  `StatefulNotification` expose `IDictionary<string, object?> State`. Handlers can put data in
  the starting notification's `State`; derive the completed notification from
  `StatefulNotification` and copy it over with `completed.WithStateFrom(starting)` (extension in
  `Umbraco.Cms.Core.Notifications`), then publish the completed one as in Step 2.
  Create the pair per operation; do not keep shared context on handlers or singletons.
- A plain `INotification` with your own `bool Cancelled` property works but gives consumers a
  different contract from every Umbraco notification; prefer the base classes.

## Step 3: Write the files

- Write the notification class (and handler, if the project consumes its own notification),
  substituting `<Namespace>`.
- Add the publish call to the triggering code per Step 2, and the handler registration per the
  project's style.
- `dotnet build` if a real project exists; otherwise say so explicitly and name the documented
  APIs used.

## Done

Tell the user:

- The notification's name and payload, and where it is published from.
- Whether it is published immediately or on scope completion, and why.
- How a consumer subscribes (`AddNotificationHandler<YourNotification, TheirHandler>()`), and that
  handlers are transient.

## Runtime coverage

**Asserted** on the Clean reference host by `examples/approach-b-publish-custom/`: the shipped
notification and handler are compiled, and a fixture proves the notification is delivered when
published via `IEventAggregator` and via a completed scope, and *not* delivered from a scope that
is disposed without `Complete()`.
