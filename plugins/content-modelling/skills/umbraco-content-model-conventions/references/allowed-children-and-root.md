# Allowed children and allow-at-root

**Applies to:** the structure settings of every document type.
**Look for:** several page types allowed at root, a listing that allows unrelated types, an item
type that allows children, settings stored on the home page.
**Why:** these two settings decide what an editor can create and where. Loose settings produce a
content tree nobody designed.

## Allowed at root

Only three kinds of type are allowed at root:

| Type | Why it is at root |
|---|---|
| The home page (`homePage`) | It is the site |
| Site settings (`siteSettings`) | One per site, found without walking the tree |
| Data and taxonomy folders (`...Folder`) | Shared data that belongs to no page |

Everything else is created under a parent. If a new page type has no parent yet, add it to the
allowed children of the home page; do not allow it at root to make it creatable.

**Root order matters.** With the default `HideTopLevelNodeFromPath` setting, the first root node
in sort order owns `/`, and a second root that is a page changes every URL's shape. When site settings
or a data folder joins the root, the home page must stay sorted first (or carry the domain in
Culture and Hostnames), and the new root node must not be a page (no template). Say so in the change, and
check the order after the user creates the node.

On an empty site with nothing allowed at root, do not quietly allow a content page at root. Say
that a home page is missing and ask whether to create one.

## Allowed children

| Kind | Allows | Reason |
|---|---|---|
| Home page | Content pages, listing pages, programmatic pages | It is the top of the site |
| Content page | Itself, plus listing pages | Editors build their own hierarchy, and a section can contain a listing |
| Listing page | Its item type, plus itself | A listing can be divided into sub-listings of the same kind |
| Item page (`articlePage`) | Nothing | It is a leaf |
| Programmatic page (search, sitemap) | Nothing | It is driven by code |
| Data folder | Its item type, and itself if nesting is wanted | It is a container |
| Data item | Nothing | It is a leaf |
| Site settings | Nothing | It is a singleton |

Set allowed children **last**, after every type in the change exists, because a parent cannot
allow a child that has not been created. Adding a new type usually means editing an existing
parent too; include that parent in the change.

## Listings

A listing page has its own collection view data type, named `<Type> Collection View`, so each
listing can choose its columns and ordering without affecting the others. The one exception: two
listings of the **same** item type (two regional news listings) may share one, named after the
item they list, because the same columns fit both.

## Site settings

Site-wide settings live on a separate root singleton, `siteSettings`: no template, no children,
allowed at root. They do not live on the home page. Keeping them apart means the home page stays
a page, settings can have their own permissions, and the settings node is found by type rather
than by assuming the home page carries it. Its tabs are numbered 0 to 9; see
[tabs-groups-sorts.md](tabs-groups-sorts.md).

**Related:** [Templates](templates.md), [Tree organisation](tree-organisation.md).
