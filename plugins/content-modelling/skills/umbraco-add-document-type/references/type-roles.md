# Type roles

A document type that is not an element plays one of five roles. The role decides its folder, its
alias suffix and which caller adds behaviour to it. Settle the role before deciding anything else.

## Lookup

| Role | Folder | Suffix | Compositions | Caller that adds behaviour |
|---|---|---|---|---|
| Page | Document Types root | `...Page` | SEO always; page details and sharing by kind | [umbraco-add-page](../../umbraco-add-page/SKILL.md) (template, parent, children), [umbraco-add-listing-page](../../umbraco-add-listing-page/SKILL.md) (collection, the pairing) |
| Composition | `Compositions/` | `...Composition` | None (flat) | [umbraco-add-composition](../../umbraco-add-composition/SKILL.md) owns this role entirely; hand off |
| Data folder or item | `Data/` | `...Folder`; `...Item` or a plain noun | None | [umbraco-add-data-folder](../../umbraco-add-data-folder/SKILL.md) (allowed at root, children, the picker) |
| Site settings | Document Types root | `siteSettings` | None | [umbraco-add-site-settings](../../umbraco-add-site-settings/SKILL.md) (allowed at root, tabs 0 to 9) |
| Standalone plain type | `Data/` | Never `Page`; a plain noun or `...Item` | None | None. It needs a parent that allows it, which is a data folder or an existing type updated by [umbraco-change-document-type](../../umbraco-change-document-type/SKILL.md) |

Names come from [naming.md](../../umbraco-content-model-conventions/references/naming.md#suffixes),
folders from
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#document-types),
icons and colours from
[icons-and-colours.md](../../umbraco-content-model-conventions/references/icons-and-colours.md#colour-by-role),
which compositions a role takes from
[when-to-apply.md](../../umbraco-add-composition/references/when-to-apply.md#which-types-take-it).

## Why a plain type never sits at the root

The requirements linter assigns a type's role by its folder: anything at the Document Types root
except `siteSettings` is a page, and must have a `...Page` alias and a default template. A
template-less type at the root therefore fails the lint, and on the site it reads as a page that
cannot render. So a type that is not a page goes in `Data/`, where the linter requires the
opposite: no template, and an alias that does not end in `Page` or `Composition`.

## Settling the role

1. **Will visitors land on it?** If yes, or if the name says page (Careers page, Contact page),
   it is a page, whatever else was asked for. Hand off to
   [umbraco-add-page](../../umbraco-add-page/SKILL.md); from there this skill supplies the schema.
2. **Is it a reusable set of fields for other types?** A composition; hand off to
   [umbraco-add-composition](../../umbraco-add-composition/SKILL.md).
3. **Is it shared data pages pick from** (authors, offices, categories), with the folder that
   holds it? A data folder or item; the folder, the item, the picker and the content-tree
   behaviour are [umbraco-add-data-folder](../../umbraco-add-data-folder/SKILL.md)'s change, and
   this skill writes the two types' schema when called.
4. **Is it the site-wide settings node?** Site settings; hand off to
   [umbraco-add-site-settings](../../umbraco-add-site-settings/SKILL.md).
5. **Otherwise** a standalone plain type in `Data/`: say where its nodes will be created (which
   existing type or folder allows it), and that it has no template, so its nodes have a URL that
   nothing renders.

## What a non-page never gets

- A template or allowed templates: `—` on the requirements page.
- The SEO, Open Graph or page-details compositions. Search engines and social networks never see
  a node nothing renders. If the user asks for SEO on a data item so that it "shows up in
  Google", the thing they want is a page for it; say so and offer
  [umbraco-add-page](../../umbraco-add-page/SKILL.md).
- The page colour. Data and settings take `color-green`.

**Related:** [workflow.md](workflow.md).
