#!/usr/bin/env node
// check-skill-docs.mjs [--strict] [dir]   check every implementation skill under dir
//                                         (default plugins/implementation/skills)
//
// Two conformance rules a reader can't see at a glance:
//   - The skill links the Umbraco docs it's built on at least once (docs.umbraco.com, in SKILL.md
//     or a reference). A skill with none is teaching from memory.
//   - A reference over 100 lines starts with a `## Contents` index: a list of links to its own
//     sections, each saying when to read it, so the agent can load only the part it needs.
//     check-skill-links.mjs already checks that the anchors resolve.
//
// Content-modelling skills are out of scope: they build on the conventions skill, not the Umbraco
// docs. Reports warnings and exits 0; --strict exits 1 when anything is reported.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const MAX_LINES_WITHOUT_INDEX = 100;
const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const DOCS_LINK = /https:\/\/docs\.umbraco\.com\//g;

export function docsLinkCount(text) {
  return (text.match(DOCS_LINK) ?? []).length;
}

/** Lines as an editor shows them: a final newline doesn't start another line. */
export function lineCount(text) {
  if (text === '') return 0;
  const lines = text.split(/\r?\n/).length;
  return /\r?\n$/.test(text) ? lines - 1 : lines;
}

/** A `## Contents` heading within the first 30 lines, followed by at least two links to `#anchors`. */
export function hasContentsIndex(text) {
  const lines = text.split(/\r?\n/);
  const start = lines.slice(0, 30).findIndex((line) => /^##\s+Contents\s*$/i.test(line));
  if (start < 0) return false;
  let links = 0;
  for (const line of lines.slice(start + 1)) {
    if (/^#{1,2}\s/.test(line)) break; // the next section ends the index
    if (/^\s*[-*]\s.*\]\(#[^)]+\)/.test(line)) links += 1;
  }
  return links >= 2;
}

function markdownFiles(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return entry.name === 'evals' ? [] : markdownFiles(full);
    return entry.name.endsWith('.md') ? [full] : [];
  });
}

/** Problems with one skill directory, as strings. */
export function checkSkill(skillDir) {
  const problems = [];
  const files = markdownFiles(skillDir);
  const links = files.reduce((sum, file) => sum + docsLinkCount(fs.readFileSync(file, 'utf8')), 0);
  if (links === 0) problems.push('no Umbraco docs link (docs.umbraco.com) in SKILL.md or any reference');

  const refsDir = path.join(skillDir, 'references');
  if (fs.existsSync(refsDir)) {
    for (const file of markdownFiles(refsDir)) {
      const text = fs.readFileSync(file, 'utf8');
      const lines = lineCount(text);
      if (lines > MAX_LINES_WITHOUT_INDEX && !hasContentsIndex(text)) {
        problems.push(`${path.relative(skillDir, file)}: ${lines} lines and no "## Contents" index`);
      }
    }
  }
  return problems;
}

function main(argv) {
  const strict = argv.includes('--strict');
  const dirArg = argv.find((a) => !a.startsWith('--'));
  const root = path.resolve(dirArg ?? path.join(repoRoot, 'plugins', 'implementation', 'skills'));
  const skills = fs
    .readdirSync(root, { withFileTypes: true })
    .filter((e) => e.isDirectory() && fs.existsSync(path.join(root, e.name, 'SKILL.md')))
    .map((e) => path.join(root, e.name));

  // In GitHub Actions, warnings become annotations so they show on the PR rather than only in the log.
  const prefix = process.env.GITHUB_ACTIONS === 'true' ? '::warning::' : 'warning: ';
  let count = 0;
  for (const skill of skills) {
    for (const problem of checkSkill(skill)) {
      console.log(`${prefix}${path.basename(skill)}: ${problem}`);
      count += 1;
    }
  }
  console.log(`${skills.length} skill(s) checked: ${count} warning(s)`);
  return strict && count > 0 ? 1 : 0;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
