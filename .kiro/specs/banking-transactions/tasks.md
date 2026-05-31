# Implementation Plan: Banking Transactions

## Overview

Implements local persistence, API import, and UI display of a player's in-game bank transaction history. The implementation follows the established architecture: POCO model in OE2EmpireTracker.Common/Models, static service class in OE2EmpireTracker.Common/Services, PlayerContext integration for persistence, and a WinForms MDI child form with DataGridView for display.

## Tasks

- [x] 1. Create BankingTransaction data model
  - [x] 1.1 Create BankingTransaction POCO class
    - Create `OE2EmpireTracker.Common/Models/BankingTransaction.cs`
    - Fields: UUID, OwnerUUID, TransactionDateTime, CreditChange, OldBalance, NewBalance, TransactionType, Detail, CharacterId, SystemObjectId, SystemId, IsManualEntry
    - Use decimal for CreditChange/OldBalance/NewBalance with 0m defaults
    - Use nullable int for CharacterId/SystemObjectId/SystemId
    - Use JsonProperty attributes for serialization
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 1.2 Write unit tests for BankingTransaction model
    - Create `OE2EmpireTracker.Tests/Models/BankingTransactionTests.cs`
    - Test default values (decimal 0m, nullable ints null, strings empty)
    - Test JSON serialization round-trip with Newtonsoft.Json
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] 2. Create BankingTransactionTypes constants and DTOs
  - [x] 2.1 Create BankingTransactionTypes constant class
    - Create `OE2EmpireTracker.Common/Constants/BankingTransactionTypes.cs`
    - Static dictionary mapping int codes to human-readable labels
    - GetLabel method handling unknown codes (fallback to Detail + code)
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 2.2 Create BankingImportResult and BankingSummary DTOs
    - Create `OE2EmpireTracker.Common/Services/BankingImportResult.cs`
    - Create `OE2EmpireTracker.Common/Services/BankingSummary.cs`
    - BankingImportResult: TransactionsImported, DuplicatesSkipped, PagesCompleted, Success, ErrorMessage, FailedAtPage
    - BankingSummary: TotalIncome, TotalExpenses, NetChange
    - _Requirements: 3.4, 3.7, 6.4, 6.5, 6.6_

  - [x] 2.3 Write unit tests for BankingTransactionTypes
    - Create `OE2EmpireTracker.Tests/Constants/BankingTransactionTypesTests.cs`
    - Test known code lookup returns correct label
    - Test unknown code with Detail returns "{Detail} ({code})"
    - Test unknown code with null/empty Detail returns "Unknown ({code})"
    - _Requirements: 7.1, 7.2, 7.3_


- [x] 3. Integrate banking transactions into PlayerContext
  - [x] 3.1 Add BankingTransaction persistence to PlayerRoot
    - Modify `OE2EmpireTracker.Common/Services/PlayerRoot.cs`
    - Add `BankingTransaction[]` array property with JsonProperty("bankingTransaction")
    - Add `decimal BankingBalance` property with JsonProperty("bankingBalance")
    - _Requirements: 2.1, 4.4_

  - [x] 3.2 Add banking fields, properties, and methods to PlayerContext
    - Modify `OE2EmpireTracker.Common/Services/PlayerContext.cs`
    - Add _bankingTransactionList, _bankingTransactionCache fields
    - Add BankingTransactionList (IReadOnlyList) and BankingBalance properties
    - Add BankingDataChanged event and OnBankingDataChanged method
    - Add InitBankingTransactions (deduplicate by UUID, log errors for duplicates)
    - Add AddBankingTransaction (throw InvalidOperationException on duplicate UUID)
    - Add RemoveBankingTransaction
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 4.4_

  - [x] 3.3 Write unit tests for PlayerContext banking methods
    - Create `OE2EmpireTracker.Tests/Services/PlayerContextBankingTests.cs`
    - Test InitBankingTransactions deduplicates by UUID
    - Test AddBankingTransaction with unique UUID succeeds
    - Test AddBankingTransaction with duplicate UUID throws InvalidOperationException
    - Test BankingDataChanged event fires on add
    - _Requirements: 2.2, 2.3, 2.4, 2.5_

