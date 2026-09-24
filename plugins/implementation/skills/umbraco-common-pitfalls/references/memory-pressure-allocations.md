# Memory pressure from excessive object allocation

**Symptoms:** periodic application pauses, high garbage-collector activity, memory that stays
high under load.
**Look for:** `.Select(x => new SomeModel(x, ...))` over large collections, especially before
`.OrderBy(...)` or `.Take(...)`.
**Docs section:** "Be mindful about memory"

Creating thousands of wrapper objects via LINQ `Select` generates garbage-collector pressure.
Large allocations promoted to Generation 2/3 are expensive to collect and can cause application
pauses.

Prefer querying `IPublishedContent` directly rather than wrapping every node in a custom model:

```cshtml
@foreach (var recipe in recipeNode.Children()
    .OrderByDescending(x => x.Value<int>("votes"))
    .Take(10))
```

**Related:** [Logic in constructors](logic-in-constructors.md).
