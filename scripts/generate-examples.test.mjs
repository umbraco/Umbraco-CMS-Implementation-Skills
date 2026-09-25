// Tests for the example generator and its placeholder checker.
//
// Run: node --test scripts/*.test.mjs
//
// Each test builds a throwaway skill tree in a temp dir, so every check is
// exercised against a case that must fail as well as one that must pass. The
// placeholder checker exists because an unsubstituted `<token>` still compiles
// and then silently does nothing, so watching it actually fail is the point.

import test from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { ManifestError, check } from "./check-placeholders.mjs";
import { generate, lint, manifests, substitute } from "./generate-examples.mjs";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const CLI = path.join(HERE, "generate-examples.mjs");

/**
 * Materialise a tree from a {relativePath: contents} map. Directories are
 * created implicitly; a null value means "create an empty directory".
 */
function makeTree(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "skills-examples-"));
  for (const [rel, contents] of Object.entries(files)) {
    const full = path.join(root, rel);
    if (contents === null) {
      fs.mkdirSync(full, { recursive: true });
      continue;
    }
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, contents, "utf8");
  }
  return root;
}

const SKILL = "plugins/impl/skills/demo";
const manifest = (body) => JSON.stringify(body);

/** A skill tree with one approach. `assets` and `body` override the defaults. */
function skillTree({ assets = {}, body = { namespace: "N", assets: ["Foo.cs"] }, approach = "a" }) {
  const files = { [`${SKILL}/examples/${approach}/.generate.json`]: manifest(body) };
  for (const [name, contents] of Object.entries(assets)) {
    files[`${SKILL}/assets/${name}`] = contents;
  }
  return files;
}

/** Run check() capturing what it would have written to stderr. */
function checkCapturing(manifestPath, assetPaths) {
  const lines = [];
  const status = check(manifestPath, assetPaths, { log: (m) => lines.push(m) });
  return { status, output: lines.join("\n") };
}

/** Run generate() capturing both streams. */
function generateCapturing(manifestPath, outDir) {
  const out = [];
  const err = [];
  const status = generate(manifestPath, outDir, {
    log: (m) => out.push(m),
    errorLog: (m) => err.push(m),
  });
  return { status, out: out.join("\n"), err: err.join("\n") };
}

// --- the placeholder checker ------------------------------------------------

test("accepts an asset whose every placeholder is declared", () => {
  const root = makeTree(
    skillTree({
      assets: { "Foo.cs": 'namespace <Namespace>;\nvar x = Has("<alias>");\n' },
      body: { namespace: "N", placeholders: { "<alias>": "a" }, assets: ["Foo.cs"] },
    }),
  );
  const { status } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 0);
});

test("rejects an undeclared placeholder inside a string literal", () => {
  const root = makeTree(
    skillTree({ assets: { "Foo.cs": 'var x = Has("<sneaky>");\n' } }),
  );
  const { status, output } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 1);
  assert.match(output, /carries <sneaky>, which the manifest does not declare/);
  assert.match(output, /Foo\.cs:1/);
});

test("rejects an undeclared placeholder in a namespace declaration", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Other>.Thing;\n" } }));
  const { status, output } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 1);
  assert.match(output, /carries <Other>/);
});

test("ignores generic type arguments — they are not placeholder positions", () => {
  const root = makeTree(
    skillTree({
      assets: {
        "Foo.cs": "Task<IReadOnlyList<ITemplate>> Get();\nvar b = Value<bool>();\nif (a < b) {}\n",
      },
    }),
  );
  const { status } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 0);
});

test("ignores XML element names written in prose", () => {
  const root = makeTree(
    skillTree({ assets: { "Foo.cs": "// Only <loc> + <lastmod> are emitted\n" } }),
  );
  const { status } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 0);
});

test("rejects a declared placeholder that no asset uses", () => {
  const root = makeTree(
    skillTree({
      assets: { "Foo.cs": "namespace <Namespace>;\n" },
      body: { namespace: "N", placeholders: { "<ghost>": "g" }, assets: ["Foo.cs"] },
    }),
  );
  const { status, output } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 1);
  assert.match(output, /declares <ghost>, but no listed asset uses it/);
});

