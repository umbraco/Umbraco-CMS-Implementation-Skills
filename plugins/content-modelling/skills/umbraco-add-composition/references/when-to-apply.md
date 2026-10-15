# When to make a composition, and where to apply it

## Make one, or not

| Situation | Do |
|---|---|
| Two or more types need the same fields, or will soon | Make a composition; see [when to make one](../../umbraco-content-model-conventions/references/compositions.md#when-to-make-one) |
| One type needs the fields and nothing else will | Keep them on that type (the same rule). Say why; the user can revisit when a second type appears |
| The site already has a composition for this concern | Reuse it, or extend it (an Update of that composition), never a second one |
| The fields are block styling (background colour, anchor, spacing) | Not this skill: a settings composition for a block settings model belongs to [umbraco-add-block](../../umbraco-add-block/SKILL.md), or to [umbraco-add-element-type](../../umbraco-add-element-type/SKILL.md) for a settings composition on its own |
| The request is for a composition that includes another composition | Refuse the nesting. Make the new concern its own flat composition and apply both side by side |

The rules behind these are in
[compositions.md](../../umbraco-content-model-conventions/references/compositions.md).

## Which types take it

Apply a composition selectively, by what each type is. Read every candidate type from the site
first; the table is the default, the project can differ.

| Type | Page details | SEO | Sharing | Tags | Visibility |
|---|---|---|---|---|---|
| Home page | Yes | Yes | Yes | No | No |
| Content page | Yes | Yes | Yes | If the site tags content | Yes |
| Listing page | Yes | Yes | Yes | No | Yes |
| Item page (article, event) | Yes | Yes | Yes | If the site tags content | Yes |
| Programmatic page (search, sitemap) | Only if editors set a heading | Yes, often with indexing off | Rarely | No | Yes |
| Data folder or data item | No | No | No | Only if items are tagged | No |
| Site settings | No | No | No | No | No |
| Element type or settings model | Never | Never | Never | Never | Never |
| Another composition | Never | Never | Never | Never | Never |

Data items and site settings are not pages (no template), so search and sharing fields on them are
never read.
Element types never take page compositions; blocks are styled through settings compositions.

When the user says "every page", read that as every **page type** (every type with a template) and
list them by name in the requirements doc, so the user approves the exact list. Do not add types
created later automatically: a page added later takes the SEO composition as it is created, because
[umbraco-add-page](../../umbraco-add-page/SKILL.md) makes SEO mandatory.

## What blocks a type from taking it

Check each target before writing it into the requirements doc.

| Blocker | How to find it | What to do |
|---|---|---|
| **Alias clash.** The type, or a composition it already has, defines a property with one of the new composition's aliases | Read the type with `get-document-type-by-id` and each composition it has; compare aliases | Leave that type out of this change and say why. See below |
| **Nesting.** The target is itself a composition | Its folder is `Compositions/`, or it is used by other types | Never apply it; compositions stay flat |
| **Tab sort disagreement.** The target already has a tab of the same name with a different sort | Compare container sorts on the read-back | Report it. The global sort is right; fixing the target's tab is a separate Update |

If the connected tool list includes `get-document-type-available-compositions`, ask it which
compositions a type can take before writing that type into the requirements doc, and trust its
answer over your own comparison.

### Moving existing fields into a composition

A user often wants to "move" fields that already sit on several page types into a new
composition. Umbraco stores the values against the property on each type. Deleting those
properties to make room for the composition **deletes every stored value**, and the composition's
properties start empty.

So do not plan to remove properties from existing types. Instead:

1. Say plainly that moving the fields loses their content unless the values are migrated first,
   and that a content migration is code work outside this schema change.
2. Offer what can be done safely now: create the composition and apply it only to types that do
   not have the clashing fields (usually new types), leaving the existing fields where they are.
3. Record the rest in the changeset summary as a follow-up that needs a migration.

**Related:** [standard-compositions.md](standard-compositions.md), [workflow.md](workflow.md).
