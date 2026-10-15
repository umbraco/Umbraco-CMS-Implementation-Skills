# Apply manually in the backoffice

Use this when the Umbraco Developer MCP is not connected, the site is not reachable, or the MCP
rejected a value (see [apply-via-mcp.md](apply-via-mcp.md#values-the-mcp-rejects)). The requirements
doc is the same; only the hands change. You write the walkthrough, the user carries it out.

Missing MCP tools are not a reason to skip the requirements doc, to output uSync or package files,
or to claim the schema exists.

## Write the walkthrough

Generate one numbered list from the approved changeset, in the order of its checklist (the order in
[apply-via-mcp.md](apply-via-mcp.md#order)). Every value comes from the requirements pages; do not
paraphrase an alias or a sort.

Put it in the changeset under `## Apply log`, or hand it to the user in the conversation, so they
can tick items off. Write each step so it can be followed without opening the requirements doc.

| Artefact | Where in the backoffice | What each step must state |
|---|---|---|
| Data type folder | Settings, Data Types, Create, Folder | The folder name |
| Data type | Settings, Data Types, in its folder, Create | Name, property editor, every Configuration value |
| Block List or Block Grid | As a data type | Each block: content element, settings element, at root, in areas; the amount or grid columns |
| Document type folder | Settings, Document Types, Create, Folder | The folder name and its parent |
| Composition, page type, data folder or item | Settings, Document Types, in its folder, Create | Name, **alias typed by hand**, icon and colour, description |
| Element type | Create, Element Type | As above; created directly in `Elements/` |
| Tabs and groups | Design tab of the type | Each tab or group name and its sort order. Sort orders are edited with the reorder button |
| Property | In its tab or group | Name, alias, data type, mandatory, sort, description |
| Compositions | Design, Compositions | Which to tick |
| Template | Settings, Templates; create page templates under the master | Name, alias, master |
| Default and allowed templates | Templates tab of the type | The default template |
| Allowed at root, allowed children, collection | Structure tab of the type | Exactly the values in Definition |
| Vary by culture | Settings tab of the type, then each property's settings | Yes or No on the type; on every property too, except those the page lists as `Invariant` |

Two things the backoffice will get wrong unless the step says so:

- **The alias.** Umbraco generates one from the name and leaves acronyms in capitals. State the
  alias and tell the user to unlock the field and type it.
- **Sort orders.** New tabs and properties are numbered 0, 1, 2. State every sort from the
  requirements doc.

## Asked for the steps before approval

Without the MCP, a user often asks for the backoffice steps in the same breath as the change.
Give the walkthrough in the same reply, headed as steps to follow **once the changeset is
approved**, and still stop for approval. Writing the walkthrough is not applying it: every status
stays `proposed`, and nothing is ticked until the user says they have done it.

## After the user has done it

Ask the user to confirm which steps they completed, and tick those. Then verify as far as you
can; see [verify.md](verify.md). Without the MCP you cannot read the site, so the status is
`applied <date> manually` on the user's confirmation, and your summary must say the result was
confirmed by the user and not read back.

If the user has not carried out the steps yet, the status stays `approved`. A walkthrough being
written is not the schema being applied.
