# Requirements Document

## Introduction

This feature adds a dedicated Game API Status form to OE2 Empire Tracker that provides detailed visibility into API request metrics. The existing status bar shows only basic connection state and last sync time, but with the game API's low TPS (transactions per second) and the large number of sequential requests needed for full data pulls (assets across multiple locations, colonies, buildings, warehouses, workers), players need a dedicated form showing request throughput, outstanding requests, success/failure rates, and data transfer sizes. This form enables players to understand request pacing, diagnose connectivity issues, and monitor sync progress in real time.

## Glossary

- **Metrics_Collector**: The component that intercepts Game_API_Client requests and responses to record timing, size, status code, and outcome data for each API call.
- **Status_Form**: The WinForms MDI child form (FormGameApiStatus) that displays real-time and historical API metrics in a dedicated window.
- **Request_Record**: A single captured data point representing one completed or failed API request, including endpoint, HTTP status code, duration, bytes sent, bytes received, and timestamp.
- **TPS**: Transactions per second — the rolling average rate of completed API requests over a configurable time window.
- **Outstanding_Requests**: The count of API requests that have been dispatched but have not yet received a response or timed out.
- **Request_History**: A bounded collection of recent Request_Records used for computing rolling metrics and displaying in the form.
- **Game_API_Client**: The existing HTTP client component that communicates with the Outer Empires 2 game server API endpoints (defined in game-api-integration spec).
- **Connection_Monitor**: The existing component that tracks game API reachability via periodic token exchange attempts.
- **Sync_Scheduler**: The existing component that manages periodic polling intervals and coordinates data retrieval timing.

## Requirements

### Requirement 1: Metrics Collection

**User Story:** As a player, I want the application to collect detailed metrics on every game API request, so that I can see throughput, latency, and error information in the status form.

#### Acceptance Criteria

1. WHEN the Game_API_Client sends a request, THE Metrics_Collector SHALL record the request start time, endpoint URL, and HTTP method, and increment the Outstanding_Requests count.
2. WHEN the Game_API_Client receives a response, THE Metrics_Collector SHALL record the HTTP status code, response duration in milliseconds, bytes received (Content-Length header value if present, otherwise response body byte length), and decrement the Outstanding_Requests count.
3. IF a request fails with an exception (timeout, network error, circuit breaker rejection), THEN THE Metrics_Collector SHALL record the failure category (one of: "Timeout", "NetworkError", "CircuitBreakerRejection") and the exception message, decrement the Outstanding_Requests count, and classify the outcome as an error.
4. THE Metrics_Collector SHALL store each completed or failed request as a Request_Record in the Request_History collection.
5. THE Metrics_Collector SHALL limit the Request_History to the most recent 500 records, discarding the oldest records when the limit is exceeded.
6. THE Metrics_Collector SHALL raise a MetricsUpdated event after recording each request completion or failure, providing the updated metrics snapshot containing: current TPS, total request count, outcome counts (success, client error, server error, rate limited, exception), cumulative bytes sent, cumulative bytes received, current Outstanding_Requests count, and peak Outstanding_Requests count.

### Requirement 2: TPS Calculation and Graph

**User Story:** As a player, I want to see the current transactions-per-second rate and a rolling graph of TPS over time, so that I can understand how fast the tracker is consuming my API rate limit and observe request pacing patterns.

#### Acceptance Criteria

1. THE Metrics_Collector SHALL compute TPS as the count of completed requests (success and failure) within the last 60 seconds, divided by 60, regardless of how long the application has been running (yielding a lower value during the first 60 seconds of operation as the window fills).
2. WHEN a new Request_Record is added to the Request_History, THE Metrics_Collector SHALL recalculate the TPS value.
3. THE Metrics_Collector SHALL expose the current TPS value as a decimal with two decimal places of precision (e.g., 0.50 for 30 requests per minute).
4. THE Metrics_Collector SHALL maintain a rolling series of TPS samples taken at 1-second intervals, retaining the most recent 300 samples (5 minutes of history), initializing unoccupied sample slots to 0.00 until real samples are recorded.
5. THE Status_Form SHALL display a line graph of the TPS sample series, with time on the horizontal axis and TPS value on the vertical axis, updating every 1 second.
6. THE Status_Form TPS graph SHALL auto-scale the vertical axis to the maximum TPS value observed in the visible 300-sample window, with a minimum vertical axis maximum of 1.00 to prevent degenerate scaling when all values are near zero.

