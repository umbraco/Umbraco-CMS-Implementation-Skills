#!/usr/bin/env node
// Validate published skills against the open Agent Skills (SKILL.md) spec.
//
// Checks every skill under plugins/*/skills/*/SKILL.md for:
//   - parseable YAML frontmatter
//   - required fields: name, description
//   - name matches its folder name; lowercase alphanumeric + hyphens; <= 64 chars
//   - description non-empty; <= 1024 chars
//   - no non-portable (tool-specific) frontmatter keys
//   - relative links/references in the body resolve to real files in the skill dir
//   - no stray skill folders missing a SKILL.md
//   - no symlinks inside published skills (Windows-hostile)
//   - marketplace.json / plugin.json parse and reference real plugin dirs
//
// Repo-authoring skills under .claude/skills/ are not published, so they are exempt
// from the portability rules — but their frontmatter is still checked for parseable
// YAML and a name/description that matches the folder. Invalid YAML there is silently
// ignored by the tools that read it (the Vercel Skills CLI drops such a skill without
// an error), so it needs to fail loudly here instead.
//
// Also maintains the skills index in AGENTS.md between the SKILLS-INDEX markers:
//   node scripts/validate-skills.mjs --write-index   # regenerate index
//   node scripts/validate-skills.mjs --check-index   # fail if index is stale (CI)
//
// Exit code 0 = all good, 1 = validation errors found.
// No dependencies — frontmatter is read by ./frontmatter.mjs.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { parseFrontmatter } from "./frontmatter.mjs";

const DEFAULT_REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

/** Everything derived from the repo root, so tests can point at a fixture. */
function context(repoRoot) {
  return {
    repoRoot,
    pluginsDir: path.join(repoRoot, "plugins"),
    authoringSkillsDir: path.join(repoRoot, ".claude", "skills"),
    agentsMd: path.join(repoRoot, "AGENTS.md"),
  };
}

const INDEX_START = "<!-- SKILLS-INDEX:START";
const INDEX_END = "<!-- SKILLS-INDEX:END -->";
const WRITE_INDEX_CMD = "node scripts/validate-skills.mjs --write-index";

// Open-standard fields (agentskills.io). Anything else is flagged so a skill
// never silently depends on one tool's extension.
const ALLOWED_FRONTMATTER_KEYS = ["allowed-tools", "description", "license", "metadata", "name"];

const NAME_RE = /^[a-z0-9]+(-[a-z0-9]+)*$/;
const NAME_MAX = 64;
const DESCRIPTION_MAX = 1024;

// Markdown links [text](target).
const MD_LINK_RE = /\[[^\]]*\]\(([^)\s]+)\)/g;

const WHEN_TO_USE_CUES = ["use when", "use this", "use for", "trigger", "use whenever"];

class Reporter {
  errors = [];
  warnings = [];

  constructor(repoRoot) {
    this.repoRoot = repoRoot;
  }

  #rel(p) {
    return path.relative(this.repoRoot, p).split(path.sep).join("/");
  }

  error(p, msg) {
    this.errors.push(`ERROR   ${this.#rel(p)}: ${msg}`);
  }

  warn(p, msg) {
    this.warnings.push(`WARNING ${this.#rel(p)}: ${msg}`);
  }
}

// -------------------------------------------------------------- fs helpers

function isDir(p) {
  try {
    return fs.statSync(p).isDirectory();
  } catch {
    return false;
  }
}

function isFile(p) {
  try {
    return fs.statSync(p).isFile();
  } catch {
    return false;
  }
}

/** Immediate subdirectories, sorted by name. */
function subdirs(p) {
  if (!isDir(p)) return [];
  return fs
    .readdirSync(p, { withFileTypes: true })
    .filter((e) => e.isDirectory())
    .map((e) => path.join(p, e.name))
    .sort();
}

function isEmptyDir(p) {
  return fs.readdirSync(p).length === 0;
}

