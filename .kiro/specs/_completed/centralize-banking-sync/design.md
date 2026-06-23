# Design Document: Centralize Banking Sync

## Overview

This feature integrates banking data synchronization (transactions and balance) into the existing `GameApiSyncScheduler.SyncCharacterAsync` method as an additional step after asset sync. The manual import buttons are removed from `FormBanking` since the scheduler now handles this automatically. No new preferences, services, or events are required — the existing `BankingDataChanged` eventing and `FormBanking` subscription handle UI refresh.

## Architecture

### Component Interaction

```
GameApiSyncScheduler.SyncCharacterAsync
  ├── Profile sync (existing)
  ├── Colony sync (existing, try/catch)
  ├── Asset sync (existing, try/catch)
  └── Banking sync (NEW, try/catch)
        ├── BankingService.ImportTransactionsAsync(client, appId, accessToken, playerContext)
        │     └── internally calls playerContext.OnBankingDataChanged()
        └── BankingService.ImportBalanceAsync(client, appId, accessToken)
              └── if non-null: set playerContext.BankingBalance, WriteContext(), OnBankingDataChanged()
```

### Sync Ordering Within SyncCharacterAsync

The banking sync step is placed after asset sync, maintaining the existing fault-isolation pattern:

1. **Profile sync** — fetch and merge profile data
2. **Colony sync** — try/catch isolated, failure logged
3. **Asset sync** — try/catch isolated, failure logged
4. **Banking sync** — try/catch isolated, failure logged (NEW)

Each step is wrapped in its own try/catch so that a failure in banking does not affect the success/failure of profile, colony, or asset sync.

## Components and Interfaces

### Modified: GameApiSyncScheduler (OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs)

Add a new private method and a virtual method for testability:

```csharp
/// <summary>
/// Syncs banking transactions and balance for a character.
/// Called after asset sync within SyncCharacterAsync.
/// </summary>
private async Task SyncBankingAsync(string playerUUID, string accessToken)
{
    Log.Info("Banking sync starting for character {0}", playerUUID);

    // 1. Import transactions
    BankingImportResult txResult = await BankingService.ImportTransactionsAsync(
        _client, _appId, accessToken, GetPlayerContext()).ConfigureAwait(false);

    if (!txResult.Success)
    {
        Log.Warn("Banking transaction import reported failure for character {0}: {1}",
            playerUUID, txResult.ErrorMessage);
    }
    else
    {
        Log.Info("Banking transactions imported: {0} new, {1} duplicates skipped",
            txResult.TransactionsImported, txResult.DuplicatesSkipped);
    }

    // 2. Import balance (always attempt regardless of transaction result)
    decimal? balance = await BankingService.ImportBalanceAsync(
        _client, _appId, accessToken).ConfigureAwait(false);

    if (balance.HasValue)
    {
        SetBankingBalance(balance.Value);
        WriteContext();
        RaiseBankingDataChanged();
        Log.Info("Banking balance updated to {0:N2} for character {1}", balance.Value, playerUUID);
    }
    else
    {
        Log.Warn("Banking balance import returned null for character {0}", playerUUID);
    }
}
```


Add new virtual methods for testability (following the existing pattern for `RaiseColonyDataChanged`, `RaiseAssetDataChanged`):

```csharp
/// <summary>
/// Raises the BankingDataChanged event to notify UI subscribers.
/// Override point for testing. In production, calls PlayerContext.OnBankingDataChanged.
/// </summary>
internal virtual void RaiseBankingDataChanged()
{
    // Default implementation is a no-op — wired to PlayerContext in production via GameApiContext
}

/// <summary>
/// Retrieves the PlayerContext for banking operations.
/// Override point for testing.
/// </summary>
internal virtual PlayerContext GetPlayerContext()
{
    // Default implementation returns null — wired in production via GameApiContext
    return null;
}

/// <summary>
/// Sets the banking balance on the player context.
/// Override point for testing.
/// </summary>
internal virtual void SetBankingBalance(decimal balance)
{
    // Default implementation is a no-op — wired in production via GameApiContext
}
```

Integration in `SyncCharacterAsync` — add after the existing asset sync try/catch block:

