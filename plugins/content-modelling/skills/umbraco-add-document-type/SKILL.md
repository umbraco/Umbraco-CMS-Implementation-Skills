---
name: umbraco-add-document-type
description: >
  Add the schema of one document type to an Umbraco 17+ site: name, alias, folder by role, icon,
  description, tabs, properties, compositions and culture, never a template. Used directly for a
  type visitors never land on; other skills call it for the schema of a page, data folder or site
  settings. Asks whether visitors will land on it and hands a page to umbraco-add-page. Written as
  a requirements doc, approved, applied through the Umbraco Developer MCP or a manual backoffice
  walkthrough, and read back.
  Use this whenever the user asks to "add a document type", "create a document type with these
  fields", or "add a document type with no template".
  SKIP: non-Umbraco projects or Umbraco < 17; anything visitors land on (use umbraco-add-page);
  element types and blocks (use umbraco-add-element-type or umbraco-add-block); compositions (use
  umbraco-add-composition); data folders (use umbraco-add-data-folder); site settings (use
  umbraco-add-site-settings); changing a type that exists (use umbraco-change-document-type).
---

# Add Document Type

Adds the **schema of one document type**: what it is called, where it sits in the Document Types
tree, its tabs and fields, which compositions it takes and whether it varies by culture. Never a
template. A template is what makes a page, and a page belongs to
[`umbraco-add-page`](../umbraco-add-page/SKILL.md), which calls this skill for the page's schema
and adds the behaviour itself. It works requirements-first: write the change down, stop for the
user's approval, apply it, then read it back.

A document type's nodes can still have a URL; nothing renders them as a page without a template.
So whether something is a page is a question of intent: will visitors land on it? See
[the terms](../umbraco-content-model-conventions/SKILL.md#terms) and
[type-roles.md](references/type-roles.md).

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and the six-step workflow from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md)
([change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md));
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Existing types, the role's folder, compositions, data types, languages | [workflow.md](references/workflow.md#1-inspect) |
| 2. Decide the role | Page, data folder or item, site settings, or a standalone plain type | [type-roles.md](references/type-roles.md) |
| 3. Decide the details | Name, alias, folder, icon, description, tabs, fields, compositions, culture | [workflow.md](references/workflow.md#2-decide) |
| 4. Write and lint | The document type, any data type it needs, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Through the MCP if connected, otherwise a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read it back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### Page or document type?

When the user asks for "a document type" and it is not clear whether visitors will land on it,
ask that one question and nothing else: "Will visitors land on this on the website, or is it
data that pages use?" A page is hard to mistake once named: a Careers page, a Contact page. It
goes to [`umbraco-add-page`](../umbraco-add-page/SKILL.md) with its template, however the user
phrased the request, and this skill supplies only the schema. A type visitors never land on stays
here, in its role's folder, with no template.

### How to decide between MCP and manual

MCP when connected, otherwise a manual walkthrough from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Used by another skill

[`umbraco-add-page`](../umbraco-add-page/SKILL.md) calls this skill for a page's schema, with the
role page; [`umbraco-add-listing-page`](../umbraco-add-listing-page/SKILL.md) for each of its two
pages; [`umbraco-add-data-folder`](../umbraco-add-data-folder/SKILL.md) for the folder and item
types; [`umbraco-add-site-settings`](../umbraco-add-site-settings/SKILL.md) for the settings type.
When called from another skill:

- **Decide the schema rows only** and write the document type's requirements page from
  [workflow.md](references/workflow.md#2-decide): name, alias, folder, icon, description, tabs,
  properties, compositions, vary by culture. The caller adds the behaviour (template, allowed at
  root, allowed children, collection) to the same page.
- **Hand back** the page, its dependency flags and its checklist rows (create, fix-up), so the
  caller can merge them
  ([combined-changeset.md](../umbraco-add-page/references/combined-changeset.md)).
- **Do not stop for approval and do not apply.** The caller owns the changeset, the single
  approval gate and the apply.

## Version compatibility

Targets **Umbraco 17+**. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **A template is what makes a page.** This skill never gives a type one. If the thing asked for
  is a page, say so and hand off; see
  [templates.md](../umbraco-content-model-conventions/references/templates.md).
- **The folder says what a type is.** Pages sit at the Document Types root; everything else is in
  `Compositions/`, `Data/` or `Elements/`. A plain type never sits at the root: the linter treats
  anything there, other than `siteSettings`, as a page; see
  [type-roles.md](references/type-roles.md).
- **No SEO or sharing fields on a type that is not a page.** Search engines never see it; see
  [when-to-apply.md](../umbraco-add-composition/references/when-to-apply.md#which-types-take-it).
  If the user wants it found, it is a page.
- **Nothing other than the home page, site settings and data folders goes at root.** A new type
  becomes creatable through a parent's allowed children; see
  [allowed-children-and-root.md](../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root).
- **Decide names and sorts before creating anything**; renaming an alias later breaks templates,
  models and queries. Type the alias by hand; see
  [naming.md](../umbraco-content-model-conventions/references/naming.md#casing).
- **"Just create it" is a request for the type, not approval** of a requirements doc the user
  has not yet seen; the gate is in
  [change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site. The eval fixture under `evals/files/` is this skill's own copy; sibling skills keep
theirs, because eval file paths resolve relative to the skill, so the copies can drift.
