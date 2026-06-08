# Game API Status Mockups

### FormGameApiStatus

MDI child form. Stacked panels: summary, TPS graph, breakdown/transfer/state row, controls, request history grid, and test API panel at bottom.

```
+---------------------------------------------------------------------------------+
| Game API Status                                                  [_][box][X]    |
+---------------------------------------------------------------------------------+
| TPS: 0.50       Total: 127       Outstanding: 2       Peak: 5                  |
| Connection: Connected            Last Sync: 2024-01-31 14:30                    |
+---------------------------------------------------------------------------------+
| [TPS Graph - white panel showing line graph over 5 minutes]                     |
|     1.0 |          /\                                                           |
|     0.5 |    /\  /    \  /\                                                     |
|     0.0 |___/  \/      \/  \___                                                 |
|         +---------------------------------------------------> Time              |
+---------------------------------------------------------------------------------+
| 2xx: 120       | Sent: 4.50 KB    | Circuit Breaker: Closed                     |
| 4xx: 5         | Received: 1.2 MB | Rate Limit: 30/min                          |
| 5xx: 0         |                  | Rate Limiter: Active                        |
| 429: 2         |                  |                                             |
| Exceptions: 0  |                  |                                             |
+---------------------------------------------------------------------------------+
| [Reset Counters] [Copy to Clipboard]  Reset at: 2024-01-31 14:00               |
+---------------------------------------------------------------------------------+
| Time        | Method | Endpoint                | Status | Duration | Bytes      |
|-------------|--------|-------------------------|--------|----------|------------|
| 14:30:12.45 | GET    | /v1/banking/balance     | 200    | 145      | 256        |
| 14:30:11.20 | GET    | /v1/banking/transactions| 200    | 892      | 4,512      |
| 14:30:10.05 | GET    | /v1/assets/123/Co       | 429    | 50       | 0          |
+---------------------------------------------------------------------------------+
| Location ID:[____] Type:[Co v] [Fetch]                                          |
| +--- Response -----------------------------------------------------------+      |
| | {"assets": [...], "status": "ok"}                                      |      |
| +------------------------------------------------------------------------+      |
+---------------------------------------------------------------------------------+
```

### Control List — FormGameApiStatus

| Control | Type | Purpose |
|---------|------|---------|
| pnlSummary | Panel (Dock=Top) | Summary metrics row |
| lblTps | Label | Current TPS value |
| lblTotalRequests | Label | Total request count |
| lblOutstanding | Label | Current outstanding requests |
| lblPeakOutstanding | Label | Peak outstanding since reset |
| lblConnectionState | Label | API connection state |
| lblLastSync | Label | Last sync timestamp |
| pnlTpsGraph | Panel (Dock=Top, white bg) | TPS line graph canvas |
| pnlMiddle | Panel (Dock=Top) | Container for breakdown/transfer/state |
| pnlBreakdown | Panel (Dock=Left) | Request outcome counts |
| lblSuccess | Label | 2xx count |
| lblClientErrors | Label | 4xx count |
| lblServerErrors | Label | 5xx count |
| lblRateLimited | Label | 429 count |
| lblExceptions | Label | Exception count |
| pnlTransfer | Panel (Dock=Left) | Data transfer totals |
| lblBytesSent | Label | Bytes sent total |
| lblBytesReceived | Label | Bytes received total |
| pnlState | Panel (Dock=Fill) | Circuit breaker and rate limiter state |
| lblCircuitBreaker | Label | Circuit breaker state |
| lblRateLimit | Label | Rate limit configuration |
| lblRateLimitState | Label | Rate limiter active/paused state |
| pnlControls | Panel (Dock=Top) | Action buttons row |
| btnReset | Button | Reset all counters |
| btnCopyToClipboard | Button | Copy metrics summary to clipboard |
| lblResetTimestamp | Label | When counters were last reset |
| dgvHistory | DataGridView (Dock=Fill) | Request history grid |
| colTime | DataGridViewTextBoxColumn | Request timestamp (HH:mm:ss.fff) |
| colMethod | DataGridViewTextBoxColumn | HTTP method |
| colEndpoint | DataGridViewTextBoxColumn | Endpoint relative path |
| colStatus | DataGridViewTextBoxColumn | HTTP status code |
| colDuration | DataGridViewTextBoxColumn | Duration in milliseconds |
| colBytes | DataGridViewTextBoxColumn | Bytes received |
| pnlTestApi | Panel (Dock=Bottom) | Test API section |
| pnlTestApiInput | FlowLayoutPanel (Dock=Top) | Test input controls |
| lblTestLocationId | Label | "Location ID:" caption |
| txtTestLocationId | TextBox | Location ID input |
| lblTestLocationType | Label | "Type:" caption |
| cboTestLocationType | ComboBox (DropDownList) | Location type (Co/St/Sh/Cr) |
| btnTestFetch | Button | Execute test API call |
| txtTestResult | TextBox (Multiline, ReadOnly) | Raw API response display |
