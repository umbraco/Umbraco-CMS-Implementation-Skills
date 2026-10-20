---
name: umbraco-add-data-folder
description: >
  Add shared content that is not a page to an Umbraco 17+ site: a data folder document type at the
  content root, the data item it holds (authors, categories, locations, reusable content) and the
  picker data type pages use to choose items, including content-driven taxonomy. The change is
  written as a requirements doc, approved, applied through the Umbraco Developer MCP or a manual
  backoffice walkthrough, and read back.
  Use this whenever the user asks to "add authors", "add categories", "set up a taxonomy", "store
  reusable content", "add a data folder", "create a folder of locations editors can pick from",
  "let articles pick an author", or "where should shared data live in the content tree".
  SKIP: non-Umbraco projects or Umbraco < 17; pages or listing sections (use umbraco-add-page or
  umbraco-add-listing-page); a single plain document type with no folder or picker (use
  umbraco-add-document-type); the site settings singleton (use
  umbraco-add-site-settings); free-form tags (use umbraco-add-data-type).
---

# Add Data Folder

Adds **shared data that belongs to no page**: a folder document type editors create at the root of
the content tree (`authorFolder`) and the item type it holds (`author`). Pages then pick items from
it instead of retyping the same name on every page. It works requirements-first: write the change
down, stop for the user's approval, apply it, then read it back.

"Folder" here is a **document type** that holds content nodes, not the organisational folders in
the Settings tree. The schema of the folder and item types is decided by
[`umbraco-add-document-type`](../umbraco-add-document-type/SKILL.md#used-by-another-skill) with
the role data folder or item; this skill adds the content-tree behaviour and the picker.

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Existing data folders and items, what is allowed at root, the `Data/` folder | [workflow.md](references/workflow.md#1-inspect) |
| 2. Decide | Folder and item names, fields, nesting, the picker and who uses it | [workflow.md](references/workflow.md#2-decide) |
| 3. Taxonomy | Categories and similar controlled lists: data items or tags | [taxonomy.md](references/taxonomy.md) |
| 4. Write and lint | Folder, item, picker data type, the types that gain a picker, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Through the MCP if connected, otherwise a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read everything back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

MCP when connected, otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Version compatibility

Targets **Umbraco 17+**. The picker uses `Umbraco.MultiNodeTreePicker` with the editor UI
`Umb.PropertyEditorUi.ContentPicker`, read from the Umbraco 17.5.3 packages. MCP tool names were
checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Shared data lives in one place.** An author, an office or a category that several pages refer
  to is a content node in a data folder, not a set of numbered fields on the home page and not a
  name retyped into a Textstring on every page.
- **Data is not a page.** Folders and items have no template and no SEO or sharing compositions.
  They live in `Data/` in the Document Types tree and at the content root beside the home page,
  sorted after it; see
  [allowed-children-and-root.md](../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root).
- **A controlled list is content, not tags.** Categories, topics and regions that the site filters
  or groups by are data items picked by id; free-form tags are for editor-invented keywords. The
  choice is in [taxonomy.md](references/taxonomy.md).
- **Pick, do not retype.** A page refers to an item through a picker restricted to that folder and
  item type, so renaming an author changes it everywhere.
- **One picker data type per use.** "Pick one author" and "pick up to three categories" are
  different configurations, and each is its own data type.
- **"Just add categories" is a request for the change, not approval** of a requirements doc the
  user has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran
against a live site.
