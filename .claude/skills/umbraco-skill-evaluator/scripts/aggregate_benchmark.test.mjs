import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { describe, it } from "node:test";

import {
  aggregateResults,
  calculateStats,
  generateBenchmark,
  generateMarkdown,
  loadRunResults,
  orderConfigs,
  signed,
} from "./aggregate_benchmark.mjs";

function makeIteration(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "aggregate-"));
  for (const [relative, content] of Object.entries(files)) {
    const full = path.join(root, relative);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, typeof content === "string" ? content : JSON.stringify(content));
  }
  return root;
}

const grading = (passed, total, extra = {}) => ({
  expectations: [{ text: "t", passed: true, evidence: "e" }],
  summary: { passed, failed: total - passed, total, pass_rate: passed / total },
  ...extra,
});

describe("calculateStats", () => {
  it("returns mean, sample stddev, min and max rounded to 4 places", () => {
    assert.deepEqual(calculateStats([1, 2, 3, 4]), { mean: 2.5, stddev: 1.291, min: 1, max: 4 });
    assert.deepEqual(calculateStats([5]), { mean: 5, stddev: 0, min: 5, max: 5 });
    assert.deepEqual(calculateStats([]), { mean: 0, stddev: 0, min: 0, max: 0 });
  });
});

describe("signed", () => {
  it("formats like Python's {:+.Nf}", () => {
    assert.equal(signed(0.15, 2), "+0.15");
    assert.equal(signed(0, 2), "+0.00");
    assert.equal(signed(-3.04, 1), "-3.0");
    assert.equal(signed(13135.4, 0), "+13135");
  });
});

describe("orderConfigs", () => {
  it("puts the primary first and its baseline second", () => {
    assert.deepEqual(orderConfigs(["without_skill", "extra", "with_skill"]), ["with_skill", "without_skill", "extra"]);
    assert.deepEqual(orderConfigs(["old_skill", "new_skill"]), ["new_skill", "old_skill"]);
  });
});

describe("loadRunResults", () => {
  it("reads grading, metadata and timing in the primary layout", () => {
    const dir = makeIteration({
      "b-eval/eval_metadata.json": { eval_id: 2, eval_name: "b" },
      "b-eval/with_skill/grading.json": grading(9, 9, { timing: { total_duration_seconds: 12.5 } }),
      "b-eval/with_skill/timing.json": { total_tokens: 1000, total_duration_seconds: 99 },
      "b-eval/without_skill/grading.json": grading(3, 9),
      "b-eval/without_skill/timing.json": { total_tokens: 800, total_duration_seconds: 20 },
      "skill-snapshot/SKILL.md": "ignored",
    });

    const results = loadRunResults(dir);

    assert.deepEqual(Object.keys(results), ["with_skill", "without_skill"]);
    const w = results.with_skill[0];
    assert.equal(w.eval_id, 2);
    assert.equal(w.eval_name, "b");
    assert.equal(w.passed, 9);
    assert.equal(w.time_seconds, 12.5, "grading.json timing wins over timing.json");
    assert.equal(w.tokens, 1000, "tokens always come from timing.json");
    assert.equal(results.without_skill[0].time_seconds, 20, "timing.json is the fallback for time");
  });

  it("falls back to the legacy run-N layout and skips configs without grading", () => {
    const dir = makeIteration({
      "e/with_skill/run-2/grading.json": grading(1, 2),
      "e/with_skill/run-2/timing.json": { total_tokens: 5 },
      "e/without_skill/outputs/a.md": "no grading here",
    });
    const results = loadRunResults(dir);
    assert.deepEqual(Object.keys(results), ["with_skill"]);
    assert.equal(results.with_skill[0].run_number, 2);
    assert.equal(results.with_skill[0].tokens, 5);
    assert.equal(results.with_skill[0].eval_id, "e", "eval id defaults to the folder name");
  });

  it("skips a config whose grading.json is invalid", () => {
    const dir = makeIteration({ "e/with_skill/grading.json": "{not json" });
    assert.deepEqual(loadRunResults(dir), {});
  });
});

describe("aggregateResults and generateBenchmark", () => {
  const dir = makeIteration({
    "a/eval_metadata.json": { eval_id: 1, eval_name: "a" },
    "a/with_skill/grading.json": grading(9, 9),
    "a/with_skill/timing.json": { total_tokens: 110000, total_duration_seconds: 300 },
    "a/without_skill/grading.json": grading(4, 9),
    "a/without_skill/timing.json": { total_tokens: 100000, total_duration_seconds: 250 },
    "b/eval_metadata.json": { eval_id: 2, eval_name: "b" },
    "b/without_skill/grading.json": grading(1, 9),
    "b/without_skill/timing.json": { total_tokens: 90000, total_duration_seconds: 200 },
    "b/with_skill/grading.json": grading(9, 9),
    "b/with_skill/timing.json": { total_tokens: 108000, total_duration_seconds: 280 },
  });

  it("computes the delta as primary minus baseline", () => {
    const summary = aggregateResults(loadRunResults(dir));
    assert.deepEqual(Object.keys(summary), ["with_skill", "without_skill", "delta"]);
    assert.equal(summary.with_skill.pass_rate.mean, 1);
    assert.equal(summary.without_skill.pass_rate.mean, 0.2778);
    assert.deepEqual(summary.delta, { pass_rate: "+0.72", time_seconds: "+65.0", tokens: "+14000" });
  });

  it("builds benchmark.json with paired runs, primary first", () => {
    const benchmark = generateBenchmark(dir, "demo", "plugins/x");
    assert.equal(benchmark.metadata.skill_name, "demo");
    assert.deepEqual(benchmark.metadata.evals_run, [1, 2]);
    assert.equal(benchmark.metadata.runs_per_configuration, 2);
    assert.match(benchmark.metadata.timestamp, /^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$/);
    assert.deepEqual(benchmark.runs.map((r) => `${r.configuration}:${r.eval_id}`), [
      "with_skill:1", "with_skill:2", "without_skill:1", "without_skill:2",
    ]);
    assert.deepEqual(benchmark.notes, []);
  });

  it("renders benchmark.md with the summary table and notes", () => {
    const benchmark = generateBenchmark(dir, "demo");
    benchmark.notes = ["first note"];
    const md = generateMarkdown(benchmark);
    assert.match(md, /^# Skill Benchmark: demo/);
    assert.match(md, /\| Metric \| With Skill \| Without Skill \| Delta \|/);
    assert.match(md, /\| Pass Rate \| 100% ± 0% \| 28% ± 24% \| \+0\.72 \|/);
    assert.match(md, /\| Time \| 290\.0s ± 14\.1s \| 225\.0s ± 35\.4s \| \+65\.0s \|/);
    assert.match(md, /## Notes\n\n- first note$/);
  });
});
