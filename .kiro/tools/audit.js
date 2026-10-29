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
    { name: 'Dead Code', script: 'dead-code.js' },
    { name: 'Duplicate Code', script: 'dupe-code.js' },
    { name: 'UTF-8 Encoding', script: 'utf8-check.js' },
    { name: 'Logger Check', script: 'logger-check.js' },
    { name: 'Unused Classes', script: 'unused-class.js' },
    { name: 'Inline Sort', script: 'inline-sort-check.js' },
    { name: 'Spec Quality', script: 'spec-quality.js' },
    { name: 'Term Consistency', script: 'term-consistency.js' },
    { name: 'Event Matrix', script: 'event-matrix.js' },
    { name: 'User Flow Check', script: 'user-flow-check.js' },
    { name: 'Flatpack Integrity', script: 'flatpack-check.js' },
    { name: 'Code Metrics', script: 'code-metrics.js' },
    { name: 'Mutation Audit', script: 'mutation-audit.js' },
    { name: 'DateTime Check', script: 'datetime-check.js' },
    // Runs a real dotnet build; needs far more than the 60s default.
    { name: 'Server Build', script: 'server-build-check.js', timeout: 300000 },
];

let totalFindings = 0;
const results = [];

for (const tool of tools) {
    const scriptPath = path.join(TOOLS_DIR, tool.script);
    try {
        const output = execSync('node ' + scriptPath, {
            encoding: 'utf8',
            timeout: tool.timeout || 60000,
            stdio: ['pipe', 'pipe', 'pipe']
        });
        results.push({ name: tool.name, status: 'CLEAN', count: 0, output: output.trim() });
    } catch (err) {
        // Merge stderr: some checks (e.g. server-build-check) report entirely on
        // stderr, which previously left `output` empty and scored them 0 findings.
        const stdout = (err.stdout || '').trim();
        const stderr = (err.stderr || '').trim();
        const output = [stdout, stderr].filter(Boolean).join('\n');
        const lines = output.split('\n').filter(l => l.trim());
        // Last line is usually "N findings"
        const lastLine = lines[lines.length - 1] || '';
        const countMatch = lastLine.match(/(\d+)\s+findings/);
        let count = countMatch ? parseInt(countMatch[1]) : lines.length;
        const timedOut = err.killed === true || err.signal != null;

        // A non-zero exit ALWAYS counts as at least one finding. Without this a
        // check could fail, emit nothing parseable, score 0, and the aggregate
        // would still exit 0 -- reporting a clean audit over a broken gate.
        // That is the same false-success failure mode as the old oebackup.ps1,
        // and it hid 4 real StyleCop analyzer crashes in the Server build.
        if (count === 0) {
            count = 1;
        }

        totalFindings += count;
        results.push({
            name: tool.name,
            status: timedOut ? 'ERROR' : 'FINDINGS',
            count,
            output: output || ('check exited ' + (err.status != null ? err.status : '?') + ' with no output'),
        });
    }
}

// Summary
console.log('=== Audit Summary ===\n');
for (const r of results) {
    const icon = r.status === 'CLEAN' ? '\u2713' : '\u2717';
    let label;
    if (r.status === 'CLEAN') {
        label = 'clean';
    } else if (r.status === 'ERROR') {
        label = 'ERROR (check did not complete) - counted as ' + r.count;
    } else {
        label = r.count + ' findings';
    }

    console.log(icon + ' ' + r.name + ': ' + label);
}
console.log('\nTotal: ' + totalFindings + ' findings');

// Always show details on failure. Previously this required --fix, so a failing
// gate could print a bare summary and leave the cause invisible.
if (totalFindings > 0) {
    console.log('\n=== Details ===\n');
    for (const r of results) {
        if (r.status !== 'CLEAN') {
            console.log('--- ' + r.name + ' ---');
            console.log(r.output);
            console.log('');
        }
    }
}

process.exit(totalFindings > 0 ? 1 : 0);