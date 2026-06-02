# Requirements Document

## Introduction

Centralizes banking data synchronization (transactions and balance) into the existing background scheduler (`GameApiSyncScheduler.SyncCharacterAsync`), eliminating manual import buttons from `FormBanking`. Banking sync becomes an automatic step in the scheduler cycle alongside profile, colony, and asset sync.

## Glossary

- **Scheduler**: The `GameApiSyncScheduler` class that performs periodic background synchronization of player data from the Game API.
- **SyncCharacterAsync**: The method within Scheduler that orchestrates all sync steps for a single character per cycle.
- **BankingService**: The static service class providing `ImportTransactionsAsync` and `ImportBalanceAsync` methods for fetching banking data from the Game API.
- **FormBanking**: The WinForms form that displays banking transactions, balance, and charts.
- **PlayerContext**: The singleton that holds player data in memory and persists it, raising change events.
- **BankingDataChanged**: The event on PlayerContext fired when banking transactions or balance are updated.

## Requirements

### Requirement 1

**User Story:** As a player, I want banking data to sync automatically in the background, so that I do not have to manually trigger imports.

#### Acceptance Criteria

1. WHEN SyncCharacterAsync completes asset sync successfully, THE Scheduler SHALL invoke BankingService.ImportTransactionsAsync using the same GameApiClient, appId, and accessToken already available in the sync context.
2. WHEN SyncCharacterAsync completes transaction sync, THE Scheduler SHALL invoke BankingService.ImportBalanceAsync using the same GameApiClient, appId, and accessToken.
3. WHEN banking transaction import succeeds with new transactions, THE Scheduler SHALL raise BankingDataChanged on PlayerContext.
4. WHEN banking balance import succeeds with a non-null value, THE Scheduler SHALL persist the balance to PlayerContext and raise BankingDataChanged.

### Requirement 2

**User Story:** As a player, I want banking sync failures to be isolated from other sync steps, so that a banking API error does not prevent profile, colony, or asset data from syncing.

#### Acceptance Criteria

1. IF BankingService.ImportTransactionsAsync throws an exception, THEN THE Scheduler SHALL log the error and preserve all prior sync results (profile, colony, asset) for the current cycle.
2. IF BankingService.ImportBalanceAsync throws an exception, THEN THE Scheduler SHALL log the error and preserve the transaction import result for the current cycle.
3. IF banking transaction import fails (non-exception failure), THEN THE Scheduler SHALL log a warning and continue to balance import.

### Requirement 3

**User Story:** As a player, I want the manual Import Transactions and Import Balance buttons removed from FormBanking, so that the UI does not offer redundant controls now that sync is automatic.

#### Acceptance Criteria

1. THE FormBanking SHALL NOT display an "Import Transactions" button.
2. THE FormBanking SHALL NOT display an "Import Balance" button.
3. THE FormBanking SHALL retain the "Add Transaction" button for manual entry of local transactions.
4. WHEN BankingDataChanged fires, THE FormBanking SHALL refresh its transaction grid and balance display as it does today.

### Requirement 4

**User Story:** As a player, I want banking sync to run on the same interval as other sync steps, so that my banking data stays current without extra configuration.

#### Acceptance Criteria

1. THE Scheduler SHALL execute banking sync (transactions and balance) once per character per scheduler cycle.
2. THE Scheduler SHALL execute banking sync after asset sync within SyncCharacterAsync.
3. WHILE the Scheduler is suspended (disconnected), THE Scheduler SHALL NOT attempt banking sync.
