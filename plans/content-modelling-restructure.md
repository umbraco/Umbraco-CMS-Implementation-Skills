# Content Modelling Restructure

Source: "Content Modelling Restructure Plan", Claude Doc dated 15 Oct 2026 (authored by Matt).
First transcribed into the repo on 15 Oct 2026 and updated the same day from the revised doc,
which added the concepts table, the five principles, the six-skill table with its callers and the
five-step page flow. This plan **supersedes**
`plans/split-page-type-into-document-type-and-template-skills.md`, which proposed an
`umbraco-add-template` skill and a different split; that file is kept for history only.

Plugin: `umbraco-cms-content-modelling-skills` (`plugins/content-modelling/`).

## Summary

One skill per concept. `umbraco-add-page` orchestrates a page (document type + template +
mandatory SEO + parent) in one changeset, built from peer skills `umbraco-add-document-type`,
`umbraco-add-element-type`, `umbraco-add-block`, `umbraco-add-composition` and
`umbraco-configure-block-editor`. Today's `umbraco-add-page-type` becomes the page orchestrator,
today's `umbraco-add-element-type` becomes `umbraco-add-block`, and two schema skills are new.

## Step 0: branch

The repo's default branch is `main` (there is no `develop`). Start from a freshly pulled `main`:

```bash
git checkout main
git pull
git checkout -b feature/content-modelling-restructure
```

Never branch off `feature/content-modelling-skills` or any other feature branch.

## The model

A page is something visitors land on: a document type with a template, SEO and a place in the
site. Underneath it, document types and element types are the schema, and a block is a content
element paired with a settings element.

A page is the only thing with a template. A document type is schema only, a block is a content
element plus a settings element, and every peer skill can be called on its own. `add-page`
orchestrates; the peers are not its children.

## The concepts

This table is the source of truth for wording across the plugin. It becomes the `## Terms` table
in `umbraco-content-model-conventions/SKILL.md` (see "A template makes a page" below).

| Concept | What it is | Template | Examples |
|---|---|---|---|
| Page | A document type visitors are meant to land on. It has a template, takes the SEO composition and sits under a parent that allows it | Yes | Contact page, article page, search page |
| Document type | The schema of something: its tabs, fields and compositions. Its nodes can still have a URL, but nothing renders them as a page | No | Author, category, site settings |
| Element type | The schema of content that lives inside something else, never on its own in the content tree | No | Accordion, accordion item, a settings model |
| Block | A set of elements: the content element editors fill in, and the settings element that controls how it looks | No | An accordion and its settings |
| Composition | A reusable set of fields that does one job, added to the types that need it | No | SEO, Open Graph |
| Block area | The place on a page where editors combine blocks | No | The main content Block Grid |

## Principles

The five principles from the doc, and where each one lands in the skills.

| # | Principle | Where it lands |
|---|---|---|
| 1 | **A template is what makes a page.** Pages and plain document types can both have URLs. Whether something is a page is a question of intent: will visitors land on it? When that is unclear, the skill asks | `add-page` and `add-document-type` both ask about intent and hand off to each other. Conventions and linter wording: "routable" becomes "page (has a template)". No headless or `Content/` folder convention |
| 2 | **Every page has SEO.** If the site has no SEO composition yet, adding the first page adds it too. Sharing and page-details fields stay optional | `add-page`'s `### SEO is mandatory` section; `page-seo` linter warning; review check. Sharing and page-details compositions stay selective |
| 3 | **Content and style stay apart.** A block's content element holds what editors write; its settings element holds how it looks, built from small single-purpose settings compositions | `add-block` owns the pairing (`settings-models.md`); `add-element-type` owns the single elements, including a settings composition on its own |
| 4 | **Skills are peers.** Each building block has its own skill that works on its own or inside another flow. The page skill calls into them; it does not own them | Every peer gains a `## Used by another skill` section: inspect, decide and write only; the caller owns the changeset and the one approval |
| 5 | **One page, one approval.** Everything a new page needs is written up together, reviewed once, then applied leaves first so each part exists before something depends on it | `add-page/references/combined-changeset.md` and the apply order below |

## Decisions

Operational decisions that follow from the principles. All settled.

