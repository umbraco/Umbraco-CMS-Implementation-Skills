#!/usr/bin/env node
// review-queue.mjs [--label NAME] [--json]
// review-queue.mjs --follow-ups [--json]
//
// Lists the open skill PRs waiting for review (label `for-ai-review` by default), oldest first, with
// what a reviewer needs to know before starting: the skills each PR touches, whether we can push to
// its branch, whether it conflicts with main, and the web projects its evals ship (candidates for
// boot-check). It also flags committed eval workspaces, which the evaluator says to delete before a PR.
//
// File lists and conflicts come from git, not GitHub: `gh` caps a PR's file list at 100 and reports
// mergeability as UNKNOWN until it has computed it. Each PR head is fetched to refs/review-queue/<n>
// and trial-merged with origin/main (`git merge-tree`), which writes nothing to the working tree.
// Run it from inside the repo.
//
// --follow-ups lists reviewed PRs that need something more: `ai-review-pending-edits` PRs whose
// author has merged our fork PR (relabel) or enabled maintainer edits (push), and `ai-review-done`
// PRs with commits newer than our last summary comment (re-review).
//
// Needs the GitHub CLI (`gh`), authenticated, and git 2.38+. No other dependencies.

import { execFileSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { canPush } from "./setup-review-worktree.mjs";

const FIELDS = "number,title,author,createdAt,headRefName,headRefOid,isCrossRepository,maintainerCanModify,url";

const git = (...args) => execFileSync("git", args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });

/** Fetch a PR head and return { paths, conflicts } relative to origin/main. */
function inspect(number) {
  const ref = `refs/review-queue/${number}`;
  git("fetch", "--quiet", "origin", `+pull/${number}/head:${ref}`);
  const paths = git("diff", "--name-only", `origin/main...${ref}`).split("\n").filter(Boolean);
  let conflicts = [];
  try {
    git("merge-tree", "--write-tree", "--name-only", "--no-messages", "origin/main", ref);
  } catch (error) {
    // Exit code 1 means conflicts: the tree id, then the conflicted paths.
    if (error.status !== 1) throw error;
    conflicts = String(error.stdout).split("\n").slice(1).filter(Boolean);
  }
  return { paths, conflicts };
}

