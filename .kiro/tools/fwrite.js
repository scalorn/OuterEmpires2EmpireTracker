#!/usr/bin/env node
/**
 * fwrite.js - Reliable file write/append/replace/batch tool for AI agents.
 *
 * Single-file Node.js CLI with zero external dependencies.
 * Provides atomic writes, lock retry, verification, and structured output.
 */

'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');

// ============================================================================
// Section 1: Constants and Configuration
// ============================================================================

/** Exit codes for structured error reporting */
const EXIT_CODES = {
    SUCCESS: 0,
    GENERAL_ERROR: 1,
    REPLACE_NOT_FOUND: 2,
    REPLACE_MULTIPLE: 3,
    FILE_LOCKED: 4,
    STDIN_TIMEOUT: 5,
    VERIFICATION_FAILED: 6,
    BATCH_VALIDATION: 7,
    BATCH_EXECUTION: 8
};

/** Retry configuration for file lock handling */
const RETRY_CONFIG = {
    maxRetries: 3,
    backoffMs: [100, 200, 400],
    retryableCodes: ['EBUSY', 'EPERM', 'EACCES']
};

/** Stdin read timeout in milliseconds */
const STDIN_TIMEOUT_MS = 5000;

/** Filesystem error code to human-readable message mapping */
const FS_ERROR_MAP = {
    ENOENT: 'File or directory does not exist',
    EACCES: 'Permission denied (file may be read-only)',
    EPERM: 'Operation not permitted (may be locked by another process)',
    EBUSY: 'File is busy (locked by another process)',
    ENOSPC: 'Disk is full',
    ENAMETOOLONG: 'File path exceeds OS maximum length',
    EISDIR: 'Expected a file but found a directory'
};

// ============================================================================
// Section 2: Utility Functions
// ============================================================================

/**
 * Strips a leading UTF-8 BOM (U+FEFF) from a string if present.
 * @param {string} str - Input string
 * @returns {string} String without leading BOM
 */
function stripBom(str) {
    if (str.length > 0 && str.charCodeAt(0) === 0xFEFF) {
        return str.slice(1);
    }
    return str;
}

/**
 * Validates UTF-8 encoding by checking for replacement characters (U+FFFD)
 * which indicate invalid byte sequences were encountered during decoding.
 * @param {string} content - Decoded string content
 * @returns {{ valid: boolean, byteOffset: number|null, warning: string|null }}
 */
function validateEncoding(content) {
    const idx = content.indexOf('\uFFFD');
    if (idx === -1) {
        return { valid: true, byteOffset: null, warning: null };
    }
    const byteOffset = Buffer.byteLength(content.slice(0, idx), 'utf8');
    return {
        valid: false,
        byteOffset,
        warning: 'Invalid UTF-8 sequence detected at byte offset ' + byteOffset
    };
}

/**
 * Detects whether a file's content uses CRLF line endings.
 * @param {string} content - File content
 * @returns {boolean} True if content uses CRLF
 */
function detectCRLF(content) {
    return content.includes('\r\n');
}

/**
 * Normalizes line endings to LF for matching purposes.
 * @param {string} content - Input content
 * @returns {string} Content with all CRLF converted to LF
 */
function normalizeToLF(content) {
    return content.replace(/\r\n/g, '\n');
}

/**
 * Restores CRLF line endings in content (converts bare LF to CRLF).
 * Only converts LF that are not already preceded by CR.
 * @param {string} content - Content with LF endings
 * @returns {string} Content with CRLF endings
 */
function restoreCRLF(content) {
    return content.replace(/(?<!\r)\n/g, '\r\n');
}

// ============================================================================
// Section 3: Output Formatter
// ============================================================================

/**
 * Formats a success message with OK: prefix and [verified] indicator.
 * @param {string} message - Human-readable success description
 * @returns {string} Formatted success string
 */
function formatSuccess(message) {
    return 'OK: ' + message + ' [verified]';
}

/**
 * Formats an error message with structured fields.
 * @param {{ operation: string, path: string, reason: string, remediation: string }} error
 * @returns {string} Formatted error string
 */
