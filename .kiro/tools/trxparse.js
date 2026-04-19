#!/usr/bin/env node
/**
 * trxparse.js — Parse vstest.console TRX result files and output a clean summary.
 *
 * Usage:
 *   node .kiro/tools/trxparse.js [path-to-trx]
 *
 * If no path is given, finds the most recent .trx file in TestResults/.
 *
 * Output:
 *   PASS: 1463 passed, 0 failed, 0 skipped (2.4s)
 *   or
 *   FAIL: 1460 passed, 3 failed, 0 skipped (2.4s)
 *     FAILED: Namespace.TestClass.TestMethod
 *       Message: Expected 5 but was 3
 *       StackTrace: at ... line 42
 */

const fs = require('fs');
const path = require('path');

function findLatestTrx() {
    const dir = 'TestResults';
    if (!fs.existsSync(dir)) {
        console.error('No TestResults/ directory found. Run vstest.console with /Logger:trx first.');
        process.exit(1);
    }
    const files = fs.readdirSync(dir)
        .filter(f => f.endsWith('.trx'))
        .map(f => ({ name: f, mtime: fs.statSync(path.join(dir, f)).mtimeMs }))
        .sort((a, b) => b.mtime - a.mtime);

    if (files.length === 0) {
        console.error('No .trx files found in TestResults/.');
        process.exit(1);
    }
    return path.join(dir, files[0].name);
}

function extractText(xml, tag) {
    const re = new RegExp(`<${tag}[^>]*>([\\s\\S]*?)</${tag}>`, 'g');
    const matches = [];
    let m;
    while ((m = re.exec(xml)) !== null) matches.push(m[1]);
    return matches;
}

function extractAttr(element, attr) {
    const re = new RegExp(`${attr}="([^"]*)"`, 'i');
    const m = element.match(re);
    return m ? m[1] : '';
}

function main() {
    const trxPath = process.argv[2] || findLatestTrx();

    if (!fs.existsSync(trxPath)) {
        console.error(`File not found: ${trxPath}`);
        process.exit(1);
    }

    const xml = fs.readFileSync(trxPath, 'utf8');

    // Extract counters from ResultSummary
    const countersMatch = xml.match(/<Counters[^/]*\/>/);
    let passed = 0, failed = 0, skipped = 0, total = 0;
    if (countersMatch) {
        passed = parseInt(extractAttr(countersMatch[0], 'passed')) || 0;
        failed = parseInt(extractAttr(countersMatch[0], 'failed')) || 0;
        const notExecuted = parseInt(extractAttr(countersMatch[0], 'notExecuted')) || 0;
        const inconclusive = parseInt(extractAttr(countersMatch[0], 'inconclusive')) || 0;
        skipped = notExecuted + inconclusive;
        total = parseInt(extractAttr(countersMatch[0], 'total')) || 0;
    }

    // Extract duration
    const timesMatch = xml.match(/<Times[^/]*\/>/);
    let duration = '';
    if (timesMatch) {
        const start = extractAttr(timesMatch[0], 'start');
        const finish = extractAttr(timesMatch[0], 'finish');
        if (start && finish) {
            const ms = new Date(finish) - new Date(start);
            duration = ` (${(ms / 1000).toFixed(1)}s)`;
        }
    }

    // Extract failed test details
    const failures = [];
    const resultRe = /<UnitTestResult[^>]*outcome="Failed"[^>]*>/gi;
    let resultMatch;
    while ((resultMatch = resultRe.exec(xml)) !== null) {
        const testName = extractAttr(resultMatch[0], 'testName');

        // Find the Output/ErrorInfo block after this result
        const afterResult = xml.substring(resultMatch.index, resultMatch.index + 5000);
        const msgMatch = afterResult.match(/<Message>([\s\S]*?)<\/Message>/);
        const stackMatch = afterResult.match(/<StackTrace>([\s\S]*?)<\/StackTrace>/);

        failures.push({
            name: testName,
            message: msgMatch ? msgMatch[1].trim() : '',
            stack: stackMatch ? stackMatch[1].trim().split('\n')[0].trim() : ''
        });
    }

    // Output
    const status = failed > 0 ? 'FAIL' : 'PASS';
    console.log(`${status}: ${passed} passed, ${failed} failed, ${skipped} skipped${duration}`);

    for (const f of failures) {
        console.log(`  FAILED: ${f.name}`);
        if (f.message) console.log(`    Message: ${f.message}`);
        if (f.stack) console.log(`    Stack: ${f.stack}`);
    }

    process.exit(failed > 0 ? 1 : 0);
}

main();
