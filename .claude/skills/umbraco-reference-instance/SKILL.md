---
name: umbraco-reference-instance
description: >
  Boot the committed reference Umbraco site and prove that a skill's produced code actually
  runs. Use this whenever you want to run/validate an implementation or content-modelling
  skill against a real, running Umbraco instance — e.g. "test the sitemap skill in Umbraco",
  "does this skill's code actually work", "boot the reference site", "run the instance",
  "validate <skill> end-to-end". This is the runtime counterpart to `umbraco-skill-evaluator`:
  the evaluator grades whether Claude *writes* the right code; this skill proves the code
  *builds and serves* in Umbraco. Authoring/maintainer tooling — not shipped in any plugin.
---

# Umbraco reference instance

`Umbraco-CMS.Skills/` (at the repo root, with `Umbraco-CMS.Skills.sln`) is a real Umbraco
**17** web project used only for validating skills. It:

- targets `net10.0`, `Umbraco.Cms 17.5.3` (scaffolded via the `psw` CLI; versions live in
  `Umbraco-CMS.Skills/Directory.Packages.props`) — matching the skills' "Umbraco 17+" target;
- installs **unattended** on first boot into a SQLite DB (`admin@example.com` / `1234567890`);
- ships the **Clean** starter kit, so there is real content (Document Types, templates,
  published pages) for skill output to run against — `/sitemap.xml` has URLs to emit, a
  missing page has a site to render a 404 into, the Delivery API returns real nodes.

The runtime DB and build output are `.gitignore`d — only the project scaffolding is
committed, and Clean re-installs on first boot. **Never commit** a populated `App_Data`
SQLite DB, `bin/`, `obj/`, the `Umbraco.Skills.Sandbox/` scratch project, or
`.local-nuget-feed/`.

## Environment conventions

Same variable names as the backoffice test-runner, so tooling is interchangeable:

| Variable | Default | Meaning |
|---|---|---|
| `UMBRACO_URL` | `https://localhost:44372` | Base URL the instance binds to (also `http://localhost:60372`). |
| `UMBRACO_USER_LOGIN` | `admin@example.com` | Unattended admin email. |
| `UMBRACO_USER_PASSWORD` | `1234567890` | Unattended admin password. |

The scripts read these from the environment and fall back to the defaults above.

## Just boot the site

```bash
.claude/skills/umbraco-reference-instance/scripts/instance.sh boot    # start + wait until ready
.claude/skills/umbraco-reference-instance/scripts/instance.sh status  # is it up?
.claude/skills/umbraco-reference-instance/scripts/instance.sh stop     # stop only if this script started it
```

`boot` is idempotent: if the instance is already answering on `UMBRACO_URL` it reuses it and
does not start a second one. First boot is slow (unattended install + Clean import) — the
script polls for up to ~4 minutes.

Log in to the backoffice at `<UMBRACO_URL>/umbraco` with the credentials above. For
clicking through the backoffice, use the `umbraco-chrome-navigation` skill.

## Find out what content the instance actually has

Do this **before** writing an example or its assertions. Skill assets routinely navigate by
Document Type alias (`FirstChildOfType("errorPage")`, "first child of the site root"), and such
code only proves something if the alias it looks for exists in a shape it can reach — guessing
produces either a mysteriously null lookup or, worse, a test that passes for the wrong reason.

Boot the instance and inspect it with the **Umbraco Developer MCP's CLI**, which reads the content
tree and Document Types over the versioned Management API. Use `--call`, not the MCP tools through
a chat session: same data, but a command you can re-run and paste into a commit message. It needs
the API user from above; credentials come from `.mcp.json`.

