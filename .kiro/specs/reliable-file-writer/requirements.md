# Requirements Document

## Introduction

The Reliable File Writer is a replacement for the current `fwrite.js` tool that addresses all file writing problems encountered during AI-assisted development in a PowerShell-on-Windows environment. The tool must handle arbitrary content (including Unicode, special characters, and large files) reliably, atomically, and with clear error reporting. It eliminates the multi-step ceremony of the current approach by consolidating content delivery and file operations into fewer, more robust commands.

## Glossary

- **Writer**: The reliable-file-writer Node.js tool that performs file operations
- **Caller**: The AI agent or script invoking the Writer via PowerShell
- **Target_File**: The file being written, appended to, or modified by the Writer
- **Content_File**: A temporary file containing the content to be written or used in a replacement operation
- **Old_Text**: The text to be found and replaced in a Target_File during a replace operation
- **New_Text**: The text that replaces Old_Text in a Target_File during a replace operation
- **Atomic_Write**: A write operation that either fully succeeds or leaves the Target_File unchanged
- **Line_Ending_Normalization**: The process of matching replacement text line endings to the Target_File's existing convention (LF or CRLF)
- **Backup_File**: A temporary copy of the Target_File created before modification, used for rollback on failure
- **Lock_Holder**: A process that has an exclusive file handle preventing other processes from writing

## Requirements

### Requirement 1: Write Entire File

**User Story:** As an AI agent, I want to write an entire file from a content source, so that I can create new files or completely rewrite existing ones.

#### Acceptance Criteria

1. WHEN a write operation is requested with a Content_File path, THE Writer SHALL read the Content_File and write its contents to the Target_File
2. WHEN the Target_File's parent directory does not exist, THE Writer SHALL create all necessary parent directories before writing
3. WHEN the write operation completes successfully, THE Writer SHALL delete the Content_File
4. WHEN the write operation completes successfully, THE Writer SHALL output the number of characters written and the Target_File path
5. IF the Content_File does not exist or is unreadable, THEN THE Writer SHALL exit with a non-zero code and report which file could not be read
6. THE Writer SHALL strip a leading UTF-8 BOM (U+FEFF) from Content_File content before writing to the Target_File

### Requirement 2: Append to File

**User Story:** As an AI agent, I want to append content to an existing file, so that I can add new content without overwriting what's already there.

#### Acceptance Criteria

1. WHEN an append operation is requested with a Content_File path, THE Writer SHALL read the Content_File and append its contents to the end of the Target_File
2. WHEN the append operation completes successfully, THE Writer SHALL delete the Content_File
3. WHEN the append operation completes successfully, THE Writer SHALL output the number of characters appended and the Target_File path
4. IF the Target_File does not exist, THEN THE Writer SHALL create it (including parent directories) and write the content as a new file
5. IF the Content_File does not exist or is unreadable, THEN THE Writer SHALL exit with a non-zero code and report which file could not be read
6. THE Writer SHALL strip a leading UTF-8 BOM (U+FEFF) from Content_File content before appending

### Requirement 3: Replace Unique String

**User Story:** As an AI agent, I want to replace a unique string in a file, so that I can make targeted edits without rewriting the entire file.

#### Acceptance Criteria

1. WHEN a replace operation is requested, THE Writer SHALL read Old_Text from the old Content_File and New_Text from the new Content_File
2. THE Writer SHALL normalize line endings in Old_Text to LF before searching the Target_File
3. THE Writer SHALL normalize the Target_File content to LF before searching for Old_Text
4. WHEN Old_Text is found exactly once in the normalized Target_File content, THE Writer SHALL replace it with New_Text
5. WHEN the Target_File originally used CRLF line endings, THE Writer SHALL convert the result back to CRLF before writing
6. WHEN the replacement completes successfully, THE Writer SHALL delete both Content_Files (old and new)
7. WHEN the replacement completes successfully, THE Writer SHALL output the character counts of Old_Text and New_Text and the Target_File path
8. IF Old_Text is not found in the Target_File, THEN THE Writer SHALL check whether New_Text already exists in the Target_File
9. WHEN Old_Text is not found but New_Text already exists in the Target_File, THE Writer SHALL report success with an idempotent-match message and exit with code 0
10. IF Old_Text is not found and New_Text is also not found, THEN THE Writer SHALL exit with a non-zero code and report that the old string was not found, including its character count
11. IF Old_Text is found more than once in the Target_File, THEN THE Writer SHALL exit with a non-zero code and report the number of occurrences found
12. THE Writer SHALL strip a leading UTF-8 BOM (U+FEFF) from both Content_Files before processing

### Requirement 4: Atomic Write Safety

**User Story:** As an AI agent, I want file writes to be atomic, so that a timeout or crash never leaves a file in a corrupted or partial state.

#### Acceptance Criteria

1. WHEN performing a write or replace operation on an existing Target_File, THE Writer SHALL write the new content to a temporary sibling file in the same directory before replacing the Target_File
2. WHEN the temporary sibling file is fully written, THE Writer SHALL rename it over the Target_File in a single filesystem operation
3. IF the rename operation fails, THEN THE Writer SHALL attempt to delete the temporary sibling file and report the failure
4. WHEN performing a write operation on a new file where the Target_File does not yet exist, THE Writer SHALL write directly without the rename strategy
5. IF a write operation is interrupted before the rename completes, THE Target_File SHALL remain in its original unmodified state

### Requirement 5: Content Encoding Handling

**User Story:** As an AI agent, I want the tool to handle any content encoding correctly, so that Unicode characters, special symbols, and multi-byte sequences are never mangled.

#### Acceptance Criteria