```csharp
// Banking sync runs after asset sync (Req 4.2)
// Wrapped in try/catch so banking failures don't affect profile/colony/asset sync (Req 2.1, 2.2)
try
{
    await SyncBankingAsync(playerUUID, tokenResult.Token.AccessToken).ConfigureAwait(false);
}
catch (Exception bankingEx)
{
    Log.Error(bankingEx, "Banking sync failed for character {0}, profile/colony/asset sync results preserved", playerUUID);
}
```

### Modified: FormBanking (OE2EmpireTracker/Forms/Banking/FormBanking.cs)

**Remove** from the constructor:
- `btnImportTransactions.Click += BtnImportTransactions_Click;`
- `btnImportBalance.Click += BtnImportBalance_Click;`

**Remove** event handler methods:
- `BtnImportTransactions_Click`
- `BtnImportBalance_Click`

**Retain**:
- `btnAddTransaction.Click += BtnAddTransaction_Click;` (manual entry)
- `playerContext.BankingDataChanged += OnBankingDataChanged;` (auto-refresh from scheduler)
- All existing display/filter/chart logic

### Modified: FormBanking.Designer.cs

**Remove** control declarations and layout for:
- `btnImportTransactions`
- `btnImportBalance`

**Retain**:
- `btnAddTransaction`
- All other controls (grid, charts, filters, date pickers, period buttons)

### Interfaces

No new interfaces are introduced. The existing `BankingService` static methods are called directly. The scheduler uses the same virtual-override pattern for testability that it uses for colony and asset sync.

### Method Signatures Used

```csharp
// Already exists — no changes needed
public static async Task<BankingImportResult> ImportTransactionsAsync(
    GameApiClient apiClient, string appId, string accessToken, PlayerContext playerContext)

// Already exists — no changes needed
public static async Task<decimal?> ImportBalanceAsync(
    GameApiClient apiClient, string appId, string accessToken)
```

## Data Models

No data model changes. The existing `PlayerContext.BankingBalance` property and `BankingTransactionList` collection are used as-is. Persistence uses the existing `WriteContext()` path.

## Error Handling

| Failure Mode | Behavior | Requirement |
|---|---|---|
| `ImportTransactionsAsync` throws | Caught by banking try/catch, logged, profile/colony/asset preserved | Req 2.1 |
| `ImportBalanceAsync` throws | Caught by banking try/catch, logged, transaction result preserved | Req 2.2 |
| `ImportTransactionsAsync` returns `Success=false` | Logged as warning, proceeds to balance import | Req 2.3 |
| `ImportBalanceAsync` returns `null` | Logged as warning, no balance update | Req 1.4 (inverse) |
| Scheduler suspended | `SyncCharacterAsync` not called, banking sync skipped | Req 4.3 |

## Testing Strategy

- **Property tests** (FsCheck, NUnit): Properties 1–3 above — balance round-trip, fault isolation, once-per-character invariant
- **Unit tests** (NUnit): Verify call ordering (banking after asset), button removal from Designer.cs, BankingDataChanged subscription retained
- **Integration**: Existing `BankingDataChanged` eventing validates FormBanking refresh without changes


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Balance Persistence Round-Trip

*For any* non-null decimal value returned by `ImportBalanceAsync`, after `SyncBankingAsync` completes, the `PlayerContext.BankingBalance` SHALL equal that value, and `WriteContext()` SHALL have been called exactly once, and `BankingDataChanged` SHALL have been raised.

**Validates: Requirements 1.4**

### Property 2: Banking Exception Fault Isolation

*For any* exception thrown by `ImportTransactionsAsync` or `ImportBalanceAsync`, the `SyncCharacterAsync` method SHALL return `true` (indicating profile sync success), and all prior sync results (profile merge, colony data, asset data) SHALL remain unchanged.

**Validates: Requirements 2.1, 2.2**

### Property 3: Once-Per-Character-Per-Cycle Invariant

*For any* list of N configured characters, a single scheduler cycle (`SyncNowAsync`) SHALL invoke banking sync exactly N times total — once per character — regardless of individual sync success or failure.

**Validates: Requirements 4.1**
