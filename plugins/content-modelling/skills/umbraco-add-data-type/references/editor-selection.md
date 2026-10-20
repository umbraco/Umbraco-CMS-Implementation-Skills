# Choosing the property editor

Pick the editor from what the editor needs to enter and how the value is used, not from what looks
richest. A plain field rendered as plain text is safer than rich text an editor can break.

Aliases below were read from Umbraco 17.5.3 (`Umbraco.Cms.Core` for the property editor,
`Umbraco.Cms.StaticAssets` for the editor UI). A site may have more editors from packages; the
site's own list wins.

## By field

| The editor enters | Property editor | Editor UI | Built-in data type to start from |
|---|---|---|---|
| A short single line (a title, a name) | `Umbraco.TextBox` | `Umb.PropertyEditorUi.TextBox` | Textstring |
| Several lines of plain text (a summary) | `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` | Textarea |
| Formatted text with links and headings | `Umbraco.RichText` | `Umb.PropertyEditorUi.Tiptap` | Richtext editor |
| Yes or no | `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` | True/false |
| One value from a fixed list | `Umbraco.DropDown.Flexible` (single) or `Umbraco.RadioButtonList` | `Umb.PropertyEditorUi.Dropdown`, `Umb.PropertyEditorUi.RadioButtonList` | — (always a new data type holding the values) |
| Several values from a fixed list | `Umbraco.CheckBoxList` or `Umbraco.DropDown.Flexible` (multiple) | `Umb.PropertyEditorUi.CheckBoxList`, `Umb.PropertyEditorUi.Dropdown` | — |
| Free-form keywords | `Umbraco.Tags` | `Umb.PropertyEditorUi.Tags` | Tags |
| A whole number | `Umbraco.Integer` | `Umb.PropertyEditorUi.Integer` | Numeric |
| A decimal number | `Umbraco.Decimal` | `Umb.PropertyEditorUi.Decimal` | — |
| A value in a range | `Umbraco.Slider` | `Umb.PropertyEditorUi.Slider` | — |
| A date | `Umbraco.DateOnly` | `Umb.PropertyEditorUi.DateOnlyPicker` | — |
| A date and time, in the site's time | `Umbraco.DateTimeUnspecified` | `Umb.PropertyEditorUi.DateTimePicker` | — |
| A date and time in a stated time zone (events across regions) | `Umbraco.DateTimeWithTimeZone` | `Umb.PropertyEditorUi.DateTimeWithTimeZonePicker` | — |
| A time of day | `Umbraco.TimeOnly` | `Umb.PropertyEditorUi.TimeOnlyPicker` | — |
| An email address | `Umbraco.EmailAddress` | `Umb.PropertyEditorUi.EmailAddress` | — |
| A colour from the site's palette | `Umbraco.ColorPicker` | `Umb.PropertyEditorUi.ColorPicker` | — (a new data type holding the palette) |
| One or more images or files | `Umbraco.MediaPicker3` | `Umb.PropertyEditorUi.MediaPicker` | Image Media Picker, Media Picker |
| A link, internal or external | `Umbraco.MultiUrlPicker` | `Umb.PropertyEditorUi.MultiUrlPicker` | Multi URL Picker (set the maximum to 1 for a single link) |
| One page on the site | `Umbraco.ContentPicker` | `Umb.PropertyEditorUi.DocumentPicker` | Content Picker |
| Several pages, or items from a data folder | `Umbraco.MultiNodeTreePicker` | `Umb.PropertyEditorUi.ContentPicker` | — (a new data type with its start node and allowed types) |
| A repeated set of fields, or a layout of blocks | `Umbraco.BlockList`, `Umbraco.BlockGrid` | — | Not here: [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) |
| The children of a listing, as a table | `Umbraco.ListView` | `Umb.PropertyEditorUi.Collection` | Not here: [umbraco-add-listing-page](../../umbraco-add-listing-page/SKILL.md) |

`Umbraco.DropDown.Flexible` is the one dropdown editor: single or multiple selection is its
`multiple` setting, so a single-value dropdown and a multi-value one are two data types with the
same editor.

Umbraco 17 also keeps `Umbraco.DateTime` (the older date picker). Prefer the specific date editors
above for new fields, and follow the project if it already uses `Umbraco.DateTime` throughout.

Two names are easy to swap: the **single** content picker is `Umbraco.ContentPicker` with the
editor UI `Umb.PropertyEditorUi.DocumentPicker`, while the **multiple** one is
`Umbraco.MultiNodeTreePicker` with the editor UI `Umb.PropertyEditorUi.ContentPicker`.

## Choices that come up often

- **Link fields use the Multi URL Picker**, with its maximum set to 1 for a single link. It stores
  the link text, target and URL together, and follows the page if it moves. A Textstring holding a
  URL does neither.
- **Pick content, do not type it.** An author, a category or a related page is a picker to the
  item, not a text field holding its name.
- **Dropdown or radio buttons.** Radio buttons for two to five short options that should all be
  visible; a dropdown for longer lists.
- **Rich text only where formatting is needed.** A summary or a caption is plain text.

## The Value Type column

The requirements doc's Value Type is the type Models Builder gives the property. Take it from an
existing property on the site that uses the same data type, or from the generated model. For the
common editors:

| Editor | Value Type |
|---|---|
| Text Box, Text Area, Email Address, Dropdown (single), Radio Button List | `System.String` |
| Rich text | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` |
| True/false | `System.Boolean` |
| Integer | `System.Int32` |
| Decimal | `System.Decimal` |
| Tags, Checkbox List, Dropdown (multiple) | `System.Collections.Generic.IEnumerable<System.String>` |
| Media Picker, single | `Umbraco.Cms.Core.Models.MediaWithCrops` |
| Multi URL Picker, maximum 1 | `Umbraco.Cms.Core.Models.Link` |
| Content Picker | `Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent` |

For anything else, write the type you read from the site; if you cannot read it, say so in the
changeset summary rather than guessing.

**Related:** [reuse-or-create.md](reuse-or-create.md), [naming-and-folders.md](naming-and-folders.md).
