# Implementation Plan: Web UI Frontend

## Overview

Build a React + TypeScript SPA in `OE2EmpireTracker.Web/` that serves as a web-based alternative to the desktop WinForms client. The frontend is served as static files from the existing .NET 8.0 Kestrel server, with two access modes: public view-only and authenticated full-access. Colony planning computations remain server-side via new API endpoints.

Tasks are sized to ≤5 files modified, ≤200 new lines, and ≤3 acceptance criteria each.

## Tasks

- [x] 1. Scaffold Vite + React + TypeScript project
  - [x] 1.1 Initialize OE2EmpireTracker.Web/ with Vite, React 19, TypeScript strict mode
    - Run `npm create vite@latest` or manually create package.json, tsconfig.json, vite.config.ts, index.html
    - Install core deps: react, react-dom, typescript, @vitejs/plugin-react
    - Configure vite.config.ts with build output to `../OE2EmpireTracker.Server/wwwroot/`, content-hashed filenames, and dev proxy to localhost:5443
    - Add scripts: dev, build, preview, typecheck, lint
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 19.1, 19.2_

  - [x] 1.2 Install and configure Tailwind CSS + ESLint
    - Install tailwindcss, @tailwindcss/vite, eslint, typescript-eslint, eslint-plugin-react-hooks
    - Create tailwind.config.ts and eslint.config.js
    - Create src/index.css with Tailwind directives
    - Create src/main.tsx entry point (minimal React render)
    - _Requirements: 14.4, 19.3, 19.4_

  - [x] 1.3 Create runtime configuration and public assets
    - Create public/config.json with apiBaseUrl and version fields
    - Create src/hooks/useRuntimeConfig.ts to read config at startup
    - Create src/utils/constants.ts for client-side constants
    - Add global type declaration for window.__OE2_CONFIG__
    - _Requirements: 20.1, 20.2, 20.3_

- [x] 2. Server-side static file serving
  - [x] 2.1 Add UseStaticFiles and SPA fallback to Program.cs
    - Add UseStaticFiles middleware with cache headers (immutable for hashed assets, no-cache for index.html)
    - Add MapFallbackToFile("index.html") for SPA routing
    - Ensure static files are served without authentication
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 3. TypeScript type generation setup
  - [x] 3.1 Create NSwag type generation script and configuration
    - Create scripts/generate-types.ts that invokes NSwag CLI
    - Create nswag.json configuration pointing to Common and Server DLLs
    - Configure output to src/api/types/generated.ts with auto-generated header
    - Handle C# enums → string unions, DateTime → string, Nullable → T | null, List → T[], Dictionary → Record
    - Add `npm run generate-types` script to package.json
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. API client layer
  - [x] 4.1 Create ky-based API client with auth interceptor
    - Install ky dependency
    - Create src/api/client.ts with ky instance, prefixUrl from runtime config
    - Add beforeRequest hook to attach Bearer token from auth store
    - Add afterResponse hook to clear auth on 401
    - Implement structured error handling (ApiError type with status, message, isNetworkError, isRetryable, validationErrors)
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [ ]* 4.2 Write property tests for API client auth and error handling
    - **Property 1: Authenticated requests include Bearer token**
    - **Property 2: 401 responses clear authentication state**
    - **Property 8: API errors produce structured error objects**
    - **Validates: Requirements 4.1, 4.3, 13.2, 13.3, 15.4**

  - [x] 4.3 Create endpoint modules for characters, factions, data, sharing, global
    - Create src/api/endpoints/characters.ts (list, get, switch)
    - Create src/api/endpoints/factions.ts (get faction, members, shared data)
    - Create src/api/endpoints/data.ts (CRUD for colonies, blueprints, surveys, profiles)
    - Create src/api/endpoints/sharing.ts (get/put sharing rules)
    - Create src/api/endpoints/global.ts (baseline data)
    - _Requirements: 13.1, 13.4, 13.5_

  - [x] 4.4 Create endpoint module for public data and colony planner
    - Create src/api/endpoints/public.ts (public blueprints, surveys with pagination)
    - Create src/api/endpoints/colony-planner.ts (status, build-order, eligibility)
    - _Requirements: 13.1, 8.1_

- [x] 5. Authentication flow (store + login page + guard)
  - [x] 5.1 Create Zustand auth store with persistence
    - Install zustand dependency
    - Create src/auth/store.ts with AuthState interface (token, characterName, characterUUID, role, isAuthenticated)
    - Implement login (validate token via GET /api/v1/characters), logout (clear state), switchCharacter
    - Use zustand/persist middleware with localStorage key 'oe2-auth'
    - _Requirements: 4.1, 4.5_

  - [x] 5.2 Create LoginPage and AuthGuard components
    - Create src/auth/LoginPage.tsx with token input form, error display, and redirect on success
    - Create src/auth/AuthGuard.tsx that redirects to /login if not authenticated (wraps Outlet)
    - Handle invalid/expired token display (Requirement 4.6)
    - _Requirements: 4.2, 4.3, 4.6_

