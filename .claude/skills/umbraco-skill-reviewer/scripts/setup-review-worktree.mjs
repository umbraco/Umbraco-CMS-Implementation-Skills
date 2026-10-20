#!/usr/bin/env node
// setup-review-worktree.mjs <pr-number>... [--json]
//
// Sets up a review worktree for each PR, one after another: `../<repo>-pr-<n>` on a local branch
// `review/pr-<n>` that tracks the PR's real branch (the contributor's fork, for a fork PR). Prints
// what the review needs next: the worktree path, the PR head it started from, and whether we can
// push to the PR branch.
//
// One at a time on purpose: worktrees share one `.git/config`, and `gh pr checkout` run in parallel
// can fail to lock it ("could not lock config file") while still checking the branch out, leaving a fork
// PR with no push remote, and the push step would then fall back to `origin`, the wrong repository.
// Re-running `gh pr checkout` on an existing branch doesn't repair it either. So for a fork PR we can
// push to, this sets the push remote itself, to the fork's URL in the same scheme as `origin`.
//
// An existing worktree for a PR is reused if it's on `review/pr-<n>`, and refused otherwise.
// Run it from inside the repo. Needs `gh` (authenticated) and git. Exit code: 0 all set up, 1 any
// failed, 2 usage error.

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const run = (cmd, args, cwd) => execFileSync(cmd, args, { cwd, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }).trim();
const tryRun = (cmd, args, cwd) => {
  try {
    return run(cmd, args, cwd);
  } catch {
    return "";
  }
};

/** `../<repo>-pr-<n>` beside the repo's main checkout. */
export function worktreePath(repoRoot, number) {
  return path.join(path.dirname(repoRoot), `${path.basename(repoRoot)}-pr-${number}`);
}

/** The fork's URL, in the same scheme (SSH or HTTPS) as this repo's `origin`. */
export function forkUrl(originUrl, owner, repo) {
  return originUrl.startsWith("git@") ? `git@github.com:${owner}/${repo}.git` : `https://github.com/${owner}/${repo}.git`;
}

/** A branch in this repo is always pushable; a fork only when the author allowed maintainer edits. */
export function canPush(pr) {
  return !pr.isCrossRepository || pr.maintainerCanModify;
}

function setUp(repoRoot, number) {
  const branch = `review/pr-${number}`;
  const dir = worktreePath(repoRoot, number);
  const result = { number, worktree: dir, branch };

  if (fs.existsSync(dir)) {
    const current = tryRun("git", ["branch", "--show-current"], dir);
    if (current !== branch) {
      return { ...result, error: `${dir} exists but is on "${current || "a detached HEAD"}", not ${branch}` };
    }
    result.reused = true;
  } else {
    run("git", ["worktree", "add", dir, "origin/main", "--detach"], repoRoot);
  }

  if (!result.reused) tryRun("gh", ["pr", "checkout", String(number), "--branch", branch], dir);

  const pr = JSON.parse(
    run("gh", ["pr", "view", String(number), "--json", "isCrossRepository,maintainerCanModify,headRefName,headRefOid,headRepository,headRepositoryOwner,url"], dir),
  );
  result.url = pr.url;
  result.headRefName = pr.headRefName;
  result.canPush = canPush(pr);

  // A fork PR we can push to must push to the fork, never to `origin`.
  if (pr.isCrossRepository && result.canPush) {
    const url = forkUrl(run("git", ["remote", "get-url", "origin"], dir), pr.headRepositoryOwner.login, pr.headRepository.name);
    run("git", ["config", `branch.${branch}.pushRemote`, url], dir);
  }
  result.pushRemote = tryRun("git", ["config", "--get", `branch.${branch}.pushRemote`], dir) || "origin";

  result.startHead = tryRun("git", ["rev-parse", "HEAD"], dir);
  if (result.startHead !== pr.headRefOid) {
    return { ...result, error: `worktree HEAD ${result.startHead.slice(0, 7)} is not the PR head ${pr.headRefOid.slice(0, 7)}` };
  }
  return result;
}

function main(argv) {
  const json = argv.includes("--json");
  const numbers = argv.filter((a) => a !== "--json");
  if (numbers.length === 0 || numbers.some((n) => !/^\d+$/.test(n))) {
    console.error("Usage: setup-review-worktree.mjs <pr-number>... [--json]");
    return 2;
  }
  const repoRoot = run("git", ["rev-parse", "--show-toplevel"], process.cwd());
  run("git", ["fetch", "--quiet", "origin"], repoRoot);

  const results = numbers.map((n) => {
    try {
      return setUp(repoRoot, Number(n));
    } catch (error) {
      return { number: Number(n), error: String(error.stderr || error.message).trim().split("\n")[0] };
    }
  });

  if (json) {
    process.stdout.write(`${JSON.stringify(results, null, 2)}\n`);
  } else {
    for (const r of results) {
      process.stdout.write(
        r.error
          ? `#${r.number}  FAILED: ${r.error}\n`
          : `#${r.number}  ${r.worktree}${r.reused ? " (reused)" : ""}\n      start head ${r.startHead.slice(0, 7)}  push: ${r.canPush ? `yes, to ${r.pushRemote}` : "NO (see Can't push)"}\n`,
      );
    }
  }
  return results.some((r) => r.error) ? 1 : 0;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