```bash
MCP="npx @umbraco-cms/mcp-dev@lts-17 --umbraco-base-url https://localhost:44372 --umbraco-readonly"
export NODE_TLS_REJECT_UNAUTHORIZED=0 UMBRACO_CLIENT_ID=umbraco-back-office-mcp UMBRACO_CLIENT_SECRET=1234567890

$MCP --list-tools                                             # what's available
$MCP --call get-document-root      --call-args '{}'            # site root + published state
$MCP --call get-document-children  --call-args '{"parentId":"<root-id>"}'
$MCP --call get-document-type-by-id --call-args '{"id":"<type-id>"}'   # ...and its ALIAS
```

`--umbraco-readonly` keeps an inspection from mutating the instance. Note the last call is not
optional: the API identifies types by **key and display name, not alias**, so the document and
document-type listings give you a GUID and a name but never the alias your C# navigates by. Don't
infer it from the name — Clean's "XML Sitemap" type has the alias `xMLSitemap`.

For a quick look with no auth at all, the Delivery API answers the same question for *published*
content and needs no API user:

```bash
curl -sk "https://localhost:44372/umbraco/delivery/api/v2/content?fetch=children:/&take=100"
```

That returns each child's `contentType` as its **alias** directly, which is why the precondition
tests use it (see below). None of this is needed to run the `dotnet test` gate.

The MCP server is configured in `.mcp.json` at the repo root, pinned to the v17 line
(`@umbraco-cms/mcp-dev@lts-17`) to match the instance. It authenticates as an **API user**, which
the instance doesn't have until you create one:

```bash
.claude/skills/umbraco-reference-instance/scripts/instance.sh boot
.claude/skills/umbraco-reference-instance/scripts/instance.sh api-user
```

`api-user` wraps `scripts/create-api-user.mjs`: it logs in as the unattended admin, gets a token
via the Swagger OAuth client, and creates an API user holding the client credentials `.mcp.json`
expects. Idempotent — re-running it when the credentials already authenticate just exits. The
script is vendored from the MCP repo's **v17** branch because it ships in git only, not in the npm
package; its header explains why the branch matters. The credentials are fixed local dev values
for a throwaway instance, so **never point this config at a real site**.

Both the MCP and this script talk HTTPS to the ASP.NET dev certificate, which Node won't trust —
hence `NODE_TLS_REJECT_UNAUTHORIZED=0` in `.mcp.json`, and the same exemption inside `api-user`
(scoped to localhost only). Claude Code reads `.mcp.json` at startup and asks you to approve the
server, so a **new session** is needed before the Umbraco tools appear.

Don't reach into the SQLite database for this. It works, but it couples to Umbraco's internal
schema (`nodeObjectType` GUIDs, `cmsDocumentType`, the `umbracoContent`/`cmsContentType` join) —
private, unversioned, and liable to change between majors, failing confusingly rather than
loudly when it does.

## Validate a skill deterministically (the CI gate)

Runtime validation is a **`dotnet test` gate — no LLM, reproducible pass/fail**. Each validated
approach ships an `examples/<approach>/` project; the skill's `assets/` are projected into it at build
time with placeholders substituted, a reference host references it, and that host's test project boots
in-process (`WebApplicationFactory`) and asserts the skill's endpoints over HTTP.

There are **two** hosts — `Umbraco-CMS.Skills` (Clean) for approaches written as C# that registers
into DI, and `Umbraco-CMS.Skills.Blank` (no starter kit) for approaches written as Document Types +
templates + config. They are tested as separate processes, because Umbraco's `StaticServiceProvider`
is process-wide static state.

```bash
dotnet build Umbraco-CMS.Skills.sln
dotnet test Umbraco-CMS.Skills.TestHost/Umbraco-CMS.Skills.TestHost.csproj --no-build
dotnet test Umbraco-CMS.Skills.TestHost.Blank/Umbraco-CMS.Skills.TestHost.Blank.csproj --no-build
node scripts/generate-examples.mjs --lint  # every placeholder an asset carries is declared
```

