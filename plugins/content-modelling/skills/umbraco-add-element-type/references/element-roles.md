# Element roles

An element type is the schema of content that lives inside something else. It plays one of four
roles, and the role decides everything that follows: the folder, the alias, the container and
what it may compose. Settle the role first.

## Lookup

| | Content element | Repeater item | Settings model | Settings composition |
|---|---|---|---|---|
| **What it is** | The block editors pick and fill in | One entry in a block's list | What a block's settings are, assembled from settings compositions | One style concern, reused by settings models |
| **Example alias** | `accordion`, `richText`, `testimonial` | `accordionItem`, `pricingRow` | `blockSettings`, `accordionSettings` | `spacingSettingsComposition` |
| **Alias rule** | Plain noun; `...Block` only on a clash | `...Item` or `...Row` | `<block>Settings`, or a shared name | `...SettingsComposition` |
| **Folder** | `Elements/` | `Elements/`, beside its parent | `Elements/Settings/` | `Elements/Compositions/` |
| **Container** | No tab; one group `Content`, sort 0 | No tab; one group `Content`, sort 0 | None of its own; it shows its compositions' tabs | A tab, `Style` (50) or `Settings` (100); no group |
| **Own properties** | Content fields, short generic aliases | Content fields, short generic aliases | None | The fields of its one concern |
| **Compositions** | None | None | Settings compositions only, at least one | None |
| **Icon** | A noun that fits, no colour | A noun that fits, no colour | `icon-settings`, no colour | `icon-settings`, no colour |
| **Used by** | A Block Grid or Block List registers it | Its parent's Block List | Registered beside a content element | Settings models |
| **Linter role** | element | element | settings model | settings composition |

Names and suffixes come from
[naming.md](../../umbraco-content-model-conventions/references/naming.md#suffixes), folders from
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#document-types),
containers from
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md#which-container-to-use),
the settings rules from
[compositions.md](../../umbraco-content-model-conventions/references/compositions.md#blocks-settings-compositions-and-settings-models).
The requirements doc format gives the element template three shapes by folder
([requirements-format.md](../../umbraco-content-requirements-documentation/references/requirements-format.md#properties));
the linter enforces them.

## Settling the role

1. **A settings composition** when the request is one style option (spacing, a background
   colour, an anchor) that blocks will share. It is one concern; two options are two
   compositions.
2. **A settings model** when the request is "what settings do these blocks get": a named set of
   settings compositions and nothing else. A project normally has two shared ones, for blocks
   with and without a title.
3. **A repeater item** when the element is one entry in a list a parent block holds: a panel, a
   slide, a row. Its parent and the Block List between them are the block's business.
4. **A content element** otherwise: the thing editors pick from the catalogue and fill in.

If the request is for the content element *and* its settings, or for a parent *and* its items,
it is a block: hand off to [`umbraco-add-block`](../../umbraco-add-block/SKILL.md), which calls
this skill for each part.

## Role-specific points

### Content element

Holds content only. If a colour, spacing or alignment field is asked for on it, say that style
belongs on the block's settings element and leave it off; the block skill pairs the two. An
element with no style options at all is complete on its own. Whether the element varies by
culture matches the elements beside it on a multi-language site.

### Repeater item

Has no settings model unless each item is styled on its own, which is rare. Its fields are short
and generic (`title`, `content`), never prefixed with the parent's name. It needs no Block List
to exist; the list is created with the parent, by
[nested-items.md](../../umbraco-add-block/references/nested-items.md).

### Settings model

Optional: a block has one only when it has style options. No properties of its own; the linter
rejects any. Built from at least one settings composition, passed in `compositions` on the
create. A shared model is named for what it serves (`blockSettings`, `titledBlockSettings`); a
bespoke one for its block (`accordionSettings`). Which settings model a block uses, whether a new
one is needed at all, and how it is described are decided in
[settings-models.md](../../umbraco-add-block/references/settings-models.md#which-settings-model-a-block-uses).

### Settings composition

Follows the composition rules: one concern, flat, no template, never composed into a content
element or a page type. Its tab is `Style` (50) for how the block looks or `Settings` (100) for
how it behaves. The usual concerns, the purpose data type a fixed list of choices needs (it joins
the changeset ahead of the composition) and the description are in
[settings-models.md](../../umbraco-add-block/references/settings-models.md#settings-compositions).

A new settings composition does nothing until a settings model composes it. When the user asks
for one on its own, do not update any settings model here: name composing it into the settings
models that need it as the follow-up, for [umbraco-add-block](../../umbraco-add-block/SKILL.md)
or [umbraco-change-document-type](../../umbraco-change-document-type/SKILL.md). A request that
starts from the blocks ("give our blocks a spacing option") is a block change: hand it to
`umbraco-add-block`, which makes the composition and updates the models in one changeset.

**Related:** [workflow.md](workflow.md).
