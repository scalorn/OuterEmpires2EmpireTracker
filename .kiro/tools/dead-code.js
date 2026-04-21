#!/usr/bin/env node
/**
 * dead-code.js — Detect private methods that are never referenced outside their declaration.
 *
 * Usage:
 *   node .kiro/tools/dead-code.js
 *
 * Scans all .cs files in OE2EmpireTracker/ (excluding Designer.cs, obj/, bin/).
 * For each private method, checks if its name appears anywhere else in the codebase.
 * Event handler wiring (e.g. += MethodName) and reflection-based calls are considered references.
 *
 * Known limitations:
 * - Cannot detect methods called only via reflection with computed names
 * - Does not analyze inheritance chains (virtual/override)
 * - Designer.cs files are excluded from declaration scanning but included as reference sources
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SRC_DIR = path.join('OE2EmpireTracker');
const SKIP_DIRS = ['obj', 'bin', '.vs', 'specs'];

// Known false positives: methods that are wired by the framework or called indirectly
const KNOWN_SAFE = new Set([
    'Dispose',
    'InitializeComponent',
    'Main',
    'BeginProgrammaticUpdate',
    'EndProgrammaticUpdate',
]);

function findCsFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.includes(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findCsFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            results.push(fullPath);
        }
    }
    return results;
}

// Extract private method declarations (non-Designer files only)
function extractPrivateMethods(filePath, content) {
    const methods = [];
    // Match: private [static] [async] ReturnType MethodName(
    const regex = /^\s*private\s+(?:(?:static|async|override|virtual|new)\s+)*\w+(?:<[^>]+>)?\s+(\w+)\s*\(/gm;
    let match;
    while ((match = regex.exec(content)) !== null) {
        const name = match[1];
        if (KNOWN_SAFE.has(name)) continue;
        // Skip property accessors (get/set) and constructors
        if (name === path.basename(filePath, '.cs')) continue;
        const line = content.substring(0, match.index).split('\n').length;
        methods.push({ name, file: filePath, line });
    }
    return methods;
}

// Main
const allFiles = findCsFiles(SRC_DIR);
const nonDesignerFiles = allFiles.filter(f => !f.endsWith('.Designer.cs'));

// Step 1: Extract all private method declarations from non-Designer files
const privateMethods = [];
for (const file of nonDesignerFiles) {
    const content = fs.readFileSync(file, 'utf8');
    privateMethods.push(...extractPrivateMethods(file, content));
}

// Step 2: Build a combined text of all files for reference searching
const allContents = new Map();
for (const file of allFiles) {
    allContents.set(file, fs.readFileSync(file, 'utf8'));
}

// Also include test files as reference sources
const testDir = path.join('OE2EmpireTracker.Tests');
if (fs.existsSync(testDir)) {
    const testFiles = findCsFiles(testDir);
    for (const file of testFiles) {
        allContents.set(file, fs.readFileSync(file, 'utf8'));
    }
}

// Step 3: For each private method, check if its name appears in any other file
// or appears more than once in its own file (beyond the declaration)
const findings = [];

for (const method of privateMethods) {
    const methodName = method.name;
    let refCount = 0;

    for (const [file, content] of allContents) {
        if (file === method.file) {
            // In the declaring file, count occurrences beyond the declaration itself
            // A method name should appear at least twice: declaration + at least one call/reference
            const occurrences = content.split(methodName).length - 1;
            if (occurrences >= 2) { refCount++; break; }
        } else {
            // In other files, any occurrence counts as a reference
            if (content.includes(methodName)) { refCount++; break; }
        }
    }

    if (refCount === 0) {
        const relFile = path.relative('.', method.file).replace(/\\/g, '/');
        findings.push(`DEAD CODE: ${method.name} (${relFile}:${method.line})`);
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Dead Code Audit ===\n');
    findings.sort().forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
