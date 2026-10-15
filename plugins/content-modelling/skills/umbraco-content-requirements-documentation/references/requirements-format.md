# Requirements doc format

One markdown page per artefact, in a folder that mirrors the backoffice trees. Start every page
from the matching template in `assets/` and replace every `<Placeholder>`; the linter rejects any
that are left.

## Folder layout

```
docs/umbraco-schema/
    README.md                         records the requirements doc location; links to the indexes
    .order                            optional: page order for wikis that read it
    _changesets/
        2026-09-30-article-section.md
    Data-Types.md                     folder index
    Data-Types/
        Toggle.md                     folder index
        Toggle/Toggle-default-on.md
    Document-Types.md
    Document-Types/
        ArticlePage.md                pages (types with a template) at the root, as in the backoffice
        Compositions/SeoComposition.md
        Data/AuthorFolder.md
        Elements/Accordion.md
        Elements/Compositions/...     settings compositions
        Elements/Settings/...         settings models
    Templates.md
    Templates/Master.md
    Templates/Master/ArticlePage.md   a page template sits under its master
```

The folder a page is in decides which rules apply to it, so put it where the artefact lives in
the backoffice. The tree itself is defined in
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md).

| Artefact | File name | Template |
|---|---|---|
| Document type (page, data folder or item, site settings) | the alias in PascalCase: alias `articlePage` is `ArticlePage.md` | [document-type.md](../assets/document-type.md) |
| Composition | `SeoComposition.md` | [composition.md](../assets/composition.md) |
| Element type, settings model, settings composition | `AccordionItem.md` | [element-type.md](../assets/element-type.md) |
| Data type | the name with each run of other characters as one hyphen: `Toggle-default-on.md`; its editor-kind folder likewise: `Text-Area/`, `Block-Grid/` | [data-type.md](../assets/data-type.md) |
| Block List or Block Grid data type | as above | [block-data-type.md](../assets/block-data-type.md) |
| Collection view data type | as above | [collection-view-data-type.md](../assets/collection-view-data-type.md) |
| Template | the alias in PascalCase | [template.md](../assets/template.md) |
| Folder index | the folder name, beside the folder | [folder-index.md](../assets/folder-index.md) |
| Changeset | `<yyyy-mm-dd>-<slug>.md` in `_changesets/` | [changeset.md](../assets/changeset.md) |

## Page layout

Every requirements page has, in this order:

1. `# Name`, the artefact's display name.
2. The status line, `> **Status:** proposed`. Values are in
   [requirements-lifecycle.md](requirements-lifecycle.md). An element type adds a second blockquote
   saying it is an element type.
3. `## Definition`, a `| Setting | Value |` table. The rows are fixed per kind; keep every row
   in the template and use an em dash for a value that does not apply. The one optional row is
   `Collection` (the backoffice's "Collection" setting, the `Umbraco.ListView` editor): keep it on
   a listing page and delete it everywhere else. Element types have no template rows; they do
   have `Vary by culture`, because a block can vary by language only when its element type does.
4. `## Properties` (content types) or `## Configuration` (data types). Templates have neither.
5. `## Used by`, what refers to this artefact.
6. `## Dependencies`, what this artefact needs.

There are no GUID, UDI or Deploy rows. Identity is the alias (or the name, for a data type); ids
are looked up at apply time and never written into a requirements doc.

### Writing values

- An empty value is an em dash, `—`. Never leave a cell blank.
- Aliases, editor aliases, value types, icons and file names are in backticks.
- A reference to another artefact with a requirements page is a **relative markdown link** to that
  page. A reference to something that already exists on the site and has no requirements page is
  plain text, and it must then appear in Dependencies with the flag `Exists`.
- `Folder` is a breadcrumb of links to the folder index pages.
- Yes and No are written `Yes` and `No`.

### Properties

Exactly twelve columns:

```
| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
```

One row per property the type defines itself. Inherited properties are not repeated. A type with
none writes one line instead of the table: `No own properties: everything is inherited from the
compositions listed above.`

Element types write `—` for Tab and Tab Sort, `Content` for Group and `0` for Group Sort. The
sorts to use are in
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md).

The element-type template serves three roles, and the folder decides which rules the linter
applies:

| Role | Folder | Properties |
|---|---|---|
| Content element or child item | `Elements/` | As above: no tab, one `Content` group, sort 0 |
| Settings composition | `Elements/Compositions/` | Tab `Style` (50) or `Settings` (100), no group: replace the `—`/`Content` cells |
| Settings model | `Elements/Settings/` | No own properties; write the one-line form and list its settings compositions in the Compositions row |

**Culture.** When a type's `Vary by culture` is `Yes`, every property in the table varies too.
A property that must stay the same in every language is named in one line directly after the
table: `Invariant: ` followed by the aliases in backticks, comma-separated. A type that does not
vary has no such line.

### Dependencies

```
| Artifact | Type | Flags |
```

`Type` is one of `data-type`, `document-type`, `template`, `data-type-container`,
`document-type-container`. `Flags` is exactly one of:

| Flag | Meaning |
|---|---|
| `Exists` | Already on the site; confirmed when the schema was inspected |
| `New in this changeset` | Created by this changeset; must link to its requirements page |
| `Missing` | Needed, not on the site and not in the changeset. The requirements doc cannot be approved until this is resolved |

Every data type named in Properties must be listed. A folder is listed as a container and links
to its index page.

## Index pages and `.order`

An index page is optional. It is named after its folder, starts with `> Folder index: lists the
requirements docs in this folder.` and contains one link per page. A `.order` file lists the page
names in a folder, one per line and without the extension, for wikis that use it.

## Changeset

A changeset lists the requirements docs a change consists of, the order they are applied in and a
tick-list. It is the document the user approves. Sections: `## Summary`, `## Requirements pages` (an
`| Order | Requirements page | Kind | Action |` table; Action is `Create` or `Update`), `## Apply
checklist` (one `- [ ]` item per step, in apply order, ending with allowed children and
verification) and `## Apply log`. The order is defined in
[apply-via-mcp.md](apply-via-mcp.md#order).

## Linting

```bash
node scripts/lint-requirements.mjs docs/umbraco-schema
```

Run it from this skill's folder, or give the full path to the script. It needs Node.js and
nothing else. It exits 0 when there are no errors, 1 when there are, and prints one line per
finding with the file, line and rule. It checks: required headings and Definition rows, the
twelve-column header, the status line, alias casing, suffix by folder and reserved aliases, tab
sorts against the global table, element and composition shape, a template named after the alias
for every page (a type with a template), the `Collection` row only on a listing, the `Folder`
breadcrumb, the dependency types and flags, the changeset's actions and that an applied changeset
has every page applied and every step ticked, that links resolve, and that no `<placeholder>` is
left, whatever its casing. Four things are warnings rather than errors: a property sort that is
not a multiple of 100, a data type name that follows no pattern, a page whose Compositions row has
no SEO composition (`page-seo`; every page takes it, but a page every changeset lists only as an
Update is left alone), and a name, tab, group or description containing `?`, `&`, a
percent-encoded sequence or `../`, which the Umbraco Developer MCP refuses.

If Node.js is not available, say the requirements doc was not linted. Do not report it as
lint-clean.
