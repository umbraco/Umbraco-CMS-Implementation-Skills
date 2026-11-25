# Apply and verify

Steps 5 and 6 of umbraco-draft-page. Only after the user has approved the page plan.

## With the CMS Editor MCP

1. **Create the page as a draft** under the approved parent with the approved page type. Use the
   plain create tool, never create-and-publish.
2. **Add the blocks in plan order.** Use the tool that matches the property's editor (block list,
   block grid or a block inside rich text). For a block grid, place blocks in the areas the plan
   names.
3. **Set the page fields** (title, meta description, any pickers) with the page edit tool, which
   saves without publishing.
4. **Read back.** Re-read the page and inspect its blocks. Tick each plan row: saved, or failed with
   the reason.

The writes are separate calls, not one transaction. If a call fails halfway, stop, report what was
saved and what was not, and continue only with the user's agreement. Don't delete and recreate the
page to start over without asking.

## Without the MCP

Hand over the approved plan as backoffice steps: where to create the page, which page type or
blueprint to use, then each block in order with the copy to paste into each field. Say clearly that
nothing was written to the site.

## Hand over

Tell the editor:

- The draft's name and location, and that it is not published.
- What to check in the preview: headings, layout on mobile, images and links.
- Open items: missing images, claims to confirm, unverified limits, gaps.

Never use publishing or scheduling tools in this skill. If the user asks to publish, explain that
the preview is the check that the content data cannot replace, and leave publishing to them.
