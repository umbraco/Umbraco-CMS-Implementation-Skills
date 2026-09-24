# Eager loading — use lazy loading instead

**Symptoms:** models that do work for properties the view never reads, a content cache that
grows larger than expected.
**Look for:** property values assigned in the constructor, properties that return resolved
`IPublishedContent` or custom models instead of IDs.
**Docs section:** "Do not eager load data, lazy load it instead"

Resolve property values only when actually accessed. The `??=` null-coalescing assignment is the
idiomatic pattern:

```csharp
private int? _votes;
public int Votes => _votes ??= this.Value<int>("votes");

private List<int> _related;
public IEnumerable<int> RelatedRecipes =>
    _related ??= this.Value<IEnumerable<int>>("related").ToList();
```

Return IDs, not resolved `IPublishedContent` instances. Storing resolved entities on a cached
model bloats the content cache.

**Related:** [Logic in constructors](logic-in-constructors.md).
