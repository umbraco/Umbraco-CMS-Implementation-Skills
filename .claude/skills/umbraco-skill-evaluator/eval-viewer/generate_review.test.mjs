import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { after, before, describe, it } from "node:test";

import {
  createReviewServer,
  embedFile,
  findRuns,
  generateHtml,
  getMimeType,
  loadPreviousIteration,
} from "./generate_review.mjs";

/** Build a throwaway workspace from { "relative/path": content } entries. */
function makeWorkspace(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "eval-viewer-"));
  for (const [relative, content] of Object.entries(files)) {
    const full = path.join(root, relative);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, content);
  }
  return root;
}

/** Pull EMBEDDED_DATA back out of a generated page the way the browser would. */
function embeddedData(html) {
  const match = html.match(/const EMBEDDED_DATA = (.*);\n/);
  assert.ok(match, "EMBEDDED_DATA assignment not found");
  return JSON.parse(match[1]);
}

describe("findRuns", () => {
  it("finds every directory with outputs/, ordered by eval_id then id", () => {
    const ws = makeWorkspace({
      "b-eval/eval_metadata.json": JSON.stringify({ eval_id: 2, prompt: "second" }),
      "b-eval/with_skill/outputs/answer.md": "b",
      "a-eval/eval_metadata.json": JSON.stringify({ eval_id: 1, prompt: "first" }),
      "a-eval/without_skill/outputs/answer.md": "a2",
      "a-eval/with_skill/outputs/answer.md": "a1",
      "node_modules/x/outputs/ignored.md": "skip me",
    });

    const runs = findRuns(ws);

    assert.deepEqual(runs.map((r) => r.id), [
      "a-eval-with_skill",
      "a-eval-without_skill",
      "b-eval-with_skill",
    ]);
    assert.equal(runs[0].prompt, "first");
    assert.equal(runs[2].eval_id, 2);
  });

  it("falls back to the transcript's Eval Prompt section, then a placeholder", () => {
    const ws = makeWorkspace({
      "x/run/transcript.md": "## Eval Prompt\n\nDo the thing\n\n## Steps\n",
      "x/run/outputs/a.txt": "",
      "y/run/outputs/a.txt": "",
    });
    const [x, y] = findRuns(ws);
    assert.equal(x.prompt, "Do the thing");
    assert.equal(y.prompt, "(No prompt found)");
  });

  it("excludes metadata files from outputs and reads grading beside the run", () => {
    const ws = makeWorkspace({
      "e/run/outputs/final-answer.md": "hi",
      "e/run/outputs/transcript.md": "nope",
      "e/run/outputs/grading.json": "{}",
      "e/run/grading.json": JSON.stringify({ summary: { passed: 3 } }),
    });
    const [run] = findRuns(ws);
    assert.deepEqual(run.outputs.map((o) => o.name), ["final-answer.md"]);
    assert.equal(run.grading.summary.passed, 3);
  });
});

