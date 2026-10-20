#!/usr/bin/env node
// lint-requirements.mjs <requirements-root>             lint a schema requirements folder; exit 0 clean, 1 lint errors, 2 usage
// lint-requirements.mjs <requirements-root> --rules     print the sorted ids of the rules that reported an error; exit 0
// lint-requirements.mjs <requirements-root> --warnings  print the sorted ids of the rules that reported a warning; exit 0
//
// Checks the mechanical half of the content-model conventions on a written requirements doc, so nobody has to
// check them by eye: page layout, status lines, alias casing, suffix by folder and reserved aliases,
// tab sorts, element and composition shape, templates, the Collection row, folder breadcrumbs,
// dependency types and flags, changeset shape and status, links, leftover placeholders, and (as
// warnings) property sorts not in hundreds, data type names, a page with no SEO composition (unless
// every changeset lists it only as an Update) and values the Umbraco Developer MCP would refuse.
//
// A type at the Document-Types root is a page (it has a template), except siteSettings.
//
// The format it enforces is documented in ../references/requirements-format.md. No dependencies.

import fs from 'node:fs';
import path from 'node:path';

const DASH = '—';
const STATUS = /^> \*\*Status:\*\* (proposed|approved|applied \d{4}-\d{2}-\d{2} (?:via MCP|manually))$/;
// Any <Token>, whatever its casing or spacing (<Alias>, <alias>, <Editor kind>, <yyyy-mm-dd>), outside
// inline code, where angle brackets are legitimate (`IEnumerable<IPublishedContent>`).
const PLACEHOLDER = /<[A-Za-z][A-Za-z0-9 -]*>/g;
const CAMEL = /^[a-z][a-zA-Z0-9]*$/;
// Property aliases that clash with IPublishedContent members; Umbraco refuses them.
const RESERVED_ALIASES = new Set(['id', 'key', 'name', 'url', 'urlsegment', 'path', 'level', 'sortorder', 'templateid', 'contenttype', 'parent', 'children', 'writerid', 'creatorid', 'writername', 'creatorname', 'createdate', 'updatedate', 'cultures', 'itemtype', 'isdraft', 'ispublished', 'properties']);
// The Umbraco Developer MCP's input sanitiser refuses these on every text field.
const MCP_UNSAFE = /[?&]|%[0-9A-Fa-f]{2}|\.\.[\\/]/;
const DEPENDENCY_TYPES = ['data-type', 'document-type', 'template', 'data-type-container', 'document-type-container'];
const ACTIONS = ['Create', 'Update'];
const PROPERTY_HEADER = ['Tab', 'Tab Sort', 'Group', 'Group Sort', 'Name', 'Alias', 'Data Type', 'Editor', 'Value Type', 'Mandatory', 'Sort', 'Description'];
const FLAGS = ['Exists', 'New in this changeset', 'Missing'];

// A tab has the same sort wherever it appears. A tab that is not in this table is specific to one
// type and sorts 0.
const TAB_SORTS = {
  'Content': 100,
  'Page Details': 200,
  'Section Navigation': 300,
  'Tags': 400,
  'Sidebar': 500,
  'SEO': 600,
  'Visibility': 900,
  'Admin': 3000,
};
const SETTINGS_TAB_SORTS = { 'Style': 50, 'Settings': 100 };

const DEFINITION_ROWS = {
  documentType: ['Name', 'Alias', 'Kind', 'Icon', 'Description', 'Folder', 'Allowed at root', 'Vary by culture', 'Default template', 'Allowed templates', 'Allowed children', 'Compositions'],
  elementType: ['Name', 'Alias', 'Kind', 'Icon', 'Description', 'Folder', 'Allowed at root', 'Vary by culture', 'Allowed children', 'Compositions'],
  dataType: ['Name', 'Property editor', 'Editor UI', 'Database type', 'Folder'],
  template: ['Name', 'Alias', 'File', 'Master'],
};
const HEADINGS = {
  documentType: ['Definition', 'Properties', 'Used by', 'Dependencies'],
  elementType: ['Definition', 'Properties', 'Used by', 'Dependencies'],
  dataType: ['Definition', 'Configuration', 'Used by', 'Dependencies'],
  template: ['Definition', 'Used by', 'Dependencies'],
  changeset: ['Summary', 'Requirements pages', 'Apply checklist', 'Apply log'],
};

