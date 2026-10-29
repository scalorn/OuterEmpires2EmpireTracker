#!/usr/bin/env node
/*
 * Sorts the plain `using <namespace>;` block at the top of C# files to satisfy
 * StyleCop SA1208/SA1210: System directives first, then the rest, each group
 * ordered ordinally by namespace.
 *
 * Only touches the first contiguous run of simple using directives. Alias usings
 * (`using X = Y;`), static usings (`using static X;`), conditional-compilation
 * directives and anything else are left exactly where they are, so a file
 * containing them is skipped rather than rearranged.
 *
 * Usage: node .kiro/tools/sort-usings.js <dir> [--check]
 */

const fs = require('fs');
const path = require('path');

const SIMPLE_USING = /^using\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;\s*$/;

function walk(dir, out) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === 'bin' || entry.name === 'obj' || entry.name === 'node_modules') {
        continue;
      }

      walk(full, out);
    } else if (entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) {
      out.push(full);
    }
  }

  return out;
}

function isSystem(ns) {
  return ns === 'System' || ns.startsWith('System.');
}

function compare(a, b) {
  // StyleCop compares namespace names case-insensitively, so `Newtonsoft.Json`
  // sorts before `NUnit.Framework` even though 'U' < 'e' ordinally. Case is only
  // the tiebreak, to keep the sort total.
  const la = a.toLowerCase();
  const lb = b.toLowerCase();
  if (la !== lb) {
    return la < lb ? -1 : 1;
  }

  if (a === b) {
    return 0;
  }

  return a < b ? -1 : 1;
}

function sortBlock(namespaces) {
  const system = namespaces.filter(isSystem).sort(compare);
  const other = namespaces.filter((n) => !isSystem(n)).sort(compare);
  return system.concat(other);
}

function processFile(file, check) {
  const original = fs.readFileSync(file, 'utf8');
  const crlf = original.includes('\r\n');
  const lines = original.split(/\r?\n/);

  // Find the first simple using directive.
  let start = -1;
  for (let i = 0; i < lines.length; i++) {
    if (SIMPLE_USING.test(lines[i])) {
      start = i;
      break;
    }

    // Bail out once a namespace or type declaration is reached.
    if (/^\s*(namespace|\[|public|internal|sealed|abstract|static|partial)\b/.test(lines[i])) {
      return { file, status: 'no-block' };
    }
  }

  if (start === -1) {
    return { file, status: 'no-block' };
  }

  // Extend over the contiguous run.
  let end = start;
  while (end + 1 < lines.length && SIMPLE_USING.test(lines[end + 1])) {
    end++;
  }

  // Anything unusual immediately after the run means hand inspection, not a
  // mechanical sort.
  const next = lines[end + 1] === undefined ? '' : lines[end + 1].trim();
  if (next.startsWith('using ') || next.startsWith('#if') || next.startsWith('#endif')) {
    return { file, status: 'skipped' };
  }

  const namespaces = lines.slice(start, end + 1).map((l) => l.match(SIMPLE_USING)[1]);
  const sorted = sortBlock(namespaces);

  if (namespaces.join('\n') === sorted.join('\n')) {
    return { file, status: 'ok' };
  }

  if (check) {
    return { file, status: 'would-change' };
  }

  const rebuilt = lines
    .slice(0, start)
    .concat(sorted.map((ns) => `using ${ns};`))
    .concat(lines.slice(end + 1));

  fs.writeFileSync(file, rebuilt.join(crlf ? '\r\n' : '\n'), 'utf8');
  return { file, status: 'changed' };
}

const args = process.argv.slice(2);
const check = args.includes('--check');
const target = args.find((a) => !a.startsWith('--'));

if (!target) {
  console.error('usage: node .kiro/tools/sort-usings.js <dir> [--check]');
  process.exit(2);
}

const results = walk(path.resolve(target), []).map((f) => processFile(f, check));
const counts = {};
for (const r of results) {
  counts[r.status] = (counts[r.status] || 0) + 1;
  if (r.status === 'changed' || r.status === 'would-change' || r.status === 'skipped') {
    console.log(`${r.status}: ${path.relative(process.cwd(), r.file)}`);
  }
}

console.log('');
console.log(Object.entries(counts).map(([k, v]) => `${k}=${v}`).join('  '));
process.exit(check && counts['would-change'] ? 1 : 0);
