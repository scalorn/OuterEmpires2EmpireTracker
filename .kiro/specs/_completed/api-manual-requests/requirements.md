# Requirements Document

## Introduction

The Game API Status form currently supports one manual request type: fetching location details by ID and type. Game developers need the ability to make targeted API requests to any endpoint for debugging purposes. This feature replaces the existing single-endpoint Test API panel with a comprehensive manual request panel that supports all Game_API_Client endpoints. Each endpoint has appropriate input fields for its parameters — some require no parameters, some require a single numeric ID, and some have additional options. All requests flow through the existing Game_API_Client (which enforces the 0.80 TPS rate limit) so no rate limit bypass is possible.

## Glossary

- **Status_Form**: The existing WinForms MDI child form (FormGameApiStatus) displaying real-time API metrics and manual test controls.
- **Game_API_Client**: The existing HTTP client that communicates with game server endpoints, enforcing rate limiting (0.80 TPS) via a token bucket governor.
- **Manual_Request_Panel**: The UI section of the Status_Form that allows the user to select an endpoint, enter parameters, execute the request, and view the response.
- **Response_Display**: The read-only text area that shows the formatted JSON response, HTTP status code, or error message from a manual request.
- **Endpoint_Category**: A grouping of endpoints by their parameter requirements — parameterless, single-ID, or multi-parameter.

## Requirements

### Requirement 1: Endpoint Selection

**User Story:** As a developer, I want to select any game API endpoint from a dropdown, so that I can debug any API resource from a single panel.

#### Acceptance Criteria

1. THE Manual_Request_Panel SHALL provide a dropdown (ComboBox) listing all available endpoints grouped by category, with the following entries:
   - "Character" (GET /v1/character)
   - "Character Skills" (GET /v1/character/skills)
   - "Colony List" (GET /v1/colonies)
   - "Colony Buildings" (GET /v1/colonies/{colonyId}/buildings)
   - "Colony Warehouse" (GET /v1/colonies/{colonyId}/warehouse)
   - "Colony Workers" (GET /v1/colonies/{colonyId}/workers)
   - "Colony Summary" (GET /v1/colonies/{colonyId}/summary)
   - "Banking Balance" (GET /v1/banking/balance)
   - "Banking Transactions" (GET /v1/banking/transactions)
   - "Accepted Jobs" (GET /v1/jobs/accepted)
   - "Asset Locations" (GET /v1/assets/locations)
   - "Location Detail" (GET /v1/assets/locations/{locationId})
   - "Blueprint" (GET /v1/assets/blueprints/{blueprintId})
   - "Survey" (GET /v1/assets/surveys/{surveyId})
   - "Crate" (GET /v1/assets/crates/{crateId})
   - "Kill Mail List" (GET /v1/killmails)
   - "Kill Mail Detail" (GET /v1/killmails/{killMailId})
   - "Ship Configuration" (GET /v1/ships/configuration)
   - "Ship Cargo" (GET /v1/ships/cargo)
   - "Mail List" (GET /v1/mail)
   - "Mail Detail" (GET /v1/mail/{mailId})
   - "Market Listings" (GET /v1/market/listings)
   - "Market Prices" (GET /v1/market/prices)
   - "Market Items" (GET /v1/market/items)
   - "Market Ship Components" (GET /v1/market/{marketId}/ship-components)
   - "Market Buy Orders" (GET /v1/market/buy-orders)
   - "Market Sell Orders" (GET /v1/market/sell-orders)
   - "Market Buy Competitors" (GET /v1/market/buy-competitors)
   - "Market Sell Competitors" (GET /v1/market/sell-competitors)
2. THE Manual_Request_Panel SHALL default the dropdown selection to "Location Detail" to preserve backward compatibility with the existing behavior.
3. WHEN the user selects an endpoint, THE Manual_Request_Panel SHALL show or hide parameter input fields appropriate to that endpoint's requirements.


### Requirement 2: Parameter Input Fields

**User Story:** As a developer, I want to see only the input fields relevant to my selected endpoint, so that the interface is clean and I know exactly what to enter.

#### Acceptance Criteria

1. WHEN the user selects a parameterless endpoint (Character, Character Skills, Colony List, Banking Balance, Banking Transactions, Accepted Jobs, Asset Locations, Kill Mail List, Ship Configuration, Ship Cargo, Mail List, Market Buy Orders, Market Sell Orders), THE Manual_Request_Panel SHALL hide all parameter fields and enable the Request button immediately.
2. WHEN the user selects a single-ID endpoint (Colony Buildings, Colony Warehouse, Colony Workers, Colony Summary, Kill Mail Detail, Mail Detail, Blueprint, Survey, Crate, Market Ship Components), THE Manual_Request_Panel SHALL display a single text input labeled with the appropriate ID name (e.g., "Colony ID", "Blueprint ID", "Survey ID", "Crate ID", "Kill Mail ID", "Mail ID", "Market ID").
3. WHEN the user selects "Location Detail", THE Manual_Request_Panel SHALL display a text input for "Location ID" and a dropdown for location type containing the values "Co" (Colony), "St" (Station), "Sh" (Ship), "Cr" (Character).
4. WHEN the user selects "Market Listings", THE Manual_Request_Panel SHALL display a required text input for "View" (e.g., "local", "regional", "global") and optional text inputs for "Search", "Type", "Sub Type", "Order By", and optional numeric inputs for "Range" and "Evolution".
5. WHEN the user selects "Market Prices" or "Market Items", THE Manual_Request_Panel SHALL display the same inputs as Market Listings (View required, plus optional filters).
6. WHEN the user selects "Market Buy Competitors" or "Market Sell Competitors", THE Manual_Request_Panel SHALL display a text input for "Market IDs" (comma-separated numeric IDs).

