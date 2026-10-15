# Workflow

Follow [the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what one element needs at each step. The role, decided first, is in
[element-roles.md](element-roles.md).

## 1. Inspect

For an element, these are the answers you need before deciding anything:

| Question | Why it matters here |
|---|---|
| Is there already an element for this noun or concern? | Do not create a second one; offer to extend the first |
| Does the noun clash with a media type, data type or reserved word? | Only then does a content element's alias take the `...Block` suffix |
| Do `Elements/`, `Elements/Settings/` and `Elements/Compositions/` exist? | The role's folder joins the change first if it is missing |
| Which settings compositions and settings models exist, and what do they offer? | A settings model reuses them; a new settings composition must not duplicate one, and the models that should compose it are named as the follow-up |
| Which data types exist for the fields? | Reuse before creating |
| On a multi-language site, do the existing elements vary by culture? | A new element matches the elements beside it |
| Where will the element be used? | Not changed here; named in the summary for [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) or the block that owns it |

## 2. Decide

| Decision | For an element | Rule |
|---|---|---|
| Role | Content element, repeater item, settings model or settings composition | [element-roles.md](element-roles.md#settling-the-role) |
| Name and alias | Title Case name; camelCase alias by role: plain noun, `...Item`, `<block>Settings` or `...SettingsComposition` | [naming.md](../../umbraco-content-model-conventions/references/naming.md) |
| Folder | By role: `Elements/`, `Elements/Settings/` or `Elements/Compositions/` | [tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#document-types) |
| Container | By role: one `Content` group (sort 0), no containers of its own, or a `Style` (50) or `Settings` (100) tab | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use) |
| Fields | Content fields only on a content element or item; one concern's fields on a settings composition; none on a settings model. Short generic aliases, booleans as predicates, sorts in hundreds | [property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md) |
| Data types | Existing ones first; a fixed list of choices gets its own purpose data type | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md), [editor-selection.md](../../umbraco-add-data-type/references/editor-selection.md) |
| Icon | A noun that fits, no colour; `icon-settings` for settings | [icons-and-colours.md](../../umbraco-content-model-conventions/references/icons-and-colours.md) |
| Description | What the element shows, or what the settings control; "Adds ..." for a settings composition | [descriptions.md](../../umbraco-content-model-conventions/references/descriptions.md) |
| Compositions | None, except a settings model, which lists its settings compositions. Page compositions never go on elements | [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models) |
| Vary by culture | `No` on a single-language site; `Yes` when blocks must be translated, which needs the type and its properties to vary | [requirements-format.md](../../umbraco-content-requirements-documentation/references/requirements-format.md#properties), Culture paragraph |

Element types have no allowed children, are never allowed at root and have no template.

### Style on a content element

When the user asks for a colour, spacing, alignment or anchor field on the element editors fill
in, do not put it there. Say that style lives on the block's settings element, built from a
settings composition, and leave the content element to its content. If the user wants the
settings made now, that is the block: hand off to
[`umbraco-add-block`](../../umbraco-add-block/SKILL.md).

### Tabs on a content element

When the user asks for tabs on an element (Content and Settings, Content and Meta), do not add
them. An element is edited in a small overlay, and the second tab is almost always its settings
model. One `Content` group, sort 0.

## 3. Write the requirements doc

One element is these pages, as needed, in this order:

| Page | From | Location | Action |
|---|---|---|---|
| A new data type for a field, only if needed | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The element, in its role's folder | [element-type.md](../../umbraco-content-requirements-documentation/assets/element-type.md) | `Document-Types/Elements/<AliasPascalCase>.md`, `Elements/Settings/...` or `Elements/Compositions/...` | Create |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to an element:

- The element template's default rows are a content element's: Tab `—`, Group `Content`, Group
  Sort `0`. A **settings composition** replaces them with its tab and no group. A **settings
  model** writes the "No own properties" line and lists its settings compositions in the
  Compositions row. The linter applies the role's rules by folder.
- `Used by` says where the element is registered (a Block Grid, a Block List, a settings model
  for a settings composition). If that is not part of this change, write `—` and name it in the
  summary as the follow-up.
- A missing `Elements`, `Elements/Settings` or `Elements/Compositions` folder is listed in
  Dependencies as a `document-type-container`, `New in this changeset`, with its index page from
  [folder-index.md](../../umbraco-content-requirements-documentation/assets/folder-index.md).
- A new **settings composition**'s `Used by` is `—`. The changeset summary names the settings
  models that should compose it as the follow-up for
  [umbraco-add-block](../../umbraco-add-block/SKILL.md) or
  [umbraco-change-document-type](../../umbraco-change-document-type/SKILL.md); no existing
  settings model is updated in this change.

The changeset's checklist, in apply order:

```
- [ ] 1. Create data type folder `<Editor kind>` and data type `<Name>`                       (only if new)
- [ ] 2. Create document type folder `Elements` (or `Elements/Settings`, `Elements/Compositions`)   (only if missing)
- [ ] 3. Create element `<alias>`, move it into `<role folder>`, then fix-up (sorts, mandatory, descriptions, culture)
- [ ] 4. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

Call out whether the element is shared (a settings composition or settings model other blocks
will reuse), then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps without the MCP, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For an element, also:

- **Create, then move, then fix-up.** `create-element-type` has no folder parameter, so the
  element is created at the tree root
  ([apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#tools)).
  Move it with `move-document-type` into its role's folder straight away, looking up the folder
  id at apply time, and confirm the move with `get-document-type-ancestors`.
- **The container.** A content element or item passes `group: "Content"` and no tab; the fix-up
  sets the group's sort to 0. A settings composition passes its tab (`Style` or `Settings`) and
  no group. A settings model passes no properties and its compositions' ids in `compositions`.

## 6. Verify and report

For an element, also confirm on the read-back that it sits in its role's folder, that a content
element or item has no tab and exactly one `Content` group, that a settings composition has its
tab at the right sort and no group, and that a settings model has no own properties and lists
exactly the compositions in the requirements doc.

In the report, what is left is: registering the element where it will be used
([umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md)), pairing it with
its settings or its items if those were not part of this change
([umbraco-add-block](../../umbraco-add-block/SKILL.md)), composing a new settings composition into
the settings models that need it ([umbraco-add-block](../../umbraco-add-block/SKILL.md) or
[umbraco-change-document-type](../../umbraco-change-document-type/SKILL.md)), anything in the
Apply log, and the Razor partial that renders it, which is implementation work.
