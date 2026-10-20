# Descriptions

**Applies to:** the description of every type and every property.
**Look for:** empty descriptions, a description that repeats the name ("The title"), a
composition described by what it is rather than what it adds.
**Why:** descriptions are the only documentation an editor sees, and the type description is what
appears in the create dialog when they choose what to make.

## Type descriptions

One sentence, written for an editor choosing a type.

| Kind | Pattern | Example |
|---|---|---|
| Page | What the page is for | "A single article, news item or story." |
| Listing page | What it lists | "Lists articles in a feed, newest first." |
| Composition | Starts with "Adds ..." | "Adds meta title, meta description and indexing settings for this page." |
| Element | What the block shows | "A set of expandable panels, each with a title and content." |
| Child element | Its place in the parent | "One expandable panel inside an Accordion." |
| Data item or folder | What it holds | "Holds the authors that articles can pick from." |

A composition is always "Adds ...", because that is how it reads in the list of compositions on
a page type.

## Property descriptions

Say what an editor cannot work out from the label. Cover whichever of these apply, in this order:

1. **An example or the expected format**: "Full URL, for example `https://example.com/page`."
2. **The fallback**: "Falls back to the page title if this is not set."
3. **The default**: "On by default."
4. **A limit**, when the data type enforces one: "Limited to 160 characters."

Leave a description empty only when the label is genuinely complete (`Title` on a block). Do not
restate the label.

Descriptions are part of the schema, so they go in the requirements doc and are applied with
everything else. The MCP create tools do not set property descriptions; they are written in the
fix-up pass that `umbraco-content-requirements-documentation` describes.

**Related:** [Property aliases](property-aliases.md).
