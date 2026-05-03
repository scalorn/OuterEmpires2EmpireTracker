# Requirements Document

## Introduction

BL-123 restructures FormPricingPlan so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a PricingPlanService that applies changes atomically. The form never directly mutates a PricingPlan --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints) and BL-111 (player profiles).

### Key Differences from BL-108/BL-111

1. **Simpler model** --- PricingPlan has flat scalar fields (UUID, Name, OwnerUUID, Description, FixedCostPerItem, HourlyCostRate) plus one `Dictionary<string, decimal>` (ResourcePrices). No nested objects like PlayerRank or PlayerSkill.
2. **No import** --- PricingPlan has no clipboard import feature. No Import method needed on the service.
3. **No move** --- No global/player split like Blueprint. Plans are always player-scoped.
4. **Resource price grid** --- The DataGridView for resource prices populates from `Resource.Resources` (game constants) and reads prices from the plan ResourcePrices dictionary. Clearing a cell removes the entry (unpriced); setting to zero means free.
5. **Validation** --- Name cannot be empty, costs and prices cannot be negative.
6. **ReadOnlyPricingPlan already complete** --- The existing ReadOnlyPricingPlan already exposes all properties the form needs. No gap fill required.
7. **No timer** --- Unlike PlayerProfile skill training timer, PricingPlan has no live countdown or timer logic.

## Glossary

- **PricingPlan**: The mutable entity representing a named pricing configuration with base resource prices and time-cost parameters.
- **ReadOnlyPricingPlan**: An immutable wrapper around PricingPlan that exposes only getter properties and a read-only ResourcePrices dictionary.
- **PricingPlanViewModel**: A new ViewModel class that holds a local edit buffer of all PricingPlan fields, disconnected from the entity.
- **PricingPlanService**: A new service class that is the sole mutator of PricingPlan entities (create, update, delete).
- **FormPricingPlan**: The WinForms form for viewing and editing pricing plans.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of field values in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any local field differs from the original snapshot.
- **PricingPlanUpdateRequest**: A DTO carrying the current local state from the ViewModel to the service for an update operation.
- **PricingPlanCreateRequest**: A DTO carrying field values for creating a new pricing plan.
- **ResourcePrices**: A dictionary keyed by composite string ResourceName|Purity mapping to decimal credit values.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: ReadOnlyPricingPlan Completeness

**User Story:** As a developer, I want to confirm ReadOnlyPricingPlan already exposes all properties the form needs, so that no gap fill is required.

#### Acceptance Criteria

1. THE ReadOnlyPricingPlan SHALL expose UUID, Name, OwnerUUID, Description, FixedCostPerItem, and HourlyCostRate as read-only properties.
2. THE ReadOnlyPricingPlan SHALL expose ResourcePrices as an IReadOnlyDictionary<string, decimal>.
3. THE ReadOnlyPricingPlan SHALL NOT expose any setters or mutation methods.

### Requirement 2: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the plan list view to store ReadOnlyPricingPlan in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormPricingPlan populates the list view, THE FormPricingPlan SHALL create ListViewItem Tags containing ReadOnlyPricingPlan instances obtained from PlayerContext.GetCurrentPlayerReadOnlyPricingPlans().
2. WHEN the user selects a plan in the list view, THE FormPricingPlan SHALL extract the ReadOnlyPricingPlan from the selected item's Tag and pass it to the ViewModel's LoadFrom method.
3. WHEN the FormPricingPlan filters the plan list, THE FormPricingPlan SHALL use ReadOnlyPricingPlan.Name for the filter comparison.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable PricingPlan, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormPricingPlan SHALL NOT hold a direct reference to a mutable PricingPlan in any read-only code path (list view Tags, display-only fields, filter logic).
2. THE PricingPlanViewModel SHALL NOT expose the mutable PricingPlan entity via a public property or accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyPricingPlan into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a plan, THE PricingPlanViewModel SHALL copy all scalar field values from the ReadOnlyPricingPlan into local properties: Name, Description, FixedCostPerItem, HourlyCostRate.
2. WHEN the user selects a plan, THE PricingPlanViewModel SHALL deep-copy the ResourcePrices dictionary into a local Dictionary<string, decimal>.
3. THE PricingPlanViewModel SHALL retain the original ReadOnlyPricingPlan snapshot for dirty comparison.
4. THE PricingPlanViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
5. THE PricingPlanViewModel SHALL NOT hold a reference to the mutable PricingPlan entity.

