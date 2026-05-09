# Implementation Plan: Reliable File Writer

## Overview

Replace the existing `.kiro/tools/fwrite.js` with a comprehensive, atomic file writer that handles write, append, replace, and batch operations with lock retry, verification, and structured output. The implementation is a single Node.js file with zero external dependencies (only `fs`, `path`, `os`). Tests use `fast-check` for property-based testing.

## Tasks

- [x] 1. Implement core infrastructure and utilities
  - [x] 1.1 Create the new fwrite.js with constants, exit codes, and retry configuration
    - Define all exit codes (0-8), RETRY_CONFIG (maxRetries: 3, backoffMs: [100, 200, 400], retryableCodes), STDIN_TIMEOUT_MS (5000)
    - Implement utility functions: BOM stripping (strip leading U+FEFF), encoding validation (detect invalid UTF-8 sequences and report byte offset), line ending detection (detect CRLF vs LF), line ending normalization (convert to LF for matching, restore original)
    - Implement the Output Formatter: formatSuccess (OK: prefix with [verified] indicator) and formatError (ERROR: prefix with operation, path, reason, remediation)
    - Implement filesystem error mapping (ENOENT, EACCES, EPERM, EBUSY, ENOSPC, ENAMETOOLONG, EISDIR to human-readable messages)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 9.1, 9.4, 9.5, 9.6_

  - [x] 1.2 Implement the Content Pipeline (readContentFile and readStdin)
    - readContentFile: read file as UTF-8, strip BOM, validate encoding, optionally delete after read
    - readStdin: read from stdin with 5-second timeout, strip BOM, validate encoding
    - Return { content, warnings } from both functions
    - Handle missing/unreadable content files with structured error (exit code 1)
    - _Requirements: 1.1, 1.5, 2.5, 10.1, 10.2, 10.3, 10.4, 11.1, 11.2, 11.3, 11.4_

  - [x] 1.3 Implement the Atomic Writer with lock retry
    - atomicWrite: for existing files, write to temp sibling (.tmp suffix) then rename over target; for new files, create parent dirs and write directly
    - appendToFile: append content with lock retry; create file + parent dirs if target doesn't exist
    - Lock retry layer: catch EBUSY/EPERM/EACCES, retry up to 3 times with exponential backoff (100ms, 200ms, 400ms)
    - Temp file cleanup on failure (delete .tmp sibling if rename fails)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3_

  - [x] 1.4 Implement the Verification Layer
    - After write/replace, read back the target file and compare byte length to expected output length
    - On mismatch: exit with code 6, report expected vs actual byte counts
    - On success: include [verified] indicator in output
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 2. Implement write and append command handlers
  - [x] 2.1 Implement the write/writefile command handlers
    - `write <target>`: read content from stdin, overwrite target
    - `writefile <target> <content_file>`: read content from file, overwrite target
    - Create parent directories if they don't exist
    - Use atomic write strategy for existing files, direct write for new files
    - Delete content file on success
    - Output: OK: Wrote N chars to <path> [verified]
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 10.1_

  - [x] 2.2 Write property test for write round-trip (Property 1)
    - **Property 1: Write round-trip preserves content**
    - For any valid UTF-8 string, writefile then readback produces identical content (after BOM strip)
    - Use fast-check arbitrary strings including multi-byte characters, empty strings, embedded newlines
    - **Validates: Requirements 1.1, 5.1, 5.2, 5.4**

  - [x] 2.3 Implement the append/appendfile command handlers
    - `append <target>`: read content from stdin, append to target
    - `appendfile <target> <content_file>`: read content from file, append to target
    - Create file + parent dirs if target doesn't exist
    - Delete content file on success
    - Output: OK: Appended N chars to <path> [verified]
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 10.1_

  - [x] 2.4 Write property test for append preserves existing content (Property 2)
    - **Property 2: Append preserves existing content**
    - For any existing file content and any append content, file contains original + appended with no modification
    - **Validates: Requirements 2.1, 2.3**

- [x] 3. Implement replace command handler
  - [x] 3.1 Implement the replace command handler
    - `replace <target> <old_file> <new_file>`: read old_text and new_text from content files
    - Strip BOM from both content files
    - Normalize target file and old_text to LF for matching
    - If old_text found exactly once: replace with new_text, restore original line endings (CRLF if target was CRLF), atomic write result
    - If old_text not found: check if new_text exists (idempotent match, exit 0), otherwise exit 2 with first 80 chars preview
    - If old_text found multiple times: exit 3 with occurrence count and line numbers
    - Delete both content files on success
    - Output: OK: Replaced N chars with M chars in <path> [verified]
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12_

  - [x] 3.2 Write property test for replace idempotency (Property 3)
    - **Property 3: Replace is idempotent when new_text already present**
    - For any file containing new_text but not old_text, replace succeeds with exit 0 and file unchanged
    - **Validates: Requirements 3.8, 3.9**

  - [x] 3.3 Write property test for replace uniqueness enforcement (Property 4)
    - **Property 4: Replace uniqueness enforcement**
    - For any file where old_text appears N times (N != 1), replace fails without modifying file
    - **Validates: Requirements 3.4, 3.10, 3.11**

  - [x] 3.4 Write property test for line ending normalization round-trip (Property 5)
    - **Property 5: Line ending normalization round-trip**
    - For any CRLF file with old_text appearing once, after replacement file still has consistent CRLF
    - **Validates: Requirements 3.2, 3.3, 3.5**

