# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what shared data needs at each step.

## 1. Inspect

For shared data, establish:

| Question | Why it matters here |
|---|---|
| Is there already a folder or item type for this data? | Extend it instead of making a second |
| Does a `Data/` folder exist in Document Types? | If not, it joins the change first |
| What is allowed at root, and which node sorts first there? | The data folder joins the home page and site settings at root, and must sort after the home page |
| Are the values stored somewhere already (a text field on pages, tags)? | Moving existing values into items is a content migration |
| Which page types will pick the items, and is there a composition they share? | Where the picker property goes |
| Is there a `Content Picker/` data type folder, or existing pickers? | Reuse an identical picker; otherwise a new one |

## 2. Decide

| Decision | For shared data | Rule |
|---|---|---|
| Schema of the folder and item types: names, aliases, folder, icons, descriptions, culture | Decided by `add-document-type` with the role data folder or item: folder `<noun>Folder`, item the plain singular noun (`author`) or `<noun>Item` when the noun is not a real-world thing (`reusableContentItem`); both in `Data/`; both `color-green` (`icon-folder color-green` for the folder, a noun for the item); described by what they hold ("Holds the authors that articles can pick from.") | [add-document-type workflow](../../umbraco-add-document-type/references/workflow.md#2-decide), [type-roles.md](../../umbraco-add-document-type/references/type-roles.md) |
| Allowed at root | Folder Yes, item No; the folder node sorts after the home page and is not a page | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root) |
| Allowed children | Folder: the item, and itself only if editors need sub-folders. Item: nothing | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-children) |
| Templates and compositions | None on either. No SEO, sharing or page details | [templates.md](../../umbraco-content-model-conventions/references/templates.md), [compositions.md](../../umbraco-content-model-conventions/references/compositions.md) |
| Item fields | What each item holds beyond its name, on the `Content` tab. A link (a website, a map, a booking page) is a Multi URL Picker with a maximum of 1, never a Textstring holding a URL; a page is picked, not typed | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md), [property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md), [editor-selection.md](../../umbraco-add-data-type/references/editor-selection.md#choices-that-come-up-often) |
| Folder fields | Usually none | — |
| The picker | A multinode tree picker restricted to the folder and the item type, with the maximum the use needs | [taxonomy.md](taxonomy.md#the-shape-of-a-content-driven-taxonomy) |
| Where the picker property goes | On the composition the consuming types share, or the one type that needs it | [taxonomy.md](taxonomy.md#where-the-picker-goes) |

## 3. Write the requirements doc

Shared data is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| The item type | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/Data/<AliasPascalCase>.md` | Create |
| The folder type | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/Data/<AliasPascalCase>.md` | Create |
| The picker data type | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/Content-Picker/<Name-Slug>.md` | Create |
| Each type or composition that gains the picker | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) or [composition.md](../../umbraco-content-requirements-documentation/assets/composition.md) | Where it lives | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to shared data:

- Both pages write `—` for Default template, Allowed templates and Compositions; the linter
  rejects a template on a type in `Data/`. Delete the `Collection` row.
- The picker's Configuration names the start node as the data folder **content node** the user
  creates (by name), the allowed item type and the maximum. The start node is content, not
  schema: note in the summary that the folder node must exist before the picker is configured.
- Each type or composition that gains the picker is an Update page, written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes.

The changeset's checklist, in apply order:

```
- [ ] 1. Create document type folder `Data`                                     (only if new)
- [ ] 2. Create document type `<itemAlias>` in `Data`, then fix-up
- [ ] 3. Create document type `<folderAlias>` in `Data`, then fix-up (allowed at root)
- [ ] 4. Set allowed children on `<folderAlias>`: `<itemAlias>`
- [ ] 5. Create the `<Folder name>` content node at the content root, sorted after the home page   (content, by the user)
- [ ] 6. Create data type folder `Content Picker` and data type `<Picker name>`
- [ ] 7. Add property `<alias>` to `<type or composition alias>`, keeping everything else   (one per consumer)
- [ ] 8. Verify every requirements doc against the site and set each status line
```

Step 5 is content, not schema, and is carried out by the user in the Content section. It is listed
so the checklist order is right, and the verification does not depend on it.

Step 6 is a justified exception to the default apply order in
[apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#order),
which creates data types first: the picker's start node is the folder's content node, which
cannot exist until the folder type does, so the picker is created after it.

## 4. Stop for approval

Call out every existing type that gains a picker, and any values already stored elsewhere that
would need migrating into items; then stop, as
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval)
describes. If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For shared data, also:

- **Both types in `Data/`.** `create-document-type` takes the `Data` folder's id as `parentId`;
  look it up at apply time.
- **The item before the folder**, so the folder's allowed children can name it.
- **The picker after the folder node exists.** The start node is a content node id; ask the user to
  create the folder node, then look up its id. Copy the picker's `values` shape from an existing
  multinode tree picker read with `get-data-type`.
- **Each consumer** by read-modify-write, confirming the property count grew by exactly one.

## 6. Verify and report

For shared data, also confirm that the folder is allowed at root and allows the item, that neither
type has a template, and that the picker's start node and allowed type are the ones in the
requirements doc. The schema tools cannot read content, so ask the user to confirm that the folder
node sorts after the home page at the content root and has no URL of its own.

In the report's third item (what is left), name the folder content node and its items, the
migration of any values stored elsewhere, and the Razor or C# that reads the picked items.
