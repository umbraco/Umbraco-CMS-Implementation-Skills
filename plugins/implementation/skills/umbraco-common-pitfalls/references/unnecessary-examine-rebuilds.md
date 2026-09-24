# Rebuilding Examine indexes unnecessarily

**Symptoms:** out-of-memory errors on large sites, search results out of sync, rebuilds used as a
routine fix.
**Look for:** scheduled or code-triggered index rebuilds, rebuilds run as part of deployment or
startup.
**Docs section:** "Rebuilding indexes"

Index rebuilds iterate every content and media item and can cause out-of-memory on large sites.
Keep Umbraco and Examine up to date; that resolves most sync issues without manual rebuilds.

Primary causes of index drift: outdated Umbraco version; rebuilding while simultaneously
restarting the app domain.

**Related:** [Service lookups inside Examine events](service-lookups-in-examine-events.md) —
makes every rebuild more expensive.