Run the two `dotnet test` commands **separately, exactly as above** — not
`dotnet test Umbraco-CMS.Skills.sln`. `UmbracoHostSentinel` fails loudly if both hosts ever land in
one process, and invoking per project keeps that isolation from resting on a VSTest implementation
detail. CI (`.github/workflows/validate-skills.yml`) invokes them the same way.

**Adding a skill to the gate is an authoring task**, so it's documented where authors work:
[`umbraco-skill-author`'s runtime-validation reference](../umbraco-skill-author/references/runtime-validation.md)
covers the `examples/<approach>/` layout, how `.generate.json` substitutes placeholders and declares
content preconditions, how to write a fixture against the shared host, and how to prove the fixture
can actually fail. It is the single source of truth for those mechanics — this file covers running
and debugging the instance instead.

`assets/` stay the single source of truth, and nothing generated is committed: each example is
projected into its own `obj/` during the build, so what compiles and serves IS what the skill ships
and there is no second copy to drift. Skills whose `assets/` aren't on the current branch are skipped,
so a build is safe pre-merge.

## When a skill's code doesn't compile

Expect this. Umbraco's published-content API changed substantially across v13 → v15 → v17, and
skills written from older docs or tutorials carry APIs that no longer exist. Catching that is the
main thing this gate buys, so treat a red build as the gate working, not as a problem with the
example. Two failure modes cost real time if you don't know them:

**Fix declaration errors before believing the error count.** Roslyn resolves declarations first
and won't report method-body diagnostics while any remain, so an unresolved type (`CS0246` on a
missing `using`) hides every bad call in the file. `umbraco-custom-error-pages` reported 2 errors;
adding the missing usings turned that into 8, and only then did the actually-interesting problems
appear. If a line you're sure is wrong reports nothing, you have an earlier error to clear —
don't conclude the call is fine.

**Check API shape against the version you're compiling, not the source you have open.** The
`Umbraco-CMS` working directory (see AGENTS.md) is on whatever branch it happens to be on, which
may be a different major than `Directory.Packages.props` pins — confirm with
`git -C <umbraco-src> rev-parse --abbrev-ref HEAD`. Then let the compiler answer rather than
grep: add a temporary probe file that calls the API with deliberately wrong arguments, and read
which overload it binds to. That is how the `FirstChild(alias)` bug was pinned down — v17 has no
alias overload, so the alias was silently binding to the `culture` parameter and matching the
first child of any type. **Anything that compiles but binds a string to the wrong parameter is
invisible to a compile check** and will only be caught by an assertion on real content, which is
why the gate asserts behaviour and not just build success.

Where an asset genuinely targets an older floor than the instance (e.g. a skill claiming 16.1+
while the instance is 17), the gate can only prove the 17 end. Compiling the assets in a
throwaway project pinned to the floor version (`Umbraco.Cms.Web.Website 16.1.*`, `net9.0`) checks
the claim, but compile-only — it proves the APIs bind, not that behaviour matches.

## Explore interactively (manual boot)

For poking at a skill by hand, or for the parts a `dotnet test` can't cover (backoffice setup —
e.g. sitemap Approach B's Document Type + content node, driven via `umbraco-chrome-navigation`),
use the manual harness:

```bash
scripts/instance.sh try plugins/implementation/skills/umbraco-sitemap  # materialize assets → sidecar → reference
scripts/instance.sh boot
curl -sk https://localhost:44372/sitemap.xml
scripts/instance.sh reset                                              # restore the committed instance
```

`try` copies **every** `assets/*.cs` into a sidecar library (namespace-substituted) — so for
mutually-exclusive assets, point it at a pruned copy. This path is for exploration; the
`dotnet test` gate above is the source of truth for whether a skill works.

## Relationship to other skills

- `umbraco-skill-evaluator` — grades whether Claude *produces* correct skill output (LLM
  grading). Run it for quality/regression scoring; run **this** skill to prove the output
  compiles and serves.
- `umbraco-chrome-navigation` (backoffice plugin) — drives the running backoffice for the
  browser half of validation.
