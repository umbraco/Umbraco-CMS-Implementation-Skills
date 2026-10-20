# Minimum viable page

When the user asks for "a page type", "a basic page" or "something editors can make pages with"
and says nothing more, propose a **Content Page** instead of asking a list of questions. It is
deliberately small: the right name, place, compositions, template and parent, and two fields.
Everything else is layered on later.

It still goes through the whole [workflow](workflow.md): inspect, write the requirements doc,
stop for approval. The default only saves the decisions.

## The default

| Setting | Value |
|---|---|
| Name and alias | Content Page, `contentPage` |
| Kind | Content ([page-kinds.md](page-kinds.md#content)) |
| Folder | The Document Types root |
| Icon | `icon-document color-light-blue` |
| Description | A general page of text, for anything that is not an article or a listing. |
| Compositions | `seoComposition` always, plus every other page composition the site already has |
| Own properties | `pageTitle` (Textstring) and `bodyText` (Richtext editor) on the `Content` tab |
| Template | Content Page, `contentPage.cshtml`, under the master |
| Allowed at root | No |
| Allowed as a child of | The home page, and itself |
| Allowed children | Itself, plus any listing page types the site has |
| Vary by culture | No, unless the site has more than one language; then ask |

Adjust it to what the inspection found:

| The site has | Change |
|---|---|
| A composition that already gives a page title | Drop `pageTitle` |
| A listing page type (`...ListingPage`) | Add it to Allowed children, so a section can hold a listing |
| A body block grid data type (for example `Main Content Block Grid`) | Use it as `mainContent` instead of `bodyText`; the grid is an Update only if the page needs a block it does not offer ([combined-changeset.md](combined-changeset.md)) |
| No home page | Ask whether to add one in the same change ([the root question](workflow.md#the-root-question)) |
| No master template | Add a Master template to the change, first |
| No SEO composition | Add it to the same change ([SEO is mandatory](workflow.md#seo-is-mandatory)); page details and sharing compositions that are missing are follow-ups, not part of this change |
| A `contentPage` already | Nothing to add; say so and offer to extend it |

## Worked example

A single-language site with a home page (`homePage`), a master template, an existing
`seoComposition` and no listing pages. The requirements folder is `docs/umbraco-schema/`. The
home page has no requirements page and is read with the MCP, so its page is written from the
read-back as an `Update`
([requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists));
that page is not shown here.

`Document-Types/ContentPage.md`:

```markdown
# Content Page

> **Status:** proposed

## Definition

| Setting | Value |
|---|---|
| Name | Content Page |
| Alias | `contentPage` |
| Kind | Document Type |
| Icon | `icon-document color-light-blue` |
| Description | A general page of text, for anything that is not an article or a listing. |
| Folder | [Document Types](../Document-Types.md) |
| Allowed at root | No |
| Vary by culture | No |
| Default template | [Content Page](../Templates/Master/ContentPage.md) |
| Allowed templates | [Content Page](../Templates/Master/ContentPage.md) |
| Allowed children | [Content Page](ContentPage.md) |
| Compositions | SEO Composition |

## Properties

| Tab | Tab Sort | Group | Group Sort | Name | Alias | Data Type | Editor | Value Type | Mandatory | Sort | Description |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Content | 100 | — | — | Page Title | `pageTitle` | Textstring | `Umbraco.TextBox` | `System.String` | No | 100 | The heading at the top of the page. Falls back to the page name if this is not set. |
| Content | 100 | — | — | Body Text | `bodyText` | Richtext editor | `Umbraco.RichText` | `Umbraco.Cms.Core.Strings.IHtmlEncodedString` | No | 200 | The main text of the page. |

## Used by

- Allowed as a child of: [Home Page](HomePage.md), [Content Page](ContentPage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Richtext editor | `data-type` | Exists |
| SEO Composition | `document-type` | Exists |
| Textstring | `data-type` | Exists |
| [Content Page](../Templates/Master/ContentPage.md) | `template` | New in this changeset |
```

`Templates/Master/ContentPage.md`:

```markdown
# Content Page

> **Status:** proposed

## Definition

| Setting | Value |
|---|---|
| Name | Content Page |
| Alias | `contentPage` |
| File | `contentPage.cshtml` |
| Master | Master |

## Used by

- Default template for: [Content Page](../../Document-Types/ContentPage.md)
- Allowed on: [Content Page](../../Document-Types/ContentPage.md)

## Dependencies

| Artifact | Type | Flags |
|---|---|---|
| Master | `template` | Exists |
```

`_changesets/2026-09-30-content-page.md`:

```markdown
# Changeset: Content page

> **Status:** proposed

## Summary

Adds a Content Page type and its template, so editors can create general pages under the home
page and under each other. It takes the existing SEO Composition and has a page title and body
text of its own. The Home Page is updated to allow it as a child.

## Requirements pages

| Order | Requirements page | Kind | Action |
|---|---|---|---|
| 1 | [Content Page](../Templates/Master/ContentPage.md) | Template | Create |
| 2 | [Content Page](../Document-Types/ContentPage.md) | Document type | Create |
| 3 | [Home Page](../Document-Types/HomePage.md) | Document type | Update |

## Apply checklist

- [ ] 1. Create template `contentPage` under `master`
- [ ] 2. Create document type `contentPage`, then fix-up (sorts, descriptions, template)
- [ ] 3. Set allowed children on `contentPage`: `contentPage`
- [ ] 4. Set allowed children on `homePage`: add `contentPage`, keeping the existing entries
- [ ] 5. Verify every requirements doc against the site and set each status line

## Apply log

—
```

Without the MCP, `HomePage.md` is not written: the Used by line names Home Page as plain text,
row 3 of Requirements pages is dropped, and the summary says the home page was confirmed by the
user, not read from the site.

**Related:** [workflow.md](workflow.md), [page-kinds.md](page-kinds.md).