- [x] 6. Checkpoint - Verify scaffolding builds and auth flow works
  - Ensure `npm run build` succeeds in OE2EmpireTracker.Web/
  - Ensure `npm run typecheck` passes with zero errors
  - Ensure `npm run lint` passes
  - Ask the user if questions arise.

- [x] 7. TanStack Query setup and data hooks
  - [x] 7.1 Configure TanStack Query provider and query key conventions
    - Install @tanstack/react-query
    - Create src/main.tsx QueryClientProvider setup with default options (retry logic, staleTime)
    - Create src/api/hooks/queryKeys.ts with hierarchical key factory
    - _Requirements: 15.1, 15.3_

  - [x] 7.2 Create data hooks for characters, blueprints, surveys, colonies
    - Create src/api/hooks/useCharacters.ts (useQuery for character list, character detail)
    - Create src/api/hooks/useBlueprints.ts (useQuery for blueprints with filters, useMutation for CRUD)
    - Create src/api/hooks/useSurveys.ts (useQuery for surveys with filters, useMutation for CRUD)
    - Create src/api/hooks/useColonies.ts (useQuery for colonies, useMutation for CRUD)
    - _Requirements: 10.2, 10.3, 10.4, 10.6_

  - [x] 7.3 Create data hooks for faction, sharing, and colony planner
    - Create src/api/hooks/useFaction.ts (faction membership, shared data)
    - Create src/api/hooks/useSharing.ts (sharing rules CRUD)
    - Create src/api/hooks/useColonyPlanner.ts (useMutation for status, build-order, eligibility)
    - _Requirements: 11.1, 11.2, 16.1, 9.3_

- [x] 8. WebSocket client
  - [x] 8.1 Implement WebSocketClient class with reconnection
    - Create src/ws/WebSocketClient.ts with connect, disconnect, ping (25s interval), exponential backoff reconnect
    - Handle connection URL construction from runtime config (http→ws replacement)
    - Implement max reconnect attempts (10), backoff formula: min(1000 * 2^N, 30000)
    - _Requirements: 12.1, 12.3, 12.4_

  - [x] 8.2 Create useWebSocket hook with TanStack Query cache invalidation
    - Create src/ws/useWebSocket.ts hook that manages WS lifecycle (connect on auth, disconnect on logout)
    - Implement handleServerEvent that maps entityType to query key invalidation
    - Wire into TanStack Query's queryClient.invalidateQueries
    - _Requirements: 12.2, 12.5_

  - [ ]* 8.3 Write property tests for WebSocket reconnection and event handling
    - **Property 6: WebSocket event triggers correct cache invalidation**
    - **Property 7: Reconnection uses exponential backoff**
    - **Validates: Requirements 12.2, 12.3**

- [x] 9. Layout and navigation components
  - [x] 9.1 Create AppShell, Sidebar, Header, Footer layout components
    - Install @radix-ui/react-collapsible (for sidebar collapse)
    - Create src/components/layout/AppShell.tsx (sidebar + header + content area with Outlet)
    - Create src/components/layout/Sidebar.tsx (collapsible nav links, mode-aware sections)
    - Create src/components/layout/Header.tsx (mode display, character name, logout button)
    - Create src/components/layout/Footer.tsx (version from runtime config)
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [x] 9.2 Create common UI components (loading, error, empty states)
    - Create src/components/common/LoadingSpinner.tsx
    - Create src/components/common/ErrorBoundary.tsx (route-level error boundary)
    - Create src/components/common/RetryableError.tsx (network/server error with retry button)
    - Create src/components/common/EmptyState.tsx
    - _Requirements: 15.1, 15.2, 15.3, 15.5_

  - [x] 9.3 Create DataTable and FilterBar reusable components
    - Create src/components/common/DataTable.tsx (sortable, paginated table)
    - Create src/components/common/FilterBar.tsx (generic filter controls)
    - _Requirements: 6.1, 7.1_

