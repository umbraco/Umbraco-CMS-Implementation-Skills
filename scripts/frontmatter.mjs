// A strict reader for SKILL.md YAML frontmatter — no dependencies.
//
// This is NOT a general YAML parser, and deliberately so. It accepts only the
// subset of YAML that SKILL.md frontmatter actually uses:
//
//   key: plain scalar
//   key: "double quoted"  |  'single quoted'
//   key: >   |  key: |    (with optional - / + chomping)
//   key:                  (followed by a more-indented mapping or - sequence)
//
// Every other line shape is an ERROR rather than a best-effort guess. That
// direction matters: this file exists to catch frontmatter that real consumers
// silently reject, so being stricter than YAML produces a loud, fixable false
// positive, while being more lenient would reproduce the bug it guards against
// (an unquoted ': ' in a description made a real skill unparseable, and the
// Vercel Skills CLI dropped it without printing anything).
//
// Consequence for authors: if a description needs a colon, quote it or use a
// '>' block scalar. That is the house style anyway.

const FRONTMATTER_RE = /^---[ \t]*\r?\n([\s\S]*?)\r?\n---[ \t]*(?:\r?\n|$)/;

/** Characters that begin a YAML construct this reader does not support. */
const FLOW_START = new Set(["{", "["]);
const NODE_PROPERTY_START = new Set(["&", "*", "!"]);

class ParseError extends Error {
  constructor(message, lineNumber) {
    super(lineNumber == null ? message : `line ${lineNumber}: ${message}`);
  }
}

/**
 * Parse a SKILL.md document's frontmatter.
 *
 * @param {string} text Full file contents.
 * @returns {{ok: true, data: Record<string, unknown>, body: string}
 *          | {ok: false, error: string}}
 */
export function parseFrontmatter(text) {
  const match = FRONTMATTER_RE.exec(text);
  if (!match) {
    return {
      ok: false,
      error: "missing YAML frontmatter block (--- ... ---) at top of file",
    };
  }

  const body = text.slice(match[0].length);
  const lines = match[1].split("\n").map((line) => line.replace(/\r$/, ""));

  try {
    const data = parseRoot(lines);
    return { ok: true, data, body };
  } catch (err) {
    if (err instanceof ParseError) return { ok: false, error: err.message };
    throw err;
  }
}

function parseRoot(lines) {
  const cursor = { lines, i: 0 };
  // Peek, never consume: parseMapping below needs to see this line too.
  const first = peekSignificant(cursor);
  if (first == null) throw new ParseError("frontmatter must be a YAML mapping");

  if (indentOf(first.text, first.number) !== 0) {
    throw new ParseError("unexpected indent on the first key", first.number);
  }
  if (isSequenceItem(first.text.trimStart())) {
    throw new ParseError("frontmatter must be a YAML mapping, not a sequence");
  }

  const data = parseMapping(cursor, 0, [0]);

  const trailing = nextSignificant(cursor);
  if (trailing != null) {
    throw new ParseError("unexpected content after the mapping", trailing.number);
  }
  return data;
}

// ------------------------------------------------------------------ scanning

/** Peek at the next non-blank, non-comment line without consuming it. */
function peekSignificant(cursor) {
  for (let i = cursor.i; i < cursor.lines.length; i++) {
    const text = cursor.lines[i];
    if (isSkippable(text)) continue;
    return { text, number: i + 1, index: i };
  }
  return null;
}

/** Consume and return the next significant line. */
function nextSignificant(cursor) {
  const line = peekSignificant(cursor);
  if (line == null) {
    cursor.i = cursor.lines.length;
    return null;
  }
  cursor.i = line.index + 1;
  return line;
}

function isSkippable(text) {
  const trimmed = text.trim();
  return trimmed === "" || trimmed.startsWith("#");
}

function isSequenceItem(trimmed) {
  return trimmed === "-" || trimmed.startsWith("- ");
}

function indentOf(text, lineNumber) {
  const leading = text.slice(0, text.length - text.trimStart().length);
  if (leading.includes("\t")) {
    throw new ParseError(
      "tab in indentation — YAML forbids tabs for indentation; use spaces",
      lineNumber,
    );
  }
  return leading.length;
}

// ------------------------------------------------------------------ mappings

