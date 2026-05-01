#!/usr/bin/env node
/**
 * term-consistency.js — Detect terminology drift across spec files.
 *
 * Checks for known synonym pairs where the project should use one canonical term.
 * Reports files that use the non-canonical variant.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SPEC_DIRS = ['spec', 'docs'];

// Each entry: canonical term + non-canonical synonyms
// wordBoundary: true means match only as whole words (prevents substring matches)
// Add pairs as terminology drift is discovered
const TERM_RULES = [
    { canonical: 'colony structure', synonyms: ['colony building'], wordBoundary: true },
    { canonical: 'flatpack', synonyms: ['flat pack', 'flat-pack'], wordBoundary: true },
    { canonical: 'blueprint', synonyms: ['blue print', 'blue-print'], wordBoundary: true },
    { canonical: 'PlayerContext', synonyms: ['player context'], wordBoundary: true },
    { canonical: 'EmpireContext', synonyms: ['empire context'], wordBoundary: true },
    { canonical: 'ItemBag', synonyms: ['item bag'], wordBoundary: true },
    { canonical: 'ProcessCompletionTime', synonyms: ['completion timer'], wordBoundary: true },
    { canonical: 'delivery route', synonyms: ['delivery path', 'shipping route'], wordBoundary: true },
    { canonical: 'delivery plan', synonyms: ['delivery schedule', 'shipping plan'], wordBoundary: true },
    { canonical: 'pricing plan', synonyms: ['price plan', 'pricing scheme'], wordBoundary: true },
    { canonical: 'stock target', synonyms: ['inventory target'], wordBoundary: true },
    { canonical: 'supply chain', synonyms: ['supply route', 'production chain'], wordBoundary: true },
    { canonical: 'write-through', synonyms: ['write through', 'writethrough'], wordBoundary: true },
];

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

function checkFile(filePath, rules) {
    const findings = [];
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    const relPath = path.relative('.', filePath).replace(/\\/g, '/');

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const lineNum = i + 1;

        // Skip code blocks
        if (line.startsWith('```')) {
            i++;
            while (i < lines.length && !lines[i].startsWith('```')) i++;
            continue;
        }

        for (const rule of rules) {
            for (const synonym of rule.synonyms) {
                // Case-insensitive word-boundary check
                const synonymLower = synonym.toLowerCase();
                const escaped = synonymLower.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                const regex = new RegExp('\\b' + escaped + '\\b', 'i');
                if (!regex.test(line)) continue;

                // Find the match position
                const match = line.match(regex);
                if (!match) continue;
                const idx = match.index;

                // Make sure it's not inside a code span (backticks)
                const before = line.substring(0, idx);
                const backtickCount = (before.match(/`/g) || []).length;
                if (backtickCount % 2 === 1) continue; // inside code span

                findings.push(
                    `${relPath}:${lineNum} DRIFT: "${synonym}" should be "${rule.canonical}": "${line.substring(0, 80).trim()}"`
                );
            }
        }
    }

    return findings;
}

// Main
let allFiles = [];
for (const dir of SPEC_DIRS) {
    getSpecFiles(dir, allFiles);
}

if (allFiles.length === 0) {
    console.log('No spec/doc files found');
    process.exit(0);
}

const allFindings = [];
for (const file of allFiles) {
    const findings = checkFile(file, TERM_RULES);
    allFindings.push(...findings);
}

if (allFindings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Terminology Consistency Audit ===\n');
    allFindings.forEach(f => console.log(f));
    console.log('\n' + allFindings.length + ' findings');
    process.exit(1);
}
