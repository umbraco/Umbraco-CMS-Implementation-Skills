---
name: umbraco-add-listing-page
description: >
  Add a listing section to an Umbraco 17+ site: the listing page type, the item page type it
  lists, the listing's own collection view data type and the allowed-children pairing between
  them, with both templates, hung under the right parent. The change is written as a requirements
  doc, approved, applied through the Umbraco Developer MCP or a manual backoffice walkthrough, and
  read back.
  Use this whenever the user asks to "add a blog", "add a news section", "create a listing page",
  "add an events listing", "add an articles section with a list of articles", "set up a list view
  for the news items", or "configure the collection view for this listing".
  SKIP: non-Umbraco projects or Umbraco < 17; one standalone page (use umbraco-add-page); data
  folders and items that are not pages, such as authors or categories (use umbraco-add-data-folder);
  compositions (use umbraco-add-composition); rendering the list, paging or filtering in Razor or
  C#.
---

# Add Listing Page

Adds **a listing pair**: a listing page (`articleListingPage`) and the item page it lists
(`articlePage`), made together because neither is useful alone. The listing gets its own
collection view so editors manage items as a table, and the two types allow each other the way
the conventions say. It works requirements-first: write the change down, stop for the user's
approval, apply it, then read it back.

One standalone page belongs to
[`umbraco-add-page`](../umbraco-add-page/SKILL.md); this skill calls it once for each of the two
pages, so each gets its template and takes the SEO composition, and adds only what the pairing
needs. The schema of each page is decided by
[`umbraco-add-document-type`](../umbraco-add-document-type/SKILL.md#used-by-another-skill) with
the role page.

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Home page, compositions, master template, existing listings and items | [workflow.md](references/workflow.md#1-inspect) |
| 2. Decide the pair | Names, compositions, fields, templates, parent and children | [workflow.md](references/workflow.md#2-decide) |
| 3. Decide the collection view | Columns, ordering, page size, layouts | [collection-view.md](references/collection-view.md) |
| 4. Write and lint | Collection view, two templates, two page types, the parent update, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Through the MCP if connected, otherwise a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read everything back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

MCP when connected, otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Version compatibility

Targets **Umbraco 17+**. The collection view uses `Umbraco.ListView` with the editor UI
`Umb.PropertyEditorUi.Collection`, read from the Umbraco 17.5.3 packages. MCP tool names were
checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Always the pair.** A listing without its item type cannot list anything, and an item type with
  nowhere to live cannot be created. Specify both, and the allowed children between them, in one
  changeset.
- **Name them together**: `<noun>ListingPage` lists `<noun>Page`; see
  [naming.md](../umbraco-content-model-conventions/references/naming.md#suffixes).
- **Each listing has its own collection view**, except listings of the same item type, which may
  share one; see
  [allowed-children-and-root.md](../umbraco-content-model-conventions/references/allowed-children-and-root.md#listings).
- **The listing is not the item's data.** Fields that describe an item (a date, a summary, an
  author) live on the item, or a composition; the listing shows them as columns.
- **Order by something the item holds.** Newest first by a date on the item is usually right; the
  create date is a fallback when the item has none.
- **The parent is an existing type**, updated by a full-body replace: the riskiest call in the
  change, as [`umbraco-add-page`](../umbraco-add-page/SKILL.md#best-practices) says.
- **Every page takes SEO.** Both pages take the SEO composition; if the site has none, it joins
  the change, as [SEO is mandatory](../umbraco-add-page/references/workflow.md#seo-is-mandatory)
  says.
- **"Just add a blog" is a request for the section, not approval** of a requirements doc the user
  has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no assets; its only
code is the Razor layout stub borrowed from
[`umbraco-add-page`](../umbraco-add-page/references/workflow.md#5-apply), which is
uncompiled context that no host builds; for real templates see the
[Umbraco templates docs](https://docs.umbraco.com/umbraco-cms/fundamentals/design/templates.md)
or the implementation plugin. The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site.