function parseMapping(cursor, indent, openIndents) {
  const result = {};
  let seenAny = false;

  for (;;) {
    const line = peekSignificant(cursor);
    if (line == null) break;

    const lineIndent = indentOf(line.text, line.number);
    if (lineIndent < indent) {
      if (!openIndents.includes(lineIndent)) {
        throw new ParseError(
          `indent of ${lineIndent} does not match any open block ` +
            `(open: ${openIndents.join(", ")})`,
          line.number,
        );
      }
      break;
    }
    if (lineIndent > indent) {
      throw new ParseError(
        `unexpected indent of ${lineIndent}, expected ${indent}`,
        line.number,
      );
    }

    cursor.i = line.index + 1;
    seenAny = true;

    const trimmed = line.text.trim();
    if (isSequenceItem(trimmed)) {
      throw new ParseError("sequence item where a 'key: value' pair was expected", line.number);
    }

    const { key, rest } = splitKey(trimmed, line.number);
    if (Object.prototype.hasOwnProperty.call(result, key)) {
      throw new ParseError(`duplicate key '${key}'`, line.number);
    }
    result[key] = parseValue(cursor, rest, indent, openIndents, line.number);
  }

  if (!seenAny) throw new ParseError("expected a mapping but found nothing", indent);
  return result;
}

/** Split "key: rest" — the colon must be followed by a space or end the line. */
function splitKey(trimmed, lineNumber) {
  let key = null;
  let rest = "";

  for (let i = 0; i < trimmed.length; i++) {
    if (trimmed[i] !== ":") continue;
    const next = trimmed[i + 1];
    if (next === undefined) {
      key = trimmed.slice(0, i);
      rest = "";
      break;
    }
    if (next === " " || next === "\t") {
      key = trimmed.slice(0, i);
      rest = trimmed.slice(i + 1).trim();
      break;
    }
  }

  if (key == null) {
    throw new ParseError(`not a 'key: value' pair: ${JSON.stringify(trimmed)}`, lineNumber);
  }
  key = key.trim();
  if (key === "") throw new ParseError("empty key", lineNumber);
  if (key.startsWith('"') || key.startsWith("'")) {
    throw new ParseError("quoted keys are not supported", lineNumber);
  }
  return { key, rest };
}

// -------------------------------------------------------------------- values

function parseValue(cursor, rest, indent, openIndents, lineNumber) {
  const blockHeader = /^([|>])([-+]?)[ \t]*(#.*)?$/.exec(rest);
  if (blockHeader) {
    return readBlockScalar(cursor, indent, blockHeader[1], blockHeader[2]);
  }

  if (rest === "") {
    return readNestedBlock(cursor, indent, openIndents, lineNumber);
  }

  return parseScalar(rest, lineNumber);
}

function readNestedBlock(cursor, indent, openIndents, lineNumber) {
  const next = peekSignificant(cursor);
  if (next == null) {
    throw new ParseError("key has no value and no nested block", lineNumber);
  }
  const childIndent = indentOf(next.text, next.number);
  if (childIndent <= indent) {
    throw new ParseError("key has no value and no nested block", lineNumber);
  }

  const nested = [...openIndents, childIndent];
  return isSequenceItem(next.text.trim())
    ? parseSequence(cursor, childIndent, nested)
    : parseMapping(cursor, childIndent, nested);
}

function parseSequence(cursor, indent, openIndents) {
  const items = [];

  for (;;) {
    const line = peekSignificant(cursor);
    if (line == null) break;

    const lineIndent = indentOf(line.text, line.number);
    if (lineIndent < indent) {
      if (!openIndents.includes(lineIndent)) {
        throw new ParseError(
          `indent of ${lineIndent} does not match any open block ` +
            `(open: ${openIndents.join(", ")})`,
          line.number,
        );
      }
      break;
    }
    if (lineIndent > indent) {
      throw new ParseError(
        `unexpected indent of ${lineIndent}, expected ${indent}`,
        line.number,
      );
    }

    const trimmed = line.text.trim();
    if (!isSequenceItem(trimmed)) break;

    cursor.i = line.index + 1;
    if (trimmed === "-") {
      throw new ParseError("empty sequence item is not supported", line.number);
    }
    items.push(parseScalar(trimmed.slice(2).trim(), line.number));
  }

  return items;
}

function readBlockScalar(cursor, parentIndent, style, chomp) {
  const raw = [];
  let contentIndent = null;

  while (cursor.i < cursor.lines.length) {
    const text = cursor.lines[cursor.i];
    if (text.trim() === "") {
      raw.push("");
      cursor.i++;
      continue;
    }
    const lineIndent = indentOf(text, cursor.i + 1);
    if (lineIndent <= parentIndent) break;
    if (contentIndent == null) contentIndent = lineIndent;
    if (lineIndent < contentIndent) break;
    raw.push(text.slice(contentIndent));
    cursor.i++;
  }

  // Blank lines trailing the block belong to the document, not the scalar,
  // except as chomping fodder — drop them and let `chomp` decide.
  while (raw.length > 0 && raw[raw.length - 1] === "") raw.pop();
  if (raw.length === 0) return chomp === "+" ? "\n" : "";

  const folded = style === "|" ? raw.join("\n") : foldLines(raw);
  if (chomp === "-") return folded;
  if (chomp === "+") return `${folded}\n`;
  return `${folded}\n`;
}

/**
 * YAML folded-scalar semantics: a single line break between two non-empty
 * lines becomes a space; blank lines survive as newlines. A line that is
 * more-indented than its neighbours would keep its break in real YAML, but
 * this reader rejects that shape earlier rather than modelling it.
 */
function foldLines(lines) {
  let out = "";
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (i === 0) {
      out = line;
      continue;
    }
    if (line === "") {
      out += "\n";
      continue;
    }
    out += lines[i - 1] === "" ? line : ` ${line}`;
  }
  return out;
}

