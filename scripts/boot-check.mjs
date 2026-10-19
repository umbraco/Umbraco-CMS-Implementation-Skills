#!/usr/bin/env node
// boot-check.mjs <project> [--port N] [--timeout SEC] [--settle SEC] [--no-build] [--seed-sql FILE]
//                          [--out FILE] [--json]
//
// Builds an Umbraco web project and boots it once, to prove code that compiles also STARTS. Some
// Umbraco mistakes only surface at boot: a collection anchor that isn't registered (InsertBefore
// throws inside .AddComposers()), a strong [ComposeAfter] on a disabled composer, a component that
// throws at startup. A compile check passes all of them. Skill evals use this on each run's edited
// project; see the umbraco-skill-evaluator skill.
//
// <project> is a .csproj, or a directory holding exactly one.
//
// Every boot is a FRESH unattended install into its own temp work directory: the SQLite database and
// the Umbraco logs are pointed there through environment variables, so the project only gains its
// bin/, obj/ and Umbraco's scratch folder umbraco/Data/TEMP. The work directory is kept and printed,
// for inspection.
//
// PASS needs positive evidence, not just "something answered on the port":
//   - the process answers GET / with a status below 500,
//   - its own log reports runtime level Run, and
//   - its own log has no Error or Fatal entries.
// Log entries are matched to the booted process by ProcessId, so entries from an earlier boot (the
// seeding boot, a previous run) never count.
//
// --seed-sql runs a first boot just to install, stops it, executes the SQL against the new database
// (node:sqlite), and then does the checked boot. Use it when the code under test reads tables a fresh
// install doesn't have.
//
// The app is started in its own process group and the whole group is stopped afterwards, so nothing
// outlives the check. A port that is already in use fails the check up front instead of answering
// for us.
//
// Exit code: 0 PASS, 1 FAIL, 2 usage error.
//
// No dependencies — Node standard library only.

import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import http from "node:http";
import net from "node:net";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

/** Resolve <project> (a .csproj or a directory with exactly one) to the .csproj path. */
export function resolveProject(target) {
  const full = path.resolve(target);
  if (fs.existsSync(full) && fs.statSync(full).isFile()) return full;
  if (!fs.existsSync(full)) throw new Error(`project not found: ${target}`);
  const candidates = fs.readdirSync(full).filter((name) => name.endsWith(".csproj"));
  if (candidates.length !== 1) {
    throw new Error(`expected exactly one .csproj in ${target}, found ${candidates.length}`);
  }
  return path.join(full, candidates[0]);
}

/** Path of the built DLL for a .csproj: bin/Debug/<TargetFramework>/<AssemblyName>.dll. */
export function findDll(csprojPath) {
  const xml = fs.readFileSync(csprojPath, "utf8");
  const tag = (name) => new RegExp(`<${name}>\\s*([^<]+?)\\s*</${name}>`).exec(xml)?.[1];
  const assembly = tag("AssemblyName") ?? path.basename(csprojPath, ".csproj");
  const framework = tag("TargetFramework") ?? tag("TargetFrameworks")?.split(";")[0];
  if (!framework) throw new Error(`no TargetFramework in ${csprojPath}`);
  return path.join(path.dirname(csprojPath), "bin", "Debug", framework, `${assembly}.dll`);
}

/** Environment that makes Umbraco install unattended into workDir and log there. */
export function bootEnv(port, workDir) {
  const db = path.join(workDir, "Umbraco.sqlite.db");
  return {
    ASPNETCORE_ENVIRONMENT: "Development",
    ASPNETCORE_URLS: `http://127.0.0.1:${port}`,
    ConnectionStrings__umbracoDbDSN: `Data Source=${db};Cache=Shared;Foreign Keys=True;Pooling=True`,
    ConnectionStrings__umbracoDbDSN_ProviderName: "Microsoft.Data.Sqlite",
    Umbraco__CMS__Unattended__InstallUnattended: "true",
    Umbraco__CMS__Unattended__UnattendedUserName: "Administrator",
    Umbraco__CMS__Unattended__UnattendedUserEmail: "admin@example.com",
    Umbraco__CMS__Unattended__UnattendedUserPassword: "1234567890",
    Umbraco__CMS__Logging__Directory: path.join(workDir, "logs"),
    // The "Runtime level: ..." line this check relies on is logged at Debug.
    Serilog__MinimumLevel__Default: "Debug",
  };
}

