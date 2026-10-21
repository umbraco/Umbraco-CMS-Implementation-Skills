# Approach A: Handle a built-in notification

Register a class that runs when Umbraco publishes one of its own notifications. Choose this
whenever the trigger is something Umbraco does (save, publish, move, delete, startup...). If the
trigger is *your* code and you want others to subscribe to it, use
[Approach B](approach-b-publish-custom.md).

## Contents

- [Docs: start with the relevant page](#docs-start-with-the-relevant-page): read when you have chosen a notification and need the one docs page to fetch, or the payload, load-balancing, culture and cancelled-publish facts the docs omit.
- [The files](#the-files): read when you want the shipped handler and composer, or the shape to copy for another notification (sync or async).
- [Step 1: Discover project context](#step-1-discover-project-context): read when starting, to find the namespace, Umbraco version and how handlers are already registered.
- [Step 2: Decide which notification](#step-2-decide-which-notification): read when choosing between the "-ing", "-ed" and cache refresher notifications, or handling multilingual, startup or bulk-import cases.
- [Step 3: Write the files](#step-3-write-the-files): read when substituting placeholders in the assets and building.
- [Done](#done): read when wrapping up, for what to tell the user.
- [Runtime coverage](#runtime-coverage): read when you need to know what the reference host actually proves for this approach.

## Docs: start with the relevant page

Use the shipped handler/composer when they fit. For Program.cs, put the same registration in the
existing builder chain before `Build()`. Choose the notification before fetching documentation;
start with the matching page below, not every link. Verify further only for a missing API detail
or a version mismatch.

| Chosen notification | Fetch |
|---|---|
| A cancel rule on `ContentPublishingNotification` (the shipped asset), any other Content notification, culture helpers, `Suppress()` | [ContentService Notifications](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/contentservice-notifications.md) |
| `ContentCacheRefresherNotification` | [CacheRefresher Notifications](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/cacherefresher-notifications.md), plus the payload facts below |
| Startup / shutdown | [Application Lifetime Notifications](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/umbracoapplicationlifetime-notifications.md) |
| Media, Member, or anything not listed | find it in [Using Notifications](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications.md) (overview + full list) |
| Async handler details or the `State` dictionary, if the asset comments aren't enough | [Notification Handler](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/notifications/notification-handler.md) |

These additional details are not on the docs pages. Use them before searching source;
if the installed API differs, verify that specific detail rather than suppressing the check:

- **New entity?** `content.HasIdentity == false` in "-ing"; `((IRememberBeingDirty)content).WasPropertyDirty("Id")` in "-ed".
- **Cancelled publish:** `IContentService.Publish` returns `PublishResultType.FailedPublishCancelledByEvent`
  before it saves anything, so no `ContentPublishedNotification` and no cache refresh follow. (The
  backoffice's "Save and publish" saves in a separate request first, which is why the editor's
  draft survives a cancelled publish.)
- **`ContentCacheRefresherNotification` payload:** `MessageObject` is `ContentCacheRefresher.JsonPayload[]`;
  each item has `Id`, `Key` (`Guid?`), `ChangeTypes` (`TreeChangeTypes` flags, test with bitwise
  checks), `Blueprint`, `PublishedCultures`, `UnpublishedCultures`. A successful publish arrives as
  `RefreshNode`, or `RefreshBranch` for a branch publish or a node's first publish (its descendants
  come back with it), with non-empty `PublishedCultures` (`["*"]`
  for invariant content); saves and cancelled publishes carry none.
- **Load balancing:** cache refresher notifications run on every server. Local work (invalidating
  an in-memory cache) belongs on all of them; an outbound side effect that must happen once needs
  a guard such as `IServerRoleAccessor.CurrentServerRole` being `Single` or `SchedulingPublisher`.
- **Batches and languages:** skip unrelated entities with `continue`, not `return`. Check
  `IsPublishingCulture` before validating a translation. A variant Document Type can still have
  invariant properties: check the property's `PropertyType.VariesByCulture()` and read invariant
  values without a culture. For success logs, use `HasPublishedCulture`, not `AvailableCultures`.

## The files

- `assets/PublishingRuleHandler.cs`: an `INotificationHandler<ContentPublishingNotification>`
  that cancels the publish when a property on a given Document Type breaks a rule. It is
  variant-aware: for a culture-variant type it checks only the cultures actually being published.
  The rule itself is one method (`Violates`) marked for replacement.
  It assumes the property varies like its Document Type; adapt the lookup as described above
  when a variant type has an invariant property.
- `assets/NotificationHandlersComposer.cs`: registers the handler. Add further
  `AddNotificationHandler` / `AddNotificationAsyncHandler` lines here for other notifications.

For any other notification, copy the asset's shape (class implementing `INotificationHandler<T>`,
one `Handle` method, one `AddNotificationHandler<T, THandler>()` line) and change the type. When
the work awaits anything (HTTP, email, DB), use `INotificationAsyncHandler<T>` with
`Task HandleAsync(T notification, CancellationToken cancellationToken)` and register with
`AddNotificationAsyncHandler`; don't block on `.Result` inside a sync handler.

## Step 1: Discover project context

- Root namespace and Umbraco version (`<PackageReference Include="Umbraco.Cms"` must be 17+).
- **How handlers are already registered.** Search for `: IComposer` and for
  `AddNotificationHandler` in `Program.cs`. Follow whichever exists; if neither, create a
  composer (works for sites and packages alike).
- Folder conventions (`Composers/`, `NotificationHandlers/`, `Handlers/`...); follow them, else the
  project root.
- For a cancel rule: the Document Type alias and property alias involved, and whether that type
  varies by culture (check the Document Type in the backoffice, or `VariesByCulture()` on it).

## Step 2: Decide which notification

Ask, in this order:

1. **Veto/alter, or react?** Veto: the "-ing" notification and `CancelOperation`. React:
   the "-ed" one.
2. **Does the reaction read the published cache (`IPublishedContent`, `UmbracoHelper`), or must
   it run on every server?** Then handle `ContentCacheRefresherNotification`: it is raised on each
   server when that server's cache has been refreshed, whereas the "-ed" notification runs only
   where the publish happened. The cache-refresher page documents this.
3. **Multilingual site?** Use `IsPublishingCulture` / `IsSavingCulture` / `HasPublishedCulture`
   etc. from the ContentService page. Remember a single-culture unpublish arrives as a
   *Publishing* notification (`IsUnpublishingCulture`), not an Unpublishing one.
4. **Startup work?** `UmbracoApplicationStartedNotification`, guarded by an injected
   `IRuntimeState.Level == RuntimeLevel.Run` (the notification carries no level, and it also fires
   on the first boot of an install). `IsRestarting` alone is the wrong guard: it lets the work run
   on that install boot and skips the in-process restart where an attended install or upgrade first
   reaches `Run`. Add it only if the work must not repeat after that restart. Keep it light; see
   `umbraco-common-pitfalls`, and
   [`umbraco-composing`](../../umbraco-composing/references/startup-and-runtime-levels.md) for
   components and the other runtime levels.
5. **Bulk import/migration that itself saves content?** Wrap it in a scope and call
   `scope.Notifications.Suppress()` only if you understand it also skips cache and index
   updates (the ContentService page explains the trade-off).

## Step 3: Write the files

Substitute in the assets:

- `<Namespace>`: the project's root namespace.
- `<DocTypeAlias>` / `<PropertyAlias>`: the confirmed aliases.
- Replace the body of `Violates` with the real rule **and** the `EventMessage` text, which
  describes the shipped upper-case rule; keep it user-facing, since the editor sees it in the
  backoffice.

Then, if a real `.csproj` exists, run `dotnet build` and fix compile errors before finishing. If
no buildable project exists, say so explicitly and state which documented APIs the code targets.
Never imply a build you didn't run.

## Done

Tell the user:

- Which notification(s) are handled and why: that half of the pair, or the cache refresher over
  the "-ed" notification (it runs on every server, after that server's cache has refreshed).
- Where the handler and its registration live, and how to add more handlers.
- For a cancel rule: that the editor sees the `EventMessage` text as a backoffice notification,
  the publish does not happen, and the draft they saved is kept.
- Any caveat that applies: load-balanced delivery, culture behaviour, startup cost.

## Runtime coverage

**Asserted** on the Clean reference host by `examples/approach-a-handle-builtin/`: the shipped
handler is compiled and a fixture proves a publish that breaks the rule is cancelled with the
handler's message, and one that obeys it succeeds.
