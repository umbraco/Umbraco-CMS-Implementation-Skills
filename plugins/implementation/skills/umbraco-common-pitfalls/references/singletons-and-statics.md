# Singletons and statics

**Symptoms:** code that is hard to test, hidden dependencies, lifetime mismatches between
services.
**Look for:** `static` fields holding services, `StaticServiceProvider`, `Current.*`, or other
Service Locator calls in application code.
**Docs section:** "Usage of Singletons and Statics"

Umbraco provides DI everywhere. Static fields and Service Locator calls make code untestable,
create API leakage, and introduce lifetime mismatches. Use constructor injection instead — all
Umbraco controllers, composers, notification handlers, and Razor base classes support it.

**Related:** [Static references to request-scoped instances](static-request-scoped-instances.md)
— the most harmful variant of this pitfall.
