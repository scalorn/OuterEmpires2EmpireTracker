# Requirements Document

## Introduction

The OE2EmpireTracker sharing/visibility system allows characters to share their tracked data (blueprints, surveys, colonies) with other characters or factions. The server-side API and storage layer already exist, but three major gaps prevent end-to-end functionality: public data endpoints return empty arrays, the web UI shared data view calls a non-existent endpoint, and the WinForms desktop app has no UI for configuring sharing rules. This spec covers wiring the existing infrastructure into working features across all three apps.

## Glossary

- **Sharing_Rule**: A record defining what data a character shares, with whom (target), and which data types are included
- **Sharing_Target_Type**: The audience for shared data — either Faction (all members of a faction) or Character (a specific character)
- **Data_Type**: A category of shareable data — Colonies, Blueprints, or Surveys
- **Public_Data_Endpoint**: An unauthenticated API endpoint that serves data shared publicly by any character
- **Sharing_Configuration**: The set of sharing rules owned by a character, managed via PUT /api/v1/characters/{uuid}/sharing
- **Shared_With_Me_Endpoint**: The authenticated endpoint GET /api/v1/characters/{uuid}/shared-with-me/{dataType} that returns data other characters have shared with the caller
- **Server**: The ASP.NET Core API server (OE2EmpireTracker.Server)
- **Web_UI**: The React web application (OE2EmpireTracker.Web)
- **Desktop_App**: The WinForms desktop application (OE2EmpireTracker)
- **Storage_Backend**: The IStorageBackend implementation that persists sharing rules and entity data

## Requirements

### Requirement 1: Public Data Endpoints Return Shared Data

**User Story:** As a public visitor, I want to browse publicly shared blueprints, surveys, and colonies, so that I can discover useful data without needing an account.

#### Acceptance Criteria

1. WHEN a GET request is made to /api/v1/public/blueprints, THE Server SHALL query all characters' sharing rules and return blueprints where a rule grants access with TargetType of Public and DataType of null or "Blueprints"
2. WHEN a GET request is made to /api/v1/public/surveys, THE Server SHALL query all characters' sharing rules and return surveys where a rule grants access with TargetType of Public and DataType of null or "Surveys"
3. WHEN a GET request is made to /api/v1/public/colonies, THE Server SHALL query all characters' sharing rules and return colonies where a rule grants access with TargetType of Public and DataType of null or "Colonies"
4. THE Server SHALL paginate public data responses using page and pageSize query parameters with a strict maximum pageSize of 100 regardless of available data volume
5. WHEN no characters have shared data publicly, THE Server SHALL return an empty items array with totalCount of 0

### Requirement 2: Web UI Shared Data View Uses Correct Endpoint

**User Story:** As a logged-in player, I want to view data that other characters have shared with me, so that I can access faction intelligence and collaborative information.

#### Acceptance Criteria

1. WHEN the SharedDataView page loads, THE Web_UI SHALL call GET /api/v1/characters/{uuid}/shared-with-me/{dataType} to retrieve shared data for each data type
2. THE Web_UI SHALL remove the non-existent getSharedCharacters call to /api/v1/characters/{uuid}/shared-data and replace it with calls to the Shared_With_Me_Endpoint for each supported Data_Type
3. WHEN the Shared_With_Me_Endpoint returns data, THE Web_UI SHALL display entities grouped by sharer character with the sharer's name visible
4. WHEN the Shared_With_Me_Endpoint returns an empty array for all data types, THE Web_UI SHALL display an empty state message indicating no data has been shared with the user, without placeholder content or cached data
5. IF the Shared_With_Me_Endpoint returns an error, THEN THE Web_UI SHALL display a retryable error message with a retry button

### Requirement 3: WinForms Sharing Configuration Form

**User Story:** As a desktop app user, I want to configure what data I share and with whom, so that I can control my data visibility without needing the web UI.

#### Acceptance Criteria

