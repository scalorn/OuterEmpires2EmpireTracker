# Design Document: API Manual Requests

## Overview

This feature replaces the existing single-endpoint "Test API" panel in FormGameApiStatus with a comprehensive manual request panel supporting all 28 Game_API_Client endpoints. The panel dynamically shows/hides input fields based on the selected endpoint's parameter requirements, validates inputs before sending, and displays formatted JSON responses. All requests flow through the existing GameApiClient (which enforces 0.80 TPS rate limiting via its internal token bucket).

The design preserves backward compatibility — "Location Detail" remains the default endpoint with the same ID + type inputs, so existing workflows are unaffected.

## Architecture

The implementation lives entirely within the existing FormGameApiStatus form. No new services, models, or API client changes are required. The architecture follows the existing form pattern:

```
GameApiClient (OE2EmpireTracker.Common/Client)
    ↑ calls
FormGameApiStatus (Forms/GameApiStatus/)
    ├── Designer.cs — panel layout, controls, endpoint dropdown
    ├── FormGameApiStatus.cs — endpoint routing, validation, execution
    └── ManualRequestEndpoint.cs — endpoint metadata (enum + helper)
```

### Key Decisions

1. **Endpoint metadata as a helper class** — A static `ManualRequestEndpoint` class defines each endpoint's display name, category (parameterless / single-ID / location / market-view / market-competitors), and the ID label text. This avoids a large switch statement and makes adding endpoints trivial.

2. **No new service layer** — The form already has access to `GameApiContext.Instance.Client` and the credential flow. Manual requests are a debugging tool, not a business operation. No persistence, no ViewModel.

3. **Dynamic field visibility in the form** — The panel shows/hides controls based on the selected endpoint's category. This matches the existing pattern in the form (controls shown/hidden based on state).

4. **Reuse existing controls where possible** — The existing `txtTestLocationId`, `cboTestLocationType`, `btnTestFetch`, and `txtTestResult` controls are renamed and repurposed. New controls are added for the endpoint dropdown, market parameters, and copy button.


## Components and Interfaces

### ManualRequestEndpoint (New — Helper Class)

**File:** `OE2EmpireTracker/Forms/GameApiStatus/ManualRequestEndpoint.cs`

Defines endpoint metadata for the manual request panel:

```csharp
public enum EndpointCategory
{
    Parameterless,   // No inputs needed
    SingleId,        // One numeric ID field
    LocationDetail,  // ID + location type dropdown
    MarketView,      // View (required) + optional filters
    MarketCompetitors // Market IDs (comma-separated)
}

public class ManualRequestEndpoint
{
    public string DisplayName { get; }
    public EndpointCategory Category { get; }
    public string IdLabel { get; }  // e.g. "Colony ID", "Blueprint ID"

    public static IReadOnlyList<ManualRequestEndpoint> All { get; }
    public static ManualRequestEndpoint FindByDisplayName(string name);
}
```

The `All` list defines all 28 endpoints in display order matching the requirements. Each entry carries enough metadata for the form to determine which controls to show.

### FormGameApiStatus (Modified)

**File:** `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs`

New/modified responsibilities:
- `cboEndpoint` — ComboBox populated from `ManualRequestEndpoint.All`
- `CboEndpoint_SelectedIndexChanged` — shows/hides parameter controls based on category
- `BtnRequest_Click` — validates, routes to correct GameApiClient method
- `BtnCopyResponse_Click` — copies response text to clipboard with visual feedback
- `ValidateAndExecuteAsync()` — central validation + execution method

### FormGameApiStatus.Designer.cs (Modified)

New controls added to `pnlTestApi` / `pnlTestApiInput`:
- `cboEndpoint` — endpoint selector dropdown (replaces implicit Location Detail only)
- `lblEndpoint` — "Endpoint:" label
- `txtId` — generic ID input (replaces `txtTestLocationId`)
- `lblId` — dynamic label (shows "Colony ID", "Blueprint ID", etc.)
- `cboLocationType` — retained from existing (visible only for Location Detail)
- `lblLocationType` — retained from existing
- `txtView` — market view input
- `lblView` — "View:" label
- `txtSearch` — market search filter
- `lblSearch` — "Search:" label
- `txtType` — market type filter
- `lblType` — "Type:" label
- `txtSubType` — market sub-type filter
- `lblSubType` — "Sub Type:" label
- `txtOrderBy` — market order-by field
- `lblOrderBy` — "Order By:" label
- `txtRange` — market range (numeric)
- `lblRange` — "Range:" label
- `txtEvolution` — market evolution (numeric)
- `lblEvolution` — "Evolution:" label
- `txtMarketIds` — comma-separated market IDs
- `lblMarketIds` — "Market IDs:" label
- `btnRequest` — "Request" button (replaces `btnTestFetch`)
- `btnCopyResponse` — "Copy Response" button
- `txtResponse` — response display (replaces `txtTestResult`)

