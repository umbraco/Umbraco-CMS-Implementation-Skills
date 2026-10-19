# Stages 2 and 4 — Fix on the PR branch, push, summarise

## Set up a worktree on the PR branch

Work in a worktree, never in the user's checkout:

```bash
git fetch origin
git worktree add ../<repo>-pr-<n> origin/main --detach
cd ../<repo>-pr-<n>
gh pr checkout <n> --branch review/pr-<n>
```

`gh pr checkout` points the branch's push target at the PR's real branch, including a contributor's
fork, so a plain `git push` later updates the PR. Before you start, check
`gh pr view <n> --json maintainerCanModify`. If it's `false` you can't push to a fork: post your
findings as a review comment instead, and say so in your report.

Note the PR head you started from (`git rev-parse HEAD`). You'll need it before pushing.

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

Use a merge, never a rebase: the branch is shared, and a rebase needs a force-push.

## Fix

- Fix each finding from [review.md](review.md), smallest change that's right. Keep the author's voice,
  structure and wording elsewhere: a review isn't a rewrite.
- If a fix changes what the skill teaches (not just wording), note it: that's a red flag for running
  evals in [verify.md](verify.md#when-to-run-evals).
- A design question you can't settle from the source (two valid approaches, a scope call) isn't yours
  to decide. Leave it for the human in your report rather than picking one.
- Commit in the PR's existing commit style, one commit per review pass is fine.

## Push, comment, relabel

### 1. Check the PR head hasn't moved

```bash
gh pr view <n> --json headRefOid --jq .headRefOid   # must equal the head you started from
```

If it moved, the author pushed while you worked. `git pull` (merge, not rebase), then compare:
authors often fix the same things in parallel. Drop your change where theirs is equivalent; only
keep what still adds something, and re-run [verify.md](verify.md) on the result. If nothing of yours
is left, don't push. Validate their commit and say so instead.

### 2. Push

```bash
git push
```

Never `--force`. If the push is rejected, the head moved: go back to step 1.

### 3. One summary comment

The author should read it in under a minute. At most one short paragraph and a few bullets:

```markdown
Reviewed and fixed on the branch. The Umbraco claims I checked against the 17.5.3 source held up
apart from the ones below; validation, the dotnet test gate and boot-check pass on the result.

- `references/x.md`: `AddUnique` defaults to singleton, not scoped (`ServiceCollectionExtensions.cs:29`), so the advice now passes the lifetime.
- `assets/Y.cs`: comment now mentions the attended-install restart.
- Merged `main` to clear the conflict in `Umbraco-CMS.Skills.csproj`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Say what changed and the evidence in a few words. Don't paste the findings list, the checks' output,
or praise. If something is left for a human, add one bullet saying what.

### 4. Swap the labels

```bash
gh pr edit <n> --remove-label for-ai-review --add-label ai-review-done
```

The labels are decorative, so this is safe. Don't approve, request changes, or merge.
