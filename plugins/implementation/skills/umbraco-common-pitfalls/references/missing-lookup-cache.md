# Not caching expensive lookups

**Symptoms:** the same traversal runs on every request, for example to find a settings node or
the global navigation root.
**Look for:** tree traversal or search (`.Root().Descendants...`, `.FirstOrDefault(x =>
x.ContentType.Alias == ...)`) used to find a well-known, site-wide node.
**Docs section:** "Not caching expensive lookups"

If the same content item (global nav root, settings node) is needed on every request, cache or
hardcode its ID and retrieve via `Umbraco.Content(id)`. A direct ID lookup is a single cache
dictionary hit; tree traversal is not.

**Related:** [Over-querying](over-querying.md).
