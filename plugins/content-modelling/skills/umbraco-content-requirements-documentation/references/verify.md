# Verify

Applying is not finished until the site has been compared with the requirements doc. A tool call
that returned without an error is not evidence; several of the write tools return nothing at all.

## With the MCP

For every requirements page in the changeset, read the artefact back and compare it field by field.

| Requirements page | Read with | Compare |
|---|---|---|
| Document type, composition, element type | `get-document-type-by-id` | `alias`, `name`, `icon`, `description`, `isElement`, `allowedAsRoot`, `variesByCulture`; every property's alias, data type, container, `sortOrder`, `validation.mandatory`, `description` and its own `variesByCulture`; every container's name, type and `sortOrder`; `compositions`; `allowedDocumentTypes`; `defaultTemplate` and `allowedTemplates`; `collection` |
| Its folder | `get-document-type-ancestors` | The folder chain matches the Folder row |
| Data type | `get-data-type` | `name`, `editorAlias`, `editorUiAlias`, each configuration value |
| Its folder | `get-data-type-ancestors` | The folder matches |
| Template | `get-template` | `name`, `alias` |
| Its master | `get-template-children` on the master | The template is listed |

Compare the **property count** first. A fix-up that sent an incomplete body deletes properties,
and that is the failure this step exists to catch.

Ids in the read-back are resolved to names or aliases before comparing. A data type id means
nothing to the reader of a requirements doc.

## Record the result

- Every field matches: set the page to `> **Status:** applied <yyyy-mm-dd> via MCP`.
- Something differs: leave the page `approved`, write the difference in the changeset's Apply
  log, and tell the user. Do not edit the requirements doc to match the site.
- When every page in the changeset is applied and every checklist item is ticked, set the
  changeset to applied too. Then run the linter once more; it checks that an applied changeset
  has no unticked item and no page that is not itself `applied`.

## Without the MCP

You cannot read the site. Ask the user to confirm each checklist item, and offer the comparison
table above as the list of things to look at in the backoffice. Set
`applied <yyyy-mm-dd> manually` only for what they confirm.

## What to tell the user

State three things separately:

1. What was created or changed, by name.
2. What was **verified by reading it back**, and what was only confirmed by the user or not
   checked at all.
3. What is left for them to do by hand, from the Apply log.

Never say the schema is applied, working or correct when the evidence is only that a create call
returned. The icon colour class is one known gap: `get-icons` returns names only, so a colour
cannot be validated and must be reported as unchecked.