1. THE Desktop_App SHALL provide a FormSharing form accessible from the main menu under a "Sharing" menu item
2. WHEN FormSharing opens, THE Desktop_App SHALL load the current character's sharing rules by calling GET /api/v1/characters/{uuid}/sharing via the server client
3. THE Desktop_App SHALL display sharing rules in a DataGridView with columns for Target Type, Target UUID, and Data Type
4. WHEN the user adds a new sharing rule, THE Desktop_App SHALL allow selection of Target Type (Faction or Character) from a dropdown, entry of Target UUID, and optional selection of Data Type (Colonies, Blueprints, Surveys, or All)
5. WHEN the user saves sharing rules, THE Desktop_App SHALL call PUT /api/v1/characters/{uuid}/sharing with the complete list of rules
6. WHEN the user deletes a sharing rule, THE Desktop_App SHALL remove the rule from the list and persist the updated rules to the server
7. IF the server returns an error on save, THEN THE Desktop_App SHALL display an error message and retain the unsaved state for retry
8. WHEN the current player changes, THE Desktop_App SHALL reload sharing rules for the new character

### Requirement 4: Web UI Sharing Configuration Completeness

**User Story:** As a web UI user, I want the sharing configuration page to support all sharing target types and provide clear feedback, so that I can manage my sharing rules effectively.

#### Acceptance Criteria

1. THE Web_UI SharingConfig page SHALL display existing sharing rules with target type, target UUID, and data type for each rule
2. WHEN the user adds a new rule, THE Web_UI SHALL validate that the Target UUID field is non-empty before allowing save
3. THE Web_UI SHALL support all Sharing_Target_Type values: Faction and Character
4. THE Web_UI SHALL support all Data_Type values: Colonies, Blueprints, Surveys, and a null option representing all data types
5. WHEN a mutation succeeds, THE Web_UI SHALL refresh the rules list to reflect the saved state; IF the refresh itself fails, THEN the save SHALL still be considered successful
6. IF a mutation fails, THEN THE Web_UI SHALL display an error notification to the user

### Requirement 5: Server Public Sharing Target Type

**User Story:** As a character owner, I want to mark data as publicly visible, so that unauthenticated visitors can browse my shared data through the public endpoints.

#### Acceptance Criteria

1. THE Server SHALL accept SharingTargetType values of Faction, Character, and Public in sharing rules submitted via PUT /api/v1/characters/{uuid}/sharing
2. THE Storage_Backend SharingTargetType enum SHALL include a Public value in addition to Faction and Character
3. WHEN a sharing rule has TargetType of Public, THE Server SHALL make the corresponding data available through the public data endpoints without requiring authentication under any circumstances
4. THE Web_UI SharingConfig page SHALL include Public as a selectable target type when creating sharing rules
5. THE Desktop_App FormSharing SHALL include Public as a selectable target type in the Target Type dropdown

### Requirement 6: Cross-App Behavioral Consistency

**User Story:** As a player using multiple apps, I want sharing configuration and shared data viewing to behave identically regardless of which app I use, so that I have a consistent experience.

#### Acceptance Criteria

1. THE Desktop_App and THE Web_UI SHALL use the same server API endpoints for sharing rule management (GET and PUT /api/v1/characters/{uuid}/sharing); apps MAY display rules obtained from any source (including cache) as long as management operations use the required endpoints
2. WHEN a sharing rule is created in the Desktop_App, THE Web_UI SHALL display that rule when the SharingConfig page is loaded
3. WHEN a sharing rule is created in the Web_UI, THE Desktop_App SHALL display that rule when FormSharing is opened
4. THE Desktop_App and THE Web_UI SHALL present the same set of Data_Type options: Colonies, Blueprints, Surveys, and All (null)
5. THE Desktop_App and THE Web_UI SHALL present the same set of Sharing_Target_Type options: Faction, Character, and Public
6. WHEN a sharing rule is created in either app, THE rule SHALL appear immediately in that same app's interface without requiring navigation away and back

### Requirement 7: Sharing Rule Validation

**User Story:** As a player configuring sharing rules, I want the system to validate my rules before saving, so that I do not create invalid or conflicting configurations.

#### Acceptance Criteria

1. WHEN a sharing rule is submitted with an empty TargetUUID, THE Server SHALL return a 400 Bad Request error
2. WHEN a sharing rule is submitted with an invalid TargetType value, THE Server SHALL return a 400 Bad Request error
3. THE Server SHALL assign the authenticated character's UUID as OwnerCharacterUUID on all submitted rules, ignoring any client-provided value
4. WHEN a sharing rule is submitted without an Id, THE Server SHALL generate a new GUID for the rule
5. THE Desktop_App SHALL disable save controls when any rule has an empty Target UUID field
6. THE Web_UI SHALL disable the save button when the Target UUID field is empty
7. THE Server SHALL enforce all validation rules independently of client-side validation, returning appropriate 400 errors regardless of client state
