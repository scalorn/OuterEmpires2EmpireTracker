#!/usr/bin/env node
/**
 * perf-fix.js — Automatically adds PERF timing to methods flagged by perf-check.js.
 *
 * Reads perf-check output, then for each missing method:
 * 1. Finds the method signature line
 * 2. Finds the opening brace (skipping lines that are property declarations)
 * 3. Checks the method doesn't already have Stopwatch
 * 4. Inserts `var sw = Stopwatch.StartNew();` after the opening brace
 * 5. Finds the method's closing brace (brace-depth counting)
 * 6. Inserts `sw.Stop(); Log.Info(...)` before the closing brace
 *
 * Skips methods where the opening brace line contains property declarations
 * (get; set;) which indicates a nested class, not a method body.
 */

const { execSync } = require('child_process');
const fs = require('fs');

let output;
try {
    output = execSync('node .kiro/tools/perf-check.js', { encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
} catch (err) {
    output = err.stdout || '';
}
const lines = output.split('\n').filter(l => l.startsWith('MISSING PERF:'));

// Group by file to avoid re-reading
const byFile = {};
for (const line of lines) {
    const m = line.match(/MISSING PERF: (\w+)\.(\w+) \(([^:]+):(\d+)\)/);
    if (!m) continue;
    const [, className, methodName, filePath, lineStr] = m;
    const lineNum = parseInt(lineStr);
    if (!byFile[filePath]) byFile[filePath] = [];
    byFile[filePath].push({ className, methodName, lineNum });
}

let totalFixed = 0;

// Process files in reverse line order so insertions don't shift earlier line numbers
for (const [filePath, methods] of Object.entries(byFile)) {
    methods.sort((a, b) => b.lineNum - a.lineNum); // reverse order

    let content = fs.readFileSync(filePath, 'utf8').split('\n');

    for (const { methodName, lineNum } of methods) {
        // Find opening brace
        let braceIdx = -1;
        for (let i = lineNum - 1; i < Math.min(lineNum + 10, content.length); i++) {
            if (content[i].includes('{')) {
                // Skip if this looks like a property/class body (has get; set; or public/private property)
                const nextFew = content.slice(i + 1, i + 4).join(' ');
                if (nextFew.match(/\b(get|set)\s*[;{]/) || nextFew.match(/public\s+\w+\s+\w+\s*\{/)) {
                    break; // This is a class/struct, not a method
                }
                braceIdx = i;
                break;
            }
        }
        if (braceIdx < 0) continue;

        // Check if already has Stopwatch (within first 3 lines after brace)
        let hasSw = false;
        for (let k = braceIdx + 1; k < Math.min(braceIdx + 4, content.length); k++) {
            if (content[k].match(/Stopwatch|sw\s*=.*StartNew/)) { hasSw = true; break; }
        }

        // Check if already has PERF log
        let hasPerf = false;
        let depth2 = 0;
        for (let k = braceIdx; k < content.length; k++) {
            for (const ch of content[k]) { if (ch === '{') depth2++; if (ch === '}') depth2--; }
            if (content[k].includes('PERF ' + methodName)) { hasPerf = true; break; }
            if (depth2 === 0) break;
        }
        if (hasPerf) continue;

        // Determine indentation from the brace line
        const braceIndent = content[braceIdx].match(/^(\s*)/)[1];
        const bodyIndent = braceIndent + '    ';

        // Find closing brace using depth counting
        let depth = 0;
        let closingIdx = -1;
        for (let i = braceIdx; i < content.length; i++) {
            for (const ch of content[i]) {
                if (ch === '{') depth++;
                if (ch === '}') depth--;
            }
            if (depth === 0) { closingIdx = i; break; }
        }
        if (closingIdx < 0) continue;

        // Insert stop+log before closing brace
        const stopLine = `${bodyIndent}sw.Stop(); Log.Info("PERF ${methodName}: {0}ms", sw.ElapsedMilliseconds);`;
        content.splice(closingIdx, 0, stopLine);

        // Insert start after opening brace (only if sw doesn't already exist)
        if (!hasSw) {
            const startLine = `${bodyIndent}var sw = System.Diagnostics.Stopwatch.StartNew();`;
            content.splice(braceIdx + 1, 0, startLine);
        }

        totalFixed++;
    }

    fs.writeFileSync(filePath, content.join('\n'));
}

console.log(`Fixed ${totalFixed} methods`);