function formatError(error) {
    var msg = 'ERROR: ' + error.operation + ' ' + error.path + ' - ' + error.reason + '.';
    if (error.remediation) {
        msg += ' Suggested fix: ' + error.remediation;
    }
    return msg;
}

/**
 * Maps a filesystem error to a human-readable message.
 * @param {Error} err - Node.js filesystem error
 * @returns {string} Human-readable error description
 */
function mapFsError(err) {
    if (err.code && FS_ERROR_MAP[err.code]) {
        return FS_ERROR_MAP[err.code];
    }
    return err.message || 'Unknown filesystem error';
}

// ============================================================================
// Section 4: Content Pipeline
// ============================================================================

/**
 * Reads content from a file, strips BOM, validates encoding.
 * Optionally deletes the file after reading.
 * @param {string} filePath - Path to content file
 * @param {boolean} deleteAfter - Whether to delete the file after reading
 * @returns {{ content: string, warnings: string[] }}
 */
function readContentFile(filePath, deleteAfter) {
    var warnings = [];

    var raw;
    try {
        raw = fs.readFileSync(filePath, 'utf8');
    } catch (err) {
        var reason = mapFsError(err);
        var msg = formatError({
            operation: 'read',
            path: filePath,
            reason: 'Content file unreadable: ' + reason,
            remediation: 'Ensure the content file exists and is accessible.'
        });
        console.log(msg);
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }

    var content = stripBom(raw);

    var encoding = validateEncoding(content);
    if (!encoding.valid) {
        warnings.push(encoding.warning);
    }

    if (deleteAfter) {
        try {
            fs.unlinkSync(filePath);
        } catch (e) {
            warnings.push('Could not delete content file: ' + filePath);
        }
    }

    return { content: content, warnings: warnings };
}

/**
 * Reads content from stdin with a timeout.
 * @param {number} [timeoutMs] - Timeout in milliseconds (default: STDIN_TIMEOUT_MS)
 * @returns {Promise<{ content: string, warnings: string[] }>}
 */
function readStdin(timeoutMs) {
    var timeout = timeoutMs || STDIN_TIMEOUT_MS;

    return new Promise(function(resolve, reject) {
        var chunks = [];
        var resolved = false;

        var timer = setTimeout(function() {
            if (!resolved) {
                resolved = true;
                process.stdin.destroy();
                var msg = formatError({
                    operation: 'stdin',
                    path: '-',
                    reason: 'Stdin timed out after ' + timeout + 'ms with no data',
                    remediation: 'Use the temp file approach (writefile/appendfile) for large content.'
                });
                console.log(msg);
                process.exit(EXIT_CODES.STDIN_TIMEOUT);
            }
        }, timeout);

        process.stdin.setEncoding('utf8');
        process.stdin.on('data', function(chunk) {
            chunks.push(chunk);
        });
        process.stdin.on('end', function() {
            if (!resolved) {
                resolved = true;
                clearTimeout(timer);
                var raw = chunks.join('');
                var content = stripBom(raw);
                var warnings = [];
                var encoding = validateEncoding(content);
                if (!encoding.valid) {
                    warnings.push(encoding.warning);
                }
                resolve({ content: content, warnings: warnings });
            }
        });
        process.stdin.on('error', function(err) {
            if (!resolved) {
                resolved = true;
                clearTimeout(timer);
                reject(err);
            }
        });

        process.stdin.resume();
    });
}

// ============================================================================
// Section 5: Atomic Writer with Lock Retry
// ============================================================================

/**
 * Sleeps for the specified number of milliseconds.
 * @param {number} ms - Milliseconds to sleep
 * @returns {Promise<void>}
 */
function sleep(ms) {
    return new Promise(function(resolve) { setTimeout(resolve, ms); });
}

/**
 * Determines if an error is retryable (file lock).
 * @param {Error} err - Filesystem error
 * @returns {boolean}
 */
function isRetryableError(err) {
    return err.code && RETRY_CONFIG.retryableCodes.indexOf(err.code) !== -1;
}

