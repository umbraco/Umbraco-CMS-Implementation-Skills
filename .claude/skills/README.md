# `.claude/skills`

This directory holds **project-local Claude Code skills** used to *author, validate and
maintain* the skills that ship in this marketplace — not the published skills themselves.
Nothing here ships in a plugin.

The published skills live in the plugins:

- `plugins/content-modelling/skills/`
- `plugins/implementation/skills/`

## What's here

Each of these answers a different question, and none of them covers for another:

| Skill | Answers |
|-------|---------|
| `umbraco-skill-author` | How should this skill be **shaped**? House conventions, authoring steps, templates, and the conformance checklist. Start here. |
| `umbraco-reference-instance` | Does the skill's code **compile and serve** in a real Umbraco? The `dotnet test` gate, plus a manual boot/`try` harness. |
| `umbraco-skill-evaluator` | Does Claude actually **write** that code when the skill is loaded? The with-skill vs. baseline eval loop. |

```
.claude/skills/
├── umbraco-skill-author/
│   ├── SKILL.md
│   ├── references/          # authoring-steps, conformance-checklist, runtime-validation
│   └── templates/
├── umbraco-reference-instance/
│   ├── SKILL.md
│   └── scripts/
└── umbraco-skill-evaluator/
    ├── SKILL.md
    ├── agents/              # grader
    ├── references/
    ├── scripts/
    └── eval-viewer/
```

The contribution bar these enforce is summarised in [`CLAUDE.md`](../../CLAUDE.md#skill-development--contribution).

## What goes here

Add any other repo-authoring helper skills here too — validators, analyzers, generators.
Keep skills meant for end users in the plugin `skills/` folders instead.
