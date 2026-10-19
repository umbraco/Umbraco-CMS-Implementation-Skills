---
name: umbraco-skill-reviewer
description: >
  Review a skill pull request in this marketplace end to end: check the skill against the house
  conventions and fact-check its Umbraco claims against the source, fix what's wrong on the PR
  branch yourself, verify the result (validation, the dotnet test gate, boot-check), and leave a
  short summary. Use this whenever the user asks to "review PR 52", "review the skill PRs",
  "go through the for-ai-review PRs", "check this skill PR and fix it", or "is this skill PR
  ready to merge". Works on one PR or on every open PR labelled for-ai-review. Authoring/maintainer
  tooling — not shipped in any plugin. SKIP: writing a new skill from scratch (use
  umbraco-skill-author), running evals on its own (use umbraco-skill-evaluator), and pull requests
  that don't touch a skill.
---

# Umbraco skill reviewer

Takes a skill PR from "opened" to "ready to merge": review, fix, verify, summarise. It composes the
other authoring skills rather than repeating them: the bar is
[`umbraco-skill-author`'s conformance checklist](../umbraco-skill-author/references/conformance-checklist.md),
runtime proof comes from [`umbraco-reference-instance`](../umbraco-reference-instance/SKILL.md) and
`scripts/boot-check.mjs`, and evals from [`umbraco-skill-evaluator`](../umbraco-skill-evaluator/SKILL.md).

## Ground rules (decided by the maintainers)

- **Reviews and updates run on their own.** Fix it yourself and push to the PR branch; post the
  summary comment and set the labels without asking first. Don't leave suggestions: they bounce
  back and forth. One short summary comment (a paragraph and a few bullets at most).
- **If you can't push** (maintainer edits off), don't wait on the author: open the fixes as a pull
  request into their branch, so they land in one click.
- **Evals run only when something looks off.** The red flags are in [verify.md](references/verify.md#when-to-run-evals).
- **The labels are decorative**, so swap them freely: `for-ai-review` (to review), `ai-review-done`,
  and `ai-review-pending-edits` (fixes waiting in a pull request into the author's branch).
- **Never approve or merge.** A human merges.
- **Never claim a check you didn't run.** Not in the comment, not in your report.

## The stages

Open each reference as you reach its stage. The traps they list are why this skill exists.

| Stage | Reference | Output |
|---|---|---|
| 1. Review the skill | [review.md](references/review.md) | A findings list: each with file, line, what's wrong, and the source that proves it |
| 2. Fix on the PR branch | [fix-and-push.md](references/fix-and-push.md) | Commits on the PR branch, in the author's voice |
| 3. Verify | [verify.md](references/verify.md) | Validation, gate and boot results; evals only on a red flag |
| 4. Push and summarise | [fix-and-push.md](references/fix-and-push.md#push-comment-relabel) | Pushed commits, one summary comment, labels swapped |

For more than one PR, start from [batch.md](references/batch.md): it lists the queue with
`scripts/review-queue.mjs` and runs one reviewer per PR. `review-queue.mjs --follow-ups` lists PRs
already reviewed that need finishing: fork PRs the author merged, PRs that became pushable, and PRs
with new commits since our review.

## Report back

End with a table, one row per PR: what you fixed, what you verified, and anything you left for a
human (a design question, a fixture you couldn't repair, a check you couldn't run, and why).
