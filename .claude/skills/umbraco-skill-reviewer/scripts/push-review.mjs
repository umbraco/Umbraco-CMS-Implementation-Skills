#!/usr/bin/env node
// push-review.mjs <pr-number> --start-head <sha> [--dry-run]
//
// Gets a review's fixes onto a PR and proves they arrived, from inside its review worktree (as set
// up by setup-review-worktree.mjs). Prints one result line; only "LANDED" or "FORK PR" means the
// summary comment can go up.
//
//   MOVED (exit 3)    the PR head isn't the start head: the author pushed while you worked. Merge,
//                     compare and re-verify (fix-and-push.md), then run this again with the new head.
//   NOTHING (exit 4)  HEAD is still the start head: there's nothing of yours to push.
//   LANDED (exit 0)   pushed to the PR branch, and the PR head now equals your HEAD.
//   FORK PR (exit 0)  we can't push to the PR branch, so the fixes are on origin's review/pr-<n> and in
//                     a pull request into the author's branch (an open one is reused; pushing the
//                     branch updates it).
//   FAILED (exit 1)   anything else, with the reason. Don't comment or relabel.
//
// It never force-pushes, and refuses to push a fork PR to `origin` (that would land the fixes in this
// repository, not on the PR). Push output is shown as it happens. On #51 a push failed silently behind
// `| tail -1` and the summary comment went up anyway; this script is what stops that recurring.
// --dry-run checks everything and prints the pushes and the PR it would create, without doing them.
//
// Needs `gh` (authenticated) and git. Exit 2 is a usage error.

import { execFileSync, spawnSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { canPush } from "./setup-review-worktree.mjs";

const run = (cmd, args) => execFileSync(cmd, args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }).trim();
const tryRun = (cmd, args) => {
  try {
    return run(cmd, args);
  } catch {
    return "";
  }
};
const sleep = (seconds) => spawnSync("sleep", [String(seconds)]);

/** Owner and name of a GitHub repo from a remote URL (SSH or HTTPS). */
export function parseRepo(url) {
  const match = /github\.com[:/]([^/]+)\/(.+?)(?:\.git)?\/?$/.exec(url);
  if (!match) throw new Error(`not a GitHub remote: ${url}`);
  return { owner: match[1], name: match[2] };
}

/** Run a command with its output visible; with --dry-run, only print it. */
function step(dryRun, cmd, args) {
  console.log(`$ ${cmd} ${args.join(" ")}`);
  if (dryRun) return true;
  return spawnSync(cmd, args, { stdio: "inherit" }).status === 0;
}

/** Wait for GitHub to report the PR head as `sha` (it lags a push by a few seconds). */
function prHeadBecomes(number, sha) {
  for (let i = 0; i < 6; i += 1) {
    if (run("gh", ["pr", "view", String(number), "--json", "headRefOid", "--jq", ".headRefOid"]) === sha) return true;
    sleep(5);
  }
  return false;
}

function pushReview(number, startHead, dryRun) {
  const pr = JSON.parse(
    run("gh", ["pr", "view", String(number), "--json", "headRefOid,headRefName,isCrossRepository,maintainerCanModify,headRepository,headRepositoryOwner"]),
  );
  if (pr.headRefOid !== startHead) {
    return { status: "MOVED", code: 3, detail: `PR head is ${pr.headRefOid.slice(0, 7)}, you started from ${startHead.slice(0, 7)}` };
  }
  const head = run("git", ["rev-parse", "HEAD"]);
  if (head === startHead) return { status: "NOTHING", code: 4, detail: "HEAD is still the start head" };

  if (canPush(pr)) {
    const branch = run("git", ["branch", "--show-current"]);
    const remote = tryRun("git", ["config", "--get", `branch.${branch}.pushRemote`]) || "origin";
    if (pr.isCrossRepository && remote === "origin") {
      return { status: "FAILED", code: 1, detail: "fork PR would push to origin; run setup-review-worktree.mjs again" };
    }
    if (!step(dryRun, "git", ["push", remote, `HEAD:${pr.headRefName}`])) return { status: "FAILED", code: 1, detail: "push refused (see its output above)" };
    if (dryRun) return { status: "LANDED", code: 0, detail: "(dry run: nothing pushed)" };
    if (!prHeadBecomes(number, head)) return { status: "FAILED", code: 1, detail: "pushed, but the PR head isn't your commit" };
    return { status: "LANDED", code: 0, detail: `PR head is ${head.slice(0, 7)}` };
  }

  // Can't push to the PR branch: keep the fixes on origin and offer them as a PR into the author's branch.
  const reviewBranch = `review/pr-${number}`;
  const origin = parseRepo(run("git", ["remote", "get-url", "origin"]));
  if (!step(dryRun, "git", ["push", "origin", `HEAD:refs/heads/${reviewBranch}`])) return { status: "FAILED", code: 1, detail: "push to origin refused" };
  if (!dryRun && !run("git", ["ls-remote", "origin", `refs/heads/${reviewBranch}`]).startsWith(head)) {
    return { status: "FAILED", code: 1, detail: `origin's ${reviewBranch} isn't your commit` };
  }

  const fork = `${pr.headRepositoryOwner.login}/${pr.headRepository.name}`;
  const open = JSON.parse(run("gh", ["pr", "list", "--repo", fork, "--state", "open", "--json", "url,headRefName,headRepositoryOwner"]));
  const existing = open.find((p) => p.headRefName === reviewBranch && p.headRepositoryOwner?.login === origin.owner);
  if (existing) return { status: "FORK PR", code: 0, detail: `${existing.url} (existing; updated by the push)` };

  const args = [
    "pr", "create", "--repo", fork, "--base", pr.headRefName, "--head", `${origin.owner}:${reviewBranch}`,
    "--title", `Review fixes from ${origin.owner}/${origin.name}#${number}`,
    "--body", `Review fixes for ${origin.owner}/${origin.name}#${number}, opened here because maintainers can't push to this branch. Merge this and the fixes land on #${number}.`,
  ];
  console.log(`$ gh ${args.join(" ")}`);
  if (dryRun) return { status: "FORK PR", code: 0, detail: "(dry run: not created)" };
  const created = spawnSync("gh", args, { encoding: "utf8" });
  if (created.status !== 0) return { status: "FAILED", code: 1, detail: `gh pr create failed: ${created.stderr.trim()}` };
  return { status: "FORK PR", code: 0, detail: created.stdout.trim() };
}

function main(argv) {
  const dryRun = argv.includes("--dry-run");
  const rest = argv.filter((a) => a !== "--dry-run");
  const i = rest.indexOf("--start-head");
  const number = rest.find((a, k) => /^\d+$/.test(a) && k !== i + 1);
  const startHead = i >= 0 ? rest[i + 1] : undefined;
  if (!number || !startHead || !/^[0-9a-f]{40}$/.test(startHead)) {
    console.error("Usage: push-review.mjs <pr-number> --start-head <full sha> [--dry-run]");
    return 2;
  }
  let result;
  try {
    result = pushReview(Number(number), startHead, dryRun);
  } catch (error) {
    result = { status: "FAILED", code: 1, detail: String(error.stderr || error.message).trim().split("\n")[0] };
  }
  console.log(`${result.status}: ${result.detail}`);
  return result.code;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