test("never reports <Namespace> as unused — assets need not mention it", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "// nothing at all\n" } }));
  const { status } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 0);
});

test("reports a token used twice on one line once, but once per line across lines", () => {
  const root = makeTree(
    skillTree({ assets: { "Foo.cs": 'A("<t>") && B("<t>");\nC("<t>");\n' } }),
  );
  const { output } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  const occurrences = output.match(/carries <t>/g) || [];
  assert.equal(occurrences.length, 2);
});

test("honours backslash escapes when finding string literals", () => {
  // The \" must not end the literal early, so <in> is seen as inside a string.
  const root = makeTree(skillTree({ assets: { "Foo.cs": 'var s = "a\\"b <in> c";\n' } }));
  const { status, output } = checkCapturing(path.join(root, SKILL, "examples/a/.generate.json"), [
    path.join(root, SKILL, "assets/Foo.cs"),
  ]);
  assert.equal(status, 1);
  assert.match(output, /carries <in>/);
});

test("reports a manifest with no namespace as a message, not a crash", () => {
  const root = makeTree(skillTree({ body: { assets: ["Foo.cs"] } }));
  assert.throws(
    () => check(path.join(root, SKILL, "examples/a/.generate.json"), []),
    (error) => error instanceof ManifestError && /missing a non-empty "namespace"/.test(error.message),
  );
});

test("reports unparseable manifest JSON as a message, not a crash", () => {
  const root = makeTree({ [`${SKILL}/examples/a/.generate.json`]: "{ not json" });
  assert.throws(
    () => check(path.join(root, SKILL, "examples/a/.generate.json"), []),
    (error) => error instanceof ManifestError && /is not valid JSON/.test(error.message),
  );
});

test("rejects a manifest that is valid JSON but not an object", () => {
  const root = makeTree({ [`${SKILL}/examples/a/.generate.json`]: "[1, 2]" });
  assert.throws(
    () => check(path.join(root, SKILL, "examples/a/.generate.json"), []),
    (error) => error instanceof ManifestError && /must contain a JSON object/.test(error.message),
  );
});

// --- substitution -----------------------------------------------------------

test("substitute replaces every occurrence of every token", () => {
  const result = substitute("<A> and <A> and <B>", { "<A>": "x", "<B>": "y" });
  assert.equal(result, "x and x and y");
});

test("substitute treats tokens literally, not as patterns", () => {
  assert.equal(substitute("a.c", { "a.c": "ok" }), "ok");
});

// --- generation -------------------------------------------------------------

test("projects assets into the output dir with placeholders substituted", () => {
  const root = makeTree(
    skillTree({
      assets: { "Foo.cs": 'namespace <Namespace>;\nvar a = "<alias>";\n' },
      body: { namespace: "Real.Ns", placeholders: { "<alias>": "resolved" }, assets: ["Foo.cs"] },
    }),
  );
  const out = path.join(root, "out");
  const { status } = generateCapturing(path.join(root, SKILL, "examples/a/.generate.json"), out);
  assert.equal(status, 0);
  assert.equal(
    fs.readFileSync(path.join(out, "Foo.cs"), "utf8"),
    'namespace Real.Ns;\nvar a = "resolved";\n',
  );
});

test("skips a skill whose assets/ folder is absent, so a build works pre-merge", () => {
  const root = makeTree({ [`${SKILL}/examples/a/.generate.json`]: manifest({ namespace: "N" }) });
  const out = path.join(root, "out");
  const { status, out: stdout } = generateCapturing(
    path.join(root, SKILL, "examples/a/.generate.json"),
    out,
  );
  assert.equal(status, 0);
  assert.match(stdout, /skip a — no assets\/ on this branch/);
  assert.equal(fs.existsSync(out), false);
});

