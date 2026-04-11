# Implementation Plan: Colony Stable UUID

## Overview

Fix structure duplication on colony reimport (stale FlatpackBlueprintUUID references after blueprint migration) and add deterministic colony UUIDs with data migration. Extends the existing DeterministicUUID/RemapUUID/MigrationRunner infrastructure.

## Tasks

- [x] 1. Extend Colony model and DeterministicUUID
  - [x] 1.1 Add `LegacyUUID` string property to Colony.cs (default null, `[DefaultValue(null)]`) matching the Blueprint pattern
    - _Requirements: 2.6_
  - [x] 1.2 Add colony overloads to DeterministicUUID.cs — `Generate(Colony)` and `Generate(string ownerUUID, string planetName, string systemName)` using a separate ColonyNamespace GUID and `GenerateV5`
    - _Requirements: 2.3_
  - [x] 1.3 Write property test for deterministic colony UUID round-trip
    - **Property 1: Deterministic colony UUID round-trip**
    - Generate random (ownerUUID, planetName, systemName) triples, assert Generate called twice yields same value; different triples yield different UUIDs
    - **Validates: Requirements 2.3**

- [-] 2. Extend RemapUUID to walk colony references
  - [-] 2.1 Add Colony.UUID, RouteStop.ColonyUUID, and DeliveryPlanStop.ColonyUUID walking to RemapUUID.Remap()
    - _Requirements: 2.5_
  - [~] 2.2 Write property test for RemapUUID colony reference walking
    - **Property 3: RemapUUID walks all colony references**
    - Generate colonies, delivery routes, and delivery plans with matching ColonyUUIDs, call Remap, assert all references updated
    - **Validates: Requirements 2.5**

- [ ] 3. Implement Migration002_ColonyDeterministicUUIDs
  - [~] 3.1 Create Migration002_ColonyDeterministicUUIDs.cs with Phase 1 (stale FlatpackBlueprintUUID cleanup via LegacyUUID lookup) and Phase 2 (deterministic colony UUID assignment via RemapUUID.Remap)
    - Add `<Compile Include>` entry to OE2EmpireTracker.csproj
    - _Requirements: 2.1, 2.4_
  - [~] 3.2 Register Migration002 in MigrationRunner.cs — add to Migrations dictionary at key 2, bump CurrentVersion to 2
    - _Requirements: 2.4_
  - [~] 3.3 Write property test for stale FlatpackBlueprintUUID cleanup
    - **Property 4: Stale FlatpackBlueprintUUID cleanup**
    - Generate colonies with structures referencing blueprint LegacyUUIDs, run Migration002, assert FlatpackBlueprintUUID remapped to current UUID
    - **Validates: Requirements 2.1**
  - [~] 3.4 Write property test for colony UUID migration with LegacyUUID preservation
    - **Property 2: Colony UUID migration preserves LegacyUUID**
    - Generate colonies with random UUIDs, run migration, assert LegacyUUID == original UUID and UUID == deterministic UUID; run again, assert LegacyUUID unchanged
    - **Validates: Requirements 2.4, 2.6**
  - [~] 3.5 Write property test for migration idempotency
    - **Property 5: Migration idempotency**
    - Run Migration002 twice on same data, assert colony UUIDs, LegacyUUIDs, and all delivery route/plan references identical after both runs
    - **Validates: Requirements 3.4**

- [ ] 4. Update ColonyImportHelper to use deterministic UUIDs
  - [~] 4.1 Replace `Guid.NewGuid()` with `DeterministicUUID.Generate(ownerUUID, tempColony.PlanetName, tempColony.SystemName)` in ColonyImportHelper.CreateFromTemp
    - _Requirements: 2.3_

- [ ] 5. Update csproj and verify build
  - [~] 5.1 Ensure OE2EmpireTracker.csproj has `<Compile Include>` for Migration002_ColonyDeterministicUUIDs.cs
    - _Requirements: 2.4_
  - [~] 5.2 Ensure OE2EmpireTracker.Tests.csproj has `<Compile Include>` entries for all new test files
    - _Requirements: 2.4_

- [ ] 6. Final checkpoint — Ensure all tests pass
  - Build solution and run all tests. Ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- Property tests validate universal correctness properties from the design document
- The migration reuses the existing DeterministicUUID/RemapUUID/MigrationRunner infrastructure
- New .cs files require `<Compile Include>` entries in the old-style csproj files
- Do NOT use `dotnet test` — use vstest.console against the built test DLL
- Do NOT use `semanticRename` — it doesn't work with this project type
