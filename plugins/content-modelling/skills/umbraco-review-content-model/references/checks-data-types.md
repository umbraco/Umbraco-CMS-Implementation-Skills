# Checks: data types

Rules: [naming.md](../../umbraco-content-model-conventions/references/naming.md#data-types),
[tree-organisation.md](../../umbraco-content-model-conventions/references/tree-organisation.md#data-types).
These checks need the configuration of each data type (`get-data-type`) and what uses it
(`get-references-data-type`). A requirements folder shows only the data types it specifies; say so.

| Check | How to detect | Severity | Fix with |
|---|---|---|---|
| Duplicates | Two custom data types with the same editor and the same configuration | Low | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md): keep one, move properties (same value format, so safe) |
| Unused | A custom data type with no references | Low | delete after confirming, not part of the review |
| Constraint on the property instead of the data type | A property with a validation regex or message that expresses a length or count the data type could hold | Medium | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) |
| A built-in reconfigured | A built-in data type (Textstring, Textarea, ...) whose configuration differs from the default, or renamed | Medium: every use changed at once | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) |
| Text where a picker belongs | A Textstring whose alias or description says it holds a URL, a page, an author or a category | Medium | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) or [umbraco-add-data-folder](../../umbraco-add-data-folder/SKILL.md); converting existing values is a migration |
| Rich text for plain text | A rich text property for a summary, caption or title | Low | [umbraco-add-data-type](../../umbraco-add-data-type/SKILL.md) |
| Free-form tags with duplicates | A Tags property whose values differ only by case or spelling | Low | [umbraco-add-data-folder](../../umbraco-add-data-folder/SKILL.md) (taxonomy) |
| One block editor shared by placements that should differ | A Block Grid used by two properties whose areas need different blocks | High if a block appears where it cannot render | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) |
| A repeater allowing unrelated blocks | A Block List registering several unrelated elements | Medium | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) |
| Block registered without its settings model | A Block Grid entry with no settings element for a block that has one elsewhere | Low | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) |
| Page areas on Block Lists | Page-level layout properties using `Umbraco.BlockList` | Low (a project convention if consistent) | [umbraco-configure-block-editor](../../umbraco-configure-block-editor/SKILL.md) |

A data type change affects every property that uses it. List those properties in the finding.

**Related:** [checks-naming.md](checks-naming.md).