/**
 * Writes content atomically using write-to-temp + rename strategy for existing files.
 * For new files, creates parent dirs and writes directly.
 * Retries on EBUSY/EPERM/EACCES with exponential backoff.
 * @param {string} targetPath - Final destination path
 * @param {string} content - Content to write
 * @returns {Promise<{ bytesWritten: number, retryAttempt: number }>}
 */
async function atomicWrite(targetPath, content) {
    var resolvedPath = path.resolve(targetPath);
    var dir = path.dirname(resolvedPath);
    var tempPath = resolvedPath + '.tmp';

    fs.mkdirSync(dir, { recursive: true });

    var targetExists = false;
    try {
        fs.accessSync(resolvedPath, fs.constants.F_OK);
        targetExists = true;
    } catch (e) {
        targetExists = false;
    }

    var bytesWritten = Buffer.byteLength(content, 'utf8');

    if (targetExists) {
        for (var attempt = 0; attempt <= RETRY_CONFIG.maxRetries; attempt++) {
            try {
                fs.writeFileSync(tempPath, content, 'utf8');
                fs.renameSync(tempPath, resolvedPath);
                return { bytesWritten: bytesWritten, retryAttempt: attempt };
            } catch (err) {
                try { fs.unlinkSync(tempPath); } catch (e) { /* ignore */ }

                if (isRetryableError(err) && attempt < RETRY_CONFIG.maxRetries) {
                    await sleep(RETRY_CONFIG.backoffMs[attempt]);
                    continue;
                }

                if (isRetryableError(err)) {
                    var msg = formatError({
                        operation: 'write',
                        path: targetPath,
                        reason: 'File locked after ' + RETRY_CONFIG.maxRetries + ' retries (' + RETRY_CONFIG.backoffMs.reduce(function(a, b) { return a + b; }, 0) + 'ms total)',
                        remediation: 'Close the application or IDE that has the file open.'
                    });
                    console.log(msg);
                    process.exit(EXIT_CODES.FILE_LOCKED);
                }

                throw err;
            }
        }
    } else {
        for (var attempt2 = 0; attempt2 <= RETRY_CONFIG.maxRetries; attempt2++) {
            try {
                fs.writeFileSync(resolvedPath, content, 'utf8');
                return { bytesWritten: bytesWritten, retryAttempt: attempt2 };
            } catch (err) {
                if (isRetryableError(err) && attempt2 < RETRY_CONFIG.maxRetries) {
                    await sleep(RETRY_CONFIG.backoffMs[attempt2]);
                    continue;
                }

                if (isRetryableError(err)) {
                    var msg2 = formatError({
                        operation: 'write',
                        path: targetPath,
                        reason: 'File locked after ' + RETRY_CONFIG.maxRetries + ' retries (' + RETRY_CONFIG.backoffMs.reduce(function(a, b) { return a + b; }, 0) + 'ms total)',
                        remediation: 'Close the application or IDE that has the file open.'
                    });
                    console.log(msg2);
                    process.exit(EXIT_CODES.FILE_LOCKED);
                }

                throw err;
            }
        }
    }
}

/**
 * Appends content to a file with lock retry.
 * Creates file + parent dirs if target doesn't exist.
 * @param {string} targetPath - File to append to
 * @param {string} content - Content to append
 * @returns {Promise<{ bytesWritten: number, retryAttempt: number }>}
 */
async function appendToFile(targetPath, content) {
    var resolvedPath = path.resolve(targetPath);
    var dir = path.dirname(resolvedPath);

    fs.mkdirSync(dir, { recursive: true });

    var bytesWritten = Buffer.byteLength(content, 'utf8');

    for (var attempt = 0; attempt <= RETRY_CONFIG.maxRetries; attempt++) {
        try {
            fs.appendFileSync(resolvedPath, content, 'utf8');
            return { bytesWritten: bytesWritten, retryAttempt: attempt };
        } catch (err) {
            if (isRetryableError(err) && attempt < RETRY_CONFIG.maxRetries) {
                await sleep(RETRY_CONFIG.backoffMs[attempt]);
                continue;
            }

            if (isRetryableError(err)) {
                var msg = formatError({
                    operation: 'append',
                    path: targetPath,
                    reason: 'File locked after ' + RETRY_CONFIG.maxRetries + ' retries (' + RETRY_CONFIG.backoffMs.reduce(function(a, b) { return a + b; }, 0) + 'ms total)',
                    remediation: 'Close the application or IDE that has the file open.'
                });
                console.log(msg);
                process.exit(EXIT_CODES.FILE_LOCKED);
            }

            throw err;
        }
    }
}

