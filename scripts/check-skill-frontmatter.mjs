#!/usr/bin/env node
// check-skill-frontmatter.mjs [dir]   validate every SKILL.md under plugins/ (or dir)
//
// A skill is discovered by its frontmatter alone, so a wrong `name` or an over-long `description`
// fails silently at run time: the skill is never offered, or its description is truncated. This
// fails the build instead. Exit 0 when every skill passes, 1 otherwise.
//
// Checked: the frontmatter parses and holds only `name` and `description`; `name` equals the folder
// name; `description` is at most 1024 characters (the Agent Skills limit; Claude Code allows 1536);
// `evals/evals.json`, when present, parses, its `skill_name` matches, and every path in an eval's
// `files` exists. A path ending in `/` names a fixture directory and must be one; any other path
// must be a file.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const MAX_DESCRIPTION = 1024;
const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const root = path.resolve(process.argv[2] ?? path.join(repoRoot, 'plugins'));
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git']);

function findSkills(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return SKIP_DIRS.has(entry.name) ? [] : findSkills(full);
    return entry.name === 'SKILL.md' ? [full] : [];
  });
}

// Minimal YAML: `key: value` and `key: >` folded blocks, which is all a SKILL.md frontmatter uses.
function frontmatter(text) {
  const lines = text.split(/\r?\n/);
  if (lines[0] !== '---') return null;
  const end = lines.indexOf('---', 1);
  if (end < 0) return null;
  const fields = {};
  let current = null;
  for (const line of lines.slice(1, end)) {
    const key = line.match(/^([A-Za-z_][\w-]*):(.*)$/);
    if (key) {
      current = key[1];
      const value = key[2].trim();
      fields[current] = value === '>' || value === '|' ? '' : value;
    } else if (current !== null) {
      fields[current] = `${fields[current]} ${line.trim()}`.trim();
    }
  }
  return fields;
}

const problems = [];
const skills = findSkills(root);
for (const file of skills) {
  const dir = path.dirname(file);
  const where = path.relative(repoRoot, file).split(path.sep).join('/');
  const fields = frontmatter(fs.readFileSync(file, 'utf8'));
  if (!fields) {
    problems.push(`${where}: no YAML frontmatter block`);
    continue;
  }
  const keys = Object.keys(fields);
  for (const key of keys) if (!['name', 'description'].includes(key)) problems.push(`${where}: unexpected frontmatter key "${key}"`);
  if (fields.name !== path.basename(dir)) problems.push(`${where}: name "${fields.name}" does not match the folder "${path.basename(dir)}"`);
  if (!fields.description) problems.push(`${where}: description is missing`);
  else if (fields.description.length > MAX_DESCRIPTION) problems.push(`${where}: description is ${fields.description.length} characters; the limit is ${MAX_DESCRIPTION}`);

  const evalsFile = path.join(dir, 'evals', 'evals.json');
  if (!fs.existsSync(evalsFile)) continue;
  let evals;
  try {
    evals = JSON.parse(fs.readFileSync(evalsFile, 'utf8'));
  } catch (error) {
    problems.push(`${where}: evals/evals.json does not parse: ${error.message}`);
    continue;
  }
  if (evals.skill_name !== fields.name) problems.push(`${where}: evals/evals.json skill_name "${evals.skill_name}" does not match "${fields.name}"`);
  for (const item of evals.evals ?? []) {
    for (const rel of item.files ?? []) {
      const target = path.join(dir, rel);
      if (!fs.existsSync(target)) problems.push(`${where}: eval ${item.id} names a file that does not exist: ${rel}`);
      else if (rel.endsWith('/')) {
        if (!fs.statSync(target).isDirectory()) problems.push(`${where}: eval ${item.id} names a file with a trailing slash, not a directory: ${rel}`);
      } else if (!fs.statSync(target).isFile()) problems.push(`${where}: eval ${item.id} names a directory without a trailing slash: ${rel}`);
    }
  }
}

problems.forEach((p) => console.error(p));
console.log(`${skills.length} skill(s) checked: ${problems.length} problem(s)`);
process.exit(problems.length > 0 ? 1 : 0);
