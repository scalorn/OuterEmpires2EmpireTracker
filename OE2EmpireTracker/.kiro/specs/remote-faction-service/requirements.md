# Requirements Document

## Introduction

The Remote Faction Service is a standalone, cross-platform command-line application that provides a centralized HTTP API for managing shared faction and external character data. Currently, each OE2 Empire Tracker instance stores factions and external characters locally in its own PlayerData.json file. This service extracts that shared data into a central location so multiple tracker instances can read and write to a single source of truth, keeping faction and character data consistent across all connected clients.

The service is a separate project from the tracker application. It runs as a .NET 8+ Minimal API, deployable on Windows, Linux, and macOS. The tracker (.NET Framework 4.8.1 WinForms app) acts as a client, communicating with this service over HTTP.

## Glossary

- **Faction_Service**: The remote HTTP API application that manages shared faction and external character data
- **Tracker_Client**: An instance of the OE2 Empire Tracker WinForms application that connects to the Faction_Service as a consumer
- **Faction**: A named group/organization in the game, identified by a deterministic UUID derived from its name
- **External_Character**: A non-tracked game character (other player or NPC) that belongs to a faction, identified by a deterministic UUID derived from its name
- **Deterministic_UUID**: A UUID generated from a name string using a namespace-specific seed, ensuring the same name always produces the same UUID regardless of which client creates it
- **Last_Write_Wins**: A conflict resolution strategy where the most recent write to an entity overwrites any previous state without merge logic
- **Health_Endpoint**: An HTTP endpoint that returns service availability status without requiring authentication

## Requirements

### Requirement 1: Cross-Platform Execution

**User Story:** As a server operator, I want to run the Faction Service on Windows, Linux, or macOS, so that I can deploy it on whatever infrastructure I have available.

#### Acceptance Criteria

1. THE Faction_Service SHALL execute as a command-line application without a graphical user interface
2. THE Faction_Service SHALL target .NET 8 or later and support deployment on Windows, Linux, and macOS
3. THE Faction_Service SHALL be publishable as a self-contained single-file executable for each target platform
4. THE Faction_Service SHALL read configuration from environment variables and an optional appsettings.json file

### Requirement 2: Faction Management API

**User Story:** As a tracker user, I want to create, read, update, and delete factions through the remote service, so that all tracker instances share the same faction data.

#### Acceptance Criteria

1. WHEN a valid faction creation request is received, THE Faction_Service SHALL create a Faction with a deterministic UUID derived from the faction name, a Name, and a Description
2. WHEN a faction list request is received, THE Faction_Service SHALL return all factions
3. WHEN a request for a specific faction UUID is received, THE Faction_Service SHALL return the matching faction
4. WHEN a valid faction update request is received, THE Faction_Service SHALL update the Name and Description of the specified faction
5. WHEN a faction deletion request is received, THE Faction_Service SHALL remove the faction and clear the FactionUUID field on all external characters that reference it
6. IF a faction creation request specifies a name that produces a UUID already in use, THEN THE Faction_Service SHALL return a conflict error indicating the faction already exists
7. IF a faction request specifies a UUID that does not exist, THEN THE Faction_Service SHALL return a not-found error

### Requirement 3: External Character Management API

**User Story:** As a tracker user, I want to create, read, update, and delete external characters through the remote service, so that all tracker instances share the same character data.

#### Acceptance Criteria

1. WHEN a valid external character creation request is received, THE Faction_Service SHALL create an External_Character with a deterministic UUID derived from the character name, a Name, and an optional FactionUUID
2. WHEN an external character list request is received, THE Faction_Service SHALL return all external characters
3. WHEN a request for a specific external character UUID is received, THE Faction_Service SHALL return the matching external character
4. WHEN a valid external character update request is received, THE Faction_Service SHALL update the Name and FactionUUID of the specified external character
5. WHEN an external character deletion request is received, THE Faction_Service SHALL remove the external character
6. IF an external character creation request specifies a name that produces a UUID already in use, THEN THE Faction_Service SHALL return a conflict error indicating the character already exists
7. IF an external character request specifies a UUID that does not exist, THEN THE Faction_Service SHALL return a not-found error
8. IF an external character creation or update request specifies a FactionUUID that does not exist, THEN THE Faction_Service SHALL return a validation error indicating the faction does not exist

### Requirement 4: Data Persistence

**User Story:** As a server operator, I want faction and character data to survive service restarts, so that data is not lost when the service is stopped or the host reboots.

#### Acceptance Criteria

1. THE Faction_Service SHALL persist all faction and external character data to a JSON file on disk
2. WHEN the Faction_Service starts, THE Faction_Service SHALL load existing data from the JSON persistence file if it exists
3. WHEN a create, update, or delete operation completes, THE Faction_Service SHALL write the updated state to the persistence file before returning a success response
4. IF the persistence file does not exist on startup, THEN THE Faction_Service SHALL initialize with an empty data set and create the file on the first write operation
5. THE Faction_Service SHALL use atomic file writes (write to temporary file, then rename) to prevent data corruption from interrupted writes

