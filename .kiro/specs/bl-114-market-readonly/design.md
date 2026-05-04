# BL-114 Design: Market Immutable Data Model with Service Layer

## Overview

BL-114 applies the same immutable data model pattern established in BL-108 through BL-123 to the Market forms. FormMarket, FormListingEdit, and FormRecordSale stop directly mutating MarketListing entities. A new MarketListingService is the sole mutator of MarketListing entities with CRUD for listings and sale recording. The existing static MarketService (RecordSale, RecordPurchase, ComputeProfitLoss) is retained as stateless computation; the new service wraps it for persistence and event firing.

### Key Differences from BL-112 (DeliveryRoute)

1. **Grid-based form** --- FormMarket uses a DataGridView for listings, not a detail panel with text boxes. No ViewModel edit buffer needed.
2. **Modal dialog editing** --- FormListingEdit and FormRecordSale are modal dialogs that collect input and return it. The service applies mutations atomically.
3. **No dirty tracking** --- No IsDirty, no unsaved changes prompts. Each operation is atomic.
4. **Four mutation types** --- CreateListing, UpdateListing, DeleteListing, RecordSale (vs DeliveryRoute's Create, Update, Delete).
5. **Cross-entity RecordSale** --- RecordSale decrements listing quantity AND creates a MarketTransaction. Handled atomically by the service.
6. **Coexists with static MarketService** --- The new instance service delegates to the existing static MarketService.RecordSale for the actual sale logic.

### Similarities to BL-112

1. **ReadOnly wrappers in Tags** --- Grid row Tags store ReadOnlyMarketListing.
2. **Service as sole mutator** --- Form -> service -> entity.
3. **Internal FindMutable method** --- PlayerContext.FindMutableMarketListing for service-only access.
4. **Request DTOs** --- MarketListingCreateRequest and MarketListingUpdateRequest.
5. **Reference protection** --- MarketListingReferenceCounter checks before delete.

## Architecture

### Current Architecture

FormMarket directly mutates MarketListing entities in four places:
1. **CmdListingAdd_Click**: Creates a new MarketListing, calls PlayerContext.AddMarketListing, InvalidateMarketListingCache, WriteContext, OnMarketDataChanged.
2. **CmdListingEdit_Click**: Opens FormListingEdit which directly mutates the MarketListing properties in its OK handler. Parent calls InvalidateMarketListingCache, WriteContext, OnMarketDataChanged.
3. **CmdListingDelete_Click**: Calls PlayerContext.RemoveMarketListing, InvalidateMarketListingCache, WriteContext, OnMarketDataChanged.
4. **CmdRecordSale_Click**: Calls static MarketService.RecordSale (which mutates listing.Quantity), then AddMarketTransaction, WriteContext, OnMarketDataChanged.

FormListingEdit receives a mutable MarketListing reference and directly sets its properties (ItemName, ItemType, StationUUID, Quantity, PricePerUnit, CurrentHP, MaxHP) in the OK click handler.

FormRecordSale receives a mutable MarketListing for display only (available quantity, condition, default price). It does not mutate the listing directly --- the parent form handles mutation via MarketService.RecordSale.

### Target Architecture

FormMarket routes all mutations through MarketListingService. The grid stores ReadOnlyMarketListing in row Tags. FormListingEdit accepts ReadOnlyMarketListing and exposes edited values as properties. FormRecordSale accepts ReadOnlyMarketListing for display. The service is the only code that mutates MarketListing entities.
### Data Flow

The data flow follows four distinct paths: Add, Edit, Delete, and RecordSale. Each is atomic --- no intermediate state is visible to the user.

#### Add Listing Flow

1. User clicks Add
2. FormMarket calls MarketListingService.CreateListing(request) with default values
3. Service creates MarketListing entity with generated UUID, sets OwnerUUID
4. Service adds to PlayerContext, invalidates cache, persists, fires event
5. Service returns ReadOnlyMarketListing
6. FormMarket refreshes grid (event handler)

#### Edit Listing Flow

1. User selects row, clicks Edit
2. FormMarket extracts ReadOnlyMarketListing from row Tag
3. FormMarket opens FormListingEdit(readOnlyListing, playerContext)
4. FormListingEdit populates controls from ReadOnlyMarketListing
5. User edits fields, clicks OK
6. FormMarket builds MarketListingUpdateRequest from dialog properties
7. FormMarket calls MarketListingService.UpdateListing(uuid, request)
8. Service looks up mutable entity, applies fields, persists, fires event
9. Service returns ReadOnlyMarketListing
10. FormMarket refreshes grid (event handler)

#### Delete Listing Flow

1. User selects row, clicks Delete
2. FormMarket extracts ReadOnlyMarketListing from row Tag
3. FormMarket checks MarketListingReferenceCounter for transaction references
4. If references exist: show warning, abort
5. If no references: prompt confirmation
6. FormMarket calls MarketListingService.DeleteListing(uuid)
7. Service removes entity, persists, fires event
8. FormMarket refreshes grid (event handler)

#### Record Sale Flow

1. User selects row, clicks Record Sale
2. FormMarket extracts ReadOnlyMarketListing from row Tag
3. FormMarket opens FormRecordSale(readOnlyListing)
4. FormRecordSale displays available quantity, condition, default price
5. User enters sale details, clicks OK
6. FormMarket calls MarketListingService.RecordSale(uuid, qty, price, counterparty, faction, stationUUID)
7. Service looks up mutable entity, delegates to static MarketService.RecordSale
8. Service adds transaction to PlayerContext, persists, fires event
9. Service returns MarketTransaction (or null if validation failed)
10. FormMarket refreshes grid (event handler)
## Components and Interfaces

### ReadOnlyMarketListing (Existing --- No Changes)

Already complete. Exposes UUID, OwnerUUID, StationUUID, ItemType, ItemReferenceID, ItemName, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent as read-only properties. Includes Equals/GetHashCode based on UUID and ToString returning ItemName.

### ReadOnlyMarketTransaction (Existing --- No Changes)

Already complete. Exposes all fields as read-only: UUID, OwnerUUID, TransactionType, ItemType, ItemReferenceID, ItemName, Quantity, PricePerUnit, TotalPrice, Counterparty, CounterpartyFaction, StationUUID, Timestamp, Notes, ListingUUID, CurrentHP, MaxHP, MaxRepairPercent. Includes Equals/GetHashCode based on UUID.

### PlayerContext: FindMutableMarketListing (New)

A new internal method reusing the existing _marketListingCache. Follows the same pattern as FindMutableBlueprint, FindMutableSurvey, FindMutableColony, and FindMutableDeliveryRoute:

Marked internal so only the service project can access it. The existing public FindMarketListing remains for other consumers. Reuses the same _marketListingCache that FindMarketListing already builds and maintains.
### MarketListingService

Centralizes all MarketListing mutation. The forms never touch the entity directly. Only this service (plus JSON deserialization, migration code, and the existing static MarketService.RecordSale called by this service) mutates MarketListing objects.

Key differences from DeliveryRouteService:
- **Four operations**: CreateListing, UpdateListing, DeleteListing, RecordSale (vs three for DeliveryRoute).
- **RecordSale is cross-entity**: Creates a MarketTransaction in addition to modifying the listing.
- **Delegates to static MarketService**: RecordSale delegates the actual sale logic to the existing static MarketService.RecordSale.
- **No write locks**: MarketListing has no ReaderWriterLockSlim.
- **Cache invalidation**: Calls InvalidateMarketListingCache after mutations.

Constructor takes PlayerContext dependency. All methods follow the pattern: look up mutable entity -> mutate -> invalidate cache -> persist -> fire event -> return result.

#### CreateListing

Creates a new MarketListing with generated UUID, sets OwnerUUID from current player, populates all fields from request, adds to PlayerContext, invalidates cache, persists, fires event, returns ReadOnlyMarketListing.

#### UpdateListing

Looks up mutable MarketListing via FindMutableMarketListing, applies all fields from request, invalidates cache, persists, fires event, returns ReadOnlyMarketListing. Throws InvalidOperationException if UUID not found.

#### DeleteListing

Looks up mutable MarketListing via FindMutableMarketListing. If found, removes from PlayerContext via RemoveMarketListing, invalidates cache, persists, fires event. No-op if UUID is empty or not found.

#### RecordSale

Looks up mutable MarketListing via FindMutableMarketListing. Delegates to static MarketService.RecordSale to decrement quantity and create the transaction. If the static method returns a valid transaction, adds it to PlayerContext via AddMarketTransaction, invalidates caches, persists, fires event. Returns the transaction (or null if validation failed). Throws InvalidOperationException if listing UUID not found.
### FormListingEdit (Modified)

Currently receives a mutable MarketListing and mutates it directly. After migration:

- Constructor accepts ReadOnlyMarketListing (null for new listing) and PlayerContext.
- Populates controls from ReadOnlyMarketListing properties (or defaults for new).
- OK handler sets public properties instead of mutating the entity:
  - EditedItemName (string)
  - EditedItemType (ItemType.ItemTypeEnum)
  - EditedItemReferenceID (string)
  - EditedStationUUID (string)
  - EditedQuantity (int)
  - EditedPricePerUnit (decimal)
  - EditedCurrentHP (int)
  - EditedMaxHP (int)
  - EditedMaxRepairPercent (decimal)
- No mutable MarketListing reference held.

### FormRecordSale (Modified)

Currently receives a mutable MarketListing for display. After migration:

- Constructor accepts ReadOnlyMarketListing instead of MarketListing.
- Populates display fields (available quantity, condition, default price) from ReadOnlyMarketListing properties.
- No behavioral change --- already returns values as properties (SaleQuantity, SalePricePerUnit, Counterparty, CounterpartyFaction).
- No mutable MarketListing reference held.

### Delete Flow with Reference Protection

1. Check MarketListingReferenceCounter for transaction references (MarketTransaction.ListingUUID).
2. If count > 0: show warning message with reference count, prevent deletion.
3. If count == 0: prompt for confirmation.
4. On confirm: call MarketListingService.DeleteListing, grid refreshes via event.
## Data Models

### MarketListingCreateRequest (New)

A plain DTO carrying field values for creating a new listing:

- ItemName (string)
- ItemType (ItemType.ItemTypeEnum)
- ItemReferenceID (string)
- StationUUID (string)
- Quantity (int)
- PricePerUnit (decimal)
- CurrentHP (int)
- MaxHP (int)
- MaxRepairPercent (decimal)

No UUID (service assigns). No OwnerUUID (service sets from current player).

### MarketListingUpdateRequest (New)

A plain DTO carrying field values for updating an existing listing:

- ItemName (string)
- ItemType (ItemType.ItemTypeEnum)
- ItemReferenceID (string)
- StationUUID (string)
- Quantity (int)
- PricePerUnit (decimal)
- CurrentHP (int)
- MaxHP (int)
- MaxRepairPercent (decimal)

### MarketListing (Existing --- No Changes)

The existing MarketListing model is unchanged. Fields: UUID, OwnerUUID, StationUUID, ItemType, ItemReferenceID, ItemName, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent. The service is the only code that mutates it after migration (except deserialization, migration code, and the existing static MarketService.RecordSale called by the service).

### MarketTransaction (Existing --- No Changes)

The existing MarketTransaction model is unchanged. Fields: UUID, OwnerUUID, TransactionType, ItemType, ItemReferenceID, ItemName, Quantity, PricePerUnit, TotalPrice, Counterparty, CounterpartyFaction, StationUUID, Timestamp, Notes, ListingUUID, CurrentHP, MaxHP, MaxRepairPercent.

### ReadOnlyMarketListing (Existing --- No Changes)

Already complete. No gap fill needed.

### ReadOnlyMarketTransaction (Existing --- No Changes)

Already complete. No gap fill needed.
## Correctness Properties

### Property 1: Service.CreateListing Round-Trip

*For any* valid MarketListingCreateRequest values, calling Service.CreateListing SHALL produce a ReadOnlyMarketListing whose fields match the request fields (ItemName, ItemType, ItemReferenceID, StationUUID, Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent) and whose UUID is non-empty.

**Validates: Requirements 4.2, 4.4, 4.9**

### Property 2: Service.UpdateListing Round-Trip

*For any* valid existing MarketListing entity and valid MarketListingUpdateRequest values, calling Service.UpdateListing SHALL produce a ReadOnlyMarketListing whose fields match the request fields.

**Validates: Requirements 5.3, 5.7**

### Property 3: Service.DeleteListing Removes Listing

*For any* valid existing MarketListing entity, calling Service.DeleteListing with the listing UUID SHALL cause the listing to no longer be findable via PlayerContext.

**Validates: Requirements 6.2**

### Property 4: Service.RecordSale Decrements Quantity and Creates Transaction

*For any* valid existing MarketListing entity with Quantity >= saleQuantity > 0, calling Service.RecordSale SHALL produce a MarketTransaction with correct ItemName, Quantity, PricePerUnit, TotalPrice, and TransactionType=Sell, and the listing quantity SHALL be decremented by saleQuantity.

**Validates: Requirements 7.3, 7.4, 7.8**

### Property 5: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct MarketListing property sets SHALL only find matches in MarketListingService, MarketService (existing static), JSON deserialization, migration code, and the MarketListing class itself.

**Validates: Requirements 15.1, 15.2, 15.3, 15.4, 18.1, 18.2**
## Error Handling

### Validation

- **RecordSale quantity exceeds availability**: MarketService.RecordSale returns null. The service propagates null to the form. The form shows a warning MessageBox.
- **RecordSale non-positive quantity**: MarketService.RecordSale returns null. Same handling.
- **FormListingEdit invalid numeric input**: Dialog validates Quantity and PricePerUnit parsing before setting properties.

### Service Errors

- **UpdateListing with non-existent UUID**: MarketListingService.UpdateListing throws InvalidOperationException. The form catches this and shows an error dialog.
- **DeleteListing with empty/non-existent UUID**: MarketListingService.DeleteListing returns silently (no error).
- **RecordSale with non-existent listing UUID**: MarketListingService.RecordSale throws InvalidOperationException.
- **Null request**: CreateListing and UpdateListing throw ArgumentNullException for null requests.

### Reference Protection

- **Delete with transaction references**: MarketListingReferenceCounter checks MarketTransaction.ListingUUID references. If count > 0, deletion is blocked with a warning message showing the reference count.

## Testing Strategy

### Dual Testing Approach

- **Property-based tests** (FsCheck + NUnit): Verify universal properties across randomly generated MarketListing inputs. Minimum 25 iterations per property test.
- **Unit tests** (NUnit): Verify specific examples, edge cases, and error conditions.

### Property-Based Tests

**Library**: FsCheck 2.x with FsCheck.NUnit integration (already in the project).

**Generator**: A ValidMarketListingGen() generator that produces random MarketListing entities with random UUID, OwnerUUID, StationUUID, ItemType, ItemReferenceID, ItemName, Quantity (0-1000), PricePerUnit (0-10000), CurrentHP (0-100), MaxHP (0-100), MaxRepairPercent (0-100).

**Test files**:

1. OE2EmpireTracker.Tests/Services/MarketListingServicePropertyTests.cs
   - Property 1: CreateListing round-trip
   - Property 2: UpdateListing round-trip
   - Property 3: DeleteListing removes listing
   - Property 4: RecordSale decrements quantity and creates transaction

**Configuration**: [FsCheck.NUnit.Property(MaxTest = 25)] for round-trip tests, [FsCheck.NUnit.Property(MaxTest = 50)] for RecordSale (more combinations).

### Unit Tests

**Test file**: OE2EmpireTracker.Tests/Services/MarketListingServiceTests.cs

- CreateListing assigns non-empty UUID
- CreateListing sets OwnerUUID to current player UUID
- CreateListing fires MarketDataChanged event
- UpdateListing with non-existent UUID throws InvalidOperationException
- UpdateListing fires MarketDataChanged event
- DeleteListing with empty UUID returns without error
- DeleteListing with non-existent UUID returns without error
- DeleteListing fires MarketDataChanged event
- RecordSale with non-existent listing UUID throws InvalidOperationException
- RecordSale with quantity exceeding availability returns null
- RecordSale with valid inputs returns transaction with correct fields
- RecordSale fires MarketDataChanged event
- RecordSale adds transaction to PlayerContext

### Mutation Guard Test

**Test file**: OE2EmpireTracker.Tests/Services/MarketListingMutationGuardTests.cs

A static analysis test that greps the codebase for direct MarketListing property sets and asserts they only appear in:

- MarketListingService.cs (the new sole mutator)
- MarketService.cs (the existing static service, called by MarketListingService)
- MarketListing.cs (the model class itself, default values)
- PlayerContext.cs (deserialization/migration)
- Test files (test setup)

Checks for MarketListing property sets (ItemName, ItemType, Quantity, PricePerUnit, StationUUID, CurrentHP, MaxHP, etc.) outside allowed files.

This validates Property 5 and Requirements 15.1, 15.2, 15.3, 15.4, 18.1, 18.2.