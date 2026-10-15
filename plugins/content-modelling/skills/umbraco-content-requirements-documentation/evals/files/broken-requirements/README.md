# Broken requirements doc fixture

Deliberately wrong. Every page here breaks one or more rules so that `lint-requirements.mjs` can be proven
able to fail. `expected-rules.txt` lists the rule ids the linter must report, one per line and
sorted; CI compares it with the output of `lint-requirements.mjs --rules`. `expected-warnings.txt` does the
same for `--warnings`.

Do not fix these pages, and do not use them as examples of the format. The correct example is
the sibling `golden-requirements` folder.

| Page | What is wrong |
|---|---|
| `Document-Types/Article.md` | Status is not a valid value; page alias has no `Page` suffix; allowed at root; no default template; a `Collection` row on a page that is not a listing; Folder written as plain text instead of a breadcrumb; `Content` tab sorted 1; property alias with an acronym in capitals; the reserved alias `name`; Mandatory not Yes or No; a data type missing from Dependencies; a dependency type that is not in the vocabulary; an invalid flag; a dead link; a leftover placeholder; a sort that is not a multiple of 100 (warning); a description ending in `?`, which the MCP refuses (warning) |
| `Document-Types/ArticleListingPage.md` | Listing with no Collection row; approved with a `Missing` dependency; a page with no SEO composition (warning) |
| `Document-Types/Compositions/SeoComposition.md` | No Icon row; no Used by section; composes another composition, has a template and allowed children |
| `Document-Types/Elements/Accordion.md` | Element with a tab and a group that is not `Content` |
| `Document-Types/Elements/AccordionItem.md` | Properties table without the twelve-column header |
| `Document-Types/Elements/Settings/AccordionSettings.md` | Settings model with its own property and no compositions |
| `Data-Types/Toggle/ToggleDefaultOn.md` | No title on the first line; file not named after the data type; a name that follows no pattern (warning) |
| `Templates/ArticlePage.md` | File not named after the alias |
| `Notes/Stray.md` | A requirements page outside the known folders |
| `_changesets/2026-09-30-broken.md` | Marked applied with an unticked step and a proposed requirements doc; a Requirements docs row with no link; an Action that is not Create or Update; a lowercase `<alias>` placeholder left in the checklist |
