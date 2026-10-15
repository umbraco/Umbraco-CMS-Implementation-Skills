# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a composition needs at each step.

## 1. Inspect

For a composition, these are the answers you need before deciding anything:

| Question | Why it matters here |
|---|---|
| Is there already a composition for this concern? | Reuse or extend it instead of making a second |
| Does a `Compositions` folder exist? | If not, the folder joins the change first |
| Which compositions already put fields in the tab this one will use? | A shared tab means groups, and an Update of the existing composition |
| Which types would take it, and what do they define now? | Every target is an Update; an alias clash blocks a target |
| Which data types exist for the fields? | Reuse before creating |

Read every candidate target with `get-document-type-by-id`, and every composition it already has,
so the alias comparison in [when-to-apply.md](when-to-apply.md#what-blocks-a-type-from-taking-it)
is made against the real schema. `get-document-type-composition-references` tells you what already
uses an existing composition.

## 2. Decide

| Decision | For a composition | Rule |
|---|---|---|
| The concern | One job; two jobs are two compositions | [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#when-to-make-one) |
| Name and alias | Title Case name and camelCase alias, both ending `Composition` (`seoComposition`) | [naming.md](../../umbraco-content-model-conventions/references/naming.md) |
| Folder | `Compositions/` | [tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md) |
| Icon | `icon-settings` or a noun that fits, no colour | [icons-and-colours.md](../../umbraco-content-model-conventions/references/icons-and-colours.md#colour-by-role) |
| Description | "Adds ..." | [descriptions.md](../../umbraco-content-model-conventions/references/descriptions.md) |
| Tab, group and sorts | The standard tab and group for the concern | [standard-compositions.md](standard-compositions.md), [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md) |
| Fields and data types | Short list, area-prefixed aliases, existing data types first | [standard-compositions.md](standard-compositions.md) |
| Behaviour | None of its own | [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#rules) |
| Targets | Selective, by type kind; clashes excluded | [when-to-apply.md](when-to-apply.md) |
| Vary by culture | Match the targets. `No` on a single-language site | — |

## 3. Write the requirements doc

A composition change is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| A new data type, only if a field needs one | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The composition | [composition.md](../../umbraco-content-requirements-documentation/assets/composition.md) | `Document-Types/Compositions/<AliasPascalCase>.md` | Create |
| An existing composition that now shares the tab and gains a group | [composition.md](../../umbraco-content-requirements-documentation/assets/composition.md) | `Document-Types/Compositions/<AliasPascalCase>.md` | Update |
| Each target type, with the composition added to its Compositions row | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | Where the type lives, e.g. `Document-Types/<AliasPascalCase>.md` | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to a composition:

- `Used by` lists every target by link (or plain text when it has no requirements page).
- The `Compositions` folder is listed in Dependencies as a `document-type-container`, flagged
  `New in this changeset` if it is created now, and the `Folder` breadcrumb links to the index
  pages. Write missing index pages from
  [folder-index.md](../../umbraco-content-requirements-documentation/assets/folder-index.md).
- **The targets.** Every target is an existing type, written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes; each one is its own checklist item, so the user approves the exact list and a failure
  leaves a record of which types have the composition.
- Types left out because of a clash are named in the summary with the reason.

The changeset's checklist, in apply order:

```
- [ ] 1. Create data type folder `<Editor kind>` and data type `<Name>`   (only if new)
- [ ] 2. Create document type folder `Compositions`                     (only if new)
- [ ] 3. Create composition `<alias>`, then fix-up (sorts, groups, mandatory, descriptions)
- [ ] 4. Update composition `<existing alias>`: move its fields into group `<Group>`   (only if the tab is now shared)
- [ ] 5. Add `<alias>` to the compositions of `<target alias>`, keeping the existing ones   (one item per target)
- [ ] 6. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

For a composition, call out: every **existing** type that will change, any type left out and why,
and anything that would need a content migration. Then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a composition, also:

- **The create.** `parentId` is the `Compositions` folder's id, looked up at apply time. Each
  property carries its `tab` and, when the tab is shared, its `group`.
- **An existing composition gaining a group.** Read it, add a group container whose parent is the
  tab's container, point each of its properties at the new group, and send the whole body back.
  Take the container shape from a type on the site that already has a group. Its property count
  must not change.
- **Each target.** Read it with `get-document-type-by-id`, append the composition to its
  `compositions` after the entries it already has, copying the shape of those entries, and send
  the whole body back with `update-document-type`. Read it again: its own property count is
  unchanged and the composition is listed. Do the targets one at a time, ticking each, so a failure
  leaves a clear record of which types have it.

## 6. Verify and report

For a composition, also confirm that `get-document-type-composition-references` on the new
composition lists exactly the targets in the requirements doc, and that each target's own
properties are the ones it had before.

In the report's "what is left", name the types left out and why, any field move that needs a
content migration, and the Razor or C# that reads the new fields, which is implementation work.
