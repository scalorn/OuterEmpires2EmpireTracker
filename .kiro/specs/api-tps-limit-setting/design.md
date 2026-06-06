# Technical Design: API TPS Limit Setting

## Overview

This design introduces a configurable TPS (transactions per second) rate limit and a general-purpose parallel request queue (`GameApiRequestQueue`) to enable high-throughput API data fetching. The system has three layers:

1. **Settings Layer** — A `Tps` property on `GameApiConnectionSettings`, exposed via a ValidatedTextBox on the Preferences Form's Game API tab.
2. **Queue Service Layer** — `GameApiRequestQueue`, a new service in `OE2EmpireTracker.Common/Services/` implementing token-bucket rate governing, cascading work, adaptive 429 handling, and thread-safe error collection.
3. **Consumer Layer** — The restructured `GameApiFullDiscoveryTests` fixture, which replaces 23 sequential ordered tests with a single queue-driven run method.

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│  FormPreferences (Game API Tab)                                      │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │ txtTpsLimit (ValidatedTextBox) ──> GameApiConnectionSettings │    │
│  └──────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
         │ persists via PreferencesStore.Save()
         ▼
┌──────────────────────────────────────────────────────────────────────┐
│  GameApiConnectionSettings (Model)                                   │
│  + Tps : double = 0.5                                                │
└──────────────────────────────────────────────────────────────────────┘
         │ read by consumers
         ▼
┌──────────────────────────────────────────────────────────────────────┐
│  GameApiRequestQueue (Service)                                       │
│  ┌─────────────────┐  ┌──────────────────────────────────────┐      │
│  │ TokenBucketGov  │  │  ConcurrentQueue<WorkItem>           │      │
│  │ (Rate Governor) │  │  (pending work items, FIFO)          │      │
│  └────────┬────────┘  └───────────────────┬──────────────────┘      │
│           │ grants tokens                  │ dequeues items          │
│           ▼                                ▼                         │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │ Dispatch Loop (single async loop)                            │    │
│  │ - Awaits token from governor                                  │    │
│  │ - Dequeues next work item                                     │    │
│  │ - Fires-and-forgets the async delegate                        │    │
│  │ - On completion: enqueues cascaded items, records result      │    │
│  └──────────────────────────────────────────────────────────────┘    │
│  ┌──────────────────────────┐  ┌────────────────────────────────┐    │
│  │ AdaptiveRateController   │  │ CompletionTracker              │    │
│  │ - 429 pause/backoff      │  │ - inflight count               │    │
│  │ - recovery ramp          │  │ - drain TaskCompletionSource   │    │
│  │ - effective TPS property │  │ - error list                   │    │
│  └──────────────────────────┘  └────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
         │ dispatches work items that call
         ▼
┌──────────────────────────────────────────────────────────────────────┐
│  GameApiClient (existing, unchanged)                                 │
│  - Internal SemaphoreSlim rate limiter remains active (dual layer)   │
│  - Polly retry + circuit breaker policies unchanged                  │
│  - 429 handling + X-RateLimit-Limit header reading unchanged         │
└──────────────────────────────────────────────────────────────────────┘
```

The queue operates **above** the GameApiClient. Work item delegates call GameApiClient methods directly. Dual-layer rate limiting:

1. **Queue rate governor** — Controls dispatch timing (TPS-based token bucket).
2. **GameApiClient internal limiter** — Secondary safety net (SemaphoreSlim sliding window at 30 req/min or server-communicated rate).


## Components and Interfaces

### GameApiRequestQueue — Public Interface

File: `OE2EmpireTracker.Common/Services/GameApiRequestQueue.cs`

```csharp
namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// General-purpose parallel task queue with token-bucket rate governing.
    /// Dispatches async work items at a configurable TPS, supports cascading work,
    /// and adapts to HTTP 429 responses with backoff/recovery.
    /// </summary>
    public class GameApiRequestQueue
    {
        public GameApiRequestQueue(double configuredTps, TimeSpan? perItemTimeout = null);

        public double ConfiguredTps { get; }
        public double EffectiveTps { get; }
        public QueueCompletionStatus CompletionStatus { get; }
        public IReadOnlyList<QueueError> Errors { get; }

        public Task EnqueueAsync(WorkItem workItem);
        public void Start(CancellationToken cancellationToken = default);
        public Task DrainAsync();
        public void NotifyRateLimited(int retryAfterSeconds);
    }
}
```

### WorkItem and Supporting Types

File: `OE2EmpireTracker.Common/Services/WorkItem.cs`

```csharp
namespace OE2EmpireTracker.Services
{
    public class WorkItem
    {
        public string Label { get; set; }
        public Func<CancellationToken, Task<IReadOnlyList<WorkItem>>> ExecuteAsync { get; set; }
    }

