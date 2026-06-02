# Implementation Plan: Centralize Banking Sync

## Overview

Integrates banking data synchronization into `GameApiSyncScheduler.SyncCharacterAsync` as a new step after asset sync, then removes the manual import buttons from `FormBanking`. Uses the existing virtual-override testability pattern.

## Tasks

- [x] 1. Add SyncBankingAsync and virtual methods to GameApiSyncScheduler
  - [x] 1.1 Add virtual methods (RaiseBankingDataChanged, GetPlayerContext, SetBankingBalance) and private SyncBankingAsync method
    - Add `internal virtual void RaiseBankingDataChanged()` no-op override point
    - Add `internal virtual PlayerContext GetPlayerContext()` no-op override point
    - Add `internal virtual void SetBankingBalance(decimal balance)` no-op override point
    - Add `private async Task SyncBankingAsync(string playerUUID, string accessToken)` with transaction import, warning log on failure, balance import, persist and raise on non-null
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.3_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_
    - _Verification: getDiagnostics clean compile_

- [x] 2. Wire SyncBankingAsync into SyncCharacterAsync
  - [x] 2.1 Add try/catch call to SyncBankingAsync after asset sync block in SyncCharacterAsync
    - Insert `try { await SyncBankingAsync(...) } catch { Log.Error(...) }` after the existing asset sync try/catch
    - Uses `tokenResult.Token.AccessToken` and `playerUUID` already in scope
    - _Requirements: 2.1, 2.2, 4.2_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_
    - _Verification: getDiagnostics clean compile_

- [~] 3. Checkpoint - Verify scheduler changes compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Remove import buttons from FormBanking UI
  - [x] 4.1 Remove btnImportTransactions and btnImportBalance from FormBanking.Designer.cs
    - Remove field declarations for `btnImportTransactions` and `btnImportBalance`
    - Remove control instantiation lines in InitializeComponent
    - Remove property assignment blocks (Location, Name, Size, Text, UseVisualStyleBackColor, Anchor)
    - Remove `Controls.Add` lines for both buttons
    - _Requirements: 3.1, 3.2_
    - _Files: OE2EmpireTracker/Forms/Banking/FormBanking.Designer.cs_
    - _Verification: getDiagnostics clean compile_

  - [x] 4.2 Remove click handler wiring and event handler methods from FormBanking.cs
    - Remove `btnImportTransactions.Click += BtnImportTransactions_Click;` from constructor
    - Remove `btnImportBalance.Click += BtnImportBalance_Click;` from constructor
    - Remove `BtnImportTransactions_Click` method entirely
    - Remove `BtnImportBalance_Click` method entirely
    - Retain `btnAddTransaction.Click += BtnAddTransaction_Click;` and `playerContext.BankingDataChanged += OnBankingDataChanged;`
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
    - _Files: OE2EmpireTracker/Forms/Banking/FormBanking.cs_
    - _Verification: getDiagnostics clean compile_

- [~] 5. Checkpoint - Verify full solution builds cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Write scheduler banking sync tests
  - [-] 6.1 Write property test: Banking Exception Fault Isolation (Property 2)
    - **Property 2: Banking Exception Fault Isolation**
    - **Validates: Requirements 2.1, 2.2**
    - Create test class in OE2EmpireTracker.Tests/Services/ that subclasses GameApiSyncScheduler
    - Override SyncBankingAsync-related virtuals to throw, verify SyncCharacterAsync still returns true
    - _Files: OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerBankingTests.cs_

  - [-] 6.2 Write property test: Balance Persistence Round-Trip (Property 1)
    - **Property 1: Balance Persistence Round-Trip**
    - **Validates: Requirements 1.4**
    - For any non-null decimal from ImportBalanceAsync, verify SetBankingBalance called with that value, WriteContext called once, RaiseBankingDataChanged raised
    - _Files: OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerBankingTests.cs_

  - [-] 6.3 Write unit test: banking sync called after asset sync in SyncCharacterAsync
    - Verify call ordering: profile → colony → asset → banking
    - Use subclass that records method call order
    - _Requirements: 4.2_
    - _Files: OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerBankingTests.cs_

- [ ] 7. Write FormBanking removal verification tests
  - [-] 7.1 Write unit test: FormBanking has no Import buttons and still subscribes to BankingDataChanged
    - Instantiate FormBanking and verify btnImportTransactions and btnImportBalance do not exist in Controls collection
    - Verify BankingDataChanged subscription is active (fire event, confirm refresh)
    - _Requirements: 3.1, 3.2, 3.4_
    - _Files: OE2EmpireTracker.Tests/Forms/FormBankingTests.cs_

- [~] 8. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The design uses the existing virtual-override pattern (same as colony/asset sync) so no new interfaces are needed
- FormBanking.Designer.cs changes are safe to hand-edit since we are only removing controls (no repositioning)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1"] },
    { "id": 2, "tasks": ["4.1", "4.2"] },
    { "id": 3, "tasks": ["6.1", "6.2", "6.3", "7.1"] }
  ]
}
```
