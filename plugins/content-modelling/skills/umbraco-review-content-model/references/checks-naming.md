# Checks: names and aliases

Rules: [naming.md](../../umbraco-content-model-conventions/references/naming.md),
[property-aliases.md](../../umbraco-content-model-conventions/references/property-aliases.md).
On a requirements folder the linter already reports alias casing and suffixes (`alias-casing`,
`alias-suffix`); carry its findings into the report instead of re-checking by eye. A `...Page`
alias with no template is one finding, reported once under
[Page type with no default template](checks-structure.md#templates), not here as well.

## Types

| Check | How to detect | Severity | Fix with |
|---|---|---|---|
| Page without the `Page` suffix | A type with a template (or at the tree root) whose alias does not end `Page`, other than `siteSettings` | Medium | a rename, specified with the skill that owns the type |
| Listing not named after its item | A type with a collection and allowed children whose alias is not `<noun>ListingPage` listing `<noun>Page` | Medium | [umbraco-add-listing-page](../../umbraco-add-listing-page/SKILL.md) |
| Composition without the `Composition` suffix | A type used as a composition (`get-document-type-composition-references` returns users) or in `Compositions/` | Medium | [umbraco-add-composition](../../umbraco-add-composition/SKILL.md) |
| Element with a page or composition suffix, or `...Block` without a clash | An element type alias ending `Page`, `Composition`, `Folder`, or `Block` where the plain noun is free | Low | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |
| Settings model or settings composition misnamed | In `Elements/Settings/` not ending `Settings`; in `Elements/Compositions/` not ending `SettingsComposition` | Low | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |
| Acronym left in capitals | Two capitals in a row in an alias (`sEOTitle`, `xMLSitemapPage`), usually the backoffice's generated alias | Medium | the owning skill |
| Name and alias disagree | The name is not the Title Case form of the alias (`News` with alias `articlePage`) | Low | the owning skill |
| The rejected `Has...` prefix | A composition named `HasSeo` | Low (note the aside in naming.md) | [umbraco-add-composition](../../umbraco-add-composition/SKILL.md) |

## Properties

| Check | How to detect | Severity | Fix with |
|---|---|---|---|
| Boolean not a predicate | A True/false property whose alias is a noun (`banner`, `indexing`) | Low | the owning skill |
| Page field without an area prefix, clashing across compositions | The same alias (`title`) defined by two compositions or a composition and a type | High: Umbraco rejects the second composition | [umbraco-add-composition](../../umbraco-add-composition/SKILL.md) |
| Page field without an area prefix | `title` on a page or page composition | Low | the owning skill |
| Block field with the block's name in it | `accordionTitle` on `accordion` | Low | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |
| Override without the suffix | An optional replacement of a computed value not ending `Override` | Low | the owning skill |
| Numbered fields | `item1Title`, `item2Title`: a repeater modelled as fields | Medium | [umbraco-add-block](../../umbraco-add-block/SKILL.md) (repeater) |

## Data type names

| Check | How to detect | Severity | Fix with |
|---|---|---|---|
| Name does not follow a pattern | Not `<Editor> (<qualifier>)`, `<Subject> <Editor kind>`, a Block List plural, `<Placement> Block Grid` or `<Type> Collection View`; or names such as `Textarea 2`, `Custom Dropdown` | Low | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) |

Renaming an **alias** changes templates, models and queries; always say so in the suggested fix.
Renaming a **name** is safe.

**Related:** [checks-structure.md](checks-structure.md).
