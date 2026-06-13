# Implementation Plan: API Manual Requests

## Overview

Replace the single-endpoint "Test API" panel in FormGameApiStatus with a comprehensive manual request panel supporting all 28 Game_API_Client endpoints. Implementation creates the endpoint metadata helper class, extracts testable validation/formatting logic, updates the form's Designer and code-behind, then wires everything together. All requests flow through the existing rate-limited GameApiClient.

## Tasks

- [x] 1. Create ManualRequestEndpoint metadata class
  - [x] 1.1 Create EndpointCategory enum and ManualRequestEndpoint class
    - Create `OE2EmpireTracker/Forms/GameApiStatus/ManualRequestEndpoint.cs`
    - Define `EndpointCategory` enum (Parameterless, SingleId, LocationDetail, MarketView, MarketCompetitors)
    - Define `ManualRequestEndpoint` class with DisplayName, Category, IdLabel properties
    - Populate static `All` list with all 28 endpoints in display order per design
    - Implement `FindByDisplayName(string name)` static method
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 1.2 Write property test for endpoint-to-field visibility mapping
    - **Property 1: Endpoint-to-Field Visibility Mapping**
    - **Validates: Requirements 1.3, 2.1, 2.2**
    - Verify that every endpoint in `ManualRequestEndpoint.All` has a valid category
    - Verify parameterless endpoints have null IdLabel, single-ID endpoints have non-null IdLabel


- [x] 2. Extract testable validation and formatting helpers
  - [x] 2.1 Create ManualRequestValidation static helper
    - Create `OE2EmpireTracker/Forms/GameApiStatus/ManualRequestValidation.cs`
    - Implement `ValidateInputs(ManualRequestEndpoint endpoint, string idText, string viewText, string marketIdsText)` returning `(bool IsValid, string ErrorMessage)`
    - Single-ID: reject empty/non-numeric (error: "Error: ID must be a number")
    - MarketView: reject empty View (error: "Error: View is required")
    - MarketCompetitors: reject empty Market IDs (error: "Error: Market IDs are required")
    - Parameterless/LocationDetail: always valid (LocationDetail ID validated as SingleId)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 2.2 Write property test for invalid ID rejection
    - **Property 2: Invalid ID Rejection**
    - **Validates: Requirements 3.1, 3.2**
    - For any SingleId endpoint and any non-positive-integer string, validation returns IsValid=false with "Error: ID must be a number"

  - [x] 2.3 Create ManualRequestFormatter static helper
    - Create `OE2EmpireTracker/Forms/GameApiStatus/ManualRequestFormatter.cs`
    - Implement `FormatResponse(bool success, int statusCode, string responseBody)` returning formatted display string
    - Success: "HTTP 200 OK\r\n\r\n" + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(body), Formatting.Indented)
    - Error status: "HTTP {statusCode} — {responseBody}"
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 2.4 Write property test for successful response formatting
    - **Property 3: Successful Response Formatting**
    - **Validates: Requirements 5.1, 5.2**
    - For any valid JSON string with success=true, output starts with "HTTP 200 OK" followed by blank line followed by indented JSON


- [x] 3. Checkpoint - Verify metadata and helpers compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Update FormGameApiStatus Designer for manual request panel
  - [x] 4.1 Add endpoint dropdown and restructure input panel controls
    - Modify `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.Designer.cs`
    - Add `cboEndpoint` ComboBox with `lblEndpoint` label
    - Rename existing `txtTestLocationId` → `txtId`, `btnTestFetch` → `btnRequest`, `txtTestResult` → `txtResponse`
    - Add `lblId` dynamic label (replaces static "Location ID" label)
    - Retain `cboLocationType` and `lblLocationType` for Location Detail
    - Add `btnCopyResponse` button
    - _Requirements: 1.1, 1.2, 6.1, 7.1, 7.2, 7.4_

  - [x] 4.2 Add market parameter controls to the input panel
    - Modify `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.Designer.cs`
    - Add `txtView` + `lblView`, `txtSearch` + `lblSearch`, `txtType` + `lblType`
    - Add `txtSubType` + `lblSubType`, `txtOrderBy` + `lblOrderBy`
    - Add `txtRange` + `lblRange`, `txtEvolution` + `lblEvolution`
    - Add `txtMarketIds` + `lblMarketIds`
    - Layout: second row visible only for market endpoints
    - _Requirements: 2.4, 2.5, 2.6_


