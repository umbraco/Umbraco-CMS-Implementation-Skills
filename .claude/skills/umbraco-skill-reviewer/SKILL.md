---
name: umbraco-skill-reviewer
description: >
  Review a skill pull request in this marketplace end to end: check the skill against the house
  conventions and fact-check its Umbraco claims against the source, fix what's wrong on the PR
  branch yourself, verify the result, and leave a short summary. Use this whenever the user wants a
  skill PR reviewed, checked, fixed or finished, even if they only paste a PR link or say "look at
  PR 52": "review PR 52", "review the skill PRs", "go through the for-ai-review PRs", "anything in
  the review queue?", "check this skill PR and fix it", "finish the pending reviews", or "is this
  skill PR ready to merge". Works on one PR or on every open PR labelled for-ai-review, and finishes
  PRs reviewed earlier. SKIP: writing a new skill from scratch (use umbraco-skill-author), running
  evals on its own (use umbraco-skill-evaluator), and pull requests that don't touch a skill.
---

# Umbraco skill reviewer

Takes a skill PR from "opened" to "ready to merge": review, fix, verify, summarise. Maintainer
tooling, not shipped in any plugin. It composes the
other authoring skills rather than repeating them: the bar is
[`umbraco-skill-author`'s conformance checklist](../umbraco-skill-author/references/conformance-checklist.md),
runtime proof comes from the `dotnet test` gate ([`umbraco-reference-instance`](../umbraco-reference-instance/SKILL.md))
and the repo-root `scripts/boot-check.mjs`, and evals from [`umbraco-skill-evaluator`](../umbraco-skill-evaluator/SKILL.md).

## Ground rules (decided by the maintainers)

- **Reviews and updates run on their own.** Fix it yourself and push to the PR branch (or, when you
  can't push, open the fixes as a pull request into the author's branch, as
  [fix-and-push.md](references/fix-and-push.md#cant-push-open-a-pull-request-into-the-authors-branch) describes).
  Post the summary comment and set the labels without asking first. Don't leave suggestions: they
  bounce back and forth.
- **Evals run only when something looks off.** The red flags are in [verify.md](references/verify.md#when-to-run-evals).
- **The labels are decorative**, so swap them freely: `for-ai-review`, `ai-review-done` and
  `ai-review-pending-edits`.
- **Never approve, request changes, or merge.** A human merges.
- **Never claim a check you didn't run.** Not in the comment, not in your report: the comment is
  what a human uses to decide whether to merge, so an unrun check that reads as run makes a red PR
  look ready.

## The stages

Open each reference as you reach its stage. The traps they list are why this skill exists.

| Stage | Reference | Output |
|---|---|---|
| 1. Review the skill | [review.md](references/review.md), in a worktree set up as [fix-and-push.md](references/fix-and-push.md#set-up-a-worktree-on-the-pr-branch) describes | A written findings list (format at the end of review.md) |
| 2. Fix on the PR branch | [fix-and-push.md](references/fix-and-push.md) | Commits on the PR branch, in the author's voice |
| 3. Verify | [verify.md](references/verify.md) | Validation, gate and boot results; evals only on a red flag |
| 4. Push and summarise | [fix-and-push.md](references/fix-and-push.md#push-comment-relabel), with `scripts/push-review.mjs` | Pushed commits, one summary comment, labels swapped |

For more than one PR, start from [batch.md](references/batch.md): it lists the queue with
`scripts/review-queue.mjs`, sets up the worktrees with `scripts/setup-review-worktree.mjs`, and runs
one reviewer per PR, then finishes PRs reviewed earlier (`--follow-ups`).

## Report back

End with a table, one row per PR: the PR, the skill, what you fixed, what you verified, and anything
you left for a human (a design question, a fixture you couldn't repair, a check you couldn't run, and
why).
