// Tests for the skill spec validator.
//
// Run: node --test scripts/*.test.mjs
//
// Each test builds a throwaway repo in a temp dir and runs the validator
// against it, so every check is exercised against a case that must fail as
// well as one that must pass. A validator nobody has watched fail is just a
// green tick.

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

import { run } from "./validate-skills.mjs";

const MARKER_START = "<!-- SKILLS-INDEX:START — generated; do not edit by hand -->";
const MARKER_END = "<!-- SKILLS-INDEX:END -->";
const AGENTS_MD = `# Agents\n\n## Skills index\n\n${MARKER_START}\nplaceholder\n${MARKER_END}\n\nTail.\n`;

/**
 * Materialise a repo from a {relativePath: contents} map. Directories are
 * created implicitly; a null value means "create an empty directory".
 */
function makeRepo(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "skills-validator-"));
  for (const [rel, contents] of Object.entries(files)) {
    const full = path.join(root, rel);
    if (contents === null) {
      fs.mkdirSync(full, { recursive: true });
      continue;
    }
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, contents, "utf8");
  }
  return root;
}

/** A published skill whose frontmatter is entirely valid. */
const goodSkill = (name = "good-skill", extraFrontmatter = "") =>
  `---\nname: ${name}\n${extraFrontmatter}description: >\n  Does a thing. Use when you need the thing done.\n---\n\nBody.\n`;

const BASE = {
  "AGENTS.md": AGENTS_MD,
  ".claude-plugin/marketplace.json": '{"name":"m","plugins":["./plugins/p"]}',
  "plugins/p/.claude-plugin/plugin.json": '{"name":"p"}',
};

function validate(files, options = {}) {
  const root = makeRepo({ ...BASE, ...files });
  const lines = [];
  const result = run({ repoRoot: root, log: (l) => lines.push(l), ...options });
  return { ...result, root, output: lines.join("\n") };
}

/** Assert exactly one error mentioning `fragment`, and nothing unexpected. */
function assertError(result, fragment) {
  const matching = result.errors.filter((e) => e.includes(fragment));
  assert.equal(
    matching.length,
    1,
    `expected one error containing ${JSON.stringify(fragment)}, got:\n${result.errors.join("\n")}`,
  );
  assert.equal(result.exitCode, 1);
}

// -------------------------------------------------------------- happy path

test("a clean repo passes with no errors or warnings", () => {
  const result = validate({ "plugins/p/skills/good-skill/SKILL.md": goodSkill() });
  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.warnings, []);
  assert.equal(result.exitCode, 0);
  assert.equal(result.publishedCount, 1);
});

// ------------------------------------------------------------- frontmatter

test("name must match the folder", () => {
  const result = validate({ "plugins/p/skills/good-skill/SKILL.md": goodSkill("other-name") });
  assertError(result, "does not match folder name 'good-skill'");
});

test("name must be lowercase kebab-case", () => {
  const result = validate({ "plugins/p/skills/Bad_Name/SKILL.md": goodSkill("Bad_Name") });
  assertError(result, "name must be lowercase letters/digits");
});

test("a name over 64 characters is an error", () => {
  const long = "a".repeat(65);
  const result = validate({ [`plugins/p/skills/${long}/SKILL.md`]: goodSkill(long) });
  assertError(result, "name exceeds 64 characters (65)");
});

test("a missing description is an error", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": "---\nname: good-skill\n---\nBody.\n",
  });
  assertError(result, "'description' is required");
});

test("a description over 1024 characters is an error", () => {
  const long = `Use when ${"x".repeat(1100)}`;
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": `---\nname: good-skill\ndescription: "${long}"\n---\nBody.\n`,
  });
  assertError(result, "description exceeds 1024 characters");
});

test("a description with no 'when to use' cue warns but does not fail", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: It does a thing.\n---\nBody.\n",
  });
  assert.deepEqual(result.errors, []);
  assert.equal(result.warnings.length, 1);
  assert.match(result.warnings[0], /no obvious 'when to use' cue/);
  assert.equal(result.exitCode, 0);
});

test("non-portable frontmatter keys are an error under plugins/", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": goodSkill("good-skill", "model: opus\n"),
  });
  assertError(result, "non-portable frontmatter key(s) ['model']");
});