/** Walk a directory WITHOUT following symlinks, yielding every entry path. */
function* walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    yield { full, entry };
    // Recurse into real directories only — never through a symlink.
    if (entry.isDirectory() && !entry.isSymbolicLink()) yield* walk(full);
  }
}

// ------------------------------------------------------------- validations

function readFrontmatter(skillMd, rep) {
  const text = fs.readFileSync(skillMd, "utf8");
  const result = parseFrontmatter(text);
  if (!result.ok) {
    const msg = result.error.startsWith("missing YAML frontmatter")
      ? result.error
      : `frontmatter is not valid YAML: ${result.error}`;
    rep.error(skillMd, msg);
    return null;
  }
  if (result.data === null || typeof result.data !== "object" || Array.isArray(result.data)) {
    rep.error(skillMd, "frontmatter must be a YAML mapping");
    return null;
  }
  return result;
}

function validateFrontmatter(skillMd, fm, folderName, rep) {
  const name = fm.name;
  const description = fm.description;

  if (typeof name !== "string" || name.trim() === "") {
    rep.error(skillMd, "frontmatter 'name' is required and must be a non-empty string");
  } else {
    if (name !== folderName) {
      rep.error(skillMd, `name '${name}' does not match folder name '${folderName}'`);
    }
    if (name.length > NAME_MAX) {
      rep.error(skillMd, `name exceeds ${NAME_MAX} characters (${name.length})`);
    }
    if (!NAME_RE.test(name)) {
      rep.error(
        skillMd,
        "name must be lowercase letters/digits with single hyphens (e.g. 'document-types')",
      );
    }
  }

  if (typeof description !== "string" || description.trim() === "") {
    rep.error(skillMd, "frontmatter 'description' is required and must be a non-empty string");
  } else {
    if (description.length > DESCRIPTION_MAX) {
      rep.error(
        skillMd,
        `description exceeds ${DESCRIPTION_MAX} characters (${description.length})`,
      );
    }
    const lowered = description.toLowerCase();
    if (!WHEN_TO_USE_CUES.some((cue) => lowered.includes(cue))) {
      rep.warn(
        skillMd,
        "description has no obvious 'when to use' cue — agents rely on this to decide whether to load the skill",
      );
    }
  }

  const unknown = Object.keys(fm)
    .filter((k) => !ALLOWED_FRONTMATTER_KEYS.includes(k))
    .sort();
  if (unknown.length > 0) {
    rep.error(
      skillMd,
      `non-portable frontmatter key(s) ${fmtList(unknown)} — published skills may only use ${fmtList(ALLOWED_FRONTMATTER_KEYS)}`,
    );
  }
}

/** Render a list the way Python prints sorted(set) — ['a', 'b'] */
function fmtList(items) {
  return `[${items.map((i) => `'${i}'`).join(", ")}]`;
}

