# Implementation Plan: JSON Default-Value Skipping

## Overview

Create a shared `JsonSerializerSettings` object that skips default-valued fields on write, update all four serialization call sites to use it, register the new file in the csproj, and add FsCheck property tests plus unit tests to verify round-trip fidelity and file size reduction.

## Tasks

- [x] 1. Create JsonSettings static class and register in csproj
  - [x] 1.1 Create `OE2EmpireTracker/Services/JsonSettings.cs`
    - Define `JsonSettings` static class in `OE2EmpireTracker.Services` namespace
    - Expose `public static readonly JsonSerializerSettings SerializerSettings` configured with `Formatting.Indented`, `DefaultValueHandling.Ignore`, `NullValueHandling.Ignore`
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [x] 1.2 Add `JsonSettings.cs` to `OE2EmpireTracker.csproj`
    - Add `<Compile Include="Services\JsonSettings.cs" />` to the existing `<ItemGroup>` containing other Services Compile entries
    - _Requirements: 1.4_

- [x] 2. Update serialization call sites to use shared settings
  - [x] 2.1 Update `EmpireContext.writeContext()`
    - Change `JsonConvert.SerializeObject(baselineRoot, Formatting.Indented)` to `JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings)`
    - _Requirements: 2.1_
  - [x] 2.2 Update `PlayerContext.writeContext()`
    - Change `JsonConvert.SerializeObject(playerRoot, Formatting.Indented)` to `JsonConvert.SerializeObject(playerRoot, JsonSettings.SerializerSettings)`
    - _Requirements: 2.2_
  - [x] 2.3 Update `PreferencesStore.Save()`
    - Change `JsonConvert.SerializeObject(_preferences, Formatting.Indented)` to `JsonConvert.SerializeObject(_preferences, JsonSettings.SerializerSettings)`
    - _Requirements: 2.3_
  - [x] 2.4 Update `ItemBagJSONConverter.WriteJson()`
    - Change `JsonConvert.SerializeObject(entry.Value, Formatting.Indented)` to `JsonConvert.SerializeObject(entry.Value, JsonSettings.SerializerSettings)`
    - _Requirements: 4.3_

- [x] 3. Checkpoint — Verify build compiles cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Add test file and FsCheck property tests
  - [x] 4.1 Create `OE2EmpireTracker.Tests/Services/JsonDefaultSkipTests.cs` with FsCheck Arbitrary generators
    - Create the test file in `OE2EmpireTracker.Tests/Services/`
    - Implement FsCheck `Arbitrary` generators for: `PropertyBag`, `ItemBag` (with `Item`), `ColonyStructure`, `Colony`, `Blueprint`, `DeliveryPlan`/`DeliveryPlanStop`/`DeliveryItem`, `DeliveryRoute`, `PlayerProfile` (with `PlayerSkill`), `Survey`, `UIPreferences`/`WindowPosition`/`WindowState`/`FormControlState`/`ComboState`/`GridState`/`GridColumnState`, `PlayerRoot`, `BaselineRoot`
    - Register generators in a composite `Arbitrary` class for FsCheck
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - [x] 4.2 Write property test: PlayerRoot round-trip (Property 1)
    - **Property 1: PlayerRoot serialization round-trip**
    - Serialize a random `PlayerRoot` with `JsonSettings.SerializerSettings`, deserialize back, assert all non-default fields match and default fields are restored
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 5.1, 5.3, 5.4, 2.2, 3.1**
  - [x] 4.3 Write property test: BaselineRoot round-trip (Property 2)
    - **Property 2: BaselineRoot serialization round-trip**
    - Serialize a random `BaselineRoot` with `JsonSettings.SerializerSettings`, deserialize back, assert equivalence
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 5.2, 2.1, 3.2**
  - [x] 4.4 Write property test: UIPreferences round-trip (Property 3)
    - **Property 3: UIPreferences serialization round-trip**
    - Serialize a random `UIPreferences` with `JsonSettings.SerializerSettings`, deserialize back, assert equivalence
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 5.5, 2.3, 3.3**
  - [x] 4.5 Write property test: ItemBag custom converter round-trip (Property 4)
    - **Property 4: ItemBag custom converter round-trip**
    - Serialize a random `ItemBag` with `JsonSettings.SerializerSettings`, deserialize back, assert same items and field values
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 4.1, 4.3**
  - [x] 4.6 Write property test: PropertyBag custom converter round-trip (Property 5)
    - **Property 5: PropertyBag custom converter round-trip**
    - Serialize a random `PropertyBag` with `JsonSettings.SerializerSettings`, deserialize back, assert identical key-value entries
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 4.2**
  - [x] 4.7 Write property test: Compact output never larger than verbose (Property 6)
    - **Property 6: Compact serialization is never larger than verbose**
    - For a random `PlayerRoot` or `BaselineRoot`, compare JSON length with settings vs without, assert compact ≤ verbose
    - `MaxNbOfTest = 100`
    - **Validates: Requirements 6.1, 6.2**

- [x] 5. Add unit tests
  - [x] 5.1 Write unit test: Settings configuration verification
    - Assert `JsonSettings.SerializerSettings.DefaultValueHandling == DefaultValueHandling.Ignore`
    - Assert `JsonSettings.SerializerSettings.NullValueHandling == NullValueHandling.Ignore`
    - Assert `JsonSettings.SerializerSettings.Formatting == Formatting.Indented`
    - _Requirements: 1.1, 1.2, 1.3_
  - [x] 5.2 Write unit test: ItemBagJSONConverter omits defaults on nested Items
    - Create an `ItemBag` with an `Item` having default-valued fields (Quantity=0, Volume=0.0, NickName="")
    - Serialize with `JsonSettings.SerializerSettings`, assert the JSON string does not contain those default-valued keys
    - _Requirements: 4.3_
  - [x] 5.3 Write unit test: File size reduction on real test data
    - Load `TestData/PlayerData.json` and `TestData/BaselineData.json`
    - Deserialize, re-serialize with old settings (Formatting.Indented only) and new settings (JsonSettings.SerializerSettings)
    - Assert new output length ≤ old output length for both files
    - _Requirements: 6.1, 6.2_
  - [x] 5.4 Write unit test: Backward compatibility with verbose JSON
    - Deserialize a JSON string containing explicit default values (e.g. `"Quantity": 0, "NickName": ""`)
    - Deserialize the same object serialized with defaults omitted
    - Assert both produce equivalent objects
    - _Requirements: 3.4_

- [x] 6. Register test file in test csproj
  - Add `<Compile Include="Services\JsonDefaultSkipTests.cs" />` to `OE2EmpireTracker.Tests.csproj`
  - _Requirements: all_

- [x] 7. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Deserialization call sites are intentionally NOT modified (Requirement 3)
- `PropertyBagJSONConverter` and `LockTrackingJsonConverter` are unaffected — they write via `JsonWriter` directly
