# Implementation Plan: Ship Template Pricing

## Overview

Add a pricing plan selector and aggregate cost display to FormShipTemplate. The implementation modifies only two existing files (FormShipTemplate.cs and FormShipTemplate.Designer.cs) to add UI controls, pricing logic, event wiring, and recalculation triggers. The pattern mirrors the existing pricing integration on FormBlueprintV2.

## Tasks

- [x] 1. Add pricing UI controls to Designer.cs
  - [x] 1.1 Add flpPricing FlowLayoutPanel, lblPricingPlan Label, cmbPricingPlan FilteredTextComboSet, and lblComputedPrice Label to FormShipTemplate.Designer.cs
    - flpPricing: FlowDirection=LeftToRight, AutoSize=true, positioned in flpDetail between flpHull and dgvSlots
    - lblPricingPlan: Text="Pricing Plan:", AutoSize=true, anchored to middle-left
    - cmbPricingPlan: FilteredTextComboSet control for plan selection
    - lblComputedPrice: AutoSize=true, displays computed price
    - Update flpDetail.Controls collection ordering to insert flpPricing after flpHull
    - _Requirements: 1.1, 1.5, 5.1, 5.2, 5.3, 5.4_

- [x] 2. Add pricing fields, methods, and event wiring to FormShipTemplate.cs
  - [x] 2.1 Add _pricingPlanUUIDs field and PopulatePricingPlanCombo() method
    - Declare `private List<string> _pricingPlanUUIDs = new List<string>()`
    - Implement PopulatePricingPlanCombo(): get plans via playerContext.GetCurrentPlayerPricingPlans(), sort via CollectionSortHelper.OrderPricingPlans(), build name list with "(none)" default, build parallel UUID list, set items on cmbPricingPlan with ProgrammaticUpdateGuard and PERF timing
    - _Requirements: 1.2, 1.4, 7.1_

  - [x] 2.2 Add UpdateTemplatePrice() method
    - Implement price computation: resolve hull blueprint, iterate filled component slots, call PriceCalculator.ComputeBlueprintPrice for each, sum prices, AND IsComplete flags
    - Parse ManufactureRunTime via EvolutionChainService.ParseTimeToSeconds / 3600 (default 0 if missing)
    - Format result as N2, append " *" if incomplete
    - Clear label when no hull, no plan, or plan not found
    - Include PERF timing
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 7.3, 7.4_

  - [x] 2.3 Add RefreshPricing(), CmbPricingPlan_SelectedItemChanged, and OnPricingDataChanged handlers
    - RefreshPricing(): calls PopulatePricingPlanCombo() + UpdateTemplatePrice()
    - CmbPricingPlan_SelectedItemChanged: calls UpdateTemplatePrice() (with programmatic update guard check)
    - OnPricingDataChanged: IsDisposed check, InvokeRequired + BeginInvoke marshal, calls RefreshPricing()
    - _Requirements: 1.3, 4.1, 4.4, 6.3, 7.2_

  - [x] 2.4 Wire event subscriptions and unsubscriptions
    - In constructor: subscribe cmbPricingPlan.SelectedItemChanged += CmbPricingPlan_SelectedItemChanged
    - In constructor: subscribe playerContext.PricingDataChanged += OnPricingDataChanged
    - In OnFormClosed: unsubscribe playerContext.PricingDataChanged -= OnPricingDataChanged
    - _Requirements: 6.1, 6.2_

- [x] 3. Wire recalculation triggers into existing handlers
  - [x] 3.1 Add UpdateTemplatePrice() call to CmbHull_SelectedItemChanged
    - Call UpdateTemplatePrice() after existing hull-change logic
    - _Requirements: 4.2_

  - [x] 3.2 Add UpdateTemplatePrice() call to DgvSlots_CellValueChanged
    - Call UpdateTemplatePrice() after existing RefreshStats() call
    - _Requirements: 4.3_

  - [x] 3.3 Add PopulatePricingPlanCombo() call to OnCurrentPlayerChanged handler
    - Price clears via ClearForm() which is already called
    - _Requirements: 4.5_

  - [x] 3.4 Update ClearForm() to clear lblComputedPrice.Text but preserve cmbPricingPlan selection
    - Add `lblComputedPrice.Text = string.Empty;` to ClearForm()
    - Do NOT reset cmbPricingPlan selection
    - _Requirements: 7.5_

  - [x] 3.5 Update SetDetailEnabled() to include cmbPricingPlan.Enabled
    - Add `cmbPricingPlan.Enabled = enabled;` to SetDetailEnabled()
    - _Requirements: 1.6_

  - [x] 3.6 Update FlpDetail_Layout() grid height calculation to account for flpPricing
    - Subtract flpPricing.Height from available grid height
    - _Requirements: 5.2_