function validateBodyLinks(skillMd, body, skillDir, rep) {
  const resolvedSkillDir = path.resolve(skillDir);
  for (const match of body.matchAll(MD_LINK_RE)) {
    const target = match[1];
    if (/^(https?:\/\/|mailto:|#)/.test(target)) continue;
    const clean = target.split("#", 1)[0];
    if (clean === "") continue;

    const resolved = path.resolve(skillDir, clean);
    const inside =
      resolved === resolvedSkillDir || resolved.startsWith(resolvedSkillDir + path.sep);
    if (!inside) {
      rep.warn(
        skillMd,
        `link '${target}' points outside the skill folder — bundled resources should live within it`,
      );
      continue;
    }
    if (!fs.existsSync(resolved)) {
      rep.error(skillMd, `link '${target}' does not resolve to a file in the skill folder`);
    }
  }
}

function validateNoSymlinks(skillDir, rep) {
  for (const { full, entry } of walk(skillDir)) {
    if (entry.isSymbolicLink()) {
      rep.error(full, "symlink inside a published skill — breaks Windows checkouts; commit real files");
    }
  }
}

function validateManifests(ctx, rep) {
  const marketplace = path.join(ctx.repoRoot, ".claude-plugin", "marketplace.json");
  const candidates = [];
  if (fs.existsSync(marketplace)) candidates.push(marketplace);
  for (const pluginDir of subdirs(ctx.pluginsDir)) {
    const pluginJson = path.join(pluginDir, ".claude-plugin", "plugin.json");
    if (isFile(pluginJson)) candidates.push(pluginJson);
  }
  for (const manifest of candidates) {
    try {
      JSON.parse(fs.readFileSync(manifest, "utf8"));
    } catch (err) {
      rep.error(manifest, `invalid JSON: ${err.message}`);
    }
  }
}

/**
 * Check repo-authoring skills under .claude/skills/ are at least loadable.
 *
 * These are not published, so the portability rules (allowed frontmatter keys,
 * no symlinks, resolvable links) deliberately do not apply — they may use
 * Claude-specific features. What does apply is that the frontmatter parses and
 * carries a matching name/description: tools that read SKILL.md skip a skill
 * with malformed frontmatter silently, so it must fail here instead.
 *
 * @returns the number of authoring skills checked.
 */
function validateAuthoringSkills(ctx, rep) {
  if (!isDir(ctx.authoringSkillsDir)) return 0;

  let checked = 0;
  for (const skillDir of subdirs(ctx.authoringSkillsDir)) {
    const skillMd = path.join(skillDir, "SKILL.md");
    if (!isFile(skillMd)) {
      if (!isEmptyDir(skillDir)) {
        rep.error(skillDir, "authoring skill folder has no SKILL.md — it will not be discovered");
      }
      continue;
    }

    const parsed = readFrontmatter(skillMd, rep);
    if (parsed === null) continue;
    const fm = parsed.data;
    checked += 1;

    const name = fm.name;
    if (typeof name !== "string" || name.trim() === "") {
      rep.error(skillMd, "frontmatter 'name' is required and must be a non-empty string");
    } else if (name !== path.basename(skillDir)) {
      rep.error(skillMd, `name '${name}' does not match folder name '${path.basename(skillDir)}'`);
    }

    const description = fm.description;
    if (typeof description !== "string" || description.trim() === "") {
      rep.error(skillMd, "frontmatter 'description' is required and must be a non-empty string");
    }
  }

  return checked;
}

/** Return validated skill metadata for the index; report problems as we go. */
function collectSkills(ctx, rep) {
  const skills = [];
  if (!isDir(ctx.pluginsDir)) return skills;

  for (const pluginDir of subdirs(ctx.pluginsDir)) {
    const skillsRoot = path.join(pluginDir, "skills");
    if (!isDir(skillsRoot)) continue;

    for (const skillDir of subdirs(skillsRoot)) {
      const skillMd = path.join(skillDir, "SKILL.md");
      if (!isFile(skillMd)) {
        // A folder with content but no SKILL.md is invisible to every agent.
        if (!isEmptyDir(skillDir)) {
          rep.error(skillDir, "skill folder has no SKILL.md — it will not be discovered by any agent");
        }
        continue;
      }

      const parsed = readFrontmatter(skillMd, rep);
      if (parsed === null) continue;
      const { data: fm, body } = parsed;

      validateFrontmatter(skillMd, fm, path.basename(skillDir), rep);
      validateBodyLinks(skillMd, body, skillDir, rep);
      validateNoSymlinks(skillDir, rep);

      skills.push({
        plugin: path.basename(pluginDir),
        name: String(fm.name ?? path.basename(skillDir)),
        description: String(fm.description ?? "").trim(),
        path: path.relative(ctx.repoRoot, skillMd).split(path.sep).join("/"),
      });
    }
  }
  return skills;
}

// ------------------------------------------------------------------- index

function renderIndex(skills) {
  if (skills.length === 0) {
    return (
      "_No skills published yet. This section is regenerated automatically when skills\n" +
      "are added under `plugins/*/skills/`._"
    );
  }
  const lines = ["| Skill | Plugin | Use when |", "| --- | --- | --- |"];
  for (const s of skills) {
    let desc = s.description.replaceAll("|", "\\|").replaceAll("\n", " ");
    if (desc.length > 200) desc = `${desc.slice(0, 197)}...`;
    lines.push(`| [\`${s.name}\`](${s.path}) | \`${s.plugin}\` | ${desc} |`);
  }
  return lines.join("\n");
}

function spliceIndex(agentsText, indexBlock) {
  const start = agentsText.indexOf(INDEX_START);
  const end = agentsText.indexOf(INDEX_END);
  if (start === -1 || end === -1 || end < start) return null;
  const markerLineEnd = agentsText.indexOf("\n", start) + 1;
  return agentsText.slice(0, markerLineEnd) + indexBlock + "\n" + agentsText.slice(end);
}

// -------------------------------------------------------------------- main

/**
 * Validate the repo and optionally manage the AGENTS.md index.
 *
 * @param {object} [options]
 * @param {string} [options.repoRoot] Root to validate; defaults to this repo.
 * @param {boolean} [options.writeIndex] Rewrite the AGENTS.md skills index.
 * @param {boolean} [options.checkIndex] Fail if the index is stale.
 * @param {(line: string) => void} [options.log] Sink for output lines.
 * @returns {{exitCode: number, errors: string[], warnings: string[],
 *            publishedCount: number, authoringCount: number}}
 */
export function run({
  repoRoot = DEFAULT_REPO_ROOT,
  writeIndex = false,
  checkIndex = false,
  log = console.log,
} = {}) {
  const ctx = context(repoRoot);
  const rep = new Reporter(repoRoot);

  validateManifests(ctx, rep);
  const skills = collectSkills(ctx, rep);
  const authoringCount = validateAuthoringSkills(ctx, rep);

  if (writeIndex || checkIndex) {
    if (!fs.existsSync(ctx.agentsMd)) {
      rep.error(ctx.agentsMd, "AGENTS.md not found — cannot manage skills index");
    } else {
      const current = fs.readFileSync(ctx.agentsMd, "utf8");
      const updated = spliceIndex(current, renderIndex(skills));
      if (updated === null) {
        rep.error(ctx.agentsMd, "SKILLS-INDEX markers missing or malformed");
      } else if (writeIndex && updated !== current) {
        fs.writeFileSync(ctx.agentsMd, updated, "utf8");
        log(`Updated skills index in ${path.relative(repoRoot, ctx.agentsMd)}`);
      } else if (checkIndex && updated !== current) {
        rep.error(ctx.agentsMd, `skills index is stale — run: ${WRITE_INDEX_CMD}`);
      }
    }
  }

  for (const w of rep.warnings) log(w);
  for (const e of rep.errors) log(e);

  log(
    `\nValidated ${skills.length} published skill(s) ` +
      `and ${authoringCount} authoring skill(s): ` +
      `${rep.errors.length} error(s), ${rep.warnings.length} warning(s)`,
  );

  return {
    exitCode: rep.errors.length > 0 ? 1 : 0,
    errors: rep.errors,
    warnings: rep.warnings,
    publishedCount: skills.length,
    authoringCount,
  };
}

function main(argv) {
  const known = ["--write-index", "--check-index"];
  const unknownFlags = argv.filter((a) => !known.includes(a));
  if (unknownFlags.length > 0) {
    console.error(`Unknown argument(s): ${unknownFlags.join(", ")}`);
    console.error(`Usage: node scripts/validate-skills.mjs [${known.join("] [")}]`);
    return 2;
  }
  return run({
    writeIndex: argv.includes("--write-index"),
    checkIndex: argv.includes("--check-index"),
  }).exitCode;
}

// Only act as a CLI when executed directly, so tests can import run().
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main(process.argv.slice(2)));
}