// ============================================================================
// Section 6: Verification Layer
// ============================================================================

/**
 * Verifies a write operation by reading back the file and comparing byte length.
 * @param {string} targetPath - File to verify
 * @param {number} expectedBytes - Expected byte length of the written content
 * @returns {boolean} True if verification passed
 */
function verifyWrite(targetPath, expectedBytes) {
    var resolvedPath = path.resolve(targetPath);

    var stat;
    try {
        stat = fs.statSync(resolvedPath);
    } catch (err) {
        var msg = formatError({
            operation: 'verify',
            path: targetPath,
            reason: 'Cannot read back file for verification: ' + mapFsError(err),
            remediation: 'Check file permissions and disk health.'
        });
        console.log(msg);
        process.exit(EXIT_CODES.VERIFICATION_FAILED);
    }

    var actualBytes = stat.size;

    if (actualBytes !== expectedBytes) {
        var msg2 = formatError({
            operation: 'verify',
            path: targetPath,
            reason: 'Byte length mismatch: expected ' + expectedBytes + ', actual ' + actualBytes,
            remediation: 'Retry the write operation. If persistent, check disk health.'
        });
        console.log(msg2);
        process.exit(EXIT_CODES.VERIFICATION_FAILED);
    }

    return true;
}

/**
 * Verifies an append operation by checking total file byte length.
 * @param {string} targetPath - File to verify
 * @param {number} expectedTotalBytes - Expected total byte length after append
 * @returns {boolean} True if verification passed
 */
function verifyAppend(targetPath, expectedTotalBytes) {
    var resolvedPath = path.resolve(targetPath);

    var stat;
    try {
        stat = fs.statSync(resolvedPath);
    } catch (err) {
        var msg = formatError({
            operation: 'verify',
            path: targetPath,
            reason: 'Cannot read back file for verification: ' + mapFsError(err),
            remediation: 'Check file permissions and disk health.'
        });
        console.log(msg);
        process.exit(EXIT_CODES.VERIFICATION_FAILED);
    }

    var actualBytes = stat.size;

    if (actualBytes !== expectedTotalBytes) {
        var msg2 = formatError({
            operation: 'verify',
            path: targetPath,
            reason: 'Byte length mismatch after append: expected ' + expectedTotalBytes + ', actual ' + actualBytes,
            remediation: 'Retry the append operation. If persistent, check disk health.'
        });
        console.log(msg2);
        process.exit(EXIT_CODES.VERIFICATION_FAILED);
    }

    return true;
}

// ============================================================================
// Section 6b: Replace Engine
// ============================================================================

