# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a document type's schema needs at each step. The role, decided first, is
in [type-roles.md](type-roles.md). When another skill calls this one, it runs steps 1 to 3 for the
schema rows and hands the page back; the caller adds the behaviour rows and owns the rest.

## 1. Inspect

For a document type, these are the answers you need before deciding anything:

| Question | Why it matters here |
|---|---|
| Does a type with this name or alias already exist? | Do not create a second one; offer to extend the first with [umbraco-change-document-type](../../umbraco-change-document-type/SKILL.md) |
| Does the role's folder exist (`Data/`, `Compositions/`)? | A missing folder joins the change first, with its index page |
| Which compositions exist, and which fields does each give? | A page takes SEO and the others by kind; a non-page takes none. A type does not repeat a composition's fields |
| Which data types exist for the fields? | Reuse `Textstring`, `Richtext editor`, the pickers or a project type before anything new |
| What will allow this type as a child? | A type nothing allows cannot be created; the parent is the caller's or the user's decision, named in the summary |
| How many languages? | Vary by culture is asked about only when there is more than one |

## 2. Decide

Work through this table in order. Each row names the rule to open; apply it rather than
restating it.

| Decision | For a document type | Rule |
|---|---|---|
| Role | Page, data folder or item, site settings, or a standalone plain type | [type-roles.md](type-roles.md#settling-the-role) |
| Name and alias | Title Case name, camelCase alias with acronyms collapsed; the suffix by role: `...Page`, `...Folder`, `...Item` or a plain noun, `siteSettings` | [naming.md](../../umbraco-content-model-conventions/references/naming.md) |
| Folder | By role: the Document Types root for a page or site settings, `Data/` for everything else | [tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#document-types), [type-roles.md](type-roles.md) |
| Icon and colour | A noun that fits; `color-light-blue` for a page, `color-green` for data and settings | [icons-and-colours.md](../../umbraco-content-model-conventions/references/icons-and-colours.md) |
| Description | What the type is for | [descriptions.md](../../umbraco-content-model-conventions/references/descriptions.md) |
| Tabs, groups and sorts | Own fields on the `Content` tab (100), or a type-specific tab at 0; site settings tabs 0 to 9; property sorts in hundreds | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md) |
| Own properties | Only what no composition gives; area-prefixed aliases, booleans as predicates, pickers for links and references | [property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md), [editor-selection.md](../../umbraco-add-data-type/references/editor-selection.md#choices-that-come-up-often) |
| Data types | Existing ones first; a field with a configuration nothing on the site has gets its own data type | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) |
| Compositions | A page: the SEO composition always, the others by kind. A non-page: none | [when-to-apply.md](../../umbraco-add-composition/references/when-to-apply.md#which-types-take-it), [compositions.md](../../umbraco-content-model-conventions/references/compositions.md) |
| Vary by culture | `No` on a single-language site; otherwise ask, and name any invariant properties | [requirements-format.md](../../umbraco-content-requirements-documentation/references/requirements-format.md#properties), Culture paragraph |

### Own properties

Give the type a field only when no composition already supplies it, and reuse an existing data
type whenever its configuration fits. Check each new alias against the compositions the type takes
and against the reserved aliases in
[property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md):
a clash fails on create. When a field genuinely needs a configuration no data type on the site
has, the new data type joins the changeset as its own requirements page, and reuse or create is
decided by [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md). Block editors are
configured separately, by
[umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md). A type with no
own properties writes the one line the requirements doc format gives for that case; a page made
only of compositions is normal.

### Behaviour rows

The behaviour rows are not decided here. For a page they are `add-page`'s: template, allowed at
root, allowed children. For a data folder or item they are `add-data-folder`'s; for site settings
`add-site-settings`'. A standalone plain type fills them in itself:

| Row | Value |
|---|---|
| Allowed at root | `No` |
| Default template, Allowed templates | `—` |
| Allowed children | `—` unless the user names children |
| Collection | Row deleted; it belongs to listing pages only |

A plain type that no parent allows cannot be created in the content tree. Say so in the summary
and, when the user names a parent, add it as an Update (step 3).

### A non-page asked for with page things

- **A template** ("no template needed yet" counts too: the name says page). A type that visitors
  land on is a page; hand off to [umbraco-add-page](../../umbraco-add-page/SKILL.md), which brings
  the template, the SEO composition and the parent in one change. Do not write a template-less
  `...Page` at the root.
- **The SEO composition on a data item or settings.** Decline it and say why: nothing renders the
  node, so search engines never see the fields
  ([type-roles.md](type-roles.md#what-a-non-page-never-gets)). If the user wants the thing found
  on the website, that is a page for it, by `add-page`.
- **Allowed at root.** Only the home page, site settings and data folders are. A plain type is
  made creatable by a parent's allowed children, which is an Update of that parent.

## 3. Write the requirements doc

A document type's schema is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| Any new data type for a field | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The document type | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/<AliasPascalCase>.md` for a page or site settings; `Document-Types/Data/<AliasPascalCase>.md` otherwise | Create |
| The parent that will allow it, when the user names one and this skill runs on its own | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | Where it lives | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to a document type:

- Delete the `Collection` row unless the caller is writing a listing page.
- A type in `Data/` writes `—` for Default template, Allowed templates and Compositions; the
  linter rejects a template there and a `...Page` or `...Composition` alias there.
- A missing `Data` folder is listed in Dependencies as a `document-type-container`, `New in this
  changeset`, with its index page from
  [folder-index.md](../../umbraco-content-requirements-documentation/assets/folder-index.md).
- When called by another skill, the page is handed back with the behaviour rows still to be
  filled by the caller; never write a partial page into the changeset.

The changeset's checklist for a standalone type, in apply order:

```
- [ ] 1. Create data type folder `<Editor kind>` and data type `<Name>`        (only if new)
- [ ] 2. Create document type folder `Data`                                   (only if new)
- [ ] 3. Create document type `<alias>` in `Data`, then fix-up (sorts, mandatory, descriptions, culture)
- [ ] 4. Set allowed children on `<parent alias>`: add `<alias>`, keeping the existing entries   (when a parent was named)
- [ ] 5. Verify every requirements doc against the site and set each status line
```

When called by another skill, rows 3 and 4 become that skill's rows in its own order.

## 4. Stop for approval

Call out any existing type that will change (a parent gaining a child), and anything resting on
the user's word rather than a read of the site, then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For a document type, also:

- **The create.** `create-document-type` takes the `Data` folder's id as `parentId`, looked up at
  apply time, for every role but page and site settings, which sit at the root and pass none.
  Pass the compositions' ids in `compositions`; a non-page passes none.
- **The fix-up** sets tab and group sorts, property sorts, mandatory flags, descriptions and
  culture variation, as
  [apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#the-fix-up-pass)
  describes. Templates, collection and allowed at root are the caller's fix-up.
- **The parent**, when one is named: read it, append the new type to `allowedDocumentTypes` after
  the entries it has, write the whole body back, and confirm the property count is unchanged.

## 6. Verify and report

For a document type, also confirm on the read-back that it sits in the right folder, that a
non-page has no template and no SEO or sharing composition, and that every tab and property sort
matches the requirements doc.

In the report, what is left (item 3) names: the parent or folder that will hold the type's nodes
if none was updated, any data type deferred to another skill, and the Razor or C# that reads the
type, which is implementation work.
