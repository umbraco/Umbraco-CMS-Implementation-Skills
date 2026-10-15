#!/usr/bin/env node
// Aggregate individual run results into benchmark summary statistics.
//
// Reads grading.json files from run directories and produces:
// - run_summary with mean, stddev, min, max for each metric
// - delta between the primary configuration and its baseline
//
// Usage:
//   node aggregate_benchmark.mjs <iteration_dir> --skill-name <name> [--skill-path <path>] [--output <file>]
//
// Primary layout (what SKILL.md tells the agent to build):
//
//   <iteration_dir>/
//   └── <eval-name>/                 # descriptive, e.g. extract-tables
//       ├── eval_metadata.json
//       ├── with_skill/
//       │   ├── outputs/
//       │   └── grading.json         # sibling of outputs/
//       └── without_skill/           # or old_skill/ when iterating
//           ├── outputs/
//           └── grading.json
//
// `without_skill` is replaced by `old_skill` when iterating on an existing skill. Discovery matches the
// viewer (generate_review.mjs): each config directory holds grading.json directly, alongside outputs/.
//
// Legacy layout (older runs that nested replicate runs under run-*/) is also accepted — grading.json is
// then read from <config>/run-N/grading.json.
//
// Node standard library only, like the rest of this repo's tooling.

import fs from "node:fs";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { parseArgs } from "node:util";

/** Directories under an iteration that are never eval directories. */
const NON_EVAL_DIRS = new Set(["runs", "skill-snapshot", "__pycache__", ".git"]);

/**
 * The configuration the skill is meant to win on, and the baseline it's compared against. Delta is
 * always primary - baseline, regardless of the order config directories happen to sort in.
 */
const PRIMARY_CONFIGS = ["with_skill", "new_skill"];
const BASELINE_CONFIGS = ["without_skill", "old_skill"];

const round4 = (x) => Math.round(x * 1e4) / 1e4;

/** "+0.15" / "-3.0": a signed fixed-point string, as Python's "{:+.Nf}" produced. */
export function signed(x, digits) {
  const text = x.toFixed(digits);
  return text.startsWith("-") ? text : `+${text}`;
}

/** Mean, sample stddev, min and max, rounded to 4 places. */
export function calculateStats(values) {
  if (!values.length) return { mean: 0, stddev: 0, min: 0, max: 0 };
  const n = values.length;
  const mean = values.reduce((a, b) => a + b, 0) / n;
  const stddev = n > 1 ? Math.sqrt(values.reduce((a, x) => a + (x - mean) ** 2, 0) / (n - 1)) : 0;
  return { mean: round4(mean), stddev: round4(stddev), min: round4(Math.min(...values)), max: round4(Math.max(...values)) };
}

const isDir = (p) => {
  try {
    return fs.statSync(p).isDirectory();
  } catch {
    return false;
  }
};

const listDirs = (dir) => fs.readdirSync(dir).sort().map((name) => path.join(dir, name)).filter(isDir);

/** Parse a JSON file; on a syntax error warn (when asked) and return undefined. */
function readJson(filePath, { warn = false } = {}) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch (error) {
    if (warn && error instanceof SyntaxError) console.log(`Warning: Invalid JSON in ${filePath}: ${error.message}`);
    return undefined;
  }
}

/**
 * Load grading.json for one configuration: [grading, runNumber]. In the primary layout grading.json sits
 * directly under the config dir (run 1). In the legacy layout it lives under run-N/, in which case the
 * lowest-sorting run is used.
 */
function loadGrading(configDir) {
  const direct = path.join(configDir, "grading.json");
  if (fs.existsSync(direct)) return [readJson(direct, { warn: true }) ?? null, 1];

  for (const runDir of listDirs(configDir).filter((d) => path.basename(d).startsWith("run-"))) {
    const gradingFile = path.join(runDir, "grading.json");
    if (!fs.existsSync(gradingFile)) continue;
    const parsed = Number.parseInt(path.basename(runDir).split("-")[1], 10);
    const runNumber = Number.isNaN(parsed) ? 1 : parsed;
    return [readJson(gradingFile, { warn: true }) ?? null, runNumber];
  }
  return [null, 1];
}

/**
 * Wall-clock time and token count for a run. Tokens live only in timing.json, so always read it for
 * tokens; time may already be copied into grading.json by the grader, so timing.json is only a
 * fallback for time.
 */
