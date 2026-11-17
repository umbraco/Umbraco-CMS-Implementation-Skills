# Learn the site

Step 2 of umbraco-draft-page: find out how pages of this type are built here before planning one.
With the CMS Editor MCP, read it from the site. Without it, ask the user for an export of one or two
existing pages and the page type's fields.

## What to read

| Need | Where it comes from |
|---|---|
| Parent page | Search by name rather than walking the tree |
| Page type and its fields | The document type's schema: property aliases, names, descriptions. Separate page-level fields (title, meta description, navigation) from block editors |
| A ready-made start | Blueprints for the page type, if the site has them |
| Which blocks pages use | The blocks on one to three recent, published pages of the same type. Page data only counts blocks; inspect the blocks to see types, keys and values |
| Shapes of structured fields | The property value template for pickers and other non-block editors |
| Voice and approved facts | The user. Ask if they have a style guide, approved claims or product facts |

## Rules

- **Recent and published.** Old pages may use blocks that are no longer allowed. Prefer the newest
  published pages and say which pages you read.
- **Three kinds of rule, kept apart.** What the schema allows (the document type and block data),
  what the user's guidelines say, and what existing pages happen to do. Only the first is a hard
  limit; typical copy length is not a maximum.
- **Unknown stays unknown.** Field limits, allowed blocks per area and validation may not be visible.
  Mark them as unverified in the plan rather than guessing.
- **Layout needs the preview.** Heading levels and layout come from templates. Don't infer them from
  block names or JSON.
- **Check who you are.** If a tool is missing or a call is refused, check the current user and its
  groups; the MCP can only do what that backoffice user can do.
