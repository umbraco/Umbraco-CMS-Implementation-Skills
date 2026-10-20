# Stages 2 and 4 — Fix on the PR branch, push, summarise

## Set up a worktree on the PR branch

Work in a worktree, never in the user's checkout. From the repo:

```bash
node .claude/skills/umbraco-skill-reviewer/scripts/setup-review-worktree.mjs <n>
```

It creates `../<repo>-pr-<n>` on a branch `review/pr-<n>` that tracks the PR's branch, makes sure a
fork PR we can push to pushes to the fork (never to `origin`), and prints the **start head** and
whether you can push. Note the start head: you'll need it before pushing. If it says `push: NO`,
everything is the same until the end: see [Can't push](#cant-push-open-a-pull-request-into-the-authors-branch).

## If the PR conflicts with `main`, merge `main` in

Many skill PRs conflict with `main` in the same few shared files once other skills land. Fix that as
part of the review, so the PR is actually mergeable:

```bash
git merge origin/main
```

- **`AGENTS.md` skills index**: don't hand-merge the generated table. Take either side, then run
  `node scripts/validate-skills.mjs --write-index`.
- **`Umbraco-CMS.Skills.sln`, `Umbraco-CMS.Skills/*.csproj`**: these are lists (projects,
  `<ProjectReference>`s). Keep both sides' entries.
- **Prose in `AGENTS.md` or READMEs**: both sides usually added a paragraph in the same spot. Keep both.
- **A whole-file conflict** usually means the PR changed the file's line endings (CRLF to LF or back).
  Keep `main`'s line endings and re-apply the PR's actual change on top.

Use a merge, never a rebase: the branch is shared, and a rebase needs a force-push.

## Fix

- Fix each finding from [review.md](review.md), smallest change that's right. Keep the author's voice,
  structure and wording elsewhere: a review isn't a rewrite.
- If a fix changes what the skill teaches (not just wording), note it: that's a red flag for running
  evals in [verify.md](verify.md#when-to-run-evals).
- A design question you can't settle from the source (two valid approaches, a scope call, a policy
  the repo hasn't decided) isn't yours to decide. Leave it for the human in your report rather than
  picking one.
- Commit in the PR's existing commit style, with a subject that says it's the review (for example
  `Review fixes for umbraco-x`), a short bullet list of what changed, and the `Co-Authored-By`
  trailer your session asks for. One commit per review pass is fine.

## Push, comment, relabel

### 1. Push, and prove it landed

From the worktree:

```bash
node .claude/skills/umbraco-skill-reviewer/scripts/push-review.mjs <n> --start-head <start head>
```

It checks the PR head hasn't moved, then either pushes to the PR branch and confirms the PR head is
your commit (`LANDED`), or, when you can't push, opens the fork PR ([Can't push](#cant-push-open-a-pull-request-into-the-authors-branch); `FORK PR`).
It never force-pushes and won't push a fork PR to `origin`.

- **Only comment and relabel after `LANDED` or `FORK PR`.** On #51 a push failed silently and the
  summary comment went up anyway; the script's result line is what prevents that now.
- **`MOVED`**: the author pushed while you worked. `git pull` (merge, not rebase), then compare:
authors often fix the same things in parallel. Drop your change where theirs is equivalent; only
keep what still adds something, and re-run [verify.md](verify.md) on the result, then run the script
again with the new PR head as the start head. If nothing of yours is left (`NOTHING`), don't push:
validate their commit and say so instead.
- **`FAILED`**: read the reason; fix it, never `--force` past it.

### 2. One summary comment

The author should read it in under a minute. At most one short paragraph and a few bullets:

```markdown
Reviewed and fixed on the branch. The Umbraco claims I checked against the 17.5.3 source held up
apart from the ones below; validation and the dotnet test gate pass on the result.

- `references/x.md`: `AddUnique` defaults to singleton, not scoped (`ServiceCollectionExtensions.cs:29`), so the advice now passes the lifetime.
- `assets/Y.cs`: comment now mentions the attended-install restart.
- Merged `main` to clear the conflict in `Umbraco-CMS.Skills.csproj`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Say what changed and the evidence in a few words, and name only the checks you actually ran (add
boot-check or evals when you ran them). Don't paste the findings list, the checks' output, or praise.
If something is left for a human, add one bullet saying what.

Always end with the `🤖 Generated with [Claude Code](https://claude.com/claude-code)` footer, word for
word: `review-queue.mjs --follow-ups` finds our last summary by it.

### 3. Swap the labels

```bash
gh pr edit <n> --remove-label for-ai-review --add-label ai-review-done
```


## Can't push: open a pull request into the author's branch

When `maintainerCanModify` is `false` (or the fork belongs to an organisation, where GitHub doesn't
offer the setting), don't wait on the author: `push-review.mjs` pushes the fixes to `review/pr-<n>`
on `origin` (so they're off your machine) and opens them as a pull request into the author's branch,
reusing one that's already open.

Then one comment on the PR, addressed to the author: the summary as above, opening with where the
fixes are instead of "fixed on the branch":

```markdown
@<author> reviewed; I can't push to your branch, so the fixes are a pull request into it:
<fork PR link>. Merge that and they land here. To get future review fixes pushed straight to the PR,
tick **Allow edits by maintainers** in the right-hand sidebar.

- ...the same bullets...

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Label it `ai-review-pending-edits` instead of `ai-review-done`
(`gh pr edit <n> --remove-label for-ai-review --add-label ai-review-pending-edits`). Run the evals
your red flags call for on the local branch as usual: the fork PR is what lands, so it's what they
should prove.

### Finishing

`review-queue.mjs --follow-ups` reports when a pending PR can be finished:

- `relabel`: the author merged the fork PR, so the PR head contains your commit.
- `push`: the author enabled maintainer edits. Set the worktree up again (`setup-review-worktree.mjs <n>`),
  run `git merge --ff-only origin/review/pr-<n>` in it, and run `push-review.mjs` with the PR head as
  the start head: it now lands the commit on their branch.

Either way, then swap the label to `ai-review-done` and delete `review/pr-<n>` from `origin`
(`git push origin --delete review/pr-<n>`).