function readTimeAndTokens(configDir, grading) {
  let timeSeconds = grading.timing?.total_duration_seconds ?? 0;
  let tokens = 0;
  const runTimings = listDirs(configDir)
    .filter((d) => path.basename(d).startsWith("run-"))
    .map((d) => path.join(d, "timing.json"));
  for (const timingFile of [path.join(configDir, "timing.json"), ...runTimings]) {
    if (!fs.existsSync(timingFile)) continue;
    const timing = readJson(timingFile);
    if (timing === undefined) continue;
    tokens = timing.total_tokens ?? tokens;
    if (timeSeconds === 0) timeSeconds = timing.total_duration_seconds ?? 0;
    break;
  }
  return [timeSeconds, tokens];
}

/** All run results in an iteration directory, keyed by config name ("with_skill", "without_skill", …). */
export function loadRunResults(iterationDir) {
  const evalDirs = listDirs(iterationDir).filter((d) => !NON_EVAL_DIRS.has(path.basename(d)));
  if (!evalDirs.length) {
    console.log(`No eval directories found in ${iterationDir}`);
    return {};
  }

  const results = {};
  for (const evalDir of evalDirs) {
    let evalId = path.basename(evalDir);
    let evalName = path.basename(evalDir);
    const metadata = readJson(path.join(evalDir, "eval_metadata.json"));
    if (metadata && typeof metadata === "object") {
      if ("eval_id" in metadata) evalId = metadata.eval_id;
      if ("eval_name" in metadata) evalName = metadata.eval_name;
    }

    for (const configDir of listDirs(evalDir)) {
      const [grading, runNumber] = loadGrading(configDir);
      if (grading === null) continue;

      const [timeSeconds, tokens] = readTimeAndTokens(configDir, grading);
      const summary = grading.summary ?? {};

      // The viewer requires expectation fields: text, passed, evidence.
      const expectations = grading.expectations ?? [];
      for (const exp of expectations) {
        if (!("text" in exp) || !("passed" in exp)) {
          console.log(`Warning: expectation in ${configDir} missing required fields (text, passed, evidence): ${JSON.stringify(exp)}`);
        }
      }

      (results[path.basename(configDir)] ??= []).push({
        eval_id: evalId,
        eval_name: evalName,
        run_number: runNumber,
        pass_rate: summary.pass_rate ?? 0,
        passed: summary.passed ?? 0,
        failed: summary.failed ?? 0,
        total: summary.total ?? 0,
        time_seconds: timeSeconds,
        tokens,
        expectations,
      });
    }
  }
  return results;
}

/** Configs with the primary first and its baseline second. */
export function orderConfigs(configs) {
  const primary = PRIMARY_CONFIGS.find((c) => configs.includes(c));
  const baseline = BASELINE_CONFIGS.find((c) => configs.includes(c));
  const ordered = [primary, baseline].filter(Boolean);
  return [...ordered, ...configs.filter((c) => !ordered.includes(c))];
}

/** Summary statistics per configuration, primary first, plus the primary - baseline delta. */
export function aggregateResults(results) {
  const runSummary = {};
  const configs = orderConfigs(Object.keys(results));

  for (const config of configs) {
    const runs = results[config] ?? [];
    runSummary[config] = {
      pass_rate: calculateStats(runs.map((r) => r.pass_rate)),
      time_seconds: calculateStats(runs.map((r) => r.time_seconds)),
      tokens: calculateStats(runs.map((r) => r.tokens ?? 0)),
    };
  }

  const primary = runSummary[configs[0]] ?? {};
  const baseline = configs.length >= 2 ? runSummary[configs[1]] ?? {} : {};
  const delta = (metric) => (primary[metric]?.mean ?? 0) - (baseline[metric]?.mean ?? 0);

  runSummary.delta = {
    pass_rate: signed(delta("pass_rate"), 2),
    time_seconds: signed(delta("time_seconds"), 1),
    tokens: signed(delta("tokens"), 0),
  };
  return runSummary;
}

/** The complete benchmark.json object for an iteration directory. */
export function generateBenchmark(iterationDir, skillName = "", skillPath = "") {
  const results = loadRunResults(iterationDir);
  const runSummary = aggregateResults(results);

  // Primary configuration first, so the viewer pairs runs.
  const runs = [];
  for (const config of orderConfigs(Object.keys(results))) {
    for (const r of results[config]) {
      runs.push({
        eval_id: r.eval_id,
        eval_name: r.eval_name,
        configuration: config,
        run_number: r.run_number,
        result: {
          pass_rate: r.pass_rate,
          passed: r.passed,
          failed: r.failed,
          total: r.total,
          time_seconds: r.time_seconds,
          tokens: r.tokens ?? 0,
        },
        expectations: r.expectations,
      });
    }
  }

  // Eval ids may be strings, so sort as strings.
  const evalIds = [...new Set(Object.values(results).flat().map((r) => r.eval_id))]
    .sort((a, b) => (String(a) < String(b) ? -1 : String(a) > String(b) ? 1 : 0));

  // Runs per configuration comes from the data rather than an assumed count.
  const runsPerConfig = Math.max(0, ...Object.values(results).map((v) => v.length));

  return {
    metadata: {
      skill_name: skillName || "<skill-name>",
      skill_path: skillPath || "<path/to/skill>",
      executor_model: "<model-name>",
      analyzer_model: "<model-name>",
      timestamp: new Date().toISOString().replace(/\.\d{3}Z$/, "Z"),
      evals_run: evalIds,
      runs_per_configuration: runsPerConfig,
    },
    runs,
    run_summary: runSummary,
    notes: [], // filled in by the analyst pass
  };
}