async function replaceInFile(targetPath, oldText, newText) {
    var resolvedPath = path.resolve(targetPath);
    var rawTarget;
    try {
        rawTarget = fs.readFileSync(resolvedPath, 'utf8');
    } catch (err) {
        return { success: false, exitCode: 1, message: formatError({
            operation: 'replace', path: targetPath,
            reason: 'Cannot read target file: ' + mapFsError(err),
            remediation: 'Ensure the target file exists and is accessible.'
        }) };
    }
    var isCRLF = detectCRLF(rawTarget);
    var normalizedTarget = normalizeToLF(rawTarget);
    var normalizedOld = normalizeToLF(oldText);
    var count = 0;
    var positions = [];
    var searchFrom = 0;
    while (true) {
        var idx = normalizedTarget.indexOf(normalizedOld, searchFrom);
        if (idx === -1) break;
        count++;
        positions.push(idx);
        searchFrom = idx + 1;
    }
    if (count === 1) {
        var before = normalizedTarget.slice(0, positions[0]);
        var after = normalizedTarget.slice(positions[0] + normalizedOld.length);
        var result = before + newText + after;
        if (isCRLF) { result = restoreCRLF(result); }
        var writeResult = await atomicWrite(targetPath, result);
        verifyWrite(targetPath, writeResult.bytesWritten);
        return { success: true, oldLen: oldText.length, newLen: newText.length, idempotent: false };
    } else if (count === 0) {
        var normalizedNew = normalizeToLF(newText);
        if (normalizedNew.length > 0 && normalizedTarget.indexOf(normalizedNew) !== -1) {
            return { success: true, oldLen: 0, newLen: 0, idempotent: true };
        }
        var preview = oldText.slice(0, 80);
        return { success: false, exitCode: 2, message: formatError({
            operation: 'replace', path: targetPath,
            reason: 'Old string not found (' + oldText.length + ' chars, starts with: ' + JSON.stringify(preview) + ')',
            remediation: 'Verify the old text matches the current file content exactly.'
        }) };
    } else {
        var lineNumbers = [];
        for (var i = 0; i < positions.length; i++) {
            var textBefore = normalizedTarget.slice(0, positions[i]);
            var lineNum = (textBefore.match(/\n/g) || []).length + 1;
            lineNumbers.push(lineNum);
        }
        return { success: false, exitCode: 3, message: formatError({
            operation: 'replace', path: targetPath,
            reason: 'Old string found ' + count + ' times (lines ' + lineNumbers.join(', ') + ')',
            remediation: 'Include more surrounding context to make the match unique.'
        }), count: count, lineNumbers: lineNumbers };
    }
}

// ============================================================================
// Section 7b: Backup Manager
// ============================================================================

function createBackup(targetPath) {
    var resolvedPath = path.resolve(targetPath);
    var existed = false;
    try {
        fs.accessSync(resolvedPath, fs.constants.F_OK);
        existed = true;
    } catch (e) {
        existed = false;
    }
    var timestamp = Date.now() + '_' + Math.random().toString(36).slice(2, 8);
    var backupPath = resolvedPath + '.bak.' + timestamp;
    if (existed) {
        fs.copyFileSync(resolvedPath, backupPath);
    }
    return { targetPath: resolvedPath, backupPath: backupPath, existed: existed };
}

function restoreBackups(backups) {
    for (var i = backups.length - 1; i >= 0; i--) {
        var entry = backups[i];
        try {
            if (entry.existed) {
                fs.copyFileSync(entry.backupPath, entry.targetPath);
            } else {
                try { fs.unlinkSync(entry.targetPath); } catch (e) { /* ignore */ }
            }
        } catch (e) {
            // Best effort restore
        }
    }
}

function cleanupBackups(backups) {
    for (var i = 0; i < backups.length; i++) {
        if (backups[i].existed) {
            try { fs.unlinkSync(backups[i].backupPath); } catch (e) { /* ignore */ }
        }
    }
}

// ============================================================================
// Section 7: Command Handlers
// ============================================================================

/**
 * Handles the 'write' command: read content from stdin, overwrite target.
 * @param {string} targetPath - Target file path
 */
