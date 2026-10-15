# One changeset per page

A page is written up once, approved once and applied leaves first. This file says how the parts a
page needs are routed to the peer skill whose rules apply, how their pages merge into one
changeset, and the order they are applied in. Open it when the page needs more than the common
case in [workflow.md](workflow.md#3-write-the-requirements-doc): a block, a block area or a new
data type.

## Parts and who writes them

| Part the page needs | When | Rules from | Pages written |
|---|---|---|---|
| New data type for a page field | Nothing on the site fits | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md#used-by-another-skill) | `Data-Types/<Editor-Kind>/<Name-Slug>.md`, plus the folder index if the folder is new |
| SEO composition | The site has none | [umbraco-add-composition](../../umbraco-add-composition/SKILL.md#used-by-another-skill) | `Document-Types/Compositions/SeoComposition.md`, plus the `Compositions` index and `Data-Types/Toggle/Toggle-default-on.md` if needed |
| Sharing or page-details composition | Missing | Not added; named as a follow-up | None |
| New block | The user names a block that does not exist | [umbraco-add-block](../../umbraco-add-block/SKILL.md#used-by-another-skill) | `Document-Types/Elements/...`, `Elements/Settings/...`, `Elements/Compositions/...`, `Data-Types/Block-List/...` as the block needs |
| Single new element | One element with no settings and no repeater | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md#used-by-another-skill) | `Document-Types/Elements/<AliasPascalCase>.md` |
| Block area | The page has one | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md#used-by-another-skill), approach A | `Data-Types/Block-Grid/<Placement>-Block-Grid.md`: Create, or Update keeping every existing entry |
| Master template | The site has none | This skill | `Templates/Master.md` |
| Page template | Always | This skill | `Templates/<MasterPascalCase>/<AliasPascalCase>.md` |
| Page schema | Always | [umbraco-add-document-type](../../umbraco-add-document-type/SKILL.md#used-by-another-skill), role page | `Document-Types/<AliasPascalCase>.md` |
| Page behaviour: template link, allowed at root, own children | Always | This skill | The same page |
| Parent update | The parent exists | This skill | `Document-Types/HomePage.md` (Update) |
| Home page | The site has none | Ask, then this skill with the kind Root | `Document-Types/HomePage.md` and its template |

Each peer, when called from here, inspects, decides and writes only; it does not stop for its own
approval and does not apply. It hands back its pages, their dependency flags and its checklist
rows.

## Merge rules

- **One changeset file and one summary.** The summary says what the page is for and lists each
  part the page brought with it, in a sentence each.
- **Pages numbered in apply order**, leaves first (below). The Requirements pages table and the
  checklist agree.
- **Each folder created once.** A `Compositions`, `Elements` or data type folder that several
  parts need is one checklist item, at the first step that needs it, with its index page written
  once.
- **Dependency flags point inside the changeset.** A part written here is `New in this changeset`
  on every page that needs it, with a link; nothing in the changeset is `Missing`.
- **Allowed children set only after every type exists**, so the page's own children and the parent
  update are the last steps before verification.
- **One verify item at the end**, covering every requirements page in the changeset.

## Apply order

Leaves first, matching
[apply-via-mcp.md](../../umbraco-content-requirements-documentation/references/apply-via-mcp.md#order).
Keep only the rows the page needs, each with its condition removed, and keep the numbering
contiguous in the checklist.

```
- [ ] 1. Create data type folders and data types (page fields, the SEO toggle, block fields)   (only if new)
- [ ] 2. Create document type folders `Compositions`, `Elements`, `Elements/Compositions`, `Elements/Settings`   (only those missing)
- [ ] 3. Create composition `seoComposition` in `Compositions`, then fix-up                    (only if new)
- [ ] 4. Create each settings composition, move it into `Elements/Compositions`, then fix-up    (only if new)
- [ ] 5. Create settings model `<alias>`, move it into `Elements/Settings`, then fix-up         (only if new)
- [ ] 6. Create each repeater item element, move it into `Elements`, then fix-up                (repeater only)
- [ ] 7. Create each Block List data type                                                     (repeater only)
- [ ] 8. Create each content element, move it into `Elements`, then fix-up                     (new blocks only)
- [ ] 9. Create or update data type `<Block Grid>`, keeping the existing entries               (block area only)
- [ ] 10. Create template `master`                                                           (only if new)
- [ ] 11. Create template `<alias>` under `master`
- [ ] 12. Create document type `<alias>` with `seoComposition`, then fix-up (sorts, mandatory, descriptions, template, culture)
- [ ] 13. Set allowed children on `<alias>`: `<alias>`                                       (content page only)
- [ ] 14. Set allowed children on `homePage`: add `<alias>`, keeping the existing entries
- [ ] 15. Verify every requirements doc against the site and set each status line
```

Step 12 also sets `mainContent` to the Block Grid when the page has a block area. A block's
settings element is optional, so steps 4 and 5 drop out for a block that has none.

## At the approval gate and in the report

The approval gate's callouts are in
[workflow.md](workflow.md#4-stop-for-approval), and what is left after verification is in
[workflow.md](workflow.md#6-verify-and-report). Both cover the parts listed here.

## Example: a landing page with a new block

A site with `homePage`, `contentPage`, the three page compositions, a master template, blocks
`hero` and `richText` with the shared settings models, and a `Main Content Block Grid` used by
`contentPage.mainContent`. The user asks for a Landing Page built from blocks, with a new
Testimonial block. The changeset's checklist:

```
- [ ] 1. Create element `testimonial`, move it into `Elements`, then fix-up
- [ ] 2. Update data type `Main Content Block Grid`: add `testimonial` with `blockSettings`, keeping `hero` and `richText`
- [ ] 3. Create template `landingPage` under `master`
- [ ] 4. Create document type `landingPage`, then fix-up (sorts, descriptions, template)
- [ ] 5. Set allowed children on `homePage`: add `landingPage`, keeping the existing entries
- [ ] 6. Verify every requirements doc against the site and set each status line
```

Five requirements pages (the element, the grid as an Update, the template, the page, the home page
as an Update) and one approval. The testimonial's Razor partial and the landing page's markup are
what is left.

**Related:** [workflow.md](workflow.md), [page-kinds.md](page-kinds.md),
[minimum-viable-page.md](minimum-viable-page.md).