| # | Decision | Detail |
|---|---|---|
| 1 | Rename and split | `umbraco-add-page-type` becomes `umbraco-add-page`. `umbraco-add-element-type` moves to `umbraco-add-block`. New: `umbraco-add-document-type` and a rewritten `umbraco-add-element-type` |
| 2 | `add-block` can place the block in an existing block area | Run on its own, `add-block` calls `configure-block-editor` to register the block in an existing block area the user names, in the same changeset |
| 3 | Listing stays separate | A listing and the items it lists are a pair. `umbraco-add-listing-page` calls `add-page` once for each of its two pages |
| 4 | SEO is enforced | A `page-seo` linter warning and a review check |
| 5 | No version bump yet | Plugin stays at 1.2.0 until the user decides |

### Decisions after the plan

Settled on 15 Oct 2026, after comparing this branch with PR #4, a second implementation of the
same plan. PR #4's better parts were merged into this branch, which is the one carried forward.

| # | Decision | Detail |
|---|---|---|
| 6 | A block's settings element is optional | A block with no style options is registered with settings `—`. This narrows principle 3: content and style stay apart, but a block with no style has no settings element |
| 7 | A lone settings composition does not update settings models | `add-element-type` writes only the composition and names composing it into settings models as a follow-up for `add-block` or `change-document-type`. "Give blocks an option" goes to `add-block`, which does both in one changeset |
| 8 | `page-seo` ignores Update-only pages | The linter does not warn on a page every changeset lists only as an Update: an existing page the change touches for another reason |

The linter already accepts mixed-kind changesets. It assigns a type's role by folder: anything at
the Document Types root except `siteSettings` is a page and needs a `...Page` alias and a template.
So `add-document-type` places plain types by role, never at the root.

## The skills

One skill per concept. Two folders move with `git mv` to keep history; two are new; two existing
skills gain callers. Every peer, plus `add-data-type`, gains a `## Used by another skill` section:
when called by another skill, inspect, decide and write only, then hand back pages and checklist
rows. The caller owns the changeset and the single approval.

