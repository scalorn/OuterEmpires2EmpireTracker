# Requirements Document

## Introduction

BL-114 restructures FormMarket, FormListingEdit, and FormRecordSale so that all data access goes through ReadOnly wrappers, the form uses ReadOnlyMarketListing in grid Tags, and all mutations go through a new MarketListingService. The form never directly mutates a MarketListing --- only the service does. This follows the same immutable data model pattern established in BL-108 through BL-123.

### Key Differences from BL-112 (DeliveryRoute)

1. **Grid-based form, not detail form** --- FormMarket is a grid-based listing/transaction viewer with Add/Edit/Delete/RecordSale buttons. Edits happen via modal dialogs (FormListingEdit, FormRecordSale).
2. **No ViewModel edit buffer** --- Edits happen in modal dialogs, not inline. No persistent ViewModel with dirty tracking or unsaved changes prompts.
3. **No unsaved changes prompts** --- Each operation (Add, Edit, Delete, RecordSale) is atomic and immediate via the service.
4. **Multiple mutation types** --- CreateListing, UpdateListing, DeleteListing, and RecordSale. DeliveryRoute had only Create, Update, Delete.
5. **RecordSale creates a transaction** --- Decrements listing quantity AND creates a MarketTransaction atomically.
6. **Existing static MarketService remains** --- Stateless computation methods coexist with the new instance service.
7. **ReadOnly wrappers already complete** --- ReadOnlyMarketListing and ReadOnlyMarketTransaction are fully implemented.
8. **Reference protection uses transactions** --- MarketListingReferenceCounter counts MarketTransaction.ListingUUID references.
9. **Dialog-based editing** --- FormListingEdit returns edited values as properties; the service applies changes.
10. **FormRecordSale already returns values** --- Returns SaleQuantity, SalePricePerUnit, Counterparty, CounterpartyFaction.
### Similarities to BL-112

1. **ReadOnly wrappers in grid Tags** --- Grid row Tags store ReadOnlyMarketListing instead of mutable MarketListing.
2. **Service as sole mutator** --- Form -> service -> entity. No direct entity mutation from forms.
3. **Always player-scoped** --- Listings and transactions are always owned by the current player.
4. **Delete reference protection** --- MarketListingReferenceCounter checks transactions before allowing deletion.
5. **PlayerContext.FindMutableMarketListing** --- Same internal method pattern for service-only access.
6. **Request DTOs** --- MarketListingCreateRequest and MarketListingUpdateRequest carry data from dialog to service.

### Scoping Decision: No ViewModel

Unlike BL-112 (DeliveryRoute) which has a detail panel with text boxes requiring a ViewModel edit buffer, FormMarket is a grid-based form where all edits happen via modal dialogs. Each dialog collects input, returns it, and the service applies the mutation atomically. There is no need for a persistent ViewModel with dirty tracking, unsaved changes prompts, or LoadFrom/BuildRequest patterns. The dialogs themselves serve as the transient edit buffer.

### Scoping Decision: FormListingEdit Returns Properties

FormListingEdit currently receives a mutable MarketListing reference and mutates it directly in the OK handler. After migration, FormListingEdit will receive a ReadOnlyMarketListing (for edit) or null (for add), populate its controls from the read-only data, and expose the edited values as public properties. The parent form builds a request DTO from these properties and passes it to the service.

### Scoping Decision: RecordSale Moves to Service

The parent form currently calls MarketService.RecordSale (static), then adds the transaction and persists. After migration, MarketListingService.RecordSale handles the entire operation: decrement listing quantity, create transaction, add transaction to PlayerContext, persist, fire event.
## Glossary

- **MarketListing**: The mutable entity representing a market listing with ItemName, ItemType, Quantity, PricePerUnit, StationUUID, condition fields (CurrentHP, MaxHP, MaxRepairPercent), and ownership (OwnerUUID).
- **MarketTransaction**: The mutable entity representing a completed buy or sell transaction with item details, pricing, counterparty info, and a ListingUUID linking back to the source listing.
- **ReadOnlyMarketListing**: An immutable wrapper around MarketListing that exposes only getter properties.
- **ReadOnlyMarketTransaction**: An immutable wrapper around MarketTransaction that exposes only getter properties.
- **MarketListingService**: A new instance service class that is the sole mutator of MarketListing entities (create, update, delete, record sale).
- **MarketService**: The existing static service with RecordSale, RecordPurchase, and ComputeProfitLoss. Stateless computation methods. Retained as-is.
- **FormMarket**: The main WinForms form with Listings grid, Transactions grid, and Summary tab.
- **FormListingEdit**: A modal dialog for creating or editing a single listing. Returns edited values to the parent form.
- **FormRecordSale**: A modal dialog for recording a sale against a listing. Returns sale details to the parent form.
- **MarketListingCreateRequest**: A DTO carrying field values for creating a new listing.
- **MarketListingUpdateRequest**: A DTO carrying field values for updating an existing listing.
- **MarketListingReferenceCounter**: A utility that counts how many transactions reference a given listing UUID, used for delete protection.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: Listings Grid Uses ReadOnly Wrappers

