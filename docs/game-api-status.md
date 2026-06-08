# Game API Status

The Game API Status form shows detailed metrics about communication between the tracker and the game's REST API. Use it to understand request pacing, diagnose connectivity issues, and monitor background sync progress.

## Opening the Form

Open **Tools → Game API Status** from the main menu.

## Summary Panel

The top row shows at-a-glance metrics:

- **TPS** — Transactions per second (rolling 60-second average)
- **Total** — Total requests since last reset
- **Outstanding** — Requests currently in-flight (sent but no response yet)
- **Peak** — Highest outstanding count observed since reset
- **Connection** — Current API connection state
- **Last Sync** — When the last background sync completed

## TPS Graph

A real-time line graph showing TPS over the last 5 minutes (300 one-second samples). The vertical axis auto-scales to the maximum value in the visible window.

## Request Breakdown

Shows cumulative counts by HTTP response category:

- **2xx** — Successful responses
- **4xx** — Client errors
- **5xx** — Server errors
- **429** — Rate-limited responses (subset of 4xx)
- **Exceptions** — Timeouts, network errors, circuit breaker rejections

## Data Transfer

Shows total bytes sent (request bodies) and received (response bodies) since last reset. Values display in B, KB, or MB as appropriate.

## Circuit Breaker and Rate Limiter

- **Circuit Breaker** — Shows Closed (normal), Open (blocking, with countdown), or Half-Open (probing)
- **Rate Limit** — Configured requests/minute; shows server limit if different
- **Rate Limiter** — Active (normal) or "Rate Limited" with pause-until time when a 429 was received

## Request History

A scrollable grid showing up to 500 recent requests with:

- **Time** — Timestamp (HH:mm:ss.fff)
- **Method** — HTTP method (GET, POST, etc.)
- **Endpoint** — Relative URL path
- **Status** — HTTP status code
- **Duration** — Response time in milliseconds
- **Bytes** — Response body size

Newest requests appear at the top.

## Controls

### Reset Counters

Click **Reset Counters** to clear all cumulative metrics and start fresh. The reset timestamp shows when the current measurement period began.

### Copy to Clipboard

Click **Copy to Clipboard** to copy a plain-text summary of all current metrics. Useful for sharing diagnostics or bug reports.

## Test API Panel

The bottom section lets you manually test API calls:

1. Enter a **Location ID** (numeric)
2. Select a **Type** from the dropdown (Co = Colony, St = Station, Sh = Ship, Cr = Character)
3. Click **Fetch** to make the API call

The raw JSON response appears in the result area below. This is useful for verifying API connectivity and inspecting response data.

## Form Lifecycle

- The form subscribes to metrics updates when opened and unsubscribes when closed
- Metrics collection continues in the background regardless of whether the form is open
- Opening the form immediately shows the current state without waiting for new requests
- Only one instance can be open at a time; re-selecting the menu item activates the existing window
