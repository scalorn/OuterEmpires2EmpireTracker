# Implementation Plan

## Overview

This plan implements the Game API Status Form feature, adding a `GameApiMetricsCollector` service that intercepts all `GameApiClient` requests to record timing, size, status, and outcome data, and a `FormGameApiStatus` MDI child form that displays real-time and historical API metrics. Tasks are organized in waves: foundational models first, then collector logic, then GameApiClient integration, then form UI, and finally tests.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": "wave-1", "name": "Foundation Models", "tasks": ["1", "2", "9"] },
    { "id": "wave-2", "name": "Collector Core", "tasks": ["3"] },
    { "id": "wave-3", "name": "TPS Calculation", "tasks": ["4"] },
    { "id": "wave-4", "name": "Client Integration", "tasks": ["5"] },
    { "id": "wave-5", "name": "Property Tests", "tasks": ["6", "7", "8"] },
    { "id": "wave-6", "name": "Form Layout", "tasks": ["10"] },
    { "id": "wave-7", "name": "Form Display Logic", "tasks": ["11"] },
    { "id": "wave-8", "name": "TPS Graph and History", "tasks": ["12"] },
    { "id": "wave-9", "name": "Controls and State", "tasks": ["13"] },
    { "id": "wave-10", "name": "Menu Integration", "tasks": ["14"] },
    { "id": "wave-11", "name": "Form Unit Tests", "tasks": ["15"] }
  ]
}
```

## Tasks

- [x] 1. Create RequestRecord model
  - [x] 1.1 Create immutable `RequestRecord` class with constructor and read-only properties: Timestamp, Endpoint, HttpMethod, StatusCode, DurationMs, BytesSent, BytesReceived, IsSuccess, FailureCategory, ErrorMessage
  - Satisfies: Req 1, Criteria 1-4 ("record request start time, endpoint, method... record HTTP status code, duration, bytes... record failure category and message... store as RequestRecord")
  - Inputs: design.md (RequestRecord section)
  - Output: `OE2EmpireTracker.Common/Models/RequestRecord.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 2. Create MetricsSnapshot model
  - [x] 2.1 Create immutable `MetricsSnapshot` class with constructor and read-only properties: CurrentTps, TotalRequests, SuccessCount, ClientErrorCount, ServerErrorCount, ExceptionCount, RateLimitedCount, BytesSent, BytesReceived, OutstandingRequests, PeakOutstanding, ResetTimestamp
  - Satisfies: Req 1, Criterion 6 ("MetricsUpdated event providing updated metrics snapshot")
  - Inputs: design.md (MetricsSnapshot section)
  - Output: `OE2EmpireTracker.Common/Models/MetricsSnapshot.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings


- [x] 9. Create RateLimitState and CircuitBreakerState models
  - [x] 9.1 Create `RateLimitState` class with properties: ConfiguredRequestsPerMinute, ServerRequestsPerMinute (int?), IsPaused, PauseUntil (DateTime?)
  - [x] 9.2 Create `CircuitBreakerState` class with properties: State ("Closed"/"Open"/"Half-Open"), OpenedAt (DateTime?), BreakDuration (TimeSpan?), computed TimeUntilHalfOpen property
  - Satisfies: Req 9, Criteria 1-5 ("circuit breaker state display, countdown, rate limit display")
  - Inputs: design.md (RateLimitState, CircuitBreakerState sections)
  - Output: `OE2EmpireTracker.Common/Models/RateLimitState.cs`, `OE2EmpireTracker.Common/Models/CircuitBreakerState.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 3. Create GameApiMetricsCollector core (counters, history, outstanding tracking)
  - [x] 3.1 Create singleton `GameApiMetricsCollector` with thread-safe lock, cumulative counters (total, success, clientError, serverError, exception, rateLimited, bytesSent, bytesReceived), outstanding count, peak outstanding, and bounded history list (max 500)
  - [x] 3.2 Implement `OnRequestStarted(endpoint, method)` — increment outstanding, update peak
  - [x] 3.3 Implement `OnRequestCompleted(endpoint, method, statusCode, durationMs, bytesSent, bytesReceived)` — classify status code (2xx/3xx→success, 4xx→clientError, 429→also rateLimited, 5xx→serverError), create RequestRecord, trim history, decrement outstanding (floor 0), raise MetricsUpdated
  - [x] 3.4 Implement `OnRequestFailed(endpoint, method, failureCategory, message)` — increment exception count, create RequestRecord, decrement outstanding (floor 0), raise MetricsUpdated
  - [x] 3.5 Implement `ResetCounters()` — zero all counters, clear history, set peak to current outstanding, set resetTimestamp
  - [x] 3.6 Implement `GetCurrentSnapshot()` and `GetHistory()` public accessors
  - Satisfies: Req 1, Criteria 1-5 (request recording, history bounded to 500); Req 3, Criteria 1-3 (cumulative counters, 429 sub-counter); Req 5, Criteria 1,3 (outstanding count, peak tracking)
  - Inputs: design.md (GameApiMetricsCollector section), RequestRecord.cs, MetricsSnapshot.cs
  - Output: `OE2EmpireTracker.Common/Services/GameApiMetricsCollector.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 4. Add TPS calculation and sampling to GameApiMetricsCollector
  - [x] 4.1 Add 1-second `System.Threading.Timer` that computes TPS (requests completed in last 60 seconds / 60) and stores in circular buffer of 300 decimal samples
  - [x] 4.2 Implement `GetTpsSamples()` returning a copy of the 300-sample array ordered oldest-to-newest
  - [x] 4.3 Recalculate TPS on each new RequestRecord addition (for immediate snapshot accuracy)
  - Satisfies: Req 2, Criteria 1-4 (TPS as requests in last 60s / 60, recalculate on new record, two decimal places, 300-sample circular buffer at 1s intervals)
  - Inputs: GameApiMetricsCollector.cs (from Task 3)
  - Output: `OE2EmpireTracker.Common/Services/GameApiMetricsCollector.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 5. Integrate metrics hooks into GameApiClient
  - [x] 5.1 Add `OnRequestStarted` / `OnRequestCompleted` / `OnRequestFailed` calls in `ExecuteWithPoliciesAsync` with null-check on Instance
  - [x] 5.2 Add `ExtractRelativePath(string requestUrl)` helper method
  - [x] 5.3 Add `GameApiMetricsCollector.Initialize()` call in `GameApiContext.Initialize()` and `GameApiMetricsCollector.Reset()` in `GameApiContext.Reset()`
  - Satisfies: Req 1, Criteria 1-3 (record on send, record on response, record on failure)
  - Inputs: design.md (GameApiClient Extensions section), existing GameApiClient.cs, GameApiContext.cs
  - Output: `OE2EmpireTracker.Common/Client/GameApiClient.cs` (modified), `OE2EmpireTracker.Common/Client/GameApiContext.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings


- [x] 6. Property tests: counter consistency and rate-limited sub-counter
  - [x] 6.1 Property test: TotalRequests == SuccessCount + ClientErrorCount + ServerErrorCount + ExceptionCount after random sequences of OnRequestCompleted/OnRequestFailed with various status codes (Property 1)
  - [x] 6.2 Property test: Every 429 response increments both ClientErrorCount and RateLimitedCount; RateLimitedCount <= ClientErrorCount always holds (Property 6)
  - [x] 6.3 Property test: Status codes outside 2xx/4xx/5xx (e.g. 3xx) are classified as success and increment SuccessCount
  - Satisfies: Req 3, Criterion 2 ("total equals sum of four categories"); Req 3, Criterion 3 ("429 increments both 4xx and rate-limited"); Req 3, Criterion 5 ("status outside 2xx/4xx/5xx classified as 2xx")
  - Inputs: GameApiMetricsCollector.cs
  - Output: `OE2EmpireTracker.Tests/Services/GameApiMetricsCollectorCounterTests.cs`
  - Verification: `vstest.console` — all tests pass

- [x] 7. Property tests: outstanding non-negativity and history boundedness
  - [x] 7.1 Property test: Outstanding count never goes negative after random interleaved OnRequestStarted/OnRequestCompleted/OnRequestFailed sequences including more completions than starts (Property 2)
  - [x] 7.2 Property test: GetHistory().Count <= 500 after generating 1–2000 random request completions (Property 3)
  - Satisfies: Req 5, Criterion 1 ("never report below zero"); Req 1, Criterion 5 ("limit history to 500")
  - Inputs: GameApiMetricsCollector.cs
  - Output: `OE2EmpireTracker.Tests/Services/GameApiMetricsCollectorOutstandingTests.cs`
  - Verification: `vstest.console` — all tests pass

- [x] 8. Property tests: TPS window accuracy and reset idempotency
  - [x] 8.1 Property test: TPS equals N/60 (within tolerance) when N requests are recorded within a frozen 60-second window using SystemClock (Property 4)
  - [x] 8.2 Property test: Calling ResetCounters() twice produces same state as calling once — all counters zero, history empty, TPS samples cleared (Property 5)
  - Satisfies: Req 2, Criterion 1 ("TPS as count in last 60s / 60"); Req 3, Criterion 4 ("reset all counters to zero"); Req 8, Criterion 1 ("reset clears history and TPS samples")
  - Inputs: GameApiMetricsCollector.cs
  - Output: `OE2EmpireTracker.Tests/Services/GameApiMetricsCollectorTpsResetTests.cs`
  - Verification: `vstest.console` — all tests pass

- [x] 10. Create FormGameApiStatus Designer layout
  - [x] 10.1 Create `FormGameApiStatus.cs` partial class implementing `IProgrammaticUpdateSource` with NLog logger, `_isProgrammaticUpdate` field, BeginProgrammaticUpdate/EndProgrammaticUpdate
  - [x] 10.2 Create `FormGameApiStatus.Designer.cs` with panels (pnlSummary, pnlTpsGraph, pnlBreakdown, pnlTransfer, pnlState, pnlControls), labels (lblTps, lblTotalRequests, lblOutstanding, lblPeakOutstanding, lblConnectionState, lblLastSync, lblSuccess, lblClientErrors, lblServerErrors, lblRateLimited, lblExceptions, lblBytesSent, lblBytesReceived, lblCircuitBreaker, lblRateLimit, lblRateLimitState, lblResetTimestamp), buttons (btnReset, btnCopyToClipboard), and DataGridView (dgvHistory with columns: colTime, colMethod, colEndpoint, colStatus, colDuration, colBytes)
  - [x] 10.3 Create `FormGameApiStatus.resx` resource file
  - Satisfies: Req 6, Criteria 1-4 (summary panel, breakdown panel, transfer panel, history grid); Req 7, Criterion 5 (IProgrammaticUpdateSource, NLog, WindowStateHelper)
  - Inputs: design.md (Form Layout, Control Inventory sections)
  - Output: `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs`, `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.Designer.cs`, `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.resx`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings


- [x] 11. Implement FormGameApiStatus display logic and event subscription
  - [x] 11.1 Implement constructor: create `_refreshTimer` (1000ms interval), create `_countdownTimer` (1000ms interval), subscribe to `GameApiMetricsCollector.Instance.MetricsUpdated`, call `GetCurrentSnapshot()` for initial display
  - [x] 11.2 Implement `OnMetricsUpdated` handler — set `_refreshPending = true` (coalesced by timer)
  - [x] 11.3 Implement `RefreshTimer_Tick` — if `_refreshPending`, call `UpdateDisplay()` with latest snapshot, reset flag
  - [x] 11.4 Implement `UpdateDisplay(MetricsSnapshot)` — update all labels with snapshot values, call `FormatBytes()` for transfer labels
  - [x] 11.5 Implement `FormatBytes(long bytes)` — return "N B" / "N.NN KB" / "N.NN MB" based on thresholds (1024, 1048576)
  - [x] 11.6 Implement `OnFormClosed` — unsubscribe from MetricsUpdated, stop timers, save window state
  - Satisfies: Req 6, Criterion 5 ("subscribe to MetricsUpdated, refresh within 1 second via timer coalescing"); Req 6, Criterion 7 ("display zero values when no requests recorded"); Req 7, Criteria 3-4 ("subscribe on open, unsubscribe on close, collector continues independently")
  - Inputs: FormGameApiStatus.Designer.cs (from Task 10), GameApiMetricsCollector.cs
  - Output: `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 12. Implement TPS graph rendering and history grid population
  - [x] 12.1 Implement `PnlTpsGraph_Paint` — GDI+ line graph with auto-scaled Y axis (minimum 1.00), grid lines, anti-aliased line from 300 TPS samples
  - [x] 12.2 Implement `PopulateHistoryGrid(IReadOnlyList<RequestRecord>)` — populate dgvHistory rows newest-first with timestamp (HH:mm:ss.fff), method, endpoint relative path, status code, duration (ms), bytes received
  - [x] 12.3 Wire `pnlTpsGraph.Paint` event and call `pnlTpsGraph.Invalidate()` from refresh timer
  - Satisfies: Req 2, Criteria 5-6 ("line graph of TPS samples, auto-scale vertical axis, minimum 1.00"); Req 6, Criterion 4 ("scrollable list of RequestRecords, newest-first")
  - Inputs: FormGameApiStatus.cs (from Task 11), design.md (TPS Graph Rendering section)
  - Output: `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 13. Implement reset, copy-to-clipboard, and circuit breaker/rate limiter display
  - [x] 13.1 Implement `BtnReset_Click` — call `GameApiMetricsCollector.Instance.ResetCounters()`, display reset timestamp in lblResetTimestamp
  - [x] 13.2 Implement `BtnCopyToClipboard_Click` — format metrics summary as plain text (one metric per line), copy to clipboard, show "Copied!" for 2 seconds on success, show error message on failure
  - [x] 13.3 Implement `UpdateCircuitBreakerDisplay()` — show state label, countdown when Open (formatted as "Xm Ys"), hide countdown when Closed/Half-Open
  - [x] 13.4 Implement `UpdateRateLimiterDisplay()` — show configured rate, server rate if different, "Rate Limited" + pause-until time (HH:mm:ss) when paused
  - [x] 13.5 Implement `CountdownTimer_Tick` — update circuit breaker countdown every 1 second while Open
  - Satisfies: Req 8, Criteria 1-5 ("Reset Counters button, display reset timestamp, Copy to Clipboard, visual confirmation, error on failure"); Req 9, Criteria 1-5 ("CB state, countdown, rate limit display, paused state"); Req 6, Criterion 6 ("rate limit config and state")
  - Inputs: FormGameApiStatus.cs (from Task 12), RateLimitState.cs, CircuitBreakerState.cs
  - Output: `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 14. Add MainWindow menu integration and single-instance pattern
  - [x] 14.1 Add `toolsGameApiStatusMenuItem` to Tools menu in MainWindow.Designer.cs
  - [x] 14.2 Implement `ToolsGameApiStatusMenuItem_Click` — check MdiChildren for existing FormGameApiStatus, activate if found, otherwise create new instance with MdiParent, WindowStateHelper.RestoreState, and Show
  - Satisfies: Req 7, Criteria 1-2 ("accessible from Tools menu, MDI child"); Req 7, Criterion 6 ("activate existing instance rather than opening duplicate")
  - Inputs: design.md (MainWindow Menu Integration section), existing MainWindow.cs
  - Output: `OE2EmpireTracker/Forms/MainWindow.cs` (modified), `OE2EmpireTracker/Forms/MainWindow.Designer.cs` (modified)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [x] 15. Form unit tests (FormatBytes, clipboard text format)
  - [x] 15.1 Unit tests for `FormatBytes`: verify "512 B" for 512, "4.50 KB" for 4608, "12.34 MB" for 12939264, "0 B" for 0, "1023 B" for 1023, "1.00 KB" for 1024, "1.00 MB" for 1048576
  - [x] 15.2 Unit tests for clipboard text format: verify output contains all expected labels and values, one metric per line
  - Satisfies: Req 4, Criterion 3 ("automatic unit selection: B, KB, MB"); Req 8, Criterion 3 ("copy metrics summary as plain text")
  - Inputs: FormGameApiStatus.cs
  - Output: `OE2EmpireTracker.Tests/Forms/FormGameApiStatusTests.cs`
  - Verification: `vstest.console` — all tests pass

## Notes

- All property tests use FsCheck 2.16.6 APIs (LINQ query syntax generators, `[FsCheck.NUnit.Property]` attribute). No 3.x APIs.
- Tests use `SystemClock.UtcNow` for deterministic time control.
- The metrics collector is initialized in `GameApiContext.Initialize()` regardless of whether the form is open.
- The form uses timer-based coalescing (1-second interval) to prevent excessive UI updates.
- No new NuGet packages required — uses existing System.Drawing, NLog, Polly.