describe("embedFile", () => {
  it("embeds text, including C#/Razor, as text", () => {
    const ws = makeWorkspace({ "a.cshtml": "@inherits X", "b.cs": "class B {}" });
    assert.deepEqual(embedFile(path.join(ws, "a.cshtml")), { name: "a.cshtml", type: "text", content: "@inherits X" });
    assert.equal(embedFile(path.join(ws, "b.cs")).type, "text");
  });

  it("embeds images, PDFs and xlsx as base64, and unknown files as downloads", () => {
    const ws = makeWorkspace({ "i.png": "PNG", "d.pdf": "PDF", "s.xlsx": "XL", "x.bin": "BIN" });
    const b64 = (s) => Buffer.from(s).toString("base64");
    assert.equal(embedFile(path.join(ws, "i.png")).data_uri, `data:image/png;base64,${b64("PNG")}`);
    assert.equal(embedFile(path.join(ws, "d.pdf")).type, "pdf");
    assert.equal(embedFile(path.join(ws, "s.xlsx")).data_b64, b64("XL"));
    assert.deepEqual(embedFile(path.join(ws, "x.bin")), {
      name: "x.bin",
      type: "binary",
      mime: "application/octet-stream",
      data_uri: `data:application/octet-stream;base64,${b64("BIN")}`,
    });
  });

  it("maps the overridden MIME types", () => {
    assert.equal(getMimeType("a.svg"), "image/svg+xml");
    assert.equal(getMimeType("a.DOCX"), "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
  });
});

describe("generateHtml", () => {
  it("keeps </script> in run output from closing the embedding script", () => {
    const runs = [{ id: "r", prompt: "p", outputs: [{ name: "f.html", type: "text", content: "<script>x()</script>" }] }];
    const html = generateHtml(runs, "s");
    assert.equal((html.match(/<\/script>/gi) ?? []).length, (generateHtml([], "s").match(/<\/script>/gi) ?? []).length,
      "only the template's own </script> tags may appear");
    assert.equal(embeddedData(html).runs[0].outputs[0].content, "<script>x()</script>");
  });

  it("injects output containing $& and $' literally", () => {
    const content = "cost: $& and $' and $$";
    const html = generateHtml([{ id: "r", prompt: "p", outputs: [{ name: "a.md", type: "text", content }] }], "s");
    assert.equal(embeddedData(html).runs[0].outputs[0].content, content);
  });

  it("embeds previous feedback/outputs and the benchmark", () => {
    const html = generateHtml([], "skill", { r: { feedback: "fb", outputs: [{ name: "o" }] }, q: { feedback: "", outputs: [] } }, { runs: [] });
    const data = embeddedData(html);
    assert.deepEqual(data.previous_feedback, { r: "fb" });
    assert.deepEqual(Object.keys(data.previous_outputs), ["r"]);
    assert.deepEqual(data.benchmark, { runs: [] });
    assert.equal(data.skill_name, "skill");
  });
});

describe("loadPreviousIteration", () => {
  it("joins non-empty feedback to runs, keeping feedback for runs that no longer exist", () => {
    const ws = makeWorkspace({
      "e/run/outputs/a.md": "x",
      "feedback.json": JSON.stringify({
        reviews: [
          { run_id: "e-run", feedback: "good" },
          { run_id: "gone", feedback: "old" },
          { run_id: "blank", feedback: "   " },
        ],
      }),
    });
    const previous = loadPreviousIteration(ws);
    assert.equal(previous["e-run"].feedback, "good");
    assert.equal(previous["e-run"].outputs.length, 1);
    assert.deepEqual(previous.gone, { feedback: "old", outputs: [] });
    assert.equal(previous.blank, undefined);
  });
});

describe("review server", () => {
  let server;
  let base;
  let ws;

  before(async () => {
    ws = makeWorkspace({ "e/run/outputs/a.md": "x" });
    server = createReviewServer({
      workspace: ws,
      skillName: "s",
      feedbackPath: path.join(ws, "feedback.json"),
      previous: {},
      benchmarkPath: null,
    });
    // Reject rather than hang if the environment forbids binding a local port.
    await new Promise((resolve, reject) => {
      server.once("error", reject);
      server.listen(0, "127.0.0.1", resolve);
    });
    base = `http://127.0.0.1:${server.address().port}`;
  });

  after(() => new Promise((resolve) => server.close(resolve)));

  it("serves the page with the current runs", async () => {
    const res = await fetch(`${base}/`);
    assert.equal(res.status, 200);
    assert.match(res.headers.get("content-type"), /text\/html/);
    assert.equal(embeddedData(await res.text()).runs[0].id, "e-run");
  });

  it("round-trips feedback and rejects malformed bodies", async () => {
    assert.deepEqual(await (await fetch(`${base}/api/feedback`)).json(), {});

    const feedback = { reviews: [{ run_id: "e-run", feedback: "nice" }], status: "complete" };
    const saved = await fetch(`${base}/api/feedback`, { method: "POST", body: JSON.stringify(feedback) });
    assert.equal(saved.status, 200);
    assert.deepEqual(JSON.parse(fs.readFileSync(path.join(ws, "feedback.json"), "utf8")), feedback);
    assert.deepEqual(await (await fetch(`${base}/api/feedback`)).json(), feedback);

    const bad = await fetch(`${base}/api/feedback`, { method: "POST", body: "[]" });
    assert.equal(bad.status, 500);
  });

  it("answers 404 for anything else", async () => {
    assert.equal((await fetch(`${base}/nope`)).status, 404);
  });
});
