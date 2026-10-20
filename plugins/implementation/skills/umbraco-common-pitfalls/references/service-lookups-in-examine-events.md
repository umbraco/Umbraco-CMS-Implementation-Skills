# Service lookups inside Examine events

**Symptoms:** index rebuilds that take far longer than expected, heavy database load while
indexing.
**Look for:** `_contentService`, `_mediaService`, or other service calls inside
`TransformingIndexValues` or `DocumentWriting` handlers.
**Docs section:** "Performing lookups and logic in Examine events"

`TransformingIndexValues` and `DocumentWriting` fire for **every document being indexed**. A
service call inside one of these events becomes an N+1 problem — once per document, multiplied
by every rebuild.

**Bad:**
```csharp
private void OnTransformingIndexValues(object sender, IndexingItemEventArgs e)
{
    var content = _contentService.GetById(int.Parse(e.ValueSet.Id)); // N+1
}
```

**Good:** use the data already present in `e.ValueSet.Values` rather than fetching from the
service layer. For data that truly isn't in the index, batch-load it before the event fires.

**Related:** [Rebuilding Examine indexes unnecessarily](unnecessary-examine-rebuilds.md).
