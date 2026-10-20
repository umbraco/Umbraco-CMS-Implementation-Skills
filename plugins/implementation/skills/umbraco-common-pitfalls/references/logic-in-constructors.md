# Logic in constructors

**Symptoms:** list pages that are slow even when they display only a few items; cost that scales
with the size of the source collection rather than the output.
**Look for:** traversal (`.Parent()`, `.Children()`, `.Descendants()`), LINQ queries, or
`.Value(...)` calls inside the constructor of a model or `PublishedContentWrapped` subclass.
**Docs section:** "Do not put logic inside your constructors"

Constructors should only set fields and validate parameters. LINQ operations such as `Select`,
`OrderBy`, or `Where` may instantiate objects thousands of times — if the constructor performs
expensive work, the cost multiplies.

**Bad:**
```csharp
public RecipeModel(IPublishedContent content, IPublishedValueFallback fallback)
    : base(content, fallback)
{
    // Runs for every object LINQ touches, including ones that are later discarded
    RelatedRecipes = content.Parent()
        .Children<RecipeModel>()
        .Where(x => x.Value<IEnumerable<int>>("related").Contains(content.Id));
}
```

**Good:** use lazy-loaded properties — see [Eager loading](eager-loading.md).

**Version note:** the example above is for Umbraco 17. In Umbraco 18, `PublishedContentWrapped`
has a single-argument constructor, `(IPublishedContent content)`; passing an
`IPublishedValueFallback` fails to compile (CS1729). Match the constructor to the project's
version.

**Related:** [Memory pressure from excessive object allocation](memory-pressure-allocations.md).