**Layout approach:** The `pnlTestApiInput` FlowLayoutPanel is expanded to two rows. Row 1: endpoint dropdown + ID field + location type (when applicable) + Request button. Row 2 (visible only for market endpoints): View, Search, Type, SubType, OrderBy, Range, Evolution, Market IDs fields. The response text area fills the remaining space below.


## Data Models

No new data models are required. This feature operates entirely in-memory with no persistence. The endpoint metadata is defined as a static read-only collection in `ManualRequestEndpoint`.

### ManualRequestEndpoint Static Data

```csharp
// Parameterless endpoints
("Character", Parameterless, null),
("Character Skills", Parameterless, null),
("Colony List", Parameterless, null),
("Banking Balance", Parameterless, null),
("Banking Transactions", Parameterless, null),
("Accepted Jobs", Parameterless, null),
("Asset Locations", Parameterless, null),
("Kill Mail List", Parameterless, null),
("Ship Configuration", Parameterless, null),
("Ship Cargo", Parameterless, null),
("Mail List", Parameterless, null),
("Market Buy Orders", Parameterless, null),
("Market Sell Orders", Parameterless, null),

// Single-ID endpoints
("Colony Buildings", SingleId, "Colony ID"),
("Colony Warehouse", SingleId, "Colony ID"),
("Colony Workers", SingleId, "Colony ID"),
("Colony Summary", SingleId, "Colony ID"),
("Kill Mail Detail", SingleId, "Kill Mail ID"),
("Mail Detail", SingleId, "Mail ID"),
("Blueprint", SingleId, "Blueprint ID"),
("Survey", SingleId, "Survey ID"),
("Crate", SingleId, "Crate ID"),
("Market Ship Components", SingleId, "Market ID"),

// Special endpoints
("Location Detail", LocationDetail, "Location ID"),
("Market Listings", MarketView, null),
("Market Prices", MarketView, null),
("Market Items", MarketView, null),
("Market Buy Competitors", MarketCompetitors, null),
("Market Sell Competitors", MarketCompetitors, null),
```

### Endpoint-to-Method Routing Map

Each endpoint display name maps to a specific `GameApiClient` method:

| Display Name | GameApiClient Method |
|---|---|
| Character | `GetCharacterAsync(appId, token)` |
| Character Skills | `GetCharacterSkillsAsync(appId, token)` |
| Colony List | `GetColonyListAsync(appId, token)` |
| Colony Buildings | `GetColonyBuildingsAsync(appId, token, colonyId)` |
| Colony Warehouse | `GetColonyWarehouseAsync(appId, token, colonyId)` |
| Colony Workers | `GetColonyWorkersAsync(appId, token, colonyId)` |
| Colony Summary | `GetColonySummaryAsync(appId, token, colonyId)` |
| Banking Balance | `GetBankingBalanceAsync(appId, token)` |
| Banking Transactions | `GetBankingTransactionsAsync(appId, token)` |
| Accepted Jobs | `GetAcceptedJobsAsync(appId, token)` |
| Asset Locations | `GetAssetLocationsAsync(appId, token)` |
| Location Detail | `GetAssetLocationDetailAsync(appId, token, locationId, locationType)` |
| Blueprint | `GetAssetBlueprintAsync(appId, token, blueprintId)` |
| Survey | `GetAssetSurveyAsync(appId, token, surveyId)` |
| Crate | `GetAssetCrateAsync(appId, token, crateId)` |
| Kill Mail List | `GetKillMailListAsync(appId, token)` |
| Kill Mail Detail | `GetKillMailDetailAsync(appId, token, killMailId)` |
| Ship Configuration | `GetShipConfigurationAsync(appId, token)` |
| Ship Cargo | `GetShipCargoAsync(appId, token)` |
| Mail List | `GetMailListAsync(appId, token)` |
| Mail Detail | `GetMailDetailAsync(appId, token, mailId)` |
| Market Listings | `GetMarketListingsAsync(appId, token, view, range?, search?, type?, subType?, evolution?, orderBy?)` |
| Market Prices | `GetMarketPricesAsync(appId, token, view, range?, search?, type?, subType?, evolution?, orderBy?)` |
| Market Items | `GetMarketItemsAsync(appId, token, view, range?, search?, type?, subType?, evolution?, orderBy?)` |
| Market Ship Components | `GetMarketShipComponentsAsync(appId, token, marketId)` |
| Market Buy Orders | `GetMarketBuyOrdersAsync(appId, token)` |
| Market Sell Orders | `GetMarketSellOrdersAsync(appId, token)` |
| Market Buy Competitors | `GetMarketBuyCompetitorsAsync(appId, token, marketIds)` |
| Market Sell Competitors | `GetMarketSellCompetitorsAsync(appId, token, marketIds)` |


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Endpoint-to-Field Visibility Mapping

*For any* endpoint from the ManualRequestEndpoint.All list, selecting it in the dropdown SHALL result in exactly the correct set of input controls being visible — parameterless endpoints show no fields, single-ID endpoints show only the ID field with the correct label, LocationDetail shows ID + location type dropdown, MarketView shows the market filter fields, and MarketCompetitors shows only the Market IDs field.

