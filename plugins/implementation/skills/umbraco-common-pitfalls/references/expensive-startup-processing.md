# Expensive processing during startup

**Symptoms:** slow cold starts, long restarts after every deployment or app-pool recycle.
**Look for:** heavy work (I/O, remote calls, large queries, schema setup) in
`UmbracoApplicationStartingNotification` handlers or composers.
**Docs section:** "Processing during startup"

Code in `UmbracoApplicationStartingNotification` handlers runs synchronously during boot. Slow
startup hurts cold starts and every application restart.

**Good:** lazy-load instead:
```csharp
private readonly Lazy<ExpensiveResource> _resource = new(() => BuildExpensiveResource());
```

Or use `LazyInitializer.EnsureInitialized`. For one-time DB operations (e.g. creating a schema
table), set a persistence flag so the work is skipped on subsequent restarts.
