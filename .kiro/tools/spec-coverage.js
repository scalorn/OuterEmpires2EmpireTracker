#!/usr/bin/env node
/**
 * spec-coverage.js — Check that every class has spec documentation.
 *
 * Usage:
 *   node .kiro/tools/spec-coverage.js
 *
 * For each .cs file in the main project, checks if its primary class name
 * appears in any spec/ markdown file.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SOURCE_DIR = 'OE2EmpireTracker';
const SPEC_DIRS = ['spec', '.kiro/specs', 'docs'];
const SKIP_FILES = ['Program.cs', 'AssemblyInfo.cs'];
const EXCLUDE_DIRS = ['obj', 'bin', 'Properties'];

// 1. Get all .cs files in main project
function getSourceFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);

        if (entry.isDirectory()) {
            if (EXCLUDE_DIRS.includes(entry.name)) continue;
            getSourceFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            if (/\.Designer\.cs$/.test(entry.name)) continue;
            if (SKIP_FILES.includes(entry.name)) continue;
            results.push(fullPath);
        }
    }
    return results;
}

// 2. Get all .md files in spec/ recursively
function getSpecFiles(dir, results) {
    results = results || [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            getSpecFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.md')) {
            results.push(fullPath);
        }
    }
    return results;
}

// 3. Load all spec content into one big string for searching
function loadSpecContent(specFiles) {
    let content = '';
    for (const file of specFiles) {
        content += fs.readFileSync(file, 'utf8') + '\n';
    }
    return content;
}

// Main
const sourceFiles = getSourceFiles(SOURCE_DIR);
const specFiles = [];
for (const dir of SPEC_DIRS) {
    getSpecFiles(dir, specFiles);
}
const specContent = loadSpecContent(specFiles);

const findings = [];

for (const file of sourceFiles) {
    const basename = path.basename(file, '.cs');
    // Class name is the filename without extension
    if (!specContent.includes(basename)) {
        const relFile = path.relative('.', file).replace(/\\/g, '/');
        findings.push('NO SPEC: ' + basename + ' (' + relFile + ')');
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Spec Coverage Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