test("a declare-only manifest generates nothing but still succeeds", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "x\n" }, body: { namespace: "N" } }));
  const out = path.join(root, "out");
  const { status } = generateCapturing(path.join(root, SKILL, "examples/a/.generate.json"), out);
  assert.equal(status, 0);
  assert.deepEqual(fs.readdirSync(out), []);
});

test("fails when a listed asset is missing from assets/", () => {
  const root = makeTree(
    skillTree({ assets: { "Other.cs": "x\n" }, body: { namespace: "N", assets: ["Foo.cs"] } }),
  );
  const { status, err } = generateCapturing(
    path.join(root, SKILL, "examples/a/.generate.json"),
    path.join(root, "out"),
  );
  assert.equal(status, 1);
  assert.match(err, /'Foo\.cs' is listed in .* but missing from assets\//);
});

test("an undeclared placeholder blocks generation entirely", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": 'var a = "<sneaky>";\n' } }));
  const out = path.join(root, "out");
  const { status } = generateCapturing(path.join(root, SKILL, "examples/a/.generate.json"), out);
  assert.equal(status, 1);
  // Nothing must be written, or a later build could compile the broken projection.
  assert.equal(fs.existsSync(out), false);
});

test("leaves an unchanged file's mtime alone so MSBuild can skip the compile", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Namespace>;\n" } }));
  const manifestPath = path.join(root, SKILL, "examples/a/.generate.json");
  const out = path.join(root, "out");
  generateCapturing(manifestPath, out);

  const generated = path.join(out, "Foo.cs");
  // Backdate well beyond filesystem timestamp granularity, so "unchanged" is unambiguous.
  const past = new Date(Date.parse("2020-01-01T00:00:00Z"));
  fs.utimesSync(generated, past, past);
  const before = fs.statSync(generated).mtimeMs;

  generateCapturing(manifestPath, out);
  assert.equal(fs.statSync(generated).mtimeMs, before);
});

test("rewrites when the asset's content has changed", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Namespace>;\n" } }));
  const manifestPath = path.join(root, SKILL, "examples/a/.generate.json");
  const out = path.join(root, "out");
  generateCapturing(manifestPath, out);

  fs.writeFileSync(path.join(root, SKILL, "assets/Foo.cs"), "namespace <Namespace>;\n// added\n");
  generateCapturing(manifestPath, out);
  assert.match(fs.readFileSync(path.join(out, "Foo.cs"), "utf8"), /\/\/ added/);
});

test("preserves CRLF byte-for-byte rather than normalising line endings", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Namespace>;\r\nvar a = 1;\r\n" } }));
  const out = path.join(root, "out");
  generateCapturing(path.join(root, SKILL, "examples/a/.generate.json"), out);
  assert.equal(fs.readFileSync(path.join(out, "Foo.cs"), "utf8"), "namespace N;\r\nvar a = 1;\r\n");
});

// --- manifest discovery -----------------------------------------------------

test("discovers example manifests and ignores the copies under bin/ and obj/", () => {
  const root = makeTree({
    [`${SKILL}/examples/a/.generate.json`]: manifest({ namespace: "N" }),
    [`${SKILL}/examples/b/.generate.json`]: manifest({ namespace: "N" }),
    [`${SKILL}/obj/examples/a/.generate.json`]: manifest({ namespace: "COPY" }),
    [`${SKILL}/bin/examples/a/.generate.json`]: manifest({ namespace: "COPY" }),
  });
  const found = manifests(root).map((p) => path.relative(root, p));
  assert.deepEqual(found, [
    path.join(SKILL, "examples/a/.generate.json"),
    path.join(SKILL, "examples/b/.generate.json"),
  ]);
});

test("only counts a manifest sitting at examples/<approach>/", () => {
  const root = makeTree({
    [`${SKILL}/examples/.generate.json`]: manifest({ namespace: "N" }),
    [`${SKILL}/.generate.json`]: manifest({ namespace: "N" }),
    [`${SKILL}/examples/a/nested/.generate.json`]: manifest({ namespace: "N" }),
  });
  assert.deepEqual(manifests(root), []);
});