const args = process.argv.slice(2);
const rulesOnly = args.includes('--rules');
const warningsOnly = args.includes('--warnings');
const rootArg = args.find((a) => !a.startsWith('--'));
if (!rootArg || !fs.existsSync(rootArg) || !fs.statSync(rootArg).isDirectory()) {
  console.error('usage: node lint-requirements.mjs <requirements-root> [--rules]');
  process.exit(2);
}
const root = path.resolve(rootArg);
const findings = [];
const report = (severity, file, line, rule, message) =>
  findings.push({ severity, file: path.relative(root, file).split(path.sep).join('/'), line, rule, message });

// ---------------------------------------------------------------- parsing

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full);
    return entry.name.endsWith('.md') ? [full] : [];
  });
}

const splitRow = (line) =>
  line.trim().replace(/^\|/, '').replace(/\|$/, '').split(/(?<!\\)\|/).map((cell) => cell.trim());
const unquote = (value) => value.replace(/^`|`$/g, '');
const links = (text) => [...text.matchAll(/\[([^\]]*)\]\(([^)\s]+)\)/g)].map((m) => ({ text: m[1], target: m[2] }));
const linkText = (cell) => (links(cell)[0]?.text ?? cell).trim();
const isDash = (value) => value === DASH;
const pascal = (alias) => alias.charAt(0).toUpperCase() + alias.slice(1);
const slug = (name) => name.replace(/[^A-Za-z0-9]+/g, '-').replace(/^-|-$/g, '');

function parse(file) {
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
  const sections = {};
  let current = null;
  lines.forEach((text, index) => {
    const heading = text.match(/^## (.+)$/);
    if (heading) {
      current = heading[1].trim();
      sections[current] = { line: index + 1, lines: [] };
    } else if (current) {
      sections[current].lines.push({ text, line: index + 1 });
    }
  });
  return { file, lines, sections };
}

// The first table in a section: { header, rows: [{ cells, line }] }, or null.
function table(section) {
  if (!section) return null;
  const rows = section.lines.filter((l) => l.text.trim().startsWith('|'));
  if (rows.length < 2) return null;
  return {
    header: splitRow(rows[0].text),
    line: rows[0].line,
    rows: rows.slice(2).map((r) => ({ cells: splitRow(r.text), line: r.line })),
  };
}

function definition(page) {
  const t = table(page.sections['Definition']);
  const map = new Map();
  if (t) t.rows.forEach((r) => map.set(r.cells[0], { value: r.cells[1] ?? '', line: r.line }));
  return { table: t, map, get: (key) => map.get(key)?.value, line: (key) => map.get(key)?.line ?? page.sections['Definition']?.line ?? 1 };
}

function statusOf(page) {
  const line = page.lines.find((l) => l.startsWith('> **Status:**'));
  const match = line?.match(STATUS);
  return match ? match[1] : null;
}

// ---------------------------------------------------------------- classification

function classify(file, page) {
  const rel = path.relative(root, file).split(path.sep);
  if (rel.length === 1 && rel[0] === 'README.md') return { kind: 'readme' };
  if (rel[0] === '_changesets') return { kind: 'changeset' };
  if (page.lines.some((l) => l.startsWith('> Folder index'))) return { kind: 'index' };
  const folders = rel.slice(0, -1);
  if (folders[0] === 'Data-Types') return { kind: 'dataType' };
  if (folders[0] === 'Templates') return { kind: 'template' };
  if (folders[0] === 'Document-Types') {
    const has = (name) => folders.includes(name);
    if (has('Elements') && has('Compositions')) return { kind: 'elementType', role: 'settingsComposition' };
    if (has('Elements') && has('Settings')) return { kind: 'elementType', role: 'settingsModel' };
    if (has('Elements')) return { kind: 'elementType', role: 'element' };
    if (has('Compositions')) return { kind: 'documentType', role: 'composition' };
    if (has('Data')) return { kind: 'documentType', role: 'data' };
    return { kind: 'documentType', role: 'page' };
  }
  return { kind: 'unknown' };
}

// ---------------------------------------------------------------- checks

function checkLinks(page) {
  let fenced = false;
  page.lines.forEach((text, index) => {
    if (text.trim().startsWith('```')) fenced = !fenced;
    if (fenced) return;
    for (const { target } of links(text)) {
      if (/^(https?:|mailto:|#)/.test(target)) continue;
      const resolved = path.resolve(path.dirname(page.file), decodeURIComponent(target.split('#')[0]));
      if (!fs.existsSync(resolved)) report('error', page.file, index + 1, 'link-resolves', `link target does not exist: ${target}`);
    }
  });
}

const stripInlineCode = (text) => text.replace(/`[^`]*`/g, (code) => ' '.repeat(code.length));

function checkPlaceholders(page) {
  let fenced = false;
  page.lines.forEach((text, index) => {
    if (text.trim().startsWith('```')) fenced = !fenced;
    if (fenced) return;
    const found = stripInlineCode(text).match(PLACEHOLDER);
    if (found) report('error', page.file, index + 1, 'no-placeholders', `leftover placeholder ${[...new Set(found)].join(', ')}`);
  });
}

