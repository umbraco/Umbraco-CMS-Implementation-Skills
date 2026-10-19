# Stage 1 — Review the skill

## Get the context first

```bash
gh pr view <n> --json title,body,author,headRefName,headRefOid,maintainerCanModify,files
gh pr view <n> --comments                                      # earlier reviews and replies
gh api repos/umbraco/Umbraco-CMS-Implementation-Skills/pulls/<n>/comments   # inline threads
```

Read the earlier reviews before your own: don't re-raise something already answered, and check
the fixes the author says they made actually landed. The PR description usually states the coverage
tier, the eval results and what isn't covered. Each of those is a claim to check, not a fact.

## Scope

Review the **skill**: `SKILL.md`, `references/`, `assets/`, `scripts/`, `evals/`. For the gate
scaffolding (`examples/`, fixtures, `.generate.json`, solution and host project wiring), only check
that it builds and the gate passes ([verify.md](verify.md)); known scaffold issues are tracked
separately, so don't review its design unless asked.

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
  grading). The evaluator says to delete it before a PR; remove it from the branch with `git rm -r`
  (`review-queue.mjs` flags these).

Compare the structure with a merged sibling (`umbraco-sitemap` is the golden standard).

### 2. Every Umbraco claim, against the source

This is the most valuable step, and the one CI can't do: AI-written skills state API behaviour
confidently and sometimes wrongly. For each claim about Umbraco's behaviour (a default, an order,
a lifetime, what throws and when, what's obsolete, what a type is registered as), find the line in
the source that proves or disproves it, and note the file and line.

Check against the version the repo pins (`Umbraco.Cms` in `Directory.Packages.props`), in this order:

1. **A local clone of `umbraco/Umbraco-CMS`** at the matching tag (`git checkout release-<version>`).
   Ask the user where theirs is; don't guess. Note if you checked a newer tag than the pin.
2. **The restored NuGet packages** (`~/.nuget/packages/umbraco.cms.*/<version>/lib/*/`): the
   assemblies and their XML docs prove a type or member exists, but not how it behaves.
3. **GitHub, at the tag**: `gh api 'repos/umbraco/Umbraco-CMS/contents/<path>?ref=release-<version>' --jq .content | base64 -d`.

Behaviour needs the source, not the docs: the docs are what the skill is correcting.

### 3. Docs corrections

For each row in a *Docs corrections* table, fetch the live `.md` page and confirm the docs really
say what the row claims, and that the correction is right in substance. A correction can overstate
the problem: an example that's correct for its own intent (an allow-list, say) isn't a docs error,
even if it's the wrong tool for the skill's case.

### 4. Links and siblings

- Every doc link resolves (`curl -s -o /dev/null -w '%{http_code}' -L <url>`), and fetch-me links
  use the `.md` page.
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