- [x] 4. Checkpoint - Ensure build compiles and audit passes
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Write property-based tests (FsCheck + NUnit)
  - [-] 5.1 Write property test: Template price equals sum of individual blueprint prices
    - **Property 1: Template price aggregation equals sum of individual ComputeBlueprintPrice calls**
    - Generate random PricingPlan, hull blueprint, and 0-8 component blueprints with random Resources and ManufactureRunTime
    - Assert aggregate price == sum of individual PriceCalculator.ComputeBlueprintPrice results
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.5**

  - [~] 5.2 Write property test: Incomplete flag propagation
    - **Property 2: Any incomplete individual price makes aggregate incomplete**
    - Generate scenarios where some blueprints have missing resource prices
    - Assert aggregate IsComplete == AND of all individual IsComplete flags
    - **Validates: Requirements 2.6**

  - [~] 5.3 Write property test: Pricing plan dropdown ordering
    - **Property 3: Dropdown always sorted with "(none)" first and parallel UUID list aligned**
    - Generate random sets of pricing plans with random names
    - Assert first item is "(none)" with empty UUID, remaining sorted ordinal case-insensitive, UUIDs match
    - **Validates: Requirements 1.2**

  - [~] 5.4 Write property test: Price display formatting
    - **Property 4: Display matches N2 format with asterisk when incomplete**
    - Generate random decimal prices >= 0 and random IsComplete booleans
    - Assert formatted text == Price.ToString("N2") when complete, Price.ToString("N2") + " *" when incomplete
    - **Validates: Requirements 3.1, 3.2**

- [ ] 6. Write unit tests (NUnit example-based)
  - [~] 6.1 Write unit test: UpdateTemplatePrice_NoHull_ClearsLabel
    - Verify price label is empty when no hull is selected
    - **Validates: Requirements 3.3**

  - [~] 6.2 Write unit test: UpdateTemplatePrice_NoPlan_ClearsLabel
    - Verify price label is empty when "(none)" plan is selected
    - **Validates: Requirements 3.4**

  - [~] 6.3 Write unit test: UpdateTemplatePrice_HullOnly_ShowsHullPrice
    - Verify hull-only template shows just the hull blueprint price
    - **Validates: Requirements 7.4**

  - [~] 6.4 Write unit test: UpdateTemplatePrice_UnresolvableComponent_FlagsIncomplete
    - Verify unresolvable component contributes 0 and flags incomplete
    - **Validates: Requirements 7.3**

  - [~] 6.5 Write unit test: ClearForm_PreservesPricingPlanSelection
    - Verify ClearForm() does not reset cmbPricingPlan selection
    - **Validates: Requirements 7.5**

  - [~] 6.6 Write unit test: OnPricingDataChanged_PlanDeleted_RevertsToNone
    - Verify deleted plan causes revert to "(none)" on next refresh
    - **Validates: Requirements 7.2**

  - [~] 6.7 Write unit test: SetDetailEnabled_False_DisablesPricingCombo
    - Verify SetDetailEnabled(false) disables cmbPricingPlan
    - **Validates: Requirements 1.6**

- [~] 7. Final checkpoint - Ensure all tests pass and audit is clean
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Only two files are modified: FormShipTemplate.Designer.cs and FormShipTemplate.cs
- The implementation mirrors the existing pricing integration on FormBlueprintV2
- Property tests use FsCheck with NUnit (minimum 100 iterations per property)
- Unit tests validate specific edge cases and error conditions
- All pricing computation delegates to the existing static PriceCalculator.ComputeBlueprintPrice
- The parallel UUID list pattern reuses the same approach as FormBlueprintV2
