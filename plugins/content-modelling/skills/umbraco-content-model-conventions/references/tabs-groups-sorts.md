# Tabs, groups and sort orders

**Applies to:** the tab, group and sort order of every property.
**Look for:** tabs sorted 0, 1, 2; the same tab with different sorts on two compositions;
properties sorted 1, 2, 3; a tab on an element type; an element with several groups.
**Why:** a page type is assembled from compositions. Tabs only line up in the same order on every
page type if every composition agrees on one global sort per tab.

## Which container to use

| Kind | Tabs | Groups |
|---|---|---|
| Document type, composition | Yes, every property is in a tab | Only when two compositions share a tab and their fields need telling apart |
| Content element, child item | No tab | Exactly one group, `Content`, sort 0 |
| Settings composition (for block settings) | `Style` or `Settings` | None |
| Settings model | None of its own; it shows the `Style` and `Settings` tabs of its compositions | None |
| Site settings | Yes | As needed |

A content element is edited in a small overlay; tabs there are noise. If an element seems to need
tabs it is two elements, or the second tab is really its settings model. Settings compositions are
the one kind of element type that does carry a tab: the block's settings pane is where `Style` and
`Settings` are read, so a review must not count them as "an element with a tab".

## Global tab sorts

A tab has the same sort everywhere it appears. The name decides the sort, not the type.

| Tab | Sort |
|---|---|
| A tab specific to one type | 0 |
| Content | 100 |
| Page Details | 200 |
| Section Navigation | 300 |
| Tags | 400 |
| Sidebar | 500 |
| SEO | 600 |
| Visibility | 900 |
| Admin | 3000 |

The gaps are deliberate: a project can add a tab between two others without renumbering. Editor
content comes first, configuration in the middle, and the tabs most editors never open come last.
Tab and group names avoid `&`, `?` and `%` (`Terms and Conditions`, not `Terms & Conditions`): the
Umbraco Developer MCP refuses any value containing them, so a name with one can never be applied
cleanly.

Settings compositions use their own two tabs: `Style` sorts 50 and `Settings` sorts 100.

The site settings singleton is the exception. Its tabs are its own, so they are simply numbered
0 to 9 in the order an editor should read them.

## Groups

When two compositions put fields in the same tab, each gives its fields a group so the editor can
see which belong together, for example `Search` and `Sharing` inside `SEO`. A group is not named
after its tab. Group sorts go up in hundreds from 0 (`Search` 0, `Sharing` 100) and order the
groups within that tab, leaving room for a third composition between them. A composition that owns its tab alone uses no group.

## Property sorts

Property sorts go up in hundreds: 100, 200, 300. A page type that needs a field between two
composition fields can then use 150 without touching the composition. Sorts restart in each
container.

## Applying sorts

Sorts are part of the requirements doc. Creating a type through the backoffice or the MCP numbers
tabs and properties 0, 1, 2, so every sort is set explicitly afterwards; how is described by
`umbraco-content-requirements-documentation`. Do not assume a created type has the right sorts until
you have read it back.

**Related:** [Compositions](compositions.md), [Allowed children and
root](allowed-children-and-root.md).