- [x] 4. Checkpoint - Model and persistence layer
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement BankingService core methods
  - [x] 5.1 Create BankingService with ComputeSummary and FilterTransactions
    - Create `OE2EmpireTracker.Common/Services/BankingService.cs`
    - ComputeSummary: sum positive CreditChange → TotalIncome, sum |negative| → TotalExpenses, Net = Income - Expenses
    - FilterTransactions: filter by transactionType, fromDate, toDate with AND logic, sort descending by TransactionDateTime
    - _Requirements: 5.7, 5.9, 6.4, 6.5, 6.6, 6.7_

  - [x] 5.2 Write property test for summary arithmetic consistency
    - Create `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 2: Summary Arithmetic Consistency**
    - For any list of transactions: Sum(positive CreditChange) - Sum(|negative CreditChange|) = NetChange
    - Use FsCheck 2.16.6 Gen.Choose + Arb.From for decimal generators
    - **Validates: Requirements 6.4, 6.5, 6.6**

  - [x] 5.3 Write property test for filter completeness
    - Append to `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 3: Filter Completeness**
    - Every transaction in filtered result satisfies ALL active filter conditions
    - No transaction outside filter criteria appears in result
    - **Validates: Requirements 5.7, 5.9**

  - [x] 5.4 Write unit tests for ComputeSummary and FilterTransactions
    - Create `OE2EmpireTracker.Tests/Services/BankingServiceSummaryTests.cs`
    - Create `OE2EmpireTracker.Tests/Services/BankingServiceFilterTests.cs`
    - Test empty set returns all zeros
    - Test single positive/negative transaction
    - Test filter by type, by date range, combined
    - _Requirements: 5.7, 5.9, 6.4, 6.5, 6.6, 6.7_


