# Design Document

## Overview

The banking-transactions feature adds local persistence, API import, and UI display of a player's in-game bank transaction history. It follows the established architecture: a POCO model in OE2EmpireTracker.Common/Models, a static service class in OE2EmpireTracker.Common/Services, PlayerContext integration for persistence, and a WinForms MDI child form with DataGridView for display.

The GameApiClient already exposes `GetBankingBalanceAsync` and `GetBankingTransactionsAsync` (with offset/limit pagination). This design wires those endpoints into the local data model and UI.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      FormBanking (UI)                        │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │ Balance  │  │ Period/Type  │  │ DataGridView          │ │
│  │ Label    │  │ Filters      │  │ (transactions)        │ │
│  └──────────┘  └──────────────┘  └───────────────────────┘ │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ Summary Panel: Income | Expenses | Net               │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
        │                           │
        ▼                           ▼
┌───────────────────┐    ┌──────────────────────────┐
│ BankingService    │    │ BankingTransactionTypes   │
│ (static methods)  │    │ (Constants)               │
└───────────────────┘    └──────────────────────────┘
        │
        ▼
┌───────────────────┐    ┌──────────────────────────┐
│ PlayerContext     │    │ GameApiClient             │
│ (persistence)     │    │ (API calls)              │
└───────────────────┘    └──────────────────────────┘
        │
        ▼
┌───────────────────┐
│ PlayerData.json   │
│ (BankingTxn array)│
└───────────────────┘
```

## Components and Interfaces

### BankingService (Static Service Class)

Location: `OE2EmpireTracker.Common/Services/BankingService.cs`

```csharp
public static class BankingService
{
    public static async Task<BankingImportResult> ImportTransactionsAsync(
        GameApiClient apiClient, string appId, string accessToken, PlayerContext playerContext);

    public static async Task<decimal?> ImportBalanceAsync(
        GameApiClient apiClient, string appId, string accessToken);

    public static BankingTransaction CreateManualTransaction(
        string ownerUUID, DateTime transactionDateTime, decimal creditChange,
        int transactionType, string detail);

    public static BankingSummary ComputeSummary(IEnumerable<BankingTransaction> transactions);

    public static IReadOnlyList<BankingTransaction> FilterTransactions(
        IReadOnlyList<BankingTransaction> transactions,
        int? transactionType, DateTime? fromDate, DateTime? toDate);
}
```

### BankingImportResult (DTO)

Location: `OE2EmpireTracker.Common/Services/BankingImportResult.cs`

```csharp
public class BankingImportResult
{
    public int TransactionsImported { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int PagesCompleted { get; set; }
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
    public int FailedAtPage { get; set; } = -1;
}
```

### BankingSummary (DTO)

Location: `OE2EmpireTracker.Common/Services/BankingSummary.cs`

```csharp
public class BankingSummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange { get; set; }
}
```

### BankingTransactionTypes (Constants)

Location: `OE2EmpireTracker.Common/Constants/BankingTransactionTypes.cs`

```csharp
public static class BankingTransactionTypes
{
    public static readonly Dictionary<int, string> TypeLabels = new Dictionary<int, string>
    {
        { 1, "Worker Wages" },
        { 2, "Market Sale" },
        { 3, "Market Purchase" },
        { 4, "Refueling" },
        { 5, "Colony Income" },
        { 6, "Transfer Received" },
        { 7, "Transfer Sent" },
        { 8, "Job Payment" },
        { 9, "Bounty" },
        { 10, "Insurance Payout" },
    };

    public static string GetLabel(int typeCode, string detail) { ... }
}
```

**Note:** The actual integer codes will be confirmed from API responses during development. The mapping is designed to be easily extended.

### PlayerContext Extensions

New fields, properties, and methods added to the existing PlayerContext singleton:

```csharp
// Fields
private List<BankingTransaction> _bankingTransactionList = new List<BankingTransaction>();
private Dictionary<string, BankingTransaction> _bankingTransactionCache;
private decimal _bankingBalance = 0m;

// Properties
public IReadOnlyList<BankingTransaction> BankingTransactionList => _bankingTransactionList;
public decimal BankingBalance { get; set; }

// Event
public event EventHandler BankingDataChanged;
public void OnBankingDataChanged() { BankingDataChanged?.Invoke(this, EventArgs.Empty); }

