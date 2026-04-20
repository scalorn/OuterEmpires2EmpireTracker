#!/usr/bin/env node
/**
 * perf-check.js — Scan Form*.cs files for methods missing PERF timing.
 *
 * Usage:
 *   node .kiro/tools/perf-check.js
 *
 * Finds all Form*.cs files in OE2EmpireTracker/Forms/ and checks that
 * Populate*, Refresh*, Rebuild*, PopulateForm*, PopulateList* methods
 * contain Stopwatch and Log.Info("PERF instrumentation.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const FORMS_DIR = path.join('OE2EmpireTracker', 'Forms');
const TRIVIAL_FORMS = [
    'FormAbout', 'FormHelp', 'FormAutoFill', 'FormPreferences',
    'FormListingEdit', 'FormRecordSale', 'FormStructureAllocation'
];
const METHOD_PREFIXES = ['Populate', 'Refresh', 'Rebuild', 'PopulateForm', 'PopulateList'];
const MIN_METHOD_LINES = 5;

// Recursively find all Form*.cs files (excluding *.Designer.cs)
function findFormFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findFormFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.startsWith('Form') && entry.name.endsWith('.cs')) {
            if (/\.Designer\.cs$/.test(entry.name)) continue;
            const formName = entry.name.replace('.cs', '');
            if (TRIVIAL_FORMS.includes(formName)) continue;
            results.push(fullPath);
        }
    }
    return results;
}

// Extract methods and their bodies from a file
function extractMethods(content, lines) {
    const methods = [];
    // Match method signatures like: private void PopulateSomething(...) or public void RefreshData(...)
    const methodRegex = /(?:private|public|protected|internal)\s+(?:(?:static|async|override|virtual)\s+)*\w+\s+(Populate\w*|Refresh\w*|Rebuild\w*)\s*\([^)]*\)/g;
    let match;

    while ((match = methodRegex.exec(content)) !== null) {
        const methodName = match[1];
        const startIdx = match.index;
        const lineNum = content.substring(0, startIdx).split('\n').length;

        // Find the opening brace
        let braceIdx = content.indexOf('{', startIdx);
        if (braceIdx === -1) continue;

        // Count braces to find method end
        let depth = 0;
        let endIdx = braceIdx;
        for (let i = braceIdx; i < content.length; i++) {
            if (content[i] === '{') depth++;
            else if (content[i] === '}') {
                depth--;
                if (depth === 0) { endIdx = i; break; }
            }
        }

        const methodBody = content.substring(braceIdx, endIdx + 1);
        const methodLines = methodBody.split('\n').length;

        methods.push({
            name: methodName,
            line: lineNum,
            body: methodBody,
            lineCount: methodLines
        });
    }
    return methods;
}

// Main
const formFiles = findFormFiles(FORMS_DIR);
const findings = [];

for (const file of formFiles) {
    const content = fs.readFileSync(file, 'utf8');
    const lines = content.split('\n');
    const formName = path.basename(file, '.cs');
    const methods = extractMethods(content, lines);

    for (const method of methods) {
        // Skip trivial methods (less than MIN_METHOD_LINES)
        if (method.lineCount < MIN_METHOD_LINES) continue;

        const hasStopwatch = method.body.includes('Stopwatch');
        const hasPerfLog = method.body.includes('Log.Info("PERF') || method.body.includes("Log.Info(\"PERF");

        if (!hasStopwatch || !hasPerfLog) {
            const relFile = path.relative('.', file).replace(/\\/g, '/');
            findings.push('MISSING PERF: ' + formName + '.' + method.name + ' (' + relFile + ':' + method.line + ')');
        }
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== PERF Timing Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
