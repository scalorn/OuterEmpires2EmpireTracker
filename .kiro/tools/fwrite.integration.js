'use strict';

/** fwrite.integration.js - Integration tests for fwrite.js
Tests end-to-end operations via subprocess spawning.
Run: node .kiro/tools/fwrite.integration.js */

const fs = require('fs');
const path = require('path');
const assert = require('assert');
const { execSync } = require('child_process');

const FWRITE = path.resolve(__dirname, 'fwrite.js');
const TEST_DIR = path.join(__dirname, '_integration_tmp_' + process.pid);
const PROJECT_ROOT = path.resolve(__dirname, '../..');

function setup() { fs.mkdirSync(TEST_DIR, { recursive: true }); }
function teardown() { fs.rmSync(TEST_DIR, { recursive: true, force: true }); }
function tempFile(name) { return path.join(TEST_DIR, name); }

let passCount = 0;
let failCount = 0;
function reportResult(name, passed, error) {
    if (passed) { passCount++; console.log('  PASS: ' + name); }
    else { failCount++; console.log('  FAIL: ' + name); if (error) console.log('    ' + error.message || error); }
}

function runFwrite(args, opts) {
    var cmd = 'node "' + FWRITE + '" ' + args;
    var options = Object.assign({ cwd: PROJECT_ROOT, stdio: 'pipe', encoding: 'utf8' }, opts || {});
    try {
        var stdout = execSync(cmd, options);
        return { exitCode: 0, stdout: stdout.toString() };
    } catch (e) {
        return { exitCode: e.status || 1, stdout: (e.stdout || '').toString(), stderr: (e.stderr || '').toString() };
    }
}

// Test: Write operation via subprocess
function testWriteOperation() {
    console.log('\nIntegration: Write operations');
    setup();
    try {
        var target = tempFile('write_test.txt');
        var contentFile = tempFile('write_content.tmp');
        var content = 'Hello from integration test!\nLine 2\nLine 3';
        fs.writeFileSync(contentFile, content, 'utf8');
        var result = runFwrite('writefile "' + target + '" "' + contentFile + '"');
        assert.strictEqual(result.exitCode, 0, 'Write should succeed');
        assert.ok(result.stdout.includes('OK:'), 'Should output OK:');
        assert.ok(result.stdout.includes('[verified]'), 'Should include verified');
        var readBack = fs.readFileSync(target, 'utf8');
        assert.strictEqual(readBack, content, 'Content should match');
        reportResult('writefile creates file with correct content', true);
    } catch (err) {
        reportResult('writefile creates file with correct content', false, err);
    }
    try {
        var target2 = tempFile('subdir/nested/write_test.txt');
        var contentFile2 = tempFile('write_content2.tmp');
        fs.writeFileSync(contentFile2, 'nested content', 'utf8');
        var result2 = runFwrite('writefile "' + target2 + '" "' + contentFile2 + '"');
        assert.strictEqual(result2.exitCode, 0, 'Write to nested dir should succeed');
        assert.strictEqual(fs.readFileSync(target2, 'utf8'), 'nested content');
        reportResult('writefile creates parent directories', true);
    } catch (err) {
        reportResult('writefile creates parent directories', false, err);
    }
    teardown();
}

// Test: Append operation via subprocess
function testAppendOperation() {
    console.log('\nIntegration: Append operations');
    setup();
    try {
        var target = tempFile('append_test.txt');
        fs.writeFileSync(target, 'existing content', 'utf8');
        var contentFile = tempFile('append_content.tmp');
        fs.writeFileSync(contentFile, ' + appended', 'utf8');
        var result = runFwrite('appendfile "' + target + '" "' + contentFile + '"');
        assert.strictEqual(result.exitCode, 0, 'Append should succeed');
        assert.ok(result.stdout.includes('OK:'), 'Should output OK:');
        var readBack = fs.readFileSync(target, 'utf8');
        assert.strictEqual(readBack, 'existing content + appended');
        reportResult('appendfile appends to existing file', true);
    } catch (err) {
        reportResult('appendfile appends to existing file', false, err);
    }
    try {
        var target2 = tempFile('append_new.txt');
        var contentFile2 = tempFile('append_content2.tmp');
        fs.writeFileSync(contentFile2, 'brand new via append', 'utf8');
        var result2 = runFwrite('appendfile "' + target2 + '" "' + contentFile2 + '"');
        assert.strictEqual(result2.exitCode, 0, 'Append to new file should succeed');
        assert.strictEqual(fs.readFileSync(target2, 'utf8'), 'brand new via append');
        reportResult('appendfile creates new file if not exists', true);
    } catch (err) {
        reportResult('appendfile creates new file if not exists', false, err);
    }
    teardown();
}

