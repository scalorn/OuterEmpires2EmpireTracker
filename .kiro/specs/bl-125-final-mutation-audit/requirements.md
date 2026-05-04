# Requirements Document

## Introduction

BL-125 is the capstone validation that the immutable data model is fully enforced across ALL entity types. No new code is created --- only verification tests. This extends the mutation guard test pattern to cover every entity type and verifies that WriteContext() is only called from service classes, and that no form or ViewModel directly sets entity properties.

### Key Characteristics

1. **No new code** --- Only verification tests.
2. **Extends mutation guard tests** --- Covers ALL entity types.
3. **WriteContext() audit** --- Verifies WriteContext() is only called from service classes and PlayerContext itself.
4. **Comprehensive static analysis** --- Greps the entire codebase for entity property sets and mutation patterns.

## Glossary

- **Mutation Guard Test**: A static analysis test that greps the codebase for direct entity property sets and asserts they only appear in allowed files.
- **WriteContext()**: The PlayerContext method that persists all data to JSON.
- **Service Classes**: The sole mutators of entities.

## Requirements

### Requirement 1: All Entity Types Have Mutation Guard Tests
#### Acceptance Criteria
1. EVERY migrated entity type SHALL have a mutation guard test.
2. THE tests SHALL cover: Blueprint, Colony, Survey, PlayerProfile, DeliveryRoute, PricingPlan, ShipTemplate, Ship, Station, BuildPlan, StockPlan, StockProfile, SupplyChain, Faction, ExternalCharacter, Asteroid.
3. Each test SHALL verify property sets only appear in allowed files.

### Requirement 2: WriteContext() Audit
#### Acceptance Criteria
1. A test SHALL grep for WriteContext() calls across the codebase.
2. WriteContext() SHALL only appear in service classes, PlayerContext, and test code.
3. WriteContext() SHALL NOT appear in Form*.cs or *ViewModel.cs files.

### Requirement 3: No Form or ViewModel Directly Sets Entity Properties
#### Acceptance Criteria
1. A test SHALL confirm no Form*.cs file directly sets entity properties.
2. A test SHALL confirm no *ViewModel.cs file directly sets entity properties.

### Requirement 4: Comprehensive Coverage
#### Acceptance Criteria
1. Tests SHALL scan ALL .cs files in OE2EmpireTracker/ (excluding Designer.cs).
2. Tests SHALL report violations with file name and line number.
3. Tests SHALL have an allowlist for known acceptable patterns.

## Correctness Properties

### Property 1: No Direct Entity Mutation Outside Services
### Property 2: WriteContext() Only in Services
### Property 3: Forms and ViewModels Are Clean

## Out of Scope

- Creating new services or ViewModels.
- Fixing violations found by the audit.
- Runtime verification.
