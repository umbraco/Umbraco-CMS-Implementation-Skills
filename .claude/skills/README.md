# `.claude/skills`

This directory holds **project-local Claude Code skills** used to *author and maintain*
the skills that ship in this marketplace — not the published skills themselves.

The published skills live in the plugins:

- `plugins/content-modelling/skills/`
- `plugins/implementation/skills/`

## What goes here

Put the **`skill-creator`** skill in this directory. It's the tooling skill used to
scaffold, edit, evaluate, and optimise the marketplace skills, so it belongs alongside
the repo rather than in a shipped plugin.

```
.claude/skills/
└── skill-creator/
    └── SKILL.md
```

Add any other repo-authoring helper skills here too (validators, analyzers, etc.).
Keep skills that are meant for end users in the plugin `skills/` folders instead.
