#!/usr/bin/env node
/**
 * event-matrix.js — Cross-check the form-to-event subscription matrix in
 * spec/requirements/DataChangeEvents.md against actual code subscriptions.
 *
 * Parses the markdown table (REQ-DCE-024) and verifies:
 * 1. Every subscription listed in the matrix exists in the form's .cs file
 * 2. Every playerContext.*Changed subscription in code is listed in the matrix
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SPEC_FILE = path.join('spec', 'requirements', 'DataChangeEvents.md');
const FORMS_DIR = path.join('OE2EmpireTracker', 'Forms');

// Parse the subscription matrix from the markdown file
function parseMatrix(specContent) {
    const lines = specContent.split('\n');
    const matrix = []; // { form, events: string[] }

    let inTable = false;
    for (const line of lines) {
        // Detect table rows (starts with |, has Form column)
        if (line.startsWith('| Form ')) {
            inTable = true;
            continue;
        }
        if (line.startsWith('|---')) continue;
        if (!inTable) continue;
        if (!line.startsWith('|')) { inTable = false; continue; }

        const cells = line.split('|').map(c => c.trim()).filter(c => c);
        if (cells.length < 3) continue;

        const formName = cells[0];
        const domainEventsCell = cells[2];

        const events = [];
        // CurrentPlayerChanged column
        if (cells[1] === 'Yes') {
            events.push('CurrentPlayerChanged');
        }

        // Domain events column - parse comma-separated event names
        if (domainEventsCell && !domainEventsCell.startsWith('(')) {
            const parts = domainEventsCell.split(',').map(s => s.trim());
            for (const p of parts) {
                if (p && !p.startsWith('(')) {
                    events.push(p);
                }
            }
        }

        // Special case: MainWindow subscribes to PlayerProfilesChanged
        if (domainEventsCell.includes('PlayerProfilesChanged')) {
            events.push('PlayerProfilesChanged');
        }

        matrix.push({ form: formName, events });
    }

    return matrix;
}

// Find the .cs file for a form name
function findFormFile(formName) {
    function search(dir) {
        const entries = fs.readdirSync(dir, { withFileTypes: true });
        for (const entry of entries) {
            const fullPath = path.join(dir, entry.name);
            if (entry.isDirectory()) {
                const result = search(fullPath);
                if (result) return result;
            } else if (entry.isFile() && entry.name === formName + '.cs') {
                return fullPath;
            }
        }
        return null;
    }
    return search(FORMS_DIR);
}

// Extract actual event subscriptions from a form file
function extractSubscriptions(content) {
    const subs = [];
    // Match patterns like: playerContext.SomeEvent += OnHandler
    // or: playerContext.SomeChanged += OnSomeChanged
    const regex = /playerContext\.(\w+Changed)\s*\+=/g;
    let match;
    while ((match = regex.exec(content)) !== null) {
        const eventName = match[1];
        if (!subs.includes(eventName)) {
            subs.push(eventName);
        }
    }
    return subs;
}

// Main
if (!fs.existsSync(SPEC_FILE)) {
    console.log('ERROR: ' + SPEC_FILE + ' not found');
    process.exit(1);
}

const specContent = fs.readFileSync(SPEC_FILE, 'utf8');
const matrix = parseMatrix(specContent);

if (matrix.length === 0) {
    console.log('ERROR: Could not parse subscription matrix from ' + SPEC_FILE);
    process.exit(1);
}

const findings = [];

for (const entry of matrix) {
    const formFile = findFormFile(entry.form);
    if (!formFile) {
        // MainWindow is in Forms/ root, not a subdirectory
        if (entry.form === 'MainWindow') {
            const mwPath = path.join(FORMS_DIR, 'MainWindow.cs');
            if (!fs.existsSync(mwPath)) {
                findings.push('MISSING_FILE: ' + entry.form + '.cs not found');
                continue;
            }
            const content = fs.readFileSync(mwPath, 'utf8');
            checkForm(entry, content, mwPath);
        } else {
            findings.push('MISSING_FILE: ' + entry.form + '.cs not found in Forms/');
        }
        continue;
    }

    const content = fs.readFileSync(formFile, 'utf8');
    checkForm(entry, content, formFile);
}

function checkForm(entry, content, filePath) {
    const relPath = path.relative('.', filePath).replace(/\\/g, '/');
    const actualSubs = extractSubscriptions(content);

    // Check: every event in matrix exists in code
    for (const expectedEvent of entry.events) {
        if (!actualSubs.includes(expectedEvent)) {
            findings.push(
                'IN_MATRIX_NOT_CODE: ' + entry.form + ' should subscribe to ' +
                expectedEvent + ' per matrix, but no subscription found (' + relPath + ')'
            );
        }
    }

    // Check: every subscription in code exists in matrix
    for (const actualEvent of actualSubs) {
        if (!entry.events.includes(actualEvent)) {
            findings.push(
                'IN_CODE_NOT_MATRIX: ' + entry.form + ' subscribes to ' +
                actualEvent + ' in code, but not listed in matrix (' + relPath + ')'
            );
        }
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Event Matrix Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
