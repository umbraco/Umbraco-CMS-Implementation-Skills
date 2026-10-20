# Stage 1 — Review the skill

## Get the context first

```bash
gh pr view <n> --json title,body,author,headRefName,headRefOid,isCrossRepository,maintainerCanModify,files
gh pr view <n> --comments                                      # earlier reviews and replies
gh api repos/umbraco/Umbraco-CMS-Implementation-Skills/pulls/<n>/comments   # inline threads
```

Read the earlier reviews before your own: don't re-raise something already answered, and check
the fixes the author says they made actually landed. Points left unanswered on the current head are
findings: verify each against the source and fix the ones that hold. An earlier review can be wrong
too; if the source disproves one, say so in your summary with the source line. The PR description
usually states the coverage tier, the eval results and what isn't covered. Each of those is a claim
to check, not a fact.

**Check the PR still adds something.** `gh pr view --json files` lists the PR's diff from its own
merge base, so a PR that is already on `main` (a duplicate of a merged one, or work that landed
another way) still looks full. Compare against `main` itself: `git diff --stat origin/main...HEAD`
for what the PR changes, and `git diff origin/main HEAD -- plugins/` for what would still differ
after merging. If nothing would, there's nothing to fix or push: comment saying so (naming the PR or
commit it duplicates), label it `ai-review-done`, and leave closing it to a human (on #52 it was #23's
exact head).

## Scope

Review the **skill**: `SKILL.md`, `references/`, `assets/`, `scripts/`, `evals/`. For the gate
scaffolding (`examples/`, fixtures, `.generate.json`, solution and host project wiring), only check
that it builds and the gate passes ([verify.md](verify.md)); known scaffold issues are tracked
separately, so don't review its design unless asked. **A failing gate is always in scope, whatever
causes it**: the PR can't land red, so fix the cause (on #50 it was a `requires` entry pointing at a
page the Delivery API hides from anonymous requests).

**Overlap with a skill already on `main`** (two skills triggering on the same request) is a scope
call: report it for the human, and don't move triggers between skills yourself.

## What to check

### 1. Conventions

Walk [the conformance checklist](../../umbraco-skill-author/references/conformance-checklist.md)
against the actual files. The ones PRs most often miss:

- the SKILL.md doesn't tell the agent to open the reference and start from its asset **before
  writing code**;
- a coverage tier that isn't true (e.g. **Documented** for an approach that ships code);
- a doc link duplicated between SKILL.md and a reference;
- expectations that accept only one implementation, or reward an answer the skill itself advises
  against;
- a committed eval workspace (`eval-results/`, `<skill>-workspace/`: transcripts, outputs,
  grading). The evaluator says to delete it before a PR, so remove it with `git rm -r`
  (`review-queue.mjs` flags these), even when the author kept it on purpose; say in the summary that
  the branch history still has it at the commit before yours.

A skill whose approaches are all **Documented** (it ships no code, like `umbraco-common-pitfalls`)
fails the checklist's "at least one approach is Asserted" line. That's a policy call, not a fix:
leave it for the human in your report.

Compare the structure with a merged sibling (`umbraco-sitemap` is the golden standard).

### 2. Every Umbraco claim, against the source

This is the most valuable step, and the one CI can't do: AI-written skills state API behaviour
confidently and sometimes wrongly. For each claim about Umbraco's behaviour (a default, an order,
a lifetime, what throws and when, what's obsolete, what a type is registered as), find the line in
the source that proves or disproves it, and note the file and line.

Check against the version the repo pins (`Umbraco.Cms` in `Directory.Packages.props`), in this order:

1. **A local clone of `umbraco/Umbraco-CMS`.** Ask the user where theirs is; don't guess. Read the
   pinned tag **without changing its checkout** (someone else may be using it):

   ```bash
   T=release-<version>
   git -C <clone> grep -n 'GetWebProfiler' "$T" -- src     # search the pinned source
   git -C <clone> show "${T}:src/Umbraco.Core/Logging/IProfiler.cs"   # read one file at the tag
   git -C <clone> tag --contains <commit> | grep -E '^release-'      # "since version X" claims
   git -C <clone> log -S 'GetHttpClient' --oneline -- src            # find the commit that added it
   ```

   Quote `"${T}:path"` exactly like that: in zsh, `$T:src` reads `:s` as a substitution modifier.
2. **The restored NuGet packages** (`~/.nuget/packages/umbraco.cms.*/<version>/lib/*/`): the
   assemblies and their XML docs prove a type or member exists, but not how it behaves.
3. **GitHub, at the tag**: `gh api 'repos/umbraco/Umbraco-CMS/contents/<path>?ref=release-<version>' --jq .content | base64 -d`.

Claims of the form "new in X" or "since X" need the version they arrived in, not just the pin: an
API can be in a newer 17.x and missing from the pinned 17.5.3 (on #47, `GetHttpClient()` was
described as new in 18 but arrived in 17.7.0).

A claim the source can't settle (what a page returns, what a header does) can be proved at runtime:
borrow a fixture, boot it, and probe it. On #37 a booted site and a CORS preflight, against a control
boot without the setting, proved the skill wrong where reading the code had been ambiguous.

Behaviour needs the source, not the docs: the docs are what the skill is correcting.

### 3. Docs corrections

For each row in a *Docs corrections* table, fetch the live `.md` page and confirm the docs really
say what the row claims, and that the correction is right in substance. A correction can overstate
the problem: an example that's correct for its own intent (an allow-list, say) isn't a docs error,
even if it's the wrong tool for the skill's case.

### 4. Links and siblings

- Every doc link resolves, and fetch-me links use the `.md` page. A status code isn't enough:
  docs.umbraco.com answers **200** with a "Page Not Found" page for a dead link, so check the body too:

  ```bash
  curl -sL "<url>" | head -c 2000 | grep -qi 'page not found' && echo "DEAD: <url>"
  ```
- Relative links to sibling skills point at files that exist on `main`.
- The skill doesn't contradict a merged sibling on the same topic (grep the other skills for the
  APIs it names).

### 5. Evals against the skill

- Each expectation matches what the skill now teaches: if the skill calls something "usually too
  early", an expectation that accepts it should say when it's right, and the skill should too.
- Expectations are checkable from the run's files, `build.txt` and `boot.txt`/`probe.json`.
- Fixtures build, and boot if the evals rely on a running site ([verify.md](verify.md)).

## Write the findings down before fixing

One line each: severity (high, medium, low, nit), file and line, what's wrong, and the evidence (the
source line, the docs quote, the failing command). Findings with no evidence are opinions: either
find the evidence or drop them. This list becomes your fix plan and the bullets of the summary.
