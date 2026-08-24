# Umbraco CMS Implementation Skills Marketplace

> **Experimental Beta:** This project is an exploration of what's possible with Skills for Umbraco. It's evolving as we learn what works best.

A Claude Code plugin marketplace with skills for Umbraco **content modelling** and **implementation**, plus a committed [reference Umbraco instance](#reference-instance) for validating that skill output actually builds and runs.

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

Most supported editors read skills from a **shared `.agents/skills/` directory** rather
than an editor-specific one, so one install typically covers your whole team — see
[Where skills get installed](#where-skills-get-installed).

> **Note:** The `-a` flag targets a specific editor. Without it, the CLI installs once
> into the shared `.agents/skills/` directory and symlinks `.claude/skills/` to it — it
> does *not* copy the skills into every agent's directory.

Install all skills for your editor:
```bash
# For Cursor
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a cursor

# For GitHub Copilot
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a github-copilot

# For Devin CLI
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a devin

# For Devin Desktop (formerly Windsurf)
npx skills add umbraco/Umbraco-CMS-Implementation-Skills --skill '*' -a windsurf
```

### Where skills get installed

`.agents/skills/` is the **recommended** portable, cross-agent location. Devin,
Devin Desktop/Windsurf, Cursor, GitHub Copilot, Codex, Gemini CLI, OpenCode and
many others can discover skills there. You generally don't need a per-agent copy:

```
your-project/
└── .agents/
    └── skills/
        ├── umbraco-sitemap/SKILL.md
        └── umbraco-custom-error-pages/SKILL.md
```

| Editor | Minimum Version | Installs to |
|--------|----------------|-------------|
| **Cursor** | 2.4+ (January 2026) | `.agents/skills/` |
| **GitHub Copilot** (VS Code) | VS Code 1.109+ (January 2026) | `.agents/skills/` |
| **GitHub Copilot** (Coding Agent) | Supported | `.agents/skills/` |
| **Codex / Gemini CLI / OpenCode** | Current | `.agents/skills/` |
| **Claude Code** | Current (use Quick Start above) | `.claude/skills/` |
| **Devin CLI** | Current | `.agents/skills/` |
| **Devin Desktop (formerly Windsurf)** | Current | `.agents/skills/` |

**Claude Code exception.** `-a claude-code` writes to `.claude/skills/`; Devin,
Devin Desktop (formerly Windsurf), and the other agents in the table use the
shared `.agents/skills/` location.

If you omit `-a`, the CLI uses its detected agents or prompts for selection; the
shared location is `.agents/skills/`.

Cursor additionally reads `.cursor/skills/`, `.claude/skills/` and `.codex/skills/`, so
skills already installed for another agent are usually picked up without reinstalling.

Editors load skills **on-demand** — they read only each skill's `name` and `description`
up front, then load the full `SKILL.md` when it matches what you're working on. That is
why a skill's `description` matters: it is the only text the agent sees when deciding
whether the skill is relevant.

Skills are installed as **copies**, pinned in a `skills-lock.json`. To pick up changes
after this repo is updated:

```bash
npx skills update
```

VS Code users who would rather track this repo live than hold copies can clone it once
and point [`chat.agentSkillsLocations`](https://code.visualstudio.com/docs/agent-customization/agent-skills)
at the checkout instead.

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

## Reference instance

`Umbraco-CMS.Skills/` (with `Umbraco-CMS.Skills.sln` at the repo root) is a real Umbraco
**17** web project used to validate that a skill's output actually compiles and serves — the
runtime counterpart to the LLM-based `umbraco-skill-evaluator`.

- Targets `net10.0`, `Umbraco.Cms 17.*` (matching the skills' "Umbraco 17+" target).
- Installs **unattended** on first boot into a SQLite database.
- Ships the **[Clean](https://github.com/prjseal/Clean-Starter-Kit-for-Umbraco-v9)** starter
  kit, so there is real content (Document Types, templates, published pages) for skill output
  to run against.

Only the project scaffolding is committed — the runtime SQLite DB and build output are
`.gitignore`d, and Clean re-installs on first boot.

**How it was scaffolded** (via the [Package Script Writer CLI](https://github.com/prjseal/Package-Script-Writer-CLI), the same tool the backoffice skills use — regenerate with this if you ever need to rebuild it from scratch):

```bash
dotnet tool install --global PackageScriptWriter.Cli   # if not already installed
psw --default \
    -n "Umbraco-CMS.Skills" -s "Umbraco-CMS.Skills" \
    -k "Clean|7.0.8" \
    -da \
    --database-type SQLite \
    --template-version 17.5.3 \
    --admin-email admin@example.com --admin-password 1234567890 \
    --auto-run --build-only
```

Package versions are managed centrally in `Umbraco-CMS.Skills/Directory.Packages.props`.
(The committed project also has the launch URL set to `https://localhost:44372`.)

**Run it:**

```bash
cd Umbraco-CMS.Skills
dotnet run
```

Then open the backoffice at **https://localhost:44372/umbraco** and log in with
**admin@example.com** / **1234567890**.

**Validate a skill against it — deterministically.** Runtime validation is a **`dotnet test`
gate** (no LLM, reproducible): each validated approach ships an `examples/<approach>/` project that
compiles the skill's own assets into a reference instance, and a test host boots that instance
in-process (`WebApplicationFactory`) and asserts the skill's endpoints over HTTP. There are **two**
instances — one with the Clean starter kit for approaches written as C#, one with no starter kit for
approaches written as Document Types + templates + config, where Clean would otherwise supply a
competing implementation of the feature under test.

```bash
# Two hosts, tested as separate processes (Umbraco keeps process-wide static state).
dotnet build Umbraco-CMS.Skills.sln
dotnet test Umbraco-CMS.Skills.TestHost/Umbraco-CMS.Skills.TestHost.csproj --no-build
dotnet test Umbraco-CMS.Skills.TestHost.Blank/Umbraco-CMS.Skills.TestHost.Blank.csproj --no-build

python3 scripts/generate-examples.py --lint   # every placeholder an asset carries is declared
```

This runs in CI (`.github/workflows/validate-skills.yml`). For interactive poking or
backoffice-dependent steps, the `umbraco-reference-instance` authoring skill (in
`.claude/skills/`) also offers a manual boot/`try` harness. See
[`.claude/skills/umbraco-reference-instance/SKILL.md`](.claude/skills/umbraco-reference-instance/SKILL.md).

---

## Project Structure

```
Umbraco-CMS-Implementation-Skills/
├── .claude-plugin/marketplace.json      # Marketplace manifest (lists both plugins)
├── plugins/
│   ├── content-modelling/               # Content modelling plugin
│   │   ├── .claude-plugin/plugin.json
│   │   └── skills/                      # Published skills
│   └── implementation/                  # Implementation plugin
│       ├── .claude-plugin/plugin.json
│       └── skills/<skill>/
│           ├── SKILL.md, assets/, …      # the skill (assets = the shipped source of truth)
│           └── examples/<approach>/      # per-approach validation project (generated at build time)
├── Umbraco-CMS.Skills/                  # Reference instance 1 — Umbraco 17 WITH the Clean starter kit
├── Umbraco-CMS.Skills.Blank/            # Reference instance 2 — no starter kit (content-shaped approaches)
├── Umbraco-CMS.Skills.TestHost/         # dotnet test: boots instance 1, HTTP-asserts each skill
├── Umbraco-CMS.Skills.TestHost.Blank/   # dotnet test: boots instance 2 (own process)
├── TestHost.Shared/                     # boot/wait/preconditions code linked into both test hosts
├── Umbraco-CMS.Skills.sln
├── scripts/generate-examples.py         # projects assets/ into each example at build time
└── .claude/
    └── skills/                          # Repo-authoring skills (evaluator, reference-instance)
```

## Contributing

Skills are added under the relevant plugin's `skills/` folder, each as a directory
containing a `SKILL.md`.

[AGENTS.md](AGENTS.md) is the entry point for anyone working on this repo — human or
agent, whichever editor you use. It covers the skill-authoring rules, what keeps a
skill portable across agents, and the validation gates. `CLAUDE.md` imports it and adds
only the Claude Code-specific parts.

Two authoring skills (in `.claude/skills/`, not published) help maintain the
marketplace:
[`umbraco-skill-author`](.claude/skills/umbraco-skill-author/SKILL.md) scaffolds and
writes a skill, and
[`umbraco-skill-evaluator`](.claude/skills/umbraco-skill-evaluator/SKILL.md) runs the
eval loop over it.

Changes land via branch → pull request → squash-merge into `main`.

## License

MIT

## Credits

Built by Phil W ([@hifi-phil](https://github.com/hifi-phil)).

Skills based on [Umbraco CMS](https://umbraco.com/) documentation.