// Test: Replace operation via subprocess
function testReplaceOperation() {
    console.log('\nIntegration: Replace operations');
    setup();
    try {
        var target = tempFile('replace_test.txt');
        fs.writeFileSync(target, 'Hello world, this is a test file.', 'utf8');
        var oldFile = tempFile('old.tmp');
        var newFile = tempFile('new.tmp');
        fs.writeFileSync(oldFile, 'world', 'utf8');
        fs.writeFileSync(newFile, 'universe', 'utf8');
        var result = runFwrite('replace "' + target + '" "' + oldFile + '" "' + newFile + '"');
        assert.strictEqual(result.exitCode, 0, 'Replace should succeed');
        assert.ok(result.stdout.includes('OK:'), 'Should output OK:');
        var readBack = fs.readFileSync(target, 'utf8');
        assert.strictEqual(readBack, 'Hello universe, this is a test file.');
        reportResult('replace performs single string replacement', true);
    } catch (err) {
        reportResult('replace performs single string replacement', false, err);
    }
    try {
        // Test idempotent replace
        var target2 = tempFile('replace_idem.txt');
        fs.writeFileSync(target2, 'Hello universe already here.', 'utf8');
        var oldFile2 = tempFile('old2.tmp');
        var newFile2 = tempFile('new2.tmp');
        fs.writeFileSync(oldFile2, 'world', 'utf8');
        fs.writeFileSync(newFile2, 'universe', 'utf8');
        var result2 = runFwrite('replace "' + target2 + '" "' + oldFile2 + '" "' + newFile2 + '"');
        assert.strictEqual(result2.exitCode, 0, 'Idempotent replace should succeed');
        assert.ok(result2.stdout.includes('Idempotent'), 'Should report idempotent');
        reportResult('replace reports idempotent when new_text already present', true);
    } catch (err) {
        reportResult('replace reports idempotent when new_text already present', false, err);
    }
    try {
        // Test replace not found
        var target3 = tempFile('replace_nf.txt');
        fs.writeFileSync(target3, 'No match here.', 'utf8');
        var oldFile3 = tempFile('old3.tmp');
        var newFile3 = tempFile('new3.tmp');
        fs.writeFileSync(oldFile3, 'NONEXISTENT', 'utf8');
        fs.writeFileSync(newFile3, 'replacement', 'utf8');
        var result3 = runFwrite('replace "' + target3 + '" "' + oldFile3 + '" "' + newFile3 + '"');
        assert.strictEqual(result3.exitCode, 2, 'Not-found replace should exit with code 2');
        reportResult('replace exits with code 2 when old_text not found', true);
    } catch (err) {
        reportResult('replace exits with code 2 when old_text not found', false, err);
    }
    teardown();
}


// Test: Large file performance
function testLargeFilePerformance() {
    console.log('\nIntegration: Large file performance');
    setup();
    try {
        // 100KB write should complete in < 2 seconds
        var target100k = tempFile('large_100k.txt');
        var contentFile100k = tempFile('large_100k_content.tmp');
        var content100k = 'x'.repeat(100 * 1024);
        fs.writeFileSync(contentFile100k, content100k, 'utf8');
        var start100k = process.hrtime.bigint();
        var result100k = runFwrite('writefile "' + target100k + '" "' + contentFile100k + '"');
        var elapsed100k = Number(process.hrtime.bigint() - start100k) / 1e6;
        assert.strictEqual(result100k.exitCode, 0, '100KB write should succeed');
        assert.ok(elapsed100k < 2000, '100KB write should complete in < 2s, took ' + elapsed100k.toFixed(0) + 'ms');
        assert.strictEqual(fs.readFileSync(target100k, 'utf8').length, content100k.length);
        reportResult('100KB write completes in < 2 seconds (' + elapsed100k.toFixed(0) + 'ms)', true);
    } catch (err) {
        reportResult('100KB write completes in < 2 seconds', false, err);
    }
    try {
        // 500KB write should complete in < 5 seconds
        var target500k = tempFile('large_500k.txt');
        var contentFile500k = tempFile('large_500k_content.tmp');
        var content500k = 'y'.repeat(500 * 1024);
        fs.writeFileSync(contentFile500k, content500k, 'utf8');
        var start500k = process.hrtime.bigint();
        var result500k = runFwrite('writefile "' + target500k + '" "' + contentFile500k + '"');
        var elapsed500k = Number(process.hrtime.bigint() - start500k) / 1e6;
        assert.strictEqual(result500k.exitCode, 0, '500KB write should succeed');
        assert.ok(elapsed500k < 5000, '500KB write should complete in < 5s, took ' + elapsed500k.toFixed(0) + 'ms');
        assert.strictEqual(fs.readFileSync(target500k, 'utf8').length, content500k.length);
        reportResult('500KB write completes in < 5 seconds (' + elapsed500k.toFixed(0) + 'ms)', true);
    } catch (err) {
        reportResult('500KB write completes in < 5 seconds', false, err);
    }
    teardown();
}

