// Tests for the boot check.
//
// Run: node --test scripts/*.test.mjs
//
// The process handling is exercised for real, against a stand-in app instead of Umbraco: a small Node
// server that reads the same environment variables Umbraco would (ASPNETCORE_URLS, the log directory,
// the connection string) and misbehaves on request through FAKE_MODE. So CI needs no dotnet, and every
// way a boot can go wrong is produced on purpose, including the ones that make a naive check report a
// pass: a port someone else is already answering on, log entries left by a different process, and a
// child process that outlives the app.

import test from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import net from "node:net";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

import {
  bootCheck,
  buildArgs,
  classify,
  findDll,
  isWebProject,
  probeTargets,
  readLog,
  resolveProject,
  summariseException,
} from "./boot-check.mjs";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const CLI = path.join(HERE, "boot-check.mjs");

const tmp = (prefix) => fs.mkdtempSync(path.join(os.tmpdir(), prefix));

// The stand-in app. Logs Umbraco-style CLEF lines (one JSON object per line, with ProcessId) to the
// directory boot-check points Umbraco's logs at.
const FAKE_APP = String.raw`
const fs = require("node:fs");
const http = require("node:http");
const path = require("node:path");
const { spawn } = require("node:child_process");

const mode = process.env.FAKE_MODE || "ok";
const port = Number(new URL(process.env.ASPNETCORE_URLS).port);
const logDir = process.env.Umbraco__CMS__Logging__Directory;
fs.mkdirSync(logDir, { recursive: true });
const log = (entry) => fs.appendFileSync(path.join(logDir, "UmbracoTraceLog.test.json"),
  JSON.stringify({ "@t": new Date().toISOString(), ProcessId: process.pid, ...entry }) + "\n");
const level = (value) => log({ "@mt": "Runtime level: {RuntimeLevel} - {RuntimeLevelReason}",
  "@l": "Debug", RuntimeLevel: value, RuntimeLevelReason: value });
const serve = (status = 200) => http.createServer((req, res) => { res.statusCode = status; res.end("ok"); }).listen(port, "127.0.0.1");
const dbPath = /Data Source=([^;]+)/.exec(process.env.ConnectionStrings__umbracoDbDSN)[1];

if (mode === "crash") {
  level("Run");
  console.error("Unhandled exception. System.InvalidOperationException: Operation is not valid due to the current state of the object.");
  console.error("   at Umbraco.Cms.Core.Composing.OrderedCollectionBuilderBase.InsertBefore()");
  console.error("   at Acme.CampaignComposer.Compose(IUmbracoBuilder builder)");
  console.error("");
  console.error("unrelated trailing output");
  process.exit(134);
} else if (mode === "hang") {
  setInterval(() => {}, 1000);
} else if (mode === "install") {
  level("Install"); serve();
} else if (mode === "error") {
  level("Run");
  log({ "@mt": "Exception ({InstanceId}).", "@l": "Error", InstanceId: "abc", "@x": "System.Exception: boom\n   at X.Y()" });
  serve();
} else if (mode === "status500") {
  level("Run"); serve(500);
} else if (mode === "grandchild") {
  const child = spawn(process.execPath, ["-e", "setInterval(() => {}, 1000)"], { stdio: "ignore" });
  fs.writeFileSync(process.env.FAKE_PIDFILE, String(child.pid));
  level("Run"); serve();
} else if (mode === "probe") {
  level("Run");
  http.createServer((req, res) => {
    if (req.url === "/__boot-check/probe") {
      res.setHeader("content-type", "application/json");
      res.end(JSON.stringify({ runtimeLevel: "Run", contentFinders: ["My.Finder", "Umbraco.Cms.Core.Routing.ContentFinderByUrlNew"] }));
    } else res.end("ok");
  }).listen(port, "127.0.0.1");
} else if (mode === "seeded") {
  const { DatabaseSync } = require("node:sqlite");
  const db = new DatabaseSync(dbPath);
  db.exec("CREATE TABLE IF NOT EXISTS umbracoNode (id INTEGER)"); // the "install"
  const seeded = db.prepare("SELECT name FROM sqlite_master WHERE name = 'legacySku'").get();
  db.close();
  level("Run");
  if (!seeded) log({ "@mt": "no such table: legacySku", "@l": "Error" });
  serve();
} else {
  level("Run"); serve();
}
`;