**User Story:** As a developer, I want the listings grid to store ReadOnlyMarketListing in row Tags, so that no mutable entity references leak into the grid.

#### Acceptance Criteria

1. WHEN the FormMarket populates the listings grid (dgvListings), THE FormMarket SHALL create DataGridViewRow Tags containing ReadOnlyMarketListing instances obtained from PlayerContext.GetCurrentPlayerReadOnlyListings().
2. WHEN the user selects a listing row for edit, delete, or record sale, THE FormMarket SHALL extract the ReadOnlyMarketListing from the selected row Tag.
3. WHEN the FormMarket displays listing data in the grid, THE FormMarket SHALL use ReadOnlyMarketListing properties for all display values (StationUUID, ItemName, ItemType, Quantity, PricePerUnit, condition).
### Requirement 2: Transactions Grid Uses ReadOnly Wrappers

**User Story:** As a developer, I want the transactions grid to use ReadOnlyMarketTransaction data, so that no mutable entity references leak into the grid.

#### Acceptance Criteria

1. WHEN the FormMarket populates the transactions grid (dgvTransactions), THE FormMarket SHALL use ReadOnlyMarketTransaction instances obtained from PlayerContext.GetCurrentPlayerReadOnlyTransactions().
2. WHEN the FormMarket filters and displays transactions, THE FormMarket SHALL use ReadOnlyMarketTransaction properties for all filter comparisons and display values.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable MarketListing or MarketTransaction, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormMarket SHALL NOT hold a direct reference to a mutable MarketListing in any read-only code path (grid Tags, display-only fields, filter logic, reference counter display).
2. AFTER Phase 1 migration, THE FormMarket SHALL NOT hold a direct reference to a mutable MarketTransaction in any read-only code path (grid Tags, display-only fields, filter logic).


## Phase 2: MarketListingService

### Requirement 4: MarketListingService.CreateListing

**User Story:** As a developer, I want a service method that creates a new listing, so that listing creation goes through a controlled gate.

#### Acceptance Criteria

1. THE MarketListingService SHALL provide a CreateListing method accepting a MarketListingCreateRequest.
2. WHEN CreateListing is called, THE MarketListingService SHALL create a new MarketListing entity with a generated UUID.
3. WHEN CreateListing is called, THE MarketListingService SHALL set the OwnerUUID to the current player UUID.
4. WHEN CreateListing is called, THE MarketListingService SHALL populate all fields from the request (ItemName, ItemType, ItemReferenceID, StationUUID, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent).
5. WHEN CreateListing is called, THE MarketListingService SHALL add the listing to PlayerContext via AddMarketListing.
6. WHEN CreateListing is called, THE MarketListingService SHALL invalidate the market listing cache.
7. WHEN CreateListing is called, THE MarketListingService SHALL persist via PlayerContext.WriteContext().
8. WHEN CreateListing is called, THE MarketListingService SHALL fire MarketDataChanged event.
9. WHEN CreateListing is called, THE MarketListingService SHALL return the new ReadOnlyMarketListing.
### Requirement 5: MarketListingService.UpdateListing

**User Story:** As a developer, I want a service method that applies listing changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE MarketListingService SHALL provide an UpdateListing method accepting a UUID string and a MarketListingUpdateRequest.
2. WHEN UpdateListing is called, THE MarketListingService SHALL look up the mutable MarketListing by UUID via PlayerContext.FindMutableMarketListing.
3. WHEN UpdateListing is called, THE MarketListingService SHALL apply all fields from the request to the entity (ItemName, ItemType, ItemReferenceID, StationUUID, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent).
4. WHEN UpdateListing is called, THE MarketListingService SHALL invalidate the market listing cache.
5. WHEN UpdateListing is called, THE MarketListingService SHALL persist via PlayerContext.WriteContext().
6. WHEN UpdateListing is called, THE MarketListingService SHALL fire MarketDataChanged event.
7. WHEN UpdateListing is called, THE MarketListingService SHALL return the updated ReadOnlyMarketListing.
8. IF the UUID is not found, THEN THE MarketListingService SHALL throw an InvalidOperationException.

### Requirement 6: MarketListingService.DeleteListing

