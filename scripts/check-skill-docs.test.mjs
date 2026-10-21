// Tests for the docs-link and reference-index check.
//
// Run: node --test scripts/*.test.mjs

import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { checkSkill, docsLinkCount, hasContentsIndex, lineCount } from './check-skill-docs.mjs';

const CLI = path.join(path.dirname(fileURLToPath(import.meta.url)), 'check-skill-docs.mjs');

function skillTree(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'skill-docs-'));
  for (const [rel, contents] of Object.entries(files)) {
    fs.mkdirSync(path.dirname(path.join(root, rel)), { recursive: true });
    fs.writeFileSync(path.join(root, rel), contents);
  }
  return root;
}

const LINK = 'See [Composing](https://docs.umbraco.com/umbraco-cms/reference/composing.md).';
const longBody = (n) => Array.from({ length: n }, (_, i) => `line ${i}`).join('\n');
const INDEX = '# Title\n\n## Contents\n\n- [Setup](#setup): read when wiring it\n- [Traps](#traps): read before shipping\n\n## Setup\n';

test('lineCount ignores a final newline, with LF or CRLF', () => {
  assert.equal(lineCount('a\nb\n'), 2);
  assert.equal(lineCount('a\r\nb\r\n'), 2);
  assert.equal(lineCount('a\nb'), 2);
  assert.equal(lineCount(''), 0);
});

test('docsLinkCount counts docs.umbraco.com links only', () => {
  assert.equal(docsLinkCount(`${LINK} ${LINK} https://learn.microsoft.com/x`), 2);
  assert.equal(docsLinkCount('no links here'), 0);
});

test('hasContentsIndex needs a Contents heading near the top with at least two anchor links', () => {
  assert.equal(hasContentsIndex(INDEX), true);
  assert.equal(hasContentsIndex('# Title\n\n## Contents\n\n- [Setup](#setup): only one\n\n## Setup\n'), false);
  assert.equal(hasContentsIndex('# Title\n\n## Setup\n- [a](#a)\n- [b](#b)\n'), false, 'no Contents heading');
  assert.equal(hasContentsIndex(`${longBody(40)}\n## Contents\n- [a](#a)\n- [b](#b)\n`), false, 'too far down');
  assert.equal(hasContentsIndex('## Contents\n- [a](https://x.com)\n- [b](https://y.com)\n'), false, 'external links');
});

test('a skill with no docs link anywhere is reported; one in a reference is enough', () => {
  const none = skillTree({ 'SKILL.md': '# s', 'references/a.md': 'no links' });
  assert.match(checkSkill(none).join('\n'), /no Umbraco docs link/);
  const inRef = skillTree({ 'SKILL.md': '# s', 'references/a.md': LINK });
  assert.deepEqual(checkSkill(inRef), []);
});

test('docs links in evals/ do not count', () => {
  const tree = skillTree({ 'SKILL.md': '# s', 'evals/README.md': LINK });
  assert.match(checkSkill(tree).join('\n'), /no Umbraco docs link/);
});

test('a reference over 100 lines needs a Contents index; 100 lines or fewer does not', () => {
  const long = skillTree({ 'SKILL.md': LINK, 'references/long.md': longBody(101) });
  assert.match(checkSkill(long).join('\n'), /long\.md: 101 lines and no "## Contents" index/);
  const indexed = skillTree({ 'SKILL.md': LINK, 'references/long.md': `${INDEX}${longBody(120)}` });
  assert.deepEqual(checkSkill(indexed), []);
  const short = skillTree({ 'SKILL.md': LINK, 'references/short.md': `${longBody(100)}\n` });
  assert.deepEqual(checkSkill(short), []);
});

// The output depends on GITHUB_ACTIONS, which CI sets, so each run states it rather than inheriting it.
const outside = { ...process.env, GITHUB_ACTIONS: '' };

test('the CLI warns with exit 0, and fails with --strict', () => {
  const root = skillTree({ 'one/SKILL.md': '# s', 'one/references/a.md': 'no links' });
  const out = execFileSync(process.execPath, [CLI, root], { encoding: 'utf8', env: outside });
  assert.match(out, /^warning: one: no Umbraco docs link/m);
  let code = 0;
  try {
    execFileSync(process.execPath, [CLI, '--strict', root], { stdio: 'pipe', env: outside });
  } catch (error) {
    code = error.status;
  }
  assert.equal(code, 1);
});

test('in GitHub Actions the warnings are annotations, so they show on the PR', () => {
  const root = skillTree({ 'one/SKILL.md': '# s', 'one/references/a.md': 'no links' });
  const env = { ...process.env, GITHUB_ACTIONS: 'true' };
  const out = execFileSync(process.execPath, [CLI, root], { encoding: 'utf8', env });
  assert.match(out, /^::warning::one: no Umbraco docs link/m);
});
