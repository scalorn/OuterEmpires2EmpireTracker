# Requirements Document

## Introduction

A React-based web UI for the OE2 Empire Tracker faction server, delivered as a single SPA with two access modes: a public view-only mode (no auth) for community browsing of blueprints, surveys, and colony planning, and a full authenticated mode that provides a web-based alternative to the desktop WinForms client. The frontend is built with React + Vite + TypeScript, outputs to the server's wwwroot/ directory, and is served by Kestrel's static file middleware as part of a single deployment package. Colony planning computations are performed server-side via new API endpoints that invoke the existing Common library logic.

## Glossary

- **SPA**: Single Page Application — the React frontend served as static files
- **Public_Mode**: The unauthenticated view-only access mode using the server's sharing/permissions system
- **Authenticated_Mode**: The token-authenticated full-access mode matching the server's existing auth model
- **Colony_Planner**: Server-side API endpoints that compute colony build plans using Common library services
- **API_Client**: The TypeScript HTTP client layer that communicates with the server REST API
- **WebSocket_Client**: The TypeScript WebSocket client that receives real-time push events
- **Type_Generator**: The build-time tool (NSwag or custom script) that produces TypeScript types from C# models
- **Kestrel**: The ASP.NET Core web server that serves both the API and static frontend files
- **Common_Library**: The OE2EmpireTracker.Common project containing domain logic (ColonyStatusCalculator, BuildOrderOptimizer, ColonyBuildEligibility, etc.)
- **Build_Pipeline**: The combined build process — `npm run build` outputs to wwwroot/, dotnet publish produces a single deployment artifact


## Requirements

### Requirement 1: Project Scaffolding and Build Integration

**User Story:** As a developer, I want the React frontend to live in OE2EmpireTracker.Web/ and integrate into the server's build/deploy pipeline, so that a single `dotnet publish` produces a complete deployment package with both API and UI.

#### Acceptance Criteria

1. THE Build_Pipeline SHALL use Vite with React and TypeScript as the frontend toolchain
2. WHEN `npm run build` is executed in OE2EmpireTracker.Web/, THE Build_Pipeline SHALL output production assets to the server project's wwwroot/ directory
3. THE Build_Pipeline SHALL generate a production bundle with content-hashed filenames for cache busting
4. WHEN the server project is published, THE Build_Pipeline SHALL include the wwwroot/ static assets in the output package
5. THE SPA SHALL use path-based routing with a fallback to index.html for client-side route handling
6. THE Build_Pipeline SHALL include an `npm run dev` script that proxies API requests to the local server on port 5443 during development

### Requirement 2: Static File Serving

**User Story:** As a server operator, I want Kestrel to serve the React SPA as static files alongside the API, so that a single process handles both frontend and backend.

#### Acceptance Criteria

1. THE Kestrel server SHALL serve static files from wwwroot/ using UseStaticFiles middleware
2. WHEN a request does not match an API route or static file, THE Kestrel server SHALL return index.html to support SPA client-side routing
3. THE Kestrel server SHALL set appropriate cache headers (long-lived for hashed assets, no-cache for index.html)
4. THE Kestrel server SHALL serve static files without requiring authentication


### Requirement 3: TypeScript Type Generation

**User Story:** As a frontend developer, I want TypeScript interfaces generated from the server's C# models, so that the frontend stays type-safe and in sync with the API contract.

#### Acceptance Criteria

1. THE Type_Generator SHALL produce TypeScript interfaces from C# model classes in OE2EmpireTracker.Common and OE2EmpireTracker.Server
2. WHEN C# models change, THE Type_Generator SHALL regenerate TypeScript types as part of the build process
3. THE Type_Generator SHALL output generated types to a dedicated directory (e.g., src/api/types/) with a header comment indicating they are auto-generated
4. THE Type_Generator SHALL handle C# enums, nullable types, DateTime (as string), and collection types correctly
5. THE Type_Generator SHALL produce types for all API request/response DTOs used by the endpoints

### Requirement 4: Authentication Flow