/** Render a Serilog message template with the entry's own properties. */
function render(entry) {
  return String(entry["@mt"] ?? "").replace(/\{([A-Za-z0-9_]+)(?::[^}]*)?\}/g, (whole, name) =>
    entry[name] === undefined ? whole : String(entry[name]),
  );
}

/**
 * Read the Umbraco JSON logs in logDir and return what the process `pid` logged:
 * its last reported runtime level and its Error/Fatal entries.
 */
export function readLog(logDir, pid) {
  const entries = [];
  if (fs.existsSync(logDir)) {
    for (const name of fs.readdirSync(logDir).filter((n) => n.endsWith(".json"))) {
      for (const line of fs.readFileSync(path.join(logDir, name), "utf8").split("\n")) {
        if (!line.trim()) continue;
        try {
          const entry = JSON.parse(line);
          if (entry.ProcessId === pid) entries.push(entry);
        } catch {
          // A half-written last line from a killed process; skip it.
        }
      }
    }
  }
  const levels = entries.filter((e) => typeof e["@mt"] === "string" && e["@mt"].startsWith("Runtime level:"));
  const errors = entries
    .filter((e) => e["@l"] === "Error" || e["@l"] === "Fatal")
    .map((e) => {
      const detail = typeof e["@x"] === "string" ? ` — ${e["@x"].split("\n")[0]}` : "";
      return `[${e["@l"]}] ${render(e)}${detail}`;
    });
  return { runtimeLevel: levels.at(-1)?.RuntimeLevel ?? null, errors, entries: entries.length };
}

/** The "Unhandled exception." line from console output, with the stack frames that follow it. */
export function summariseException(consoleText, maxFrames = 10) {
  const lines = consoleText.split(/\r?\n/);
  const start = lines.findIndex((line) => line.startsWith("Unhandled exception."));
  if (start < 0) return null;
  const frames = [];
  for (const line of lines.slice(start + 1)) {
    if (!/^\s+at /.test(line)) break;
    frames.push(line.trimEnd());
    if (frames.length >= maxFrames) break;
  }
  return [lines[start], ...frames].join("\n");
}

/** Turn what was observed into PASS/FAIL and a one-line reason. Pure, so it is unit-tested directly. */
export function classify(obs) {
  const fail = (reason) => ({ result: "FAIL", reason });
  if (obs.outcome === "build-failed") return fail("build failed");
  if (obs.outcome === "seed-failed") return fail(`seeding failed: ${obs.seedError}`);
  if (obs.outcome === "port-busy") return fail(`port ${obs.port} is already in use`);
  if (obs.outcome === "timeout") return fail(`no HTTP answer within ${obs.timeoutSec}s`);
  if (obs.outcome === "exited") return fail(`process exited during startup (exit code ${obs.exitCode})`);
  if (obs.httpStatus >= 500) return fail(`GET / returned ${obs.httpStatus}`);
  if (obs.runtimeLevel !== "Run") {
    return fail(`runtime level ${obs.runtimeLevel ?? "not logged"}, expected Run`);
  }
  if (obs.errors.length > 0) return fail(`${obs.errors.length} error(s) logged during boot`);
  return { result: "PASS", reason: `booted to Run, GET / returned ${obs.httpStatus}` };
}

function portFree(port) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.once("error", () => resolve(false));
    server.listen(port, "127.0.0.1", () => server.close(() => resolve(true)));
  });
}

function freePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      server.close(() => resolve(port));
    });
  });
}

function httpStatus(port) {
  return new Promise((resolve) => {
    const request = http.get({ host: "127.0.0.1", port, path: "/", timeout: 5000 }, (response) => {
      response.resume();
      resolve(response.statusCode);
    });
    request.on("timeout", () => request.destroy());
    request.on("error", () => resolve(null));
  });
}

