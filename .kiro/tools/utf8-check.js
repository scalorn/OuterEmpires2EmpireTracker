#!/usr/bin/env node
/**
 * utf8-check.js — Detect files with invalid UTF-8 sequences (mojibake).
 *
 * Usage:
 *   node .kiro/tools/utf8-check.js
 *
 * Scans all .cs files in OE2EmpireTracker/ and OE2EmpireTracker.Tests/,
 * plus all .md files in spec/ and docs/.
 * Reports any file containing bytes that decode to common mojibake patterns
 * (e.g. Ã¢â‚¬â€ instead of an em-dash, Ã¢â‚¬â€œ instead of an en-dash).
 *
 * These patterns occur when UTF-8 bytes are misinterpreted as Windows-1252
 * and then re-encoded as UTF-8, producing multi-byte garbage sequences.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SCAN_DIRS = [
    { dir: 'OE2EmpireTracker', ext: '.cs' },
    { dir: 'OE2EmpireTracker.Tests', ext: '.cs' },
    { dir: 'spec', ext: '.md' },
    { dir: 'docs', ext: '.md' },
];

const SKIP_DIRS = new Set(['obj', 'bin', '.vs', 'node_modules', 'packages']);

// Common mojibake patterns: UTF-8 bytes misread as Windows-1252 then re-encoded
// Each entry: { pattern: regex, description: what it should be }
const MOJIBAKE_PATTERNS = [
    // Em-dash (U+2014): UTF-8 bytes E2 80 94 → Win-1252 reads as Ã¢â‚¬â€
    { pattern: /\u00c3\u00a2\u00e2\u201a\u00ac\u00e2\u20ac/g, desc: 'em-dash mojibake (Ã¢â‚¬â€)' },
    // En-dash (U+2013): UTF-8 bytes E2 80 93 → Win-1252 reads as Ã¢â‚¬â€œ
    { pattern: /\u00c3\u00a2\u00e2\u201a\u00ac\u00e2\u20ac\u0153/g, desc: 'en-dash mojibake' },
    // General: any sequence of 3+ consecutive bytes in the Ã/â/€ range that isn't valid text
    // Simpler approach: scan for raw byte sequences that indicate double-encoding
];

// Broader detection: look for the telltale Ã (C3) followed by common double-encoded sequences.
// When UTF-8 multi-byte chars get double-encoded, they produce sequences like:
//   Ã¢ (C3 A2) â‚¬ (E2 82 AC) â€ (E2 80) ...
// We detect any occurrence of these common garbled character clusters.
const GARBLED_REGEX = /\xC3[\xA0-\xBF]\xC3[\xA0-\xBF]/;

// Even simpler: detect specific known mojibake strings that appear in this codebase
const KNOWN_MOJIBAKE = [
    '\u00C3\u00A2\u00E2\u201A\u00AC\u00E2\u20AC',  // double-encoded em-dash prefix
];

function findFiles(dir, ext, results) {
    results = results || [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.has(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findFiles(fullPath, ext, results);
        } else if (entry.isFile() && entry.name.endsWith(ext)) {
            results.push(fullPath);
        }
    }
    return results;
}

function checkFile(filePath) {
    const buf = fs.readFileSync(filePath);
    const findings = [];

    // Check 1: Invalid UTF-8 sequences (bytes that don't form valid UTF-8)
    // Node reads as UTF-8 by default; replacement char U+FFFD indicates invalid bytes
    const text = buf.toString('utf8');
    if (text.includes('\uFFFD')) {
        const lines = text.split('\n');
        for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes('\uFFFD')) {
                findings.push({ line: i + 1, type: 'invalid UTF-8 byte (U+FFFD replacement)' });
            }
        }
    }

    // Check 2: Double-encoded UTF-8 (mojibake)
    // When UTF-8 text is misread as Latin-1/Win-1252 and re-saved as UTF-8,
    // single characters become 2-6 byte garbled sequences.
    // Detect: Ã (U+00C3) followed by a byte in U+0080..U+00BF range,
    // which is the signature of a double-encoded UTF-8 lead byte.
    const lines = text.split('\n');
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        // Pattern: Ã followed by a character in the \x80-\xBF range (as decoded Latin-1)
        // In double-encoded UTF-8, the original lead byte 0xC0-0xFF becomes Ã (0xC3) + continuation
        // Look for Ã followed by characters like ¢ â ¡ etc.
        const mojibakeMatch = line.match(/\xC3[\x80-\xBF]/);
        if (mojibakeMatch) {
            const col = mojibakeMatch.index + 1;
            const snippet = line.substring(Math.max(0, mojibakeMatch.index - 10), mojibakeMatch.index + 20).trim();
            findings.push({ line: i + 1, col, type: 'double-encoded UTF-8 (mojibake)', snippet });
        }
    }

    return findings;
}

// Main
const allFiles = [];
for (const { dir, ext } of SCAN_DIRS) {
    findFiles(dir, ext, allFiles);
}

const allFindings = [];

for (const file of allFiles) {
    const findings = checkFile(file);
    if (findings.length > 0) {
        const relFile = path.relative('.', file).replace(/\\/g, '/');
        for (const f of findings) {
            const loc = f.col ? `${relFile}:${f.line}:${f.col}` : `${relFile}:${f.line}`;
            const detail = f.snippet ? ` near "${f.snippet}"` : '';
            allFindings.push(`${f.type}: ${loc}${detail}`);
        }
    }
}

if (allFindings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== UTF-8 Encoding Audit ===\n');
    allFindings.forEach(f => console.log(f));
    console.log('\n' + allFindings.length + ' findings');
    process.exit(1);
}
