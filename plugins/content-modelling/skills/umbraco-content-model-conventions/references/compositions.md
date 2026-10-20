# Compositions

**Applies to:** every composition, and the decision of what goes in one.
**Look for:** a composition that composes another composition, a composition with a template,
allowed children or allow-at-root, one composition carrying unrelated fields, one composition
applied to every type regardless of need.
**Why:** a composition is a reusable set of fields. It stays reusable only while it does one job
and carries no behaviour of its own.

## Rules

| Rule | Reason |
|---|---|
| One concern each | `seoComposition` and `openGraphComposition` are two compositions even if they share a tab. A page can then take one without the other |
| Flat: a composition never composes another | Umbraco enforces most of this itself: a type that has compositions cannot be used as one, and a type in use as a composition cannot be given one. The rule still matters for the requirements doc, so a nested composition is never written down and then refused at apply time |
| No template, no allowed children, not allowed at root | Those are behaviour, and behaviour belongs to the page type |
| Applied selectively, except SEO | Every page takes the SEO composition. Add the others to the types that need them: a programmatic page may need SEO fields and no sharing image |
| Lives in `Compositions/` and ends in `Composition` | See [naming.md](naming.md) and [tree-organisation.md](tree-organisation.md) |
| Uses the global tab sorts | See [tabs-groups-sorts.md](tabs-groups-sorts.md) |
| Described as "Adds ..." | See [descriptions.md](descriptions.md) |

A page type can legitimately have **no properties of its own** and be nothing but a list of
compositions plus its behaviour (template, children). That is the usual shape of a content page.

## When to make one

Make a composition when the same fields are needed by two or more types, or will be. A set of
fields used by exactly one type stays on that type until a second type needs it.

## Blocks: settings compositions and settings models

Blocks follow the same idea one level down.

- A **settings model** is a separate element type (`<block>Settings`, in `Elements/Settings/`).
  A block's content element never carries its own styling fields.
- A settings model has **no properties of its own**. It is built only from settings compositions
  (`...SettingsComposition`, in `Elements/Compositions/`), each one concern: a background colour,
  an anchor, a top edge style.
- Two shared generic settings models cover most blocks: one for blocks with no title and one for
  blocks with a title. Create a bespoke `<block>Settings` only when a block needs style options
  the generic ones do not offer.
- A repeater is a parent element with a Block List of `...Item` children, not a composition.
- **An element is not a block.** A block is a content element registered in a Block Grid or
  Block List. Settings models, settings compositions and repeater items are elements too, but they
  are parts of a block, never blocks themselves.
- **Settings are optional.** A block with no style options is registered with no settings element.

**Related:** [Tabs, groups and sort orders](tabs-groups-sorts.md), [Names, aliases and
suffixes](naming.md).