**User Story:** As a developer, I want a service method that deletes a listing, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE MarketListingService SHALL provide a DeleteListing method accepting a UUID string.
2. WHEN DeleteListing is called, THE MarketListingService SHALL remove the MarketListing from PlayerContext via RemoveMarketListing.
3. WHEN DeleteListing is called, THE MarketListingService SHALL invalidate the market listing cache.
4. WHEN DeleteListing is called, THE MarketListingService SHALL persist via PlayerContext.WriteContext().
5. WHEN DeleteListing is called, THE MarketListingService SHALL fire MarketDataChanged event.
6. IF the UUID is empty or the listing is not found, THEN THE MarketListingService SHALL return without error.
### Requirement 7: MarketListingService.RecordSale

**User Story:** As a developer, I want a service method that records a sale atomically (decrement listing quantity + create transaction + persist), so that the entire sale operation goes through the controlled gate.

#### Acceptance Criteria

1. THE MarketListingService SHALL provide a RecordSale method accepting a listing UUID, quantity, pricePerUnit, counterparty, counterpartyFaction, and stationUUID.
2. WHEN RecordSale is called, THE MarketListingService SHALL look up the mutable MarketListing by UUID via PlayerContext.FindMutableMarketListing.
3. WHEN RecordSale is called, THE MarketListingService SHALL delegate to the existing static MarketService.RecordSale to decrement quantity and create the MarketTransaction.
4. WHEN RecordSale is called and the static method returns a valid transaction, THE MarketListingService SHALL add the transaction to PlayerContext via AddMarketTransaction.
5. WHEN RecordSale is called, THE MarketListingService SHALL invalidate the market listing cache.
6. WHEN RecordSale is called, THE MarketListingService SHALL persist via PlayerContext.WriteContext().
7. WHEN RecordSale is called, THE MarketListingService SHALL fire MarketDataChanged event.
8. WHEN RecordSale is called, THE MarketListingService SHALL return the created MarketTransaction (or null if validation failed).
9. IF the listing UUID is not found, THEN THE MarketListingService SHALL throw an InvalidOperationException.

### Requirement 8: PlayerContext.FindMutableMarketListing

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable MarketListing entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableMarketListing internal method accepting a UUID string.
2. THE FindMutableMarketListing method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableSurvey, and FindMutableColony.
3. THE FindMutableMarketListing method SHALL be marked internal so only the service project can access it.

## Phase 3: Form Migration

### Requirement 9: FormMarket Add Listing Through Service

**User Story:** As a developer, I want the Add button to create listings through the service, so that the form never directly creates a MarketListing entity.

#### Acceptance Criteria

1. WHEN the user clicks Add, THE FormMarket SHALL call MarketListingService.CreateListing with a MarketListingCreateRequest containing default values.
2. THE FormMarket SHALL NOT directly construct a MarketListing entity or call PlayerContext.AddMarketListing.
3. THE FormMarket SHALL NOT directly call PlayerContext.WriteContext() for listing creation.

### Requirement 10: FormMarket Edit Listing Through Service

**User Story:** As a developer, I want the Edit button to update listings through the service, so that the form never directly mutates a MarketListing entity.

#### Acceptance Criteria

1. WHEN the user clicks Edit, THE FormMarket SHALL open FormListingEdit with the ReadOnlyMarketListing from the selected row Tag.
2. WHEN FormListingEdit returns DialogResult.OK, THE FormMarket SHALL build a MarketListingUpdateRequest from the dialog properties and call MarketListingService.UpdateListing.
3. THE FormMarket SHALL NOT directly set properties on a MarketListing entity.
4. THE FormMarket SHALL NOT directly call PlayerContext.WriteContext() for listing edits.

### Requirement 11: FormMarket Delete Listing Through Service

**User Story:** As a developer, I want the Delete button to delete listings through the service, so that the form never directly removes a MarketListing entity.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormMarket SHALL check MarketListingReferenceCounter for transaction references.
2. IF the listing has references (count > 0), THEN THE FormMarket SHALL display a warning message and prevent deletion.
3. IF the listing has no references, THE FormMarket SHALL prompt for confirmation before calling MarketListingService.DeleteListing.
4. THE FormMarket SHALL NOT directly call PlayerContext.RemoveMarketListing.
5. THE FormMarket SHALL NOT directly call PlayerContext.WriteContext() for listing deletion.

### Requirement 12: FormMarket Record Sale Through Service

**User Story:** As a developer, I want the Record Sale button to record sales through the service, so that the form never directly calls the static MarketService.RecordSale or persists.

#### Acceptance Criteria

1. WHEN the user clicks Record Sale, THE FormMarket SHALL open FormRecordSale with the ReadOnlyMarketListing from the selected row Tag.
2. WHEN FormRecordSale returns DialogResult.OK, THE FormMarket SHALL call MarketListingService.RecordSale with the listing UUID and dialog values.
3. IF the service returns null (validation failed), THE FormMarket SHALL display a warning message.
4. THE FormMarket SHALL NOT directly call the static MarketService.RecordSale.
5. THE FormMarket SHALL NOT directly call PlayerContext.AddMarketTransaction or PlayerContext.WriteContext().
### Requirement 13: FormListingEdit Uses ReadOnly Input