- [x] 6. Implement BankingService import methods
  - [x] 6.1 Implement ImportTransactionsAsync
    - Add to `OE2EmpireTracker.Common/Services/BankingService.cs`
    - Build HashSet of existing composite keys (TransactionDateTime|CreditChange|Detail)
    - Paginate through API (page size 100) until fewer records returned
    - Skip duplicates, generate UUID for new transactions, add to PlayerContext
    - On API error: log, set Success=false, retain prior pages, report FailedAtPage
    - Call WriteContext and fire BankingDataChanged after all pages
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

  - [x] 6.2 Implement ImportBalanceAsync and CreateManualTransaction
    - Add to `OE2EmpireTracker.Common/Services/BankingService.cs`
    - ImportBalanceAsync: call GetBankingBalanceAsync, parse balance, return decimal or null on failure
    - CreateManualTransaction: validate creditChange != 0, create BankingTransaction with IsManualEntry=true, generated UUID
    - _Requirements: 4.1, 4.2, 4.3, 8.3_

  - [x] 6.3 Write property test for import deduplication idempotency
    - Append to `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 1: Import Deduplication Idempotency**
    - Importing same transactions twice results in zero new records on second import
    - Composite key (TransactionDateTime + CreditChange + Detail) prevents duplicates
    - **Validates: Requirements 3.3**

  - [x] 6.4 Write unit tests for ImportTransactionsAsync
    - Create `OE2EmpireTracker.Tests/Services/BankingServiceImportTests.cs`
    - Test successful multi-page import
    - Test deduplication skips existing transactions
    - Test partial failure retains prior pages
    - Test API error sets Success=false with FailedAtPage
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.7_

  - [x] 6.5 Write property test for UUID uniqueness
    - Append to `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 6: UUID Uniqueness**
    - No two BankingTransaction records share the same UUID
    - Adding duplicate UUID throws InvalidOperationException
    - **Validates: Requirements 2.3, 2.4**

  - [x] 6.6 Write property test for manual entry distinguishability
    - Append to `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 5: Manual Entry Distinguishability**
    - Every manually entered transaction has IsManualEntry=true
    - Every API-imported transaction has IsManualEntry=false
    - **Validates: Requirements 8.3**

- [x] 7. Checkpoint - Service layer complete
  - Ensure all tests pass, ask the user if questions arise.


- [x] 8. Add sorting support for banking transactions
  - [x] 8.1 Add OrderBankingTransactions to CollectionSortHelper
    - Modify `OE2EmpireTracker.Common/Services/CollectionSortHelper.cs`
    - Sort by TransactionDateTime descending (newest first)
    - Handle null/missing TransactionDateTime as epoch (1970-01-01) for sort ordering
    - _Requirements: 5.4, 9.3_

  - [x] 8.2 Add banking transaction sorting to SerializationSorter
    - Modify `OE2EmpireTracker.Common/Services/SerializationSorter.cs`
    - Add banking transaction array sorting in SortPlayerRoot method (by UUID for deterministic JSON)
    - _Requirements: 2.1_

- [x] 9. Create FormBanking MDI child form
  - [x] 9.1 Create FormBanking form shell with balance display and grid
    - Create `OE2EmpireTracker/Forms/Banking/FormBanking.cs`
    - Create `OE2EmpireTracker/Forms/Banking/FormBanking.Designer.cs`
    - Create `OE2EmpireTracker/Forms/Banking/FormBanking.resx`
    - Implement IProgrammaticUpdateSource, NLog Logger
    - Subscribe to CurrentPlayerChanged and BankingDataChanged
    - Add lblBalance, dgvTransactions with columns (Date, Type, Detail, Credit Change, Old Balance, New Balance)
    - Display transactions sorted by TransactionDateTime descending
    - Handle null TransactionDateTime displaying "(no date)"
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.8, 9.1, 9.4_

  - [x] 9.2 Add filter controls to FormBanking
    - Modify `OE2EmpireTracker/Forms/Banking/FormBanking.cs` and Designer
    - Add period filter buttons: btn24h, btn7d, btn30d, btnAllTime (default selected)
    - Add cboType dropdown (All + known types alphabetically)
    - Add dtpFrom/dtpTo DateTimePickers with checkboxes (unchecked by default)
    - Wire filter change events to refresh grid with AND logic
    - _Requirements: 5.5, 5.6, 5.7, 5.9, 6.2, 6.3, 7.4, 7.5_

  - [x] 9.3 Add summary panel to FormBanking
    - Modify `OE2EmpireTracker/Forms/Banking/FormBanking.cs` and Designer
    - Add lblIncome, lblExpenses, lblNet labels in summary panel
    - Compute and display summary using BankingService.ComputeSummary on filtered set
    - Display 0.00 when filtered set is empty
    - Format all values to 2 decimal places
    - _Requirements: 6.1, 6.2, 6.3, 6.7_


- [x] 10. Add import and manual entry buttons to FormBanking
  - [x] 10.1 Add Import Transactions and Import Balance buttons
    - Modify `OE2EmpireTracker/Forms/Banking/FormBanking.cs` and Designer
    - Add btnImportTransactions: calls BankingService.ImportTransactionsAsync, refreshes grid
    - Add btnImportBalance: calls BankingService.ImportBalanceAsync, updates lblBalance, persists via PlayerContext
    - Handle import errors: log and display result (pages completed, transactions imported)
    - _Requirements: 3.1, 3.4, 3.7, 4.1, 4.2, 4.3, 9.2_

  - [x] 10.2 Add manual transaction entry button and dialog
    - Create `OE2EmpireTracker/Forms/Banking/FormBankingEntry.cs`
    - Create `OE2EmpireTracker/Forms/Banking/FormBankingEntry.Designer.cs`
    - Create `OE2EmpireTracker/Forms/Banking/FormBankingEntry.resx`
    - Add btnAddTransaction to FormBanking that opens FormBankingEntry as modal
    - FormBankingEntry: dtpDateTime (default now, no future dates), txtCreditChange (validated decimal, non-zero), cboType (from BankingTransactionTypes), txtDetail (max 500 chars, required)
    - Validate: zero amount shows error, empty detail shows error, future date shows error
    - On OK: call BankingService.CreateManualTransaction, add to PlayerContext, refresh grid
    - On Cancel: no changes
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 11. Wire FormBanking into MainWindow menu
  - [x] 11.1 Add Banking menu item to MainWindow
    - Modify `OE2EmpireTracker/Forms/MainWindow.cs` and Designer
    - Add "Banking" menu item under appropriate menu section
    - Open FormBanking as MDI child on click
    - _Requirements: 5.1_

- [x] 12. Checkpoint - UI complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. Final integration and balance persistence
  - [x] 13.1 Implement balance persistence round-trip
    - Verify PlayerContext persists BankingBalance in PlayerRoot via WriteContext
    - Verify balance loads from JSON on application start (InitBankingTransactions)
    - Verify lblBalance displays persisted balance without API call on form open
    - _Requirements: 4.2, 4.4_

  - [x] 13.2 Write property test for balance persistence
    - Append to `OE2EmpireTracker.Tests/Services/BankingServicePropertyTests.cs`
    - **Property 4: Balance Persistence**
    - After storing a balance value, reading it back returns the same value
    - **Validates: Requirements 4.2**

- [x] 14. Final checkpoint - All tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design references `OE2EmpireTracker.Common` for Models/Services/Constants — this is the shared library project at solution root
- Forms go in `OE2EmpireTracker/Forms/Banking/` (the main WinForms project)
- Tests go in `OE2EmpireTracker.Tests/` mirroring the source structure
- FsCheck 2.16.6 is installed — do NOT use 3.x APIs (no FsCheck.Fluent, no Shrink.Default)
- GameApiClient already has GetBankingBalanceAsync and GetBankingTransactionsAsync (with offset/limit pagination)
- Use SystemClock.UtcNow instead of DateTime.UtcNow for testability

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "2.2"] },
    { "id": 1, "tasks": ["1.2", "2.3", "3.1"] },
    { "id": 2, "tasks": ["3.2"] },
    { "id": 3, "tasks": ["3.3", "5.1", "8.1", "8.2"] },
    { "id": 4, "tasks": ["5.2", "5.3", "5.4", "6.1"] },
    { "id": 5, "tasks": ["6.2", "6.3", "6.4"] },
    { "id": 6, "tasks": ["6.5", "6.6", "9.1"] },
    { "id": 7, "tasks": ["9.2", "9.3"] },
    { "id": 8, "tasks": ["10.1", "10.2"] },
    { "id": 9, "tasks": ["11.1"] },
    { "id": 10, "tasks": ["13.1"] },
    { "id": 11, "tasks": ["13.2"] }
  ]
}
```
