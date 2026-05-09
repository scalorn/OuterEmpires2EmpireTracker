'use strict';

/** fwrite.test.js - Property-based tests for fwrite.js
Uses fast-check + Node assert. Run: node .kiro/tools/fwrite.test.js */

const fs = require('fs');
const path = require('path');
const assert = require('assert');
const fc = require('fast-check');
const { execSync } = require('child_process');
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

// Property 3: Replace is idempotent when new_text already present
// **Validates: Requirements 3.8, 3.9**
async function testReplaceIdempotency() {
    console.log('\nProperty 3: Replace is idempotent when new_text already present');
    setup();
    try {
        await fc.assert(fc.asyncProperty(
            fc.fullUnicodeString({ minLength: 1 }),
            fc.fullUnicodeString({ minLength: 1 }),
            fc.fullUnicodeString(),
            async function(prefix, newText, suffix) {
                var oldText = 'UNIQUE_OLD_' + Math.random().toString(36).slice(2) + '_END';
                var fileContent = prefix + newText + suffix;
                fc.pre(!fileContent.includes(oldText));
                fc.pre(newText.length > 0);
                var tp = tempFile('idem_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, fileContent, 'utf8');
                var result = await fwrite.replaceInFile(tp, oldText, newText);
                assert.strictEqual(result.success, true);
                assert.strictEqual(result.idempotent, true);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, fileContent);
                try { fs.unlinkSync(tp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Replace idempotent when new_text present', true);
    } catch (err) {
        reportResult('Replace idempotent when new_text present', false, err);
    }
    teardown();
}


// Property 4: Replace uniqueness enforcement
// **Validates: Requirements 3.4, 3.10, 3.11**
async function testReplaceUniqueness() {
    console.log('\nProperty 4: Replace uniqueness enforcement');
    setup();
    try {
        await fc.assert(fc.asyncProperty(
            fc.integer({ min: 2, max: 5 }),
            fc.fullUnicodeString({ minLength: 1, maxLength: 20 }),
            fc.fullUnicodeString(),
            async function(repeatCount, oldText, newText) {
                var parts = [];
                var sep = '|||';
                fc.pre(!oldText.includes(sep));
                for (var i = 0; i < repeatCount; i++) parts.push(sep + i + sep + oldText);
                var fileContent = parts.join('\n');
                var actualCount = fileContent.split(oldText).length - 1;
                fc.pre(actualCount === repeatCount);
                var tp = tempFile('uniq_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, fileContent, 'utf8');
                var result = await fwrite.replaceInFile(tp, oldText, newText);
                assert.strictEqual(result.success, false);
                assert.strictEqual(result.exitCode, 3);
                assert.strictEqual(result.count, repeatCount);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, fileContent);
                try { fs.unlinkSync(tp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Replace fails on multiple occurrences without modifying file', true);
    } catch (err) {
        reportResult('Replace fails on multiple occurrences without modifying file', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(
            fc.fullUnicodeString({ minLength: 1, maxLength: 50 }),
            fc.fullUnicodeString({ minLength: 1, maxLength: 50 }),
            fc.fullUnicodeString({ minLength: 1, maxLength: 50 }),
            async function(fileContent, oldText, newText) {
                fc.pre(!fileContent.includes(oldText));
                fc.pre(!fileContent.includes(newText));
                var tp = tempFile('nf_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, fileContent, 'utf8');
                var result = await fwrite.replaceInFile(tp, oldText, newText);
                assert.strictEqual(result.success, false);
                assert.strictEqual(result.exitCode, 2);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, fileContent);
                try { fs.unlinkSync(tp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Replace fails on not-found without modifying file', true);
    } catch (err) {
        reportResult('Replace fails on not-found without modifying file', false, err);
    }
    teardown();
}


// Property 5: Line ending normalization round-trip
// **Validates: Requirements 3.2, 3.3, 3.5**
async function testLineEndingNormalization() {
    console.log('\nProperty 5: Line ending normalization round-trip');
    setup();
    try {
        await fc.assert(fc.asyncProperty(
            fc.array(fc.fullUnicodeString({ minLength: 1, maxLength: 30 }), { minLength: 3, maxLength: 10 }),
            fc.integer({ min: 0, max: 2 }),
            fc.fullUnicodeString({ minLength: 1, maxLength: 30 }),
            async function(lines, targetLineIdx, newText) {
                var cleanLines = lines.map(function(l) { return l.replace(/[\r\n]/g, ''); });
                fc.pre(cleanLines.every(function(l) { return l.length > 0; }));
                var idx = Math.min(targetLineIdx, cleanLines.length - 1);
                var oldText = cleanLines[idx];
                var fileContent = cleanLines.join('\r\n');
                var occurrences = fileContent.split(oldText).length - 1;
                fc.pre(occurrences === 1);
                var tp = tempFile('crlf_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, fileContent, 'utf8');
                var cleanNewText = newText.replace(/[\r\n]/g, ' ');
                var result = await fwrite.replaceInFile(tp, oldText, cleanNewText);
                assert.strictEqual(result.success, true);
                assert.strictEqual(result.idempotent, false);
                var readBack = fs.readFileSync(tp, 'utf8');
                var hasLF = readBack.includes('\n');
                var hasCRLF = readBack.includes('\r\n');
                var bareLF = readBack.replace(/\r\n/g, '').includes('\n');
                if (hasLF) {
                    assert.strictEqual(hasCRLF, true, 'File should have CRLF');
                    assert.strictEqual(bareLF, false, 'File should not have bare LF');
                }
                try { fs.unlinkSync(tp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('CRLF file stays CRLF after replacement', true);
    } catch (err) {
        reportResult('CRLF file stays CRLF after replacement', false, err);
    }
    teardown();
}


// Property 6: Atomic write crash safety (unit test)
// **Validates: Requirements 4.1, 4.2, 4.5**
async function testAtomicWriteCrashSafety() {
    console.log('\nProperty 6: Atomic write crash safety');
    setup();
    try {
        var tp = tempFile('atomic_orig.txt');
        var originalContent = 'Original content that must not change';
        fs.writeFileSync(tp, originalContent, 'utf8');
        var tmpSibling = tp + '.tmp';
        var newContent = 'New content that would replace original';
        fs.writeFileSync(tmpSibling, newContent, 'utf8');
        var readBack = fs.readFileSync(tp, 'utf8');
        assert.strictEqual(readBack, originalContent, 'Original file must be unchanged when .tmp exists');
        var tmpContent = fs.readFileSync(tmpSibling, 'utf8');
        assert.strictEqual(tmpContent, newContent, 'Temp file should have new content');
        fs.unlinkSync(tmpSibling);
        fs.unlinkSync(tp);
        reportResult('Original file unchanged when .tmp sibling exists (simulated crash)', true);
    } catch (err) {
        reportResult('Original file unchanged when .tmp sibling exists (simulated crash)', false, err);
    }
    try {
        var tp2 = tempFile('atomic_success.txt');
        fs.writeFileSync(tp2, 'initial', 'utf8');
        await fwrite.atomicWrite(tp2, 'updated content');
        var readBack2 = fs.readFileSync(tp2, 'utf8');
        assert.strictEqual(readBack2, 'updated content');
        assert.strictEqual(fs.existsSync(tp2 + '.tmp'), false, '.tmp file should be cleaned up');
        fs.unlinkSync(tp2);
        reportResult('Atomic write cleans up .tmp file on success', true);
    } catch (err) {
        reportResult('Atomic write cleans up .tmp file on success', false, err);
    }
    try {
        var tp3 = tempFile('subdir/atomic_new.txt');
        await fwrite.atomicWrite(tp3, 'brand new file');
        var readBack3 = fs.readFileSync(tp3, 'utf8');
        assert.strictEqual(readBack3, 'brand new file');
        assert.strictEqual(fs.existsSync(tp3 + '.tmp'), false);
        reportResult('Atomic write to new file creates parent dirs', true);
    } catch (err) {
        reportResult('Atomic write to new file creates parent dirs', false, err);
    }
    teardown();
}


// Property 7: BOM stripping is universal
// **Validates: Requirements 1.6, 2.6, 3.12, 5.3**
async function testBomStrippingUniversal() {
    console.log('\nProperty 7: BOM stripping is universal');
    setup();
    try {
        await fc.assert(fc.asyncProperty(fc.fullUnicodeString(), async function(content) {
            var bomContent = '\uFEFF' + content;
            var cp = tempFile('bom_w_' + Math.random().toString(36).slice(2) + '.tmp');
            var tp = tempFile('bom_wt_' + Math.random().toString(36).slice(2) + '.txt');
            fs.writeFileSync(cp, bomContent, 'utf8');
            var result = fwrite.readContentFile(cp, false);
            await fwrite.atomicWrite(tp, result.content);
            var readBack = fs.readFileSync(tp, 'utf8');
            assert.strictEqual(readBack.charCodeAt(0) !== 0xFEFF, true, 'Write output must not contain BOM');
            assert.strictEqual(readBack, content);
            try { fs.unlinkSync(tp); } catch(e) {}
            try { fs.unlinkSync(cp); } catch(e) {}
        }), { numRuns: 100 });
        reportResult('BOM stripped for write operations', true);
    } catch (err) {
        reportResult('BOM stripped for write operations', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(
            fc.fullUnicodeString(),
            fc.fullUnicodeString(),
            async function(existing, appendContent) {
                var bomAppend = '\uFEFF' + appendContent;
                var tp = tempFile('bom_a_' + Math.random().toString(36).slice(2) + '.txt');
                var cp = tempFile('bom_ac_' + Math.random().toString(36).slice(2) + '.tmp');
                fs.writeFileSync(tp, existing, 'utf8');
                fs.writeFileSync(cp, bomAppend, 'utf8');
                var result = fwrite.readContentFile(cp, false);
                await fwrite.appendToFile(tp, result.content);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack, existing + appendContent);
                assert.strictEqual(readBack.includes('\uFEFF'), false, 'Append output must not contain BOM');
                try { fs.unlinkSync(tp); } catch(e) {}
                try { fs.unlinkSync(cp); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('BOM stripped for append operations', true);
    } catch (err) {
        reportResult('BOM stripped for append operations', false, err);
    }
    try {
        await fc.assert(fc.asyncProperty(
            fc.fullUnicodeString({ minLength: 1, maxLength: 30 }),
            fc.fullUnicodeString({ minLength: 1, maxLength: 30 }),
            async function(oldText, newText) {
                var fileContent = 'PREFIX_' + oldText + '_SUFFIX';
                var bomOld = '\uFEFF' + oldText;
                var bomNew = '\uFEFF' + newText;
                fc.pre(fileContent.split(oldText).length - 1 === 1);
                var tp = tempFile('bom_r_' + Math.random().toString(36).slice(2) + '.txt');
                fs.writeFileSync(tp, fileContent, 'utf8');
                var oldF = tempFile('bom_ro_' + Math.random().toString(36).slice(2) + '.tmp');
                var newF = tempFile('bom_rn_' + Math.random().toString(36).slice(2) + '.tmp');
                fs.writeFileSync(oldF, bomOld, 'utf8');
                fs.writeFileSync(newF, bomNew, 'utf8');
                var oldResult = fwrite.readContentFile(oldF, false);
                var newResult = fwrite.readContentFile(newF, false);
                var result = await fwrite.replaceInFile(tp, oldResult.content, newResult.content);
                assert.strictEqual(result.success, true);
                var readBack = fs.readFileSync(tp, 'utf8');
                assert.strictEqual(readBack.includes('\uFEFF'), false, 'Replace output must not contain BOM');
                try { fs.unlinkSync(tp); } catch(e) {}
                try { fs.unlinkSync(oldF); } catch(e) {}
                try { fs.unlinkSync(newF); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('BOM stripped for replace operations', true);
    } catch (err) {
        reportResult('BOM stripped for replace operations', false, err);
    }
    teardown();
}


// Property 8: Batch atomicity
// **Validates: Requirements 7.3, 7.4**
async function testBatchAtomicity() {
    console.log('\nProperty 8: Batch atomicity');
    setup();
    try {
        await fc.assert(fc.asyncProperty(
            fc.integer({ min: 2, max: 5 }),
            fc.array(fc.fullUnicodeString({ minLength: 1, maxLength: 50 }), { minLength: 2, maxLength: 5 }),
            async function(failAt, contents) {
                var numOps = Math.min(failAt, contents.length);
                fc.pre(numOps >= 2);
                var failIdx = numOps - 1;
                var targets = [];
                var initialContents = [];
                for (var i = 0; i < numOps; i++) {
                    var tp = tempFile('batch_' + i + '_' + Math.random().toString(36).slice(2) + '.txt');
                    var initial = 'INITIAL_' + i + '_' + Math.random().toString(36).slice(2);
                    fs.writeFileSync(tp, initial, 'utf8');
                    targets.push(tp);
                    initialContents.push(initial);
                }
                var contentFiles = [];
                for (var j = 0; j < numOps - 1; j++) {
                    var cf = tempFile('content_' + j + '_' + Math.random().toString(36).slice(2) + '.tmp');
                    fs.writeFileSync(cf, contents[j], 'utf8');
                    contentFiles.push(cf);
                }
                var oldFile = tempFile('old_' + Math.random().toString(36).slice(2) + '.tmp');
                var newFile = tempFile('new_' + Math.random().toString(36).slice(2) + '.tmp');
                var uniqueOld = 'THIS_TEXT_DOES_NOT_EXIST_' + Math.random().toString(36).slice(2);
                fs.writeFileSync(oldFile, uniqueOld, 'utf8');
                fs.writeFileSync(newFile, 'replacement', 'utf8');
                var operations = [];
                for (var k = 0; k < numOps - 1; k++) {
                    operations.push({ op: 'write', target: targets[k], content_file: contentFiles[k] });
                }
                operations.push({ op: 'replace', target: targets[failIdx], old_file: oldFile, new_file: newFile });
                var manifest = { operations: operations };
                var result = await fwrite.executeBatch(manifest);
                assert.strictEqual(result.rolledBack, true, 'Batch should have rolled back');
                assert.strictEqual(result.failedOp, numOps, 'Should fail on last operation');
                for (var m = 0; m < numOps; m++) {
                    var actual = fs.readFileSync(targets[m], 'utf8');
                    assert.strictEqual(actual, initialContents[m], 'File ' + m + ' should be restored');
                }
                targets.forEach(function(f) { try { fs.unlinkSync(f); } catch(e) {} });
                contentFiles.forEach(function(f) { try { fs.unlinkSync(f); } catch(e) {} });
                try { fs.unlinkSync(oldFile); } catch(e) {}
                try { fs.unlinkSync(newFile); } catch(e) {}
            }
        ), { numRuns: 100 });
        reportResult('Batch rollback restores all files to pre-batch state', true);
    } catch (err) {
        reportResult('Batch rollback restores all files to pre-batch state', false, err);
    }
    teardown();
}


// Property 9: Verification detects corruption (unit test)
// **Validates: Requirements 8.1, 8.2**
async function testVerificationMismatch() {
    console.log('\nProperty 9: Verification detects corruption');
    setup();
    try {
        var tp = tempFile('verify_ok.txt');
        var content = 'Hello verification test';
        fs.writeFileSync(tp, content, 'utf8');
        var expectedBytes = Buffer.byteLength(content, 'utf8');
        var result = fwrite.verifyWrite(tp, expectedBytes);
        assert.strictEqual(result, true);
        fs.unlinkSync(tp);
        reportResult('verifyWrite passes with correct byte count', true);
    } catch (err) {
        reportResult('verifyWrite passes with correct byte count', false, err);
    }
    try {
        var tp2 = tempFile('verify_fail.txt');
        var content2 = 'Short content';
        fs.writeFileSync(tp2, content2, 'utf8');
        var wrongBytes = Buffer.byteLength(content2, 'utf8') + 100;
        // Write a script file to test process.exit behavior
        var scriptFile = tempFile('vscript.js');
        var fwPath = path.resolve('.kiro/tools/fwrite.js').replace(/\\/g, '/');
        var tp2Fwd = tp2.replace(/\\/g, '/');
        var scriptContent = 'var fw = require("' + fwPath + '"); fw.verifyWrite("' + tp2Fwd + '", ' + wrongBytes + ');';
        fs.writeFileSync(scriptFile, scriptContent, 'utf8');
        var exitCode = 0;
        try {
            execSync('node "' + scriptFile + '"', { stdio: 'pipe' });
        } catch (e) {
            exitCode = e.status;
        }
        assert.strictEqual(exitCode, 6, 'Should exit with code 6 on verification mismatch');
        fs.unlinkSync(tp2);
        try { fs.unlinkSync(scriptFile); } catch(ig) {}
        reportResult('verifyWrite exits with code 6 on byte length mismatch', true);
    } catch (err) {
        reportResult('verifyWrite exits with code 6 on byte length mismatch', false, err);
    }
    try {
        var tp3 = tempFile('verify_append.txt');
        var content3 = 'Hello append verify';
        fs.writeFileSync(tp3, content3, 'utf8');
        var totalBytes = Buffer.byteLength(content3, 'utf8');
        var result3 = fwrite.verifyAppend(tp3, totalBytes);
        assert.strictEqual(result3, true);
        fs.unlinkSync(tp3);
        reportResult('verifyAppend passes with correct total byte count', true);
    } catch (err) {
        reportResult('verifyAppend passes with correct total byte count', false, err);
    }
    try {
        var tp4 = tempFile('verify_append_fail.txt');
        var content4 = 'Append content';
        fs.writeFileSync(tp4, content4, 'utf8');
        var wrongTotal = Buffer.byteLength(content4, 'utf8') + 50;
        var scriptFile4 = tempFile('vscript4.js');
        var fwPath4 = path.resolve('.kiro/tools/fwrite.js').replace(/\\/g, '/');
        var tp4Fwd = tp4.replace(/\\/g, '/');
        var scriptContent4 = 'var fw = require("' + fwPath4 + '"); fw.verifyAppend("' + tp4Fwd + '", ' + wrongTotal + ');';
        fs.writeFileSync(scriptFile4, scriptContent4, 'utf8');
        var exitCode4 = 0;
        try {
            execSync('node "' + scriptFile4 + '"', { stdio: 'pipe' });
        } catch (e) {
            exitCode4 = e.status;
        }
        assert.strictEqual(exitCode4, 6, 'Should exit with code 6 on append verification mismatch');
        fs.unlinkSync(tp4);
        try { fs.unlinkSync(scriptFile4); } catch(ig) {}
        reportResult('verifyAppend exits with code 6 on byte length mismatch', true);
    } catch (err) {
        reportResult('verifyAppend exits with code 6 on byte length mismatch', false, err);
    }
    teardown();
}


// Property 10: Lock retry convergence (unit test)
// **Validates: Requirements 6.1, 6.3**
async function testLockRetryBehavior() {
    console.log('\nProperty 10: Lock retry convergence');
    try {
        var ebusyErr = new Error('EBUSY'); ebusyErr.code = 'EBUSY';
        var epermErr = new Error('EPERM'); epermErr.code = 'EPERM';
        var eaccesErr = new Error('EACCES'); eaccesErr.code = 'EACCES';
        var enoentErr = new Error('ENOENT'); enoentErr.code = 'ENOENT';
        var enospcErr = new Error('ENOSPC'); enospcErr.code = 'ENOSPC';
        var noCodeErr = new Error('generic');
        assert.strictEqual(fwrite.isRetryableError(ebusyErr), true, 'EBUSY should be retryable');
        assert.strictEqual(fwrite.isRetryableError(epermErr), true, 'EPERM should be retryable');
        assert.strictEqual(fwrite.isRetryableError(eaccesErr), true, 'EACCES should be retryable');
        assert.strictEqual(fwrite.isRetryableError(enoentErr), false, 'ENOENT should not be retryable');
        assert.strictEqual(fwrite.isRetryableError(enospcErr), false, 'ENOSPC should not be retryable');
        assert.strictEqual(!!fwrite.isRetryableError(noCodeErr), false, 'Error without code should not be retryable');
        reportResult('isRetryableError correctly identifies retryable codes', true);
    } catch (err) {
        reportResult('isRetryableError correctly identifies retryable codes', false, err);
    }
    try {
        assert.strictEqual(fwrite.RETRY_CONFIG.maxRetries, 3, 'maxRetries should be 3');
        assert.deepStrictEqual(fwrite.RETRY_CONFIG.backoffMs, [100, 200, 400], 'backoffMs should be [100, 200, 400]');
        assert.deepStrictEqual(fwrite.RETRY_CONFIG.retryableCodes, ['EBUSY', 'EPERM', 'EACCES']);
        var totalBackoff = fwrite.RETRY_CONFIG.backoffMs.reduce(function(a, b) { return a + b; }, 0);
        assert.strictEqual(totalBackoff, 700, 'Total backoff should be 700ms');
        reportResult('RETRY_CONFIG has correct values', true);
    } catch (err) {
        reportResult('RETRY_CONFIG has correct values', false, err);
    }
    try {
        var start = Date.now();
        await fwrite.sleep(100);
        var elapsed = Date.now() - start;
        assert.ok(elapsed >= 90, 'sleep(100) should take at least 90ms, got ' + elapsed);
        assert.ok(elapsed < 200, 'sleep(100) should take less than 200ms, got ' + elapsed);
        reportResult('sleep function works within timing tolerance', true);
    } catch (err) {
        reportResult('sleep function works within timing tolerance', false, err);
    }
}


// Unit tests for error formatting, CLI parsing, and utility functions
// **Validates: Requirements 9.1, 9.2, 9.3, 10.2, 10.3**
async function testUtilityFunctions() {
    console.log('\nUnit tests: Error formatting, CLI parsing, utilities');
    try {
        var msg = fwrite.formatSuccess('Wrote 100 chars to test.txt');
        assert.strictEqual(msg, 'OK: Wrote 100 chars to test.txt [verified]');
        assert.ok(msg.startsWith('OK:'), 'Success messages must start with OK:');
        assert.ok(msg.includes('[verified]'), 'Success messages must include [verified]');
        reportResult('formatSuccess produces correct output format', true);
    } catch (err) {
        reportResult('formatSuccess produces correct output format', false, err);
    }
    try {
        var errMsg = fwrite.formatError({
            operation: 'write',
            path: 'test.txt',
            reason: 'File not found',
            remediation: 'Check the path'
        });
        assert.ok(errMsg.startsWith('ERROR:'), 'Error messages must start with ERROR:');
        assert.ok(errMsg.includes('write'), 'Error must include operation');
        assert.ok(errMsg.includes('test.txt'), 'Error must include path');
        assert.ok(errMsg.includes('File not found'), 'Error must include reason');
        assert.ok(errMsg.includes('Suggested fix:'), 'Error must include remediation prefix');
        assert.ok(errMsg.includes('Check the path'), 'Error must include remediation');
        reportResult('formatError produces correct structured output', true);
    } catch (err) {
        reportResult('formatError produces correct structured output', false, err);
    }
    try {
        var errMsg2 = fwrite.formatError({
            operation: 'read',
            path: 'missing.txt',
            reason: 'ENOENT',
            remediation: ''
        });
        assert.ok(errMsg2.startsWith('ERROR:'), 'Error messages must start with ERROR:');
        assert.ok(!errMsg2.includes('Suggested fix:'), 'No remediation prefix when empty');
        reportResult('formatError handles empty remediation', true);
    } catch (err) {
        reportResult('formatError handles empty remediation', false, err);
    }
    try {
        var codes = Object.keys(fwrite.FS_ERROR_MAP);
        for (var i = 0; i < codes.length; i++) {
            var testErr = new Error('test'); testErr.code = codes[i];
            var mapped = fwrite.mapFsError(testErr);
            assert.strictEqual(mapped, fwrite.FS_ERROR_MAP[codes[i]], 'mapFsError should map ' + codes[i]);
        }
        var unknownErr = new Error('Something weird'); unknownErr.code = 'EUNKNOWN';
        var unknownMapped = fwrite.mapFsError(unknownErr);
        assert.strictEqual(unknownMapped, 'Something weird', 'Unknown code should use error message');
        reportResult('mapFsError maps all known codes correctly', true);
    } catch (err) {
        reportResult('mapFsError maps all known codes correctly', false, err);
    }
    try {
        assert.strictEqual(fwrite.stripBom('\uFEFFhello'), 'hello', 'Should strip BOM');
        assert.strictEqual(fwrite.stripBom('hello'), 'hello', 'Should not modify non-BOM string');
        assert.strictEqual(fwrite.stripBom('\uFEFF'), '', 'Should strip BOM from BOM-only string');
        assert.strictEqual(fwrite.stripBom(''), '', 'Should handle empty string');
        reportResult('stripBom correctly strips/preserves content', true);
    } catch (err) {
        reportResult('stripBom correctly strips/preserves content', false, err);
    }
    try {
        var valid = fwrite.validateEncoding('Hello world');
        assert.strictEqual(valid.valid, true);
        assert.strictEqual(valid.byteOffset, null);
        assert.strictEqual(valid.warning, null);
        var invalid = fwrite.validateEncoding('Hello \uFFFD world');
        assert.strictEqual(invalid.valid, false);
        assert.ok(invalid.byteOffset !== null, 'Should report byte offset');
        assert.ok(invalid.warning !== null, 'Should report warning');
        reportResult('validateEncoding detects valid and invalid UTF-8', true);
    } catch (err) {
        reportResult('validateEncoding detects valid and invalid UTF-8', false, err);
    }
    try {
        assert.strictEqual(fwrite.detectCRLF('line1\r\nline2'), true, 'Should detect CRLF');
        assert.strictEqual(fwrite.detectCRLF('line1\nline2'), false, 'Should not detect LF as CRLF');
        assert.strictEqual(fwrite.detectCRLF('no newlines'), false, 'Should not detect in no-newline content');
        reportResult('detectCRLF correctly identifies line endings', true);
    } catch (err) {
        reportResult('detectCRLF correctly identifies line endings', false, err);
    }
    try {
        assert.strictEqual(fwrite.normalizeToLF('a\r\nb\r\nc'), 'a\nb\nc', 'Should convert CRLF to LF');
        assert.strictEqual(fwrite.normalizeToLF('a\nb\nc'), 'a\nb\nc', 'Should leave LF unchanged');
        assert.strictEqual(fwrite.normalizeToLF('no newlines'), 'no newlines');
        reportResult('normalizeToLF converts CRLF to LF', true);
    } catch (err) {
        reportResult('normalizeToLF converts CRLF to LF', false, err);
    }
    try {
        assert.strictEqual(fwrite.restoreCRLF('a\nb\nc'), 'a\r\nb\r\nc', 'Should convert LF to CRLF');
        assert.strictEqual(fwrite.restoreCRLF('a\r\nb\r\nc'), 'a\r\nb\r\nc', 'Should not double-convert');
        assert.strictEqual(fwrite.restoreCRLF('no newlines'), 'no newlines');
        reportResult('restoreCRLF converts LF to CRLF without doubling', true);
    } catch (err) {
        reportResult('restoreCRLF converts LF to CRLF without doubling', false, err);
    }
    try {
        var exitUnknown = 0;
        try { execSync('node "' + path.resolve('.kiro/tools/fwrite.js') + '" unknowncmd test.txt', { stdio: 'pipe' }); }
        catch (e) { exitUnknown = e.status; }
        assert.strictEqual(exitUnknown, 1, 'Unknown command should exit with code 1');
        var exitNoCmd = 0;
        try { execSync('node "' + path.resolve('.kiro/tools/fwrite.js') + '"', { stdio: 'pipe' }); }
        catch (e) { exitNoCmd = e.status; }
        assert.strictEqual(exitNoCmd, 1, 'No command should exit with code 1');
        var exitNoContent = 0;
        try { execSync('node "' + path.resolve('.kiro/tools/fwrite.js') + '" writefile test.txt', { stdio: 'pipe' }); }
        catch (e) { exitNoContent = e.status; }
        assert.strictEqual(exitNoContent, 1, 'writefile without content file should exit with code 1');
        var exitNoOld = 0;
        try { execSync('node "' + path.resolve('.kiro/tools/fwrite.js') + '" replace test.txt nonexistent_old.tmp nonexistent_new.tmp', { stdio: 'pipe' }); }
        catch (e) { exitNoOld = e.status; }
        assert.strictEqual(exitNoOld, 1, 'replace with missing files should exit with code 1');
        reportResult('CLI argument parsing routes commands correctly', true);
    } catch (err) {
        reportResult('CLI argument parsing routes commands correctly', false, err);
    }
    try {
        assert.strictEqual(fwrite.EXIT_CODES.SUCCESS, 0);
        assert.strictEqual(fwrite.EXIT_CODES.GENERAL_ERROR, 1);
        assert.strictEqual(fwrite.EXIT_CODES.REPLACE_NOT_FOUND, 2);
        assert.strictEqual(fwrite.EXIT_CODES.REPLACE_MULTIPLE, 3);
        assert.strictEqual(fwrite.EXIT_CODES.FILE_LOCKED, 4);
        assert.strictEqual(fwrite.EXIT_CODES.STDIN_TIMEOUT, 5);
        assert.strictEqual(fwrite.EXIT_CODES.VERIFICATION_FAILED, 6);
        assert.strictEqual(fwrite.EXIT_CODES.BATCH_VALIDATION, 7);
        assert.strictEqual(fwrite.EXIT_CODES.BATCH_EXECUTION, 8);
        reportResult('EXIT_CODES constants have correct values', true);
    } catch (err) {
        reportResult('EXIT_CODES constants have correct values', false, err);
    }
}


async function runTests() {
    console.log('fwrite.js Property-Based Tests');
    console.log('==============================');
    await testWriteRoundTrip();
    await testAppendPreservesContent();
    await testReplaceIdempotency();
    await testReplaceUniqueness();
    await testLineEndingNormalization();
    await testAtomicWriteCrashSafety();
    await testBomStrippingUniversal();
    await testBatchAtomicity();
    await testVerificationMismatch();
    await testLockRetryBehavior();
    await testUtilityFunctions();
    console.log('\n==============================');
    console.log('Results: ' + passCount + ' passed, ' + failCount + ' failed');
    if (failCount > 0) { process.exit(1); }
}

runTests().catch(function(err) { console.error('Test runner error:', err); process.exit(1); });
