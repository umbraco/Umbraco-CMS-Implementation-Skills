# Reviewing the `for-ai-review` queue

## List the queue

```bash
node .claude/skills/umbraco-skill-reviewer/scripts/review-queue.mjs          # table
node .claude/skills/umbraco-skill-reviewer/scripts/review-queue.mjs --json   # for scripting
```

It lists every open PR labelled `for-ai-review`, oldest first, with what each needs: the skills it
touches, whether you can push to it, whether it conflicts with `main`, and whether its evals ship a
web project to boot. PRs you can't push to (`push: no`) get a review comment instead of fixes; say
so in the report.

## One reviewer per PR

Each PR gets its own reviewer, working in its own worktree (`../<repo>-pr-<n>`), so builds, ports
and branches never collide. Give each reviewer this skill and the PR number:

> Review PR #<n> with the `umbraco-skill-reviewer` skill (`.claude/skills/umbraco-skill-reviewer/SKILL.md`),
> working in your own worktree. The Umbraco-CMS source is at <path> (tag `release-<version>`).
> Fix and push to the PR branch, leave the summary comment, swap the labels, and report back
> one row: what you fixed, what you verified, what's left for a human.

- **How many at once.** Every reviewer builds the solution and boots sites, so run two or three at a
  time, not the whole queue. They don't share files, so the limit is CPU and memory.
- **Order.** Oldest first, unless two PRs touch the same skill or shared file: run those one after
  the other, the later one after merging `main` again.
- **The Umbraco source path** has to come from the user. Ask once, before starting the batch.

## After the batch

Report one table, a row per PR (from the reviewers' rows), then the queue's state:

| PR | Skill | Fixed | Verified | Left for a human |
|---|---|---|---|---|

Clean up each worktree once its PR is pushed (`git worktree remove ../<repo>-pr-<n>`), and delete the
`review/pr-<n>` branches. Remove anything the boots left in the system temp directory
(`boot-check-*`) once you no longer need the evidence.
