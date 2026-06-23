# Technical Design Document

## Overview

This document describes the technical design for the in-game mail feature. It follows the established architecture pattern: POCO model in `OE2EmpireTracker.Common/Models/`, static service class in `OE2EmpireTracker.Common/Services/`, persistence via `PlayerContext`, and a WinForms MDI child form with `IProgrammaticUpdateSource`.

The mail system uses the same incremental sync strategy as banking transactions: a monotonically increasing integer key (MailId) serves as the high-water mark for efficient delta synchronization from the game API.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FormMail (UI)                                │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │ Filter Panel  │  │  Mail ListView   │  │   Detail Panel       │  │
│  │ (type, read,  │  │  (From, Subject, │  │   (MailContent       │  │
│  │  search)      │  │   Date, unread)  │  │    display)          │  │
│  └──────────────┘  └──────────────────┘  └──────────────────────┘  │
└──────────────┬──────────────────────────────────────────────────────┘
               │ Events: MailDataChanged, timer tick
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      MailService (Static)                            │
│  - SyncMailAsync(appId, token)                                      │
│  - MarkAsRead(mailId)                                               │
│  - GetFilteredMessages(readFilter, typeFilter, searchText)          │
│  - GetHighWaterMark()                                               │
│  - Event: MailDataChanged                                           │
└──────────────┬──────────────────────────────────────────────────────┘
               │ Read/Write
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      PlayerContext                                   │
│  - MailMessageList : IReadOnlyList<MailMessage>                      │
│  - AddMailMessage(MailMessage)                                       │
│  - InitMailMessages(PlayerRoot)                                     │
│  - FindMailMessage(int mailId) : MailMessage                        │
│  - Event: MailDataChanged                                           │
└──────────────┬──────────────────────────────────────────────────────┘
               │ Persistence
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│               PlayerRoot.MailMessage[] (JSON)                        │
└─────────────────────────────────────────────────────────────────────┘
               ▲ API calls
               │
┌─────────────────────────────────────────────────────────────────────┐
│             GameApiClient (existing)                                 │
│  - GetMailListAsync(appId, token, offset, limit)                    │
│  - GetMailDetailAsync(appId, token, mailId)                         │
└─────────────────────────────────────────────────────────────────────┘
```

## Data Models

### MailMessage (POCO Data Model)

**File:** `OE2EmpireTracker.Common/Models/MailMessage.cs`

```csharp
public class MailMessage
{
    [JsonProperty("mailId")]
    public int MailId { get; set; }

    [JsonProperty("characterIdFrom")]
    public int CharacterIdFrom { get; set; }

    [JsonProperty("fromName")]
    public string FromName { get; set; } = string.Empty;

    [JsonProperty("characterIdTo")]
    public int CharacterIdTo { get; set; }

    [JsonProperty("toName")]
    public string ToName { get; set; } = string.Empty;

    [JsonProperty("sentTime")]
    public string SentTime { get; set; } = string.Empty;