1. THE Writer SHALL read all Content_Files as UTF-8
2. THE Writer SHALL write all output files as UTF-8 without BOM
3. THE Writer SHALL strip a leading UTF-8 BOM from Content_File input if present
4. WHEN content contains multi-byte UTF-8 characters such as em-dashes, multiplication signs, box-drawing characters, or emoji, THE Writer SHALL preserve them exactly as provided in the Content_File
5. IF a Content_File contains invalid UTF-8 byte sequences, THEN THE Writer SHALL report a warning identifying the byte offset of the first invalid sequence

### Requirement 6: File Lock Detection and Retry

**User Story:** As an AI agent, I want the tool to handle locked files gracefully, so that writes do not fail silently when another process has the file open.

#### Acceptance Criteria

1. IF a write operation fails due to a file lock with error codes EBUSY, EPERM, or EACCES on Windows, THEN THE Writer SHALL retry the operation up to 3 times with exponential backoff at 100ms, 200ms, and 400ms intervals
2. IF all retry attempts fail, THEN THE Writer SHALL exit with a non-zero code and report that the file is locked, including the Target_File path
3. WHEN a retry succeeds, THE Writer SHALL report success with a note indicating which retry attempt succeeded

### Requirement 7: Multi-File Batch Operations

**User Story:** As an AI agent, I want to perform multiple file operations in a single command, so that related changes either all succeed or all fail together.

#### Acceptance Criteria

1. WHEN a batch operation is requested with a manifest file, THE Writer SHALL read the manifest and execute all operations in sequence
2. THE Writer SHALL validate all operations in the manifest before executing any of them by verifying all Content_Files exist and all Target_Files are writable
3. IF any operation in the batch fails after validation, THEN THE Writer SHALL roll back all previously completed operations in the batch by restoring Backup_Files
4. WHEN all operations in the batch succeed, THE Writer SHALL delete all Backup_Files and report the count of operations completed
5. THE manifest file SHALL support write, append, and replace operations with the same parameters as individual commands
6. IF the manifest file does not exist or contains invalid JSON, THEN THE Writer SHALL exit with a non-zero code and report the parse error

### Requirement 8: Write Verification

**User Story:** As an AI agent, I want the tool to verify that writes actually succeeded, so that I can trust the success report.

#### Acceptance Criteria

1. WHEN a write or replace operation completes, THE Writer SHALL read back the Target_File and verify its byte length matches the expected output length
2. IF the verification read-back does not match the expected length, THEN THE Writer SHALL exit with a non-zero code and report the mismatch including expected versus actual byte counts
3. WHEN verification succeeds, THE Writer SHALL include a verification-passed indicator in the success output

### Requirement 9: Clear Error Reporting

**User Story:** As an AI agent, I want clear, actionable error messages, so that I can diagnose and fix problems without guessing.

#### Acceptance Criteria

1. WHEN an error occurs, THE Writer SHALL output a structured error message containing the operation attempted, the file path involved, the specific error reason, and a suggested remediation
2. WHEN a replace operation fails because Old_Text was not found, THE Writer SHALL output the first 80 characters of Old_Text to aid debugging
3. WHEN a replace operation fails because Old_Text appears multiple times, THE Writer SHALL output the line numbers of all occurrences
4. IF a file system error occurs such as permission denied, disk full, or path too long, THEN THE Writer SHALL map the OS error code to a human-readable explanation
5. THE Writer SHALL use exit code 0 for success and non-zero exit codes for failures
6. THE Writer SHALL prefix all error output with `ERROR:` and all success output with `OK:`

### Requirement 10: Stdin Pipe Mode for Small Content

**User Story:** As an AI agent, I want a stdin pipe mode for small content, so that simple writes do not require creating a temp file first.

#### Acceptance Criteria

1. WHEN a write or append operation is requested without a Content_File argument, THE Writer SHALL read content from stdin
2. THE Writer SHALL set a 5-second timeout on stdin reading to prevent hanging on pipe buffer issues
3. IF the stdin timeout is reached, THEN THE Writer SHALL exit with a non-zero code and report that stdin timed out, suggesting the temp file approach for large content
4. WHEN stdin content is received within the timeout, THE Writer SHALL process it identically to Content_File content including BOM stripping and encoding handling

### Requirement 11: PowerShell Integration

**User Story:** As an AI agent working in PowerShell, I want the tool to work reliably regardless of how PowerShell delivers content, so that encoding and heredoc issues do not cause silent failures.

#### Acceptance Criteria

1. THE Writer SHALL accept Content_Files written by PowerShell Out-File with Encoding utf8 parameter, which adds a BOM, without corruption
2. THE Writer SHALL accept Content_Files written by System.IO.File WriteAllText with no BOM without corruption
3. THE Writer SHALL handle Content_Files with either LF or CRLF line endings regardless of the Target_File line ending convention
4. WHEN the Content_File contains trailing whitespace or newlines added by PowerShell heredoc syntax, THE Writer SHALL preserve them exactly as provided without implicit trimming
5. THE Writer SHALL function correctly when invoked via node on Windows with paths using either forward slashes or backslashes

### Requirement 12: Performance for Large Content

**User Story:** As an AI agent, I want the tool to handle large files without hanging or timing out, so that writing 100KB or larger files is as reliable as writing small ones.

#### Acceptance Criteria

1. THE Writer SHALL complete a 100KB write operation within 2 seconds on local disk
2. THE Writer SHALL complete a 500KB write operation within 5 seconds on local disk
3. THE Writer SHALL not buffer entire file content in memory more than twice, once for read and once for write
4. WHEN processing a replace operation on a large file, THE Writer SHALL use string operations rather than line-by-line processing to avoid quadratic time complexity
