#!/usr/bin/env node
// Generate and serve a review page for eval results.
//
// Reads the workspace directory, discovers runs (directories with outputs/), embeds all output data
// into a self-contained HTML page, and serves it via a tiny HTTP server. Feedback auto-saves to
// feedback.json in the workspace.
//
// Usage:
//   node generate_review.mjs <workspace-path> [--port PORT] [--skill-name NAME]
//       [--benchmark /path/to/benchmark.json]
//       [--previous-workspace /path/to/iteration-<N-1>]
//       [--static /path/to/output.html]
//
// Node standard library only, like the rest of this repo's tooling.

import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import http from "node:http";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { parseArgs } from "node:util";

const HERE = path.dirname(fileURLToPath(import.meta.url));

/** Files to exclude from output listings. */
const METADATA_FILES = new Set(["transcript.md", "grading.json"]);

/** Extensions rendered as inline text. C#/Razor/MSBuild are included because this evaluator grades Umbraco skills. */
const TEXT_EXTENSIONS = new Set([
  ".txt", ".md", ".json", ".csv", ".py", ".js", ".mjs", ".cjs", ".ts", ".tsx", ".jsx",
  ".yaml", ".yml", ".xml", ".html", ".css", ".scss", ".sh", ".rb", ".go", ".rs",
  ".java", ".c", ".cpp", ".h", ".hpp", ".sql", ".r", ".toml",
  ".cs", ".cshtml", ".razor", ".csproj", ".props", ".targets", ".config", ".sln",
]);

/** Extensions rendered as inline images. */
const IMAGE_EXTENSIONS = new Set([".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"]);

/** Node has no built-in MIME table; this covers what evals produce. Anything else downloads as octet-stream. */
const MIME_TYPES = {
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".gif": "image/gif",
  ".svg": "image/svg+xml",
  ".webp": "image/webp",
  ".pdf": "application/pdf",
  ".zip": "application/zip",
  ".gz": "application/gzip",
  ".htm": "text/html",
  ".xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  ".pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation",
};

const SKIP_DIRS = new Set(["node_modules", ".git", "__pycache__", "skill", "inputs"]);

export function getMimeType(filePath) {
  return MIME_TYPES[path.extname(filePath).toLowerCase()] ?? "application/octet-stream";
}

function readJson(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch {
    return null;
  }
}

function isDir(p) {
  try {
    return fs.statSync(p).isDirectory();
  } catch {
    return false;
  }
}

/** Recursively find directories that contain an outputs/ subdirectory. */
export function findRuns(workspace) {
  const runs = [];
  findRunsRecursive(workspace, workspace, runs);
  runs.sort((a, b) => {
    const ea = a.eval_id ?? Infinity;
    const eb = b.eval_id ?? Infinity;
    if (ea !== eb) return ea < eb ? -1 : 1;
    return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
  });
  return runs;
}

function findRunsRecursive(root, current, runs) {
  if (!isDir(current)) return;

  if (isDir(path.join(current, "outputs"))) {
    runs.push(buildRun(root, current));
    return;
  }

  for (const name of fs.readdirSync(current).sort()) {
    const child = path.join(current, name);
    if (!SKIP_DIRS.has(name) && isDir(child)) findRunsRecursive(root, child, runs);
  }
}

