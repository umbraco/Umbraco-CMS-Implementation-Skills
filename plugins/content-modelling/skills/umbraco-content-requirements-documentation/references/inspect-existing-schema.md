# Inspect the existing schema

Read the site before writing a requirements doc. An empty project and a mature one need different
answers, and a requirements doc that names a data type or composition must say truthfully whether it
`Exists`.

## What to establish

| Question | Why it matters |
|---|---|
| Which document types, element types and folders exist, and where | Naming clashes, and whether `Compositions/`, `Data/`, `Elements/` need creating |
| Which compositions exist | Reuse one instead of writing a second |
| What is allowed at root | Whether there is a home page to hang new types under |
| Which data types exist, in which folders | Reuse before creating; which editor-kind folders exist |
| Which templates exist, and which is the master | Where new page templates go |
| How many languages | Only ask about varying by culture when there is more than one |
| Whether the project already follows a different convention | The project's convention wins; note the departure |

## With the Umbraco Developer MCP

Use read tools only. Tool names here were checked against `@umbraco-cms/mcp-dev` 17.6.8; if the
connected tool list differs, trust the list.

| Need | Tool | Note |
|---|---|---|
| Every document type and folder | `get-all-document-types` | Returns name, id, `isElement`, `isFolder` and parent. **Not the alias** |
| Alias, properties, containers, templates of one type | `get-document-type-by-id` | The only way to learn an alias. Do not infer it from the name |
| Several types at once, in full | `get-document-type-batch` | The full shape for a list of ids: read `allowedAsRoot` here to find root types |
| Several types at once, light | `get-document-types-by-id-array` | Name, icon, `isElement` only. It returns **neither the alias nor `allowedAsRoot`** |
| Find a type by name | `get-document-type-tree-search` | |
| Where a type sits | `get-document-type-ancestors` | Returns the folder chain |
| What a type allows | `get-document-type-allowed-children` | The same list as `allowedDocumentTypes` on the full read |
| What uses a composition | `get-document-type-composition-references` | |
| Data types | `get-all-data-types`, `find-data-type`, `get-data-type` | `get-data-type` returns the configuration values |
| What uses a data type | `get-references-data-type` | |
| Templates | `get-template-root`, `get-template-children`, `get-template` | Children of a template are the pages that use it as master |
| Languages | `get-language` | Lists every language. Needs the `language` tool collection. `get-language-items` returned an empty list when called with no ISO codes, so do not count languages with it |
| Icons | `get-icons` | The package's built-in list, names only; see below |

Two tools that older guidance names **do not exist**: `get-document-type-allowed-at-root` and
`search-document-type`. Use `get-all-document-types` with `get-document-type-batch` (read
`allowedAsRoot`), and `get-document-type-tree-search`.

`get-icons` returns the icon list built into the MCP package, not the site's: an icon a package
or the project added cannot be confirmed with it, and neither can a colour class.

The server exposes tools by collection. Inspection and apply need `document-type`, `data-type`,
`template` and `language`; if a tool is missing, the collection is probably not enabled in the
MCP configuration (`UMBRACO_INCLUDE_TOOL_COLLECTIONS`). Say which collection is missing.

## Without the MCP

If the MCP is not connected or the site is not running, do not guess and do not describe the
schema from memory. Either:

- read an existing requirements folder, if the project has one, and treat its `applied` pages as the
  record of what exists; or
- ask the user the questions in the table above, in one message.

Whatever was not confirmed is written as `Missing`, or as `Exists` with a note in the changeset
summary that it is the user's word and was not read from the site.
