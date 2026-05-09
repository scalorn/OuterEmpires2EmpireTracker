'use strict';

/** fwrite.test.js - Property-based tests for fwrite.js
Uses fast-check + Node assert. Run: node .kiro/tools/fwrite.test.js */

const fs = require('fs');
const path = require('path');
const assert = require('assert');
const fc = require('fast-check');
const fwrite = require('./fwrite.js');

const TEST_DIR = path.join(__dirname, '_test_tmp_' + process.pid);
function setup() { fs.mkdirSync(TEST_DIR, { recursive: true }); }
function teardown() { fs.rmSync(TEST_DIR, { recursive: true, force: true }); }
function tempFile(name) { return path.join(TEST_DIR, name); }

let passCount = 0;
let failCount = 0;
function reportResult(name, passed, error) {
    if (passed) { passCount++; console.log('  PASS: ' + name); }
    else { failCount++; console.log('  FAIL: ' + name); if (error) console.log('    ' + error.message); }
}
// Property 1: Write round-trip preserves content
// **Validates: Requirements 1.1, 5.1, 5.2, 5.4**
async function testWriteRoundTrip() {
    console.log('\nProperty 1: Write round-trip preserves content');
    setup();
    try {
        await fc.assert(fc.asyncProperty(fc.fullUnicodeString(), async function(content) {
            var tp = tempFile('rt_' + Math.random().toString(36).slice(2) + '.txt');
            var cp = tempFile('ct_' + Math.random().toString(36).slice(2) + '.tmp');
            fs.writeFileSync(cp, content, 'utf8');
            var result = fwrite.readContentFile(cp, false);
            await fwrite.atomicWrite(tp, result.content);
            var readBack = fs.readFileSync(tp, 'utf8');
            assert.strictEqual(readBack, result.content);
            try { fs.unlinkSync(tp); } catch(e) {}
            try { fs.unlinkSync(cp); } catch(e) {}
        }), { numRuns: 100 });
        reportResult('UTF-8 write round-trip preserves content', true);
    } catch (err) {
        reportResult('UTF-8 write round-trip preserves content', false, err);
    }
    try {
        fc.assert(fc.property(fc.fullUnicodeString(), function(content) {
            var bomContent = '\uFEFF' + content;
            var cp = tempFile('bom_' + Math.random().toString(36).slice(2) + '.tmp');
            fs.writeFileSync(cp, bomContent, 'utf8');
            var result = fwrite.readContentFile(cp, false);
            assert.strictEqual(result.content, content);
            try { fs.unlinkSync(cp); } catch(e) {}
        }), { numRuns: 100 });
        reportResult('BOM stripped and content preserved', true);
    } catch (err) {
        reportResult('BOM stripped and content preserved', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(fc.stringOf(fc.oneof(
            fc.fullUnicode(), fc.constant('\u2014'), fc.constant('\u00D7'),
            fc.constant('\u2500'), fc.constant('\n'), fc.constant('')
        )), async function(content) {
            var tp = tempFile('mb_' + Math.random().toString(36).slice(2) + '.txt');
            await fwrite.atomicWrite(tp, content);
            var readBack = fs.readFileSync(tp, 'utf8');
            assert.strictEqual(readBack, content);
            try { fs.unlinkSync(tp); } catch(e) {}
        }), { numRuns: 100 });
        reportResult('Multi-byte characters preserved', true);
    } catch (err) {
        reportResult('Multi-byte characters preserved', false, err);
    }
    teardown();
}

// Property 2: Append preserves existing content
// **Validates: Requirements 2.1, 2.3**
async function testAppendPreservesContent() {
    console.log('\nProperty 2: Append preserves existing content');
    setup();
    try {
        await fc.assert(fc.asyncProperty(fc.fullUnicodeString(), fc.fullUnicodeString(),
            async function(existingContent, appendContent) {
                var tp = tempFile('ap_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, existingContent, 'utf8');
                await fwrite.appendToFile(tp, appendContent);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, existingContent + appendContent);
                try { fs.unlinkSync(tp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Existing content preserved after append', true);
    } catch (err) {
        reportResult('Existing content preserved after append', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(fc.fullUnicodeString(), async function(content) {
            var tp = tempFile('apn_' + Math.random().toString(36).slice(2) + '.txt');
            assert.strictEqual(fs.existsSync(tp), false);
            await fwrite.appendToFile(tp, content);
            var readBack = fs.readFileSync(tp, 'utf8');
            assert.strictEqual(readBack, content);
            try { fs.unlinkSync(tp); } catch(e) {}
        }), { numRuns: 100 });
        reportResult('Append to non-existent file creates it correctly', true);
    } catch (err) {
        reportResult('Append to non-existent file creates it correctly', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(fc.fullUnicodeString(), fc.fullUnicodeString(),
            async function(existingContent, appendContent) {
                var tp = tempFile('pl_' + Math.random().toString(36).slice(2) + '.txt');
                var cf = tempFile('plc_' + Math.random().toString(36).slice(2) + '.tmp');
                fs.writeFileSync(tp, existingContent, 'utf8');
                fs.writeFileSync(cf, appendContent, 'utf8');
                var result = fwrite.readContentFile(cf, false);
                await fwrite.appendToFile(tp, result.content);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, existingContent + result.content);
                try { fs.unlinkSync(tp); } catch(e) {}
                try { fs.unlinkSync(cf); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Full pipeline (readContentFile + appendToFile) preserves content', true);
    } catch (err) {
        reportResult('Full pipeline (readContentFile + appendToFile) preserves content', false, err);
    }
    teardown();
}

async function runTests() {
    console.log('fwrite.js Property-Based Tests');
    console.log('==============================');
    await testWriteRoundTrip();
    await testAppendPreservesContent();
    console.log('\n==============================');
    console.log('Results: ' + passCount + ' passed, ' + failCount + ' failed');
    if (failCount > 0) { process.exit(1); }
}

runTests().catch(function(err) { console.error('Test runner error:', err); process.exit(1); });
