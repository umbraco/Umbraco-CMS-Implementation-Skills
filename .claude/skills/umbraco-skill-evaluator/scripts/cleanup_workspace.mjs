#!/usr/bin/env node
// Safely remove a completed skill-evaluator workspace.
//
// Usage:
//   node cleanup_workspace.mjs <workspace>                                   # validate only
//   node cleanup_workspace.mjs <workspace> --yes --final-review-complete     # delete
//
// Node standard library only, like the rest of this repo's tooling.

import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { parseArgs } from "node:util";

const isDir = (p) => {
  try {
    return fs.statSync(p).isDirectory();
  } catch {
    return false;
  }
};

/** Throws unless `workspace` looks like an evaluator workspace: the guard against deleting anything else. */
export function validateWorkspace(workspace) {
  if (!isDir(workspace)) {
    throw new Error(`Workspace does not exist or is not a directory: ${workspace}`);
  }
  if (!path.basename(workspace).endsWith("-workspace")) {
    throw new Error("Refusing to clean a directory whose name does not end with '-workspace'.");
  }
  const iterations = fs.readdirSync(workspace)
    .filter((name) => name.startsWith("iteration-") && isDir(path.join(workspace, name)));
  if (!iterations.length) {
    throw new Error("Refusing to clean a directory without an iteration-* child directory.");
  }
  if (!iterations.some((name) => fs.existsSync(path.join(workspace, name, "benchmark.json")))) {
    throw new Error("Refusing to clean a directory without evaluator benchmark output.");
  }
}

/** Runs the CLI; returns the process exit code. */
export function run(argv, { log = console.log, error = console.error } = {}) {
  let values;
  let positionals;
  try {
    ({ values, positionals } = parseArgs({
      args: argv,
      allowPositionals: true,
      options: {
        yes: { type: "boolean", default: false },
        "final-review-complete": { type: "boolean", default: false },
      },
    }));
  } catch (e) {
    // Includes the retired --clean-python-caches flag: there is no Python tooling left to leave caches.
    error(`error: ${e.message}`);
    return 2;
  }

  if (positionals.length !== 1) {
    error("Usage: node cleanup_workspace.mjs <workspace> [--yes --final-review-complete]");
    return 2;
  }

  const workspace = path.resolve(positionals[0]);
  try {
    validateWorkspace(workspace);
  } catch (e) {
    error(`error: ${e.message}`);
    return 2;
  }

  if (!values.yes) {
    log(`Validated evaluator workspace; nothing deleted: ${workspace}`);
    log("After review is complete, re-run with --yes --final-review-complete to remove it.");
    return 0;
  }

  if (!values["final-review-complete"]) {
    error("Refusing to delete before final review confirmation; add --final-review-complete after feedback processing is complete.");
    return 2;
  }

  fs.rmSync(workspace, { recursive: true, force: true });
  log(`Removed evaluator workspace: ${workspace}`);
  return 0;
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  process.exitCode = run(process.argv.slice(2));
}
