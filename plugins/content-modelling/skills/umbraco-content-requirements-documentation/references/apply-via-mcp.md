# Apply through the Umbraco Developer MCP

Use this when the [Umbraco Developer
MCP](https://docs.umbraco.com/umbraco-in-ai/mcp/cms-developer-mcp) is connected and the changeset is
`approved`. If it is not connected, use [apply-manually.md](apply-manually.md) with the same
requirements doc.

**Trust the connected tool list over this file.** Tool names and parameters change between
versions. What follows was run against `@umbraco-cms/mcp-dev` 17.6.8 on Umbraco 17.5.3 and read
from the package source of 18.1.7. If a tool named here is absent, look for its replacement in
the tool list before giving up, and say which version you found.

## Before you start

- The changeset status is `approved` and the linter reports no errors.
- The tool collections `document-type`, `data-type`, `template` and `language` are enabled (the
  same four that inspection needs).
- Re-read the site for anything the requirements doc flags `Exists`. If it is gone, stop and tell
  the user.
- Look up ids by name or alias at apply time. Never take an id from a requirements doc or from
  memory.

## Order

Work down the changeset checklist, ticking each item as it completes. This is the default order,
because each step needs the ids the earlier ones produce. A skill may state a justified exception
in its own checklist (a data folder's picker data type is created after the folder's content node,
so its start node can be set); the changeset's checklist is then the order to follow.

1. Data type folders, then plain data types.
2. Document type folders (`Compositions`, `Data`, `Elements` and its sub-folders).
3. Compositions.
4. Element types, leaf first. A parent element is created after the Block List data type that
   holds its children.
5. Block List and Block Grid data types, each after the elements it registers.
6. Collection view data types.
7. Templates, master first.
8. Page types, data folders and data items.
9. Allowed children, last, once every type exists.

## Tools

| Step | Tool | What to know |
|---|---|---|
| Folder | `create-data-type-folder`, `create-document-type-folder` | Returns **no body**, so no id. Pass your own `id` (a new UUID), then confirm with `get-data-type-folder` or `get-document-type-folder` |
| Data type | `create-data-type` | `name`, `editorAlias`, `editorUiAlias`, `values` (an array of `{alias, value}`), `parentId` for the folder. Returns the id. Copy the shape of `values` from an existing data type of the same editor with `get-data-type` |
| Composition, page, data folder or item | `create-document-type` | `name`, `alias`, `icon`, `description`, `parentId`, `allowedAsRoot`, `compositions`, `collection`, `allowedDocumentTypes`, `properties`. Returns the id. `allowedDocumentTypes` is useful only for types that already exist; a type's own id, and ids of types later in the changeset, are set in the fix-up |
| Element type | `create-element-type` | `name`, `alias`, `icon`, `description`, `compositions`, `properties` only: **no `parentId`, `allowedAsRoot` or `collection`**. It is created at the tree root and then moved |
| Move an element into `Elements/` | `move-document-type` | `{ id, data: { target: { id: <folder id> } } }`. Returns no body |
| Template | `create-template` | `name`, `alias`, `content`. There is no parent parameter: a page template is placed under the master by `Layout = "master.cshtml";` in its content |
| Change a template | `update-template` | `{ id, data: { name, alias, content } }`. Returns no body; read it back with `get-template` |
| Everything the create could not set | `update-document-type` | The fix-up pass, below |
| Read back | `get-document-type-by-id`, `get-data-type`, `get-template` | See [verify.md](verify.md) |

Each property passed to a create tool accepts only `name`, `alias`, `dataTypeId`, `tab` and
`group`. A create therefore leaves, on every type:

- tab and group sorts numbered 0, 1, 2 in order of first appearance;
- property sorts numbered 0, 1, 2 in array order, across the whole type;
- every property not mandatory, with no description, and with its own `variesByCulture` false;
- no default template, no allowed templates, and not varying by culture.

## The fix-up pass

Run it after **every** `create-document-type` and `create-element-type`.

1. **Read**: `get-document-type-by-id` for the id the create returned.
2. **Modify** that body, and only what the requirements doc says:
   - each entry in `containers`: set `sortOrder` to the Tab Sort or Group Sort;
   - each entry in `properties`: set `sortOrder`, `description` and `validation.mandatory`;
   - `defaultTemplate` to `{ "id": <template id> }` and `allowedTemplates` to a list holding it;
   - `variesByCulture` on the type **and on each property** (every property varies when the
     type does, except those the requirements page lists as `Invariant`; a property has its own
     flag and the create left it false), and `collection` to
     `{ "id": <collection view data type id> }` for a listing;
   - `allowedDocumentTypes`, in the last step, as
     `[{ "documentType": { "id": <id> }, "sortOrder": 0 }, ...]`.
3. **Write**: `update-document-type` with `{ "id": <id>, "data": <the whole body without its id> }`.
   The update replaces the type. A property or container left out of the body is **deleted**, so
   always send back everything that was read.
4. **Verify**: `update-document-type` returns no body, which is not proof of success. Read the type
   again and confirm the property count is unchanged and the values are those in the requirements
   doc.

Never build the update body from the requirements doc alone. Always start from what was read.

## Values the MCP rejects

The server's input sanitiser refuses a string on every text field (names, aliases, tab and group
names, descriptions, template content and ids alike), on create and on update, when it contains:

| Characters | Error says |
|---|---|
| `?` or `&` | "contains query parameter characters" |
| A percent-encoded sequence such as `%20` | "contains percent-encoded characters" |
| `../` or `..\` | "contains a path traversal sequence" |
| A control character (tab excepted) | "contains control characters" |

So a tab named `Terms & Conditions`, or a description ending in a question mark, cannot be
written through the MCP. The requirements linter warns about such values (`mcp-unsafe-value`), so
most are caught before approval; the standard tab names avoid them for this reason.

When a requirements doc value is rejected:

1. Apply the artefact with the nearest accepted value (`Terms and Conditions`; a description
   reworded as a statement).
2. Do **not** change the requirements doc. The requirements doc is what was approved.
3. Record the difference in the changeset's Apply log as a step for the user to finish in the
   backoffice, and tell them.
4. Leave that requirements page `approved`, not `applied`, until the site matches it.

## If a step fails

Stop at the failed step. Leave it unticked, write the error in the Apply log and tell the user what
exists so far. Do not delete what was created to "clean up" unless the user asks. The next attempt
resumes from the first unticked item; see [requirements-lifecycle.md](requirements-lifecycle.md).

## Done

Go to [verify.md](verify.md). Do not mark anything applied from here.
