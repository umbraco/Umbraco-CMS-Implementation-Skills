# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this repo is

A **Claude Code plugin marketplace** providing Umbraco skills, split into two plugins:

- **`umbraco-cms-content-modelling-skills`** (`plugins/content-modelling/`) — document
  types, element types, data types, compositions, content structure.
- **`umbraco-cms-implementation-skills`** (`plugins/implementation/`) — site build-out,
  templates, views, controllers, delivery.

It is a sibling to the Umbraco Backoffice Skills marketplace and follows the same conventions.

## Structure

```
.claude-plugin/marketplace.json   # Marketplace manifest — lists both plugins
plugins/<plugin>/
  .claude-plugin/plugin.json       # Per-plugin manifest
  skills/<skill-name>/SKILL.md      # Published skills (one folder per skill)
.claude/skills/                    # Repo-authoring skills (NOT published) — e.g. skill-creator
```

### Published vs authoring skills

- **Published skills** ship to users and live in `plugins/*/skills/`.
- **Authoring skills** (tooling to create/validate/maintain skills, such as
  `skill-creator`) live in `.claude/skills/` and are not part of any plugin.

Don't put authoring tooling in a plugin's `skills/` folder, and don't put
user-facing skills in `.claude/skills/`.

## Conventions

- **Skill folders** are kebab-case and each contains a `SKILL.md` with YAML frontmatter
  (`name`, `description`). Match the structure of the Umbraco Backoffice Skills repo.
- **Versions** are kept in sync between `marketplace.json` and each plugin's `plugin.json`.
  When bumping a plugin version, update both.
- **Marketplace name:** `umbraco-cms-implementation-marketplace`.

## Workflow

Changes land via **branch → pull request → squash-merge into `main`**:

1. Branch off `main` (never commit directly to `main`).
2. Commit, push, open a PR with `gh pr create --base main`.
3. Address review, then `gh pr merge <n> --squash --delete-branch`.
4. `git checkout main && git pull --ff-only`.

Only commit/push/merge when explicitly asked.

## Source references

Skills are most accurate when the Umbraco source is available as a working directory:

```bash
/add-dir /path/to/Umbraco-CMS
```
