# Implementation Plan: BL-016 Pricing Plans

## Overview

Implement pricing plans as a per-player valuation layer. Build the PricingPlan model, ComputedPrice model, PriceCalculator static service, PlayerContext integration, and FormPricingPlan WinForms MDI child form. Follow existing patterns from DeliveryRoute/DeliveryPlan for ownership, persistence, and cascade delete.

## Tasks

- [x] 1. Create PricingPlan and ComputedPrice models
  - [x] 1.1 Create `OE2EmpireTracker/Models/PricingPlan.cs` with UUID, Name, OwnerUUID, Description, FixedCostPerItem (decimal, default 0m), HourlyCostRate (decimal, default 0m), and `Dictionary<string, decimal> ResourcePrices`
    - Follow the DeliveryRoute ownership pattern (UUID, Name, OwnerUUID)
    - Composite key format: `"{ResourceName}|{Purity}"`
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 1.1, 1.7, 2.6_
  - [x] 1.2 Create `OE2EmpireTracker/Models/ComputedPrice.cs` with `decimal Price` and `bool IsComplete` properties
    - Lightweight class, not persisted
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 3.1, 3.2, 6.1_

- [x] 2. Implement PriceCalculator service
  - [x] 2.1 Create `OE2EmpireTracker/Services/PriceCalculator.cs` as a static class with `MakeResourceKey`, `TryGetResourcePrice`, `DeterminePurity`, `ComputeCommodityPrice`, and `ComputeBlueprintPrice` methods
    - `MakeResourceKey(string resourceName, string purity)` → `"{resourceName}|{purity}"`
    - `DeterminePurity(string resourceName)` → "S1" if starts with "S1. ", "S2" if starts with "S2. ", else "Refined"
    - `TryGetResourcePrice(PricingPlan, string resourceName, string purity, out decimal price)` → lookup in ResourcePrices dictionary
    - `ComputeCommodityPrice(PricingPlan, Commodity)` → sum of (quantity × Refined price) for each ConstructionResources entry; set IsComplete based on coverage
    - `ComputeBlueprintPrice(PricingPlan, Blueprint, decimal manufacturingHours)` → resourceCost + FixedCostPerItem + (HourlyCostRate × manufacturingHours); purity determined per resource name
    - Handle edge cases: empty resource dictionaries, unparseable quantities (log warning, skip, mark incomplete)
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 6.1, 6.2, 6.3_
  - [x] 2.2 Write property test: Purity determination produces only Refined, S1, or S2
    - **Property 7: Purity determination produces only Refined, S1, or S2**
    - Generate random resource name strings (with and without S1./S2. prefixes), verify output is always one of three values
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 2.6**
  - [x] 2.3 Write property test: Non-negative decimal validation
    - **Property 2: Non-negative decimal validation**
    - Generate arbitrary decimals, verify acceptance matches non-negative check
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 1.8, 2.3, 2.4**
  - [x] 2.4 Write property test: Zero price is valid, absent entry is incomplete
    - **Property 6: Zero price is valid, absent entry is incomplete**
    - Generate random resource names, set some to zero, leave others absent, verify TryGetResourcePrice behavior
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 6.2, 6.3**
  - [x] 2.5 Write property test: Commodity price is sum of input quantities times Refined prices
    - **Property 3: Commodity price is sum of input quantities times Refined prices**
    - Generate random PricingPlan + Commodity with realistic resource names, verify computed price matches manual sum
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 3.1, 3.3**
  - [x] 2.6 Write property test: Completeness flag matches input coverage
    - **Property 4: Completeness flag matches input coverage**
    - Generate random plan with some resources missing, verify IsComplete matches whether all inputs have entries
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 3.2, 3.4, 4.4, 6.1**
  - [x] 2.7 Write property test: Blueprint price equals resource cost plus time costs
    - **Property 5: Blueprint price equals resource cost plus time costs**
    - Generate random PricingPlan + Blueprint + non-negative hours, verify formula: resourceCost + FixedCostPerItem + (HourlyCostRate × hours)
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.6**