**User Story:** As a user with a server token, I want to authenticate in the web UI using my existing token, so that I can access my character data without a separate account system.

#### Acceptance Criteria

1. WHEN a user enters a Bearer token, THE SPA SHALL store it securely in browser storage and include it in all subsequent API requests via the Authorization header
2. THE SPA SHALL provide a login screen that accepts a Bearer token string
3. WHEN an API request returns 401 Unauthorized, THE SPA SHALL redirect the user to the login screen and clear the stored token
4. WHILE in Authenticated_Mode, THE SPA SHALL display the authenticated character's name and role in the UI header
5. THE SPA SHALL provide a logout action that clears the stored token and returns to Public_Mode
6. IF the stored token is invalid or expired, THEN THE SPA SHALL display an error message and prompt for a new token


### Requirement 5: Public Mode — View-Only Access

**User Story:** As a community member without a token, I want to browse publicly shared blueprints, surveys, and colony data, so that I can research the game without needing server access.

#### Acceptance Criteria

1. THE SPA SHALL render a Public_Mode interface when no authentication token is present
2. WHILE in Public_Mode, THE SPA SHALL display only data exposed through the server's public sharing endpoints
3. WHILE in Public_Mode, THE SPA SHALL hide all mutation controls (create, edit, delete buttons)
4. THE SPA SHALL provide navigation to browse shared blueprints, surveys, and colony data in Public_Mode
5. WHILE in Public_Mode, THE SPA SHALL display a prompt to authenticate for full access
6. THE SPA SHALL allow access to the Colony_Planner tool in Public_Mode without authentication

### Requirement 6: Public Mode — Blueprint Browser

**User Story:** As a community member, I want to browse publicly shared blueprints with filtering and sorting, so that I can research available designs.

#### Acceptance Criteria

1. WHEN publicly shared blueprint data is available, THE SPA SHALL display blueprints in a searchable, sortable list
2. THE SPA SHALL allow filtering blueprints by type, tech level, and ship class
3. WHEN a blueprint is selected, THE SPA SHALL display its full details including properties, evolution chain, and manufacturing requirements
4. THE SPA SHALL display blueprint ownership attribution (character name) when available

### Requirement 7: Public Mode — Survey Browser

**User Story:** As a community member, I want to browse publicly shared surveys, so that I can find resource-rich locations.

#### Acceptance Criteria

1. WHEN publicly shared survey data is available, THE SPA SHALL display surveys in a searchable list
2. THE SPA SHALL allow filtering surveys by system, resource type, and purity level
3. WHEN a survey is selected, THE SPA SHALL display its full resource breakdown and location details


### Requirement 8: Colony Planner — Server-Side API

**User Story:** As a player (authenticated or public), I want to plan colony builds using the server's domain logic, so that I get accurate results without duplicating game rules in the frontend.

#### Acceptance Criteria

1. THE Kestrel server SHALL expose colony planning API endpoints under /api/v1/colony-planner/
2. WHEN a colony configuration is submitted, THE Colony_Planner SHALL invoke ColonyStatusCalculator from the Common_Library to compute structure status
3. WHEN a build order request is submitted, THE Colony_Planner SHALL invoke BuildOrderOptimizer from the Common_Library to produce an optimized build sequence
4. WHEN a build eligibility check is requested, THE Colony_Planner SHALL invoke ColonyBuildEligibility from the Common_Library to determine which structures can be built
5. THE Colony_Planner endpoints SHALL accept colony state as input (structures, resources, player skills) and return computed results without persisting state
6. THE Colony_Planner endpoints SHALL be accessible without authentication (public access)
7. IF invalid colony configuration is submitted, THEN THE Colony_Planner SHALL return descriptive validation errors with HTTP 400 status

### Requirement 9: Colony Planner — Frontend Interface

**User Story:** As a player, I want an interactive colony planner in the web UI that lets me design colonies and see build orders, so that I can plan before committing in-game.

#### Acceptance Criteria