**User Story:** As a developer, I want FormListingEdit to accept a ReadOnlyMarketListing (or null for new) instead of a mutable MarketListing, so that the dialog never directly mutates the entity.

#### Acceptance Criteria

1. THE FormListingEdit constructor SHALL accept a ReadOnlyMarketListing parameter (null for new listing) and a PlayerContext parameter.
2. WHEN editing an existing listing, THE FormListingEdit SHALL populate controls from the ReadOnlyMarketListing properties.
3. WHEN the user clicks OK, THE FormListingEdit SHALL expose the edited values as public properties (ItemName, ItemType, ItemReferenceID, StationUUID, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent) without mutating any entity.
4. THE FormListingEdit SHALL NOT hold a reference to a mutable MarketListing.
5. THE FormListingEdit SHALL NOT directly set properties on a MarketListing entity.

### Requirement 14: FormRecordSale Uses ReadOnly Input

**User Story:** As a developer, I want FormRecordSale to accept a ReadOnlyMarketListing instead of a mutable MarketListing, so that the dialog never directly mutates the entity.

#### Acceptance Criteria

1. THE FormRecordSale constructor SHALL accept a ReadOnlyMarketListing parameter.
2. THE FormRecordSale SHALL populate display fields (available quantity, condition, default price) from the ReadOnlyMarketListing properties.
3. THE FormRecordSale SHALL continue to expose SaleQuantity, SalePricePerUnit, Counterparty, and CounterpartyFaction as public properties.
4. THE FormRecordSale SHALL NOT hold a reference to a mutable MarketListing.

### Requirement 15: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the MarketListing entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE MarketListing entity SHALL only be mutated by MarketListingService methods, the existing static MarketService.RecordSale (called by MarketListingService), JSON deserialization (loading from file), and migration code.
2. THE FormMarket SHALL NOT directly set properties on a MarketListing.
3. THE FormListingEdit SHALL NOT directly set properties on a MarketListing.
4. THE FormRecordSale SHALL NOT directly set properties on a MarketListing.

## Phase 4: Verification

### Requirement 16: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 17: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 18: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates MarketListing entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct MarketListing property sets SHALL only find matches in MarketListingService, MarketService (existing static), JSON deserialization, migration code, and the MarketListing class itself.
2. AFTER migration, a grep for direct MarketListing property sets SHALL NOT find matches in FormMarket, FormListingEdit, or FormRecordSale.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: Service.CreateListing Round-Trip

FOR ALL valid MarketListingCreateRequest values, calling Service.CreateListing SHALL produce a ReadOnlyMarketListing whose fields match the request fields (ItemName, ItemType, Quantity, PricePerUnit, StationUUID, CurrentHP, MaxHP, MaxRepairPercent) and whose UUID is non-empty.

**Validates:** Requirements 4.2, 4.4, 4.9

### Property 2: Service.UpdateListing Round-Trip

FOR ALL valid existing MarketListing entities and valid MarketListingUpdateRequest values, calling Service.UpdateListing SHALL produce a ReadOnlyMarketListing whose fields match the request fields.

**Validates:** Requirements 5.3, 5.7

### Property 3: Service.DeleteListing Removes Listing

FOR ALL valid existing MarketListing entities, calling Service.DeleteListing with the listing UUID SHALL cause the listing to no longer be findable via PlayerContext.

**Validates:** Requirements 6.2

### Property 4: Service.RecordSale Decrements Quantity

FOR ALL valid existing MarketListing entities with Quantity >= saleQuantity > 0, calling Service.RecordSale SHALL produce a transaction with correct fields and the listing quantity SHALL be decremented by saleQuantity.

**Validates:** Requirements 7.3, 7.4, 7.8

### Property 5: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct MarketListing property sets SHALL only find matches in MarketListingService, MarketService, JSON deserialization, migration code, and the MarketListing class itself.

**Validates:** Requirements 15.1, 15.2, 15.3, 15.4, 18.1, 18.2

## Out of Scope

- Changing the existing static MarketService (RecordSale, RecordPurchase, ComputeProfitLoss) --- it remains as-is.
- MarketTransaction mutation control --- transactions are created by the service and never edited. No update/delete service methods for transactions.
- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern.
- Undo/redo --- future enhancement.
- Changing the MarketListingReferenceCounter logic --- it continues to work as-is.
- Summary tab computation logic --- ComputeProfitLoss continues to use the existing static MarketService.
- Pricing plan integration in the Summary tab --- remains as-is.
- RecordPurchase migration --- RecordPurchase is not called from FormMarket (it is used elsewhere). Out of scope for BL-114.