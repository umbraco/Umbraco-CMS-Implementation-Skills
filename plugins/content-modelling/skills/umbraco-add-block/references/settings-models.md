# Settings models and settings compositions

A block has up to two halves in Umbraco: the **content** an editor writes and the **settings**
that control how it looks. Each half is its own element type. The settings half is optional: a
block with no style options has none. This file decides whether a block has one, and which.
The rules behind it are in
[compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models).

## The three layers

| Layer | What it is | Rule |
|---|---|---|
| Settings composition | One style concern: a background colour, an anchor, spacing | Alias and folder: [naming.md](../../umbraco-content-model-conventions/references/naming.md#suffixes), [tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#document-types); tab: [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use) |
| Settings model | What a block's settings are, assembled from settings compositions; no own properties | [compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models) |
| Content element | The block itself, in one `Content` group | [tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use) |

The settings model is attached to the content element where the block is registered in a Block
Grid or Block List, not on the element itself. That registration belongs to
[umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md); this skill makes
the settings model exist, says which block it is for, and registers the pair only in an existing
area the user names ([workflow.md](workflow.md#registering-in-a-named-block-area)).

Each settings model and settings composition is one element type, decided by
[umbraco-add-element-type](../../umbraco-add-element-type/references/element-roles.md) in the role
settings model or settings composition. A settings composition wanted on its own, with no block
behind the request, is that skill's job; from here it is one part the block needs.

## Which settings model a block uses

| The block needs | Use |
|---|---|
| No style options at all | No settings model; the block is registered with settings `—`. Do not add one "in case" |
| The usual options, and the block has no title | The site's shared settings model for untitled blocks |
| The usual options, and the block has a title | The site's shared settings model for titled blocks |
| An option the shared models do not offer | A bespoke `<block>Settings`, composed of the shared settings compositions plus a new one for the extra option |

A project normally has exactly two shared settings models, one for blocks without a title and one
for blocks with a title (the titled one adds title options such as heading level or alignment).
Their names are the project's; `blockSettings` and `titledBlockSettings` are reasonable when there
are none yet. If the site has none, create the one this block needs as a shared model, not a
bespoke one, and say that the next block can reuse it.

A bespoke settings model is still built only from settings compositions. If a block needs one
extra option, make that option a new settings composition and compose it in
([compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models)).

## Settings compositions

Each settings composition is one concern, used by any settings model that needs it.

| Concern | Alias | Tab (sort) | Typical field: alias (data type) |
|---|---|---|---|
| Background colour | `backgroundColourSettingsComposition` | Style (50) | `backgroundColour` (a colour picker data type with the site's palette) |
| Spacing | `spacingSettingsComposition` | Style (50) | `spacing` (a dropdown of the site's spacing sizes) |
| Anchor | `anchorSettingsComposition` | Settings (100) | `anchorId` (Textstring) |
| Visibility | `visibilitySettingsComposition` | Settings (100) | `isHidden` (True/false) |

Property sorts follow
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#property-sorts).
A field whose values come from a fixed list (colours, sizes) needs its own purpose data type,
named and foldered by
[naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types), for example
`Background Color Picker` in `Color Picker/`: the folder is named after the editor, and the data
type name takes the editor's own spelling, so both read "Color" even though the property alias
is `backgroundColour`. That data type joins the changeset ahead of the settings composition.
Choosing and configuring it in depth belongs to
[umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md).

A settings composition follows the composition rules (one concern, flat, no template;
[compositions.md](../../umbraco-content-model-conventions/references/compositions.md#rules)) and
is applied only to settings models, never to a content element or a page type.

## Descriptions

A settings model is described by the blocks it serves ("Settings for blocks without a title:
background colour, spacing and anchor."). A settings composition is described as "Adds ..."
([descriptions.md](../../umbraco-content-model-conventions/references/descriptions.md#type-descriptions)).
Settings fields say what each choice does and what the default is ("Leave empty for the page
background.").

**Related:** [workflow.md](workflow.md), [nested-items.md](nested-items.md).
