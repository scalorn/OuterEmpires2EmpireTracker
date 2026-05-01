#!/usr/bin/env node
/**
 * spec-quality.js — Check that requirements files meet quality standards.
 *
 * Checks:
 * 1. Every requirement has a REQ-xxx ID
 * 2. Requirements use SHALL/MUST (testable language), not vague adjectives
 * 3. No orphan requirements (text that looks like a requirement but has no ID)
 * 4. No TODO/TBD/FIXME markers left in requirements
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const REQ_DIR = path.join('spec', 'requirements');

// Vague adjectives that should have measurable criteria
const VAGUE_WORDS = [
    'intuitive', 'robust', 'scalable', 'fast', 'efficient',
    'user-friendly', 'seamless', 'elegant', 'simple', 'easy',
    'responsive', 'performant', 'reliable', 'secure', 'flexible'
];

// Markers that indicate unfinished work
const TODO_MARKERS = ['TODO', 'TBD', 'FIXME', 'HACK', 'XXX', 'PLACEHOLDER'];

function getReqFiles() {
    if (!fs.existsSync(REQ_DIR)) return [];
    return fs.readdirSync(REQ_DIR)
        .filter(f => f.endsWith('.md') && f !== 'README.md')
        .map(f => path.join(REQ_DIR, f));
}

function checkFile(filePath) {
    const findings = [];
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    const relPath = path.relative('.', filePath).replace(/\\/g, '/');

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const lineNum = i + 1;

        // Skip headings, blank lines, code blocks, mermaid blocks
        if (line.startsWith('#') || line.trim() === '') continue;
        if (line.startsWith('```')) {
            // Skip until closing ```
            i++;
            while (i < lines.length && !lines[i].startsWith('```')) i++;
            continue;
        }

        // Check for requirement-like lines without IDs
        // A requirement-like line starts with ** or contains SHALL/MUST but has no REQ- ID
        const hasShall = /\bSHALL\b|\bMUST\b/.test(line);
        const hasReqId = /\bREQ-[A-Z]+-\d+/.test(line);
        const isBoldStart = line.startsWith('**REQ-');
        const isListItem = line.startsWith('- ') || line.startsWith('  - ');

        // Lines with SHALL/MUST that don't have a REQ ID (and aren't sub-bullets of one)
        if (hasShall && !hasReqId) {
            // Check if previous non-empty line has a REQ ID (sub-requirement)
            let parentHasId = false;
            for (let j = i - 1; j >= 0 && j >= i - 3; j--) {
                if (lines[j].trim() === '') continue;
                if (/\bREQ-[A-Z]+-\d+/.test(lines[j])) {
                    parentHasId = true;
                    break;
                }
                break;
            }
            if (!parentHasId) {
                findings.push(`${relPath}:${lineNum} MISSING_ID: Requirement-like statement without REQ-xxx ID: "${line.substring(0, 80)}..."`);
            }
        }

        // Check for vague adjectives in requirement statements
        if (hasReqId || hasShall) {
            for (const word of VAGUE_WORDS) {
                const regex = new RegExp('\\b' + word + '\\b', 'i');
                if (regex.test(line)) {
                    findings.push(`${relPath}:${lineNum} VAGUE: "${word}" used without measurable criteria: "${line.substring(0, 80)}..."`);
                }
            }
        }

        // Check for TODO markers
        for (const marker of TODO_MARKERS) {
            if (line.includes(marker)) {
                findings.push(`${relPath}:${lineNum} UNFINISHED: ${marker} marker found: "${line.substring(0, 80)}..."`);
            }
        }
    }

    return findings;
}

// Main
const files = getReqFiles();
if (files.length === 0) {
    console.log('No requirements files found in ' + REQ_DIR);
    process.exit(0);
}

const allFindings = [];
for (const file of files) {
    const findings = checkFile(file);
    allFindings.push(...findings);
}

if (allFindings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Spec Quality Audit ===\n');
    allFindings.forEach(f => console.log(f));
    console.log('\n' + allFindings.length + ' findings');
    process.exit(1);
}
