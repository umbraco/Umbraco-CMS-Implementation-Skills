# Static references to request-scoped instances

**Symptoms:** users seeing another user's content or session data, memory growth over time,
stale content that never refreshes.
**Look for:** `static UmbracoHelper`, `static IUmbracoContext` / `UmbracoContext`, or a
request-scoped object stored in a field of a singleton service.
**Docs section:** "Static references to scoped instances such as `UmbracoHelper`"

`UmbracoHelper` and `UmbracoContext` are **request-scoped**: they live for one HTTP request.
Storing them in a static or singleton field traps a request's cache snapshot and user security
context in application memory — causing memory leaks and cross-request data bleed.

**Bad:**
```csharp
public class BadApiController : Controller
{
    private static UmbracoHelper _umbracoHelper; // static + request-scoped = leak

    public BadApiController(IUmbracoHelperAccessor accessor)
    {
        if (_umbracoHelper is null)
        {
            accessor.TryGetUmbracoHelper(out var helper);
            _umbracoHelper = helper;
        }
    }
}
```

**Good:** inject `IUmbracoHelperAccessor` and resolve per-request, or inject `UmbracoHelper`
directly (it is registered as request-scoped in DI and is safe when consumed that way).

**Related:** [Singletons and statics](singletons-and-statics.md).