### Requirement 3: Request Counters

**User Story:** As a player, I want to see cumulative request counts broken down by outcome, so that I can assess overall API health at a glance.

#### Acceptance Criteria

1. THE Metrics_Collector SHALL maintain a cumulative count of total requests sent, starting at zero when the application starts and resetting to zero on counter reset.
2. THE Metrics_Collector SHALL maintain separate cumulative counts for successful responses (HTTP 2xx), client errors (HTTP 4xx), server errors (HTTP 5xx), and exceptions (timeout, network, circuit breaker), where the total requests count equals the sum of these four category counts at all times.
3. THE Metrics_Collector SHALL maintain a count of HTTP 429 (rate limited) responses as a distinct sub-counter; each 429 response SHALL increment both the client errors (4xx) count and the rate-limited (429) count.
4. WHEN the user triggers a counter reset from the Status_Form, THE Metrics_Collector SHALL reset all cumulative counters (total, 2xx, 4xx, 5xx, exceptions, 429, peak outstanding, bytes sent, bytes received) to zero, clear the Request_History, and clear the TPS sample series.
5. IF the Game_API_Client receives an HTTP response with a status code outside the 2xx, 4xx, and 5xx ranges (e.g., 3xx), THEN THE Metrics_Collector SHALL classify it as a successful response and increment the 2xx counter.

### Requirement 4: Data Transfer Tracking

**User Story:** As a player, I want to see how much data is being transferred to and from the game API, so that I can understand bandwidth usage.

#### Acceptance Criteria

1. THE Metrics_Collector SHALL track cumulative bytes received by measuring the response body length in bytes (excluding HTTP headers) for all API responses since the last counter reset.
2. THE Metrics_Collector SHALL track cumulative bytes sent by measuring the request body length in bytes for all requests that include a body (POST, PUT, PATCH) since the last counter reset.
3. THE Status_Form SHALL display data transfer totals using automatic unit selection: values below 1024 bytes displayed as whole bytes (e.g., "512 B"), values from 1024 bytes to below 1,048,576 bytes displayed as KB with two decimal places (e.g., "4.50 KB"), and values at or above 1,048,576 bytes displayed as MB with two decimal places (e.g., "12.34 MB").
4. IF a response does not provide a Content-Length header (e.g., chunked transfer encoding), THEN THE Metrics_Collector SHALL measure bytes received by counting the actual response body bytes read.

### Requirement 5: Outstanding Request Tracking

**User Story:** As a player, I want to see how many requests are currently in-flight, so that I can understand whether the client is waiting on the server.

#### Acceptance Criteria

1. THE Metrics_Collector SHALL maintain a count of requests that have been sent but have not yet received a response or timed out, where the count is incremented by 1 on each request dispatch and decremented by 1 on each response receipt or timeout, and SHALL never report a value below zero.
2. THE Status_Form SHALL display the current Outstanding_Requests count, refreshing the displayed value within 1 second of any change.
3. THE Metrics_Collector SHALL expose the peak Outstanding_Requests value observed since the last counter reset, updating the peak whenever the current count exceeds the previously recorded peak.
4. WHEN the user triggers a counter reset, THE Metrics_Collector SHALL reset the peak Outstanding_Requests value to the current Outstanding_Requests count (preserving the count of still-in-flight requests).

### Requirement 6: Status Form Display

**User Story:** As a player, I want a dedicated form showing all API metrics in an organized layout, so that I can monitor API activity without cluttering the main status bar.

#### Acceptance Criteria

1. THE Status_Form SHALL display the following metrics in a summary panel: current TPS, total requests, outstanding requests, connection state, and last sync time.
2. THE Status_Form SHALL display request outcome counts in a breakdown panel: successful (2xx), client errors (4xx), server errors (5xx), rate limited (429), and exceptions.
3. THE Status_Form SHALL display data transfer totals in a transfer panel: total bytes sent, total bytes received.
4. THE Status_Form SHALL display a scrollable list of Request_Records from the Request_History (up to 500 entries), ordered newest-first, showing: timestamp (HH:mm:ss.fff), HTTP method, endpoint relative path (path portion of the URL without scheme, host, or query string), status code, duration (ms), and bytes received.
5. THE Status_Form SHALL update all displayed metrics by subscribing to the MetricsUpdated event and refreshing the display within 1 second of each event, using a timer-based coalescing interval of 1 second to avoid excessive UI updates.
6. THE Status_Form SHALL display the current rate limit configuration (requests per minute allowed) and the rate limiter state: "Active" when requests are flowing normally, or "Paused until {timestamp}" when the rate limiter is waiting due to an HTTP 429 response.
7. WHEN the Status_Form is opened and no requests have been recorded, THE Status_Form SHALL display zero values for all counters, an empty Request_Records list, and "N/A" for last sync time.

