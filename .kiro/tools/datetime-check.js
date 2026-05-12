#!/usr/bin/env node
/**
 * datetime-check.js — Detects direct DateTime.UtcNow usage outside SystemClock.cs.
 *
 * All code must use SystemClock.UtcNow instead of DateTime.UtcNow so that tests
 * can freeze time deterministically. The only allowed file is SystemClock.cs itself.
 *
 * Exit code 0 = clean, 1 = findings exist.
 */

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const PATTERN = /DateTime\.UtcNow/g;

// Only SystemClock.cs is allowed to reference DateTime.UtcNow directly
const ALLOWED_FILES = new Set([
    path.join('OE2EmpireTracker.Common', 'Services', 'SystemClock.cs'),
]);

function walk(dir, results = []) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            if (['bin', 'obj', 'node_modules', '.git', '.vs', 'packages'].includes(entry.name)) continue;
            walk(full, results);
        } else if (entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) {
            results.push(full);
        }
    }
    return results;
}

const files = walk(ROOT);
const findings = [];

for (const file of files) {
    const rel = path.relative(ROOT, file).replace(/\\/g, '/');
    const relBackslash = path.relative(ROOT, file);

    // Check if this file is in the allowed list
    if (ALLOWED_FILES.has(relBackslash) || ALLOWED_FILES.has(rel)) continue;

    const content = fs.readFileSync(file, 'utf8');
    const lines = content.split('\n');

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        // Skip comments
        const trimmed = line.trim();
        if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('///')) continue;

        if (PATTERN.test(line)) {
            findings.push(`${rel}:${i + 1}: ${trimmed}`);
        }
        // Reset regex lastIndex
        PATTERN.lastIndex = 0;
    }
}

if (findings.length === 0) {
    process.exit(0);
} else {
    console.log('DateTime.UtcNow used directly (must use SystemClock.UtcNow instead):');
    console.log('');
    findings.forEach(f => console.log('  ' + f));
    console.log('');
    console.log(`${findings.length} findings`);
    process.exit(1);
}
