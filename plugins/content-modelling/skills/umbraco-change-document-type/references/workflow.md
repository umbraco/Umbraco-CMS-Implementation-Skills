# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a change to an existing type needs at each step.

## 1. Inspect

For an existing type, these are the answers you need before deciding anything:

| Question | Why it matters here |
|---|---|
| The type in full: `get-document-type-by-id` | Every property, container, composition, template and child; the Update page and the write-back both start from this |
| Which compositions give it fields, and which types compose it | A field a composition supplies cannot be added again; a change to a composition reaches every type that takes it |
| What uses it: `get-document-type-composition-references`, the parent's allowed children, block editors that register it | What else the change touches |
| Does it have content, and roughly how many nodes | Decides the class of every removal, rename and mandatory change; see [safe-changes.md](safe-changes.md#counting-what-is-affected) |
| Does it already have a requirements page | It is edited and set back to `proposed`; otherwise a complete page is written from the read-back |
| Which data types exist for a new field | Reuse before creating |

Read the type again immediately before applying: the site may have changed since the
requirements doc was written.

## 2. Decide

Classify each part of the request with [safe-changes.md](safe-changes.md) first. Then, for what
goes ahead:

| Decision | For an existing type | Rule |
|---|---|---|
| A new property's place | The tab the type already uses for that kind of field; next sort in hundreds after the last property there | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md) |
| A new property's alias | Follows the type's existing prefixes; never a reserved alias | [property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md) |
| A new property's data type | An existing one whose configuration fits, else a new one by [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) | [naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types) |
| A field several types need | Belongs on a composition, not on each type: hand off to [umbraco-add-composition](../../umbraco-add-composition/SKILL.md) | [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#when-to-make-one) |
| New allowed children | Per kind; the child type must exist first | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-children) |
| A new folder | One of the three top-level folders, or a sub-folder applied to every feature | [tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md) |
| Departures the type already has | The project's convention wins; a change does not silently fix unrelated departures. Name them for [umbraco-review-content-model](../../umbraco-review-content-model/SKILL.md) | — |

## 3. Write the requirements doc

A change to an existing type is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| Any new data type for a new field | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The type, as it will be after the change | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) or [element-type.md](../../umbraco-content-requirements-documentation/assets/element-type.md) | Where it lives, `<AliasPascalCase>.md` | Update |
| A parent whose allowed children change | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/<AliasPascalCase>.md` | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to a change:

- The Update page shows the type **after** the change, complete, as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes. The changeset summary says what differs from the site: each property added, removed
  or changed, each child or composition added or removed, in words.
- A removed property or composition is **not** in the Update page's table. The summary names it,
  its class from [safe-changes.md](safe-changes.md), the node count (or that the count is the
  user's word), and the migration or the user's confirmation it waits on.
- A rename of an alias is written as the safe alternative (new property, old one kept) unless the
  user has already confirmed the loss in those words.
- A change to a composition is one Update page for the composition; the types that take it are
  listed in the summary as affected, not as pages.

The changeset's checklist, in apply order:

```
- [ ] 1. Create data type folder `<Editor kind>` and data type `<Name>`      (only if new)
- [ ] 2. Update document type `<alias>`: <each addition, removal or change, in words>, keeping everything else
- [ ] 3. Update document type `<parent alias>`: add `<alias>` to allowed children, keeping the existing entries   (only if children change)
- [ ] 4. Move document type `<alias>` into `<folder>`                        (only if moving)
- [ ] 5. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

Call out, by name: every property or composition the change **removes** and what that loses,
every property made mandatory and the nodes it affects, and any part of the request written as
its safe alternative instead of as asked. Then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For an existing type, also:

- **Read, modify, write.** Read the type with `get-document-type-by-id` at apply time, change only
  what the Update page says, and send the whole body back with `update-document-type`; the fix-up
  pass in
  [apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#the-fix-up-pass)
  is the same operation. A new property is appended to `properties` with its container, sort,
  description, mandatory flag and `variesByCulture`; a new container is appended to `containers`
  with its sort; a removal is the one case where an entry is deliberately left out, and only after
  the user's confirmation.
- **A move** uses `move-document-type` with the folder's id, looked up at apply time; it returns
  no body.
- **A parent's children** are appended to `allowedDocumentTypes` after the entries it already has.
- **Manually**, the walkthrough names the exact property to add, remove or change and the tab it
  sits in, and tells the user to type the alias by hand.

## 6. Verify and report

For an existing type, also compare the **property count** with what the Update page lists: it
must have grown by the additions and shrunk by the confirmed removals, and by nothing else. For a
composition, read one type that takes it and confirm the field appears there.

In the report, what is left (item 3) names: the migration for any value move, the template or
model changes that read a new or removed field, and any departure the type still has that this
change did not touch.