// Methods (following established Init/Add/Remove pattern)
public void InitBankingTransactions(PlayerRoot playerRoot) { ... }
public void AddBankingTransaction(BankingTransaction item) { ... }
public void RemoveBankingTransaction(BankingTransaction item) { ... }
```

### FormBanking (MDI Child Form)

Location: `OE2EmpireTracker.Desktop/Forms/Banking/FormBanking.cs`

Implements: IProgrammaticUpdateSource, NLog Logger, CurrentPlayerChanged + BankingDataChanged subscriptions.

#### Controls

| Control | Type | Purpose |
|---------|------|---------|
| lblBalance | Label | Displays current bank balance |
| btn24h | Button | Period filter: last 24 hours |
| btn7d | Button | Period filter: last 7 days |
| btn30d | Button | Period filter: last 30 days |
| btnAllTime | Button | Period filter: all time (default) |
| cboType | ComboBox | Transaction type filter dropdown |
| dtpFrom | DateTimePicker | Date range start (with checkbox) |
| dtpTo | DateTimePicker | Date range end (with checkbox) |
| dgvTransactions | DataGridView | Transaction list grid |
| lblIncome | Label | Summary: total income |
| lblExpenses | Label | Summary: total expenses |
| lblNet | Label | Summary: net change |
| btnImportTransactions | Button | Trigger API transaction import |
| btnImportBalance | Button | Trigger API balance import |
| btnAddTransaction | Button | Open manual entry dialog |

### FormBankingEntry (Modal Dialog)

Location: `OE2EmpireTracker.Desktop/Forms/Banking/FormBankingEntry.cs`

| Control | Type | Purpose |
|---------|------|---------|
| dtpDateTime | DateTimePicker | Transaction date/time (default: now) |
| txtCreditChange | TextBox | Credit change amount (validated decimal) |
| cboType | ComboBox | Transaction type dropdown |
| txtDetail | TextBox | Free text detail (max 500 chars) |
| btnOK | Button | Confirm entry |
| btnCancel | Button | Cancel |
| lblError | Label | Validation error display |

## Data Models

### BankingTransaction

Location: `OE2EmpireTracker.Common/Models/BankingTransaction.cs`

```csharp
namespace OE2EmpireTracker.Models
{
    public class BankingTransaction
    {
        [JsonProperty("uuid")]
        public string UUID { get; set; } = string.Empty;

        [JsonProperty("ownerUUID")]
        public string OwnerUUID { get; set; } = string.Empty;

        [JsonProperty("transactionDateTime")]
        public string TransactionDateTime { get; set; } = string.Empty;

        [JsonProperty("creditChange")]
        public decimal CreditChange { get; set; } = 0m;

        [JsonProperty("oldBalance")]
        public decimal OldBalance { get; set; } = 0m;

        [JsonProperty("newBalance")]
        public decimal NewBalance { get; set; } = 0m;

        [JsonProperty("transactionType")]
        public int TransactionType { get; set; } = 0;

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonProperty("characterId")]
        public int? CharacterId { get; set; }

        [JsonProperty("systemObjectId")]
        public int? SystemObjectId { get; set; }

        [JsonProperty("systemId")]
        public int? SystemId { get; set; }

        [JsonProperty("isManualEntry")]
        public bool IsManualEntry { get; set; } = false;
    }
}
```

**Field rationale:**
- `UUID`: Locally generated (Guid.NewGuid().ToString()) since the game API does not provide unique IDs.
- `TransactionDateTime`: ISO 8601 string as received from the API (consistent with MarketTransaction.Timestamp).
- `CreditChange`: Decimal for precision. Positive = income, negative = expense.
- `OldBalance`/`NewBalance`: Balance snapshot before/after the transaction.
- `TransactionType`: Integer code from game API, mapped to labels via BankingTransactionTypes.
- `Detail`: Human-readable description from the game.
- `CharacterId`/`SystemObjectId`/`SystemId`: Nullable ints — not all transactions have these.
- `IsManualEntry`: Distinguishes user-entered from API-imported transactions.

### PlayerRoot Extension

```csharp
[JsonProperty("bankingTransaction")]
public BankingTransaction[] BankingTransaction { get; set; } = new BankingTransaction[0];

