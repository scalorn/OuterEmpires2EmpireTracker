# Implementation Plan: JSON Deterministic Order

## Overview

Implement deterministic ordering of all entity arrays and dictionary entries at JSON serialization time, so that git diffs of `PlayerData.json` and `BaselineData.json` reflect only actual data changes. The approach creates a static `SerializationSorter` helper class, a `SortedDictionaryContractResolver`, and a one-line change to `ItemBagJSONConverter` — then wires everything into the existing `WriteContext()` methods.

## Tasks

- [x] 1. Create SerializationSorter with generic sort helpers
  - [x] 1.1 Create `OE2EmpireTracker/Services/SerializationSorter.cs` with the static class and four internal sort helpers: `SortByString<T>`, `SortByInt<T>`, `SortByStringThenInt<T>`, `SortByStringThenString<T>`
    - `SortByString` sorts by a string key selector using `StringComparer.Ordinal`, coalescing null keys to `string.Empty`
    - `SortByInt` sorts by an integer key selector in ascending numeric order
    - `SortByStringThenInt` sorts by a primary string key then secondary int key
    - `SortByStringThenString` sorts by a primary string key then secondary string key
    - All helpers return a new array; null input returns `Array.Empty<T>()`
    - _Requirements: 1.1, 1.2, 2.1–2.7, 3.1–3.2, 4.1, 5.1–5.3, 6.1, 7.1–7.3, 8.1–8.2, 9.1, 10.1, 14.1, 14.2_

  - [x] 1.2 Write property test: String key sorting produces ascending ordinal order
    - **Property 1: String key sorting produces ascending ordinal order**
    - Create `OE2EmpireTracker.Tests/Services/SerializationSorterPropertyTests.cs`
    - Generate arrays of 0–50 elements with random string keys (including null and empty)
    - Assert each element's key ≤ next element's key under `StringComparer.Ordinal`
    - Assert null/empty keys appear before non-empty keys
    - **Validates: Requirements 1.1, 2.1, 2.2, 2.4, 2.5, 2.6, 2.7, 3.1, 3.2, 5.2, 5.3, 6.1, 8.1, 8.2**

  - [x] 1.3 Write property test: Numeric key sorting produces ascending numeric order
    - **Property 2: Numeric key sorting produces ascending numeric order**
    - Generate arrays of 0–50 elements with random int keys
    - Assert each element's key ≤ next element's key numerically
    - **Validates: Requirements 2.3, 4.1, 5.1, 9.1**

  - [x] 1.4 Write property test: Composite key sorting produces ascending composite order
    - **Property 3: Composite key sorting produces ascending composite order**
    - Generate arrays of 0–50 elements with random (string, int) and (string, string) composite keys
    - Assert elements are ordered by first key ascending, then second key ascending within ties
    - **Validates: Requirements 7.1, 7.2, 7.3, 10.1**

  - [x] 1.5 Write property test: Sorting produces a new array without modifying the source
    - **Property 5: Sorting produces a new array without modifying the source**
    - Generate arrays of 0–50 elements, snapshot original order, call sort, assert original unchanged
    - Assert returned array is a different instance
    - **Validates: Requirements 13.1**

- [x] 2. Implement SortPlayerRoot and SortBaselineRoot methods
  - [x] 2.1 Add `SortPlayerRoot(PlayerRoot source)` to `SerializationSorter`
    - Create a new `PlayerRoot` with all scalar properties copied
    - Sort each of the 20 top-level arrays by UUID using `SortByString`
    - Sort nested arrays: Colony.Structures (UUID), Colony.Commodities (Name), DeliveryRoute.Stops (Sequence), DeliveryPlan.Stops (Sequence), DeliveryPlanStop.DropOff (Name), DeliveryPlanStop.PickUp (Name), BuildPlan.Items (UUID), ShipTemplate.Components (SlotType+SlotIndex), Ship.Components (SlotType+SlotIndex), Station.Components (SlotType+SlotIndex), StockPlan.Targets (UUID), StockProfile.Entries (GroupID), SupplyChain.Stages (Sequence), Asteroid.Reserves (ResourceName+Purity)
    - Return null if source is null
    - _Requirements: 1.1, 1.2, 3.1, 3.2, 4.1, 5.1, 5.2, 5.3, 6.1, 7.1, 7.2, 7.3, 8.1, 8.2, 9.1, 10.1, 13.1, 14.1, 14.2_

  - [x] 2.2 Add `SortBaselineRoot(BaselineRoot source)` to `SerializationSorter`
    - Create a new `BaselineRoot` with all scalar properties copied
    - Sort BlueprintType by Id (string), Blueprint by UUID (string), ShipClass by Id (int), TechLevel by Id (string), Commodity by ID (string), RefiningRecipe by OutputResource (string), ResearchTime by BlueprintType (string)
    - Return null if source is null
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 13.1_

  - [x] 2.3 Write unit tests for SortPlayerRoot and SortBaselineRoot
    - Create `OE2EmpireTracker.Tests/Services/SerializationSorterTests.cs`
    - Test `SortPlayerRoot_SortsAllTopLevelArraysByUUID` — build a PlayerRoot with 2–3 entities per array in reverse order, verify sorted
    - Test `SortBaselineRoot_SortsAllArraysByPrimaryKey` — build a BaselineRoot with entities in reverse order, verify sorted
    - Test `SortPlayerRoot_SortsNestedColonyArrays` — verify Colony.Structures and Colony.Commodities sorted
    - Test `SortPlayerRoot_SortsNestedDeliveryRouteStops` — verify Stops sorted by Sequence
    - Test `SortPlayerRoot_SortsNestedDeliveryPlanArrays` — verify Stops, DropOff, PickUp sorted
    - Test `SortPlayerRoot_SortsNestedShipComponents` — verify Components sorted by SlotType+SlotIndex
    - Test `SortPlayerRoot_SortsNestedAsteroidReserves` — verify Reserves sorted by ResourceName+Purity
    - Test `SortPlayerRoot_NullInput_ReturnsNull` — verify null root returns null
    - _Requirements: 1.1, 2.1–2.7, 3.1, 3.2, 4.1, 5.1–5.3, 6.1, 7.1–7.3, 8.1, 8.2, 9.1, 10.1_