/** Build a run object with prompt, outputs and grading data. */
export function buildRun(root, runDir) {
  let prompt = "";
  let evalId = null;

  for (const candidate of [path.join(runDir, "eval_metadata.json"), path.join(runDir, "..", "eval_metadata.json")]) {
    if (!fs.existsSync(candidate)) continue;
    const metadata = readJson(candidate);
    if (metadata) {
      prompt = metadata.prompt ?? "";
      evalId = metadata.eval_id ?? null;
    }
    if (prompt) break;
  }

  if (!prompt) {
    for (const candidate of [path.join(runDir, "transcript.md"), path.join(runDir, "outputs", "transcript.md")]) {
      if (!fs.existsSync(candidate)) continue;
      try {
        const match = fs.readFileSync(candidate, "utf8").match(/## Eval Prompt\n\n([\s\S]*?)(?=\n##|$)/);
        if (match) prompt = match[1].trim();
      } catch {
        // unreadable transcript: fall through to the placeholder
      }
      if (prompt) break;
    }
  }

  if (!prompt) prompt = "(No prompt found)";

  const id = path.relative(root, runDir).split(path.sep).join("-");

  const outputsDir = path.join(runDir, "outputs");
  const outputs = [];
  if (isDir(outputsDir)) {
    for (const name of fs.readdirSync(outputsDir).sort()) {
      const filePath = path.join(outputsDir, name);
      if (fs.statSync(filePath).isFile() && !METADATA_FILES.has(name)) outputs.push(embedFile(filePath));
    }
  }

  let grading = null;
  for (const candidate of [path.join(runDir, "grading.json"), path.join(runDir, "..", "grading.json")]) {
    if (!fs.existsSync(candidate)) continue;
    grading = readJson(candidate);
    if (grading) break;
  }

  return { id, prompt, eval_id: evalId, outputs, grading };
}

/** Read a file and return its embedded representation. */
export function embedFile(filePath) {
  const name = path.basename(filePath);
  const ext = path.extname(filePath).toLowerCase();
  const mime = getMimeType(filePath);

  if (TEXT_EXTENSIONS.has(ext)) {
    let content;
    try {
      // Invalid UTF-8 is replaced with U+FFFD rather than failing, as Python's errors="replace" did.
      content = fs.readFileSync(filePath).toString("utf8");
    } catch {
      content = "(Error reading file)";
    }
    return { name, type: "text", content };
  }

  let b64;
  try {
    b64 = fs.readFileSync(filePath).toString("base64");
  } catch {
    return { name, type: "error", content: "(Error reading file)" };
  }

  if (IMAGE_EXTENSIONS.has(ext)) return { name, type: "image", mime, data_uri: `data:${mime};base64,${b64}` };
  if (ext === ".pdf") return { name, type: "pdf", data_uri: `data:${mime};base64,${b64}` };
  if (ext === ".xlsx") return { name, type: "xlsx", data_b64: b64 };
  return { name, type: "binary", mime, data_uri: `data:${mime};base64,${b64}` };
}

/** Load a previous iteration's feedback and outputs: run id -> { feedback, outputs }. */
export function loadPreviousIteration(workspace) {
  const result = {};

  const feedbackMap = {};
  const data = readJson(path.join(workspace, "feedback.json"));
  for (const review of data?.reviews ?? []) {
    if (review?.run_id && (review.feedback ?? "").trim()) feedbackMap[review.run_id] = review.feedback;
  }

  for (const run of findRuns(workspace)) {
    result[run.id] = { feedback: feedbackMap[run.id] ?? "", outputs: run.outputs ?? [] };
  }

  for (const [runId, feedback] of Object.entries(feedbackMap)) {
    if (!(runId in result)) result[runId] = { feedback, outputs: [] };
  }

  return result;
}

/** Generate the complete standalone HTML page with embedded data. */
export function generateHtml(runs, skillName, previous = null, benchmark = null) {
  const template = fs.readFileSync(path.join(HERE, "viewer.html"), "utf8");

  const previousFeedback = {};
  const previousOutputs = {};
  for (const [runId, entry] of Object.entries(previous ?? {})) {
    if (entry.feedback) previousFeedback[runId] = entry.feedback;
    if (entry.outputs?.length) previousOutputs[runId] = entry.outputs;
  }

  const embedded = {
    skill_name: skillName,
    runs,
    previous_feedback: previousFeedback,
    previous_outputs: previousOutputs,
  };
  if (benchmark) embedded.benchmark = benchmark;

  // Escape "</" so a run's output containing "</script>" can't close the embedding <script> early.
  const dataJson = JSON.stringify(embedded).replaceAll("</", "<\\/");

  // A replacer function, not a string: replacement strings interpret "$&", "$'" and friends, which
  // run output can easily contain.
  return template.replace("/*__EMBEDDED_DATA__*/", () => `const EMBEDDED_DATA = ${dataJson};`);
}

function readBenchmark(benchmarkPath) {
  return benchmarkPath && fs.existsSync(benchmarkPath) ? readJson(benchmarkPath) : null;
}

/**
 * Create the review server. Regenerates the HTML on each page load, so refreshing the browser picks up
 * new eval outputs without a restart.
 */
export function createReviewServer({ workspace, skillName, feedbackPath, previous, benchmarkPath }) {
  return http.createServer((req, res) => {
    const send = (status, type, body) => {
      const buffer = Buffer.from(body);
      res.writeHead(status, { "Content-Type": type, "Content-Length": buffer.length });
      res.end(buffer);
    };

    if (req.method === "GET" && (req.url === "/" || req.url === "/index.html")) {
      const html = generateHtml(findRuns(workspace), skillName, previous, readBenchmark(benchmarkPath));
      send(200, "text/html; charset=utf-8", html);
    } else if (req.method === "GET" && req.url === "/api/feedback") {
      const body = fs.existsSync(feedbackPath) ? fs.readFileSync(feedbackPath) : "{}";
      send(200, "application/json", body);
    } else if (req.method === "POST" && req.url === "/api/feedback") {
      const chunks = [];
      req.on("data", (chunk) => chunks.push(chunk));
      req.on("end", () => {
        try {
          const data = JSON.parse(Buffer.concat(chunks).toString("utf8"));
          if (data === null || typeof data !== "object" || Array.isArray(data) || !("reviews" in data)) {
            throw new Error("Expected JSON object with 'reviews' key");
          }
          fs.writeFileSync(feedbackPath, JSON.stringify(data, null, 2) + "\n");
          send(200, "application/json", '{"ok":true}');
        } catch (error) {
          send(500, "application/json", JSON.stringify({ error: String(error.message ?? error) }));
        }
      });
    } else {
      send(404, "text/plain", "Not Found");
    }
  });
}

/** Kill any process listening on the port (best effort; needs lsof). */
function killPort(port) {
  const result = spawnSync("lsof", ["-ti", `:${port}`], { encoding: "utf8", timeout: 5000 });
  if (result.error) {
    if (result.error.code === "ENOENT") console.error("Note: lsof not found, cannot check if port is in use");
    return;
  }
  const pids = result.stdout.split("\n").map((s) => s.trim()).filter(Boolean);
  for (const pid of pids) {
    try {
      process.kill(Number(pid), "SIGTERM");
    } catch {
      // already gone
    }
  }
  // Give the killed process a moment to release the port (synchronous sleep, as Python's time.sleep was).
  if (pids.length) Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 500);
}