// Test: Batch with mixed operations
function testBatchMixedOperations() {
    console.log('\nIntegration: Batch with mixed operations');
    setup();
    try {
        var target1 = tempFile('batch1.txt');
        var target2 = tempFile('batch2.txt');
        var target3 = tempFile('batch3.txt');
        fs.writeFileSync(target1, 'original1', 'utf8');
        fs.writeFileSync(target3, 'Hello world in batch3', 'utf8');
        var cf1 = tempFile('batch_c1.tmp');
        var cf2 = tempFile('batch_c2.tmp');
        var oldF = tempFile('batch_old.tmp');
        var newF = tempFile('batch_new.tmp');
        fs.writeFileSync(cf1, 'written by batch', 'utf8');
        fs.writeFileSync(cf2, ' appended by batch', 'utf8');
        fs.writeFileSync(oldF, 'world', 'utf8');
        fs.writeFileSync(newF, 'universe', 'utf8');
        var manifest = {
            operations: [
                { op: 'write', target: target1, content_file: cf1 },
                { op: 'append', target: target2, content_file: cf2 },
                { op: 'replace', target: target3, old_file: oldF, new_file: newF }
            ]
        };
        var manifestFile = tempFile('manifest.json');
        fs.writeFileSync(manifestFile, JSON.stringify(manifest), 'utf8');
        var result = runFwrite('batch "' + manifestFile + '"');
        assert.strictEqual(result.exitCode, 0, 'Batch should succeed');
        assert.ok(result.stdout.includes('OK:'), 'Should output OK:');
        assert.strictEqual(fs.readFileSync(target1, 'utf8'), 'written by batch');
        assert.strictEqual(fs.readFileSync(target2, 'utf8'), ' appended by batch');
        assert.strictEqual(fs.readFileSync(target3, 'utf8'), 'Hello universe in batch3');
        reportResult('Batch executes mixed write/append/replace operations', true);
    } catch (err) {
        reportResult('Batch executes mixed write/append/replace operations', false, err);
    }
    teardown();
}

// Test: Path handling with forward and backslashes
function testPathHandling() {
    console.log('\nIntegration: Path handling');
    setup();
    try {
        // Test with forward slashes
        var targetFwd = tempFile('pathtest/forward/test.txt').replace(/\\/g, '/');
        var contentFwd = tempFile('path_content_fwd.tmp');
        fs.writeFileSync(contentFwd, 'forward slash path', 'utf8');
        var result1 = runFwrite('writefile "' + targetFwd + '" "' + contentFwd + '"');
        assert.strictEqual(result1.exitCode, 0, 'Forward slash path should work');
        reportResult('Forward slash paths work correctly', true);
    } catch (err) {
        reportResult('Forward slash paths work correctly', false, err);
    }
    try {
        // Test with backslashes
        var targetBack = tempFile('pathtest\\backslash\\test.txt');
        var contentBack = tempFile('path_content_back.tmp');
        fs.writeFileSync(contentBack, 'backslash path', 'utf8');
        var result2 = runFwrite('writefile "' + targetBack + '" "' + contentBack + '"');
        assert.strictEqual(result2.exitCode, 0, 'Backslash path should work');
        reportResult('Backslash paths work correctly', true);
    } catch (err) {
        reportResult('Backslash paths work correctly', false, err);
    }
    teardown();
}

// Main runner
function runIntegrationTests() {
    console.log('fwrite.js Integration Tests');
    console.log('===========================');
    testWriteOperation();
    testAppendOperation();
    testReplaceOperation();
    testLargeFilePerformance();
    testBatchMixedOperations();
    testPathHandling();
    console.log('\n===========================');
    console.log('Results: ' + passCount + ' passed, ' + failCount + ' failed');
    if (failCount > 0) { process.exit(1); }
}

runIntegrationTests();