- [x] 4. Checkpoint - Verify core operations
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement batch operations
  - [x] 5.1 Implement the Backup Manager
    - createBackup: copy target file to target.bak.{timestamp}, track in BackupEntry array
    - restoreBackups: restore all backed-up files from BackupEntry array (reverse order)
    - cleanupBackups: delete all backup files after successful batch
    - Handle case where target didn't exist before operation (delete on rollback)
    - _Requirements: 7.3, 7.4_

  - [x] 5.2 Implement the batch command handler
    - `batch <manifest_file>`: read and parse JSON manifest
    - validateBatch: verify all content files exist, target files writable, manifest well-formed before executing any operations
    - executeBatch: create backups before each operation, execute in sequence, rollback all on any failure
    - On success: delete all backups, report count of operations completed
    - On validation failure: exit 7 with parse error details
    - On execution failure: exit 8, rollback attempted, report which operation failed
    - Manifest format: { "operations": [{ "op": "write"|"append"|"replace", "target": "...", "content_file": "...", "old_file": "...", "new_file": "..." }] }
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x] 5.3 Write property test for batch atomicity (Property 8)
    - **Property 8: Batch atomicity**
    - For any batch where operation K fails, operations 1..K-1 are rolled back and files match pre-batch state
    - **Validates: Requirements 7.3, 7.4**

- [x] 6. Implement CLI parser and main entry point
  - [x] 6.1 Implement the CLI Parser and command routing
    - Parse command-line arguments: command, target, content files
    - Route to appropriate handler: write, writefile, append, appendfile, replace, batch
    - Validate required arguments per command
    - Handle unknown commands with structured error
    - Normalize file paths (support forward and backslashes on Windows)
    - _Requirements: 11.5, 9.5_

  - [x] 6.2 Wire all components together in main entry point
    - Top-level error boundary (catch unhandled errors, format as ERROR:)
    - Process exit code management
    - Ensure content files are NOT deleted on error (caller can retry)
    - _Requirements: 9.1, 9.5, 9.6_

- [x] 7. Checkpoint - Full integration verification
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Write remaining tests
  - [x] 8.1 Write property test for BOM stripping universality (Property 7)
    - **Property 7: BOM stripping is universal**
    - For any content file starting with UTF-8 BOM (EF BB BF), written output never contains BOM regardless of operation type
    - **Validates: Requirements 1.6, 2.6, 3.12, 5.3**

  - [x] 8.2 Write unit tests for atomic write crash safety (Property 6)
    - **Property 6: Atomic write crash safety**
    - Use mock fs to simulate crash after temp write but before rename; verify original file unchanged
    - **Validates: Requirements 4.1, 4.2, 4.5**

  - [x] 8.3 Write unit tests for verification mismatch detection (Property 9)
    - **Property 9: Verification detects corruption**
    - Use mock fs where read-back returns different byte length; verify exit code 6 and mismatch report
    - **Validates: Requirements 8.1, 8.2**

  - [x] 8.4 Write unit tests for lock retry behavior (Property 10)
    - **Property 10: Lock retry convergence**
    - Use mock fs with timed unlock; verify retry succeeds and reports which attempt
    - **Validates: Requirements 6.1, 6.3**

  - [x] 8.5 Write unit tests for stdin timeout, error formatting, and CLI parsing
    - Test stdin timeout (mock stdin with no data, verify exit code 5 after 5s)
    - Test error message formatting (verify structured output format with all fields)
    - Test CLI argument parsing (verify command routing for all commands)
    - _Requirements: 10.2, 10.3, 9.1, 9.2, 9.3_

  - [x] 8.6 Write integration tests for end-to-end operations
    - Test PowerShell invocation with real files (write, append, replace)
    - Test large file performance (100KB < 2s, 500KB < 5s)
    - Test real file lock scenarios
    - Test batch with mixed operations on real filesystem
    - Test path handling with forward and backslashes
    - _Requirements: 11.1, 11.2, 11.3, 11.5, 12.1, 12.2, 12.3, 12.4_

- [-] 9. Final checkpoint - Complete test suite verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The tool is a single file (.kiro/tools/fwrite.js) - all tasks modify this one file
- Tests go in .kiro/tools/fwrite.test.js (property + unit) and .kiro/tools/fwrite.integration.js (integration)
- fast-check is the property-based testing library (already in package.json or to be added)
- During implementation, the existing fwrite.js will be replaced incrementally