async function handleWrite(targetPath) {
    if (!targetPath) {
        console.log(formatError({
            operation: 'write',
            path: '-',
            reason: 'No target file specified',
            remediation: 'Usage: fwrite.js write <target>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }

    var result = await readStdin();
    var content = result.content;

    var writeResult = await atomicWrite(targetPath, content);
    verifyWrite(targetPath, writeResult.bytesWritten);

    var charCount = content.length;
    console.log(formatSuccess('Wrote ' + charCount + ' chars to ' + targetPath));
    process.exit(EXIT_CODES.SUCCESS);
}

/**
 * Handles the 'writefile' command: read content from file, overwrite target.
 * @param {string} targetPath - Target file path
 * @param {string} contentFile - Path to content file
 */
async function handleWriteFile(targetPath, contentFile) {
    if (!targetPath) {
        console.log(formatError({
            operation: 'writefile',
            path: '-',
            reason: 'No target file specified',
            remediation: 'Usage: fwrite.js writefile <target> <content_file>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
    if (!contentFile) {
        console.log(formatError({
            operation: 'writefile',
            path: targetPath,
            reason: 'No content file specified',
            remediation: 'Usage: fwrite.js writefile <target> <content_file>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }

    var result = readContentFile(contentFile, true);
    var content = result.content;

    var writeResult = await atomicWrite(targetPath, content);
    verifyWrite(targetPath, writeResult.bytesWritten);

    var charCount = content.length;
    console.log(formatSuccess('Wrote ' + charCount + ' chars to ' + targetPath));
    process.exit(EXIT_CODES.SUCCESS);
}

/**
 * Handles the 'append' command: read content from stdin, append to target.
 * @param {string} targetPath - Target file path
 */
async function handleAppend(targetPath) {
    if (!targetPath) {
        console.log(formatError({
            operation: 'append',
            path: '-',
            reason: 'No target file specified',
            remediation: 'Usage: fwrite.js append <target>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }

    var result = await readStdin();
    var content = result.content;

    // Get existing file size for verification
    var existingBytes = 0;
    var resolvedPath = path.resolve(targetPath);
    try {
        var stat = fs.statSync(resolvedPath);
        existingBytes = stat.size;
    } catch (e) {
        // File doesn't exist yet, that's fine
        existingBytes = 0;
    }

    var appendResult = await appendToFile(targetPath, content);
    var expectedTotalBytes = existingBytes + appendResult.bytesWritten;
    verifyAppend(targetPath, expectedTotalBytes);

    var charCount = content.length;
    console.log(formatSuccess('Appended ' + charCount + ' chars to ' + targetPath));
    process.exit(EXIT_CODES.SUCCESS);
}

/**
 * Handles the 'appendfile' command: read content from file, append to target.
 * @param {string} targetPath - Target file path
 * @param {string} contentFile - Path to content file
 */
async function handleAppendFile(targetPath, contentFile) {
    if (!targetPath) {
        console.log(formatError({
            operation: 'appendfile',
            path: '-',
            reason: 'No target file specified',
            remediation: 'Usage: fwrite.js appendfile <target> <content_file>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
    if (!contentFile) {
        console.log(formatError({
            operation: 'appendfile',
            path: targetPath,
            reason: 'No content file specified',
            remediation: 'Usage: fwrite.js appendfile <target> <content_file>'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }

    var result = readContentFile(contentFile, true);
    var content = result.content;

    // Get existing file size for verification
    var existingBytes = 0;
    var resolvedPath = path.resolve(targetPath);
    try {
        var stat = fs.statSync(resolvedPath);
        existingBytes = stat.size;
    } catch (e) {
        // File doesn't exist yet, that's fine
        existingBytes = 0;
    }

    var appendResult = await appendToFile(targetPath, content);
    var expectedTotalBytes = existingBytes + appendResult.bytesWritten;
    verifyAppend(targetPath, expectedTotalBytes);

    var charCount = content.length;
    console.log(formatSuccess('Appended ' + charCount + ' chars to ' + targetPath));
    process.exit(EXIT_CODES.SUCCESS);
}


async function handleReplace(targetPath, oldFile, newFile) {
    if (!targetPath) {
        console.log(formatError({ operation: 'replace', path: '-', reason: 'No target file specified', remediation: 'Usage: fwrite.js replace <target> <old_file> <new_file>' }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
    if (!oldFile || !newFile) {
        console.log(formatError({ operation: 'replace', path: targetPath, reason: 'Missing content file arguments', remediation: 'Usage: fwrite.js replace <target> <old_file> <new_file>' }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
    var oldResult = readContentFile(oldFile, false);
    var newResult = readContentFile(newFile, false);
    var oldText = oldResult.content;
    var newText = newResult.content;
    var result = await replaceInFile(targetPath, oldText, newText);
    if (result.success) {
        try { fs.unlinkSync(path.resolve(oldFile)); } catch (e) { /* ignore */ }
        try { fs.unlinkSync(path.resolve(newFile)); } catch (e) { /* ignore */ }
        if (result.idempotent) {
            console.log(formatSuccess('Idempotent match in ' + targetPath));
        } else {
            console.log(formatSuccess('Replaced ' + result.oldLen + ' chars with ' + result.newLen + ' chars in ' + targetPath));
        }
        process.exit(EXIT_CODES.SUCCESS);
    } else {
        console.log(result.message);
        process.exit(result.exitCode);
    }
}

// ============================================================================
// Section 8b: Batch Handler
// ============================================================================

function validateBatch(manifest) {
    var errors = [];
    if (!manifest || !Array.isArray(manifest.operations)) {
        errors.push('Manifest must have an "operations" array');
        return { valid: false, errors: errors };
    }
    var validOps = ['write', 'append', 'replace'];
    for (var i = 0; i < manifest.operations.length; i++) {
        var op = manifest.operations[i];
        var prefix = 'Operation ' + (i + 1) + ': ';
        if (!op.op || validOps.indexOf(op.op) === -1) {
            errors.push(prefix + 'invalid op "' + (op.op || '') + '" (must be write, append, or replace)');
            continue;
        }
        if (!op.target) {
            errors.push(prefix + 'missing "target" field');
        }
        if (op.op === 'write' || op.op === 'append') {
            if (!op.content_file) {
                errors.push(prefix + 'missing "content_file" for ' + op.op + ' operation');
            } else {
                try { fs.accessSync(path.resolve(op.content_file), fs.constants.R_OK); }
                catch (e) { errors.push(prefix + 'content_file "' + op.content_file + '" does not exist or is not readable'); }
            }
        }
        if (op.op === 'replace') {
            if (!op.old_file) {
                errors.push(prefix + 'missing "old_file" for replace operation');
            } else {
                try { fs.accessSync(path.resolve(op.old_file), fs.constants.R_OK); }
                catch (e) { errors.push(prefix + 'old_file "' + op.old_file + '" does not exist or is not readable'); }
            }
            if (!op.new_file) {
                errors.push(prefix + 'missing "new_file" for replace operation');
            } else {
                try { fs.accessSync(path.resolve(op.new_file), fs.constants.R_OK); }
                catch (e) { errors.push(prefix + 'new_file "' + op.new_file + '" does not exist or is not readable'); }
            }
        }
    }
    return { valid: errors.length === 0, errors: errors };
}

async function executeBatch(manifest) {
    var backups = [];
    var completed = 0;
    for (var i = 0; i < manifest.operations.length; i++) {
        var op = manifest.operations[i];
        var backup = createBackup(op.target);
        backups.push(backup);
        try {
            if (op.op === 'write') {
                var contentResult = readContentFile(op.content_file, false);
                await atomicWrite(op.target, contentResult.content);
            } else if (op.op === 'append') {
                var appendContentResult = readContentFile(op.content_file, false);
                await appendToFile(op.target, appendContentResult.content);
            } else if (op.op === 'replace') {
                var oldResult = readContentFile(op.old_file, false);
                var newResult = readContentFile(op.new_file, false);
                var replaceResult = await replaceInFile(op.target, oldResult.content, newResult.content);
                if (!replaceResult.success) {
                    throw new Error(replaceResult.message);
                }
            }
            completed++;
        } catch (err) {
            restoreBackups(backups);
            cleanupBackups(backups);
            return { completed: completed, rolledBack: true, failedOp: i + 1, error: err.message || String(err) };
        }
    }
    cleanupBackups(backups);
    return { completed: completed, rolledBack: false };
}

async function handleBatch(manifestFile) {
    if (!manifestFile) {
        console.log(formatError({ operation: 'batch', path: '-', reason: 'No manifest file specified', remediation: 'Usage: fwrite.js batch <manifest_file>' }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
    var rawManifest;
    try {
        rawManifest = fs.readFileSync(path.resolve(manifestFile), 'utf8');
    } catch (err) {
        console.log(formatError({ operation: 'batch', path: manifestFile, reason: 'Cannot read manifest file: ' + mapFsError(err), remediation: 'Ensure the manifest file exists and is accessible.' }));
        process.exit(EXIT_CODES.BATCH_VALIDATION);
    }
    var manifest;
    try {
        manifest = JSON.parse(stripBom(rawManifest));
    } catch (err) {
        console.log(formatError({ operation: 'batch', path: manifestFile, reason: 'Invalid JSON in manifest: ' + err.message, remediation: 'Fix the JSON syntax in the manifest file.' }));
        process.exit(EXIT_CODES.BATCH_VALIDATION);
    }
    var validation = validateBatch(manifest);
    if (!validation.valid) {
        console.log(formatError({ operation: 'batch', path: manifestFile, reason: 'Validation failed: ' + validation.errors.join('; '), remediation: 'Fix the manifest and ensure all content files exist.' }));
        process.exit(EXIT_CODES.BATCH_VALIDATION);
    }
    var result = await executeBatch(manifest);
    if (result.rolledBack) {
        console.log(formatError({ operation: 'batch', path: manifestFile, reason: 'Operation ' + result.failedOp + ' failed: ' + result.error + '. Rolled back ' + result.completed + ' prior operations.', remediation: 'Fix the failing operation and retry the batch.' }));
        process.exit(EXIT_CODES.BATCH_EXECUTION);
    }
    console.log(formatSuccess('Batch completed: ' + result.completed + ' operations'));
    process.exit(EXIT_CODES.SUCCESS);
}

// ============================================================================
// Section 8: Main Entry Point
// ============================================================================

/**
 * Main entry point. Routes commands to appropriate handlers.
 */
async function main() {
    try {
        var args = process.argv.slice(2);
        var command = args[0];
        var target = args[1];

        if (!command) {
            console.log(formatError({
                operation: 'cli',
                path: '-',
                reason: 'No command specified',
                remediation: 'Usage: fwrite.js <write|writefile|append|appendfile|replace|batch> <target> [args...]'
            }));
            process.exit(EXIT_CODES.GENERAL_ERROR);
        }

        switch (command) {
            case 'write':
                await handleWrite(target);
                break;
            case 'writefile':
                await handleWriteFile(target, args[2]);
                break;
            case 'append':
                await handleAppend(target);
                break;
            case 'appendfile':
                await handleAppendFile(target, args[2]);
                break;
            case 'replace':
                await handleReplace(target, args[2], args[3]);
                break;
            case 'batch':
                await handleBatch(target);
                break;
            default:
                console.log(formatError({
                    operation: command,
                    path: target || '-',
                    reason: 'Unknown command "' + command + '"',
                    remediation: 'Available commands: write, writefile, append, appendfile, replace, batch.'
                }));
                process.exit(EXIT_CODES.GENERAL_ERROR);
        }
    } catch (err) {
        console.log(formatError({
            operation: 'unknown',
            path: '-',
            reason: err.message || 'Unhandled error',
            remediation: 'Report this error to the developer.'
        }));
        process.exit(EXIT_CODES.GENERAL_ERROR);
    }
}

// ============================================================================
// Section 9: Exports (for testing) and CLI execution
// ============================================================================

module.exports = {
    EXIT_CODES: EXIT_CODES,
    RETRY_CONFIG: RETRY_CONFIG,
    STDIN_TIMEOUT_MS: STDIN_TIMEOUT_MS,
    FS_ERROR_MAP: FS_ERROR_MAP,

    stripBom: stripBom,
    validateEncoding: validateEncoding,
    detectCRLF: detectCRLF,
    normalizeToLF: normalizeToLF,
    restoreCRLF: restoreCRLF,

    formatSuccess: formatSuccess,
    formatError: formatError,
    mapFsError: mapFsError,

    readContentFile: readContentFile,
    readStdin: readStdin,

    atomicWrite: atomicWrite,
    appendToFile: appendToFile,
    sleep: sleep,
    isRetryableError: isRetryableError,

    verifyWrite: verifyWrite,
    verifyAppend: verifyAppend,

    handleWrite: handleWrite,
    handleWriteFile: handleWriteFile,
    handleAppend: handleAppend,
    handleAppendFile: handleAppendFile,
    handleReplace: handleReplace,
    handleBatch: handleBatch,
    replaceInFile: replaceInFile,

    createBackup: createBackup,
    restoreBackups: restoreBackups,
    cleanupBackups: cleanupBackups,
    validateBatch: validateBatch,
    executeBatch: executeBatch,

    main: main
};

// Run CLI if invoked directly
if (require.main === module) {
    main();
}