### Requirement 5: Deterministic UUID Generation

**User Story:** As a tracker user, I want the same faction or character name to always produce the same UUID regardless of which client creates it, so that data remains consistent when multiple clients independently reference the same entity.

#### Acceptance Criteria

1. THE Faction_Service SHALL generate faction UUIDs deterministically from the faction name using a faction-specific namespace UUID
2. THE Faction_Service SHALL generate external character UUIDs deterministically from the character name using a character-specific namespace UUID
3. THE Faction_Service SHALL use the same namespace UUIDs and generation algorithm as the existing Tracker_Client implementation to ensure UUID compatibility

### Requirement 6: Conflict Resolution

**User Story:** As a tracker user, I want predictable behavior when two clients edit the same entity simultaneously, so that I understand what will happen to my changes.

#### Acceptance Criteria

1. THE Faction_Service SHALL use a Last_Write_Wins conflict resolution strategy for concurrent updates to the same entity
2. WHEN two update requests arrive for the same entity, THE Faction_Service SHALL apply them sequentially in the order received
3. THE Faction_Service SHALL include a LastModified timestamp on each entity so clients can detect when data has changed since their last read

### Requirement 7: API Design

**User Story:** As a tracker developer, I want a well-structured RESTful API, so that the tracker client can integrate with the service using standard HTTP conventions.

#### Acceptance Criteria

1. THE Faction_Service SHALL expose a RESTful HTTP API using JSON request and response bodies
2. THE Faction_Service SHALL use standard HTTP methods: GET for reads, POST for creates, PUT for updates, DELETE for deletes
3. THE Faction_Service SHALL return appropriate HTTP status codes: 200 for success, 201 for created, 400 for validation errors, 404 for not found, 409 for conflicts
4. THE Faction_Service SHALL expose faction endpoints under the path prefix /api/factions
5. THE Faction_Service SHALL expose external character endpoints under the path prefix /api/characters
6. THE Faction_Service SHALL include a Health_Endpoint at /health that returns 200 when the service is operational

### Requirement 8: Service Configuration

**User Story:** As a server operator, I want to configure the service's listening port and data file location, so that I can adapt it to my deployment environment.

#### Acceptance Criteria

1. THE Faction_Service SHALL accept a configurable HTTP listening port (default: 5000)
2. THE Faction_Service SHALL accept a configurable data file path for the persistence file (default: ./data/factions.json)
3. THE Faction_Service SHALL log its configuration on startup including the listening address and data file path
4. THE Faction_Service SHALL validate configuration on startup and exit with a descriptive error message if configuration is invalid

### Requirement 9: Logging and Diagnostics

**User Story:** As a server operator, I want the service to produce structured logs, so that I can diagnose issues and monitor activity.

#### Acceptance Criteria

1. THE Faction_Service SHALL log all API requests including the HTTP method, path, and response status code
2. THE Faction_Service SHALL log all data mutation operations (create, update, delete) with the entity type and UUID
3. THE Faction_Service SHALL log errors with full exception details
4. THE Faction_Service SHALL write logs to the console (stdout) in a structured format suitable for log aggregation
5. IF an unhandled exception occurs during request processing, THEN THE Faction_Service SHALL return a 500 status code with a generic error message and log the full exception details

### Requirement 10: Client Connectivity

**User Story:** As a tracker user, I want the tracker to gracefully handle the remote service being unavailable, so that I can still use the tracker when the service is down.

#### Acceptance Criteria

1. WHEN the Faction_Service is unreachable, THE Tracker_Client SHALL continue operating using its local faction and character data
2. WHEN the Faction_Service becomes reachable after being unavailable, THE Tracker_Client SHALL synchronize by fetching the current state from the service
3. THE Tracker_Client SHALL provide a configuration option to specify the Faction_Service URL (or disable remote connectivity entirely)
4. THE Tracker_Client SHALL indicate the connection status to the Faction_Service in the user interface

### Requirement 11: Bulk Data Retrieval

**User Story:** As a tracker developer, I want to fetch all factions and characters in a single request, so that the client can efficiently synchronize its local cache on startup.

#### Acceptance Criteria

1. WHEN a bulk data request is received, THE Faction_Service SHALL return all factions and all external characters in a single response
2. THE Faction_Service SHALL expose the bulk data endpoint at GET /api/sync
3. THE Faction_Service SHALL include a server timestamp in the bulk response so clients can detect data freshness

### Requirement 12: API Key Authentication

**User Story:** As a server operator, I want to restrict access to the service using API keys, so that only authorized tracker instances can read and modify shared data.

#### Acceptance Criteria

1. THE Faction_Service SHALL require a valid API key in the X-Api-Key request header for all endpoints except /health
2. THE Faction_Service SHALL support configuring one or more valid API keys via configuration
3. IF a request is missing the X-Api-Key header or provides an invalid key, THEN THE Faction_Service SHALL return a 401 Unauthorized response
4. WHERE API key authentication is disabled in configuration, THE Faction_Service SHALL allow all requests without authentication