function parseScalar(rest, lineNumber) {
  const first = rest[0];

  if (FLOW_START.has(first)) {
    throw new ParseError(
      "flow syntax ({...} / [...]) is not supported — use an indented block",
      lineNumber,
    );
  }
  if (NODE_PROPERTY_START.has(first)) {
    throw new ParseError(
      "anchor, alias or tag syntax (& * !) is not supported",
      lineNumber,
    );
  }
  if (first === '"' || first === "'") {
    return parseQuoted(rest, first, lineNumber);
  }

  // Strip a trailing comment: in YAML ' #' begins one outside quotes.
  const commentAt = rest.search(/\s#/);
  const value = (commentAt === -1 ? rest : rest.slice(0, commentAt)).trim();

  if (/:(\s|$)/.test(value)) {
    throw new ParseError(
      `unquoted ':' in a plain value — quote it or use a '>' block scalar: ${JSON.stringify(value)}`,
      lineNumber,
    );
  }
  return value;
}

function parseQuoted(rest, quote, lineNumber) {
  let out = "";
  let i = 1;

  for (; i < rest.length; i++) {
    const ch = rest[i];

    if (quote === "'" ) {
      if (ch !== "'") { out += ch; continue; }
      if (rest[i + 1] === "'") { out += "'"; i++; continue; }
      i++;
      break;
    }

    if (ch === "\\") {
      const esc = rest[i + 1];
      if (esc === undefined) {
        throw new ParseError("unterminated escape in double-quoted value", lineNumber);
      }
      out += decodeEscape(esc, lineNumber);
      i++;
      continue;
    }
    if (ch === '"') { i++; break; }
    out += ch;
  }

  const closed = rest[i - 1] === quote && i > 1;
  if (!closed) {
    throw new ParseError(`unterminated ${quote === '"' ? "double" : "single"}-quoted value`, lineNumber);
  }

  const trailing = rest.slice(i).trim();
  if (trailing !== "" && !trailing.startsWith("#")) {
    throw new ParseError(
      `unexpected content after a quoted value: ${JSON.stringify(trailing)}`,
      lineNumber,
    );
  }
  return out;
}

function decodeEscape(esc, lineNumber) {
  switch (esc) {
    case "n": return "\n";
    case "t": return "\t";
    case "r": return "\r";
    case "0": return "\0";
    case '"': return '"';
    case "\\": return "\\";
    case "/": return "/";
    case " ": return " ";
    default:
      throw new ParseError(`unsupported escape '\\${esc}' in double-quoted value`, lineNumber);
  }
}
