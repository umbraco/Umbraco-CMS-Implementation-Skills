# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what a page needs at each step. A page's parts are decided by peer skills;
each one's `## Used by another skill` section says what it does when called from here, and
[combined-changeset.md](combined-changeset.md) says how their pages merge into this one changeset.

## 1. Inspect

Understand the page: its kind (the home page, an everyday content page, or a page rendered by code
such as search), its fields, whether it has blocks or a block area, and where it sits. Read the
site for these answers before deciding anything:

| Question | Why it matters here |
|---|---|
| Does a type with this name or alias already exist? | Do not create a second one; offer to extend the first |
| Is there a home page, and what is allowed at root? | Decides the parent, and whether the root question below must be asked |
| What does the home page allow now? | The parent update keeps every existing entry |
| Is there an SEO composition? Which other compositions exist, and which fields does each give? | Every page takes SEO; if the site has none it joins this change ([SEO is mandatory](#seo-is-mandatory)). A page takes existing compositions and does not repeat their fields |
| Is there a master template, and what is its alias? | The new template goes under it; if there is none, one joins the change |
| Which data types exist for the page's own fields? | Reuse `Textstring`, `Richtext editor` or a project type before anything new |
| Does the page have a block area, and is there a Block Grid for that placement already? | An existing grid is an Update that keeps its entries; a missing one is created, by `configure-block-editor` |
| Which blocks and settings models exist? | A block the user names that does not exist is made by `add-block`; an existing one is only registered |

## 2. Decide

Reuse first: existing compositions, data types, elements and blocks. For anything missing, apply
that concept's skill and bring its pages into this changeset. Work through this table in order;
each row names the rule or the peer to open.

| Decision | For a page | Rule |
|---|---|---|
| Kind | Root, Content or Programmatic. Listing is handed off | [page-kinds.md](page-kinds.md) |
| Schema: name, alias, folder, icon, description, tabs, own properties, vary by culture | Decided by `add-document-type` with the role page: Title Case name and camelCase alias ending `Page`, the Document Types root, the page colour, own fields on the `Content` tab | [add-document-type workflow](../../umbraco-add-document-type/references/workflow.md#2-decide), [type-roles.md](../../umbraco-add-document-type/references/type-roles.md) |
| Compositions | The SEO composition always; the site's other page compositions by kind | [SEO is mandatory](#seo-is-mandatory), [when-to-apply.md](../../umbraco-add-composition/references/when-to-apply.md#which-types-take-it) |
| Template | Named after the type, under the master | [templates.md](../../umbraco-content-model-conventions/references/templates.md) |
| Allowed at root | `No`, unless the new type is the home page | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root) |
| Parent and allowed children | Per kind; the parent is updated in the same change | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-children) |
| Blocks and the block area | Existing blocks are registered; a named block that does not exist is made by `add-block`; the area is `configure-block-editor`'s approach A | [combined-changeset.md](combined-changeset.md) |

### SEO is mandatory

Every page takes the SEO composition, programmatic pages included: a search results page still
needs a meta title and description. What that means depends on what the site has:

- **The site has an SEO composition.** The page lists it in Compositions, flagged `Exists`.
- **The site has none.** It joins this changeset, written by
  [`umbraco-add-composition`](../../umbraco-add-composition/SKILL.md#used-by-another-skill) with
  its standard fields (the SEO row of
  [standard-compositions.md](../../umbraco-add-composition/references/standard-compositions.md#the-table)):
  `metaTitle`, `metaDescription`, `isIndexable` and `canonicalUrlOverride` on the `SEO` tab (600),
  moving into a `Search` group (0) only once another composition shares that tab.
  `isIndexable` needs a toggle that defaults on; if the site has no such data type, `Toggle
  (default on)` joins the changeset ahead of the composition, with the `Toggle/` folder and its
  index page. The page's Dependencies flag the composition **New in this changeset**, never
  **Missing**: Missing blocks approval, and the composition is in the change.
- **Other existing pages lack SEO.** List them in the changeset summary as optional Updates for
  the user to add at approval. They are not included by default; the change is one page. The
  clash check in
  [when-to-apply.md](../../umbraco-add-composition/references/when-to-apply.md#what-blocks-a-type-from-taking-it)
  applies to each one the user adds.
- **Sharing and page-details compositions** are not added when missing. Name them in the summary
  as a follow-up for `umbraco-add-composition`, and do not flag them Missing.

### Own properties

Give the page a field only when no composition already supplies it, and reuse an existing data
type whenever its configuration fits. When a field genuinely needs a configuration no data type on
the site has, the new data type joins the changeset as its own requirements page, decided by
[`umbraco-add-data-type`](../../umbraco-add-data-type/SKILL.md#used-by-another-skill) and
foldered by [naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types).
A block area is a `mainContent` (or sidebar) property whose Block Grid is decided by
[`umbraco-configure-block-editor`](../../umbraco-configure-block-editor/SKILL.md#used-by-another-skill).
A page with no own properties writes the one line the requirements doc format gives for that case.

### The root question

- **No home page, and the new type is not one.** Do not allow the new page at root to make it
  creatable. Say the site has no home page and ask whether to add one in the same change. If the
  user says no, the page is specified with no parent and the summary says editors cannot create it
  until something allows it.
- **Other page types are already allowed at root.** Report which, and why that departs from the
  convention. Change them only if the user confirms, and then as `Update` pages in the changeset.
- **The new type is the home page.** It is allowed at root and allows the site's existing content,
  listing and programmatic pages. Ask about moving anything else that is at root, as
  [page-kinds.md](page-kinds.md#root) says.

## 3. Write the requirements doc

Write it up once, calling out each existing type that will change. In the common case (the site
has its SEO composition and a master template, and the page needs no new block, block area or
data type) a page is these pages:

| Page | From | Location | Action |
|---|---|---|---|
| Any new data type for an own field | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| The SEO composition, only if the site has none | [composition.md](../../umbraco-content-requirements-documentation/assets/composition.md) | `Document-Types/Compositions/SeoComposition.md` | Create |
| The master template, only if the site has none | [template.md](../../umbraco-content-requirements-documentation/assets/template.md) | `Templates/Master.md` | Create |
| Its template | [template.md](../../umbraco-content-requirements-documentation/assets/template.md) | `Templates/<MasterPascalCase>/<AliasPascalCase>.md` | Create |
| The page | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/<AliasPascalCase>.md` | Create |
| The parent, with the new type added to Allowed children | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/HomePage.md` | Update |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

When the page also needs a block, a block area or a data type, the pages those peers write join
this changeset; [combined-changeset.md](combined-changeset.md) has the full table, the merge rules
and the apply order.

Points specific to a page:

- Delete the `Collection` row; it belongs to listing pages only.
- A content page lists itself in its own Allowed children, as a link to its own page. It does not
  list itself in Dependencies.
- **The parent** is an existing type the change edits. Its page is written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes.

The changeset's checklist for the common case, in apply order:

```
- [ ] 1. Create template `master`                                   (only if new)
- [ ] 2. Create template `<alias>` under `master`
- [ ] 3. Create document type `<alias>`, then fix-up (sorts, mandatory, descriptions, template, culture)
- [ ] 4. Set allowed children on `<alias>`: `<alias>`                (content page only)
- [ ] 5. Set allowed children on `homePage`: add `<alias>`, keeping the existing entries
- [ ] 6. Verify every requirements doc against the site and set each status line
```

A new data type or a new SEO composition, if there is one, comes first, as
[apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#order)
orders: the `Toggle (default on)` data type, then the `Compositions` folder and
`seoComposition` with its fix-up, then the templates.

## 4. Stop for approval

One approval for the whole changeset, including every part listed in
[combined-changeset.md](combined-changeset.md). Call out:

- every **existing** artefact that will change: the parent (it has content on it), an existing
  Block Grid gaining a block, a settings model gaining a settings composition;
- every new **shared** artefact: the SEO composition, a settings model or settings composition
  other blocks will reuse, a block other pages can use, and which existing pages lack SEO and are
  left alone;
- anything that rests on the user's word rather than a read of the site.

What is left after the change belongs in the report (step 6), not here. Then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

Approved, then applied leaves first: data types, compositions, elements and blocks, the block
area, the templates, the page, and finally its place under the parent. The full order is in
[combined-changeset.md](combined-changeset.md#apply-order); each peer's own workflow has the apply
notes for the artefacts it wrote. For the page itself, also:

- **Template content.** Create the page template with only what makes it a page under the master:

  ```cshtml
  @inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
  @{
      Layout = "master.cshtml";
  }
  ```

  The non-generic base class compiles before Models Builder has generated the page's model. The
  markup, and a controller for a programmatic page, are implementation work outside this change.
  A new master uses `Layout = null;` and renders `@RenderBody()` inside a minimal HTML document.
- **The create.** Leave `parentId` out, because a page sits at the tree root. Pass the SEO
  composition's id in `compositions`, looked up at apply time; on a site where it is new, it was
  created earlier in this same checklist.
- **The type's own allowed children.** A content page allows itself, which needs its own id, so
  it is set in the fix-up, not in the create.
- **The parent.** Read the home page with `get-document-type-by-id`, append the new type to
  `allowedDocumentTypes` after the entries it already has, and send the whole body back with
  `update-document-type`. Read it again and confirm the property count is unchanged and the
  allowed children grew by exactly one.

## 6. Verify and report

Check and report: read everything back, then say what is left. For a page, also confirm on the
read-back that the default and allowed templates point at the new template, that the template is
a child of the master, that the page lists the SEO composition, and that the parent lists the new
type. Each peer's workflow says what to confirm for the artefacts it wrote.

In the report, what is left (item 3) names: the template's markup, for a programmatic page the
controller that renders it, the Razor partial view for each new block, the optional sharing or
page-details composition if the site lacks one, and the existing pages without SEO that were
offered and not updated.
