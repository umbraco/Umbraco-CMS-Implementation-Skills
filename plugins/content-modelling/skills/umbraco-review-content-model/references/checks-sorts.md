# Checks: tabs, groups and sorts

Rules:
[tabs-groups-sorts.md](../../umbraco-content-model-conventions/references/tabs-groups-sorts.md). On
a requirements folder the linter reports `tab-sorts`, `element-shape` and the `sort-hundreds`
warning; carry those into the report.

On the live site, read each type's `containers` (name, type Tab or Group, `sortOrder`, parent)
and each property's `container` and `sortOrder` from `get-document-type-by-id`.

| Check | How to detect | Severity | Fix with |
|---|---|---|---|
| A global tab with a local sort | A tab named in the global table (Content, Page Details, SEO, Visibility, ...) whose `sortOrder` is not its global value | Medium: tabs appear in different orders on different pages | the owning skill |
| The same tab with different sorts on two types or compositions | Compare the tab's `sortOrder` across every type that has it | Medium | the owning skill |
| A type-specific tab not sorted 0 | A tab not in the global table with a sort other than 0 | Low | the owning skill |
| Tabs numbered 0, 1, 2 | Every tab on a type numbered in creation order: usually a type created in the backoffice and never adjusted | Medium | the owning skill |
| Content element with a tab | An element type outside `Elements/Compositions/` and `Elements/Settings/` with a container of type Tab (settings compositions and the settings models built from them legitimately carry `Style` and `Settings`) | Medium | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |
| Element type with several groups, or a group not called Content | More than one group, or the group name is not `Content` | Low | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |
| Groups where a tab is not shared | A composition or type that owns a tab alone but splits it into groups | Low | the owning skill |
| Property sorts not in hundreds | Property `sortOrder` values such as 0, 1, 2 | Low | the owning skill |
| Site settings tabs outside 0 to 9 | `siteSettings` tab sorts of 100 or more | Low | [umbraco-add-site-settings](../../umbraco-add-site-settings/SKILL.md) |
| Settings composition on a page tab | A settings composition using `Content` rather than `Style` (50) or `Settings` (100) | Low | [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) |

Re-sorting changes only the order editors see; it never touches content. Say so in the suggested
fix, because it makes these the safest findings to act on first.

**Related:** [checks-compositions.md](checks-compositions.md).
