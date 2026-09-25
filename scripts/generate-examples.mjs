#!/usr/bin/env node
// generate-examples.mjs --manifest PATH --out DIR   # project the manifest's assets into DIR
// generate-examples.mjs --lint                      # placeholder-check every manifest, write nothing
//
// Each validated skill approach ships an example project that compiles the skill's OWN asset files,
// with `<Namespace>` (and any other placeholder the manifest declares) substituted for fixed values.
//
// Generation happens at BUILD TIME, into the project's obj/ — nothing is committed. That is the
// point: the file the compiler and the HTTP fixtures see is the skill's asset by construction, so it
// cannot drift from what the skill ships. There is no separate drift check to run or forget, because
// there is no second copy to drift.
//
// A skill whose assets/ folder is absent (e.g. it still lives on an unmerged branch) is SKIPPED, so a
// build is safe before the skill's PR merges. A manifest with no `assets` is declare-only — it exists
// to carry `requires`/`host` for an approach with no code of its own — and generates nothing.
//
// Writes are content-comparing: an unchanged asset leaves the generated file's mtime alone, so MSBuild
// can still skip the compile. Rewriting unconditionally would recompile the world on every build.
//
// Reads and writes are byte-exact: unlike a text-mode Python write, a CRLF asset stays CRLF and an
// LF asset stays LF on every platform, so a Windows build produces the same bytes as CI.
//
// No dependencies — Node standard library only. Invoked by plugins/Directory.Build.props at build
// time, so it must stay runnable with a bare `node` and nothing installed.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { ManifestError, check, readManifest } from "./check-placeholders.mjs";

const DEFAULT_REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

function isDir(target) {
  return fs.existsSync(target) && fs.statSync(target).isDirectory();
}

function isFile(target) {
  return fs.existsSync(target) && fs.statSync(target).isFile();
}

/** Return { assetsDir, assetNames, subs } for a manifest, or null to skip. */
function load(manifestPath) {
  const { manifest, declared } = readManifest(manifestPath);

  // <skill>/examples/<approach>/.generate.json — every approach of a skill projects the same
  // assets/, so the skill dir is two levels above the example dir.
  const assetsDir = path.resolve(path.dirname(manifestPath), "..", "..", "assets");
  if (!isDir(assetsDir)) return null;

  return { assetsDir, assetNames: manifest.assets || [], subs: declared };
}

export function substitute(text, subs) {
  let result = text;
  for (const [token, value] of Object.entries(subs)) {
    result = result.split(token).join(value);
  }
  return result;
}

export function generate(
  manifestPath,
  outDir,
  { log = console.log, errorLog = console.error } = {},
) {
  const loaded = load(manifestPath);
  if (loaded === null) {
    log(`skip ${path.basename(path.dirname(manifestPath))} — no assets/ on this branch`);
    return 0;
  }
  const { assetsDir, assetNames, subs } = loaded;

  const assetPaths = assetNames.map((name) => path.join(assetsDir, name));
  const missing = assetPaths.filter((p) => !isFile(p));
  for (const missingPath of missing) {
    errorLog(
      `ERROR: '${path.basename(missingPath)}' is listed in ${manifestPath} but missing from assets/`,
    );
  }
  if (missing.length > 0) return 1;

  if (check(manifestPath, assetPaths, { log: errorLog }) !== 0) return 1;

  fs.mkdirSync(outDir, { recursive: true });
  for (const src of assetPaths) {
    const dst = path.join(outDir, path.basename(src));
    const rendered = substitute(fs.readFileSync(src, "utf8"), subs);
    // Content-compare so an unchanged asset doesn't bump the mtime and force a recompile.
    if (isFile(dst) && fs.readFileSync(dst, "utf8") === rendered) continue;
    fs.writeFileSync(dst, rendered);
  }
  return 0;
}

/** Every example manifest, skipping the copies the SDK drops into bin/ and obj/. */
export function manifests(repoRoot = DEFAULT_REPO_ROOT) {
  const found = [];

  const walk = (dir) => {
    let entries;
    try {
      entries = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      return; // unreadable or absent — nothing to contribute
    }
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (entry.name === "bin" || entry.name === "obj") continue;
        walk(full);
      } else if (entry.isFile() && entry.name === ".generate.json") {
        // Only examples/<approach>/.generate.json counts, matching the glob this replaced.
        if (path.basename(path.dirname(path.dirname(full))) === "examples") found.push(full);
      }
    }
  };

  walk(path.join(repoRoot, "plugins"));
  return found.sort();
}

export function lint({
  repoRoot = DEFAULT_REPO_ROOT,
  log = console.log,
  errorLog = console.error,
} = {}) {
  let status = 0;
  let checked = 0;
  for (const manifestPath of manifests(repoRoot)) {
    const loaded = load(manifestPath);
    if (loaded === null) {
      log(`skip ${path.basename(path.dirname(manifestPath))} — no assets/ on this branch`);
      continue;
    }
    const { assetsDir, assetNames } = loaded;
    const assetPaths = assetNames
      .map((name) => path.join(assetsDir, name))
      .filter((p) => isFile(p));
    if (assetPaths.length > 0 && check(manifestPath, assetPaths, { log: errorLog }) !== 0) {
      status = 1;
    }
    checked += assetPaths.length;
  }
  if (status === 0) {
    log(`placeholders declared for every asset (${checked} file(s) checked)`);
  }
  return status;
}

const USAGE =
  "Usage: node scripts/generate-examples.mjs --manifest PATH --out DIR\n" +
  "       node scripts/generate-examples.mjs --lint";

function parseArgs(argv) {
  const options = { manifest: null, out: null, lint: false };
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === "--lint") {
      options.lint = true;
      continue;
    }
    const match = /^--(manifest|out)(?:=(.*))?$/.exec(arg);
    if (!match) return { error: `Unknown argument: ${arg}` };
    const key = match[1];
    // Accept both --out=DIR and --out DIR; MSBuild passes the latter.
    const value = match[2] !== undefined ? match[2] : argv[(i += 1)];
    if (value === undefined) return { error: `--${key} needs a value` };
    options[key] = value;
  }
  return options;
}

function main(argv) {
  const options = parseArgs(argv);
  if (options.error) {
    console.error(options.error);
    console.error(USAGE);
    return 2;
  }

  try {
    if (options.lint) return lint();
    if (!options.manifest || !options.out) {
      console.error("--manifest and --out are required unless --lint is given");
      console.error(USAGE);
      return 2;
    }
    return generate(options.manifest, options.out);
  } catch (error) {
    if (error instanceof ManifestError) {
      console.error(`ERROR: ${error.message}`);
      return 1;
    }
    throw error;
  }
}

// Only act as a CLI when executed directly, so tests can import lint()/generate().
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