const titleCase = (s) => s.replaceAll("_", " ").replace(/\w\S*/g, (w) => w[0].toUpperCase() + w.slice(1).toLowerCase());

/** Human-readable benchmark.md. */
export function generateMarkdown(benchmark) {
  const { metadata, run_summary: runSummary } = benchmark;
  const configs = Object.keys(runSummary).filter((k) => k !== "delta");
  const configA = configs[0] ?? "config_a";
  const configB = configs[1] ?? "config_b";
  const a = runSummary[configA] ?? {};
  const b = runSummary[configB] ?? {};
  const delta = runSummary.delta ?? {};

  const pct = (s) => `${((s?.mean ?? 0) * 100).toFixed(0)}% ± ${((s?.stddev ?? 0) * 100).toFixed(0)}%`;
  const secs = (s) => `${(s?.mean ?? 0).toFixed(1)}s ± ${(s?.stddev ?? 0).toFixed(1)}s`;
  const toks = (s) => `${(s?.mean ?? 0).toFixed(0)} ± ${(s?.stddev ?? 0).toFixed(0)}`;

  const lines = [
    `# Skill Benchmark: ${metadata.skill_name}`,
    "",
    `**Model**: ${metadata.executor_model}`,
    `**Date**: ${metadata.timestamp}`,
    `**Evals**: ${metadata.evals_run.map(String).join(", ")} (${metadata.runs_per_configuration} runs each per configuration)`,
    "",
    "## Summary",
    "",
    `| Metric | ${titleCase(configA)} | ${titleCase(configB)} | Delta |`,
    "|--------|------------|---------------|-------|",
    `| Pass Rate | ${pct(a.pass_rate)} | ${pct(b.pass_rate)} | ${delta.pass_rate ?? "—"} |`,
    `| Time | ${secs(a.time_seconds)} | ${secs(b.time_seconds)} | ${delta.time_seconds ?? "—"}s |`,
    `| Tokens | ${toks(a.tokens)} | ${toks(b.tokens)} | ${delta.tokens ?? "—"} |`,
  ];

  if (benchmark.notes?.length) {
    lines.push("", "## Notes", "", ...benchmark.notes.map((note) => `- ${note}`));
  }
  return lines.join("\n");
}

function main() {
  const { values, positionals } = parseArgs({
    allowPositionals: true,
    options: {
      "skill-name": { type: "string", default: "" },
      "skill-path": { type: "string", default: "" },
      output: { type: "string", short: "o" },
    },
  });

  if (positionals.length !== 1) {
    console.error("Usage: node aggregate_benchmark.mjs <iteration_dir> [--skill-name NAME] [--skill-path PATH] [--output FILE]");
    process.exit(2);
  }

  const iterationDir = positionals[0];
  if (!fs.existsSync(iterationDir)) {
    console.log(`Directory not found: ${iterationDir}`);
    process.exit(1);
  }

  const benchmark = generateBenchmark(iterationDir, values["skill-name"], values["skill-path"]);

  const outputJson = values.output ?? path.join(iterationDir, "benchmark.json");
  const outputMd = path.join(path.dirname(outputJson), `${path.basename(outputJson, path.extname(outputJson))}.md`);

  fs.writeFileSync(outputJson, JSON.stringify(benchmark, null, 2));
  console.log(`Generated: ${outputJson}`);
  fs.writeFileSync(outputMd, generateMarkdown(benchmark));
  console.log(`Generated: ${outputMd}`);

  const runSummary = benchmark.run_summary;
  console.log("\nSummary:");
  for (const config of Object.keys(runSummary).filter((k) => k !== "delta")) {
    console.log(`  ${titleCase(config)}: ${(runSummary[config].pass_rate.mean * 100).toFixed(1)}% pass rate`);
  }
  console.log(`  Delta:         ${runSummary.delta?.pass_rate ?? "—"}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  main();
}