/** Start the app in its own process group, with console output going to consolePath. */
function startApp({ command, args, cwd, env, consolePath }) {
  const out = fs.openSync(consolePath, "a");
  const child = spawn(command, args, {
    cwd,
    env: { ...process.env, ...env },
    stdio: ["ignore", out, out],
    detached: process.platform !== "win32",
  });
  fs.closeSync(out);
  const exited = new Promise((resolve) => child.once("exit", (code, signal) => resolve(code ?? signal)));
  return { child, exited };
}

/** Stop the app and everything it started. */
async function stopApp(app) {
  const { child, exited } = app;
  if (child.exitCode !== null || child.signalCode !== null) return;
  if (process.platform === "win32") {
    spawnSync("taskkill", ["/pid", String(child.pid), "/T", "/F"]);
  } else {
    try {
      process.kill(-child.pid, "SIGTERM");
    } catch {
      // Already gone.
    }
    const stopped = await Promise.race([exited.then(() => true), sleep(5000).then(() => false)]);
    // Kill the group either way, so nothing the app spawned can outlive the check.
    try {
      process.kill(-child.pid, "SIGKILL");
    } catch {
      // Already gone.
    }
    if (!stopped) await exited;
  }
}

/** Wait until the app answers HTTP, exits, or the timeout passes. */
async function waitForAnswer(app, port, timeoutMs) {
  const started = Date.now();
  let exitCode;
  app.exited.then((code) => {
    exitCode = code;
  });
  while (Date.now() - started < timeoutMs) {
    if (exitCode !== undefined) return { outcome: "exited", exitCode, waitedMs: Date.now() - started };
    const status = await httpStatus(port);
    if (status !== null) return { outcome: "responded", httpStatus: status, waitedMs: Date.now() - started };
    await sleep(1000);
  }
  return { outcome: "timeout", waitedMs: Date.now() - started };
}

async function applySeed(dbPath, sqlFile) {
  if (!fs.existsSync(dbPath)) throw new Error(`seeding boot created no database at ${dbPath}`);
  const { DatabaseSync } = await import("node:sqlite");
  const db = new DatabaseSync(dbPath);
  try {
    db.exec(fs.readFileSync(sqlFile, "utf8"));
  } finally {
    db.close();
  }
}

/**
 * Build and boot a project once and report on it.
 *
 * `command`/`args` default to `dotnet <built dll>`; tests replace them with a stand-in app.
 */
export async function bootCheck({
  project,
  port,
  timeoutSec = 240,
  settleSec = 3,
  build = true,
  seedSql = null,
  workDir = null,
  command = null,
  args = null,
}) {
  const csproj = command ? null : resolveProject(project);
  const cwd = csproj ? path.dirname(csproj) : path.resolve(project);
  const work = workDir ?? fs.mkdtempSync(path.join(os.tmpdir(), "boot-check-"));
  fs.mkdirSync(work, { recursive: true });
  const consolePath = path.join(work, "console.log");
  const report = { project: csproj ?? cwd, workDir: work, consolePath, logDir: path.join(work, "logs") };

  const finish = (obs) => Object.assign(report, obs, classify({ errors: [], ...obs }));

  if (build && csproj) {
    const result = spawnSync("dotnet", ["build", csproj, "--nologo", "-v", "quiet"], { encoding: "utf8" });
    if (result.status !== 0) {
      const tail = `${result.stdout}${result.stderr}`.trim().split("\n").slice(-15).join("\n");
      return finish({ outcome: "build-failed", buildOutput: tail });
    }
  }

  const chosenPort = port ?? (await freePort());
  report.port = chosenPort;
  if (!(await portFree(chosenPort))) return finish({ outcome: "port-busy", port: chosenPort });

  const launch = {
    command: command ?? "dotnet",
    args: args ?? [findDll(csproj)],
    cwd,
    env: bootEnv(chosenPort, work),
    consolePath,
  };

  if (seedSql) {
    const seeding = startApp(launch);
    await waitForAnswer(seeding, chosenPort, timeoutSec * 1000);
    await stopApp(seeding);
    try {
      await applySeed(path.join(work, "Umbraco.sqlite.db"), seedSql);
    } catch (error) {
      return finish({ outcome: "seed-failed", seedError: error.message });
    }
    report.seeded = path.resolve(seedSql);
  }

  const app = startApp(launch);
  const answer = await waitForAnswer(app, chosenPort, timeoutSec * 1000);
  if (answer.outcome === "responded") await sleep(settleSec * 1000); // let Started handlers run
  await stopApp(app);

  const log = readLog(report.logDir, app.child.pid);
  const consoleText = fs.existsSync(consolePath) ? fs.readFileSync(consolePath, "utf8") : "";
  return finish({
    ...answer,
    timeoutSec,
    pid: app.child.pid,
    runtimeLevel: log.runtimeLevel,
    errors: log.errors,
    exception: answer.outcome === "exited" ? summariseException(consoleText) : null,
  });
}

