#!/usr/bin/env node
// review-queue.mjs [--label NAME] [--json]
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
// Needs the GitHub CLI (`gh`), authenticated, and git 2.38+. No other dependencies.

import { execFileSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

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
        // A branch in this repo is always pushable; a fork only when the author allowed maintainer edits.
        canPush: !pr.isCrossRepository || pr.maintainerCanModify,
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

function main(argv) {
  let label = "for-ai-review";
  let json = false;
  for (let i = 0; i < argv.length; i += 1) {
    if (argv[i] === "--json") json = true;
    else if (argv[i] === "--label" && argv[i + 1]) label = argv[(i += 1)];
    else {
      console.error("Usage: review-queue.mjs [--label NAME] [--json]");
      return 2;
    }
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
