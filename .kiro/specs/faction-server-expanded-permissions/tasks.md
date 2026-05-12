# Faction Server Expanded Permissions — Tasks

> **Status:** Blocked — awaiting requirements iteration.

## Phase 1: Requirements Finalization

- [ ] 1.1 Review and adapt requirements to OE2 data model
- [ ] 1.2 Resolve open questions (capability inheritance, cross-faction clearance, template complexity, intel scope, feature vs capability distinction)
- [ ] 1.3 Define API surface (new endpoints vs extensions to existing)
- [ ] 1.4 Finalize data model (storage entities, relationships)

## Phase 2: Design

- [ ] 2.1 Design capability storage and resolution algorithm
- [ ] 2.2 Design group model and sharing template application
- [ ] 2.3 Design clearance level filtering integration with existing sharing rules
- [ ] 2.4 Design intel comment storage and retrieval
- [ ] 2.5 Design permission audit trail storage and retention
- [ ] 2.6 Design API endpoints and request/response schemas

## Phase 3: Implementation

- [ ] 3.1 Implement capability CRUD and grant/revoke endpoints
- [ ] 3.2 Implement group CRUD and member assignment
- [ ] 3.3 Implement clearance level storage and filtering
- [ ] 3.4 Implement intel comment CRUD with clearance filtering
- [ ] 3.5 Implement feature flag storage and enforcement
- [ ] 3.6 Implement permission audit trail logging
- [ ] 3.7 Integrate with existing auth middleware (effective permission computation)
- [ ] 3.8 Update sharing rule resolution to consider clearance levels

## Phase 4: Testing

- [ ] 4.1 Unit tests for permission resolution (role + group + individual capabilities)
- [ ] 4.2 Unit tests for clearance level filtering
- [ ] 4.3 Integration tests for group assignment cascading
- [ ] 4.4 Integration tests for intel comment visibility
- [ ] 4.5 Integration tests for audit trail completeness