/** Plain-text report, as written to --out (e.g. boot.txt beside an eval run). */
export function formatReport(r) {
  const lines = [`RESULT: ${r.result} — ${r.reason}`, `project: ${r.project}`];
  const facts = [
    r.port !== undefined && `port ${r.port}`,
    r.httpStatus !== undefined && `GET / ${r.httpStatus}`,
    r.runtimeLevel !== undefined && `runtime level ${r.runtimeLevel ?? "not logged"}`,
    r.waitedMs !== undefined && `answered after ${(r.waitedMs / 1000).toFixed(1)}s`,
  ].filter(Boolean);
  if (facts.length) lines.push(facts.join(" · "));
  if (r.seeded) lines.push(`seeded with: ${r.seeded}`);
  if (r.exception) lines.push("exception:", r.exception);
  if (r.errors?.length) lines.push("errors logged by this process:", ...r.errors.slice(0, 10).map((e) => `  ${e}`));
  if (r.buildOutput) lines.push("build output (tail):", r.buildOutput);
  lines.push(`work dir (database, logs, console.log): ${r.workDir}`);
  return `${lines.join("\n")}\n`;
}

const USAGE =
  "Usage: node scripts/boot-check.mjs <project> [--port N] [--timeout SEC] [--settle SEC]\n" +
  "                                   [--no-build] [--seed-sql FILE] [--out FILE] [--json]";

function parseArgs(argv) {
  const options = { build: true, json: false };
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === "--no-build") {
      options.build = false;
      continue;
    }
    if (arg === "--json") {
      options.json = true;
      continue;
    }
    const match = /^--(port|timeout|settle|seed-sql|out)(?:=(.*))?$/.exec(arg);
    if (match) {
      const value = match[2] !== undefined ? match[2] : argv[(i += 1)];
      if (value === undefined) return { error: `--${match[1]} needs a value` };
      options[match[1]] = value;
      continue;
    }
    if (arg.startsWith("--")) return { error: `Unknown argument: ${arg}` };
    if (options.project) return { error: `Unexpected argument: ${arg}` };
    options.project = arg;
  }
  if (!options.project) return { error: "a project (.csproj or its directory) is required" };
  for (const key of ["port", "timeout", "settle"]) {
    if (options[key] !== undefined && !/^\d+$/.test(options[key])) return { error: `--${key} must be a whole number` };
  }
  return options;
}

async function main(argv) {
  const options = parseArgs(argv);
  if (options.error) {
    console.error(options.error);
    console.error(USAGE);
    return 2;
  }
  let report;
  try {
    report = await bootCheck({
      project: options.project,
      port: options.port && Number(options.port),
      timeoutSec: options.timeout ? Number(options.timeout) : undefined,
      settleSec: options.settle ? Number(options.settle) : undefined,
      build: options.build,
      seedSql: options["seed-sql"] ?? null,
    });
  } catch (error) {
    console.error(`ERROR: ${error.message}`);
    return 2;
  }
  const text = options.json ? `${JSON.stringify(report, null, 2)}\n` : formatReport(report);
  if (options.out) fs.writeFileSync(options.out, text);
  process.stdout.write(text);
  return report.result === "PASS" ? 0 : 1;
}

// Only act as a CLI when executed directly, so tests can import bootCheck() and friends.
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exitCode = await main(process.argv.slice(2));
}
