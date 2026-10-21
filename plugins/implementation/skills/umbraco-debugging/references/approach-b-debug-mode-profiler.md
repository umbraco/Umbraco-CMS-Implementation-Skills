# Approach B — Debug mode, MiniProfiler and IProfiler

Switch Umbraco into debug mode so a request can be profiled with MiniProfiler: a timing tree of
Umbraco's own steps, your steps and the SQL behind them, shown in the browser. Choose this when the
question is "why is this page slow?", or when you need evidence from a dev, test or staging site
nobody can attach a debugger to. To read inside an Umbraco method line by line, use
[Approach A](approach-a-sourcelink.md) instead.

## Contents

- [Fetch the docs first](#fetch-the-docs-first): read when you need the official docs pages for debug mode and MiniProfiler, and where they fall short.
- [Steps](#steps): read when you are turning on debug mode, profiling a request, adding your own `IProfiler` steps, or deciding what to use on a production site.
- [Done](#done): read before replying, to check the answer covers the setting, the restart, how to trigger and read a profile, and turning it off afterwards.

## Fetch the docs first

- [Testing and Debugging](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/testing-and-debugging.md):
  debug mode, the MiniProfiler prerequisites and the `IProfiler` sample.
- [Hosting Settings](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/configuration/hostingsettings.md):
  the `Debug` key and its default (`false`).

Use these `17.latest` URLs. The unversioned pages now serve a newer major version.

The docs page is right in effect but leaves gaps, filled in below: it says to set the key in
`appsettings.json` (put it in environment config instead), and it doesn't mention the restart, the
`X-UMB-DEBUG` header, or the backoffice cookie toggle. Its screenshots are from Umbraco 8.

Don't confuse the key with its neighbours. **`Umbraco:CMS:Hosting:Debug`** is debug mode.
`Umbraco:CMS:Debug` is a different section (`LogIncompletedScopes`, `DumpOnTimeoutThreadAbort`), and
`ASPNETCORE_ENVIRONMENT=Development` is ASP.NET Core's environment name. None of them turns on
another.

## Steps

### 1. Discover project context

Find the web project's `appsettings.*.json` files and which environment the target site runs as.
Check whether `Umbraco:CMS:Hosting:Debug` is already set anywhere (it is often already `true` in
`appsettings.Development.json`), and whether the base `appsettings.json` sets it.

### 2. Turn on debug mode, outside production

Set the key in the **environment-specific** file, never in the base `appsettings.json` that
production inherits. In `appsettings.Development.json` (or `appsettings.Staging.json`, and so on):

```json
{
  "Umbraco": {
    "CMS": {
      "Hosting": {
        "Debug": true
      }
    }
  }
}
```

Merge it into the existing `Umbraco:CMS` object rather than adding a second `Umbraco` block. An
environment variable (`Umbraco__CMS__Hosting__Debug=true`) does the same for hosted environments.

**Restart the site after changing it.** Umbraco reads the value once at startup to choose the
profiler: with `false` it registers a `NoopProfiler` for the life of the process, and the log says
the profiler is not profiling because the site must run in debug mode. Flipping the value at runtime
updates some debug-mode checks but never switches the profiler, so it half-works and misleads.

### 3. Profile a request

Profiling must be requested per request, with any one of:

| Switch | How |
|---|---|
| Query string | `?umbDebug=true` on the page URL |
| Request header | `X-UMB-DEBUG: true` (handy from `curl` or an API client) |
| Cookie | Backoffice **Settings** > **Profiling** > **Activate the profiler by default**. This sets an `UMB-DEBUG` session cookie that is HttpOnly and SameSite=Strict, so it only applies when the backoffice and the front end share a domain |

Then, to **see** the results, be signed in to the backoffice, in the same browser, as a user with
access to the **Settings** section.

On a Razor page the MiniProfiler widget is **injected automatically** into the `<body>` by
`UmbracoViewPage`: don't add it by hand. It won't appear on non-HTML responses, in preview mode, on
views that don't inherit `UmbracoViewPage`, or on a page with no `<body>` tag. Only in that last kind
of layout, render it manually with `@Html.RenderProfiler()` (namespace `Umbraco.Extensions`) just
before `</body>`.

The first profiled request after boot also carries Umbraco's startup timings. SQL appears in the
tree for both SQLite and SQL Server.

#### How the gating actually works (verified against the v17 source)

The docs list "be signed in as a Settings user" alongside the query string, which reads as if the
request is only profiled for that user. In Umbraco 17's source that is not the case:

- **Starting a profile has no user check.** Whether a request is profiled depends only on debug mode
  being on at boot, the site running, the request not being a static asset or a profiler path, and
  one of the three switches above. Anyone who adds `?umbDebug=true` gets their request profiled.
- **Reading the results is what requires the backoffice login.** The results endpoints under
  `/umbraco/profiler/` authorise the backoffice cookie and require access to the Settings section.

So debug mode on a public site lets any visitor make the server do profiling work, even though they
can't read the results. That is one more reason to keep `Hosting:Debug` off in production.

### 4. Add your own steps with `IProfiler`

Umbraco already registers MiniProfiler; don't call `services.AddMiniProfiler()` or use
`MiniProfiler.Current` directly. Inject **`IProfiler`** (namespace `Umbraco.Cms.Core.Logging`) and
wrap the work you want timed:

```csharp
using (_profiler.Step("Load related products"))
{
    // work to time
}
```

- `Step(string name)` is the only public step method. It returns `IDisposable?`, and `using` on a
  null is safe. There is no `Step<T>` or `DebugDuration` on `IProfiler`; the duration helpers that
  write to the log live on `IProfilingLogger`.
- Steps nest: a `using` inside another `using` shows as a child row in the timing tree.
- Give **each suspect its own step**. One step around a whole loop tells you the loop is slow, not
  which call inside it; wrap the expensive call inside the loop in its own nested step too.
- With debug mode off, the registered profiler is a no-op, so the steps can stay in the code.

[`assets/ProfiledController.cs`](../assets/ProfiledController.cs) is a complete, minimal example:
a plain ASP.NET Core controller at `/debugging-example/profiled` that counts content nodes inside
nested steps. Replace `<Namespace>` with the web project's root namespace (it uses
`<Namespace>.Controllers`), then open `/debugging-example/profiled?umbDebug=true`. It returns plain
text, so the widget isn't injected there. Find its timings in MiniProfiler's results list at
`/umbraco/profiler/results-index` while signed in to the backoffice as a Settings user, or copy the
pattern into a controller or view component behind a Razor page, where the widget shows them.

The docs' own sample is a route-hijacking `RenderController`, which only runs for one Document
Type (`root` in the sample). Use it when the code you want to time lives in a hijacked page.

### 5. In production, use telemetry instead

Leave debug mode off, and don't offer a "switch it on briefly, then off again" window on the live
site either: profiling starts for any request carrying `?umbDebug=true`, with no login check, and
the switch needs a restart each way. To find slow or failing requests on a live site, use what is
designed to run there:

- **Logs**: Umbraco's structured Serilog logging and the backoffice Log Viewer. See
  [Logging](https://docs.umbraco.com/umbraco-cms/17.latest/develop-with-umbraco/testing-and-debugging/logging.md).
  Timed log entries come from `IProfilingLogger`.
- **An APM / tracing tool** your hosting already offers, such as Application Insights or an
  OpenTelemetry exporter.

Then reproduce the problem on a non-production environment with the profiler, or locally with
Approach A.

## Done

Reply checklist. Every answer for this approach covers each point that applies:

- **Where the key goes:** the exact key `Umbraco:CMS:Hosting:Debug`, the file or environment
  setting you put it in, and that it is not in the base `appsettings.json`.
- **Restart** after turning it on, and again after turning it off.
- **How to trigger and read a profile:** `?umbDebug=true` (or the header or the Profiling toggle),
  and that reading results needs a backoffice login with Settings access.
- **Turn it off afterwards** on any shared environment, and never on production, with the reason
  (no login check when a profile starts).
- **If you added steps:** one named step per suspect, and tell the user plainly they can **leave
  the steps in the code** permanently: with debug mode off the profiler is a no-op, so they cost
  next to nothing. Saying only that they "won't show in production" leaves them wondering whether
  to remove them.

If you added steps or the example controller and a buildable project is present, build it and
report the actual result; don't claim you saw a MiniProfiler result unless you did.
