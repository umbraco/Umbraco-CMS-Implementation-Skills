import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { describe, it } from "node:test";

import { run, validateWorkspace } from "./cleanup_workspace.mjs";

function makeDir(files, name = "demo-workspace") {
  const parent = fs.mkdtempSync(path.join(os.tmpdir(), "cleanup-"));
  const root = path.join(parent, name);
  fs.mkdirSync(root);
  for (const [relative, content] of Object.entries(files)) {
    const full = path.join(root, relative);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, content);
  }
  return root;
}

const quiet = () => {
  const out = [];
  return { out, log: (m) => out.push(m), error: (m) => out.push(m) };
};

describe("validateWorkspace", () => {
  it("accepts a workspace with benchmark output in any iteration", () => {
    validateWorkspace(makeDir({ "iteration-2/benchmark.json": "{}" }));
  });

  it("refuses anything that doesn't look like an evaluator workspace", () => {
    assert.throws(() => validateWorkspace(path.join(os.tmpdir(), "no-such-workspace")), /does not exist/);
    assert.throws(() => validateWorkspace(makeDir({ "iteration-1/benchmark.json": "{}" }, "project")), /-workspace/);
    assert.throws(() => validateWorkspace(makeDir({ "notes.md": "" })), /iteration-\*/);
    assert.throws(() => validateWorkspace(makeDir({ "iteration-1/grading.json": "{}" })), /benchmark output/);
  });
});

describe("run", () => {
  it("only validates without --yes", () => {
    const ws = makeDir({ "iteration-1/benchmark.json": "{}" });
    const io = quiet();
    assert.equal(run([ws], io), 0);
    assert.ok(fs.existsSync(ws));
    assert.match(io.out[0], /nothing deleted/);
  });

  it("refuses to delete without --final-review-complete", () => {
    const ws = makeDir({ "iteration-1/benchmark.json": "{}" });
    assert.equal(run([ws, "--yes"], quiet()), 2);
    assert.ok(fs.existsSync(ws));
  });

  it("deletes with both confirmations", () => {
    const ws = makeDir({ "iteration-1/benchmark.json": "{}", "iteration-1/x/outputs/a.md": "a" });
    assert.equal(run([ws, "--yes", "--final-review-complete"], quiet()), 0);
    assert.ok(!fs.existsSync(ws));
  });

  it("never deletes a directory that fails validation, even when told to", () => {
    const ws = makeDir({ "src/app.js": "" }, "important");
    assert.equal(run([ws, "--yes", "--final-review-complete"], quiet()), 2);
    assert.ok(fs.existsSync(path.join(ws, "src/app.js")));
  });

  it("rejects unknown flags, including the retired --clean-python-caches, without deleting", () => {
    const ws = makeDir({ "iteration-1/benchmark.json": "{}" });
    const io = quiet();
    assert.equal(run([ws, "--yes", "--final-review-complete", "--clean-python-caches"], io), 2);
    assert.ok(fs.existsSync(ws));
    assert.match(io.out[0], /^error:/);
  });
});
