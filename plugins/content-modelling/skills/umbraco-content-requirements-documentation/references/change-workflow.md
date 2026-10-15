# The six steps every change follows

Every content-modelling skill that changes schema runs the same workflow: inspect, decide, write
the requirements doc, **stop for approval**, apply, verify. This file is that workflow once. A
skill's own `workflow.md` adds only what its artefact needs at each step and links back here; it
does not repeat these steps.

## Before step 1: the requirements folder

Find or agree the requirements folder as
[requirements-lifecycle.md](requirements-lifecycle.md#where-requirements-docs-live) describes.
Ask once; never again once it is recorded in the folder's `README.md`.

## 1. Inspect

Follow [inspect-existing-schema.md](inspect-existing-schema.md) for the tools and for what to do
without the MCP. Aliases come only from `get-document-type-by-id` or `get-document-type-batch`;
the tree and array tools do not return them.

Without the MCP, ask the skill's inspection questions in **one** message, and treat every answer
as the user's word, not as something read from the site: it is flagged `Exists` with a note in the
changeset summary, never presented as a read-back.

Two questions belong to every change:

| Question | Why it matters |
|---|---|
| How many languages? | Vary by culture is asked about only when there is more than one |
| Does the project follow a different convention? | The project's convention wins; say which rule it departs from and stay consistent |

## 2. Decide

Work through the skill's decision table in order. Each row names the rule to open in
[`umbraco-content-model-conventions`](../../umbraco-content-model-conventions/SKILL.md); apply the
rule rather than restating it. Decide the name, folder, tabs and sorts **before** writing anything:
an alias is read by templates, models and queries, and renaming it later is a breaking change.

## 3. Write the requirements doc

Follow [requirements-format.md](requirements-format.md): one page per artefact from the templates
in `assets/`, plus a changeset from [changeset.md](../assets/changeset.md) at
`_changesets/<yyyy-mm-dd>-<slug>.md`. File names are the alias in PascalCase (`ArticlePage.md`) or
the data type name as a hyphen slug (`Toggle-default-on.md`, in `Toggle/`).

- Anything that already exists on the site and has no requirements page is written as plain text
  and flagged `Exists` in Dependencies.
- A `Folder` breadcrumb links to the folder index pages; write a missing index from
  [folder-index.md](../assets/folder-index.md).
- An existing type the change edits is an **Update** page, written as
  [requirements-lifecycle.md](requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes. Never write a partial page.
- The changeset's checklist is in apply order ([apply-via-mcp.md](apply-via-mcp.md#order)),
  one `- [ ]` item per step, ending with allowed children and then verification.

Then lint the folder:

```bash
node <path-to-this-skill>/scripts/lint-requirements.mjs <requirements-root>
```

until it reports no errors. If Node.js is not available, say the requirements doc was **not
linted**; never call it lint-clean.

## 4. Stop for approval

Follow [the approval gate](requirements-lifecycle.md#the-approval-gate). Show the changeset's
summary and its list of requirements pages, and call out two things: every **existing** artefact
that will change, and anything that rests on the user's word rather than a read of the site. Then
stop.

A request to "just create it", however firm, is a request for the artefact; it is not approval
of a requirements doc the user has not yet seen. If the user asked for the backoffice steps and
the MCP is not connected, give the walkthrough in the same reply as
[apply-manually.md](apply-manually.md#asked-for-the-steps-before-approval) describes; the status
stays `proposed`.

## 5. Apply

With the MCP, follow [apply-via-mcp.md](apply-via-mcp.md), including the fix-up pass after every
create. Without it, follow [apply-manually.md](apply-manually.md). Look up every id by name or
alias at apply time; never take one from a requirements doc or from memory. An existing type is
read, modified and written back whole: a body that leaves out a property deletes that property.

## 6. Verify and report

Follow [verify.md](verify.md): read every artefact back, compare field by field (property count
first), and set each status line. Then report, separately:

1. What was created or changed, by name.
2. What was **read back and matched**, and what was only confirmed by the user or not checked.
   The icon colour is always unchecked.
3. What is left: anything in the Apply log, work deferred to other skills, and the implementation
   work (markup, controllers, partials) that is outside a schema change.