1. THE SPA SHALL provide a colony planner interface accessible from both Public_Mode and Authenticated_Mode
2. THE SPA SHALL allow users to add, remove, and configure colony structures in the planner
3. WHEN the colony configuration changes, THE SPA SHALL call the Colony_Planner API and display updated status (built/ideal counts, eligibility)
4. THE SPA SHALL display the optimized build order as a sequenced list with resource requirements per step
5. THE SPA SHALL display structure limits and enforce them visually (disable adding structures that exceed limits)
6. WHEN in Authenticated_Mode, THE SPA SHALL allow loading an existing colony's current state into the planner as a starting point
7. THE SPA SHALL display resource requirements for the planned build and highlight shortfalls


### Requirement 10: Authenticated Mode — Character Data Management

**User Story:** As an authenticated user, I want to view and manage my character's data (colonies, blueprints, surveys, profiles) through the web UI, so that I don't need the desktop app for basic empire management.

#### Acceptance Criteria

1. WHILE in Authenticated_Mode, THE SPA SHALL display the user's character list and allow switching between characters (for Owner role)
2. WHILE in Authenticated_Mode, THE SPA SHALL provide CRUD interfaces for colony data
3. WHILE in Authenticated_Mode, THE SPA SHALL provide CRUD interfaces for blueprint data
4. WHILE in Authenticated_Mode, THE SPA SHALL provide CRUD interfaces for survey data
5. WHILE in Authenticated_Mode, THE SPA SHALL provide a read/edit interface for player profile data
6. WHEN data is modified, THE SPA SHALL call the appropriate server API endpoint and reflect the updated state

### Requirement 11: Authenticated Mode — Faction View

**User Story:** As a faction member, I want to see shared data from my faction mates in the web UI, so that I can coordinate with my faction.

#### Acceptance Criteria

1. WHILE in Authenticated_Mode, THE SPA SHALL display the user's faction membership and faction member list
2. WHEN the user belongs to a faction, THE SPA SHALL provide access to faction-shared data (blueprints, surveys, colonies) via the sharing endpoints
3. THE SPA SHALL clearly distinguish between the user's own data and faction-shared data in the display

### Requirement 12: Real-Time Updates via WebSocket

**User Story:** As an authenticated user, I want the web UI to receive real-time push notifications when data changes, so that I see updates without manual refresh.

#### Acceptance Criteria

1. WHILE in Authenticated_Mode, THE WebSocket_Client SHALL establish a WebSocket connection to /ws with the user's token
2. WHEN a server push event is received, THE SPA SHALL update the affected data in the UI without a full page reload
3. IF the WebSocket connection is lost, THEN THE WebSocket_Client SHALL attempt reconnection with exponential backoff
4. THE WebSocket_Client SHALL send periodic ping messages to maintain the connection within the server's heartbeat timeout
5. WHEN the user logs out, THE WebSocket_Client SHALL close the WebSocket connection cleanly


### Requirement 13: API Client Layer

**User Story:** As a frontend developer, I want a typed API client layer that wraps all server endpoints, so that components don't make raw fetch calls and error handling is centralized.

#### Acceptance Criteria

1. THE API_Client SHALL provide typed methods for all server REST endpoints (characters, factions, data, sharing, global)
2. THE API_Client SHALL automatically attach the Bearer token to authenticated requests
3. WHEN an API call fails, THE API_Client SHALL return structured error information including HTTP status and server error message
4. THE API_Client SHALL handle request serialization and response deserialization using the generated TypeScript types
5. THE API_Client SHALL support request cancellation via AbortController for in-flight requests when components unmount

### Requirement 14: Responsive Layout and Navigation

**User Story:** As a user, I want the web UI to work well on both desktop and tablet screens, so that I can use it from various devices.

#### Acceptance Criteria

1. THE SPA SHALL use a responsive layout that adapts to viewport widths from 768px to 1920px
2. THE SPA SHALL provide a navigation sidebar (collapsible on smaller viewports) with links to all major sections
3. THE SPA SHALL display the current mode (Public or Authenticated) and provide mode-switching controls in the header
4. THE SPA SHALL use consistent visual styling across all views

