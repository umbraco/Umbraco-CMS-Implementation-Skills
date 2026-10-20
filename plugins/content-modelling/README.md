# Content modelling skills: a guide

This plugin teaches Claude to model content in Umbraco 17+ the way an experienced Umbraco
developer would: consistent names, a tidy Document Types tree, compositions that do one job,
blocks with separate settings, and every page hung under the right parent with its template and
its SEO fields.

Every skill works the same way. **Claude writes the change down as a markdown requirements doc,
stops for your approval, applies it, then reads it back.** Nothing touches your site until you
have seen and approved the requirements doc.

## The concepts

One skill per concept. A template is what makes a page; everything else is schema that pages,
blocks and block areas are built from.

The definitions of the six concepts (page, document type, element type, block, composition and
block area), with the skill that owns each, are in the
[terms table](skills/umbraco-content-model-conventions/SKILL.md#terms).

## The skills at a glance

| Skill | What you ask for | What it produces |
|---|---|---|
| [`umbraco-content-model-conventions`](skills/umbraco-content-model-conventions/SKILL.md) | "What should I call this?", "Where does this go?" | Answers only. The rule book every other skill links into |
| [`umbraco-content-requirements-documentation`](skills/umbraco-content-requirements-documentation/SKILL.md) | "Write a schema requirements doc", "Apply the approved changeset" | The requirements doc format, the approval lifecycle, apply through the MCP or a manual walkthrough, verify, and a linter |
| [`umbraco-add-page`](skills/umbraco-add-page/SKILL.md) | "Add a contact page", "Add a landing page with a testimonial block" | One page: document type, template, SEO composition and parent, plus any block, block area or data type it needs, in one changeset with one approval |
| [`umbraco-add-document-type`](skills/umbraco-add-document-type/SKILL.md) | "Add a document type for offices" | The schema of one document type in the folder its role calls for, never a template. Other skills use it for a page's schema |
| [`umbraco-add-composition`](skills/umbraco-add-composition/SKILL.md) | "Add SEO fields to every page" | One single-concern composition, applied to the types that need it |
| [`umbraco-add-element-type`](skills/umbraco-add-element-type/SKILL.md) | "Add a testimonial element", "Add a spacing settings composition" | One element type: a content element, a repeater item, a settings model or a settings composition |
| [`umbraco-add-block`](skills/umbraco-add-block/SKILL.md) | "Add an accordion block" | A block: its content element, with a settings element and settings compositions when it has style options, and repeater items, placed in an existing block area if you name one |
| [`umbraco-add-data-type`](skills/umbraco-add-data-type/SKILL.md) | "Limit this to 160 characters", "Which editor should I use?" | Reuse or a new data type, the right property editor, name and folder |
| [`umbraco-configure-block-editor`](skills/umbraco-configure-block-editor/SKILL.md) | "Make the block available on the page" | A Block Grid per placement, or a Block List per repeater |
| [`umbraco-add-listing-page`](skills/umbraco-add-listing-page/SKILL.md) | "Add a blog", "Add a news section" | The listing and item page types as a pair, with a collection view |
| [`umbraco-add-data-folder`](skills/umbraco-add-data-folder/SKILL.md) | "Add authors", "Add categories" | A data folder and item at the content root, and the picker pages use |
| [`umbraco-add-site-settings`](skills/umbraco-add-site-settings/SKILL.md) | "Where do the logo and footer links go?" | The `siteSettings` singleton with tabs 0 to 9 |
| [`umbraco-change-document-type`](skills/umbraco-change-document-type/SKILL.md) | "Add a field to the article page", "Make the summary mandatory" | An Update of a type that exists, saying which changes lose content |
| [`umbraco-review-content-model`](skills/umbraco-review-content-model/SKILL.md) | "Audit our content model" | A report of what departs from the conventions, ranked, with the skill that fixes each. Changes nothing |

## How the skills fit together

Two skills are foundations. **Conventions** holds the rules and **requirements documentation**
holds the process. The builder skills hold only the decisions specific to what they build, and
link into the two foundations for everything else. That is why all of them must be installed
together.

```mermaid
flowchart TB
    subgraph Foundations
        CONV["umbraco-content-model-conventions<br/><i>the rules</i>"]
        REQ["umbraco-content-requirements-documentation<br/><i>requirements doc format, approve, apply, verify, linter</i>"]
    end

    subgraph Builders
        PAGE[umbraco-add-page]
        DOC[umbraco-add-document-type]
        COMP[umbraco-add-composition]
        ELEM[umbraco-add-element-type]
        BLOCK[umbraco-add-block]
        DT[umbraco-add-data-type]
        AREA[umbraco-configure-block-editor]
        LIST[umbraco-add-listing-page]
        DATA[umbraco-add-data-folder]
        SET[umbraco-add-site-settings]
        CHANGE[umbraco-change-document-type]
    end

    REVIEW["umbraco-review-content-model<br/><i>audit, report only</i>"]

    Builders -- "what is the rule?" --> CONV
    Builders -- "write, approve, apply, verify" --> REQ
    REQ -- "names, sorts, folders" --> CONV
    REVIEW -- "checks against" --> CONV
    REVIEW -- "lints requirements folders with" --> REQ
    REVIEW -. "each finding fixed by" .-> Builders
```

The skills are peers: each works on its own, and `umbraco-add-page` calls the others to bring
everything a page needs into one changeset with one approval. The peers decide and write; the
page skill merges and applies, leaves first.

```mermaid
flowchart LR
    PAGE[add-page] -- "the page's schema" --> DOC[add-document-type]
    PAGE -- "SEO composition, if the site has none" --> COMP[add-composition]
    PAGE -- "a block the page needs" --> BLOCK[add-block]
    PAGE -- "the page's block area" --> AREA[configure-block-editor]
    PAGE -- "a field's data type" --> DT[add-data-type]
    BLOCK -- "each element of the block" --> ELEM[add-element-type]
    BLOCK -- "place it in an existing area" --> AREA
    LIST[add-listing-page] -- "once per page" --> PAGE
    DATA[add-data-folder] -- "folder and item schema" --> DOC
    SET[add-site-settings] -- "settings schema" --> DOC
```

The builders also hand off to each other. A request outside a builder's scope is passed to the
skill that owns it, not half-built:

```mermaid
flowchart LR
    PAGE[add-page] -- "a listing pair" --> LIST[add-listing-page]
    PAGE -- "no template, visitors never land on it" --> DOC[add-document-type]
    DOC -- "it is a page after all" --> PAGE
    COMP -- "block styling fields" --> BLOCK[add-block]
    ELEM[add-element-type] -- "content plus settings" --> BLOCK
    BLOCK -- "a new block area" --> AREA[configure-block-editor]
    ELEM -- "a colour palette or dropdown" --> DT[add-data-type]
    DT -- "a Block List or Block Grid" --> AREA
    DT -- "a collection view" --> LIST
    LIST -- "authors or categories" --> DATA[add-data-folder]
    COMP[add-composition] -- "settings on the home page" --> SET[add-site-settings]
```

## One change, start to finish

Every builder follows the same six steps. Four of them belong to the requirements documentation
skill.

```mermaid
flowchart LR
    A[1. Inspect<br/>read the site] --> B[2. Decide<br/>apply the conventions]
    B --> C[3. Write the requirements doc<br/>and lint it]
    C --> D{4. Approval}
    D -- "changes requested" --> C
    D -- "approved" --> E[5. Apply<br/>MCP or manual, leaves first]
    E --> F[6. Verify<br/>read it back]
    F -- "differs" --> G[Apply log:<br/>tell the user]
    F -- "matches" --> H[Status: applied]
```

Each requirements page carries a status line that records where it is in that journey. A status
is a claim about the site, so `applied` is only written after the artefact has been read back, or
after you confirm you did the manual steps.

```mermaid
stateDiagram-v2
    [*] --> proposed: Claude writes the requirements doc
    proposed --> proposed: you ask for changes
    proposed --> approved: you approve the changeset
    approved --> applied_via_MCP: created through the MCP and read back
    approved --> applied_manually: you confirm the backoffice steps
    approved --> approved: apply failed or the site differs<br/>(recorded in the Apply log)
    applied_via_MCP --> proposed: a later change edits this page
    applied_manually --> proposed: a later change edits this page
```

### With the MCP, or without it

There is one approach with two mechanisms. If the Umbraco Developer MCP is connected, Claude
applies the requirements doc itself. If not, Claude hands you a walkthrough generated from the
same requirements doc. There is never uSync, Deploy or `package.xml` output.

```mermaid
sequenceDiagram
    actor You
    participant C as Claude
    participant S as Builder skill
    participant RQ as Requirements documentation skill
    participant MCP as Umbraco Developer MCP
    participant U as Umbraco site

    You->>C: "Add a contact page"
    C->>S: load umbraco-add-page
    S->>RQ: how do I inspect?
    C->>MCP: read tools (types, templates, languages)
    MCP->>U: GET schema
    U-->>MCP: document types, templates
    MCP-->>C: what exists
    C->>C: decide: contactPage, Content tab 100, SEO composition,<br/>child of homePage, template under master
    C->>C: write requirements pages + changeset, run lint-requirements.mjs
    C-->>You: changeset summary: "approve?"
    Note over You,C: Claude stops here. "Just do it" is not approval.
    You->>C: "Approved"
    alt MCP connected
        C->>MCP: create-template, create-document-type
        C->>MCP: fix-up: get-document-type-by-id, then update-document-type (whole body)
        C->>MCP: read back, compare with the requirements doc
        MCP-->>C: matches
        C-->>You: applied via MCP, here is what was verified
    else MCP not connected
        C-->>You: numbered backoffice walkthrough
        You->>U: follow the steps in the backoffice
        You->>C: "Done"
        C-->>You: applied manually (on your word, not read back)
    end
```

The **fix-up pass** exists because the MCP's create tools cannot set sort orders, mandatory flags,
descriptions, templates or culture variation. Claude creates the type, reads it back, edits the
body and writes the whole thing again, then reads it once more to confirm no property was lost.

## Building a site from empty

Here is the order the skills would run in to take a fresh Umbraco 17 install to a modelled site
with a home page, content pages, a blog, authors, blocks and site settings. Each step is its own
changeset and its own approval. Because every page takes SEO, the first page brings the SEO
composition with it.

```mermaid
flowchart TD
    START([Fresh Umbraco 17 install]) --> S1

    S1["1. umbraco-add-page<br/>Home Page + master template + SEO Composition"]
    S2["2. umbraco-add-composition<br/>Page Details, Open Graph"]
    S3["3. umbraco-add-page<br/>Content Page, allowed under Home"]
    S4["4. umbraco-add-site-settings<br/>logo, navigation, footer, social"]
    S5["5. umbraco-add-data-folder<br/>Author Folder + Author + Author Picker"]
    S6["6. umbraco-add-block<br/>Rich Text, Accordion + Accordion Item,<br/>shared settings models"]
    S7["7. umbraco-configure-block-editor<br/>Main Content Block Grid (approach A)"]
    S8["8. umbraco-add-listing-page<br/>Blog Listing Page + Blog Page + collection view"]
    S9["9. umbraco-review-content-model<br/>audit the result"]

    S1 --> S2 --> S3 --> S4 --> S5 --> S6 --> S7 --> S8 --> S9
    S9 --> DONE([A modelled site, documented in docs/umbraco-schema/])

    S2 -. "constraints such as<br/>160-character meta description" .-> DT1[umbraco-add-data-type]
    S6 -. "each element" .-> ELEM1[umbraco-add-element-type]
    S6 -. "repeater Block List<br/>(approach B)" .-> S7
    S8 -. "author picker on the post" .-> S5
```

The order matters for the same reason the apply order inside a changeset matters: each artefact
needs the ids of the ones before it. Compositions exist before the pages that take them, elements
before the grid that registers them, the item type before the listing that allows it. A page that
needs a new block can take it in the same changeset: `umbraco-add-page` orders the block, the
grid update, the template and the page leaves first and asks for one approval.

### What a session looks like

A conversation building the blog section, showing where each skill comes in and where you are
asked to decide:

```mermaid
sequenceDiagram
    actor You
    participant C as Claude
    participant LIST as add-listing-page
    participant PAGE as add-page
    participant DATA as add-data-folder
    participant DT as add-data-type
    participant REQ as requirements documentation
    participant CONV as conventions

    You->>C: "Add a blog. Posts have an author, a date and a body."
    C->>LIST: listing pair
    LIST->>CONV: names: blogListingPage + blogPage
    LIST->>CONV: allowed children: listing allows item + itself
    C->>C: inspect: is there an Author type?
    C-->>You: "No authors exist. Add them first?"
    You->>C: "Yes"
    C->>DATA: authorFolder + author + Author Picker
    DATA->>DT: picker data type, max 1
    DATA->>REQ: write requirements doc, changeset 1
    C-->>You: approve changeset 1?
    You->>C: approved
    C->>REQ: apply + verify changeset 1
    C->>LIST: continue the blog
    LIST->>PAGE: blogPage, then blogListingPage (template, SEO, parent)
    LIST->>DT: publish date editor (Umbraco.DateOnly)
    LIST->>REQ: collection view, two templates,<br/>two page types, Home Page update
    C-->>You: approve changeset 2?
    You->>C: approved
    C->>REQ: apply + verify changeset 2
    C-->>You: what was created, what was read back,<br/>what's left (the Razor views)
```

## What you end up with

The end goal is not only a modelled site. It is a site whose schema is **written down**. Every
change leaves requirements pages in your project, in a folder that mirrors the backoffice trees:

```
docs/umbraco-schema/
    README.md                         records where requirements docs live
    _changesets/
        2026-10-01-home-and-pages.md
        2026-10-02-blog.md            what was approved, the apply order, a tick-list, the apply log
    Data-Types/
        Content-Picker/Author-Picker.md
        Block-Grid/Main-Content-Block-Grid.md
        Collection-View/Blog-Listing-Page-Collection-View.md
    Document-Types/
        HomePage.md, ContentPage.md, BlogListingPage.md, BlogPage.md, SiteSettings.md
        Compositions/SeoComposition.md, PageDetailsComposition.md, OpenGraphComposition.md
        Data/AuthorFolder.md, Author.md
        Elements/Accordion.md, AccordionItem.md, RichText.md
        Elements/Settings/BlockSettings.md
    Templates/
        Master.md
        Master/HomePage.md, ContentPage.md, BlogListingPage.md, BlogPage.md
```

And on the site, a Document Types tree that anyone can read at a glance:

```mermaid
flowchart LR
    DT[Document Types] --> C[Compositions/]
    DT --> D[Data/]
    DT --> E[Elements/]
    DT --> P["blogListingPage<br/>blogPage<br/>contentPage<br/>homePage<br/>siteSettings"]
    C --> C1["openGraphComposition<br/>pageDetailsComposition<br/>seoComposition"]
    D --> D1["author<br/>authorFolder"]
    E --> E2[Compositions/] --> E21["backgroundColourSettingsComposition<br/>anchorSettingsComposition"]
    E --> E3[Settings/] --> E31["blockSettings<br/>titledBlockSettings"]
    E --> E1["accordion<br/>accordionItem<br/>richText"]
```

The content tree follows from the allowed-children rules: only the home page, site settings and
data folders sit at the root.

```mermaid
flowchart TD
    ROOT((Content)) --> HOME[Home Page]
    ROOT --> SETTINGS[Site Settings]
    ROOT --> AUTHORS[Authors folder]
    AUTHORS --> A1[Author]
    HOME --> CP[Content Page]
    CP --> CP2[Content Page]
    HOME --> BL[Blog Listing Page]
    BL --> BP[Blog Page]
    BL --> BL2[Blog Listing Page<br/>e.g. a year]
    BL2 --> BP2[Blog Page]
```

## Auditing an existing site

The review skill runs the other way: it reads a model and reports. It reads from the live site
through the MCP, from a requirements folder (running the linter first), or from an export you give
it, and it says which source it used. It never fixes anything. Each finding names the builder
skill that would fix it as a separate, approved change.

```mermaid
flowchart LR
    SRC{Source} -- "MCP connected" --> LIVE[Read every type<br/>and data type]
    SRC -- "requirements folder" --> LINT[Run lint-requirements.mjs]
    SRC -- "export or description" --> GIVEN[Review what was given]
    LIVE --> CHECKS
    LINT --> CHECKS
    GIVEN --> CHECKS
    CHECKS["Checks: naming, structure, sorts,<br/>data types, descriptions, compositions"] --> RANK[Rank: High / Medium / Low<br/>project conventions reported once]
    RANK --> REPORT[docs/content-model-review-date.md]
    REPORT -. "each fix, later, with approval" .-> FIX[the named add-* skill]
```

## What these skills will not do

- **Write code.** Razor views, controllers, block partials and content migrations are implementation
  work. The skills say when one is needed.
- **Change the site without approval.** "Just do it" is a request for the change, not approval of a
  requirements doc you have not seen.
- **Delete content to make room.** Moving fields into a composition, changing a property's editor or
  moving settings off the home page would lose stored values. The skills stop and say a migration is
  needed.
- **Leave a page without SEO.** Every page takes the SEO composition; if the site has none, the first
  page adds it in the same changeset. Sharing and page-details fields stay optional.
- **Claim more than they checked.** A create call that returned without an error is not proof. The
  report always separates what was read back, what you confirmed, and what was not checked (an icon
  colour, for one, cannot be validated).
