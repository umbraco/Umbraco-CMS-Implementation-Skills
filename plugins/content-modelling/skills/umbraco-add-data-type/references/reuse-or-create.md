# Reuse or create

Every property needs a data type. The question is always whether an existing one already has the
right editor **and** the right configuration.

## Decide

Work down this table and stop at the first row that fits.

| Situation | Do | Name pattern | Example |
|---|---|---|---|
| A data type with the same editor and the same configuration exists | **Reuse it** | — | `Textstring` for a plain title |
| A built-in default differs by one setting, and the variant will be useful elsewhere | Create a **generic variant** with a bracket qualifier | `<Editor> (<qualifier>)` | `Toggle (default on)`, `Text Area (3 rows)` |
| The configuration exists for one purpose: a limit, a list of values, a media type filter | Create a **purpose-specific** data type | `<Subject> <Editor kind>` | `Meta Description Text Area`, `Alignment Dropdown` |
| A block editor or a collection view | Not this skill | — | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md), [umbraco-add-listing-page](../../umbraco-add-listing-page/SKILL.md) |

Names and folders are defined in
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types) and
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types);
[naming-and-folders.md](naming-and-folders.md) adds how to apply them.

"Same configuration" means every setting, not the editor alone. Two text areas with different
character limits are two data types. Read the candidate's configuration with `get-data-type`
before deciding; the name is not proof of what it does.

## Constraints go in the data type

A rule that should hold wherever the field appears belongs in the data type's configuration
([naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types)). Where
each kind of constraint lives:

| Constraint | Where it lives |
|---|---|
| Maximum characters | Text Box or Text Area `maxChars` |
| Number of items | Media Picker, Multi URL Picker, content picker, Block List minimum and maximum |
| Which media types can be picked | Media Picker filter |
| Allowed values | Dropdown, Radio Button List or Checkbox List items |
| Default value | Toggle default, Slider initial value |
| Number range | Integer or Decimal minimum and maximum |

Property-level validation (mandatory, a regular expression) is still set on the property, because
it can differ per use. "Mandatory" is a property setting; "at most 160 characters" is a data type
setting.

## Changing an existing data type

Changing a data type changes every property that uses it, on every type, with the content already
stored in them.

1. Read its references with `get-references-data-type` and list them in the requirements doc's
   `Used by`.
2. If any user of the data type should **not** change, do not edit it. Create a variant and move
   only the properties that need the new behaviour.
3. Tightening a constraint (a lower character limit, fewer allowed items) does not change stored
   values, but existing content that breaks the new rule fails validation the next time an editor
   saves it. Say so in the changeset summary.
4. Never edit a built-in data type. Create a qualified variant instead.

## Moving a property to a different data type

Moving a property from one data type to another with the **same value format** (one text area to
another, one single-image picker to another) keeps the stored values. Moving it to a different
format (text to a picker, a single picker to a multiple one, a dropdown to a checkbox list) leaves
the stored values in a shape the new editor cannot read.

When the format changes on a property that has content, stop. Say which values would be affected,
and that converting them is a content migration, which is code work outside this schema change.
Offer a new property alongside the old one instead.

**Related:** [editor-selection.md](editor-selection.md), [naming-and-folders.md](naming-and-folders.md).
