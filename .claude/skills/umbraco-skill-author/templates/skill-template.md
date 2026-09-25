# Skill templates

Skeletons for a new skill. Copy-paste and replace every `<Placeholder>`. Delete parts you don't
need (a single-approach skill may need no `references/` and no `assets/`).

## Folder

```
plugins/<plugin>/skills/<skill-name>/
├── SKILL.md
├── references/<approach>.md      # one per approach (optional)
├── assets/<File>.cs              # code templates with <Placeholder> tokens (optional)
├── scripts/<helper>.mjs          # deterministic helpers the skill runs (optional)
├── evals/evals.json
└── templates/<skeleton>.md       # copy-paste skeletons for authoring (optional)
```

## SKILL.md skeleton

```markdown
---
name: <skill-name>
description: >
  <What it does in one line>. Use this whenever the user asks to <trigger phrase>,
  <another real user wording>, or <adjacent goal they'd phrase it as>.
  SKIP: <non-applicable cases, e.g. non-Umbraco projects or Umbraco < X>.
---

# <Skill Title>

<One-paragraph intro.>

<!-- Only if more than one approach: -->
| | **A — <name>** (default) | **B — <name>** |
|---|---|---|
| Reference | [approach-a.md](references/approach-a.md) | [approach-b.md](references/approach-b.md) |
| Source | <custom code / official docs> | <...> |
| Best for | <...> | <...> |

### How to decide
Default to A. Choose B when <condition>. Prefer the Umbraco Developer MCP; if unavailable, walk
the user through it manually — only fall back when <condition>.

## Version compatibility
Targets **Umbraco <X>+**. <Any "introduced in vX" API notes.>

## Best practices
- <Domain guidance, not just restated code.>

## Validation
Objective assertions live in [`evals/evals.json`](evals/evals.json); run them with
`umbraco-skill-evaluator`.
```

## references/<approach>.md skeleton

```markdown
# Approach <X> — <name>

<One-line summary.> Choose this when <condition>; otherwise use [the alternative](<other>.md).

## Building blocks (fetch the docs before implementing)
- **<concept>** — <role>: [<title>](https://docs.umbraco.com/.../<page>.md)

## Steps
1. Discover project context (namespace, version, folder conventions).
2. Write files, substituting <Placeholder> tokens.
3. Build/verify (or state honestly if no buildable project exists).

## Done
Tell the user: <where it lives, how to use it, any config to add>.
```

## evals/evals.json skeleton

```json
{
  "skill_name": "<skill-name>",
  "evals": [
    {
      "id": 1,
      "prompt": "<realistic, multi-step user request with context>",
      "expected_output": "<what a correct run produces>",
      "files": [],
      "expectations": [
        "<objective, checkable assertion>",
        "If a real buildable project is available, the build succeeds; if not, the agent does not falsely claim a verified build"
      ]
    }
  ]
}
```
