# Umbraco CMS Implementation Skills Marketplace

> **Experimental Beta:** This project is an exploration of what's possible with Skills for Umbraco. It's evolving as we learn what works best.

A Claude Code plugin marketplace with skills for Umbraco **content modelling** and **implementation**.

> **Status:** Scaffold. The plugin `skills/` folders are empty and ready to be populated.

## Plugins

| Plugin | Focus |
|--------|-------|
| `umbraco-cms-content-modelling-skills` | Document types, element types, data types, compositions, content structure |
| `umbraco-cms-implementation-skills` | Site build-out, templates, views, controllers, delivery |

## Quick Start

Add the marketplace:
```bash
/plugin marketplace add umbraco/Umbraco-CMS-Implementation-Skills
```

Install the plugins:
```bash
# Content modelling skills
/plugin install umbraco-cms-content-modelling-skills@umbraco-cms-implementation-marketplace

# Implementation skills
/plugin install umbraco-cms-implementation-skills@umbraco-cms-implementation-marketplace
```

---

## Install for Other Editors (Cursor, GitHub Copilot, Windsurf, and more)

These skills use the open [SKILL.md](https://agentskills.io/home) format, supported natively by multiple AI coding tools. Install them into any supported editor using the [Vercel Skills CLI](https://github.com/vercel-labs/skills).

> **Important:** Always use the `-a` flag to target your editor, otherwise skills will be symlinked into every supported agent directory.

Install all skills for your editor:
```bash
# For Cursor
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a cursor

# For GitHub Copilot
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a github-copilot

# For Windsurf
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a windsurf
```

### Editor Requirements

| Editor | Minimum Version | Skills Path |
|--------|----------------|-------------|
| **Cursor** | 2.4+ (January 2026) | `.cursor/skills/` |
| **GitHub Copilot** (VS Code) | VS Code 1.109+ (January 2026) | `.github/skills/` |
| **GitHub Copilot** (Coding Agent) | Supported | `.github/skills/` |
| **Windsurf** | Current | `.windsurf/skills/` |
| **Claude Code** | Current (use Quick Start above) | `.claude/skills/` |

All of these editors load skills **on-demand** — only the skill relevant to your current task is loaded into context.

---

## Best Practice: Add Source Code References

These skills work best when Claude has access to the Umbraco source code, so it can reference actual implementations, types, and conventions.

```bash
git clone https://github.com/umbraco/Umbraco-CMS.git
```

Add it as a working directory in Claude Code:
```bash
/add-dir /path/to/Umbraco-CMS
```

---

## Project Structure

```
Umbraco-CMS-Implementation-Skills/
├── .claude-plugin/marketplace.json      # Marketplace manifest (lists both plugins)
├── plugins/
│   ├── content-modelling/               # Content modelling plugin
│   │   ├── .claude-plugin/plugin.json
│   │   └── skills/                      # Published skills (empty for now)
│   └── implementation/                  # Implementation plugin
│       ├── .claude-plugin/plugin.json
│       └── skills/                      # Published skills (empty for now)
└── .claude/
    └── skills/                          # Repo-authoring skills (e.g. skill-creator)
```

## Contributing

Skills are added under the relevant plugin's `skills/` folder, each as a directory
containing a `SKILL.md`. The `skill-creator` skill (in `.claude/skills/`) is used to
scaffold and maintain them — see [.claude/skills/README.md](.claude/skills/README.md).

Changes land via branch → pull request → squash-merge into `main`.

## License

MIT

## Credits

Built by Phil W ([@hifi-phil](https://github.com/hifi-phil)).

Skills based on [Umbraco CMS](https://umbraco.com/) documentation.
