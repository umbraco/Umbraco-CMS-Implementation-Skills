# DescendantsOrSelf() on large trees

**Symptoms:** slow navigation or listing partials, render time that grows with total site size.
**Look for:** `.DescendantsOrSelf()` or `.Descendants()`, especially followed by
`.Where(x => x.Level == n)`.
**Docs section:** "Querying with Descendants using DescendantsOrSelf"

`DescendantsOrSelf()` iterates **every node** in the subtree. On a 10,000-node site, using it
to build a nav menu iterates all 10,000 nodes even when only level-2 children are needed.

**Bad:**
```cshtml
@foreach (var node in Model.Root().DescendantsOrSelf().Where(x => x.Level == 2))
```

**Good:**
```cshtml
@foreach (var node in Model.Root().Children())
```

Use `DescendantsOrSelf()` only when the subtree is provably small and filtering at depth is
genuinely required.

**Related:** [Over-querying](over-querying.md) — often appears in the same snippet.
