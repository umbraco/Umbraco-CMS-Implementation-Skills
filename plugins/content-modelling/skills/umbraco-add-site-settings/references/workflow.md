# Workflow

Follow
[the six steps](../../umbraco-content-requirements-documentation/references/change-workflow.md).
This file adds only what the settings singleton needs at each step.

## 1. Inspect

For site settings, establish:

| Question | Why it matters here |
|---|---|
| Is there already a `siteSettings` type, or settings on another type? | Extend the existing one; never create a second |
| Does the home page carry settings fields (a settings tab, a settings composition)? | Moving them loses values unless migrated |
| What is allowed at root, and which node is sorted first there? | Site settings joins the home page and data folders at root; the home page must stay first |
| Is it a multi-site install? | One settings node per site; how code finds the right one is agreed with the developer |
| Which data types and elements exist for the fields (pickers, repeaters)? | Reuse before creating |

## 2. Decide

| Decision | For site settings | Rule |
|---|---|---|
| Schema: name, alias, folder, icon, description | Decided by `add-document-type` with the role site settings: Site Settings, `siteSettings`, the one root type without a `Page` suffix; the Document Types root, beside the pages; `icon-settings color-green`, the data colour; described by what it holds ("Settings for the whole site: logo, navigation, footer and defaults.") | [add-document-type workflow](../../umbraco-add-document-type/references/workflow.md#2-decide), [type-roles.md](../../umbraco-add-document-type/references/type-roles.md) |
| Allowed at root | Yes, sorted after the home page | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root) |
| Allowed children, templates, compositions | None | [allowed-children-and-root.md](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#site-settings), [templates.md](../../umbraco-content-model-conventions/references/templates.md) |
| Tabs and sorts | Its own tabs, 0 to 9 in reading order | [tab-layout.md](tab-layout.md) |
| Fields | Area-prefixed aliases, pickers for links, repeaters for structured lists | [tab-layout.md](tab-layout.md#field-rules) |
| Vary by culture | Ask on a multi-language site: footer text and navigation labels usually vary; a tracking id does not | — |

### Settings already on the home page

If the home page already carries settings fields with content, do not plan to delete them. Say
that moving them to the new node loses their values unless they are migrated (a content task),
specify the new node alongside, and record the removal from the home page as a later change once
the values have moved. The same rule applies to any existing property moved between types.

## 3. Write the requirements doc

| Page | From | Location | Action |
|---|---|---|---|
| Any new data type (pickers, a script text area) | [data-type.md](../../umbraco-content-requirements-documentation/assets/data-type.md) | `Data-Types/<Editor-Kind>/<Name-Slug>.md` | Create |
| Any repeater element and its Block List | via [umbraco-add-block](../../umbraco-add-block/SKILL.md) | `Document-Types/Elements/<AliasPascalCase>.md`, `Data-Types/Block-List/<Name-Slug>.md` | Create |
| The settings type | [document-type.md](../../umbraco-content-requirements-documentation/assets/document-type.md) | `Document-Types/SiteSettings.md` | Create, or Update when one exists |
| The changeset | [changeset.md](../../umbraco-content-requirements-documentation/assets/changeset.md) | `_changesets/<yyyy-mm-dd>-<slug>.md` | — |

Points specific to site settings:

- Default template, Allowed templates, Allowed children and Compositions are all `—`, and the
  `Collection` row is deleted. The linter rejects a template on `siteSettings`.
- Every property row has a Tab Sort between 0 and 9, and property sorts in hundreds within each tab.
- `Used by` is `—`: nothing allows it as a child.
- An existing `siteSettings` type that gains tabs or fields is an Update, written as
  [requirements-lifecycle.md](../../umbraco-content-requirements-documentation/references/requirements-lifecycle.md#updating-a-type-that-already-exists)
  describes.

The changeset's checklist, in apply order:

```
- [ ] 1. Create any new data types and repeater elements                          (only if needed)
- [ ] 2. Create document type `siteSettings`, then fix-up (tab sorts 0-9, sorts, descriptions, allowed at root)
- [ ] 3. Create the Site Settings content node at the content root, sorted after the home page   (content, by the user)
- [ ] 4. Verify every requirements doc against the site and set each status line
```

## 4. Stop for approval

For site settings, call out: any settings that currently live elsewhere and would need migrating,
any script fields (editors can inject markup), and that the new root node must sort after the
home page. Then stop; see
[the approval gate](../../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).
If the user asked for the backoffice steps and the MCP is not connected, see
[apply-manually.md](../../umbraco-content-requirements-documentation/references/apply-manually.md#asked-for-the-steps-before-approval).

## 5. Apply

For site settings, also:

- **The create** passes `allowedAsRoot: true` and no `parentId`.
- **The fix-up** sets each tab's `sortOrder` to its 0 to 9 value; the create numbers them in order
  of first appearance, which is right only if the properties were passed in tab order.
- **The content node** is created by the user in the Content section, at the root. Only one, and
  it must not become the first root node: the home page stays sorted first, or carries the domain
  in Culture and Hostnames, because the first root node that is a page owns `/`
  ([root order](../../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root)).
  Tell the user this with the step, and ask them to check the sort order once the node exists.

## 6. Verify and report

For site settings, also confirm that the type is allowed at root, has no template and no allowed
children, and that every tab sort matches the requirements doc. Confirm the content root order
too: the home page is sorted first and the settings node after it. The content tree is not read
by the schema tools, so that is the user's confirmation, and the report says so.

In the report's "what is left", name: creating the settings content node and filling it in,
migrating any settings still on the home page, the Razor or C# that reads the settings (found by
type, not by assuming the home page), and permissions on the node.
