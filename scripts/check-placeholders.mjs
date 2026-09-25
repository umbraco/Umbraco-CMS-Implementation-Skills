#!/usr/bin/env node
// check-placeholders.mjs MANIFEST ASSET...
//
// Enforces the invariant that an asset may not carry a placeholder its .generate.json doesn't
// declare. This exists because a missed placeholder is SILENT: `<filterAlias>` left unsubstituted
// in `HasProperty("<filterAlias>")` still compiles, always returns false, and the feature it
// guards quietly does nothing. Nothing else in the pipeline can notice that.
//
// Two checks, both cheap and both necessary:
//
//   1. UNDECLARED — a `<Token>` in a position where a placeholder is meaningful, that the manifest
//      doesn't map. Positions are deliberately narrow: inside a double-quoted string literal, or in
//      a `namespace` declaration. Narrow scoping is what makes this precise — C# generics
//      (`Value<bool>`, `Task<IReadOnlyList<ITemplate>>`) and prose about XML elements
//      (`// Only <loc> + <lastmod> are emitted`) are never in either position, so they never
//      false-positive.
//
//   2. UNUSED — a placeholder the manifest declares that appears in no listed asset. Catches the
//      divergence the other way round: a renamed token, or a manifest copied between skills.
//
// Limits worth knowing: string literals are matched per line, so a placeholder inside a verbatim
// `@"..."` string spanning lines is not seen. In XML assets only attribute values are string
// positions, so element-position tokens (`<Design>`) are ignored — which is correct, since those
// are the XML's own structure, not placeholders.
//
// No dependencies — Node standard library only.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// A <Token> that could be a placeholder. Requires no whitespace inside, so `a < b` never matches.
const CANDIDATE = /<[A-Za-z][A-Za-z0-9_]*>/g;
// A double-quoted string literal, honouring backslash escapes.
const STRING_LITERAL = /"(?:[^"\\]|\\.)*"/g;
const NAMESPACE_DECL = /^\s*namespace\s/;

/** A manifest that cannot be used, reported as a message rather than a stack trace. */
export class ManifestError extends Error {}

/** Yield the substrings of `line` in which a <Token> would be a placeholder. */
function* placeholderPositions(line) {
  if (NAMESPACE_DECL.test(line)) yield line;
  for (const match of line.matchAll(STRING_LITERAL)) yield match[0];
}

/**
 * Repo-relative where possible — absolute paths bury the filename these errors are about.
 * Across Windows drives no relative path exists and path.relative returns the absolute
 * target, which is the right fallback.
 */
export function short(target) {
  return path.relative(process.cwd(), target);
}

/**
 * Read a .generate.json and return { manifest, declared }, where `declared` maps every
 * placeholder token to the value it is substituted for.
 *
 * `namespace` is validated here rather than left to fail later: it is the one field every
 * manifest must carry, and the failure it causes downstream (an unsubstituted `<Namespace>`
 * compiled into a real file) is exactly the silent class of bug this script exists to stop.
 */
export function readManifest(manifestPath) {
  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  } catch (error) {
    throw new ManifestError(`${short(manifestPath)} is not valid JSON: ${error.message}`);
  }
  if (typeof manifest !== "object" || manifest === null || Array.isArray(manifest)) {
    throw new ManifestError(`${short(manifestPath)} must contain a JSON object.`);
  }
  if (typeof manifest.namespace !== "string" || manifest.namespace === "") {
    throw new ManifestError(
      `${short(manifestPath)} is missing a non-empty "namespace" — it is what <Namespace> ` +
        `is substituted for.`,
    );
  }

  const declared = { "<Namespace>": manifest.namespace, ...(manifest.placeholders || {}) };
  return { manifest, declared };
}

/** Return 0 if every placeholder lines up, else 1 having explained why via `log`. */
export function check(manifestPath, assetPaths, { log = console.error } = {}) {
  const { declared } = readManifest(manifestPath);

  const errors = [];
  const seen = new Set();
  const reported = new Set();

  for (const assetPath of assetPaths) {
    const text = fs.readFileSync(assetPath, "utf8");
    const rawLines = text.split(/\r?\n/);
    for (let index = 0; index < rawLines.length; index += 1) {
      const number = index + 1;
      // Terminator re-added because NAMESPACE_DECL's trailing `\s` can match it, matching how
      // the file is read line-by-line rather than split.
      const line = `${rawLines[index]}\n`;
      for (const fragment of placeholderPositions(line)) {
        for (const match of fragment.matchAll(CANDIDATE)) {
          const token = match[0];
          seen.add(token);
          // NUL-delimited so a path containing the separator cannot forge a collision.
          const key = `${assetPath}\u0000${number}\u0000${token}`;
          // A token used twice on one line is one mistake, not two.
          if (token in declared || reported.has(key)) continue;
          reported.add(key);
          errors.push(
            `  ${short(assetPath)}:${number} carries ${token}, which the ` +
              `manifest does not declare.\n    ${line.trim().slice(0, 100)}`,
          );
        }
      }
    }
  }

  const unused = Object.keys(declared)
    .filter((token) => token !== "<Namespace>" && !seen.has(token))
    .sort();
  for (const token of unused) {
    errors.push(
      `  declares ${token}, but no listed asset uses it — ` +
        `the manifest and the assets have diverged.`,
    );
  }

  if (errors.length > 0) {
    log(`PLACEHOLDER ERROR (${short(manifestPath)}):`);
    log(
      errors.join("\n") +
        "\n  An undeclared placeholder survives substitution as a literal string: it still " +
        "compiles,\n  so nothing complains, and the code can simply never find what it " +
        "looks for.",
    );
    return 1;
  }
  return 0;
}

function main(argv) {
  if (argv.length < 2) {
    console.error("usage: node scripts/check-placeholders.mjs MANIFEST ASSET...");
    return 2;
  }
  try {
    return check(argv[0], argv.slice(1));
  } catch (error) {
    if (error instanceof ManifestError) {
      console.error(`ERROR: ${error.message}`);
      return 1;
    }
    throw error;
  }
}

// Only act as a CLI when executed directly, so tests can import check().
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
