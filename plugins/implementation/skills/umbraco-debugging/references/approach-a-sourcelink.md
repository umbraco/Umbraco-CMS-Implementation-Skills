# Approach A — Step into Umbraco source with Source Link

Set a breakpoint in, or F11 into, Umbraco's own code from a local IDE, reading the real source of
the exact Umbraco version the site runs. Choose this when the question is "what is Umbraco doing
inside this call?". If the question is "where does the time go?", or the site is on a shared
environment you can't attach to, use [Approach B](approach-b-debug-mode-profiler.md) instead.

Everything here is **IDE configuration on the developer's machine**. Nothing in the project changes,
apart from the optional VS Code `launch.json` and the optional probe class.

## Contents

- [Fetch the docs first, then correct them](#fetch-the-docs-first-then-correct-them): read when you are about to follow Umbraco's SourceLink docs page and need to know where it is out of date for Umbraco 17.
- [Why Source Link alone is not enough](#why-source-link-alone-is-not-enough): read when F11 steps over Umbraco code, symbols do not load, or locals show as optimized away, and you need the settings that must all be true.
- [Steps](#steps): read when you are setting up the debugger for a specific IDE (Visual Studio, Rider or VS Code) and verifying that you can step into Umbraco source.
- [Troubleshooting](#troubleshooting): read when the setup is done but breakpoints do not hit, symbols are missing, or you see decompiled code.
- [Done](#done): read before replying, to check the answer covers every setting, the VS Code configuration, where to break and how to confirm.

## Fetch the docs first, then correct them

Umbraco's page is
[Debugging with SourceLink](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/testing-and-debugging/debugging.md).
Read it for the idea, but it is **stale for Umbraco 17**:

- It is written for **Visual Studio 2017 & 2019**, a **.NET 5** site and Umbraco 9. Umbraco 17 runs
  on `net10.0`, and current Visual Studio menus differ (see below).
- Its sample uses `IComponent` with `Initialize()`/`Terminate()`. `IComponent` is **obsolete** in 17
  and compiles with a CS0618 warning. Use a notification handler, as
  [`assets/SourceLinkProbe.cs`](../assets/SourceLinkProbe.cs) does.
- It never mentions **symbols** or **JIT optimisation**, which are the two settings that make or
  break this in practice (next section).

Don't reproduce its steps verbatim; use the per-IDE steps below.

## Why Source Link alone is not enough

Umbraco builds its packages with Source Link and publishes symbols as a **`.snupkg`** on NuGet.org.
The `.nupkg` you restore has **no PDB** next to `Umbraco.Core.dll`. So three things must all be true:

1. **The NuGet.org symbol server is enabled**, so the debugger can download the PDB. Without it no
   PDB loads, Source Link has nothing to read, and you get "symbols not loaded" or decompiled code.
2. **Source Link support is enabled**, so the debugger fetches the source file the PDB points at
   (a `raw.githubusercontent.com/umbraco/Umbraco-CMS/<commit>/...` URL for the exact build).
3. **Just My Code is off** (Rider: external source debugging on), so the debugger is willing to stop
   in a library at all.

Also **suppress JIT optimisation on module load**. The NuGet DLLs are Release builds, so without it
locals show as "optimized away" and stepping jumps around. This only affects modules loaded after
the debugger is attached, so **launch the site under the debugger (F5) rather than attaching** to a
running process. It also has no effect on ReadyToRun (precompiled) images; set the environment
variable `COMPlus_ReadyToRun=0` if you hit that.

Module filters match **DLL names**, not package ids: the assemblies are `Umbraco.Core.dll`,
`Umbraco.Infrastructure.dll`, `Umbraco.Web.Common.dll` and so on, so filter on `Umbraco.*.dll`, not
`Umbraco.Cms.*`.

## Steps

### 1. Discover project context

Find the site's web project (the one with `Program.cs` and a `PackageReference` to `Umbraco.Cms`),
its assembly name, and its target framework (`net10.0` on Umbraco 17). Ask which IDE the user runs:
Visual Studio, Rider or VS Code. Configure only that one.

### 2. Configure the IDE

#### Visual Studio 2022

All under **Tools** (or **Debug**) > **Options** > **Debugging**:

- **General**: clear **Enable Just My Code**. This is a global setting for every project.
- **General**: tick **Enable Source Link support**.
- **General**: tick **Suppress JIT optimization on module load (Managed only)**.
- **Symbols**: under *Symbol file (.pdb) search locations*, tick **NuGet.org Symbol Server** (and
  **Microsoft Symbol Servers** if you also want to step into ASP.NET Core).
- **Symbols**: with *Automatically choose what module symbols to search for* (the recommended
  default), Visual Studio can skip modules outside your solution. Add `Umbraco.*` to **Included
  modules**, or load them by hand from the Modules window (step 3).

**Newer Visual Studio** uses an Options *pane* instead of a dialog, and every path above gains an
**All Settings** level: **Tools** > **Options** > **All Settings** > **Debugging** > **General**.
Microsoft's own pages disagree about where *Symbols* sits in the new pane (under General, or beside
it); look under **All Settings** > **Debugging** for **Symbols** > **Search Locations**.

Microsoft references:
[Just My Code](https://learn.microsoft.com/en-us/visualstudio/debugger/just-my-code),
[debugger settings (Source Link)](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-specify-debugger-settings),
[JIT optimization](https://learn.microsoft.com/en-us/visualstudio/debugger/jit-optimization-and-debugging),
[symbol and source files](https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger),
[Modules window](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-use-the-modules-window).

#### JetBrains Rider

Rider has **no "Just My Code" checkbox**; don't tell the user to look for one. Its defaults already
let you step into external code, but by **decompiling** it rather than using Source Link.

- **File | Settings | Build, Execution, Deployment | Debugger** (macOS: **JetBrains Rider |
  Settings**, Ctrl+Alt+S): check **Enable external source debug** is on (the default) and **Disable
  JIT optimization on module load** is on (also the default).
- **File | Settings | Tools | External Symbols**: tick **Use sources from symbol files when
  available** and **Allow downloading from remote locations**, and add a symbol server URL to
  **Symbol servers** (the NuGet.org server is `https://symbols.nuget.org/download/symbols`). The
  Source Link and symbol settings live here, **not** on the Debugger page.

If Rider still shows decompiled code, the PDB didn't load: re-check the External Symbols page.
Stepping into external methods is not supported on macOS ARM64.

JetBrains references:
[Debugging external code](https://www.jetbrains.com/help/rider/Debugging_External_Code.html),
[Debugger settings](https://www.jetbrains.com/help/rider/Settings_Debugger.html),
[External Symbols](https://www.jetbrains.com/help/rider/Settings_Debugger_Symbols.html).

#### VS Code (C# extension / C# Dev Kit)

Copy [`assets/vscode-launch.json`](../assets/vscode-launch.json) to `.vscode/launch.json` and
replace the placeholder:

- `<ProjectName>` → the web project's **assembly name** (usually the `.csproj` file name), so
  `program` points at `bin/Debug/net10.0/<ProjectName>.dll`. If the project targets another
  framework or sets a custom output path, adjust the rest of that path too.

It expects a `build` task in `.vscode/tasks.json` (`preLaunchTask`). If there isn't one, create a
`dotnet build` task with that label, or remove the `preLaunchTask` line and build first.

The keys that matter are **top-level properties of the launch configuration**, not nested in each
other:

| Key | Value | Why |
|---|---|---|
| `justMyCode` | `false` | Stop in Umbraco code |
| `suppressJITOptimizations` | `true` | Readable locals in Release DLLs |
| `symbolOptions.searchNuGetOrgSymbolServer` | `true` | Download Umbraco PDBs (**defaults to false**) |
| `symbolOptions.searchMicrosoftSymbolServer` | `true` | Optional: .NET / ASP.NET Core PDBs (**defaults to false**) |
| `sourceLinkOptions` | `{ "*": { "enabled": true } }` | A URL → `{ "enabled": bool }` map. Enabled for all URLs by default; only needed to restrict it. Can only be set in launch.json |

Option names that **don't exist** and silently do nothing: `"enableSourceLink"`,
`"symbolOptions": { "nuget": true }`, `justMyCode` inside `symbolOptions`, `"sourceLinkOptions": true`.

**C# Dev Kit `"type": "dotnet"` configs, or no launch.json at all:** it is not verified that these
honour the keys above inline. The safe route is the user `settings.json`, which applies to every
launch:

```json
{
  "csharp.debug.justMyCode": false,
  "csharp.debug.suppressJITOptimizations": true,
  "csharp.debug.symbolOptions": {
    "searchNuGetOrgSymbolServer": true,
    "searchMicrosoftSymbolServer": true
  }
}
```

`sourceLinkOptions` has no `settings.json` form, and none of these keys work in
`launchSettings.json`.

Reference:
[Configuring C# debugging](https://code.visualstudio.com/docs/csharp/debugger-settings).

### 3. Verify

1. Launch the site **under the debugger** (F5), and set a breakpoint in your own code just before an
   Umbraco call. The probe asset is a ready-made target (below); otherwise use any call such as
   `IContentService.Save(...)`.
2. When it breaks, open the **Modules** window (Visual Studio: **Debug** > **Windows** > **Modules**,
   Ctrl+Alt+U, available only while paused). `Umbraco.Core.dll` should show **Symbols loaded**, with
   the PDB path in the NuGet.org symbol cache. If it says *Skipped loading* or *Cannot find*,
   right-click > **Symbol Load Information** to see where it looked, then **Load Symbols**.
3. Press **F11** on the Umbraco call. Visual Studio may prompt to download the source from GitHub
   (that prompt only appears once the PDB has loaded and Source Link support is on). Accept it, and
   you land in `ContentService` in Umbraco's real source, with the file fetched for the exact commit
   your package was built from.

### F11 target: `assets/SourceLinkProbe.cs`

[`assets/SourceLinkProbe.cs`](../assets/SourceLinkProbe.cs) is an optional, throwaway class that
gives a deterministic place to break. It registers a handler for
`UmbracoApplicationStartedNotification` that calls `IContentService.GetRootContent()` once at
startup, so you can set a breakpoint on that line, press F5, and F11 into Umbraco without clicking
anything in the site. It records the outcome on a `SourceLinkProbeState` singleton.

Add it only when the user asks for a ready-made target or has no Umbraco call of their own to step
into. If their code already calls Umbraco (a controller calling `_contentService.Save`, a view
component querying content), tell them to break there and leave the project untouched: the fix for
"F11 steps over Umbraco" is IDE settings, not code.

Replace `<Namespace>` with the web project's root namespace (from the `.csproj` `RootNamespace`, or
any existing `.cs` file). Delete the file when you're done: it runs a database query on every boot
and is only there to step into.

## Troubleshooting

- **"Symbols not loaded" / "No symbols have been loaded for this document"** on an Umbraco frame:
  the NuGet.org symbol server is off, or the module-loading mode skipped `Umbraco.*`. Fix the symbol
  settings, then **Load Symbols** in the Modules window.
- **Breakpoints in Umbraco code never hit / F11 steps over the call**: Just My Code is still on
  (Visual Studio, VS Code), or external source debugging is off (Rider).
- **You see decompiled code instead of real source**: the PDB didn't load (symbol server off, or the
  module was skipped), or Source Link support is off. Source Link needs the PDB first.
- **Locals are "optimized away", stepping jumps lines**: JIT suppression is off, or you attached to
  an already-running process. Launch under the debugger instead; for ReadyToRun images set
  `COMPlus_ReadyToRun=0`.
- **VS Code ignores your settings**: check the keys are top-level in the launch configuration, the
  spellings match the table above, and both symbol-server flags are explicitly `true`.
- **A module filter matches nothing**: it uses a package id (`Umbraco.Cms.Core`) instead of the DLL
  name (`Umbraco.Core.dll`).
- **Source download fails in VS Code**: its Source Link only fetches unauthenticated URLs. Umbraco's
  repo is public, so check the machine can reach `raw.githubusercontent.com` and that no
  `sourceLinkOptions` entry disables it.

## Done

Reply checklist. Every answer for this approach covers each point that applies:

- **Every setting, by name, with why.** Just My Code off (say it is **on by default**, which is why
  F11 steps over Umbraco), Source Link on, the NuGet.org symbol server, JIT optimization suppressed.
- **VS Code:** the actual configuration, written to `.vscode/launch.json` or shown in full in the
  answer, with `justMyCode`, `symbolOptions.searchNuGetOrgSymbolServer` and
  `suppressJITOptimizations`. Pointing at a file the user doesn't have is not enough. Keep the
  user's other keys, and keep regex backslashes doubled (`\\b`, `\\s`) so the file stays valid JSON.
  If you can run a command, parse the file to prove it (strip `//` comments, then `JSON.parse`);
  don't claim it is valid without checking.
- **Where to break:** the user's own call into Umbraco, or the probe if they have none.
- **How to confirm:** the Modules window (or Rider's equivalent) showing Umbraco symbols loaded, and
  F11 opening Umbraco's source fetched from GitHub.

Don't claim you stepped into Umbraco or saw symbols load; that happens in their IDE. If you added
the probe, say it should be deleted afterwards. If a buildable project is present and you changed
it, build it and report the actual result.
