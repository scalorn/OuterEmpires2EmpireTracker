#!/usr/bin/env node
/**
 * user-flow-check.js — Check that every form with a requirements file has
 * at least one user flow diagram (Mermaid sequenceDiagram).
 *
 * Maps form names to their requirements files. If a requirements file exists
 * for a form but contains no sequenceDiagram, it's a finding.
 *
 * Requirements files that are purely infrastructure/data (no form) are exempt.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const REQ_DIR = path.join('spec', 'requirements');

// Requirements files that are infrastructure/data — no form, no user flow needed
const EXEMPT_FILES = [
    'Architecture.md',
    'BlueprintProperties.md',
    'DataChangeEvents.md',
    'DataModel.md',
    'GameConstraints.md',
    'GameMechanics.md',
    'NonFunctional.md',
    'SafeFileWriter.md',
    'UIStatePersistence.md',
    'README.md',
];

function getReqFiles() {
    if (!fs.existsSync(REQ_DIR)) return [];
    return fs.readdirSync(REQ_DIR)
        .filter(f => f.endsWith('.md') && !EXEMPT_FILES.includes(f));
}

// Main
const files = getReqFiles();
const findings = [];

for (const file of files) {
    const filePath = path.join(REQ_DIR, file);
    const content = fs.readFileSync(filePath, 'utf8');
    if (!content.includes('sequenceDiagram')) {
        findings.push('MISSING_FLOW: ' + file + ' has no user flow diagram (sequenceDiagram)');
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== User Flow Check ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