    [JsonProperty("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonProperty("mailRead")]
    public bool MailRead { get; set; }

    [JsonProperty("mailType")]
    public string MailType { get; set; }

    [JsonProperty("mailContent")]
    public string MailContent { get; set; } = string.Empty;

    [JsonProperty("localRead")]
    public bool LocalRead { get; set; }
}
```

**Design decisions:**
- Uses `int MailId` as the unique key (game API provides this as a monotonically increasing integer).
- No UUID field — MailId is the natural key from the API.
- `SentTime` stored as string (ISO 8601 from the API) to preserve the original format.
- `MailType` is nullable string: null = general/player/system, "C" = colony, "R" = research, "S" = skill.
- `LocalRead` tracks local read status independently of the API's `MailRead` field.

### PlayerRoot Extension

**File:** `OE2EmpireTracker.Common/Services/PlayerRoot.cs`

Add to `PlayerRoot`:
```csharp
[JsonProperty("mailMessage")]
public MailMessage[] MailMessage { get; set; }
```

Constructor addition:
```csharp
MailMessage = new MailMessage[0];
```


## Components and Interfaces

### Game API Wire Format

**GET /v1/mail?offset=0&limit=50 — List Response:**
```json
{
  "data": {
    "mail": [
      {
        "mailId": 1234,
        "characterIdFrom": 5678,
        "fromName": "  ",
        "characterIdTo": 9012,
        "toName": "PlayerName",
        "sentTime": "2025-11-15T10:30:00",
        "subject": "Colony Report",
        "mailRead": true,
        "mailType": "C"
      }
    ]
  }
}
```

**GET /v1/mail/{mailId} — Detail Response:**
```json
{
  "data": {
    "mailId": 1234,
    "characterIdFrom": 5678,
    "fromName": "System",
    "characterIdTo": 9012,
    "toName": "PlayerName",
    "sentTime": "2025-11-15T10:30:00",
    "subject": "Colony Report",
    "mailRead": true,
    "mailType": "C",
    "mailContent": "Your colony has completed construction..."
  }
}
```

**Key API behaviors:**
- List endpoint returns `fromName` as whitespace for system messages; detail endpoint returns the actual name (e.g., "System").
- List endpoint returns results ordered by mailId descending (newest first).
- Pagination uses offset/limit (not cursor-based).
- `mailType` is null for general player/system messages.

### MailService (Static Service Class)

**File:** `OE2EmpireTracker.Common/Services/MailService.cs`

```csharp
public static class MailService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static event EventHandler MailDataChanged;

    /// <summary>
    /// Performs incremental sync from the game API.
    /// Fetches pages of mail IDs starting at offset 0, stopping when all IDs
    /// on a page are <= the local high-water mark. For each new mailId,
    /// fetches the detail endpoint for full content.
    /// </summary>
    public static async Task<int> SyncMailAsync(
        GameApiClient client,
        string appId,
        string accessToken,
        PlayerContext playerContext)

    /// <summary>
    /// Returns the highest MailId in the local store, or 0 if empty.
    /// </summary>
    public static int GetHighWaterMark(PlayerContext playerContext)

    /// <summary>
    /// Marks a message as locally read and persists.
    /// </summary>
    public static void MarkAsRead(int mailId, PlayerContext playerContext)

    /// <summary>
    /// Returns messages matching all filter criteria (AND logic).
    /// </summary>
    public static IReadOnlyList<MailMessage> GetFilteredMessages(
        PlayerContext playerContext,
        string readFilter,
        string typeFilter,
        string searchText)

    /// <summary>
    /// Returns the count of messages where LocalRead = false.
    /// </summary>
    public static int GetUnreadCount(PlayerContext playerContext)

    /// <summary>
    /// Fires the MailDataChanged event (called after sync completes with new messages).
    /// </summary>
    public static void OnMailDataChanged()
}
```

### Sync Algorithm

```
SyncMailAsync:
1. highWaterMark = GetHighWaterMark() (max MailId in local store, or 0)
2. offset = 0, pageSize = 50, newCount = 0
3. LOOP:
   a. response = client.GetMailListAsync(appId, token, offset, pageSize)
   b. IF response.Success == false:
      - IF "401" or "403": log auth error, RETURN -1
      - IF "429": log rate limit, RETURN -1
      - ELSE: log error, RETURN -1
   c. Parse response JSON: envelope.data.mail as JArray
   d. IF array is null or empty: BREAK (end of mail)
   e. allExist = true
   f. FOR EACH mail item in array:
      - Parse mailId from item
      - IF mailId already exists in playerContext.MailMessageList: CONTINUE
      - allExist = false
      - detailResponse = client.GetMailDetailAsync(appId, token, mailId)
      - IF detailResponse.Success:
        - Parse MailMessage from detail JSON
        - Set LocalRead = false
        - Trim FromName/ToName (use detail's fromName which has actual name)
        - playerContext.AddMailMessage(message)
        - newCount++
      - ELSE:
        - Create MailMessage with empty MailContent from list data
        - playerContext.AddMailMessage(message)
        - newCount++
        - Log warning for failed detail fetch
   g. IF allExist: BREAK (early-stop: all IDs on this page already local)
   h. offset += pageSize
4. IF newCount > 0:
   - playerContext.WriteContext()
   - OnMailDataChanged()
5. RETURN newCount
```

### Filter Logic

```
GetFilteredMessages(playerContext, readFilter, typeFilter, searchText):
1. source = playerContext.MailMessageList
2. Apply readFilter:
   - "All": no filter
   - "Unread": where LocalRead == false
   - "Read": where LocalRead == true
3. Apply typeFilter:
   - "All": no filter
   - "Player/System": where MailType == null
   - "Colony": where MailType == "C"
   - "Research": where MailType == "R"
   - "Skill": where MailType == "S"
4. Apply searchText (if non-empty):
   - Case-insensitive substring match on FromName OR Subject OR MailContent
5. Return filtered list sorted by SentTime descending
```

### PlayerContext Integration

**File:** `OE2EmpireTracker.Common/Services/PlayerContext.cs`

New members:

```csharp
// Field
private List<MailMessage> _mailMessageList = new List<MailMessage>();
private Dictionary<int, MailMessage> _mailMessageCache;

// Event
public event EventHandler MailDataChanged;

// Property
public IReadOnlyList<MailMessage> MailMessageList => _mailMessageList;

// Init method (called from constructor)
public void InitMailMessages(PlayerRoot playerRoot)
{
    var source = playerRoot.MailMessage ?? new MailMessage[0];
    var seen = new HashSet<int>();
    var deduped = new List<MailMessage>();
    foreach (var item in source)
    {
        if (seen.Add(item.MailId))
        {
            deduped.Add(item);
        }
        else
        {
            Log.Error("DUPLICATE MailId on load: {0} Subject='{1}' — skipping duplicate",
                item.MailId, item.Subject);
        }
    }
    _mailMessageList = deduped;
    _mailMessageCache = null;
}

// Find by MailId
public MailMessage FindMailMessage(int mailId)
{
    if (mailId <= 0) return null;
    lock (_listLock)
    {
        if (_mailMessageCache == null)
        {
            _mailMessageCache = new Dictionary<int, MailMessage>();
            foreach (var m in _mailMessageList)
                if (!_mailMessageCache.ContainsKey(m.MailId))
                    _mailMessageCache[m.MailId] = m;
        }
        _mailMessageCache.TryGetValue(mailId, out var match);
        return match;
    }
}

// Add (skip duplicate, no throw)
public bool AddMailMessage(MailMessage item)
{
    lock (_listLock)
    {
        if (_mailMessageCache != null && _mailMessageCache.ContainsKey(item.MailId))
        {
            Log.Warn("Skipping duplicate MailMessage MailId={0}", item.MailId);
            return false;
        }
        else if (_mailMessageCache == null && _mailMessageList.Any(x => x.MailId == item.MailId))
        {
            Log.Warn("Skipping duplicate MailMessage MailId={0}", item.MailId);
            return false;
        }
        _mailMessageList.Add(item);
        if (_mailMessageCache != null)
            _mailMessageCache[item.MailId] = item;
        return true;
    }
}

// Event notification
public void OnMailDataChanged()
{
    MailDataChanged?.Invoke(this, EventArgs.Empty);
}
```

**Design decisions:**
- Uses `Dictionary<int, MailMessage>` cache keyed by MailId (int, not UUID string).
- `AddMailMessage` returns bool and logs warning on duplicate instead of throwing (unlike other entities which throw on duplicate UUID). This is because sync may encounter the same mailId across multiple pages.
- Deduplication on load uses `HashSet<int>` since MailId is the natural key.

WriteContext extension (add to lock block):
```csharp
playerRoot.MailMessage = _mailMessageList.ToArray();
```


### Background Sync Timer

**Owned by FormMail** — the timer runs only while the form is open.

```csharp
// In FormMail:
private System.Threading.Timer _syncTimer;
private bool _isSyncing;

// On form open:
_syncTimer = new System.Threading.Timer(OnSyncTimerTick, null,
    Timeout.Infinite, Timeout.Infinite);
// Trigger immediate sync
_ = Task.Run(() => ExecuteSyncAsync());
// Start periodic timer
int intervalMs = GetSyncIntervalMs();
_syncTimer.Change(intervalMs, Timeout.Infinite);

// Timer callback:
private void OnSyncTimerTick(object state)
{
    if (_isSyncing) return;
    _ = Task.Run(() => ExecuteSyncAsync());
}

// After sync completes, reschedule:
int intervalMs = GetSyncIntervalMs();
_syncTimer?.Change(intervalMs, Timeout.Infinite);

// On form close:
_syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
_syncTimer?.Dispose();
_syncTimer = null;
```

**Design decisions:**
- Timer uses one-shot mode (`Change(interval, Timeout.Infinite)`) and reschedules after each sync completes. This prevents timer drift and overlapping sync executions.
- `_isSyncing` flag prevents re-entrant sync if the timer fires while a previous sync is still running.
- Sync runs on a background thread via `Task.Run`. UI updates marshal back via `Invoke`.

### FormMail (MDI Child Form)

**File:** `OE2EmpireTracker/Forms/Mail/FormMail.cs` + `.Designer.cs` + `.resx`

**Implements:** `Form, IProgrammaticUpdateSource`

Layout:
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Mail (3 unread)                                                    [─][□][×]│
├─────────────────────────────────────────────────────────────────────────────┤
│ [cboReadFilter ▼] [cboTypeFilter ▼] [txtSearch___________] │ 42 messages   │
├────────────────────────────────────────┬────────────────────────────────────┤
│ From       │ Subject      │ Date      │                                    │
│────────────┼──────────────┼───────────│  From: System                      │
│ **System** │ **Colony..**│ **11-15** │  Date: 2025-11-15 10:30             │
│ PlayerA    │ Trade offer  │ 11-14     │  Subject: Colony Report             │
│ **System** │ **Research.**│ **11-14** │                                    │
│ PlayerB    │ Hello        │ 11-13     │  ─────────────────────────────────  │
│            │              │           │  Your colony has completed          │
│            │              │           │  construction of the new            │
│            │              │           │  Mining Rig III...                  │
│            │              │           │                                    │
├────────────────────────────────────────┴────────────────────────────────────┤
│ Last sync: 2025-11-15 10:35                              Syncing...         │
└─────────────────────────────────────────────────────────────────────────────┘
```

Controls:

| Control | Type | Purpose |
|---------|------|---------|
| `cboReadFilter` | ComboBox | Read status filter: All, Unread, Read |
| `cboTypeFilter` | ComboBox | Mail type filter: All, Player/System, Colony, Research, Skill |
| `txtSearch` | TextBox | Search text (FromName, Subject, MailContent) |
| `lblMessageCount` | Label | "N messages" count of filtered results |
| `splitContainer` | SplitContainer | Left: list, Right: detail |
| `lvwMail` | ListView | Mail list with columns: From, Subject, Date |
| `lblDetailFrom` | Label | "From: {name}" in detail panel |
| `lblDetailDate` | Label | "Date: {formatted}" in detail panel |
| `lblDetailSubject` | Label | "Subject: {subject}" in detail panel |
| `txtDetailContent` | TextBox (Multiline, ReadOnly) | Message body content |
| `lblSyncStatus` | Label | "Last sync: ..." or "Syncing..." |
| `lblPlaceholder` | Label | "No messages" placeholder (hidden when messages exist) |

Event subscriptions:

| Event | Handler | Action |
|-------|---------|--------|
| `PlayerContext.MailDataChanged` | `OnMailDataChanged` | Refresh list and counts |
| `PlayerContext.CurrentPlayerChanged` | `OnCurrentPlayerChanged` | Clear and reload for new player |
| `cboReadFilter.SelectedIndexChanged` | `OnFilterChanged` | Refresh filtered list |
| `cboTypeFilter.SelectedIndexChanged` | `OnFilterChanged` | Refresh filtered list |
| `txtSearch.TextChanged` | `OnFilterChanged` | Refresh filtered list |
| `lvwMail.SelectedIndexChanged` | `OnMailSelected` | Show detail, mark as read |
| `Form.FormClosed` | `OnFormClosed` | Stop timer, unsubscribe events |

UI Thread Marshalling — all event handlers that update UI controls check `InvokeRequired` and use `BeginInvoke` when called from background sync:

```csharp
private void OnMailDataChanged(object sender, EventArgs e)
{
    if (InvokeRequired)
    {
        BeginInvoke(new Action(() => OnMailDataChanged(sender, e)));
        return;
    }
    RefreshMailList();
    UpdateUnreadCount();
    UpdateSyncStatus("Sync complete: up to date");
}
```

### Preferences Integration

**File:** `OE2EmpireTracker.Common/Services/PreferencesStore.cs` (existing)

Add a `MailSyncIntervalMinutes` property to the preferences model:

```csharp
[JsonProperty("mailSyncIntervalMinutes")]
[DefaultValue(5)]
public int MailSyncIntervalMinutes { get; set; } = 5;
```

Constraint: Minimum 1, maximum 60. Clamped on read:
```csharp
public static int GetMailSyncIntervalMs()
{
    var prefs = PreferencesStore.GetInstance().Preferences;
    int minutes = Math.Max(1, Math.Min(60, prefs.MailSyncIntervalMinutes));
    return minutes * 60 * 1000;
}
```

## Error Handling

| Error | Response | UI Feedback |
|-------|----------|-------------|
| HTTP 401/403 | Log at Error, stop sync cycle | Status: "Sync failed: auth error" |
| HTTP 429 | Log at Warning, stop sync cycle | Status: "Sync failed: rate limited" |
| Network failure | Log at Error, stop sync cycle | Status: "Sync failed: network error" |
| Detail fetch fails | Log warning, store with empty content | No explicit UI — message appears with empty body |
| Null/missing SentTime | Sort as epoch, display "(no date)" | Date column shows "(no date)" |
| Duplicate MailId | Skip silently, log warning | No UI effect |

## Testing Strategy

### Unit Tests

| File | Coverage |
|------|----------|
| `OE2EmpireTracker.Tests/Models/MailMessageTests.cs` | JSON serialization round-trip, default values, nullable MailType |
| `OE2EmpireTracker.Tests/Services/MailServiceTests.cs` | Filter logic (all combinations), high-water mark, mark-as-read, sync algorithm with mocked API responses |

### Property-Based Tests

| Property | Test Strategy |
|----------|---------------|
| P1: Deduplication | Generate random sequences of AddMailMessage with repeated MailIds → assert distinct count |
| P3: Filter Completeness | Generate random mail sets + random filter combos → assert result = manual predicate check |
| P4: LocalRead Independence | Generate mail, set LocalRead → assert MailRead unchanged |
| P5: Sync Idempotency | Run sync twice with same mock data → assert same list state |

### Integration Tests

- `GameApiFullDiscoveryTests` already exercises `GetMailListAsync` and `GetMailDetailAsync` against the real API (marked `[Explicit]`).


## Correctness Properties

### Property 1: Deduplication Invariant

For all states of `MailMessageList`, no two elements share the same `MailId`. Verified by: after any sequence of `AddMailMessage` calls (including duplicates), the list contains at most one entry per MailId.

**Validates: Requirements 2.3, 2.4**

### Property 2: High-Water Mark Monotonicity

The high-water mark (max MailId) never decreases after a sync operation. If sync adds messages, the new high-water mark >= the old one.

**Validates: Requirements 3.3, 3.4**

### Property 3: Filter Completeness

For any filter configuration, `GetFilteredMessages` returns exactly the set of messages that satisfy ALL active filter predicates (AND logic). No message satisfying all predicates is excluded; no message violating any predicate is included.

**Validates: Requirements 5.4**

### Property 4: LocalRead Independence

Setting `LocalRead` on a message never modifies the `MailRead` field, and vice versa. The two fields are completely independent.

**Validates: Requirements 6.1, 6.2**

### Property 5: Sync Idempotency

Running sync twice with no API-side changes produces the same local state. No duplicates are created, no messages are lost.

**Validates: Requirements 3.3, 3.4, 3.5**

### Property 6: Sort Stability

Messages with null/empty SentTime sort to the end (oldest position). Messages with valid SentTime sort by parsed datetime descending.

**Validates: Requirements 4.4, 8.4**

## File Changes Summary

### New Files

| File | Description |
|------|-------------|
| `OE2EmpireTracker.Common/Models/MailMessage.cs` | POCO data model |
| `OE2EmpireTracker.Common/Services/MailService.cs` | Static service (sync, filter, mark-read) |
| `OE2EmpireTracker/Forms/Mail/FormMail.cs` | MDI child form code-behind |
| `OE2EmpireTracker/Forms/Mail/FormMail.Designer.cs` | WinForms designer layout |
| `OE2EmpireTracker/Forms/Mail/FormMail.resx` | Form resources |

### Modified Files

| File | Change |
|------|--------|
| `OE2EmpireTracker.Common/Services/PlayerRoot.cs` | Add `MailMessage[]` property |
| `OE2EmpireTracker.Common/Services/PlayerContext.cs` | Add MailMessage list, cache, init, add, find, event |
| `OE2EmpireTracker/Forms/MainWindow.cs` | Add "Mail" menu item |
| `OE2EmpireTracker.Common/Services/PreferencesStore.cs` | Add `MailSyncIntervalMinutes` preference |

### Test Files

| File | Description |
|------|-------------|
| `OE2EmpireTracker.Tests/Models/MailMessageTests.cs` | Model serialization/deserialization tests |
| `OE2EmpireTracker.Tests/Services/MailServiceTests.cs` | Sync logic, filter logic, mark-as-read, property-based tests |

## Satisfies Requirements

| Requirement | Design Elements |
|-------------|-----------------|
| Req 1: Data Model | MailMessage POCO with all specified fields |
| Req 2: Persistence | PlayerRoot.MailMessage[], PlayerContext init/add/find/write |
| Req 3: Background Sync | MailService.SyncMailAsync, FormMail timer, early-stop algorithm |
| Req 4: List Display | FormMail split layout, ListView, detail panel |
| Req 5: Filtering | MailService.GetFilteredMessages, combo boxes, search text |
| Req 6: Local Read Status | LocalRead field, MailService.MarkAsRead, bold styling |
| Req 7: Sync Configuration | PreferencesStore.MailSyncIntervalMinutes, dynamic interval |
| Req 8: Error Handling | Error table, status label updates, graceful degradation |
