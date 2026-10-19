# Stages 2 and 4 — Fix on the PR branch, push, summarise

The aim is that the fixes reach the PR without anyone having to do anything: pushed to its branch
when we can, offered as a one-click pull request into the author's branch when we can't.

## Set up a worktree on the PR branch

Work in a worktree, never in the user's checkout:

```bash
git fetch origin
git worktree add ../<repo>-pr-<n> origin/main --detach
cd ../<repo>-pr-<n>
gh pr checkout <n> --branch review/pr-<n>
gh pr view <n> --json isCrossRepository,maintainerCanModify,headRefName,headRefOid
```

`gh pr checkout` records where the PR's branch lives (the contributor's fork, for a fork PR) as the
local branch's push remote. Two things to check before you start:

- **For a fork PR, confirm the push remote was recorded**: `git config --get branch.review/pr-<n>.pushRemote`
  must print the fork's URL. Worktrees share one `.git/config`, so `gh pr checkout` run in parallel
  can fail to write it (`could not lock config file`) while still checking the branch out, and the
  push step would then fall back to `origin`, the wrong repository. If it's empty, run
  `gh pr checkout` again. In a batch, set up the worktrees one after another (see [batch.md](batch.md)).
- **Whether you can push**: a branch in this repo always; a fork only when `maintainerCanModify` is
  `true`. If you can't, do everything else the same and follow [Can't push](#cant-push-open-a-pull-request-into-the-authors-branch) at the end.

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

### 1. Check the PR head hasn't moved

```bash
gh pr view <n> --json headRefOid --jq .headRefOid   # must equal the head you started from
```

If it moved, the author pushed while you worked. `git pull` (merge, not rebase), then compare:
authors often fix the same things in parallel. Drop your change where theirs is equivalent; only
keep what still adds something, and re-run [verify.md](verify.md) on the result. If nothing of yours
is left, don't push. Validate their commit and say so instead.

### 2. Push, then prove it landed

```bash
branch=$(git branch --show-current)
remote=$(git config --get "branch.$branch.pushRemote" || echo origin)
git push "$remote" HEAD:$(gh pr view <n> --json headRefName --jq .headRefName)
gh pr view <n> --json headRefOid --jq .headRefOid    # must equal: git rev-parse HEAD
```

- Read the push's own output and exit status; don't pipe it through `tail` or `grep`, which hides a
  refusal (that happened on #51: the push failed, and the summary comment went up anyway).
- **Don't comment or relabel until the PR head equals your commit.** The comment says the fixes are
  on the branch; make sure they are.
- Never `--force`. If the push is rejected because the branch moved, go back to step 1.

### 3. One summary comment

Post it without asking first: these reviews are meant to run on their own. The author should read it
in under a minute. At most one short paragraph and a few bullets:

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

### 4. Swap the labels

```bash
gh pr edit <n> --remove-label for-ai-review --add-label ai-review-done
```

The labels are decorative, so this is safe. Don't approve, request changes, or merge.

## Can't push: open a pull request into the author's branch

When `maintainerCanModify` is `false` (or the fork belongs to an organisation, where GitHub doesn't
offer the setting), don't wait on the author. Make the fixes reachable in one click:

```bash
git push origin HEAD:refs/heads/review/pr-<n>                 # keeps the fixes off your machine
git ls-remote origin refs/heads/review/pr-<n>                 # must equal: git rev-parse HEAD
gh pr create --repo <author>/<repo> --base <headRefName> --head umbraco:review/pr-<n> \
  --title "Review fixes for <skill> (from umbraco#<n>)" \
  --body "Review fixes for umbraco/<repo>#<n>, opened here because maintainers can't push to this branch. Merge this and the fixes land on #<n>."
```

Then one comment on the PR, addressed to the author: the summary as above, opening with where the
fixes are instead of "fixed on the branch":

```markdown
@<author> reviewed; I can't push to your branch, so the fixes are a pull request into it:
<fork PR link>. Merge that and they land here. To get future review fixes pushed straight to the PR,
tick **Allow edits by maintainers** in the right-hand sidebar.

- ...the same bullets...
```

Label it `ai-review-pending-edits` instead of `ai-review-done`
(`gh pr edit <n> --remove-label for-ai-review --add-label ai-review-pending-edits`). Run the evals
your red flags call for on the local branch as usual: the fork PR is what lands, so it's what they
should prove.

Once the author merges the fork PR (or enables maintainer edits, and you push `review/pr-<n>` to
their branch as in step 2), check the PR head contains your commit, swap the label to
`ai-review-done`, and delete `review/pr-<n>` from `origin`. `review-queue.mjs --follow-ups` lists
the PRs where that has happened.
