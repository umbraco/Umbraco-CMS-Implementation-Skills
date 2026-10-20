# Requirements doc lifecycle

## Where requirements docs live

The default location is `docs/umbraco-schema/` in the user's project.

1. Look for an existing requirements folder: a `README.md` that records the location, or a folder
   holding `Document-Types/` and `_changesets/`. If one exists, use it and do not ask.
2. Otherwise ask **once**: "I'll keep the schema requirements docs in `docs/umbraco-schema/`. Is
   that the right place?"
3. Record the answer in that folder's `README.md` so no later session asks again.

## Status

Every requirements page and every changeset carries one status line directly under the title.

| Status line | Meaning | Who sets it |
|---|---|---|
| `> **Status:** proposed` | Written, not yet agreed. Nothing on the site reflects it | The agent, when writing the requirements doc |
| `> **Status:** approved` | The user has agreed to it. It may be applied | The agent, only after the user says so |
| `> **Status:** applied 2026-09-30 via MCP` | Created through the MCP and read back | The agent, after verification |
| `> **Status:** applied 2026-09-30 manually` | The user carried out the backoffice steps | The agent, after the user confirms |

The date is the day it was applied, as `yyyy-mm-dd`.

## The approval gate

After writing and linting the requirements doc, **stop**. Show the user the changeset: its summary,
the list of requirements docs and anything marked `Missing`. Ask for approval in plain words. Do not
apply in the same turn unless the user has already said, in this conversation, that the change is
approved.

These do not count as approval: the user asking for the type in the first place, the linter
passing, or an earlier changeset having been approved.

When the user approves, set the changeset and each of its requirements docs to `approved`, then
apply. A changeset may not be `approved` while one of its requirements docs is `proposed`, or while
a dependency is `Missing`; the linter reports both.

If the user asks for changes, edit the requirements docs, lint again and ask again. The status stays
`proposed`.

## Applying and resuming

Apply in the order of the changeset's checklist and tick each item as it completes. If the work
is interrupted, the next session reads the checklist and continues from the first unticked item
after confirming, by reading the site, that the ticked items really exist.

Anything that could not be done as specified goes in the changeset's `## Apply log`: what, why and
what the user must do by hand. Do not quietly alter a requirements doc to match what the tooling
could manage.

A changeset becomes `applied` when every item is ticked and every requirements doc has been
verified.

## Changing something already applied

An applied requirements doc is a record of what the site has. To change it, write a new changeset
with the action `Update`, edit the requirements page and set that page back to `proposed`. The
earlier changeset is left as it is.

## Updating a type that already exists

Many changes edit an existing type: a parent gains an allowed child, a page takes a composition,
a composition gains a group. The type is listed in the changeset with the action `Update`, and its
page is written in one of three ways, in this order of preference:

1. **It already has a requirements page.** Edit that page, set its status back to `proposed`, and
   list it as `Update`.
2. **It has none and the MCP is connected.** Read it with `get-document-type-by-id` and write its
   page from the read-back, **complete**: every Definition row and every property, not only the
   part that changes. If the read-back does not give every row the page needs, treat the type as
   in case 3. Never write a partial page, and never fill a cell with a placeholder such as
   "Unchanged".
3. **It has none and the MCP is not connected.** Do not reconstruct a page you cannot read. Name
   the type as plain text flagged `Exists`, make the change a checklist item in the changeset, and
   say in the summary that the type's current shape is the user's word.

An Update page is applied by reading the live type, changing only what the page says, and
writing the whole body back; the property count must be unchanged afterwards.