function fakeApp() {
  const dir = tmp("boot-check-fake-");
  const script = path.join(dir, "fake-app.cjs");
  fs.writeFileSync(script, FAKE_APP);
  return { dir, script };
}

/** Run bootCheck against the stand-in app in the given mode. */
async function bootFake(mode, extra = {}) {
  const { dir, script } = fakeApp();
  process.env.FAKE_MODE = mode;
  try {
    return await bootCheck({
      project: dir,
      command: process.execPath,
      args: [script],
      build: false,
      settleSec: 0,
      timeoutSec: 20,
      ...extra,
    });
  } finally {
    delete process.env.FAKE_MODE;
  }
}

const pidAlive = (pid) => {
  try {
    process.kill(pid, 0);
    return true;
  } catch {
    return false;
  }
};

// ---- the end-to-end behaviour, against the stand-in app --------------------------------------

test("a healthy boot passes with the runtime level and status it saw", async () => {
  const report = await bootFake("ok");
  assert.equal(report.result, "PASS", report.reason);
  assert.equal(report.runtimeLevel, "Run");
  assert.equal(report.httpStatus, 200);
});

test("a crash during startup fails and carries the exception and its stack frames", async () => {
  const report = await bootFake("crash");
  assert.equal(report.result, "FAIL");
  assert.equal(report.outcome, "exited");
  assert.match(report.exception, /^Unhandled exception\. System\.InvalidOperationException/);
  assert.match(report.exception, /at Acme\.CampaignComposer\.Compose/);
  assert.doesNotMatch(report.exception, /unrelated trailing output/);
});

test("an app that never answers fails on the timeout and is stopped", async () => {
  const report = await bootFake("hang", { timeoutSec: 2 });
  assert.equal(report.result, "FAIL");
  assert.equal(report.outcome, "timeout");
  assert.equal(pidAlive(report.pid), false);
});

test("answering HTTP is not enough: a runtime level other than Run fails", async () => {
  const report = await bootFake("install");
  assert.equal(report.result, "FAIL");
  assert.match(report.reason, /runtime level Install, expected Run/);
});

test("an Error logged during boot fails, with the rendered message", async () => {
  const report = await bootFake("error");
  assert.equal(report.result, "FAIL");
  assert.deepEqual(report.errors, ["[Error] Exception (abc). — System.Exception: boom"]);
});

test("a 5xx from GET / fails", async () => {
  const report = await bootFake("status500");
  assert.equal(report.result, "FAIL");
  assert.match(report.reason, /GET \/ returned 500/);
});

test("the whole process group is stopped, including children the app started", async () => {
  const pidFile = path.join(tmp("boot-check-pid-"), "child.pid");
  process.env.FAKE_PIDFILE = pidFile;
  try {
    const report = await bootFake("grandchild");
    assert.equal(report.result, "PASS", report.reason);
    const grandchild = Number(fs.readFileSync(pidFile, "utf8"));
    assert.equal(pidAlive(report.pid), false, "app still running");
    assert.equal(pidAlive(grandchild), false, "app's child outlived the check");
  } finally {
    delete process.env.FAKE_PIDFILE;
  }
});

test("a port already in use fails up front instead of reporting someone else's answer", async () => {
  const squatter = net.createServer().listen(0, "127.0.0.1");
  await new Promise((resolve) => squatter.once("listening", resolve));
  try {
    const report = await bootFake("ok", { port: squatter.address().port });
    assert.equal(report.result, "FAIL");
    assert.equal(report.outcome, "port-busy");
  } finally {
    squatter.close();
  }
});

