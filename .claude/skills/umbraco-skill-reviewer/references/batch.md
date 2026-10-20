# Reviewing the `for-ai-review` queue

## List the queue

```bash
node .claude/skills/umbraco-skill-reviewer/scripts/review-queue.mjs               # to review
node .claude/skills/umbraco-skill-reviewer/scripts/review-queue.mjs --follow-ups  # to finish
node .claude/skills/umbraco-skill-reviewer/scripts/review-queue.mjs --json        # for scripting
```

The default lists every open PR labelled `for-ai-review`, oldest first, with what each needs: the
skills it touches, whether you can push to it, whether it conflicts with `main`, and committed eval
workspaces. `push: NO` PRs get the same review; only the last step differs.

`--follow-ups` lists PRs already reviewed that need something more. `relabel` and `push` finish a
pending PR as [fix-and-push.md](fix-and-push.md#finishing) describes. `re-review` means an
`ai-review-done` PR has commits newer than our last summary: swap its label back to `for-ai-review`
and review it again, reading our earlier comment first.

## Set up every worktree first, one at a time

Worktrees share one `.git/config`, and `gh pr checkout` run in parallel fails to lock it, which can
leave a fork PR's branch with no push remote. So the orchestrator creates all the worktrees before
starting any reviewer, one after another, and checks each:

```bash
for n in <numbers>; do
  git worktree add ../<repo>-pr-$n origin/main --detach
  (cd ../<repo>-pr-$n && gh pr checkout $n --branch review/pr-$n)
  git -C ../<repo>-pr-$n config --get branch.review/pr-$n.pushRemote   # fork PRs: must print the fork URL
done
```

## One reviewer per PR

Each PR gets its own reviewer in its ready worktree, so builds, ports and branches never collide. Give
each reviewer this skill, the PR number and its worktree:

> Review PR #<n> with the `umbraco-skill-reviewer` skill (`.claude/skills/umbraco-skill-reviewer/SKILL.md`).
> Your worktree is `../<repo>-pr-<n>`, already on the PR branch: don't create another. The
> Umbraco-CMS source is at <path>; read tag `release-<version>` without changing its checkout.
> Follow the skill through to the summary comment and label, and report back one row: what you
> fixed, what you verified, what's left for a human.

- **How many at once.** Every reviewer builds the solution and boots sites, so run two or three at a
  time, not the whole queue. They don't share files, so the limit is CPU and memory.
- **Order.** Oldest first, unless two PRs touch the same skill or shared file: run those one after
  the other, the later one after merging `main` again.
- **The Umbraco source path** has to come from the user. Ask once, before starting the batch.

## After the batch

Check each reviewer's claims before reporting them: the PR head equals the reviewer's commit (or the
fork PR exists), nothing new appeared on `origin` except `review/pr-<n>` branches, and each PR has one
new summary comment and the right label.

Then report one table, a row per PR, and the queue's state:

| PR | Skill | Fixed | Verified | Left for a human |
|---|---|---|---|---|

Clean up each worktree once its fixes are on GitHub (`git worktree remove ../<repo>-pr-<n>`), and
delete the local `review/pr-<n>` branches. Remove anything the boots left in the system temp
directory (`boot-check-*`) once you no longer need the evidence.
