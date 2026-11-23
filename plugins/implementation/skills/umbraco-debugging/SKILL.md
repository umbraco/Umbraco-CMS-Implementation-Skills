---
name: umbraco-debugging
description: >
  Debug an Umbraco 17 site in two ways. Step into Umbraco's own source from Visual Studio, Rider or
  VS Code with Source Link and the NuGet.org symbol server, or switch on Umbraco debug mode to
  profile requests with MiniProfiler and your own IProfiler steps. Use this whenever the user wants
  to "step into Umbraco source", set up SourceLink or Source Link, press F11 into Umbraco code, fix
  "symbols not loaded", "skipped loading symbols" or "no source available" on Umbraco DLLs, see
  real source instead of decompiled code, turn off Just My Code, share that setup across a team,
  find out why a page is slow with MiniProfiler, use
  ?umbDebug=true (umbDebug), add IProfiler steps around their own code, or set
  Umbraco:CMS:Hosting:Debug (Hosting Debug). SKIP: Serilog and log viewer configuration, unit or
  integration testing, debugging the backoffice or its JavaScript client, and non-Umbraco projects
  or Umbraco 13 and earlier.
---

# Debugging

Two tools answer two different questions. **Source Link** lets your debugger load the PDBs Umbraco
publishes and download the matching source from GitHub, so you can set breakpoints in and F11 into
Umbraco itself. **Debug mode + MiniProfiler** shows where a request spends its time (your code,
Umbraco's code, SQL) without attaching a debugger at all.

| | **A — Step into Umbraco source** | **B — Debug mode + MiniProfiler / IProfiler** |
|---|---|---|
| Reference | [approach-a-sourcelink.md](references/approach-a-sourcelink.md) | [approach-b-debug-mode-profiler.md](references/approach-b-debug-mode-profiler.md) |
| Answers | "What is Umbraco doing inside this call, and why?" | "Why is this page slow, and which step is it?" |
| Where it runs | Your local IDE, with the site launched under the debugger | Any non-production environment, in the browser |
| What changes | IDE settings only (Source Link, symbol servers, Just My Code, JIT). Nothing in the project | `Umbraco:CMS:Hosting:Debug` in environment config, plus optional `IProfiler.Step` calls in your code |
| Ships | [`assets/SourceLinkProbe.cs`](assets/SourceLinkProbe.cs) (an F11 target), [`assets/vscode-launch.json`](assets/vscode-launch.json) | [`assets/ProfiledController.cs`](assets/ProfiledController.cs) (nested `IProfiler` steps) |

**Before answering or changing anything, open the reference for the approach you choose and start
from its asset.** The traps are covered only there: the per-IDE setting names, the symbol server
that Source Link depends on, and how profiling is gated.

### How to decide

- The user wants to **see inside an Umbraco method**: a breakpoint in Umbraco code, F11 from their
  code into `IContentService`, "symbols not loaded", decompiled code they'd rather read as real
  source. Use **A**.
- The user wants to know **where time goes**, or wants evidence from a shared dev, test or staging
  site where nobody can attach a debugger. Use **B**.
- **Both** is a normal answer: profile with B to find the slow step, then reproduce it locally and
  step into it with A. Say which half runs where: A on the developer's machine, B on a non-production
  environment. Give each half in full, following its reference's reply checklist, not a pointer to
  the other.
- If the question is vague ("help me debug this Umbraco site"), ask which of the two they need
  before changing anything.

## Version compatibility

Targets **Umbraco 17** (`net10.0`).

## Best practices

- **Never set `Umbraco:CMS:Hosting:Debug` to `true` in production, not even "temporarily"** through
  an environment variable or app setting, and never in the base `appsettings.json` that every
  environment inherits. Starting a profile has no login check, so on a live site any visitor could
  add `?umbDebug=true`. Put it in `appsettings.Development.json` or the specific non-production
  environment's config, and reproduce production problems there.

## Validation

Assertions live in [`evals/evals.json`](evals/evals.json); run them with `umbraco-skill-evaluator`.
[`evals/README.md`](evals/README.md) has the fixtures, the run notes, and the Haiku and Sonnet results
(with and without the skill). Runtime coverage:

| Part | Tier | Where |
|---|---|---|
| A — `assets/SourceLinkProbe.cs` | **Asserted** (Clean host) | `examples/approach-a-sourcelink-probe/` |
| A — `assets/vscode-launch.json` | **Documented** (IDE config; nothing to run) | — |
| B — `assets/ProfiledController.cs` | **Asserted** (Clean host) | `examples/approach-b-profiler/` |

The gate proves the assets compile and run on the pinned Umbraco. It cannot prove an IDE loaded
symbols or that MiniProfiler rendered for a given user; those stay manual checks.
