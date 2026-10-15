# Icons and colours

**Applies to:** the icon of every document type and element type.
**Look for:** the default icon on every type, colour applied to some pages and not others, an
icon name that does not exist on the site.
**Why:** the content tree and the create dialog are scanned by icon first. Colour by role lets an
editor tell a page from a data item before reading the name.

## Icon

Pick an icon that matches the noun: `icon-home` for the home page, `icon-newspaper` for an
article, `icon-list` for a listing, `icon-settings` for settings and compositions. Check that the
name exists before using it: the backoffice icon picker shows the site's icons; the MCP tool
`get-icons` lists the icons built into the MCP package, which covers Umbraco's own set but not
icons a package or the project added.

## Colour by role

The icon value is the icon name, optionally followed by a colour class:
`icon-newspaper color-light-blue`.

| Role | Colour | Example |
|---|---|---|
| Page (has a template) | `color-light-blue` | `icon-newspaper color-light-blue` |
| Data folder or data item | `color-green` | `icon-folder color-green` |
| Site settings (`siteSettings`) | `color-green` | `icon-settings color-green` |
| Composition | none | `icon-settings` |
| Element type, settings model | none | `icon-blockquote` |

Colour marks the two kinds of node an editor meets in the content tree: pages, and data that is
not a page. Site settings is a data node in that sense, so it takes the data colour. Compositions
and elements never appear in the content tree, so they stay uncoloured.

## What cannot be checked

`get-icons` returns icon names only, from the MCP package rather than the site. It cannot confirm
that a colour class is valid, so a wrong colour fails silently and the icon renders in the default
colour. Use only the colour classes in the table, and say that the colour was not validated when
you report what you applied.

**Related:** [Names, aliases and suffixes](naming.md).