function openBrowser(url) {
  const [command, args] =
    process.platform === "darwin" ? ["open", [url]]
    : process.platform === "win32" ? ["cmd", ["/c", "start", "", url]]
    : ["xdg-open", [url]];
  try {
    const child = spawn(command, args, { detached: true, stdio: "ignore" });
    child.on("error", () => {});
    child.unref();
  } catch {
    // no browser available (headless): the URL is printed anyway
  }
}

function listen(server, port) {
  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, "127.0.0.1", () => {
      server.off("error", reject);
      resolve(server.address().port);
    });
  });
}

async function main() {
  const { values, positionals } = parseArgs({
    allowPositionals: true,
    options: {
      port: { type: "string", short: "p", default: "3117" },
      "skill-name": { type: "string", short: "n" },
      "previous-workspace": { type: "string" },
      benchmark: { type: "string" },
      static: { type: "string", short: "s" },
    },
  });

  if (positionals.length !== 1) {
    console.error("Usage: node generate_review.mjs <workspace> [--port N] [--skill-name NAME] [--benchmark FILE] [--previous-workspace DIR] [--static FILE]");
    process.exit(2);
  }

  const workspace = path.resolve(positionals[0]);
  if (!isDir(workspace)) {
    console.error(`Error: ${workspace} is not a directory`);
    process.exit(1);
  }

  const runs = findRuns(workspace);
  if (!runs.length) {
    console.error(`No runs found in ${workspace}`);
    process.exit(1);
  }

  const skillName = values["skill-name"] ?? path.basename(workspace).replace("-workspace", "");
  const feedbackPath = path.join(workspace, "feedback.json");
  const previousWorkspace = values["previous-workspace"] ? path.resolve(values["previous-workspace"]) : null;
  const previous = previousWorkspace ? loadPreviousIteration(previousWorkspace) : {};
  const benchmarkPath = values.benchmark ? path.resolve(values.benchmark) : null;

  if (values.static) {
    const out = path.resolve(values.static);
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, generateHtml(runs, skillName, previous, readBenchmark(benchmarkPath)));
    console.log(`\n  Static viewer written to: ${out}\n`);
    return;
  }

  let port = Number(values.port);
  killPort(port);
  const server = createReviewServer({ workspace, skillName, feedbackPath, previous, benchmarkPath });
  try {
    port = await listen(server, port);
  } catch {
    // Port still in use after the kill attempt: take a free one.
    port = await listen(server, 0);
  }

  const url = `http://localhost:${port}`;
  console.log("\n  Eval Viewer");
  console.log("  ─────────────────────────────────");
  console.log(`  URL:       ${url}`);
  console.log(`  Workspace: ${workspace}`);
  console.log(`  Feedback:  ${feedbackPath}`);
  if (previousWorkspace) console.log(`  Previous:  ${previousWorkspace} (${Object.keys(previous).length} runs)`);
  if (benchmarkPath) console.log(`  Benchmark: ${benchmarkPath}`);
  console.log("\n  Press Ctrl+C to stop.\n");

  openBrowser(url);

  process.on("SIGINT", () => {
    console.log("\nStopped.");
    server.close(() => process.exit(0));
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  main().catch((error) => {
    console.error(error);
    process.exit(1);
  });
}
