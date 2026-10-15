---
name: umbraco-add-site-settings
description: >
  Add the site-wide settings node to an Umbraco 17+ site: a separate `siteSettings` singleton
  allowed at the content root, with no template and no children, its own tabs numbered 0 to 9
  (general, header, footer, social, SEO defaults, scripts), and the settings fields in them. The
  change is written as a requirements doc, approved, applied through the Umbraco Developer MCP or
  a manual backoffice walkthrough, and read back.
  Use this whenever the user asks to "add site settings", "where should the logo and footer links
  go", "add global settings", "add a settings node", "store the social links for the whole site",
  "add a tracking code setting", or "move settings off the home page".
  SKIP: non-Umbraco projects or Umbraco < 17; fields for a single page (a composition, use
  umbraco-add-composition); block styling settings (use umbraco-add-block); a page (use
  umbraco-add-page); questions about naming only (use umbraco-content-model-conventions).
---

# Add Site Settings

Adds **the site settings singleton**: one `siteSettings` node at the root of the content tree that
holds what belongs to the whole site (the logo, navigation, footer, social links, default sharing
image, tracking codes). It is not the home page. Its schema is decided by
[`umbraco-add-document-type`](../umbraco-add-document-type/SKILL.md#used-by-another-skill) with
the role site settings; this skill adds the tabs, the root behaviour and the content node. It
works requirements-first: write the change down, stop for the user's approval, apply it, then
read it back.

The rules come from
[`umbraco-content-model-conventions`](../umbraco-content-model-conventions/SKILL.md), and the
requirements doc format and
[the six-step workflow](../umbraco-content-requirements-documentation/references/change-workflow.md)
from
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md);
both must be installed alongside this one. If a link into either cannot be read, stop and say so.

## Steps

| Step | What happens | Reference |
|---|---|---|
| 1. Inspect | Existing settings (on the home page, in another node), root types, languages | [workflow.md](references/workflow.md#1-inspect) |
| 2. Decide the tabs | Which tabs, in which order, 0 to 9 | [tab-layout.md](references/tab-layout.md) |
| 3. Decide the fields | Aliases, data types, repeaters | [workflow.md](references/workflow.md#2-decide) |
| 4. Write and lint | The settings type, any data types and repeaters, a changeset | [workflow.md](references/workflow.md#3-write-the-requirements-doc) |
| 5. Approve | **Stop.** The user approves the changeset | [workflow.md](references/workflow.md#4-stop-for-approval) |
| 6. Apply | Through the MCP if connected, otherwise a manual walkthrough | [workflow.md](references/workflow.md#5-apply) |
| 7. Verify | Read everything back, set the status lines, report | [workflow.md](references/workflow.md#6-verify-and-report) |

### How to decide between MCP and manual

One approach, two mechanisms: the Umbraco Developer MCP when it is connected, otherwise a manual
walkthrough generated from the same requirements doc; see
[change-workflow.md](../umbraco-content-requirements-documentation/references/change-workflow.md#5-apply).

## Version compatibility

Targets **Umbraco 17+**. MCP tool names were checked as described in
[`umbraco-content-requirements-documentation`](../umbraco-content-requirements-documentation/SKILL.md#version-compatibility);
the connected tool list is the authority.

## Best practices

- **Settings are not on the home page.** The rule is in
  [allowed-children-and-root.md](../umbraco-content-model-conventions/references/allowed-children-and-root.md#site-settings);
  it deliberately overrides the older advice to put settings on the root page.
- **One type, one node per site.** A second site gets a second `siteSettings` content node, never
  a `micrositeSettings` type; how code finds the right node is agreed with the developer.
- **The home page stays first at the content root.** A settings node sorted above it changes
  every URL; see [root order](../umbraco-content-model-conventions/references/allowed-children-and-root.md#allowed-at-root)
  and check the position after the user creates the node.
- **Its tabs are its own, 0 to 9**, with no template, children or compositions; the rules are in
  [tabs-groups-sorts.md](../umbraco-content-model-conventions/references/tabs-groups-sorts.md#global-tab-sorts)
  and [templates.md](../umbraco-content-model-conventions/references/templates.md), the layout in
  [tab-layout.md](references/tab-layout.md).
- **Moving settings off the home page loses their values** unless they are migrated. Stop and say
  so; never delete the home page's fields to make room.
- **"Just add a settings node" is not approval.** It is a request for the change; stop at
  [the approval gate](../umbraco-content-requirements-documentation/references/change-workflow.md#4-stop-for-approval).

## Validation

Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`. Coverage tier: **Documented**. This skill ships no code and no assets.
The requirements docs it writes are checked by the linter in
`umbraco-content-requirements-documentation`, and an eval graded the guidance. Nothing ran against
a live site.