    public class QueueError
    {
        public string WorkItemLabel { get; set; }
        public Exception Exception { get; set; }
    }

    public class QueueCompletionStatus
    {
        public int TotalProcessed { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
    }
}
```

### Internal Components (Private Nested Classes)

#### TokenBucketGovernor

Token-bucket algorithm for rate control:
- **Bucket capacity:** `configuredTps` tokens (max burst = 1 second of accumulated tokens).
- **Refill rate:** `configuredTps` tokens per second, accumulated fractionally using `SystemClock.UtcNow` deltas.
- **AcquireAsync:** Blocks via `SemaphoreSlim(0,1)` + timer-based wakeup until a token is available.
- **Effective TPS override:** The AdaptiveRateController can temporarily reduce the refill rate.

```csharp
private class TokenBucketGovernor
{
    private double _tokens;
    private double _capacity;
    private DateTime _lastRefill;
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);
    private Func<double> _getEffectiveTps;

    public TokenBucketGovernor(double configuredTps, Func<double> getEffectiveTps);
    public Task AcquireTokenAsync(CancellationToken ct);
}
```

Refill logic: `tokensToAdd = elapsed * effectiveTps; tokens = Min(tokens + tokensToAdd, capacity)`

#### AdaptiveRateController

Manages TPS reduction on 429 responses and gradual recovery:

```csharp
private class AdaptiveRateController
{
    private double _effectiveTps;
    private double _configuredTps;
    private int _consecutiveBackoffs;
    private DateTime _pauseUntil;
    private DateTime _lastSuccessfulDispatch;
    private bool _isRecovering;

    public double EffectiveTps { get; }
    public bool IsPaused { get; }

    public void OnRateLimited(int retryAfterSeconds);
    public void OnSuccessfulDispatch();
    public TimeSpan GetPauseRemaining();
}
```

State machine:
```
NORMAL ──[429]──> PAUSED ──[expires]──> BACKOFF (50% of active rate)
  ▲                                        │
  │                                  [429 again] → PAUSED (2x duration, cap 5min)
  │                                        │
  │                                  [60s ok] → RECOVERING (+10%/min)
  │                                        │
  └──────────────[reaches configuredTps]───┘
```


#### CompletionTracker

Tracks in-flight items and signals drain:

```csharp
private class CompletionTracker
{
    private int _pending;
    private int _inflight;
    private int _succeeded;
    private int _failed;
    private TaskCompletionSource<bool> _drainTcs;