### Requirement 5: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want all editable controls to read from and write to the ViewModel's local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormPricingPlan text boxes (txtPlanName, txtDescription, txtFixedCost, txtHourlyCost) SHALL read from and write to the PricingPlanViewModel's local fields.
2. THE FormPricingPlan resource price grid (dgvResourcePrices) SHALL read from and write to the PricingPlanViewModel's local ResourcePrices dictionary.

### Requirement 6: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the PricingPlan entity on every keystroke, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE PricingPlanViewModel SHALL NOT write changes to the PricingPlan entity on every keystroke or control change.
2. THE current write-through pattern (TextChanged handler sets entity property directly) SHALL be replaced with local-only state changes in the ViewModel.
3. THE DgvResourcePrices CellValueChanged handler SHALL update the ViewModel's local ResourcePrices dictionary instead of the mutable entity's ResourcePrices.

### Requirement 7: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether any field has been modified since the last load or save, so that the Save button enables only when changes exist.

#### Acceptance Criteria

1. THE PricingPlanViewModel SHALL expose an IsDirty property that returns true when any local field differs from the original ReadOnlyPricingPlan snapshot.
2. THE IsDirty check SHALL compare all scalar fields: Name, Description, FixedCostPerItem, HourlyCostRate.
3. THE IsDirty check SHALL compare the ResourcePrices dictionary (same keys and same values).
4. WHEN the ViewModel is loaded from a ReadOnlyPricingPlan, THE IsDirty property SHALL return false.
5. WHEN the ViewModel represents a new unsaved plan (original is null), THE IsDirty property SHALL return true once any field has a non-default value.
6. THE FormPricingPlan SHALL enable the Save button only when the PricingPlanViewModel IsDirty property returns true.

### Requirement 8: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different plan, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different plan in the list view and the PricingPlanViewModel is dirty, THE FormPricingPlan SHALL prompt: `Save changes to '{name}'?` with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormPricingPlan SHALL call PricingPlanService.Update, then load the new selection.
3. WHEN the user chooses Discard, THE FormPricingPlan SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormPricingPlan SHALL cancel the selection change and keep the current plan selected.

### Requirement 9: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the pricing plan form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormPricingPlan (X button or MDI close) and the PricingPlanViewModel is dirty, THE FormPricingPlan SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormPricingPlan SHALL save and then close.
3. WHEN the user chooses Discard, THE FormPricingPlan SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormPricingPlan SHALL cancel the close and keep the form open.

### Requirement 10: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormPricingPlan has unsaved changes, THE FormPricingPlan OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormPricingPlan SHALL cancel the application exit by setting e.Cancel to true.

### Requirement 11: Unsaved Changes Prompt on New Plan

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the PricingPlanViewModel is dirty, THE FormPricingPlan SHALL prompt before clearing the form for the new plan.
2. WHEN the user chooses Save, THE FormPricingPlan SHALL save the current plan, then reset the form for a new plan.
3. WHEN the user chooses Discard, THE FormPricingPlan SHALL discard changes and reset the form for a new plan.
4. WHEN the user chooses Cancel, THE FormPricingPlan SHALL cancel the New operation and keep the current plan.


## Phase 3: PricingPlanService

### Requirement 12: PricingPlanService.Update

**User Story:** As a developer, I want a service method that applies plan changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE PricingPlanService SHALL provide an Update method accepting a UUID string and a PricingPlanUpdateRequest.
2. WHEN Update is called, THE PricingPlanService SHALL look up the mutable PricingPlan by UUID via PlayerContext.FindMutablePricingPlan.
3. WHEN Update is called, THE PricingPlanService SHALL apply all changed scalar fields from the request to the entity: Name, Description, FixedCostPerItem, HourlyCostRate.
4. WHEN Update is called, THE PricingPlanService SHALL replace the entity's ResourcePrices dictionary with the request's ResourcePrices dictionary.
5. WHEN Update is called, THE PricingPlanService SHALL persist via PlayerContext.WriteContext().
6. WHEN Update is called, THE PricingPlanService SHALL fire PricingDataChanged event.
7. WHEN Update is called, THE PricingPlanService SHALL return the updated ReadOnlyPricingPlan.
8. IF the UUID is not found, THEN THE PricingPlanService SHALL throw an InvalidOperationException.

### Requirement 13: PricingPlanService.Create