function checkLayout(page, kind) {
  if (!/^# \S/.test(page.lines[0] ?? '')) report('error', page.file, 1, 'title', 'the first line must be a "# Name" heading');
  const statusLine = page.lines.findIndex((l) => l.startsWith('> **Status:**'));
  if (statusLine < 0) report('error', page.file, 1, 'status-line', 'missing the "> **Status:** ..." line under the title');
  else if (!STATUS.test(page.lines[statusLine]))
    report('error', page.file, statusLine + 1, 'status-line', 'status must be proposed, approved, "applied <yyyy-mm-dd> via MCP" or "applied <yyyy-mm-dd> manually"');
  else if (statusLine > 2) report('error', page.file, statusLine + 1, 'status-line', 'the status line sits directly under the title');
  if (page.lines.filter((l) => l.startsWith('> **Status:**')).length > 1) report('error', page.file, statusLine + 1, 'status-line', 'a page has one status line');
  for (const heading of HEADINGS[kind]) {
    if (!page.sections[heading]) report('error', page.file, 1, 'required-headings', `missing the "## ${heading}" section`);
  }
}

function checkDefinition(page, kind, def) {
  if (!page.sections['Definition']) return;
  if (!def.table || def.table.header.join('|') !== 'Setting|Value') {
    report('error', page.file, page.sections['Definition'].line, 'definition-rows', 'Definition must be a "| Setting | Value |" table');
    return;
  }
  for (const row of DEFINITION_ROWS[kind]) {
    if (!def.map.has(row)) report('error', page.file, def.table.line, 'definition-rows', `Definition is missing the "${row}" row`);
  }
  const title = (page.lines[0] ?? '').replace(/^# /, '').trim();
  if (def.get('Name') && def.get('Name') !== title) report('error', page.file, def.line('Name'), 'definition-rows', `Name "${def.get('Name')}" does not match the title "${title}"`);
  for (const [key, value] of def.map) {
    if (value.value === '') report('error', page.file, value.line, 'definition-rows', `"${key}" is blank; write — for a value that does not apply`);
  }
  const culture = def.get('Vary by culture');
  if (culture !== undefined && !['Yes', 'No'].includes(culture)) report('error', page.file, def.line('Vary by culture'), 'definition-rows', 'Vary by culture must be Yes or No');
  for (const key of ['Name', 'Description']) {
    const value = def.get(key);
    if (value && MCP_UNSAFE.test(stripInlineCode(value))) report('warning', page.file, def.line(key), 'mcp-unsafe-value', `${key} contains ?, &, a percent-encoded sequence or ../, which the Umbraco Developer MCP refuses; it will need a manual step`);
  }
  checkFolderBreadcrumb(page, def);
}

// The Folder row is a breadcrumb of links to folder index pages (the tree root's index first).
function checkFolderBreadcrumb(page, def) {
  const folder = def.get('Folder');
  if (folder === undefined || isDash(folder)) return;
  const line = def.line('Folder');
  const found = links(folder);
  if (found.length === 0) {
    report('error', page.file, line, 'folder-breadcrumb', 'Folder is a breadcrumb of links to the folder index pages, not plain text');
    return;
  }
  for (const { target } of found) {
    const resolved = path.resolve(path.dirname(page.file), decodeURIComponent(target.split('#')[0]));
    if (!fs.existsSync(resolved)) continue; // reported by link-resolves
    const isIndex = fs.readFileSync(resolved, 'utf8').split(/\r?\n/).some((l) => l.startsWith('> Folder index'));
    if (!isIndex) report('error', page.file, line, 'folder-breadcrumb', `${target} is not a folder index page (it has no "> Folder index" line)`);
  }
}

function checkAlias(page, alias, line, what) {
  if (!CAMEL.test(alias)) report('error', page.file, line, 'alias-casing', `${what} alias "${alias}" must be camelCase`);
  else if (/[A-Z]{2}/.test(alias)) report('error', page.file, line, 'alias-casing', `${what} alias "${alias}" has an acronym in capitals; collapse it (seoComposition, xmlSitemapPage)`);
}

function checkDependencies(page, status) {
  const t = table(page.sections['Dependencies']);
  const names = new Set();
  if (!t) return names;
  if (t.header.join('|') !== 'Artifact|Type|Flags') {
    report('error', page.file, t.line, 'dependency-flags', 'Dependencies must be an "| Artifact | Type | Flags |" table');
    return names;
  }
  for (const row of t.rows) {
    const [artifact, type, flag] = row.cells;
    names.add(linkText(artifact));
    if (!DEPENDENCY_TYPES.includes(unquote(type ?? ''))) report('error', page.file, row.line, 'dependency-type', `type "${type}" must be one of: ${DEPENDENCY_TYPES.join(', ')}`);
    if (!FLAGS.includes(flag)) {
      report('error', page.file, row.line, 'dependency-flags', `flag "${flag}" must be one of: ${FLAGS.join(', ')}`);
    } else if (flag === 'Missing') {
      const blocking = status && status !== 'proposed';
      report(blocking ? 'error' : 'warning', page.file, row.line, 'dependency-missing', `dependency "${linkText(artifact)}" is Missing; it must exist or join the changeset before this requirements doc is approved`);
    } else if (flag === 'New in this changeset' && links(artifact).length === 0) {
      report('error', page.file, row.line, 'dependency-flags', `"${artifact}" is new in this changeset, so it must link to its requirements page`);
    }
  }
  return names;
}

function checkProperties(page, role, alias, dependencyNames, variesByCulture) {
  const section = page.sections['Properties'];
  if (!section) return;
  const t = table(section);
  const noOwn = section.lines.some((l) => l.text.startsWith('No own properties'));
  const invariant = section.lines.find((l) => l.text.startsWith('Invariant:'));
  if (invariant && variesByCulture !== 'Yes') report('error', page.file, invariant.line, 'property-rows', 'an "Invariant:" line belongs only on a type whose Vary by culture is Yes');
  if (role === 'settingsModel') {
    if (t) report('error', page.file, t.line, 'settings-model-shape', 'a settings model has no properties of its own; it is built only from settings compositions');
    return;
  }
  if (!t) {
    if (!noOwn) report('error', page.file, section.line, 'properties-header', 'Properties must be the 12-column table or the line "No own properties ..."');
    return;
  }
  if (t.header.join('|') !== PROPERTY_HEADER.join('|')) {
    report('error', page.file, t.line, 'properties-header', `Properties header must be: ${PROPERTY_HEADER.join(' | ')}`);
    return;
  }
  const seen = new Set();
  const tabSorts = new Map();
  for (const row of t.rows) {
    if (row.cells.length !== 12) {
      report('error', page.file, row.line, 'properties-header', `a property row must have 12 cells, found ${row.cells.length}`);
      continue;
    }
    const [tab, tabSort, group, groupSort, , aliasCell, dataType, , , mandatory, sort] = row.cells;
    const propertyAlias = unquote(aliasCell);
    checkAlias(page, propertyAlias, row.line, 'property');
    if (RESERVED_ALIASES.has(propertyAlias.toLowerCase())) report('error', page.file, row.line, 'alias-reserved', `"${propertyAlias}" is a member of IPublishedContent, so Umbraco refuses it as a property alias; prefix it with its area (pageName, publishDate)`);
    if (seen.has(propertyAlias)) report('error', page.file, row.line, 'property-rows', `duplicate property alias "${propertyAlias}"`);
    seen.add(propertyAlias);
    for (const [label, cell] of [['tab', tab], ['group', group], ['name', row.cells[4]], ['description', row.cells[11]]]) {
      if (MCP_UNSAFE.test(stripInlineCode(cell))) report('warning', page.file, row.line, 'mcp-unsafe-value', `the property ${label} contains ?, &, a percent-encoded sequence or ../, which the Umbraco Developer MCP refuses; it will need a manual step`);
    }
    if (!['Yes', 'No'].includes(mandatory)) report('error', page.file, row.line, 'property-rows', `Mandatory must be Yes or No, found "${mandatory}"`);
    if (!/^\d+$/.test(sort)) report('error', page.file, row.line, 'property-rows', `Sort must be a whole number, found "${sort}"`);
    else if (Number(sort) % 100 !== 0) report('warning', page.file, row.line, 'sort-hundreds', `property sort ${sort} is not a multiple of 100`);
    if (!dependencyNames.has(linkText(dataType))) report('error', page.file, row.line, 'property-rows', `data type "${linkText(dataType)}" is not listed in Dependencies`);

    if (role === 'element') {
      if (!isDash(tab) || !isDash(tabSort)) report('error', page.file, row.line, 'element-shape', 'an element type has no tab; Tab and Tab Sort must be —');
      if (group !== 'Content' || groupSort !== '0') report('error', page.file, row.line, 'element-shape', 'an element type has exactly one group, "Content", with sort 0');
      continue;
    }
    if (isDash(tab) || !/^\d+$/.test(tabSort)) {
      report('error', page.file, row.line, 'tab-sorts', 'every property on a document type or composition is in a tab with a numeric sort');
      continue;
    }
    if (isDash(group) !== isDash(groupSort) || (!isDash(groupSort) && !/^\d+$/.test(groupSort)))
      report('error', page.file, row.line, 'property-rows', 'Group and Group Sort must both be — or a group name with a numeric sort');
    if (tabSorts.has(tab) && tabSorts.get(tab) !== tabSort) report('error', page.file, row.line, 'tab-sorts', `tab "${tab}" has two different sorts on this page`);
    tabSorts.set(tab, tabSort);

    const sortNumber = Number(tabSort);
    if (role === 'settingsComposition') {
      if (SETTINGS_TAB_SORTS[tab] !== sortNumber) report('error', page.file, row.line, 'tab-sorts', 'a settings composition uses the tab "Style" (50) or "Settings" (100)');
    } else if (alias === 'siteSettings') {
      if (sortNumber > 9) report('error', page.file, row.line, 'tab-sorts', `site settings tabs sort 0 to 9, found ${tabSort}`);
    } else if (tab in TAB_SORTS) {
      if (TAB_SORTS[tab] !== sortNumber) report('error', page.file, row.line, 'tab-sorts', `tab "${tab}" sorts ${TAB_SORTS[tab]} everywhere, found ${tabSort}`);
    } else if (sortNumber !== 0) {
      report('error', page.file, row.line, 'tab-sorts', `tab "${tab}" is not in the global table, so it is type-specific and sorts 0, found ${tabSort}`);
    }
  }
  if (invariant) {
    for (const code of invariant.text.matchAll(/`([^`]+)`/g)) {
      if (!seen.has(code[1])) report('error', page.file, invariant.line, 'property-rows', `"${code[1]}" in the Invariant line is not a property of this type`);
    }
  }
}

const SUFFIX_RULES = {
  page: (alias) => (/Page$/.test(alias) || alias === 'siteSettings' ? null : 'a type at the tree root is a page, and a page alias ends in "Page" (the only exception at the tree root is siteSettings)'),
  composition: (alias) => (/Composition$/.test(alias) ? null : 'an alias in Compositions/ ends in "Composition"'),
  data: (alias) => (/(Page|Composition)$/.test(alias) ? 'a type in Data/ is not a page and not a composition; use "...Folder", "...Item" or a plain noun' : null),
  element: (alias) => (/(Page|Composition|Settings|Folder)$/.test(alias) ? 'a block element alias is a plain noun, or ends in "Item", "Row" or "Block"' : null),
  settingsComposition: (alias) => (/SettingsComposition$/.test(alias) ? null : 'an alias in Elements/Compositions/ ends in "SettingsComposition"'),
  settingsModel: (alias) => (/Settings$/.test(alias) ? null : 'an alias in Elements/Settings/ ends in "Settings"'),
};

function checkContentType(page, kind, role, status) {
  const def = definition(page);
  checkDefinition(page, kind, def);
  const alias = unquote(def.get('Alias') ?? '');
  if (alias) {
    checkAlias(page, alias, def.line('Alias'), 'type');
    const problem = SUFFIX_RULES[role](alias);
    if (problem) report('error', page.file, def.line('Alias'), 'alias-suffix', `"${alias}": ${problem}`);
    if (path.basename(page.file) !== `${pascal(alias)}.md`) report('error', page.file, 1, 'file-name', `the file for alias "${alias}" is named ${pascal(alias)}.md`);
  }
  const expectedKind = kind === 'elementType' ? 'Element Type' : 'Document Type';
  if (def.get('Kind') && def.get('Kind') !== expectedKind) report('error', page.file, def.line('Kind'), 'definition-rows', `Kind must be "${expectedKind}" in this folder`);

  const value = (key) => def.get(key);
  const mustBeDash = (key, rule, why) => {
    if (value(key) !== undefined && !isDash(value(key))) report('error', page.file, def.line(key), rule, `${key} must be — : ${why}`);
  };
  const atRoot = value('Allowed at root');
  if (atRoot !== undefined && !['Yes', 'No'].includes(atRoot)) report('error', page.file, def.line('Allowed at root'), 'definition-rows', 'Allowed at root must be Yes or No');

  if (role === 'composition' || role === 'settingsComposition') {
    const why = 'a composition carries fields only';
    ['Default template', 'Allowed templates', 'Allowed children', 'Compositions'].forEach((key) => mustBeDash(key, 'composition-shape', why));
    if (atRoot === 'Yes') report('error', page.file, def.line('Allowed at root'), 'composition-shape', 'a composition is never allowed at root');
  }
  if (kind === 'elementType') {
    if (atRoot === 'Yes') report('error', page.file, def.line('Allowed at root'), 'element-shape', 'an element type is never allowed at root');
    ['Default template', 'Allowed templates', 'Allowed children'].forEach((key) => mustBeDash(key, 'element-shape', 'an element type is not a node in the content tree'));
    if (role === 'settingsModel' && isDash(value('Compositions') ?? DASH)) report('error', page.file, def.line('Compositions'), 'settings-model-shape', 'a settings model is built from at least one settings composition');
  }
  if (role === 'data' || alias === 'siteSettings') {
    ['Default template', 'Allowed templates'].forEach((key) => mustBeDash(key, 'template-named-after-alias', 'only pages get a template'));
  }
  if (role === 'page' && alias && alias !== 'siteSettings') {
    checkPageTemplate(page, def, alias);
    // Every page takes the SEO composition. A warning, not an error: a page is usable without it, and the
    // skills add the composition in the same changeset when the site has none. A page every changeset
    // lists only as an Update is an existing page the change touches for another reason (a parent
    // gaining a child), so its missing SEO is not this change's to fix and is not reported.
    const compositions = value('Compositions');
    if (compositions !== undefined && !/seo/i.test(compositions) && !UPDATE_ONLY.has(page.file))
      report('warning', page.file, def.line('Compositions'), 'page-seo', `"${alias}" is a page and every page takes the SEO composition; add it to Compositions, in this changeset if the site has none`);
  }
  if (atRoot === 'Yes' && alias && !(/HomePage$|^homePage$/.test(alias) || alias === 'siteSettings' || /Folder$/.test(alias)))
    report('error', page.file, def.line('Allowed at root'), 'allowed-at-root', `"${alias}" is allowed at root; only the home page, siteSettings and data folders are`);
  if (role === 'page' && /ListingPage$/.test(alias) && isDash(value('Collection') ?? DASH))
    report('error', page.file, def.line('Alias'), 'listing-collection-view', 'a listing page has a "Collection" row naming its own "<Type> Collection View" data type');
  if (value('Collection') !== undefined && !(role === 'page' && /ListingPage$/.test(alias)))
    report('error', page.file, def.line('Collection'), 'collection-row', 'only a listing page has a Collection row; delete it everywhere else');

  const dependencyNames = checkDependencies(page, status);
  checkProperties(page, role, alias, dependencyNames, value('Vary by culture'));
}

function checkPageTemplate(page, def, alias) {
  const cell = def.get('Default template');
  if (cell === undefined) return;
  const line = def.line('Default template');
  if (isDash(cell)) {
    report('error', page.file, line, 'template-named-after-alias', 'a page has a default template; a type with no template is not a page and does not sit at the tree root');
    return;
  }
  if (linkText(cell) !== def.get('Name')) report('error', page.file, line, 'template-named-after-alias', `the default template is named after the type: expected "${def.get('Name')}", found "${linkText(cell)}"`);
  if (!(def.get('Allowed templates') ?? '').includes(linkText(cell))) report('error', page.file, def.line('Allowed templates'), 'template-named-after-alias', 'Allowed templates must include the default template');
  const link = links(cell)[0];
  if (!link) return;
  const target = path.resolve(path.dirname(page.file), decodeURIComponent(link.target.split('#')[0]));
  if (!fs.existsSync(target)) return; // reported by link-resolves
  const templateAlias = unquote(definition(parse(target)).get('Alias') ?? '');
  if (templateAlias !== alias) report('error', page.file, line, 'template-named-after-alias', `the default template alias is "${templateAlias}"; it must equal the type alias "${alias}"`);
}

function checkDataType(page, status) {
  const def = definition(page);
  checkDefinition(page, 'dataType', def);
  const name = def.get('Name');
  if (name && path.basename(page.file) !== `${slug(name)}.md`) report('error', page.file, 1, 'file-name', `the file for data type "${name}" is named ${slug(name)}.md`);
  if (name && /^(Copy of|Custom|New|Test|My)\b|\s\d+$|\b(copy|test)\b/i.test(name))
    report('warning', page.file, def.line('Name'), 'data-type-name', `"${name}" does not follow a data type naming pattern (<Editor> (<qualifier>), <Subject> <Editor kind>, the plural of a Block List's child, <Placement> Block Grid, <Type> Collection View)`);
  checkDependencies(page, status);
}

function checkTemplate(page, status) {
  const def = definition(page);
  checkDefinition(page, 'template', def);
  const alias = unquote(def.get('Alias') ?? '');
  if (alias) {
    checkAlias(page, alias, def.line('Alias'), 'template');
    if (unquote(def.get('File') ?? '') !== `${alias}.cshtml`) report('error', page.file, def.line('File'), 'template-named-after-alias', `the template file is named after its alias: expected ${alias}.cshtml`);
    if (path.basename(page.file) !== `${pascal(alias)}.md`) report('error', page.file, 1, 'file-name', `the file for template "${alias}" is named ${pascal(alias)}.md`);
  }
  checkDependencies(page, status);
}

function checkChangeset(page, status) {
  const t = table(page.sections['Requirements pages']);
  if (page.sections['Requirements pages'] && (!t || t.header.join('|') !== 'Order|Requirements page|Kind|Action')) {
    report('error', page.file, page.sections['Requirements pages'].line, 'changeset-shape', 'Requirements docs must be an "| Order | Requirements page | Kind | Action |" table');
  } else if (t) {
    for (const row of t.rows) {
      const link = links(row.cells[1] ?? '')[0];
      if (!link) {
        report('error', page.file, row.line, 'changeset-shape', 'every row in Requirements docs links to a requirements page');
        continue;
      }
      const action = row.cells[3] ?? '';
      if (!ACTIONS.includes(action)) report('error', page.file, row.line, 'changeset-shape', `Action "${action}" must be Create or Update`);
      const target = path.resolve(path.dirname(page.file), decodeURIComponent(link.target));
      if (!fs.existsSync(target)) continue; // reported by link-resolves
      const pageStatus = statusOf(parse(target));
      if (status && status !== 'proposed' && pageStatus === 'proposed')
        report('error', page.file, row.line, 'changeset-status', `the changeset is ${status} but "${link.text}" is still proposed`);
      else if (status && status.startsWith('applied') && pageStatus && !pageStatus.startsWith('applied'))
        report('error', page.file, row.line, 'changeset-status', `the changeset is applied but "${link.text}" is ${pageStatus}; a changeset is applied only when every page is`);
    }
  }
  const items = (page.sections['Apply checklist']?.lines ?? []).filter((l) => /^- \[[ x]\] /.test(l.text));
  if (page.sections['Apply checklist'] && items.length === 0) report('error', page.file, page.sections['Apply checklist'].line, 'changeset-shape', 'Apply checklist needs at least one "- [ ]" item');
  if (status && status.startsWith('applied')) {
    items.filter((l) => l.text.startsWith('- [ ]')).forEach((l) => report('error', page.file, l.line, 'changeset-status', 'the changeset is applied but this step is not ticked'));
  }
}

// ---------------------------------------------------------------- run

const files = walk(root);
if (files.length === 0) {
  console.error(`no .md files under ${root}`);
  process.exit(2);
}
// The pages every changeset lists only with the Update action, read before any page is checked.
const UPDATE_ONLY = (() => {
  const actions = new Map();
  for (const file of files) {
    const page = parse(file);
    if (classify(file, page).kind !== 'changeset') continue;
    for (const row of table(page.sections['Requirements pages'])?.rows ?? []) {
      const link = links(row.cells[1] ?? '')[0];
      if (!link) continue;
      const target = path.resolve(path.dirname(file), decodeURIComponent(link.target));
      actions.set(target, [...(actions.get(target) ?? []), row.cells[3] ?? '']);
    }
  }
  return new Set([...actions].filter(([, list]) => list.every((a) => a === 'Update')).map(([target]) => target));
})();
for (const file of files) {
  const page = parse(file);
  const { kind, role } = classify(file, page);
  checkLinks(page);
  checkPlaceholders(page);
  if (kind === 'readme' || kind === 'index') continue;
  if (kind === 'unknown') {
    report('error', file, 1, 'location', 'a requirements page lives under Document-Types/, Data-Types/, Templates/ or _changesets/');
    continue;
  }
  checkLayout(page, kind);
  const status = statusOf(page);
  if (kind === 'documentType' || kind === 'elementType') checkContentType(page, kind, role, status);
  else if (kind === 'dataType') checkDataType(page, status);
  else if (kind === 'template') checkTemplate(page, status);
  else if (kind === 'changeset') checkChangeset(page, status);
}

const errors = findings.filter((f) => f.severity === 'error');
const warnings = findings.filter((f) => f.severity === 'warning');
if (rulesOnly || warningsOnly) {
  const list = rulesOnly ? errors : warnings;
  console.log([...new Set(list.map((f) => f.rule))].sort().join('\n'));
  process.exit(0);
}
for (const f of findings) console.log(`${f.file}:${f.line}: ${f.severity} [${f.rule}] ${f.message}`);
console.log(`${files.length} file(s) checked: ${errors.length} error(s), ${warnings.length} warning(s)`);
process.exit(errors.length > 0 ? 1 : 0);