test("the portable optional keys are accepted", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": goodSkill(
      "good-skill",
      "license: MIT\nallowed-tools:\n  - Read\nmetadata:\n  version: 1.0.0\n",
    ),
  });
  assert.deepEqual(result.errors, []);
});

test("unparseable frontmatter is an error, not a silent skip", () => {
  // The bug this validator exists for: an unquoted colon in a description.
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: cousin of skill-creator: it skips\n---\nBody.\n",
  });
  assertError(result, "frontmatter is not valid YAML");
  assert.equal(result.publishedCount, 0, "an unparseable skill must not reach the index");
});

// -------------------------------------------------------------------- links

test("a relative link that does not resolve is an error", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: Use when linking.\n---\n[x](./missing.md)\n",
  });
  assertError(result, "link './missing.md' does not resolve");
});

test("a resolvable relative link passes", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: Use when linking.\n---\n[x](./references/notes.md)\n",
    "plugins/p/skills/good-skill/references/notes.md": "notes\n",
  });
  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.warnings, []);
});

test("a link escaping the skill folder warns rather than errors", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: Use when linking.\n---\n[x](../../../../AGENTS.md)\n",
  });
  assert.deepEqual(result.errors, []);
  assert.match(result.warnings[0], /points outside the skill folder/);
});

test("absolute, mailto and anchor links are ignored", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md":
      "---\nname: good-skill\ndescription: Use when linking.\n---\n" +
      "[a](https://example.com) [b](http://example.com) [c](mailto:x@example.com) [d](#section)\n",
  });
  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.warnings, []);
});

// ------------------------------------------------------------------ folders

test("a skill folder with content but no SKILL.md is an error", () => {
  const result = validate({ "plugins/p/skills/orphan/README.md": "x\n" });
  assertError(result, "skill folder has no SKILL.md");
});

test("an empty skill folder is ignored", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": goodSkill(),
    "plugins/p/skills/empty": null,
  });
  assert.deepEqual(result.errors, []);
});

test("a symlink inside a published skill is an error", (t) => {
  const root = makeRepo({
    ...BASE,
    "plugins/p/skills/good-skill/SKILL.md": goodSkill(),
    "plugins/p/skills/good-skill/real.md": "real\n",
  });
  try {
    fs.symlinkSync("real.md", path.join(root, "plugins/p/skills/good-skill/link.md"));
  } catch {
    // Unprivileged Windows checkouts cannot create symlinks; the check still
    // matters there, which is precisely why it exists.
    t.skip("symlinks not permitted on this platform");
    return;
  }
  const result = run({ repoRoot: root, log: () => {} });
  assert.equal(result.exitCode, 1);
  assert.equal(result.errors.filter((e) => e.includes("symlink inside a published skill")).length, 1);
});

// --------------------------------------------------------- authoring skills

test("authoring skills may use tool-specific frontmatter keys", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": goodSkill(),
    ".claude/skills/authoring-one/SKILL.md":
      "---\nname: authoring-one\ndescription: Maintainer tooling.\nmodel: opus\nargument-hint: x\n---\nBody.\n",
  });
  assert.deepEqual(result.errors, []);
  assert.equal(result.authoringCount, 1);
});

test("an authoring skill with unparseable frontmatter still fails loudly", () => {
  // This is the asymmetry that matters: lenient on keys, strict on parseability.
  const result = validate({
    ".claude/skills/authoring-one/SKILL.md":
      "---\nname: authoring-one\ndescription: cousin of skill-creator: it skips\n---\nBody.\n",
  });
  assertError(result, "frontmatter is not valid YAML");
});

test("an authoring skill name must match its folder", () => {
  const result = validate({
    ".claude/skills/authoring-one/SKILL.md":
      "---\nname: wrong\ndescription: Maintainer tooling.\n---\nBody.\n",
  });
  assertError(result, "does not match folder name 'authoring-one'");
});

test("an authoring folder with content but no SKILL.md is an error", () => {
  const result = validate({ ".claude/skills/authoring-one/notes.md": "x\n" });
  assertError(result, "authoring skill folder has no SKILL.md");
});