/** Turn `gh pr list --json` output (each PR with its git-derived paths and conflicts) into rows, oldest first. */
export function summarise(prs) {
  return [...prs]
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
    .map((pr) => {
      const paths = pr.paths;
      const skills = [...new Set(paths.map((p) => /^plugins\/[^/]+\/skills\/([^/]+)\//.exec(p)?.[1]).filter(Boolean))];
      return {
        number: pr.number,
        title: pr.title,
        author: pr.author.login,
        url: pr.url,
        head: pr.headRefOid,
        branch: pr.headRefName,
        skills,
        canPush: canPush(pr),
        conflicts: pr.conflicts,
        evalProjects: paths.filter((p) => /\/evals\/files\/.*\.csproj$/.test(p)),
        strays: [...new Set(paths.map((p) => /^(.*?(?:^|\/)(?:eval-results|[^/]+-workspace))\//.exec(p)?.[1]).filter(Boolean))],
      };
    });
}

export function formatTable(rows) {
  if (rows.length === 0) return "No open PRs in the queue.\n";
  const lines = ["PR    push  eval projects  skills — title"];
  for (const r of rows) {
    lines.push(
      `#${String(r.number).padEnd(4)} ${(r.canPush ? "yes" : "NO").padEnd(5)} ${String(r.evalProjects.length).padEnd(14)} ` +
        `${r.skills.join(", ") || "(no skill)"} — ${r.title}`,
    );
    if (r.conflicts.length) lines.push(`      conflicts with main: ${r.conflicts.join(", ")}`);
    if (r.strays.length) lines.push(`      committed eval workspace: ${r.strays.join(", ")}`);
  }
  return `${lines.join("\n")}\n`;
}

const SUMMARY_MARK = "Generated with [Claude Code]";

const ghJson = (args) => JSON.parse(execFileSync("gh", args, { encoding: "utf8" }));

/**
 * Decide what a reviewed PR needs next. Pure, given what git and GitHub reported:
 * - pending: { label: "ai-review-pending-edits", containsReviewCommit, canPush }
 * - done:    { label: "ai-review-done", number, commits: [{ committedDate, messageHeadline }], lastSummaryAt }
 *
 * A commit only counts as new work if it's newer than our last summary and isn't the author merging
 * our own fork PR (`Merge pull request #… from umbraco/review/pr-<n>`).
 */
export function followUpAction(pr) {
  if (pr.label === "ai-review-pending-edits") {
    if (pr.containsReviewCommit) return "relabel";
    if (pr.canPush) return "push";
    return null;
  }
  if (pr.label === "ai-review-done" && pr.lastSummaryAt) {
    const ours = `review/pr-${pr.number}`;
    const newWork = pr.commits.filter((c) => c.committedDate > pr.lastSummaryAt && !c.messageHeadline.includes(ours));
    if (newWork.length) return "re-review";
  }
  return null;
}

function followUps() {
  const me = execFileSync("gh", ["api", "user", "--jq", ".login"], { encoding: "utf8" }).trim();
  // Commits and comments per PR, one PR at a time: asking `gh pr list` for them across many PRs
  // exceeds GitHub's GraphQL node limit.
  const fields = "number,title,isCrossRepository,maintainerCanModify,commits,comments,url";
  const rows = [];
  for (const label of ["ai-review-pending-edits", "ai-review-done"]) {
    const numbers = ghJson(["pr", "list", "--state", "open", "--label", label, "--limit", "100", "--json", "number"]);
    for (const { number } of numbers) {
      const pr = ghJson(["pr", "view", String(number), "--json", fields]);
      const facts = { label, canPush: canPush(pr) };
      if (label === "ai-review-pending-edits") {
        const ref = `refs/review-queue/${pr.number}`;
        git("fetch", "--quiet", "origin", `+pull/${pr.number}/head:${ref}`);
        try {
          git("fetch", "--quiet", "origin", `+refs/heads/review/pr-${pr.number}:refs/review-queue/review-${pr.number}`);
          git("merge-base", "--is-ancestor", `refs/review-queue/review-${pr.number}`, ref);
          facts.containsReviewCommit = true;
        } catch {
          facts.containsReviewCommit = false; // no review branch, or the PR doesn't contain it yet
        }
      } else {
        const summaries = pr.comments.filter((c) => c.author?.login === me && c.body.includes(SUMMARY_MARK));
        facts.lastSummaryAt = summaries.at(-1)?.createdAt ?? null;
        facts.number = pr.number;
        facts.commits = pr.commits;
      }
      const action = followUpAction(facts);
      if (action) rows.push({ number: pr.number, title: pr.title, url: pr.url, label, action });
    }
  }
  return rows;
}

function main(argv) {
  let label = "for-ai-review";
  let json = false;
  let follow = false;
  for (let i = 0; i < argv.length; i += 1) {
    if (argv[i] === "--json") json = true;
    else if (argv[i] === "--follow-ups") follow = true;
    else if (argv[i] === "--label" && argv[i + 1]) label = argv[(i += 1)];
    else {
      console.error("Usage: review-queue.mjs [--label NAME] [--json]\n       review-queue.mjs --follow-ups [--json]");
      return 2;
    }
  }
  if (follow) {
    const rows = followUps();
    const text = rows.length
      ? `${rows.map((r) => `#${String(r.number).padEnd(4)} ${r.action.padEnd(10)} ${r.label} — ${r.title}`).join("\n")}\n`
      : "No follow-ups.\n";
    process.stdout.write(json ? `${JSON.stringify(rows, null, 2)}\n` : text);
    return 0;
  }
  const out = execFileSync("gh", ["pr", "list", "--state", "open", "--label", label, "--limit", "100", "--json", FIELDS], {
    encoding: "utf8",
  });
  git("fetch", "--quiet", "origin", "main");
  const prs = JSON.parse(out).map((pr) => ({ ...pr, ...inspect(pr.number) }));
  const rows = summarise(prs);
  process.stdout.write(json ? `${JSON.stringify(rows, null, 2)}\n` : formatTable(rows));
  return 0;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
