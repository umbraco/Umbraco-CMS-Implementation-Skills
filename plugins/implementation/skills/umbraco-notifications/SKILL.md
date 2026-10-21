---
name: umbraco-notifications
description: >
  Run server-side C# when something happens in Umbraco 17+ (content saved, published,
  unpublished, moved, deleted, media or members changed, the application started), or raise
  your own notification for other code to observe. Use this whenever the user asks to
  "run code when a page is published", "hook into publish/save/delete", "block or cancel a
  publish", "subscribe to a notification", write an INotificationHandler, "do something on
  startup", or "publish a custom notification/event" from a service or background job.
  SKIP: non-Umbraco projects, Umbraco < 17, backoffice toast notifications
  (UMB_NOTIFICATION_CONTEXT, a backoffice-extension concern), the editor "Notifications"
  email subscriptions, and plain POST-to-URL on standard events, which built-in Webhooks
  already cover.
---

# Notification handlers

Umbraco publishes a notification for most things it does, usually as a before/after pair
(`ContentPublishingNotification` / `ContentPublishedNotification`). You react with a registered
handler class, and you can publish notifications of your own.

**Check first:** if the user only wants an HTTP call to a URL on publish/unpublish/delete (or
media save/delete), built-in
[Webhooks](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/webhooks.md)
(Settings > Webhooks) do that with no code. Point them there and stop.

| | **A: Handle a built-in notification** (default) | **B: Publish your own notification** |
|---|---|---|
| Reference | [approach-a-handle-builtin.md](references/approach-a-handle-builtin.md) | [approach-b-publish-custom.md](references/approach-b-publish-custom.md) |
| You write | A handler (`INotificationHandler<T>` or async variant) + one registration line | A notification class, the publish call, usually a handler |
| Shipped code | `assets/PublishingRuleHandler.cs`, `assets/NotificationHandlersComposer.cs` | `assets/ContentImportCompletedNotification.cs`, `assets/ContentImportCompletedHandler.cs` |
| Best for | Reacting to or vetoing what Umbraco does | Letting other code react to what your service/job does |

**Before writing any code, open the reference for the approach you choose and start from its
assets.** The facts the docs pages leave out (cache-refresher payloads, cancelled publishes,
scope delivery) are only there.

### How to decide

"When Umbraco does X, do Y" is A, which is nearly everything. Pick B when the event is your own
code and others should be able to listen without referencing it. A job that publishes its own
notification plus a handler for it uses both.

## Version compatibility

**Umbraco 17+.** API details in the references were checked against Umbraco 17.5; the shipped
code is gated on the 17 reference instance.

## Best practices

- **Right half of the pair.** Veto or alter in the "before" notification (`CancelOperation`);
  do side effects in the "after" one. "After" notifications can't be cancelled (they have no
  `CancelOperation`), and "before" side effects run even if the operation fails.
- **Handlers are created fresh per notification.** Fields don't survive between calls; keep
  state in an injected service. Pass data before-to-after via the notification's `State`.
- **"Published" ran here; "cache refreshed" runs everywhere.** `ContentPublishedNotification`
  fires once, on the server that published. `ContentCacheRefresherNotification` fires on every
  server after its own cache has updated, so use it for per-server work (local caches) and for
  anything that must see the published state on other nodes of a load-balanced site.
- **Languages change what fires.** Unpublishing one language is a publish of the rest, so it
  arrives as `ContentPublishingNotification`. Use `IsPublishingCulture` / `IsUnpublishingCulture`.
- **Keep startup handlers quick** (they run every boot; see `umbraco-common-pitfalls`), and
  gate them on `IRuntimeState.Level == RuntimeLevel.Run`: they also fire during install and upgrade
  boots. `umbraco-composing` covers startup work and runtime levels in full.
- **Register the way the project already does:** existing `IComposer`s, or the
  `CreateUmbracoBuilder()` chain in `Program.cs`. If neither, a composer.

## Validation

Assertions live in [`evals/evals.json`](evals/evals.json); run them with `umbraco-skill-evaluator`.
