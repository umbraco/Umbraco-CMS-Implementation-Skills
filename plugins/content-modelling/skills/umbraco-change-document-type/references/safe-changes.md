# Which changes are safe

Every type with content behind it is a store of values. Classify each part of the request before
writing the requirements doc, and say the classification in the changeset summary so the user
approves the loss, not just the change.

| Change | Class | Why | What to offer |
|---|---|---|---|
| Add a property | Safe | New property, empty on every node | — |
| Reorder properties, tabs or groups | Safe | Changes where fields are shown, not what is stored | — |
| Move a property to another tab or group of the **same type** | Safe | The value stays on the property | — |
| Change a name (type, tab, group or property) | Safe | The alias, which code and storage use, is unchanged | — |
| Change a description | Safe | — | — |
| Change the icon | Safe | — | — |
| Change allowed children or allowed at root | Safe | Existing nodes stay where they are; only what can be created next changes | Say if a type becomes uncreatable |
| Add a composition | Safe, unless an alias clashes | New fields, empty on every node; Umbraco refuses the composition if the type already defines one of its aliases | Leave a clashing type out and say why |
| Change the default or allowed templates | Safe | Rendering changes; content does not | Keep the old template allowed until the new one renders |
| Move a type into a folder | Safe | A tree position, nothing else | — |
| Make a property mandatory | Needs confirmation | Nodes with the field empty cannot be saved until it is filled | Count the nodes; offer a default where the data type has one |
| Change a property to a **different data type of the same editor** | Needs confirmation | Values stay; the new configuration may make some invalid (a shorter limit) | Say which values would fail validation |
| Tighten a data type's limits | Needs confirmation | As above, for every property using it | — |
| Change a property to a data type of a **different editor** | Loses content | Values are stored in the old editor's format and are not converted | A new property with the new data type, a migration, then removal of the old one |
| Remove a property | Loses content | Every stored value on every node goes with it | Keep it until the values are migrated, or confirm the loss |
| Remove a composition from a type | Loses content | The composition's properties, and their values on this type's nodes, go with it | As above |
| Rename an alias (type or property) | Loses content | Umbraco stores values by property alias; a changed alias is a new, empty property to the site, and templates, models and queries that read the old one break | See below |
| Convert a page type to an element type, or back | Loses content | The content tree and block storage are different stores | Model it afresh |

Umbraco itself refuses two things, so they never reach the requirements doc: a composition on a
type that is itself used as a composition, and a property alias that a composition already
supplies; see
[compositions.md](../../umbraco-content-model-conventions/references/compositions.md).

## Renaming

Ask which the user means.

- **The name** (what editors see): change it. Safe, no code impact.
- **The alias** (what code reads): do not rename in place. Specify a **new property** with the new
  alias beside the old one, name the migration that copies the values (code work outside this
  change, see `umbraco-common-pitfalls` in the implementation plugin), and a later change that
  removes the old property once the migration has run. The same holds for a type alias, which
  also renames its Models Builder class and template lookup.

If the user insists on renaming the alias in place, say plainly that every node of the type will
show the field empty afterwards and that templates reading the old alias will stop rendering it,
and leave the status `proposed` until they confirm in those words.

## Counting what is affected

With the MCP, `get-document-type-composition-references` lists the types that compose a
composition, and `get-references-data-type` the properties using a data type. How many **content
nodes** use a type is a content question the schema tools do not answer: if the `document`
collection is connected, count with its search; otherwise ask the user, and say in the summary
that the count is their word.

**Related:** [workflow.md](workflow.md).
