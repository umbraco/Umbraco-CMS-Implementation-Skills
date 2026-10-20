# Over-querying (repeated traversals)

**Symptoms:** views that are slower than their output suggests, the same lookup repeated
throughout a template.
**Look for:** `Model.Root()`, `.Ancestor()`, `.AncestorOrSelf()`, or the same `.Value(...)`
called more than once in one view or method.
**Docs section:** "Too much querying ("Over querying")"

Every `.Root()`, `.Ancestor()`, or property resolution is a cache traversal. Calling
`Model.Root()` three times traverses upward three times.

**Bad:**
```cshtml
<a href="@Model.Root().Url()">@Model.Root().Name</a>
@foreach (var node in Model.Root().Children()) { ... }
```

**Good:**
```cshtml
@{ var root = Model.Root(); }
<a href="@root.Url()">@root.Name</a>
@foreach (var node in root.Children()) { ... }
```

**Related:** [DescendantsOrSelf() on large trees](descendantsorself-large-trees.md),
[Not caching expensive lookups](missing-lookup-cache.md).