| Skill | Concept | Called by | Scope | References | SKIP to |
|---|---|---|---|---|---|
| `umbraco-add-page` (moved from `add-page-type`) | Page: brings the others together | Users directly; the listing skill, once for each of its two pages | One page: document type + template + SEO + parent, plus any blocks or block area, in one changeset | `page-kinds.md`, `minimum-viable-page.md`, `workflow.md` (moved); new `combined-changeset.md` | `add-document-type` (no template), `add-listing-page`, `add-data-folder`, `change-document-type` |
| `umbraco-add-document-type` (new) | Document type | `add-page`, the listing, data folder and site settings skills, or directly | Schema of one document type that is not an element: name, alias, folder by role, icon, description, tabs, properties, compositions, vary by culture. Never a template | New `workflow.md` (schema rows taken from `add-page-type`), `type-roles.md` | `add-page` (wants a template, or visitors land on it), `add-element-type`, `add-block`, `add-composition` |
| `umbraco-add-element-type` (new scope) | Element type | `add-block`, or directly | One element in one of four roles: content element, repeater item, settings model, settings composition | New `workflow.md` (single-element subset of today's), `element-roles.md` | `add-block` (a whole block), `configure-block-editor` (registration) |
| `umbraco-add-block` (moved from `add-element-type`) | Block | `add-page`, or directly | Content element + settings element (shared or bespoke settings model from settings compositions), plus repeater item and Block List. Can also place the block in an existing block area via `configure-block-editor` | `settings-models.md`, `nested-items.md`, `workflow.md` (moved; decide rows point at `add-element-type`) | `add-element-type` (one element), `add-page` (the page using it) |
| `umbraco-add-composition` (existing) | Composition | `add-page` for SEO, or directly | Unchanged scope; gains the peer section and the SEO-on-a-new-site path | Existing | Settings compositions go to `add-block` or `add-element-type` |
| `umbraco-configure-block-editor` (existing) | Block area | `add-page`, `add-block`, or directly | Unchanged scope; gains the peer section. Approach A (Block Grid placement) is what `add-page` and `add-block` call | Existing | Blocks go to `add-block` |

`add-data-type` is not a concept in the doc but stays a peer: `add-page` calls it for a page field
nothing on the site fits, and `add-block` for a block field. `add-listing-page`,
`add-data-folder` and `add-site-settings` keep their own skills and call `add-document-type` for
their schema (see cross-references).

### add-page changes

- `workflow.md` keeps its six numbered sections so the `#5-apply` anchor survives. The doc's five
  steps map onto them and give each section its opening line:

  | Doc step | Workflow section |
  |---|---|
  | 1. Understand the page: kind (home, everyday content, or rendered by code like search), fields, blocks or block area, where it sits | `## 1. Inspect` and `## 2. Decide` |
  | 2. Reuse first: existing compositions, data types, elements and blocks; for anything missing, apply that concept's skill | `## 2. Decide` |
  | 3. Write it up once, calling out each existing type that will change | `## 3. Write the requirements doc` |
  | 4. Approve, then apply leaves first | `## 4. Stop for approval`, `## 5. Apply` |
  | 5. Check and report: read everything back; say what is left | `## 6. Verify and report` |

- The schema decide rows collapse into one row that delegates to `add-document-type` with role
  page. Kind, template, at-root, parent and children, and compositions stay.
- `### Compositions that do not exist yet` becomes `### SEO is mandatory`. If the site has no SEO
  composition, it joins the changeset under `add-composition`'s standard fields, with Toggle
  (default on) if missing. It is flagged **New in this changeset**, never **Missing**. Other pages
  without SEO are listed as optional Updates, not included by default.
- Best practices: "Routable means a template" becomes "A template is what makes it a page". Add
  "Every page takes SEO".
- `minimum-viable-page.md`: the "No compositions" row becomes "No SEO composition: add it to the
  same change".
- The common case stays small: four pages and five checklist items, the same as today.
  `combined-changeset.md` is opened only for blocks, block areas or new data types.

### add-document-type roles

| Role | Folder | Suffix | Caller that adds behaviour |
|---|---|---|---|
| Page | Document Types root | `...Page` | `add-page`, `add-listing-page` |
| Composition | `Compositions/` | `...Composition` | `add-composition` |
| Data folder or item | `Data/` | `...Folder`, `...Item` or a noun | `add-data-folder` |
| Site settings | Document Types root | `siteSettings` | `add-site-settings` |
| Standalone plain type | `Data/` | Never `Page` | None |

## One changeset per page

`add-page` routes each part a page needs to the peer whose rules apply, then merges them into one
changeset in leaf-first order. This lives in the new
`umbraco-add-page/references/combined-changeset.md`.

| Part the page needs | When | Rules from | Pages written |
|---|---|---|---|
| New data type for a page field | Nothing on the site fits | `add-data-type` | `Data-Types/<Kind>/<Slug>.md` |
| SEO composition | The site has none | `add-composition` | `Document-Types/Compositions/SeoComposition.md`, plus index and toggle data type if needed |
| Sharing or page-details composition | Missing | Not added; named as a follow-up | None |
| New block | The user names a block that does not exist | `add-block` | `Elements/...`, `Elements/Settings/...`, `Elements/Compositions/...`, `Data-Types/Block-List/...` |
| Single new element | One element with no settings or repeater | `add-element-type` | `Elements/<Alias>.md` |
| Block area | The page has one | `configure-block-editor`, approach A | `Data-Types/Block-Grid/<Placement>-Block-Grid.md` (Create, or Update keeping entries) |
| Master template | The site has none | `add-page` | `Templates/Master.md` |
| Page template | Always | `add-page` | `Templates/Master/<Alias>.md` |
| Page schema | Always | `add-document-type`, role page | `Document-Types/<Alias>.md` |
| Page behaviour (template link, at-root, own children) | Always | `add-page` | Same page |
| Parent update | The parent exists | `add-page` | `Document-Types/HomePage.md` (Update) |
| Home page | The site has none | Ask, then `add-page` with kind Root | `HomePage.md` and its template |

Merge rules: one changeset file and one summary; pages numbered in apply order; each folder
created once; allowed children set only after every type exists; one verify item at the end.

Apply order, leaves first (data types, compositions, elements and blocks, the block area, the
templates, the page, and finally its place under the parent), matching `apply-via-mcp.md#order`
so that file needs no change:

1. Data type folders and plain data types (page fields, SEO toggle, block field types)
2. Document type folders that are missing: `Compositions`, `Elements`, `Elements/Compositions`,
   `Elements/Settings`
3. `seoComposition` in `Compositions`, then fix-up (only if new)
4. Settings compositions: create, move, fix-up
5. Settings model: create, move, fix-up
6. Repeater item elements: create, move, fix-up
7. Block List data types
8. Content elements: create, move, fix-up
9. Block Grid data type: Create, or Update keeping existing entries
10. Template master (only if new)
11. Page template under master
12. Page document type (compositions include `seoComposition`; `mainContent` uses the grid), then
    fix-up
13. The page's own allowed children
14. Parent: add the new page, keeping existing entries
15. Verify every requirements doc and set each status line

At the approval gate, `add-page` calls out:

- every existing artefact that changes (parent, an existing block area, a settings model gaining a
  composition)
- new shared artefacts (the SEO composition, settings models) and which existing pages lack SEO
  and are left alone
- anything resting on the user's word rather than a read of the site

In the report after verify, `add-page` says what is left: template markup, a controller for a
page rendered by code, block views, and the optional sharing composition.

## A template makes a page

Every place that says "routable" to mean "has a template" is reworded to say "page". Linter rule
ids stay the same, so the expected-rules fixtures don't change.

| Area | File | Change |
|---|---|---|
| Conventions | `templates.md` | Why: "a template is what makes a document type a page; any node can have a URL, but nothing renders it without one". "Every page has exactly one default template". "Nothing else gets a template", noting those nodes may still have URLs |
| Conventions | `SKILL.md` | Look-for lines say "page type". New `## Terms` table: the six concepts above (Page, Document type, Element type, Block, Composition, Block area) with their Template column. Settings model is named under Element type's examples, not as its own row |
| Conventions | `naming.md`, `tree-organisation.md`, `allowed-children-and-root.md`, `icons-and-colours.md` | "Routable" becomes "page (has a template)" |
| Conventions | `compositions.md` | "Applied selectively" row: every page takes the SEO composition; others selectively |
| Requirements docs | `requirements-format.md:21, 152` | "pages (types with a template)" |
| Skills | `add-data-folder`, `add-listing-page`, `add-composition` `when-to-apply.md`, `add-site-settings` `workflow.md` | "Non-routable" becomes "not a page (no template)" |
| Linter | `lint-requirements.mjs` ~339, 341, 378, 397 | Messages only |
| Linter | New `page-seo` warning | Fires on a page-role type whose Compositions row has no SEO entry. Add it to `broken-requirements/expected-warnings.txt`, since `ArticleListingPage.md` has none |
| Review | `checks-structure.md`, `checks-naming.md`, `SKILL.md:79` | Page wording; fix skill becomes `add-page`. New Low check: a `...Page` alias with no template |
| Review | `checks-compositions.md` | New Medium check: a page without the SEO composition, fixed with `add-composition` |

`assets/element-type.md` and its fixture copies are left alone: "not routable as a page" is still
accurate for elements, and changing it would touch golden fixtures.

## Cross-references

The design pass found about 44 references in 23 files. Re-grep before editing; the line numbers
below are a guide.

| File | Change |
|---|---|
| `README.md` (skills table rows 17 and 19, mermaid graph nodes 41 and 43, sequence line 130, build-order nodes 169, 171 and 174) | Table gains rows for `add-page`, `add-document-type`, `add-element-type`, `add-block`; the diagrams rename `add-page-type` to `add-page` and the block step to `add-block` |
| `umbraco-add-listing-page/SKILL.md` | Links to `umbraco-add-page/SKILL.md#best-practices` and `references/workflow.md#5-apply`; SEO mandatory; calls `add-page` once per page |
| `umbraco-add-listing-page/references/workflow.md` | Links; the schema decide rows link to `add-document-type`'s workflow instead of repeating them; add an SEO composition row and "Create seoComposition (only if new)" to the checklist |
| `umbraco-add-data-folder/references/workflow.md` | The schema decide rows for the folder and item types link to `add-document-type` (role data folder or item); the skill keeps the picker and content-tree behaviour |
| `umbraco-add-site-settings/` (`SKILL.md:13`, `tab-layout.md:35`, `workflow.md:45`) | The schema decide rows link to `add-document-type` (role site settings); block styling points to `add-block` |
| `umbraco-add-composition/SKILL.md`, `when-to-apply.md:10` | Settings compositions go to `add-block` or `add-element-type`; adding a page goes to `add-page`; add the peer section with the "called by `add-page` for SEO" path |
| `umbraco-configure-block-editor/SKILL.md`, `approach-a-block-grid-placement.md:66` | Blocks go to `add-block`; add the peer section covering both callers (`add-page` for a page's block area, `add-block` for placing one block in an existing area) |
| `approach-b-block-list-repeater.md:16-17` | Link becomes `../../umbraco-add-block/references/nested-items.md` |
| `umbraco-change-document-type/SKILL.md:12-13` | Lists all four creating skills |
| `umbraco-add-data-type/SKILL.md` | Peer section only |
| `umbraco-review-content-model/references/checks-*.md` | Sorts and naming checks point to `add-element-type`; block checks to `add-block`; structure checks to `add-page` |

Anchors that must survive the moves: `#best-practices`, `#5-apply`, `#the-root-question`,
`#root`, `#content`, `#which-settings-model-a-block-uses`. New anchors `#used-by-another-skill`
and `#seo-is-mandatory` must resolve.

## Evals and fixtures

Each skill keeps its own fixture copies, because eval `files` paths resolve relative to the skill.

| Skill | Eval | Status | What it checks |
|---|---|---|---|
| `add-page` | 1, 2 | Kept | Contact page; search page with a manual walkthrough |
| `add-page` | 3 | Flipped | Empty site: `SeoComposition.md` is created in the same changeset, before `contentPage`, flagged **New in this changeset** (today it must not be) |
| `add-page` | 4 | New, fixture `no-seo-site-context.md` | "Add an About page": SEO composition created and applied to `aboutPage` only; existing pages offered, not updated; no Open Graph; one changeset |
| `add-page` | 5 | New, fixture `block-site-context.md` | Landing page with a new Testimonial block: element using `blockSettings`, Block Grid Update keeping entries, `landingPage.mainContent`, leaf-first order, one changeset |
| `add-block` | 1-3 | Moved | Eval 3, expectation 5 flips: places the block in the named block area in the same changeset |
| `add-element-type` | 1 | New | Testimonial element with no settings, in `Elements/`, Content group 0 |
| `add-element-type` | 2 | New | Video element becomes `videoBlock` because of the media type clash |
| `add-element-type` | 3 | New | Spacing settings composition in `Elements/Compositions/`, Style tab 50 |
| `add-document-type` | 1 | New | Office type with no template: `Data/`, no SEO |
| `add-document-type` | 2 | New | "Document type for a Careers page": treated as a page, never a template-less type at the root |
| `add-document-type` | 3 | New | SEO on a data item is declined |
| `add-composition` 3, `configure-block-editor` 2 | Grader text | Updated | "add-element-type" becomes "add-block (or add-element-type)" |
| review, conventions, `add-composition` | Grader text | Updated | "Routable" becomes "page" |

## Risks and verification

| Risk | Mitigation |
|---|---|
| Trigger overlap: `add-page` and `add-document-type` both hear "create a document type" | `add-page` keeps "for a page"; `add-document-type` asks about intent (will visitors land on it?) and hands off. Check with the evaluator's trigger runs |
| Trigger overlap: `add-block` and `add-element-type` both hear "settings composition" | A lone settings composition goes to `add-element-type`; "give blocks an option" goes to `add-block` |
| Peers fire their own approval gates inside an `add-page` run | The `## Used by another skill` section tells them not to |
| Broken links and anchors after the moves | Keep listed anchors; run the link checker |
| `add-document-type` description over 1024 characters | The draft is about 900; run the frontmatter checker after each edit |
| A fresh site's SEO composition needs a Toggle (default on) data type | Accepted: one data type page and an index page join the changeset |
| Per-skill fixture copies drift | Accepted; noted in each skill's Validation section |

Verification:

- `node scripts/check-skill-links.mjs` passes
- `node scripts/check-skill-frontmatter.mjs` passes
- Linter exits 0 on `golden-requirements/`
- Linter `--rules` and `--warnings` output for `broken-requirements/` and the review fixture match
  the expected files, as `.github/workflows/validate-skills.yml:52-63` does
- `grep -rn "umbraco-add-page-type" plugins README.md` returns nothing
- `grep -rni "routable" plugins` shows only deliberate uses
- `umbraco-skill-evaluator` runs on `add-page` (1-5), `add-block`, `add-element-type`,
  `add-document-type`, `add-listing-page`, `add-composition` (3), `configure-block-editor` (2)
  and `review-content-model`; every requirements folder they produce is linted

## Work order

1. Branch from freshly pulled `main` (Step 0).
2. Folder moves with `git mv`: `umbraco-add-page-type` to `umbraco-add-page`,
   `umbraco-add-element-type` to `umbraco-add-block`.
3. The two new skills: `umbraco-add-document-type` and the rewritten
   `umbraco-add-element-type`.
4. `add-page` and `add-block` rewrites, including `combined-changeset.md`.
5. Peer sections on `add-composition`, `configure-block-editor` and `add-data-type`.
6. Wording sweep, conventions `## Terms` table and linter changes (`page-seo` warning, message
   text).
7. Cross-references and anchors, including the schema delegation from the listing, data folder
   and site settings workflows.
8. Evals and fixtures.
9. README.

Changes land through a branch and pull request into `main` when asked. No version bump
(decision 5).