- [x] 3. Checkpoint — Verify sort helpers and root sort methods
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement SortedDictionaryContractResolver and ItemBag changes
  - [x] 4.1 Create `OE2EmpireTracker/Services/SortedDictionaryContractResolver.cs`
    - Extend `DefaultContractResolver`
    - Override `CreateDictionaryContract` to wrap dictionary serialization with sorted key iteration using `StringComparer.Ordinal`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_

  - [x] 4.2 Register `SortedDictionaryContractResolver` in `JsonSettings.SerializerSettings`
    - Add `ContractResolver = new SortedDictionaryContractResolver()` to the existing settings in `OE2EmpireTracker/Services/JsonSettings.cs`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_

  - [x] 4.3 Modify `ItemBagJSONConverter.WriteJson` in `OE2EmpireTracker/Models/ItemBag.cs`
    - Change `foreach (KeyValuePair<string, Item> entry in value.Items)` to iterate `value.Items.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)`
    - _Requirements: 11.1, 11.2_

  - [x] 4.4 Write property test: Dictionary serialization produces sorted key order
    - **Property 4: Dictionary serialization produces sorted key order**
    - Add to `SerializationSorterPropertyTests.cs`
    - Generate `Dictionary<string, string>` with 0–20 random key-value pairs
    - Serialize with `SortedDictionaryContractResolver`, parse JSON, verify property names in ascending ordinal order
    - **Validates: Requirements 11.1, 11.2, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6**

  - [x] 4.5 Write unit tests for ContractResolver and ItemBag sorting
    - Add `ItemBagConverter_SerializesKeysInSortedOrder` to `SerializationSorterTests.cs` — create an ItemBag with UUIDs in reverse order, serialize, verify JSON property order
    - Add `SortedDictionaryResolver_SerializesDictionaryKeysInOrder` — create a `Dictionary<string, decimal>`, serialize with resolver, verify JSON key order
    - _Requirements: 11.1, 11.2, 12.1_

- [x] 5. Checkpoint — Verify dictionary and ItemBag sorting
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Wire sorting into WriteContext methods
  - [x] 6.1 Modify `PlayerContext.WriteContext()` in `OE2EmpireTracker/Services/PlayerContext.cs`
    - Add `playerRoot = SerializationSorter.SortPlayerRoot(playerRoot);` after the lock block builds the PlayerRoot and before `JsonConvert.SerializeObject`
    - _Requirements: 1.1, 3.1, 3.2, 4.1, 5.1–5.3, 6.1, 7.1–7.3, 8.1, 8.2, 9.1, 10.1, 13.1, 13.2_

  - [x] 6.2 Modify `EmpireContext.WriteContext()` in `OE2EmpireTracker/Services/EmpireContext.cs`
    - Add `baselineRoot = SerializationSorter.SortBaselineRoot(baselineRoot);` after building the BaselineRoot and before `JsonConvert.SerializeObject`
    - _Requirements: 2.1–2.7, 13.1, 13.3_

  - [x] 6.3 Write integration tests for end-to-end verification
    - Create `OE2EmpireTracker.Tests/Services/SerializationSorterIntegrationTests.cs`
    - Test `PlayerContext_WriteContext_ProducesSortedJson` — populate PlayerContext with entities in random order, call WriteContext, deserialize output, verify all arrays sorted
    - Test `EmpireContext_WriteContext_ProducesSortedJson` — populate EmpireContext with entities in random order, call WriteContext, deserialize output, verify all arrays sorted
    - Test `PlayerContext_WriteContext_PreservesInMemoryListOrder` — snapshot in-memory list order before WriteContext, verify unchanged after
    - Test `EmpireContext_WriteContext_PreservesInMemoryListOrder` — snapshot in-memory list order before WriteContext, verify unchanged after
    - _Requirements: 1.1, 2.1–2.7, 13.1, 13.2, 13.3_

- [-] 7. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use FsCheck 2.16.6 with FsCheck.NUnit (already in the test project)
- The design uses C# throughout — all code examples and implementations target .NET Framework 4.8.1
- Sort helpers are `internal` for testability via `[InternalsVisibleTo]`