### Requirement 7: Form Lifecycle

**User Story:** As a player, I want to open and close the API status form without affecting API operations, so that monitoring is optional and non-intrusive.

#### Acceptance Criteria

1. THE Status_Form SHALL be accessible from the main menu under the Tools menu as "Game API Status".
2. THE Status_Form SHALL open as an MDI child form within the main application window, following the existing MDI child form pattern.
3. WHEN the Status_Form is opened, THE Status_Form SHALL subscribe to the Metrics_Collector MetricsUpdated event and immediately display the current metrics snapshot without waiting for new requests.
4. WHEN the Status_Form is closed, THE Status_Form SHALL unsubscribe from the Metrics_Collector MetricsUpdated event, and THE Metrics_Collector SHALL continue collecting metrics independently of the form lifecycle.
5. THE Status_Form SHALL implement the existing form patterns: IProgrammaticUpdateSource interface, NLog logger, and window state persistence via WindowStateHelper.
6. IF the Status_Form is already open when the user selects the menu item, THEN THE application SHALL activate the existing instance (making it the active MDI child) rather than opening a duplicate.

### Requirement 8: Reset and Controls

**User Story:** As a player, I want to reset the metrics counters, so that I can measure API activity for a specific time period or sync cycle.

#### Acceptance Criteria

1. THE Status_Form SHALL provide a "Reset Counters" button that resets all cumulative counters (total requests, successful, client errors, server errors, rate limited, exceptions, bytes sent, bytes received, peak outstanding requests) to zero, clears the Request_History, and clears the TPS sample series.
2. WHEN the user clicks "Reset Counters", THE Status_Form SHALL display the reset timestamp in local date-time format (e.g., "2024-03-15 14:32:07") so the user knows when the current measurement period started.
3. THE Status_Form SHALL provide a "Copy to Clipboard" button that copies the current metrics summary as plain text including: reset timestamp, total requests, outcome breakdown (2xx, 4xx, 5xx, 429, exceptions), current TPS, data transfer totals (bytes sent, bytes received), outstanding requests, and circuit breaker state — one metric per line with label and value.
4. WHEN the user clicks "Copy to Clipboard" and the operation succeeds, THE Status_Form SHALL display a brief visual confirmation (e.g., button text changes to "Copied!" for 2 seconds) indicating the content was placed on the clipboard.
5. IF the clipboard operation fails, THEN THE Status_Form SHALL display an error message indicating the clipboard could not be accessed.

### Requirement 9: Circuit Breaker and Rate Limiter Visibility

**User Story:** As a player, I want to see the circuit breaker and rate limiter states, so that I can understand why requests might be paused or failing.

#### Acceptance Criteria

1. THE Status_Form SHALL display the circuit breaker state as one of three labeled values: "Closed" (normal operation), "Open" (blocking requests), or "Half-Open" (probing with a single test request).
2. WHILE the circuit breaker is in the Open state, THE Status_Form SHALL display a countdown showing the time remaining until the half-open probe attempt, formatted as minutes and seconds (e.g., "1m 23s"), updating every 1 second.
3. THE Status_Form SHALL display the configured rate limit in requests per minute, and WHEN the server-communicated limit (from the Retry-After or rate limit headers) differs from the configured limit, THE Status_Form SHALL display both values labeled (e.g., "Configured: 30/min | Server: 20/min").
4. WHILE the rate limiter is paused due to HTTP 429, THE Status_Form SHALL display the text "Rate Limited" and the pause-until time formatted as a local clock time (HH:mm:ss).
5. WHEN the circuit breaker transitions from Open to Half-Open or Closed, THE Status_Form SHALL hide the countdown and display only the current state label.