[JsonProperty("bankingBalance")]
public decimal BankingBalance { get; set; } = 0m;
```

### Deduplication Composite Key

Since the API provides no unique ID, deduplication during import uses:
- Key format: `$"{TransactionDateTime}|{CreditChange}|{Detail}"`
- Stored in a HashSet<string> built from existing local transactions
- Checked before adding each imported transaction

## Error Handling

### API Import Errors

- **Network failure / timeout**: Log at Error level, set `BankingImportResult.Success = false`, retain all transactions from prior pages, report `FailedAtPage` number.
- **HTTP 401/403**: Log at Error level, report authentication failure to caller. Do not retry — user must re-authenticate.
- **HTTP 5xx**: Log at Error level, treat as transient. The existing Polly retry policy in GameApiClient handles retries automatically.

### Balance Import Errors

- **Any error**: Log at Error level, return null. Caller retains last known balance (or zero if none stored).

### Data Integrity Errors

- **Duplicate UUID on Add**: Throw InvalidOperationException (programming error — should never happen with generated UUIDs).
- **Null/empty UUID on Init**: Skip the record, log a warning (defensive against corrupt data files).
- **Null TransactionDateTime**: Treat as epoch (1970-01-01) for sorting, display "(no date)" in grid.

### Manual Entry Validation

- **Zero CreditChange**: Show validation error in dialog, prevent submission.
- **Empty Detail**: Show validation error in dialog, prevent submission.
- **Future date**: Show validation error in dialog, prevent submission.

## Correctness Properties

### Property 1: Import Deduplication Idempotency

**Validates: Requirements 3.3**

Importing the same set of transactions twice SHALL result in zero new records on the second import. The composite key (TransactionDateTime + CreditChange + Detail) ensures no duplicates are created regardless of how many times the import is triggered.

### Property 2: Summary Arithmetic Consistency

**Validates: Requirements 6.4, 6.5, 6.6**

For any filtered set of transactions: TotalIncome + (-TotalExpenses) = NetChange. Additionally, summing all CreditChange values in the set SHALL equal NetChange. This holds for empty sets (all zeros), single-element sets, and arbitrarily large sets.

### Property 3: Filter Completeness

**Validates: Requirements 5.7, 5.9**

Every transaction in the filtered result SHALL satisfy ALL active filter conditions (type AND date range). No transaction outside the filter criteria SHALL appear in the result. The filtered set is always a subset of the input set.

### Property 4: Balance Persistence

**Validates: Requirements 4.2**

After a successful balance import, closing and reopening the application SHALL display the same balance value without requiring another API call. The balance is persisted in PlayerData.json.

### Property 5: Manual Entry Distinguishability

**Validates: Requirements 8.3**

Every manually entered transaction SHALL have IsManualEntry = true. Every API-imported transaction SHALL have IsManualEntry = false. These sets are disjoint and the flag is immutable after creation.

### Property 6: UUID Uniqueness

**Validates: Requirements 2.3, 2.4**

No two BankingTransaction records in PlayerContext SHALL share the same UUID. Adding a duplicate UUID SHALL throw InvalidOperationException. Init deduplicates on load, retaining the first occurrence.

## Service Method Details

### ImportTransactionsAsync

1. Build a HashSet of existing composite keys from `playerContext.BankingTransactionList`
2. Loop: call `apiClient.GetBankingTransactionsAsync(appId, accessToken, offset, PageSize)` where PageSize = 100
3. For each transaction in the response:
   - Compute composite key: `$"{transactionDT}|{creditChange}|{detail}"`
   - If key exists in HashSet → skip (increment duplicatesSkipped)
   - Otherwise → create BankingTransaction with generated UUID, add to PlayerContext
4. If page returns fewer than PageSize records → stop pagination
5. On API error → log, set result.Success = false, return partial result
6. Call `playerContext.WriteContext()` after all pages processed
7. Fire `playerContext.OnBankingDataChanged()`

### ImportBalanceAsync

1. Call `apiClient.GetBankingBalanceAsync(appId, accessToken)`
2. Parse the JSON response to extract the balance decimal value
3. Return the balance (or null on failure)
4. Caller (form) stores via PlayerContext and calls WriteContext

### CreateManualTransaction

1. Validate creditChange != 0 (throw ArgumentException if zero)
2. Create new BankingTransaction with generated UUID, IsManualEntry = true, OldBalance/NewBalance = 0
3. Return the created transaction (caller adds to PlayerContext)

### ComputeSummary

1. Sum all positive CreditChange values → TotalIncome
2. Sum absolute values of all negative CreditChange values → TotalExpenses
3. NetChange = TotalIncome - TotalExpenses

### FilterTransactions

1. Start with full list
2. If transactionType has value → filter to matching TransactionType
3. If fromDate has value → filter to TransactionDateTime >= fromDate
4. If toDate has value → filter to TransactionDateTime <= toDate
5. Return filtered list sorted by TransactionDateTime descending

## UI Layout

### FormBanking

```
┌─────────────────────────────────────────────────────────────────────┐
│ FormBanking                                                         │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ lblBalance: "Balance: 1,234,567.89"                             │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ ┌─ Filters ───────────────────────────────────────────────────────┐ │
│ │ [24h] [7d] [30d] [All*]   Type: [All ▼]                        │ │
│ │ From: [☐ yyyy-MM-dd]  To: [☐ yyyy-MM-dd]                       │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ ┌─ Transactions ──────────────────────────────────────────────────┐ │
│ │ Date          │ Type         │ Detail        │ Change │ Balance │ │
│ │ 2026-05-30... │ Worker Wages │ Colony Alpha  │ -500   │ 12,000  │ │
│ │ 2026-05-29... │ Market Sale  │ Iron Ore x100 │ +2,500 │ 12,500  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ ┌─ Summary ───────────────────────────────────────────────────────┐ │
│ │ Income: 45,000.00  │  Expenses: 12,500.00  │  Net: 32,500.00   │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ [Import Transactions]  [Import Balance]  [Add Transaction]          │
└─────────────────────────────────────────────────────────────────────┘
```

## Testing Strategy

### Unit Tests (OE2EmpireTracker.Tests)

| Test Class | Covers |
|-----------|--------|
| BankingTransactionTests | Model defaults, serialization round-trip |
| BankingServiceImportTests | Import logic, deduplication, pagination, error handling |
| BankingServiceSummaryTests | ComputeSummary arithmetic, empty sets, edge cases |
| BankingServiceFilterTests | Filter by type, date range, combined filters |
| BankingTransactionTypesTests | Label lookup, unknown codes, null detail handling |
| PlayerContextBankingTests | Add/remove/init, duplicate UUID rejection, persistence |

### Property-Based Tests (FsCheck 2.16.6)

| Property | Generator | Assertion |
|----------|-----------|-----------|
| Summary arithmetic | List of random CreditChange decimals | Sum(positive) - Sum(|negative|) = NetChange |
| Filter completeness | Random transactions + random filter params | All results match filter; no non-matching results |
| Dedup idempotency | Random transaction list | Import same list twice → second import adds 0 |
| UUID uniqueness | Random transactions with some duplicate UUIDs | Init deduplicates, Add rejects duplicates |

## Dependencies

### Existing (no new packages needed)
- Newtonsoft.Json (serialization)
- NLog (logging)
- GameApiClient (API calls — already has banking endpoints)
- PlayerContext (persistence)
- SystemClock (testable time)

### New Files

| File | Location | Purpose |
|------|----------|---------|
| BankingTransaction.cs | OE2EmpireTracker.Common/Models/ | Data model |
| BankingTransactionTypes.cs | OE2EmpireTracker.Common/Constants/ | Type code → label mapping |
| BankingService.cs | OE2EmpireTracker.Common/Services/ | Import, summary, filter logic |
| BankingImportResult.cs | OE2EmpireTracker.Common/Services/ | Import result DTO |
| BankingSummary.cs | OE2EmpireTracker.Common/Services/ | Summary DTO |
| FormBanking.cs | OE2EmpireTracker.Desktop/Forms/Banking/ | Main banking form |
| FormBanking.Designer.cs | OE2EmpireTracker.Desktop/Forms/Banking/ | Designer-generated layout |
| FormBanking.resx | OE2EmpireTracker.Desktop/Forms/Banking/ | Form resources |
| FormBankingEntry.cs | OE2EmpireTracker.Desktop/Forms/Banking/ | Manual entry dialog |
| FormBankingEntry.Designer.cs | OE2EmpireTracker.Desktop/Forms/Banking/ | Designer-generated layout |
| FormBankingEntry.resx | OE2EmpireTracker.Desktop/Forms/Banking/ | Dialog resources |

### Modified Files

| File | Change |
|------|--------|
| PlayerRoot.cs | Add BankingTransaction array + BankingBalance decimal |
| PlayerContext.cs | Add banking list, cache, Init/Add/Remove methods, BankingDataChanged event |
| MainWindow.cs | Add "Banking" menu item to open FormBanking |
| CollectionSortHelper.cs | Add OrderBankingTransactions method |
| SerializationSorter.cs | Add banking transaction sorting in SortPlayerRoot |

## Requirement Traceability

| Requirement | Design Element |
|-------------|---------------|
| Req 1 (Data Model) | BankingTransaction.cs |
| Req 2 (Persistence) | PlayerContext integration, PlayerRoot extension |
| Req 3 (API Import) | BankingService.ImportTransactionsAsync |
| Req 4 (Balance Import) | BankingService.ImportBalanceAsync, PlayerContext.BankingBalance |
| Req 5 (Display) | FormBanking grid, filters, balance label |
| Req 6 (Summary) | BankingService.ComputeSummary, FormBanking summary panel |
| Req 7 (Type Classification) | BankingTransactionTypes constants |
| Req 8 (Manual Entry) | FormBankingEntry dialog, BankingService.CreateManualTransaction |
| Req 9 (Error States) | BankingImportResult partial success, null date handling |