test("discovery returns an empty list when there is no plugins/ dir at all", () => {
  assert.deepEqual(manifests(makeTree({ "README.md": "hi" })), []);
});

// --- lint -------------------------------------------------------------------

test("lint reports the number of assets checked across all manifests", () => {
  const root = makeTree({
    ...skillTree({
      assets: { "Foo.cs": "namespace <Namespace>;\n", "Bar.cs": "namespace <Namespace>;\n" },
      body: { namespace: "N", assets: ["Foo.cs", "Bar.cs"] },
    }),
  });
  const lines = [];
  const status = lint({ repoRoot: root, log: (m) => lines.push(m), errorLog: (m) => lines.push(m) });
  assert.equal(status, 0);
  assert.match(lines.join("\n"), /placeholders declared for every asset \(2 file\(s\) checked\)/);
});

test("lint fails when any manifest has a placeholder mismatch", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": 'var a = "<sneaky>";\n' } }));
  const errors = [];
  const status = lint({ repoRoot: root, log: () => {}, errorLog: (m) => errors.push(m) });
  assert.equal(status, 1);
  assert.match(errors.join("\n"), /carries <sneaky>/);
});

test("lint skips a manifest whose assets/ is absent without failing", () => {
  const root = makeTree({ [`${SKILL}/examples/a/.generate.json`]: manifest({ namespace: "N" }) });
  const lines = [];
  const status = lint({ repoRoot: root, log: (m) => lines.push(m), errorLog: (m) => lines.push(m) });
  assert.equal(status, 0);
  assert.match(lines.join("\n"), /skip a — no assets\/ on this branch/);
});

// --- command line -----------------------------------------------------------

/** Run the CLI, returning {status, stdout, stderr} without throwing on failure. */
function cli(args, cwd) {
  try {
    const stdout = execFileSync(process.execPath, [CLI, ...args], { cwd, encoding: "utf8" });
    return { status: 0, stdout };
  } catch (error) {
    return { status: error.status, stdout: error.stdout || "", stderr: error.stderr || "" };
  }
}

test("accepts --manifest/--out as separate arguments, as MSBuild passes them", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Namespace>;\n" } }));
  const result = cli(
    ["--manifest", path.join(root, SKILL, "examples/a/.generate.json"), "--out", path.join(root, "o")],
    root,
  );
  assert.equal(result.status, 0);
  assert.equal(fs.readFileSync(path.join(root, "o/Foo.cs"), "utf8"), "namespace N;\n");
});

test("accepts the --manifest=VALUE form too", () => {
  const root = makeTree(skillTree({ assets: { "Foo.cs": "namespace <Namespace>;\n" } }));
  const result = cli(
    [
      `--manifest=${path.join(root, SKILL, "examples/a/.generate.json")}`,
      `--out=${path.join(root, "o")}`,
    ],
    root,
  );
  assert.equal(result.status, 0);
  assert.ok(fs.existsSync(path.join(root, "o/Foo.cs")));
});

test("exits 2 with usage when --manifest and --out are both absent", () => {
  const result = cli([], makeTree({ "README.md": "hi" }));
  assert.equal(result.status, 2);
  assert.match(result.stderr, /--manifest and --out are required/);
});

test("exits 2 on an unrecognised argument rather than silently ignoring it", () => {
  const result = cli(["--wat"], makeTree({ "README.md": "hi" }));
  assert.equal(result.status, 2);
  assert.match(result.stderr, /Unknown argument: --wat/);
});

test("exits 1 with a message, not a stack trace, on a broken manifest", () => {
  const root = makeTree({ [`${SKILL}/examples/a/.generate.json`]: "{ nope" });
  const result = cli(
    ["--manifest", path.join(root, SKILL, "examples/a/.generate.json"), "--out", path.join(root, "o")],
    root,
  );
  assert.equal(result.status, 1);
  assert.match(result.stderr, /ERROR: .*is not valid JSON/);
  assert.doesNotMatch(result.stderr, /at .*Object\./); // no stack frames
});
