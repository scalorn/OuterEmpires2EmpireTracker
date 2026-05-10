# Implementation Plan

- [-] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Fulfilled Checkbox Not Persisted
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate `UpdateCommodityRequest` does not persist the `Fulfilled` property
  - **Scoped PBT Approach**: Scope the property to the concrete failing case: call `UpdateCommodityRequest` with `fulfilled = true` on a commodity whose `Fulfilled` is initially `false`, then assert `existing.Fulfilled == true`
  - Test that `UpdateCommodityRequest` sets `existing.Fulfilled` to the passed value (from Bug Condition in design: `isBugCondition(input)` where `input.ColumnIndex == 2` and `UpdateCommodityRequest` does NOT set `existing.Fulfilled`)
  - Generate random `(requested, delivered, needBy, fulfilled)` tuples and assert `commodity.Fulfilled == fulfilled` after the call
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (compilation error — `fulfilled` parameter does not exist on the method signature, confirming the bug)
  - Document counterexamples found: method signature lacks `fulfilled` parameter entirely
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 1.1, 1.2, 2.1, 2.2_

- [~] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Non-Fulfilled Column Edits Unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - Observe behavior on UNFIXED code: editing Amount (column 1) or NeedBy (column 3) calls `UpdateCommodityRequest` and does not modify `Fulfilled`
  - Observe: `UpdateCommodityRequest(colonyUUID, name, 10, 5, needBy)` on a commodity with `Fulfilled = true` → `Fulfilled` remains `true` (preserved by virtue of not being touched)
  - Observe: `UpdateCommodityRequest(colonyUUID, name, 20, 0, needBy)` on a commodity with `Fulfilled = false` → `Fulfilled` remains `false`
  - Write property-based test: for all `(requested, delivered, needBy)` values on a commodity with any existing `Fulfilled` state, calling `UpdateCommodityRequest` with the existing `Fulfilled` value preserves it unchanged
  - This test validates that passing `request.Fulfilled` (the existing value) from Amount/NeedBy handlers does not alter the fulfilled state
  - Verify test passes on UNFIXED code (once the `fulfilled` parameter is added but before the bug-condition logic changes)
  - **NOTE**: Since the unfixed code lacks the `fulfilled` parameter, this test will be written against the NEW signature but verified to pass after the parameter is added (preservation tests validate the fix doesn't regress non-buggy paths)
  - **EXPECTED OUTCOME**: Tests PASS (confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing
  - _Requirements: 3.1, 3.2, 3.3_

- [ ] 3. Fix for Fulfilled checkbox not persisting user value

  - [-] 3.1 Implement the fix
    - Add `bool fulfilled` parameter to `ColonyService.UpdateCommodityRequest` after `DateTime needBy`
    - Set `existing.Fulfilled = fulfilled;` inside the `if (existing != null)` block after setting `existing.NeedBy`
    - Update log statement to include `fulfilled` value for traceability
    - Update Column 2 (Fulfilled) handler in `DgvCommodityRequests_CellValueChanged` to pass `fulfilled` as the last argument
    - Update Column 1 (Amount) handler to pass `request.Fulfilled` as the last argument (preserves existing value)
    - Update Column 3 (NeedBy) handler to pass `request.Fulfilled` as the last argument (preserves existing value)
    - _Bug_Condition: isBugCondition(input) where input.ColumnIndex == 2 AND UpdateCommodityRequest does NOT set existing.Fulfilled_
    - _Expected_Behavior: After UpdateCommodityRequest, commodity.Fulfilled == fulfilled parameter value_
    - _Preservation: Amount/NeedBy edits pass existing Fulfilled value through unchanged; DeliveryFulfillment.FulfillCommodity continues to work independently_
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 3.1, 3.2, 3.3_

  - [~] 3.2 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Fulfilled Checkbox Persists User Value
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior: `UpdateCommodityRequest` sets `existing.Fulfilled` to the passed value
    - When this test passes, it confirms the expected behavior is satisfied
    - Run bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - _Requirements: 2.1, 2.2_

  - [~] 3.3 Verify preservation tests still pass
    - **Property 2: Preservation** - Non-Fulfilled Column Edits Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Confirm Amount/NeedBy edits still preserve existing Fulfilled state after fix
    - _Requirements: 3.1, 3.2_

- [~] 4. Checkpoint - Ensure all tests pass
  - Build solution with zero errors and zero warnings (including StyleCop SA* warnings)
  - Run full test suite via vstest.console and verify all tests pass
  - Run `node .kiro/tools/audit.js` and verify no new findings
  - Commit all changes with detailed commit message
  - Run backup script `D:\projects\OuterEmpires2\OE2EmpireTracker\oebackup.ps1`