### Requirement 15: Error Handling and Loading States

**User Story:** As a user, I want clear feedback when data is loading or when errors occur, so that I understand the application state.

#### Acceptance Criteria

1. WHILE data is being fetched, THE SPA SHALL display loading indicators in the affected UI region
2. IF an API request fails with a network error, THEN THE SPA SHALL display a retry-able error message
3. IF an API request fails with a server error (5xx), THEN THE SPA SHALL display a generic error message with the option to retry
4. IF an API request fails with a validation error (4xx), THEN THE SPA SHALL display the specific validation message from the server
5. THE SPA SHALL not display blank screens — every state (loading, error, empty, populated) has a defined visual representation


### Requirement 16: Sharing Configuration (Authenticated)

**User Story:** As an authenticated user, I want to manage my sharing rules through the web UI, so that I can control what data is visible to my faction and the public.

#### Acceptance Criteria

1. WHILE in Authenticated_Mode, THE SPA SHALL provide an interface to view and edit the user's sharing rules
2. THE SPA SHALL allow creating sharing rules that target a faction or specific character
3. THE SPA SHALL allow specifying which data types are shared in each rule
4. WHEN sharing rules are modified, THE SPA SHALL persist changes via the PUT /api/v1/characters/{uuid}/sharing endpoint

### Requirement 17: Colony Planner — Auto-Plan Mode

**User Story:** As a player, I want the colony planner to automatically generate an optimal build plan for a target colony configuration, so that I don't have to manually sequence builds.

#### Acceptance Criteria

1. WHEN the user specifies a target colony configuration (desired structures), THE Colony_Planner SHALL compute an optimal build order to reach that target from the current state
2. THE SPA SHALL display the auto-generated plan as a step-by-step sequence with timing estimates
3. THE SPA SHALL allow the user to modify the auto-generated plan (reorder steps, remove steps) and recompute feasibility
4. WHEN in Authenticated_Mode, THE SPA SHALL allow saving a computed plan to the user's build plans via the server API

### Requirement 18: Public Sharing Endpoints (Server Extension)

**User Story:** As a server operator, I want public-facing endpoints that serve data marked as publicly shared, so that the web UI's Public_Mode can display community data without authentication.

#### Acceptance Criteria

1. THE Kestrel server SHALL expose public data endpoints under /api/v1/public/ that do not require authentication
2. WHEN a character has sharing rules with TargetType "Public", THE public endpoints SHALL include that character's shared data in responses
3. THE public endpoints SHALL support the same data types as the authenticated sharing endpoints (blueprints, surveys, colonies)
4. THE public endpoints SHALL apply cross-reference filtering to redact private UUID references from public responses
5. THE public endpoints SHALL support pagination for large result sets


### Requirement 19: Development Experience

**User Story:** As a frontend developer, I want hot module replacement, TypeScript strict mode, and linting during development, so that I can iterate quickly with confidence.

#### Acceptance Criteria

1. THE Build_Pipeline SHALL support Vite's HMR (Hot Module Replacement) during development via `npm run dev`
2. THE Build_Pipeline SHALL enforce TypeScript strict mode with no implicit any
3. THE Build_Pipeline SHALL include ESLint with a React-focused configuration
4. THE Build_Pipeline SHALL include a `npm run typecheck` script for standalone type verification
5. THE Build_Pipeline SHALL include a `npm run generate-types` script that regenerates TypeScript types from the server's C# models

### Requirement 20: Deployment and Configuration

**User Story:** As a server operator, I want the frontend to be configurable for different server URLs and to work behind reverse proxies, so that deployment is flexible.

#### Acceptance Criteria

1. THE SPA SHALL read the API base URL from a runtime configuration file (not baked into the build) to support different deployment environments
2. THE SPA SHALL work correctly when served behind a reverse proxy with a path prefix
3. THE SPA SHALL include a build-time version identifier visible in the UI footer or about section
4. WHEN the server's UseForwardedHeaders is enabled, THE SPA SHALL function correctly with X-Forwarded-* headers

