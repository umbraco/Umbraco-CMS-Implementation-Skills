# Naming and folders

The name patterns are defined in
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types) and the
folder rule in
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types).
This file covers applying them.

## Choosing the name

Apply the patterns in
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types). Worked
examples, as examples only:

- Generic variants, `<Editor> (<qualifier>)`: `Toggle (default on)`, `Text Area (3 rows)`,
  `Multi URL Picker (single)`.
- Purpose-specific, `<Subject> <Editor kind>`: `Meta Description Text Area`,
  `Alignment Radio Button List`, `Background Color Picker`, `Event Date Picker`.

What the two placeholders should say:

- The **editor** is named as Umbraco spells it, so a variant of the True/false data type is a
  `Toggle (...)`, and a colour picker is a `... Color Picker` even on a site that writes "colour"
  everywhere else.
- The **subject** names the purpose, not the property or the page: `Meta Description Text Area`,
  not `SEO Composition Meta Description`. Another type with the same need can then reuse it.
- The **qualifier** names the difference in plain words an editor would recognise: `(default on)`,
  not `(value 1)`.
- Never a name that only says "custom" or "new" (`Custom Textarea`, `Textarea 2`).

## Choosing the folder

One folder per editor kind, named after the editor, created with the first custom data type of
that kind; the rule is in
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types).
The folder names to use, by property editor:

| Property editor | Folder |
|---|---|
| `Umbraco.TextBox` | `Text Box` |
| `Umbraco.TextArea` | `Text Area` |
| `Umbraco.RichText` | `Rich Text` |
| `Umbraco.TrueFalse` | `Toggle` |
| `Umbraco.DropDown.Flexible` | `Dropdown` |
| `Umbraco.RadioButtonList` | `Radio Button List` |
| `Umbraco.CheckBoxList` | `Checkbox List` |
| `Umbraco.Integer`, `Umbraco.Decimal` | `Numeric` |
| `Umbraco.DateOnly`, `Umbraco.DateTimeUnspecified`, `Umbraco.DateTimeWithTimeZone`, `Umbraco.TimeOnly` | `Date Picker` |
| `Umbraco.ColorPicker` | `Color Picker` |
| `Umbraco.MediaPicker3` | `Media Picker` |
| `Umbraco.MultiUrlPicker` | `Multi URL Picker` |
| `Umbraco.ContentPicker`, `Umbraco.MultiNodeTreePicker` | `Content Picker` |
| `Umbraco.Tags` | `Tags` |
| `Umbraco.BlockList`, `Umbraco.BlockGrid`, `Umbraco.ListView` | `Block List`, `Block Grid`, `Collection View` (made by the block editor and listing skills) |

If the project already names these folders differently, keep the project's names.

A variant of a built-in data type is a project artefact and goes in its editor's folder; the
built-in itself stays at the root of Data Types.

## The requirements page

The page is `Data-Types/<Editor-Kind>/<Name-Slug>.md`, named as
[requirements-format.md](../../umbraco-content-requirements-documentation/references/requirements-format.md#folder-layout)
describes: `Toggle (default on)` becomes `Data-Types/Toggle/Toggle-default-on.md`. The folder is
listed in Dependencies as a `data-type-container` and links to its index page, written from
[folder-index.md](../../umbraco-content-requirements-documentation/assets/folder-index.md) if it
does not exist yet.

**Related:** [reuse-or-create.md](reuse-or-create.md), [workflow.md](workflow.md).
