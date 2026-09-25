// Tests for the strict SKILL.md frontmatter reader.
//
// Run: node --test scripts/
//
// The parser deliberately accepts only the subset of YAML that SKILL.md
// frontmatter actually uses, and treats anything it does not recognise as an
// error. These tests pin both halves of that contract: the shapes real skills
// use must parse, and the shapes that broke a real skill must fail loudly.

import test from "node:test";
import assert from "node:assert/strict";

import { parseFrontmatter } from "./frontmatter.mjs";

/** Build a SKILL.md-shaped document from a frontmatter block and a body. */
const doc = (fm, body = "# Heading\n\nSome body.\n") => `---\n${fm}\n---\n${body}`;

function ok(text) {
  const result = parseFrontmatter(text);
  assert.equal(result.ok, true, `expected parse to succeed, got: ${result.error}`);
  return result;
}

function fails(text, expectedFragment) {
  const result = parseFrontmatter(text);
  assert.equal(result.ok, false, "expected parse to fail but it succeeded");
  assert.match(result.error, expectedFragment);
  return result;
}

// ---------------------------------------------------------------- accepted

test("plain scalar and folded block scalar — the shape every real skill uses", () => {
  const { data, body } = ok(
    doc(
      [
        "name: umbraco-sitemap",
        "description: >",
        "  Add an XML sitemap to an Umbraco 17+ site. Offers two approaches.",
        "  Use this skill whenever the user asks to add a sitemap.",
      ].join("\n"),
    ),
  );
  assert.equal(data.name, "umbraco-sitemap");
  assert.equal(
    data.description,
    "Add an XML sitemap to an Umbraco 17+ site. Offers two approaches. " +
      "Use this skill whenever the user asks to add a sitemap.\n",
  );
  assert.equal(body, "# Heading\n\nSome body.\n");
});

test("folded block scalar turns a blank line into a newline", () => {
  const { data } = ok(doc(["description: >", "  first", "", "  second"].join("\n")));
  assert.equal(data.description, "first\nsecond\n");
});

test("literal block scalar preserves newlines", () => {
  const { data } = ok(doc(["description: |", "  line one", "  line two"].join("\n")));
  assert.equal(data.description, "line one\nline two\n");
});

test("strip chomping indicator removes the trailing newline", () => {
  const { data } = ok(doc(["description: >-", "  no trailing newline"].join("\n")));
  assert.equal(data.description, "no trailing newline");
});

test("a colon is fine inside a block scalar", () => {
  // This is the fixed form of the bug that motivated the validator.
  const { data } = ok(
    doc(["description: >", "  cousin of skill-creator: it skips drafting."].join("\n")),
  );
  assert.equal(data.description, "cousin of skill-creator: it skips drafting.\n");
});

test("a colon is fine inside a quoted scalar", () => {
  const { data } = ok(doc(['description: "cousin of skill-creator: it skips"'].join("\n")));
  assert.equal(data.description, "cousin of skill-creator: it skips");
  const single = ok(doc("description: 'a: b'"));
  assert.equal(single.data.description, "a: b");
});

test("a colon not followed by a space stays part of a plain scalar", () => {
  const { data } = ok(doc("description: see https://example.com/docs"));
  assert.equal(data.description, "see https://example.com/docs");
});

test("escapes inside a double-quoted scalar are decoded", () => {
  const { data } = ok(doc('description: "a \\"quoted\\" word\\nand a newline"'));
  assert.equal(data.description, 'a "quoted" word\nand a newline');
});

test("nested mapping parses, so metadata is readable", () => {
  const { data } = ok(
    doc(["name: x", "metadata:", "  version: 1.2.0", "  author: someone"].join("\n")),
  );
  assert.deepEqual(data.metadata, { version: "1.2.0", author: "someone" });
});

test("sequence parses, so allowed-tools is readable", () => {
  const { data } = ok(doc(["name: x", "allowed-tools:", "  - Read", "  - Write"].join("\n")));
  assert.deepEqual(data["allowed-tools"], ["Read", "Write"]);
});

test("comments and blank lines are ignored", () => {
  const { data } = ok(
    doc(["# leading comment", "name: x", "", "description: y # trailing"].join("\n")),
  );
  assert.deepEqual(data, { name: "x", description: "y" });
});

test("a '#' inside a quoted scalar is not a comment", () => {
  const { data } = ok(doc('description: "tagged #hashtag"'));
  assert.equal(data.description, "tagged #hashtag");
});

test("CRLF line endings parse — the repo is checked out on Windows too", () => {
  const text = "---\r\nname: x\r\ndescription: >\r\n  folded line\r\n---\r\n# Body\r\n";
  const { data, body } = ok(text);
  assert.equal(data.name, "x");
  assert.equal(data.description, "folded line\n");
  assert.equal(body, "# Body\r\n");
});

test("an empty body is fine", () => {
  const { body } = ok("---\nname: x\n---\n");
  assert.equal(body, "");
});

// ---------------------------------------------------------------- rejected

test("REGRESSION: an unquoted colon-space in a plain scalar is an error", () => {
  // The exact bug that shipped in umbraco-skill-evaluator. Real YAML reports
  // "mapping values are not allowed here"; the Vercel CLI silently dropped the
  // skill. This parser must fail loudly rather than accept it.
  fails(
    doc("description: cousin of skill-creator: it skips drafting"),
    /unquoted ':' in a plain value/,
  );
});

test("a plain scalar ending in a colon is an error", () => {
  fails(doc("description: trailing colon:"), /unquoted ':' in a plain value/);
});

test("a tab in indentation is an error", () => {
  fails(doc(["metadata:", "\tversion: 1"].join("\n")), /tab/);
});

test("a duplicate key is an error", () => {
  fails(doc(["name: a", "name: b"].join("\n")), /duplicate key 'name'/);
});

test("an unterminated quote is an error", () => {
  fails(doc('description: "never closed'), /unterminated/);
  fails(doc("description: 'never closed"), /unterminated/);
});

test("a missing frontmatter block is an error", () => {
  fails("# Just a heading\n", /missing YAML frontmatter/);
  fails("", /missing YAML frontmatter/);
});

test("an unterminated frontmatter block is an error", () => {
  fails("---\nname: x\n", /missing YAML frontmatter/);
});

test("a sequence at the root is not a mapping", () => {
  fails(doc(["- one", "- two"].join("\n")), /must be a YAML mapping/);
});

test("an empty frontmatter block is not a mapping", () => {
  fails("---\n\n---\nbody\n", /must be a YAML mapping/);
});

test("flow syntax is rejected rather than half-understood", () => {
  fails(doc("metadata: {version: 1}"), /flow/);
  fails(doc("allowed-tools: [Read, Write]"), /flow/);
});

test("anchors, aliases and tags are rejected", () => {
  fails(doc("name: &anchor x"), /anchor, alias or tag/);
  fails(doc("name: *alias"), /anchor, alias or tag/);
  fails(doc("name: !!str x"), /anchor, alias or tag/);
});

test("a line that is neither a key nor a list item is an error", () => {
  fails(doc(["name: x", "just some prose"].join("\n")), /not a 'key: value' pair/);
});

test("an unexpected indent is an error rather than a guess", () => {
  fails(doc(["name: x", "  orphaned: y"].join("\n")), /unexpected indent/);
});

test("a dedent to a column that was never open is an error", () => {
  fails(
    doc(["metadata:", "    a: 1", "  b: 2"].join("\n")),
    /does not match any open block/,
  );
});