### Requirement 3: Input Validation

**User Story:** As a developer, I want input validated before sending, so that I get immediate feedback on invalid entries without wasting an API call.

#### Acceptance Criteria

1. WHEN the user clicks the Request button with an empty ID field for a single-ID endpoint, THE Response_Display SHALL show "Error: ID must be a number" and THE Manual_Request_Panel SHALL NOT send a request.
2. WHEN the user clicks the Request button with a non-numeric ID value for a single-ID endpoint, THE Response_Display SHALL show "Error: ID must be a number" and THE Manual_Request_Panel SHALL NOT send a request.
3. WHEN the user clicks the Request button for "Market Listings", "Market Prices", or "Market Items" with an empty View field, THE Response_Display SHALL show "Error: View is required" and THE Manual_Request_Panel SHALL NOT send a request.
4. WHEN the user clicks the Request button for "Market Buy Competitors" or "Market Sell Competitors" with an empty Market IDs field, THE Response_Display SHALL show "Error: Market IDs are required" and THE Manual_Request_Panel SHALL NOT send a request.
5. WHEN the user enters a valid positive integer ID and clicks the Request button, THE Manual_Request_Panel SHALL initiate the API request for the selected endpoint.


### Requirement 4: Request Execution

**User Story:** As a developer, I want manual requests to go through the same rate-limited client as background sync, so that debugging does not bypass the TPS governor or disrupt normal operations.

#### Acceptance Criteria

1. WHEN the user initiates a manual request, THE Manual_Request_Panel SHALL disable the Request button to prevent concurrent manual requests.
2. WHEN the user initiates a manual request, THE Response_Display SHALL show the text "Fetching..." until the response is received or an error occurs.
3. THE Manual_Request_Panel SHALL execute requests through the existing Game_API_Client instance (which enforces rate limiting), performing token exchange followed by the endpoint-specific GET request.
4. WHEN the request completes (success or failure), THE Manual_Request_Panel SHALL re-enable the Request button.
5. THE Manual_Request_Panel SHALL call the appropriate Game_API_Client method based on the selected endpoint, passing all user-provided parameters.

### Requirement 5: Response Display

**User Story:** As a developer, I want to see the raw JSON response pretty-printed with the HTTP status code, so that I can inspect the exact data the API returns.

#### Acceptance Criteria

1. WHEN the Game_API_Client returns a successful response, THE Response_Display SHALL show the JSON body formatted with indentation (pretty-printed using Newtonsoft.Json Formatting.Indented).
2. WHEN the Game_API_Client returns a successful response, THE Response_Display SHALL prefix the formatted JSON with a status line showing "HTTP 200 OK" followed by a blank line.
3. WHEN the Game_API_Client returns an error status code (401, 403, 404, or other), THE Response_Display SHALL show the HTTP status code and the raw error response text (e.g., "HTTP 404 — 404").
4. IF the request fails due to token exchange failure, THEN THE Response_Display SHALL show "Token exchange failed: {error message}".
5. IF the request fails due to an exception (timeout, network error, circuit breaker), THEN THE Response_Display SHALL show "Exception: {exception message}".
6. IF the Game API is not initialized, THEN THE Response_Display SHALL show "Error: Game API is not initialized. Check Preferences."
7. IF no player is selected, THEN THE Response_Display SHALL show "Error: No player selected."
8. IF no API key is configured for the current player, THEN THE Response_Display SHALL show "Error: No API key configured for current player."

### Requirement 6: Copy Response to Clipboard

**User Story:** As a developer, I want to copy the API response to my clipboard, so that I can share it with game developers for debugging.

#### Acceptance Criteria

1. THE Manual_Request_Panel SHALL provide a "Copy Response" button adjacent to the response display area.
2. WHEN the user clicks "Copy Response", THE Manual_Request_Panel SHALL copy the full contents of the Response_Display text to the system clipboard.
3. WHEN the clipboard copy succeeds, THE "Copy Response" button text SHALL change to "Copied!" for 2 seconds, then revert to "Copy Response".
4. IF the Response_Display is empty (no request has been made), THEN clicking "Copy Response" SHALL have no effect (the button is enabled but the clipboard is not modified).
5. IF the clipboard operation fails, THEN THE Response_Display SHALL retain its current content and the button SHALL revert to its normal state without visual confirmation.

### Requirement 7: Backward Compatibility

**User Story:** As an existing user, I want the existing location fetch functionality to continue working exactly as before, so that my current debugging workflow is not disrupted.

#### Acceptance Criteria

1. THE Manual_Request_Panel SHALL retain the existing location type dropdown (Co, St, Sh, Cr) visible when "Location Detail" is selected as the endpoint.
2. THE Manual_Request_Panel SHALL retain the existing text field for entering the numeric ID.
3. WHEN the Status_Form opens, THE endpoint dropdown SHALL default to "Location Detail" and the location type dropdown SHALL default to "Co" (Colony), matching the current default behavior.
4. THE existing tab order for the test panel controls SHALL remain functional.
