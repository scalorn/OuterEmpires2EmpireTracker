#!/usr/bin/env node
/**
 * dupe-code.js — Detect duplicated method bodies across .cs files.
 *
 * Usage:
 *   node .kiro/tools/dupe-code.js
 *
 * Scans all .cs files in OE2EmpireTracker/ (excluding Designer.cs, obj/, bin/).
 * Extracts method bodies, normalizes whitespace, and finds methods with identical
 * normalized bodies that appear in different classes.
 *
 * Only reports methods with bodies >= 5 lines (ignoring trivial one-liners).
 * Inner classes (SlotInfo, SlotDefinition, LocationEntry) that are intentionally
 * copied between forms are excluded via a known-duplicates list.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const SRC_DIR = path.join('OE2EmpireTracker');
const SKIP_DIRS = ['obj', 'bin', '.vs', 'specs'];
const MIN_BODY_LINES = 5;

// Methods that are intentionally duplicated across forms (copied by design)
// Format: "MethodName" — these are excluded from findings
const KNOWN_DUPES = new Set([
    // Hull combo methods intentionally copied between FormShipTemplate and FormShipInstance (BL-081)
    'PopulateHullCombo',
    'SelectHullInCombo',
]);

function findCsFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.includes(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findCsFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) {
            results.push(fullPath);
        }
    }
    return results;
}

// Extract methods with their normalized bodies
function extractMethods(filePath, content) {
    const methods = [];
    const className = path.basename(filePath, '.cs');
    // Match method signatures
    const regex = /(?:private|public|protected|internal)\s+(?:(?:static|async|override|virtual|new|sealed)\s+)*[\w<>\[\],\s]+?\s+(\w+)\s*\([^)]*\)\s*\{/g;
    let match;

    while ((match = regex.exec(content)) !== null) {
        const name = match[1];
        if (KNOWN_DUPES.has(name)) continue;

        // Find matching closing brace
        const braceStart = content.indexOf('{', match.index + match[0].length - 1);
        if (braceStart === -1) continue;

        let depth = 0;
        let endIdx = braceStart;
        for (let i = braceStart; i < content.length; i++) {
            if (content[i] === '{') depth++;
            else if (content[i] === '}') {
                depth--;
                if (depth === 0) { endIdx = i; break; }
            }
        }

        const body = content.substring(braceStart + 1, endIdx);
        const bodyLines = body.split('\n').filter(l => l.trim().length > 0);
        if (bodyLines.length < MIN_BODY_LINES) continue;

        // Normalize: strip whitespace, remove comments
        const normalized = bodyLines
            .map(l => l.trim())
            .filter(l => !l.startsWith('//'))
            .join('\n');

        const hash = crypto.createHash('md5').update(normalized).digest('hex');
        const line = content.substring(0, match.index).split('\n').length;

        methods.push({
            name,
            className,
            file: filePath,
            line,
            hash,
            bodyLineCount: bodyLines.length
        });
    }
    return methods;
}

// Main
const allFiles = findCsFiles(SRC_DIR);
const allMethods = [];

for (const file of allFiles) {
    const content = fs.readFileSync(file, 'utf8');
    allMethods.push(...extractMethods(file, content));
}

// Group by hash
const byHash = new Map();
for (const m of allMethods) {
    if (!byHash.has(m.hash)) byHash.set(m.hash, []);
    byHash.get(m.hash).push(m);
}

// Find duplicates (same hash, different classes)
const findings = [];
for (const [hash, methods] of byHash) {
    if (methods.length < 2) continue;

    // Check if they're in different classes
    const classes = new Set(methods.map(m => m.className));
    if (classes.size < 2) continue;

    const locations = methods.map(m => {
        const relFile = path.relative('.', m.file).replace(/\\/g, '/');
        return `${m.className}.${m.name} (${relFile}:${m.line})`;
    });
    findings.push(`DUPLICATE (${methods[0].bodyLineCount} lines): ${locations.join(' == ')}`);
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Duplicate Code Audit ===\n');
    findings.sort().forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