test("authoring skills are not counted as published", () => {
  const result = validate({
    ".claude/skills/authoring-one/SKILL.md":
      "---\nname: authoring-one\ndescription: Maintainer tooling.\n---\nBody.\n",
  });
  assert.equal(result.publishedCount, 0);
  assert.equal(result.authoringCount, 1);
});

// -------------------------------------------------------------- manifests

test("invalid JSON in a manifest is an error", () => {
  const result = validate({
    "plugins/p/skills/good-skill/SKILL.md": goodSkill(),
    "plugins/p/.claude-plugin/plugin.json": "{not json",
  });
  assertError(result, "invalid JSON");
});

// ------------------------------------------------------------------ index

test("--write-index rewrites the index between the markers", () => {
  const { root } = validate(
    { "plugins/p/skills/good-skill/SKILL.md": goodSkill() },
    { writeIndex: true },
  );
  const text = fs.readFileSync(path.join(root, "AGENTS.md"), "utf8");
  assert.match(text, /\| Skill \| Plugin \| Use when \|/);
  assert.match(text, /\[`good-skill`\]\(plugins\/p\/skills\/good-skill\/SKILL\.md\)/);
  assert.doesNotMatch(text, /placeholder/);
  // Content outside the markers survives untouched.
  assert.match(text, /^# Agents\n/);
  assert.match(text, /Tail\.\n$/);
});

test("--check-index fails when the index is stale and passes once written", () => {
  const files = { "plugins/p/skills/good-skill/SKILL.md": goodSkill() };
  const root = makeRepo({ ...BASE, ...files });

  const stale = run({ repoRoot: root, checkIndex: true, log: () => {} });
  assert.equal(stale.exitCode, 1);
  assert.match(stale.errors[0], /skills index is stale/);

  run({ repoRoot: root, writeIndex: true, log: () => {} });
  const fresh = run({ repoRoot: root, checkIndex: true, log: () => {} });
  assert.deepEqual(fresh.errors, []);
  assert.equal(fresh.exitCode, 0);
});

test("--write-index is idempotent", () => {
  const root = makeRepo({ ...BASE, "plugins/p/skills/good-skill/SKILL.md": goodSkill() });
  run({ repoRoot: root, writeIndex: true, log: () => {} });
  const first = fs.readFileSync(path.join(root, "AGENTS.md"), "utf8");
  run({ repoRoot: root, writeIndex: true, log: () => {} });
  assert.equal(fs.readFileSync(path.join(root, "AGENTS.md"), "utf8"), first);
});

test("missing index markers are an error", () => {
  const result = validate(
    {
      "AGENTS.md": "# Agents\n\nNo markers here.\n",
      "plugins/p/skills/good-skill/SKILL.md": goodSkill(),
    },
    { checkIndex: true },
  );
  assertError(result, "SKILLS-INDEX markers missing or malformed");
});

test("a missing AGENTS.md is an error when managing the index", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "skills-validator-"));
  const result = run({ repoRoot: root, checkIndex: true, log: () => {} });
  assert.match(result.errors[0], /AGENTS\.md not found/);
});

test("an empty repo renders the placeholder index rather than a table", () => {
  const { root } = validate({}, { writeIndex: true });
  const text = fs.readFileSync(path.join(root, "AGENTS.md"), "utf8");
  assert.match(text, /_No skills published yet\./);
  assert.doesNotMatch(text, /\| Skill \| Plugin \|/);
});

test("a pipe in a description is escaped so the index table survives", () => {
  const { root } = validate(
    {
      "plugins/p/skills/good-skill/SKILL.md":
        '---\nname: good-skill\ndescription: "Use when a|b matters."\n---\nBody.\n',
    },
    { writeIndex: true },
  );
  const text = fs.readFileSync(path.join(root, "AGENTS.md"), "utf8");
  assert.match(text, /a\\\|b/);
});

test("a long description is truncated in the index", () => {
  const long = `Use when ${"x".repeat(400)}`;
  const { root } = validate(
    {
      "plugins/p/skills/good-skill/SKILL.md": `---\nname: good-skill\ndescription: "${long}"\n---\nBody.\n`,
    },
    { writeIndex: true },
  );
  const row = fs
    .readFileSync(path.join(root, "AGENTS.md"), "utf8")
    .split("\n")
    .find((l) => l.includes("good-skill"));
  assert.match(row, /\.\.\. \|$/);
});