    public void OnEnqueued();
    public void OnDispatched();
    public void OnCompleted(bool success);
    public void OnCascadedEnqueued(int count);
    public Task WaitForDrainAsync();
    public bool IsFullyDrained { get; }
}
```

Drain condition: `_pending == 0 && _inflight == 0`. Uses `Interlocked` operations for thread safety.

#### Dispatch Loop

```csharp
private async Task DispatchLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        if (_rateController.IsPaused)
        {
            await Task.Delay(_rateController.GetPauseRemaining(), ct);
            continue;
        }

        await _governor.AcquireTokenAsync(ct);

        if (!_queue.TryDequeue(out WorkItem item))
        {
            if (_tracker.IsFullyDrained) break;
            await Task.Delay(50, ct);
            continue;
        }

        _tracker.OnDispatched();
        _ = ExecuteWorkItemAsync(item, ct);
    }
}
```

#### Work Item Execution

```csharp
private async Task ExecuteWorkItemAsync(WorkItem item, CancellationToken ct)
{
    try
    {
        using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        itemCts.CancelAfter(_perItemTimeout);

        IReadOnlyList<WorkItem> cascaded = await item.ExecuteAsync(itemCts.Token);
        _rateController.OnSuccessfulDispatch();
        _tracker.OnCompleted(success: true);

        if (cascaded != null && cascaded.Count > 0)
        {
            _tracker.OnCascadedEnqueued(cascaded.Count);
            foreach (var followUp in cascaded)
            {
                _queue.Enqueue(followUp);
            }
        }
    }
    catch (Exception ex)
    {
        _errors.Add(new QueueError { WorkItemLabel = item.Label, Exception = ex });
        _tracker.OnCompleted(success: false);
    }
}
```

### Thread Safety Strategy

| Component | Mechanism |
|-----------|-----------|
| Work item queue | `ConcurrentQueue<WorkItem>` |
| Error list | `ConcurrentBag<QueueError>` |
| CompletionTracker counters | `Interlocked.Increment` / `Interlocked.Decrement` |
| TokenBucketGovernor tokens | `lock` on refill + consume (low contention — single dispatch loop) |
| AdaptiveRateController state | `lock` (mutations only on 429 or recovery tick) |
| Drain signaling | `TaskCompletionSource<bool>` with `TrySetResult` |

### FormPreferences — UI Integration

New control on the Game API tab (below polling interval):

| Control | Type | Name | Properties |
|---------|------|------|------------|
| Label | Label | `lblTpsLimit` | Text="TPS Limit:" |
| Input | ValidatedTextBox | `txtTpsLimit` | ValidationPattern=`^\d{1,3}(\.\d)?$` |

Behavior:
- **PopulateGameApiFields:** `txtTpsLimit.Text = settings.Tps.ToString("F1")`
- **Leave event:** Clamp to [0.1, 100.0] or revert to last valid value if non-numeric.
- **SaveGameApiSettings:** `settings.Tps = double.Parse(txtTpsLimit.Text)`


## Data Models

### GameApiConnectionSettings (Modified)

```csharp
// File: OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs
public class GameApiConnectionSettings
{
    public string ServerUrl { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public int PollingIntervalMinutes { get; set; } = 5;
    public bool Enabled { get; set; } = false;
    public double Tps { get; set; } = 0.5;  // NEW — default 0.5 TPS
}
```

Serialization: PascalCase key `"Tps"` (matches existing `"ServerUrl"`, `"PollingIntervalMinutes"` convention). Backward compatible — absent key uses default 0.5.

### WorkItem (New)

```csharp
// File: OE2EmpireTracker.Common/Services/WorkItem.cs
public class WorkItem
{
    public string Label { get; set; }
    public Func<CancellationToken, Task<IReadOnlyList<WorkItem>>> ExecuteAsync { get; set; }
}
```

### QueueError (New)

```csharp
public class QueueError
{
    public string WorkItemLabel { get; set; }
    public Exception Exception { get; set; }
}
```

### QueueCompletionStatus (New)

```csharp
public class QueueCompletionStatus
{
    public int TotalProcessed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
}
```

### EndpointResult (Existing, unchanged)

Used in discovery tests for recording per-endpoint outcomes. No structural changes, but the backing collection changes from `List<EndpointResult>` to `ConcurrentBag<EndpointResult>`.

## Error Handling

### Queue-Level Errors

1. **Work item exception:** Caught in `ExecuteWorkItemAsync`, recorded in `ConcurrentBag<QueueError>`, work item marked failed. Other items continue unaffected.
2. **Cascading enqueue failure:** If `item.ExecuteAsync` throws during cascade generation, the parent item is recorded as failed. No cascaded items are enqueued.
3. **Cancellation:** When CancellationToken is triggered, the dispatch loop stops dequeuing new items. In-flight items have `perItemTimeout` seconds to complete before being cancelled. Timed-out items are recorded as failed.
4. **Per-item timeout:** Each work item gets a linked CancellationToken with `CancelAfter(_perItemTimeout)`. If the delegate doesn't complete within the timeout, `OperationCanceledException` is caught and recorded as failure.

### 429 Handling Flow

```
Work item receives HTTP 429
  → Work item calls queue.NotifyRateLimited(retryAfterSeconds)
  → AdaptiveRateController sets _pauseUntil and reduces effective TPS
  → Dispatch loop detects IsPaused, waits for pause to clear
  → Work item is re-enqueued at the back of the queue for retry
  → After pause: dispatch resumes at reduced TPS
  → After 60s success: recovery ramp begins (+10%/min)
```

### Discovery Test Error Handling

- Seed work item failure: Recorded as `RecordSkipped`, other seeds continue independently.
- Cascading work item failure: Recorded individually, does not affect sibling cascades.
- Queue infrastructure failure (should not happen): Test fails with exception.
- All results written to `_metadata.json` regardless of individual failures.


## Testing Strategy

### Unit Tests (NUnit)

File: `OE2EmpireTracker.Tests/Services/GameApiRequestQueueTests.cs`

| Test | Validates |
|------|-----------|
| Enqueue_SingleItem_Dispatches | Basic enqueue → execute → drain cycle |
| Enqueue_MultipleItems_FIFO | Dispatch order matches enqueue order |
| CascadingItems_AreProcessed | Cascaded items are enqueued and executed |
| FailedItem_DoesNotBlockOthers | Exception in one item, others complete |
| Cancellation_StopsNewDispatch | After cancel, no new items dispatched |
| Cancellation_InflightItemsTimeout | In-flight items cancelled after timeout |
| DrainAsync_WaitsForAllCascades | Drain blocks until nested cascades finish |
| NotifyRateLimited_PausesDispatch | No dispatch during pause window |
| Recovery_RampsBack | After backoff, TPS increases 10%/min |
| ConsecutiveBackoffs_DoubleDuration | Multiple 429s increase pause duration |

File: `OE2EmpireTracker.Tests/Services/TokenBucketGovernorTests.cs`

| Test | Validates |
|------|-----------|
| AcquireToken_RespectsTpsRate | Tokens dispensed at configured rate |
| BurstCapacity_EqualsOnceSecondOfTps | Accumulated tokens cap at TPS value |
| EffectiveTpsChange_AffectsRefillRate | Reducing TPS slows token generation |

File: `OE2EmpireTracker.Tests/Services/AdaptiveRateControllerTests.cs`

| Test | Validates |
|------|-----------|
| OnRateLimited_SetsPause | IsPaused true for duration |
| OnRateLimited_ReducesTps | EffectiveTps = 50% of prior |
| ConsecutiveBackoffs_CapsAt5Min | Pause doubles but caps at 300s |
| Recovery_Ramps10PercentPerMinute | TPS increases after 60s success |
| Recovery_StopsAtConfiguredTps | Does not exceed configured value |

### Property-Based Tests (FsCheck 2.16.6)

File: `OE2EmpireTracker.Tests/Services/GameApiRequestQueuePropertyTests.cs`

| Property | Generator Strategy |
|----------|--------------------|
| P1: Rate Governor Throughput | Random TPS [0.5, 20.0], random item count [5, 50], frozen SystemClock, verify dispatch count per 5s window |
| P2: FIFO Ordering | Random item count [2, 100], verify dispatch sequence numbers are monotonically increasing |
| P3: Cascading Completeness | Random tree depth [1, 3], random branching [0, 3], atomic counter, verify total == counter after drain |
| P4: Error Isolation | Random failure positions in item list, verify succeeded + failed == total |
| P5: 429 Backoff | Random retryAfter [1, 120], frozen clock, advance incrementally, verify no dispatch during pause |
| P6: Drain Completeness | Random delays [0, 50ms], random cascading, verify inflight == 0 after drain |

All property tests use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute (FsCheck 2.16.6 API).

## Correctness Properties

### Property 1: Rate Governor Throughput

**Validates: Requirements 7.1, 7.2, 7.3**

Over any 5-second window, the number of dispatched items SHALL NOT exceed `configuredTps * 5 + configuredTps` (allowing burst up to 1 second of accumulated tokens).

### Property 2: FIFO Ordering

**Validates: Requirements 4.3**

Items are dispatched in the order they were enqueued. For any two items A and B where A was enqueued before B, A's dispatch timestamp ≤ B's dispatch timestamp.

### Property 3: Cascading Completeness

**Validates: Requirements 5.1, 5.2, 5.3**

After drain, the total items processed equals seed items + all cascaded items generated. No work item is lost.

### Property 4: Error Isolation

**Validates: Requirements 6.1, 6.2**

A failing work item does not prevent other items from completing. After drain with K failures, `succeeded + failed == total` and `failed == K`.

### Property 5: 429 Backoff Correctness

**Validates: Requirements 17.1, 17.2, 17.3**

After NotifyRateLimited(N), no items are dispatched for at least N seconds. After resume, effective TPS ≤ 50% of prior active rate.

### Property 6: Drain Completeness

**Validates: Requirements 5.3, 6.4**

DrainAsync does not complete until all items (including cascaded) are finished. There are zero in-flight items when drain resolves.


## Discovery Tests — Restructured Design

### Structure Change

**Before:** 23 `[Test] [Order(N)]` methods with sequential `await` loops.
**After:** Single `[Test]` method that:
1. Creates `GameApiRequestQueue` with TPS=10.0.
2. Enqueues seed work items (15 independent endpoints).
3. Calls `queue.Start()`.
4. Awaits `queue.DrainAsync()`.
5. Collects results and writes `_metadata.json`.

### Seed Work Items

| # | Label | Endpoint | Cascades To |
|---|-------|----------|-------------|
| 1 | character/profile | GET /v1/character | none |
| 2 | character/skills | GET /v1/character/skills | none |
| 3 | colonies/list | GET /v1/colonies | colony detail (4 per colony) |
| 4 | banking/balance | GET /v1/banking/balance | none |
| 5 | banking/transactions-p0 | GET /v1/banking/transactions?page=0 | next page if non-empty |
| 6 | assets/locations | GET /v1/assets/locations | location detail per location |
| 7 | jobs/accepted | GET /v1/jobs/accepted | none |
| 8 | killmails/list | GET /v1/killmails | kill mail detail per mail |
| 9 | mail/list-p0 | GET /v1/mail?page=0 | next page + detail per ID |
| 10 | ship/configuration | GET /v1/ship/configuration | none |
| 11 | ship/cargo | GET /v1/ship/cargo | none |
| 12 | market/listings | GET /v1/market/listings | prices + components |
| 13 | market/items | GET /v1/market/items | none |
| 14 | market/buyorders | GET /v1/market/orders/buy | buy competitors |
| 15 | market/sellorders | GET /v1/market/orders/sell | sell competitors |

### Cascading Patterns

Colony: `list → 4 items per colonyId (summary, buildings, warehouse, workers)`
Assets: `locations → location detail per ID → crate/survey/blueprint detail per cargo item`
Kill Mails: `list → detail per killMailId`
Mail: `page N → page N+1 if non-empty + detail per mailId on page`
Banking: `transactions page N → page N+1 if non-empty; combine after drain`
Market: `listings → prices + components; buyorders → buy-competitors; sellorders → sell-competitors`

### Thread-Safe Result Collection

Replace `List<EndpointResult>` with `ConcurrentBag<EndpointResult>` for concurrent `RecordSuccess`/`RecordSkipped` calls from parallel work items.

## File Manifest

| File | Action | Purpose |
|------|--------|---------|
| `OE2EmpireTracker.Common/Models/GameApiConnectionSettings.cs` | Modify | Add `Tps` property |
| `OE2EmpireTracker.Common/Services/GameApiRequestQueue.cs` | Create | Queue service |
| `OE2EmpireTracker.Common/Services/WorkItem.cs` | Create | WorkItem, QueueError, QueueCompletionStatus |
| `OE2EmpireTracker/Forms/FormPreferences/FormPreferences.cs` | Modify | TPS input handling |
| `OE2EmpireTracker/Forms/FormPreferences/FormPreferences.Designer.cs` | Modify | Add controls |
| `OE2EmpireTracker.Tests/Client/GameApiFullDiscoveryTests.cs` | Rewrite | Queue-driven test |
| `OE2EmpireTracker.Tests/Services/GameApiRequestQueueTests.cs` | Create | Unit tests |
| `OE2EmpireTracker.Tests/Services/GameApiRequestQueuePropertyTests.cs` | Create | Property tests |
| `OE2EmpireTracker.Tests/Services/TokenBucketGovernorTests.cs` | Create | Governor tests |
| `OE2EmpireTracker.Tests/Services/AdaptiveRateControllerTests.cs` | Create | 429 handling tests |

## Dependencies

No new NuGet packages required. Uses existing framework types:
- `System.Collections.Concurrent` (ConcurrentQueue, ConcurrentBag)
- `System.Threading` (SemaphoreSlim, Interlocked, CancellationToken)
- `NLog` for logging
- `FsCheck 2.16.6` + `FsCheck.NUnit` for property-based tests

## Design Decisions

1. **Single dispatch loop vs. multiple dispatchers:** Single loop simplifies token bucket accounting and ensures strict FIFO. Concurrency comes from fire-and-forget of executed work items, not multiple dispatch threads.

2. **Nested classes vs. separate files:** TokenBucketGovernor, AdaptiveRateController, and CompletionTracker are private nested classes of GameApiRequestQueue to keep the public API surface minimal.

3. **WorkItem uses delegate instead of interface:** `Func<CancellationToken, Task<IReadOnlyList<WorkItem>>>` is simpler for test fixture lambdas than a class hierarchy.

4. **Re-enqueue on 429 vs. retry inline:** Re-enqueue puts the item at the back of the queue for fairness when many items are pending.

5. **ConcurrentBag for results vs. locking List:** No ordering guarantee needed — results are sorted for _metadata.json output.

6. **Dual-layer rate limiting (transition design):** The queue's token bucket and GameApiClient's internal SemaphoreSlim both remain active. At TPS=10, the client's 30/min limiter becomes the effective bottleneck. This is intentional during transition — the client limiter will be removed in a future spec.