**User Story:** As a developer, I want a service method that creates a new plan, so that plan creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE PricingPlanService SHALL provide a Create method accepting a PricingPlanCreateRequest.
2. WHEN Create is called, THE PricingPlanService SHALL create a new PricingPlan entity with a generated UUID.
3. WHEN Create is called, THE PricingPlanService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE PricingPlanService SHALL populate all fields from the request: Name, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices.
5. WHEN Create is called, THE PricingPlanService SHALL add the plan to PlayerContext via AddPricingPlan.
6. WHEN Create is called, THE PricingPlanService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE PricingPlanService SHALL fire PricingDataChanged event.
8. WHEN Create is called, THE PricingPlanService SHALL return the new ReadOnlyPricingPlan.

### Requirement 14: PricingPlanService.Delete

**User Story:** As a developer, I want a service method that deletes a plan, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE PricingPlanService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE PricingPlanService SHALL remove the PricingPlan from PlayerContext via RemovePricingPlan.
3. WHEN Delete is called, THE PricingPlanService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE PricingPlanService SHALL fire PricingDataChanged event.
5. IF the UUID is empty or the plan is not found, THEN THE PricingPlanService SHALL return without error.

### Requirement 15: PlayerContext.FindMutablePricingPlan

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable PricingPlan entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutablePricingPlan internal method accepting a UUID string.
2. THE FindMutablePricingPlan method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint.
3. THE FindMutablePricingPlan method SHALL be marked internal so only the service project can access it.

### Requirement 16: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new plan (IsNew is true), THE FormPricingPlan SHALL call PricingPlanService.Create with a PricingPlanCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing plan, THE FormPricingPlan SHALL call PricingPlanService.Update with the UUID and a PricingPlanUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyPricingPlan, THE FormPricingPlan SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyPricingPlan.
4. AFTER a successful save, THE PricingPlanViewModel IsDirty property SHALL return false.

### Requirement 17: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the PricingPlan entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE PricingPlan entity SHALL only be mutated by PricingPlanService methods (Update, Create, Delete), JSON deserialization (loading from file), and migration code.
2. THE FormPricingPlan SHALL NOT directly set properties on a PricingPlan.
3. THE PricingPlanViewModel SHALL NOT directly set properties on a PricingPlan.


## Phase 4: Verification

### Requirement 18: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 19: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 20: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates PricingPlan entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct PricingPlan property sets SHALL only find matches in PricingPlanService, JSON deserialization, migration code, and the PricingPlan class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid PricingPlan entities, wrapping in ReadOnlyPricingPlan and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity's fields (Name, Description, FixedCostPerItem, HourlyCostRate, and every key-value pair in ResourcePrices).

**Validates:** Requirements 4.1, 4.2

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid PricingPlan entities, wrapping in ReadOnlyPricingPlan and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 7.1, 7.4

### Property 3: IsDirty Detects Any Single Field Change

FOR ALL valid PricingPlan entities, after LoadFrom, changing any single field (Name, Description, FixedCostPerItem, HourlyCostRate, or any single ResourcePrices entry) to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 7.1, 7.2, 7.3

### Property 4: Service.Update Round-Trip

FOR ALL valid existing PricingPlan entities and valid PricingPlanUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyPricingPlan whose fields match the request values (Name, Description, FixedCostPerItem, HourlyCostRate, ResourcePrices).

**Validates:** Requirements 12.3, 12.4, 12.7

### Property 5: Service.Create Round-Trip

FOR ALL valid PricingPlanCreateRequest values, calling Service.Create SHALL produce a ReadOnlyPricingPlan whose fields match the request values and whose UUID is non-empty.

**Validates:** Requirements 13.2, 13.4, 13.8

### Property 6: Service.Delete Removes Plan

FOR ALL valid existing PricingPlan entities, calling Service.Delete with the plan's UUID SHALL cause the plan to no longer be findable via PlayerContext.

**Validates:** Requirements 14.1, 14.2

### Property 7: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct PricingPlan property sets SHALL only find matches in PricingPlanService, JSON deserialization, and the PricingPlan class itself.

**Validates:** Requirements 17.1, 17.2, 17.3, 20.1

## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Clipboard import for pricing plans --- PricingPlan has no import feature.
- Changing the PriceCalculator or computed price logic --- this BL only restructures the form and data mutation path.
- Modifying the resource price grid's visual design or adding new resource types.
- Multi-currency or exchange rate support.
