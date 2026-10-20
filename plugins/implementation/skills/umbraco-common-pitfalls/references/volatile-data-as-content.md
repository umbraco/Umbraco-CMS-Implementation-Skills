# Volatile data stored as Umbraco content nodes

**Symptoms:** site slows down under write load, cache or index churn, instability during imports.
**Look for:** `IContentService.Save()` or `Publish()` called per request, per form submission,
or in a loop over imported records.
**Docs section:** "Using Umbraco content items for volatile data"

Umbraco's publish/index/cache pipeline is not designed for high-frequency writes. Using content
nodes for hit counters, form submissions, or bulk imports degrades performance and stability.

| Don't do this | Use instead |
|---|---|
| Hit counter on a content node | Google Analytics or a custom DB table |
| New content node per form submission | Custom DB table |
| Bulk data import into content nodes | Custom DB tables; surface via content if needed |