**Validates: Requirements 1.3, 2.1, 2.2**

### Property 2: Invalid ID Rejection

*For any* single-ID endpoint and *for any* string that is not a valid positive integer (empty, whitespace, alphabetic, decimal, negative, zero), the validation logic SHALL reject the input with "Error: ID must be a number" and SHALL NOT initiate an API request.

**Validates: Requirements 3.1, 3.2**

### Property 3: Successful Response Formatting

*For any* valid JSON string returned by GameApiClient with Success=true, the response display SHALL contain the text "HTTP 200 OK" followed by a blank line followed by the JSON body formatted with Newtonsoft.Json Formatting.Indented.

**Validates: Requirements 5.1, 5.2**

### Property 4: Endpoint-to-Method Routing

*For any* endpoint selected in the dropdown with valid parameters provided, the execution logic SHALL invoke exactly the correct GameApiClient method with the correct arguments derived from the user's input fields.

**Validates: Requirements 4.5**


## Error Handling

### Pre-Request Validation Errors (shown in Response_Display, no network call)

| Condition | Message |
|---|---|
| GameApiContext.Instance is null | "Error: Game API is not initialized. Check Preferences." |
| No player selected (CurrentPlayerUUID empty) | "Error: No player selected." |
| No API key for current player | "Error: No API key configured for current player." |
| Empty or non-numeric ID (single-ID endpoints) | "Error: ID must be a number" |
| Empty View field (Market Listings/Prices/Items) | "Error: View is required" |
| Empty Market IDs (Buy/Sell Competitors) | "Error: Market IDs are required" |

### Runtime Errors (shown in Response_Display)

| Condition | Message Format |
|---|---|
| Token exchange failure | "Token exchange failed: {ErrorMessage}" |
| HTTP error response (non-200) | "HTTP {statusCode} — {responseBody}" |
| Exception (timeout, network, circuit breaker) | "Exception: {exception.Message}" |

### Clipboard Errors

- If `Clipboard.SetText()` throws (e.g., clipboard locked by another process), the exception is caught silently. The button reverts to "Copy Response" without the "Copied!" confirmation. The response text area is never modified.

### Button State Invariant

The "Request" button is disabled from the moment execution begins (`BtnRequest_Click` entry) and re-enabled in the `finally` block, guaranteeing it is always re-enabled regardless of error path.


## Testing Strategy

### Property-Based Tests (FsCheck 2.16.6 + NUnit)

**Library:** FsCheck 2.16.6 (already in the test project)
**Minimum iterations:** 100 per property test

Each correctness property maps to one property-based test:

| Property | Test Class | Test Method |
|---|---|---|
| P1: Field visibility | `ManualRequestEndpointTests` | `FieldVisibility_MatchesCategory` |
| P2: Invalid ID rejection | `ManualRequestValidationTests` | `InvalidId_AlwaysRejected` |
| P3: Response formatting | `ManualRequestValidationTests` | `SuccessResponse_AlwaysFormatted` |
| P4: Method routing | `ManualRequestEndpointTests` | `EndpointRouting_CallsCorrectMethod` |

**Tag format:** `// Feature: api-manual-requests, Property N: <text>`

### Unit Tests (Example-Based)

| Test | Validates |
|---|---|
| Default endpoint is "Location Detail" | Req 1.2, 7.3 |
| Location Detail shows type dropdown defaulting to "Co" | Req 2.3, 7.1, 7.3 |
| Market Listings shows View + optional fields | Req 2.4 |
| Market Prices/Items show same fields as Listings | Req 2.5 |
| Market Competitors shows Market IDs field | Req 2.6 |
| Empty View rejected for market endpoints | Req 3.3 |
| Empty Market IDs rejected for competitor endpoints | Req 3.4 |
| Valid ID enables request execution | Req 3.5 |
| Button disabled during fetch, re-enabled after | Req 4.1, 4.4 |
| "Fetching..." shown during request | Req 4.2 |
| Token exchange failure message format | Req 5.4 |
| Exception message format | Req 5.5 |
| Uninitialized API message | Req 5.6 |
| No player selected message | Req 5.7 |
| No API key message | Req 5.8 |
| Copy button copies response to clipboard | Req 6.2 |
| Copy button shows "Copied!" temporarily | Req 6.3 |
| Copy on empty response has no effect | Req 6.4 |

### Test Architecture Notes

- **ManualRequestEndpoint** is a pure static class with no dependencies — all property tests on field visibility and routing can test it directly without form instantiation.
- **Validation logic** will be extracted into a static helper method `ValidateInputs(ManualRequestEndpoint endpoint, string idText, string viewText, string marketIdsText)` returning `(bool IsValid, string ErrorMessage)` — this enables property testing without WinForms dependencies.
- **Response formatting** will be extracted into a static helper method `FormatResponse(bool success, string json)` returning the display string — enabling property testing.
- **Integration with GameApiClient** is tested via example-based mocking (verifying correct method is called with correct parameters).