test("--seed-sql runs between an install boot and the checked boot", async () => {
  const sql = path.join(tmp("boot-check-sql-"), "seed.sql");
  fs.writeFileSync(sql, "CREATE TABLE legacySku (Sku TEXT NOT NULL, ProductKey TEXT NOT NULL);");

  const unseeded = await bootFake("seeded");
  assert.equal(unseeded.result, "FAIL", "the stand-in should fail without the table");

  const seeded = await bootFake("seeded", { seedSql: sql });
  assert.equal(seeded.result, "PASS", seeded.reason);
  assert.equal(seeded.seeded, sql);
});

test("a seed that can't be applied is a FAIL with the reason, not a crash of the check", async () => {
  const sql = path.join(tmp("boot-check-sql-"), "bad.sql");
  fs.writeFileSync(sql, "THIS IS NOT SQL;");
  const report = await bootFake("seeded", { seedSql: sql });
  assert.equal(report.result, "FAIL");
  assert.match(report.reason, /^seeding failed: /);
});

test("each boot gets a fresh work directory, so earlier databases and logs can't leak in", async () => {
  const first = await bootFake("ok");
  const second = await bootFake("ok");
  assert.notEqual(first.workDir, second.workDir);
});

test("--probe captures what the running app reports at /__boot-check/probe", async () => {
  const report = await bootFake("probe", { probe: true });
  assert.equal(report.result, "PASS", report.reason);
  assert.deepEqual(report.probe.contentFinders, ["My.Finder", "Umbraco.Cms.Core.Routing.ContentFinderByUrlNew"]);
});

test("a probe that can't be read fails the check, so graders never silently lack the evidence", async () => {
  const report = await bootFake("ok", { probe: true }); // answers "ok" on every path, not JSON
  assert.equal(report.result, "FAIL");
  assert.match(report.reason, /^probe failed: .*did not return JSON/);
});

test("--probe needs a build, because that is how the probe gets compiled in", async () => {
  const dir = tmp("boot-check-probe-nobuild-");
  fs.writeFileSync(path.join(dir, "Site.csproj"), '<Project Sdk="Microsoft.NET.Sdk.Web"></Project>');
  await assert.rejects(bootCheck({ project: dir, probe: true, build: false }), /--no-build/);
});

// ---- the pieces ------------------------------------------------------------------------------

test("readLog only counts entries from the booted process", () => {
  const dir = tmp("boot-check-log-");
  const lines = [
    { ProcessId: 1, "@mt": "Runtime level: {RuntimeLevel} - {RuntimeLevelReason}", RuntimeLevel: "Install" },
    { ProcessId: 1, "@mt": "Exception ({InstanceId}).", "@l": "Error", InstanceId: "seed-boot" },
    { ProcessId: 2, "@mt": "Runtime level: {RuntimeLevel} - {RuntimeLevelReason}", RuntimeLevel: "Install" },
    { ProcessId: 2, "@mt": "Runtime level: {RuntimeLevel} - {RuntimeLevelReason}", RuntimeLevel: "Run" },
  ];
  fs.writeFileSync(path.join(dir, "UmbracoTraceLog.json"), `${lines.map((l) => JSON.stringify(l)).join("\n")}\n{"trunc`);
  assert.deepEqual(readLog(dir, 2), { runtimeLevel: "Run", errors: [], entries: 2 });
  assert.equal(readLog(dir, 1).errors.length, 1);
  assert.deepEqual(readLog(path.join(dir, "missing"), 2), { runtimeLevel: null, errors: [], entries: 0 });
});

test("classify applies the checks in order of severity", () => {
  const ok = { outcome: "responded", httpStatus: 200, runtimeLevel: "Run", errors: [] };
  assert.equal(classify(ok).result, "PASS");
  assert.match(classify({ ...ok, outcome: "timeout", timeoutSec: 5 }).reason, /within 5s/);
  assert.match(classify({ ...ok, outcome: "exited", exitCode: 134 }).reason, /exit code 134/);
  assert.match(classify({ ...ok, httpStatus: 503 }).reason, /503/);
  assert.match(classify({ ...ok, runtimeLevel: null }).reason, /not logged/);
  assert.match(classify({ ...ok, runtimeLevel: "BootFailed" }).reason, /BootFailed/);
  assert.match(classify({ ...ok, errors: ["x", "y"] }).reason, /2 error/);
});