- [x] 3. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Integrate PricingPlan into PlayerContext and persistence
  - [x] 4.1 Add `PricingPlan[] PricingPlan` to `PlayerRoot` class (default empty array)
    - _Requirements: 5.1, 5.2, 5.5_
  - [x] 4.2 Add `BindingList<PricingPlan> PricingPlanList` field and `InitPricingPlans(PlayerRoot)` method to `PlayerContext`
    - Sort by name, initialize BindingList, call from constructor after existing Init methods
    - _Requirements: 1.5, 5.2_
  - [x] 4.3 Add `PricingPlanList` serialization to `WriteContext()` — set `playerRoot.PricingPlan = PricingPlanList.ToArray()`
    - _Requirements: 5.1, 5.6_
  - [x] 4.4 Add PricingPlan cascade delete to `CascadeDeletePlayer()` and `CleanupOrphanedData()`
    - Follow the existing pattern for DeliveryRouteList/DeliveryPlanList
    - _Requirements: 1.4, 1.7_
  - [x] 4.5 Add `GetCurrentPlayerPricingPlans()` convenience method returning plans filtered by `_currentPlayerUUID`
    - _Requirements: 1.5, 1.7_
  - [x] 4.6 Write property test: Serialization round-trip
    - **Property 8: Serialization round-trip**
    - Generate random valid PricingPlan objects, serialize with JsonSettings.SerializerSettings, deserialize, verify equivalence of UUID, Name, OwnerUUID, Description, FixedCostPerItem, HourlyCostRate, and ResourcePrices
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 5.4**

- [x] 5. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Build FormPricingPlan WinForms MDI child form
  - [x] 6.1 Create `OE2EmpireTracker/Forms/PricingPlan/FormPricingPlan.cs` and `FormPricingPlan.Designer.cs` as an MDI child form
    - Left panel: ListBox of pricing plans for current player with Add/Edit/Delete buttons
    - Right panel: Plan details (Name TextBox, Description TextBox, FixedCostPerItem TextBox, HourlyCostRate TextBox) + DataGridView for resource prices
    - DataGridView columns: Resource Name (read-only), Purity (read-only), Price (editable decimal)
    - Pre-populate rows from `Resource.Resources` (Refined purity for natural resources, S1 for S1. prefixed, S2 for S2. prefixed)
    - Add `Compile Include` entries for both .cs and .Designer.cs to `OE2EmpireTracker.csproj`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.8, 2.1, 2.2, 2.3, 2.4, 2.5_
  - [x] 6.2 Implement plan CRUD operations in FormPricingPlan
    - Add: generate UUID, set OwnerUUID to current player, add to PricingPlanList, save via WriteContext
    - Edit: update Name/Description/FixedCostPerItem/HourlyCostRate, save via WriteContext
    - Delete: remove from PricingPlanList, save via WriteContext
    - Validate: reject empty/whitespace names, reject negative decimal values for costs and prices
    - _Requirements: 1.2, 1.3, 1.4, 1.6, 1.8, 2.3, 2.4_
  - [x] 6.3 Implement resource price editing in the DataGridView
    - Entering a value persists it to `PricingPlan.ResourcePrices` with the composite key
    - Clearing a value removes the entry from `ResourcePrices` (distinct from zero)
    - Display incomplete price indicator for computed prices missing inputs
    - _Requirements: 2.2, 2.3, 2.5, 6.4_
  - [x] 6.4 Write property test: Whitespace plan names are rejected
    - **Property 1: Whitespace plan names are rejected**
    - Generate random whitespace-only strings, verify validation rejects them
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - **Validates: Requirements 1.6**

- [x] 7. Wire FormPricingPlan into MainWindow
  - [x] 7.1 Add menu item to MainWindow to open FormPricingPlan as MDI child
    - Follow existing pattern for other MDI child forms (Blueprint, Colony, Survey, DeliveryRoute)
    - Subscribe to `CurrentPlayerChanged` event to refresh plan list when player switches
    - Add `Compile Include` entries if new Designer changes create new files
    - _Requirements: 1.2, 1.5_

- [-] 8. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Old-style csproj requires explicit `Compile Include` entries for every new .cs file
- Do NOT use `dotnet test` — use vstest.console with `/Logger:trx`
- Do NOT use `semanticRename` — manual find-and-replace only
