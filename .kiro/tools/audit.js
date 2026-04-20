#!/usr/bin/env node
/**
 * audit.js  Run all audit tools and report a combined summary.
 *
 * Usage:
 *   node .kiro/tools/audit.js           # run all checks
 *   node .kiro/tools/audit.js --fix     # run all checks and list what needs fixing
 *
 * Runs: magic-strings, spec-coverage, perf-check, refcount-check
 * Exit code 0 = all clean, 1 = findings exist
 */

const { execSync } = require('child_process');
const path = require('path');

const TOOLS_DIR = path.join('.kiro', 'tools');
const tools = [
    { name: 'Magic Strings', script: 'magic-strings.js' },
    { name: 'Spec Coverage', script: 'spec-coverage.js' },
    { name: 'PERF Timing', script: 'perf-check.js' },
    { name: 'Reference Counters', script: 'refcount-check.js' },
    { name: 'Control Wiring', script: 'control-wiring.js' },
    { name: 'Mockup Controls', script: 'mockup-controls.js' },
];

let totalFindings = 0;
const results = [];

for (const tool of tools) {
    const scriptPath = path.join(TOOLS_DIR, tool.script);
    try {
        const output = execSync('node ' + scriptPath, {
            encoding: 'utf8',
            timeout: 60000,
            stdio: ['pipe', 'pipe', 'pipe']
        });
        results.push({ name: tool.name, status: 'CLEAN', count: 0, output: output.trim() });
    } catch (err) {
        const output = (err.stdout || '').trim();
        const lines = output.split('\n').filter(l => l.trim());
        // Last line is usually "N findings"
        const lastLine = lines[lines.length - 1] || '';
        const countMatch = lastLine.match(/(\d+)\s+findings/);
        const count = countMatch ? parseInt(countMatch[1]) : lines.length;
        totalFindings += count;
        results.push({ name: tool.name, status: 'FINDINGS', count, output });
    }
}

// Summary
console.log('=== Audit Summary ===\n');
for (const r of results) {
    const icon = r.status === 'CLEAN' ? '\u2713' : '\u2717';
    console.log(icon + ' ' + r.name + ': ' + (r.status === 'CLEAN' ? 'clean' : r.count + ' findings'));
}
console.log('\nTotal: ' + totalFindings + ' findings');

// Show details if findings exist and --fix flag
if (totalFindings > 0 && process.argv.includes('--fix')) {
    console.log('\n=== Details ===\n');
    for (const r of results) {
        if (r.status === 'FINDINGS') {
            console.log('--- ' + r.name + ' ---');
            console.log(r.output);
            console.log('');
        }
    }
}

process.exit(totalFindings > 0 ? 1 : 0);