test("probeTargets compiles the probe into the web project only, with paths XML-escaped", () => {
  const xml = probeTargets("/sites/R&D/Site.csproj", "/tools/boot-check-probe.cs");
  assert.match(xml, /Condition="'\$\(MSBuildProjectFullPath\)' == '\/sites\/R&amp;D\/Site\.csproj'"/);
  assert.match(xml, /<Compile Include="\/tools\/boot-check-probe\.cs" \/>/);
});

test("buildArgs adds the probe targets and a separate output dir only when probing", () => {
  assert.deepEqual(buildArgs("/s/Site.csproj"), ["build", "/s/Site.csproj", "--nologo", "-v", "quiet"]);
  const probing = buildArgs("/s/Site.csproj", { targets: "/w/probe.targets", outDir: "/w/bin" });
  assert.ok(probing.includes("-p:CustomAfterMicrosoftCommonTargets=/w/probe.targets"));
  assert.ok(probing.includes(`-p:OutDir=/w/bin${path.sep}`));
});

test("summariseException returns null when there was no unhandled exception", () => {
  assert.equal(summariseException("info: started\n"), null);
});

test("findDll reads AssemblyName and TargetFramework, falling back to the project name", () => {
  const dir = tmp("boot-check-proj-");
  const plain = path.join(dir, "MySite.Web.csproj");
  fs.writeFileSync(plain, "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
  assert.equal(findDll(plain), path.join(dir, "bin", "Debug", "net10.0", "MySite.Web.dll"));

  const named = path.join(dir, "Other.csproj");
  fs.writeFileSync(named, "<Project><PropertyGroup><AssemblyName>Site</AssemblyName><TargetFrameworks>net10.0;net9.0</TargetFrameworks></PropertyGroup></Project>");
  assert.equal(findDll(named), path.join(dir, "bin", "Debug", "net10.0", "Site.dll"));
});

test("a class library is refused with a clear reason, since it has no site to boot", async () => {
  const dir = tmp("boot-check-lib-");
  fs.writeFileSync(path.join(dir, "Lib.csproj"), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>');
  assert.equal(isWebProject(path.join(dir, "Lib.csproj")), false);
  await assert.rejects(bootCheck({ project: dir }), /is not a web project/);
  fs.writeFileSync(path.join(dir, "Lib.csproj"), '<Project Sdk="Microsoft.NET.Sdk.Web"></Project>');
  assert.equal(isWebProject(path.join(dir, "Lib.csproj")), true);
});

test("resolveProject accepts a .csproj or a directory with exactly one", () => {
  const dir = tmp("boot-check-resolve-");
  assert.throws(() => resolveProject(dir), /found 0/);
  fs.writeFileSync(path.join(dir, "A.csproj"), "<Project/>");
  assert.equal(resolveProject(dir), path.join(dir, "A.csproj"));
  fs.writeFileSync(path.join(dir, "B.csproj"), "<Project/>");
  assert.throws(() => resolveProject(dir), /found 2/);
  assert.equal(resolveProject(path.join(dir, "B.csproj")), path.join(dir, "B.csproj"));
});

test("an error inside the check still writes a FAIL report to --out", () => {
  const out = path.join(tmp("boot-check-out-"), "boot.txt");
  let code = 0;
  try {
    execFileSync(process.execPath, [CLI, path.join(tmp("boot-check-empty-"), "missing"), "--out", out], { stdio: "pipe" });
  } catch (error) {
    code = error.status;
  }
  assert.equal(code, 2);
  assert.match(fs.readFileSync(out, "utf8"), /^RESULT: FAIL — boot-check error: project not found/);
});

test("the CLI rejects bad arguments with exit code 2", () => {
  for (const args of [[], ["--port", "abc", "x"], ["x", "--bogus"], ["x", "y"]]) {
    let code = 0;
    try {
      execFileSync(process.execPath, [CLI, ...args], { stdio: "pipe" });
    } catch (error) {
      code = error.status;
    }
    assert.equal(code, 2, `args: ${args.join(" ")}`);
  }
});
