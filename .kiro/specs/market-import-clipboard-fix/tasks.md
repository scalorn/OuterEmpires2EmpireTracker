# Implementation Plan: Market Import Clipboard Fix

## Tasks

- [x] 1. Fix `ExtractHtmlFragmentFromClipboardData` to use marker-based extraction
  - In `OE2EmpireTracker/Forms/Blueprint/BlueprintScanner.cs`
  - Search for `<!--StartFragment-->` and `<!--EndFragment-->` markers
  - Extract substring between markers
  - Fall back to existing byte-offset logic if markers not found
  - _Bugfix: 1.1, 2.1_

- [x] 2. Update diagnostic test to verify the fix
  - Update `BughuntDiagnosticTest.Diagnostic_BughuntHtml_ExtractFragment_ThenParse` to assert > 0 results
  - Assert known blueprint names present in results
  - _Bugfix: 2.1, 2.2_

- [x] 3. Add marker extraction unit tests
  - Test: clipboard with markers extracts correctly
  - Test: clipboard without markers falls back to byte offsets
  - Test: clipboard with no markers and no header returns error
  - _Bugfix: 3.3_

- [x] 4. Verify all existing tests pass
  - Build solution, run all tests via vstest.console
  - _Bugfix: 3.1, 3.2_