- [x] 10. Router setup and page structure
  - [x] 10.1 Configure React Router with public and authenticated route groups
    - Install react-router-dom
    - Create src/App.tsx with createBrowserRouter, public routes (/, /blueprints, /surveys, /planner, /login), authenticated routes (/app/*)
    - Wire AppShell as layout element, AuthGuard as authenticated wrapper
    - Create src/pages/NotFound.tsx for 404 handling
    - _Requirements: 1.5, 5.1, 5.4_

- [ ] 11. Public mode — Blueprint browser
  - [x] 11.1 Create BlueprintBrowser page with filtering
    - Create src/pages/public/BlueprintBrowser.tsx with DataTable, FilterBar (type, tech level, ship class)
    - Create src/components/domain/BlueprintCard.tsx for list item display
    - Wire to useBlueprints hook with filter state
    - Display ownership attribution when available
    - _Requirements: 6.1, 6.2, 6.4_

  - [x] 11.2 Create BlueprintDetail page
    - Create src/pages/public/BlueprintDetail.tsx showing full blueprint properties, evolution chain, manufacturing requirements
    - Use route param :id to fetch specific blueprint
    - _Requirements: 6.3_

  - [ ]* 11.3 Write property test for blueprint filter correctness
    - **Property 4: Blueprint filter correctness**
    - **Validates: Requirements 6.2**

- [ ] 12. Public mode — Survey browser
  - [x] 12.1 Create SurveyBrowser page with filtering
    - Create src/pages/public/SurveyBrowser.tsx with DataTable, FilterBar (system, resource type, purity level)
    - Create src/components/domain/SurveyCard.tsx for list item display
    - Wire to useSurveys hook with filter state
    - _Requirements: 7.1, 7.2_

  - [x] 12.2 Create SurveyDetail page
    - Create src/pages/public/SurveyDetail.tsx showing full resource breakdown and location details
    - Use route param :id to fetch specific survey
    - _Requirements: 7.3_

  - [ ]* 12.3 Write property test for survey filter correctness
    - **Property 5: Survey filter correctness**
    - **Validates: Requirements 7.2**

- [ ] 13. Checkpoint - Verify public mode pages render correctly
  - Ensure `npm run build` succeeds
  - Ensure `npm run typecheck` passes
  - Verify public routes are accessible without auth
  - Ask the user if questions arise.

- [ ] 14. Colony planner — Server-side endpoints
  - [ ] 14.1 Add colony planner controller with status and eligibility endpoints
    - Create ColonyPlannerController in OE2EmpireTracker.Server with POST /api/v1/colony-planner/status and POST /api/v1/colony-planner/eligibility
    - Accept ColonyPlannerRequest body, invoke ColonyStatusCalculator and ColonyBuildEligibility from Common library
    - Return computed results, no auth required
    - Return HTTP 400 with validation errors for invalid input
    - _Requirements: 8.1, 8.2, 8.4, 8.5, 8.6, 8.7_

  - [ ] 14.2 Add colony planner build-order endpoint
    - Add POST /api/v1/colony-planner/build-order to ColonyPlannerController
    - Accept ColonyPlannerRequest with targetStructures, invoke BuildOrderOptimizer from Common library
    - Return optimized build sequence with timing estimates
    - _Requirements: 8.3, 8.5_

- [ ] 15. Colony planner — Frontend interface
  - [ ] 15.1 Create ColonyPlanner page with structure panel
    - Create src/pages/planner/ColonyPlanner.tsx (main planner layout)
    - Create src/pages/planner/StructurePanel.tsx (add/remove/configure structures)
    - Create Zustand planner store for draft colony state with debounced API calls (300ms)
    - _Requirements: 9.1, 9.2, 9.5_

  - [ ] 15.2 Create StatusDisplay and BuildOrderView components
    - Create src/pages/planner/StatusDisplay.tsx (power, habitation, food, entertainment, warehouse bars)
    - Create src/components/domain/ColonyStatusBar.tsx (reusable status bar component)
    - Create src/pages/planner/BuildOrderView.tsx (sequenced build steps with resource requirements)
    - Create src/components/domain/BuildOrderList.tsx (step list with resource shortfall highlighting)
    - _Requirements: 9.3, 9.4, 9.7_

  - [ ] 15.3 Add auto-plan mode and colony loading
    - Add "Optimize Build Order" button that calls build-order endpoint with target structures
    - Allow reordering/removing steps from auto-generated plan
    - In authenticated mode, add "Load Colony" button to populate planner from existing colony data
    - _Requirements: 17.1, 17.2, 17.3, 9.6_

  - [ ] 15.4 Add plan saving (authenticated only)
    - Add "Save Plan" button (visible only when authenticated) to persist computed plan via server API
    - _Requirements: 17.4_

- [ ] 16. Authenticated mode — Dashboard and character management
  - [ ] 16.1 Create Dashboard page with character switcher
    - Create src/pages/authenticated/Dashboard.tsx (overview of character data, quick links)
    - Display character list for Owner role with switch functionality
    - Show character name and role in header (via auth store)
    - _Requirements: 10.1, 4.4_

  - [ ] 16.2 Create ColonyManager page
    - Create src/pages/authenticated/ColonyManager.tsx with DataTable for colonies
    - Implement CRUD operations (create, edit, delete) via useColonies mutations
    - Display colony status summary per colony
    - _Requirements: 10.2, 10.6_

  - [ ] 16.3 Create BlueprintManager page
    - Create src/pages/authenticated/BlueprintManager.tsx with DataTable for user's blueprints
    - Implement CRUD operations via useBlueprints mutations
    - _Requirements: 10.3, 10.6_

  - [ ] 16.4 Create SurveyManager page
    - Create src/pages/authenticated/SurveyManager.tsx with DataTable for user's surveys
    - Implement CRUD operations via useSurveys mutations
    - _Requirements: 10.4, 10.6_

  - [ ] 16.5 Create ProfileEditor page
    - Create src/pages/authenticated/ProfileEditor.tsx with read/edit interface for player profile data
    - Display skills, rank, profession with edit capability
    - _Requirements: 10.5, 10.6_

- [ ] 17. Authenticated mode — Faction view
  - [ ] 17.1 Create FactionView page
    - Create src/pages/authenticated/FactionView.tsx showing faction membership and member list
    - Display faction-shared data (blueprints, surveys, colonies) via sharing endpoints
    - Clearly distinguish own data from faction-shared data (visual indicator)
    - _Requirements: 11.1, 11.2, 11.3_

- [ ] 18. Authenticated mode — Sharing configuration
  - [ ] 18.1 Create SharingConfig page
    - Create src/pages/authenticated/SharingConfig.tsx with sharing rules list and editor
    - Allow creating rules targeting faction or specific character
    - Allow specifying shared data types per rule
    - Persist changes via PUT /api/v1/characters/{uuid}/sharing
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

- [ ] 19. Checkpoint - Verify authenticated mode pages
  - Ensure `npm run build` succeeds
  - Ensure `npm run typecheck` passes
  - Verify authenticated routes require auth (redirect to login without token)
  - Ask the user if questions arise.

- [ ] 20. Public mode access controls
  - [ ] 20.1 Ensure public mode hides all mutation controls
    - Audit all page components to conditionally render create/edit/delete buttons only when isAuthenticated
    - Add "Sign in for full access" prompt in public mode
    - Ensure colony planner is accessible without auth
    - _Requirements: 5.2, 5.3, 5.5, 5.6_

  - [ ]* 20.2 Write property test for public mode mutation hiding
    - **Property 3: Public mode hides mutation controls**
    - **Validates: Requirements 5.3**

- [ ] 21. Server-side public data endpoints
  - [ ] 21.1 Add public data endpoints to server
    - Create public endpoints under /api/v1/public/ (blueprints, surveys, colonies) without auth
    - Filter to only include data from characters with TargetType "Public" sharing rules
    - Redact private UUID references from public responses
    - Support pagination for large result sets
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5_

- [ ] 22. Error handling and loading states
  - [ ] 22.1 Wire error handling into all page components
    - Ensure every page uses query state to show LoadingSpinner, RetryableError, or EmptyState
    - Wire ErrorBoundary at route level in App.tsx
    - Ensure validation errors (4xx) display field-level messages
    - Ensure no blank screens — every state has a visual representation
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

- [ ] 23. Utility functions and formatters
  - [ ] 23.1 Create utility formatters for dates, numbers, and resources
    - Create src/utils/formatters.ts with date formatting (ISO → display), number formatting, resource quantity formatting
    - Wire formatters into DataTable and detail pages
    - _Requirements: 14.4_

- [ ] 24. Responsive layout polish
  - [ ] 24.1 Ensure responsive layout works from 768px to 1920px
    - Add Tailwind responsive breakpoints to AppShell, Sidebar, DataTable
    - Make sidebar collapsible on viewports < 1024px
    - Ensure DataTable scrolls horizontally on narrow viewports
    - _Requirements: 14.1, 14.2_

- [ ] 25. Build integration and deployment verification
  - [ ] 25.1 Verify full build pipeline end-to-end
    - Ensure `npm run build` outputs to OE2EmpireTracker.Server/wwwroot/ with hashed filenames
    - Verify index.html references hashed asset paths
    - Verify `dotnet publish` includes wwwroot/ in output package
    - Test that SPA fallback serves index.html for unknown routes
    - _Requirements: 1.2, 1.3, 1.4, 1.5_

  - [ ] 25.2 Verify reverse proxy and path prefix support
    - Ensure runtime config pathPrefix is applied to all route and API paths
    - Verify X-Forwarded-* header handling works with UseForwardedHeaders
    - _Requirements: 20.2, 20.4_

- [ ] 26. Final checkpoint - Full integration verification
  - Ensure `npm run build` succeeds with zero errors
  - Ensure `npm run typecheck` passes
  - Ensure `npm run lint` passes
  - Verify all routes render appropriate content
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Server-side tasks (2, 14, 21) modify the OE2EmpireTracker.Server project
- All other tasks modify files within OE2EmpireTracker.Web/
