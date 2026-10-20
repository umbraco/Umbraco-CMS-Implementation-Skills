# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a data type needs at each step.

## 1. Inspect

For a data type, these are the answers you need:

| Question | How | Why it matters here |
|---|---|---|
| Which data types use the editor you need? | `get-all-data-types` or `find-data-type`, then `get-data-type` on each candidate | Reuse needs the same configuration, not only the same editor |
| What uses a candidate? | `get-references-data-type` | Changing a shared data type changes all of these |
| Which editor-kind folders exist? | `get-data-type-root`, `get-data-type-children` | Create the folder only if it is missing |
| Which property will use it, and does it hold content now? | `get-document-type-by-id` on the type | Moving a property with content to a different value format loses it |

## 2. Decide

| Decision | Reference |
|---|---|
| Reuse, generic variant, or purpose-specific | [reuse-or-create.md](reuse-or-create.md) |
| Property editor, editor UI and value type | [editor-selection.md](editor-selection.md) |
| Name and folder | [naming-and-folders.md](naming-and-folders.md) |
| Configuration: every setting that differs from the editor's defaults, and every constraint | [reuse-or-create.md](reuse-or-create.md#constraints-go-in-the-data-type) |

If the answer is "reuse", there may be nothing to write: say which data type to use and why, and
stop there unless a property is being changed.

## 3. Write the requirements doc

| Page | From | Location | Action |
|---|---|---|---|
| The data type | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create, or Update for a changed one |
| The type whose property uses it, when a property is added or moved | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) or [element-type.md](../../umbraco-content-requirements-documentation/assets/element-type.md) | `Document-Types/<AliasPascalCase>.md`, or wherever the type lives | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to a data type:

- **Configuration** states each setting in words an editor can check in the backoffice (a sentence
  such as "Maximum characters: 160."), plus the setting aliases in the table when they are known
  from a data type read on the site. Do not invent aliases; copy them from `get-data-type`.
- **Database type** is the storage the editor uses (`Nvarchar`, `Ntext`, `Integer`, `Decimal`,
  `Date`). Copy it from a data type of the same editor on the site; write `—` if you cannot read
  it.
- **Used by** lists every property that will use the data type after this change. For an Update,
  it also lists every existing user, so the approver sees what else changes.
- The type whose property is added or moved is an Update page, written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes.

The changeset's checklist, in apply order:

```
- [ ] 1. Create data type folder `<Editor kind>`                            (only if new)
- [ ] 2. Create data type `<Name>` in `<Editor kind>`                        (or: Update data type `<Name>`)
- [ ] 3. Set property `<alias>` on `<type alias>` to use `<Name>`            (only if a property changes)
- [ ] 4. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

Call out every existing property the change touches, and any constraint that existing content may
now break; then stop, as
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval)
describes. If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a data type, also:

- **The folder**, only when the checklist creates one; the folder tool's quirks are in
  [apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#tools).
- **The create.** Build `values` from a `get-data-type` read of a data type with the same editor,
  changing only the settings the requirements doc names; never guess a configuration alias.
- **An update.** Read the data type, change only the named settings, write the whole configuration
  back with `update-data-type`, and read it again.
- **A property change.** Read the type, point the property's `dataType` at the new id, send the
  whole body back with `update-document-type`, and confirm the property count is unchanged.

## 6. Verify and report

For a data type, also confirm with `get-references-data-type` that exactly the properties in
`Used by` use it, and that a type whose property moved has an unchanged property count.

In the report's third item (what is left), name existing content that may fail the new
constraint, any content migration a value-format change would need, and the Razor or C# that
reads the value.
