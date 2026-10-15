#!/usr/bin/env node
// check-skill-links.mjs [dir]   resolve every relative markdown link under plugins/ (or dir)
//
// Skills link into their own references/ and assets/ and into sibling skills
// (../umbraco-content-model-conventions/references/naming.md). A link that does not resolve is a
// reference the agent cannot read, and it fails silently at run time, so this fails the build
// instead. Exit 0 when every link resolves, 1 otherwise.
//
// Checked: [text](relative/path.md) and [text](path.md#heading), outside code.
// Skipped: http(s), mailto and tel links; template links that still hold a <Placeholder>;
//          bin/, obj/ and node_modules/; and evals/files/broken-*/ fixtures, which are wrong on
//          purpose.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const root = path.resolve(process.argv[2] ?? path.join(repoRoot, 'plugins'));
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git']);

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (SKIP_DIRS.has(entry.name)) return [];
      if (entry.name.startsWith('broken-') && path.basename(dir) === 'files') return [];
      return walk(full);
    }
    return entry.name.endsWith('.md') ? [full] : [];
  });
}

// GitHub's heading anchor: lower case, punctuation dropped, spaces to hyphens.
const anchorOf = (heading) =>
  heading.trim().toLowerCase().replace(/`/g, '').replace(/[^\p{L}\p{N} _-]/gu, '').replace(/ /g, '-');

const anchorCache = new Map();
function anchorsIn(file) {
  if (!anchorCache.has(file)) {
    const headings = fs.readFileSync(file, 'utf8').split(/\r?\n/).filter((l) => /^#{1,6} /.test(l));
    anchorCache.set(file, new Set(headings.map((l) => anchorOf(l.replace(/^#{1,6} /, '')))));
  }
  return anchorCache.get(file);
}

if (!fs.existsSync(root)) {
  console.error(`not found: ${root}`);
  process.exit(1);
}

const problems = [];
let checked = 0;
const files = walk(root);
for (const file of files) {
  let fenced = false;
  fs.readFileSync(file, 'utf8').split(/\r?\n/).forEach((raw, index) => {
    if (raw.trim().startsWith('```')) fenced = !fenced;
    if (fenced) return;
    const line = raw.replace(/`[^`]*`/g, (code) => ' '.repeat(code.length)); // links inside inline code are examples
    for (const match of line.matchAll(/\[[^\]]*\]\(([^)\s]+)\)/g)) {
      const target = match[1];
      if (/^(https?:|mailto:|tel:)/.test(target) || target.includes('<')) continue;
      checked++;
      const [rawPath, anchor] = target.split('#');
      const resolved = rawPath ? path.resolve(path.dirname(file), decodeURIComponent(rawPath)) : file;
      const where = `${path.relative(repoRoot, file).split(path.sep).join('/')}:${index + 1}`;
      if (!fs.existsSync(resolved)) {
        problems.push(`${where}: link target does not exist: ${target}`);
      } else if (anchor && resolved.endsWith('.md') && !anchorsIn(resolved).has(anchor.toLowerCase())) {
        problems.push(`${where}: no heading for anchor #${anchor} in ${rawPath || path.basename(file)}`);
      }
    }
  });
}

problems.forEach((p) => console.error(p));
console.log(`${checked} relative link(s) in ${files.length} file(s) checked: ${problems.length} broken`);
process.exit(problems.length > 0 ? 1 : 0);
