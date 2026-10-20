# Templates

**Applies to:** the default template and allowed templates of every document type.
**Look for:** a page type with no template, a template whose alias differs from its type, a
template on a composition, element, data item or site settings, several allowed templates on one
type.
**Why:** a template is what makes a document type a page; any node can have a URL, but nothing
renders it without one. Matching the alias means the view for any page is found by its type name,
with no lookup.

## Rules

| Rule | Reason |
|---|---|
| Every page has exactly one default template | Without one the page has a URL that falls through to the 404 handler unless a controller renders it, so the schema alone does not make the page work |
| The template alias equals the document type alias, and the file is `<alias>.cshtml` | `articlePage` renders with `articlePage.cshtml` |
| The template name equals the document type name | It is recognisable in the Templates tree |
| Every page template sits under the site's master template | Layout is declared once |
| Programmatic pages get a template too | A search or sitemap page still needs a view; the controller that drives it is code, not schema |
| Nothing else gets a template | Compositions, elements, settings models, data folders, data items and site settings are not pages. Their nodes may still have URLs; nothing renders them |
| One allowed template, the default | Add a second only for a genuine alternate rendering of the same content, such as a print view |

If the site has no master template yet, create it first and then create page templates beneath
it. A template is placed under a master by its layout declaration, not by a tree setting, so the
page template's content must name the master.

## Order

Templates are created after data types and elements and before the page types that use them, with
the master first. The default template is attached to the document type in the fix-up pass; the MCP
create tool for document types does not accept a template.
`umbraco-content-requirements-documentation` describes the order and the fix-up pass.

**Related:** [Allowed children and root](allowed-children-and-root.md), [Names, aliases and
suffixes](naming.md).
