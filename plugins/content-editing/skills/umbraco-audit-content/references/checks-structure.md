# Checks: tree depth, orphans, internal links

Where a page sits and what links to it. These come from the CMS Editor MCP's site structure and
relationships results, so they show content links, not menus a template builds.

| Check | Finding when | Not a finding |
|---|---|---|
| Depth | A page that matters sits more than 4 levels deep | Archives, legal pages and utility pages deep in the tree |
| Orphans | No content references the page and it is not in navigation | A page linked only from the menu: menus are often built by the template, so ask or mark as unverified before calling it orphaned |
| Outbound links | A link to a page that is unpublished, in the recycle bin or missing | External links the tools cannot check: list them as unverified |
| Link text | "Click here" or "Read more" as the only text of an important link | Repeated "Read more" on cards with clear headings (Low at most) |

## Rules

- **Content references are not all links.** A page can be reached from navigation, search or a
  listing the template renders. Never call a page orphaned on content references alone without
  saying so.
- **Depth matters only for important pages.** Ask which pages matter, or use the search evidence
  if the user supplied it.
- **Before any move or delete, check references.** The audit only reports; moving or removing a
  page is a separate change that starts by listing what still links to it.