- [x] 5. Implement endpoint selection and dynamic field visibility
  - [x] 5.1 Implement CboEndpoint_SelectedIndexChanged handler
    - Modify `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs`
    - Populate `cboEndpoint` from `ManualRequestEndpoint.All` display names in constructor
    - Default selection to "Location Detail" (index matching)
    - Default `cboLocationType` to "Co"
    - On selection change: look up endpoint category, show/hide controls accordingly
    - Parameterless: hide all parameter fields
    - SingleId: show txtId + lblId (set lblId.Text to endpoint's IdLabel)
    - LocationDetail: show txtId + lblId + cboLocationType + lblLocationType
    - MarketView: show txtView + lblView + optional market fields
    - MarketCompetitors: show txtMarketIds + lblMarketIds
    - _Requirements: 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 7.1, 7.3_


- [x] 6. Implement request execution and endpoint routing
  - [x] 6.1 Implement BtnRequest_Click with validation and API routing
    - Modify `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs`
    - On click: disable button, show "Fetching..." in txtResponse
    - Pre-request checks: GameApiContext.Instance null, no player selected, no API key
    - Call `ManualRequestValidation.ValidateInputs()` — if invalid, show error, re-enable button, return
    - Perform token exchange via existing credential flow
    - Route to correct GameApiClient method based on endpoint display name (switch/dictionary)
    - Pass appId, token, and user-provided parameters (ID, view, filters, market IDs)
    - On success: format via `ManualRequestFormatter.FormatResponse(true, 200, body)`
    - On HTTP error: format via `ManualRequestFormatter.FormatResponse(false, statusCode, body)`
    - On token failure: show "Token exchange failed: {message}"
    - On exception: show "Exception: {message}"
    - Finally: re-enable button
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

  - [x] 6.2 Write property test for endpoint-to-method routing
    - **Property 4: Endpoint-to-Method Routing**
    - **Validates: Requirements 4.5**
    - For any endpoint in ManualRequestEndpoint.All, verify the routing dictionary/switch has a matching entry


- [x] 7. Implement copy response to clipboard
  - [x] 7.1 Implement BtnCopyResponse_Click handler
    - Modify `OE2EmpireTracker/Forms/GameApiStatus/FormGameApiStatus.cs`
    - On click: if txtResponse is empty, return (no-op)
    - Try `Clipboard.SetText(txtResponse.Text)` — on success, change button text to "Copied!", start 2-second timer to revert
    - On clipboard exception: catch silently, do not show "Copied!" confirmation
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 8. Checkpoint - Full integration verification
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Write unit tests for manual request panel
  - [x] 9.1 Write unit tests for ManualRequestEndpoint metadata
    - Create `OE2EmpireTracker.Tests/Forms/ManualRequestEndpointTests.cs`
    - Test: All list contains exactly 28 endpoints
    - Test: FindByDisplayName returns correct endpoint
    - Test: Default endpoint is "Location Detail"
    - Test: Location Detail has LocationDetail category
    - Test: Colony Buildings has SingleId category with "Colony ID" label
    - _Requirements: 1.1, 1.2, 7.3_

  - [x] 9.2 Write unit tests for ManualRequestValidation
    - Create `OE2EmpireTracker.Tests/Forms/ManualRequestValidationTests.cs`
    - Test: Empty ID rejected for single-ID endpoint
    - Test: Non-numeric ID rejected for single-ID endpoint
    - Test: Valid numeric ID accepted for single-ID endpoint
    - Test: Empty View rejected for MarketView endpoint
    - Test: Empty Market IDs rejected for MarketCompetitors endpoint
    - Test: Parameterless endpoint always valid
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 9.3 Write unit tests for ManualRequestFormatter
    - Create `OE2EmpireTracker.Tests/Forms/ManualRequestFormatterTests.cs`
    - Test: Success response has "HTTP 200 OK" header and indented JSON
    - Test: Error response shows "HTTP {code} — {body}"
    - Test: Invalid JSON in success response is handled gracefully
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The ManualRequestEndpoint class, ManualRequestValidation, and ManualRequestFormatter are pure static classes — fully testable without WinForms dependencies
- Designer.cs modifications (tasks 4.1, 4.2) are split to keep each under the 5-file / 200-line limit
- The form code-behind is split into selection logic (task 5.1) and execution logic (task 6.1) to keep each focused

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["2.2", "2.3"] },
    { "id": 3, "tasks": ["2.4", "4.1"] },
    { "id": 4, "tasks": ["4.2"] },
    { "id": 5, "tasks": ["5.1"] },
    { "id": 6, "tasks": ["6.1"] },
    { "id": 7, "tasks": ["6.2", "7.1"] },
    { "id": 8, "tasks": ["9.1", "9.2", "9.3"] }
  ]
}
```
