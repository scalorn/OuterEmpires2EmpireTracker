# Implementation Plan: Custom Control Adoption

## Overview

Adopt FilteredTextComboSet, DataGridViewFilteredComboBoxColumn, and DataEntryGridView across all remaining forms that still use the old manual patterns. Each task converts one form (or control), including Designer.cs changes, code-behind rewiring, and mockup updates. Tasks are ordered to start with the simplest conversions and build toward the more complex ones.

## Tasks

- [x] 1. Convert ColonyStructureV2 pickers
  - [x] 1.1 Replace txtSurveyFilter + cmbSurvey with FilteredTextComboSet in ColonyStructureV2
    - In `ColonyStructureV2.Designer.cs`: change `cmbSurvey` type from `System.Windows.Forms.ComboBox` to `OE2EmpireTracker.Controls.FilteredTextComboSet`, remove `txtSurveyFilter` declaration/instantiation/Controls.Add/sizing, remove `lblSurveyFilter` if it only labels the filter
    - In `ColonyStructureV2.cs`: remove `txtSurveyFilter.TextChanged += TxtSurveyFilter_TextChanged` subscription, remove `TxtSurveyFilter_TextChanged` handler, replace `cmbSurvey.SelectedIndexChanged` with `cmbSurvey.SelectedItemChanged`, convert survey population to use `cmbSurvey.SetItems(surveyNames, currentValue)`, use `cmbSurvey.SelectedItem` or `cmbSurvey.SelectedFullIndex` to read selection
    - Preserve `_isProgrammaticUpdate` guards on all event handlers
    - _Requirements: 1, 13, 14_

  - [x] 1.2 Replace txtSelectionFilter + cmbSelection with FilteredTextComboSet in ColonyStructureV2
    - Same pattern as 1.1 but for the selection/resource picker
    - In `ColonyStructureV2.Designer.cs`: change `cmbSelection` type, remove `txtSelectionFilter` and `lblSelectionFilter`
    - In `ColonyStructureV2.cs`: remove `txtSelectionFilter.TextChanged` subscription and handler, convert population to `cmbSelection.SetItems()`, rewire selection event
    - _Requirements: 2, 13, 14_

  - [x] 1.3 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/colonies.md` to reflect the control changes (remove txtSurveyFilter, txtSelectionFilter from wireframe and control list)
    - _Requirements: 15_

- [x] 2. Convert FormSurvey
  - [x] 2.1 Replace txtFilterScannerBlueprint + cmbScannerBlueprint with FilteredTextComboSet
    - In `FormSurvey.Designer.cs`: change `cmbScannerBlueprint` type to `FilteredTextComboSet`, remove `txtFilterScannerBlueprint` declaration/instantiation/Controls.Add/sizing
    - In `FormSurvey.cs`: remove `txtFilterScannerBlueprint.TextChanged` subscription, remove `TxtFilterScannerBlueprint_TextChanged` handler, remove `UpdateScannerBlueprintList()` method (or convert it to call `SetItems`), rewire selection event
    - _Requirements: 3, 13, 14_

  - [x] 2.2 Replace Resource DataGridViewComboBoxColumn with DataGridViewFilteredComboBoxColumn
    - In `FormSurvey.Designer.cs`: change `Resource` column type from `System.Windows.Forms.DataGridViewComboBoxColumn` to `OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn`
    - In `FormSurvey.cs`: change resource list population from `Resource.DataSource` or `Resource.Items.AddRange` to setting `Resource.Items = resourceNameList`
    - Keep Purity column as standard DataGridViewComboBoxColumn (only 5 items)
    - _Requirements: 7_

  - [x] 2.3 Replace dgvResources DataGridView with DataEntryGridView
    - In `FormSurvey.Designer.cs`: change `dgvResources` type from `System.Windows.Forms.DataGridView` to `DataEntryGridView`
    - No code-behind changes needed — drop-in replacement
    - _Requirements: 9_

  - [x] 2.4 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/surveys.md` to reflect control changes
    - _Requirements: 15_

- [x] 3. Convert FormDeliveryRoute
  - [x] 3.1 Replace txtDropFilter + cmbDropItem with FilteredTextComboSet
    - In `FormDeliveryRoute.Designer.cs`: change `cmbDropItem` type to `FilteredTextComboSet`, remove `txtDropFilter` declaration/instantiation/Controls.Add/sizing
    - In `FormDeliveryRoute.cs`: remove `txtDropFilter.TextChanged` lambda subscription, convert `PopulateItemPicker` to call `cmbDropItem.SetItems(itemNames, currentValue)` instead of rebuilding DataSource, rewire selection event
    - The type-cascade combo `cmbDropItemType` stays as standard ComboBox — only the item picker changes
    - _Requirements: 4, 13, 14_

  - [x] 3.2 Replace txtPickFilter + cmbPickItem with FilteredTextComboSet
    - Same pattern as 3.1 but for the pick-up item picker
    - In `FormDeliveryRoute.Designer.cs`: change `cmbPickItem` type, remove `txtPickFilter`
    - In `FormDeliveryRoute.cs`: remove `txtPickFilter.TextChanged` lambda, convert `PopulateItemPicker` for pick-up side
    - Note: `PopulateItemPicker` is a shared method used by both drop-off and pick-up — it needs to handle both FilteredTextComboSet controls. Refactor the method signature to accept `FilteredTextComboSet` instead of `ComboBox` + `ValidatedTextBox`
    - _Requirements: 5, 13, 14_

  - [x] 3.3 Replace dgvDropOff and dgvPickUp with DataEntryGridView
    - In `FormDeliveryRoute.Designer.cs`: change both `dgvDropOff` and `dgvPickUp` types to `DataEntryGridView`
    - No code-behind changes needed
    - _Requirements: 11_

  - [x] 3.4 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/delivery-routes.md` to reflect control changes
    - _Requirements: 15_

- [ ] 4. Convert FormBlueprintV2
  - [~] 4.1 Replace txtFilterBaseBlueprint + cmbBaseBlueprint with FilteredTextComboSet
    - In `FormBlueprintV2.Designer.cs`: change `cmbBaseBlueprint` type to `FilteredTextComboSet`, remove `txtFilterBaseBlueprint` declaration/instantiation/Controls.Add/sizing
    - In `FormBlueprintV2.cs`: remove `txtFilterBaseBlueprint.TextChanged` subscription, remove `TxtFilterBaseBlueprint_TextChanged` handler, convert `UpdateBaseBlueprintList()` to use `cmbBaseBlueprint.SetItems(blueprintNames, currentValue)`, maintain a parallel list of Blueprint objects for UUID lookup via `SelectedFullIndex`
    - _Requirements: 13, 14_

  - [~] 4.2 Replace colResource DataGridViewComboBoxColumn with DataGridViewFilteredComboBoxColumn
    - In `FormBlueprintV2.Designer.cs`: change `colResource` type from `DataGridViewComboBoxColumn` to `DataGridViewFilteredComboBoxColumn`
    - In `FormBlueprintV2.cs`: change resource list population to set `colResource.Items = resourceNameList`
    - _Requirements: 8_

  - [~] 4.3 Replace dgvResources DataGridView with DataEntryGridView
    - In `FormBlueprintV2.Designer.cs`: change `dgvResources` type to `DataEntryGridView`
    - No code-behind changes needed
    - _Requirements: 12_

  - [~] 4.4 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/blueprints.md` to reflect control changes
    - _Requirements: 15_

- [ ] 5. Convert FormColonyV2 — Flatpack picker
  - [~] 5.1 Replace txtFilterFlatpack + cmbFlatpacks with FilteredTextComboSet
    - In `FormColonyV2.Designer.cs`: change `cmbFlatpacks` type to `FilteredTextComboSet`, remove `txtFilterFlatpack` declaration/instantiation/Controls.Add/sizing
    - In `FormColonyV2.cs`: remove `txtFilterFlatpack.TextChanged += TxtFilterFlatpack_TextChanged` subscription, remove `TxtFilterFlatpack_TextChanged` handler, convert `PopulateFlatpackCombo()` from BindingSource pattern to `cmbFlatpacks.SetItems(flatpackNames, currentName)`, maintain a parallel list of Blueprint objects or UUIDs for lookup via `SelectedFullIndex`
    - The current code uses `cmbFlatpacks.SelectedValue` (UUID via ValueMember) — after conversion, use `SelectedFullIndex` to index into the parallel UUID list
    - _Requirements: 6.1, 13, 14_

  - [~] 5.2 Build, test, audit, commit
    - Build solution, run tests, run audit
    - _Requirements: 15_

- [ ] 6. Convert FormColonyV2 — Item picker
  - [~] 6.1 Replace txtItemFilter + cmbItem with FilteredTextComboSet
    - In `FormColonyV2.Designer.cs`: change `cmbItem` type to `FilteredTextComboSet`, remove `txtItemFilter` declaration/instantiation/Controls.Add/sizing
    - In `FormColonyV2.cs`: remove `txtItemFilter.TextChanged += TxtItemFilter_TextChanged` subscription, remove `TxtItemFilter_TextChanged` handler, convert the six `PopulateXxxItems` methods (Resource, Commodity, Worker, Survey, Blueprint, Output) to use `cmbItem.SetItems(itemNames, currentValue)` instead of rebuilding DataSource
    - The type-cascade combo `cmbItemType` stays as standard ComboBox
    - _Requirements: 6.2, 13, 14_

  - [~] 6.2 Build, test, audit, commit
    - Build solution, run tests, run audit
    - _Requirements: 15_

- [ ] 7. Convert FormColonyV2 — Overflow pickers
  - [~] 7.1 Replace txtOverflowResourceFilter + cmbOverflowResource with FilteredTextComboSet
    - In `FormColonyV2.Designer.cs`: change `cmbOverflowResource` type to `FilteredTextComboSet`, remove `txtOverflowResourceFilter` declaration/instantiation/Controls.Add/sizing
    - In `FormColonyV2.cs`: remove filter TextChanged subscription and handler, convert overflow resource population to `cmbOverflowResource.SetItems()`
    - _Requirements: 6.3, 13, 14_

  - [~] 7.2 Replace txtOverflowDestFilter + cmbOverflowDest with FilteredTextComboSet
    - In `FormColonyV2.Designer.cs`: change `cmbOverflowDest` type to `FilteredTextComboSet`, remove `txtOverflowDestFilter`
    - In `FormColonyV2.cs`: remove filter subscription/handler, convert population to `SetItems()`
    - _Requirements: 6.4, 13, 14_

  - [~] 7.3 Replace txtOverflowRouteFilter + cmbOverflowRoute with FilteredTextComboSet
    - In `FormColonyV2.Designer.cs`: change `cmbOverflowRoute` type to `FilteredTextComboSet`, remove `txtOverflowRouteFilter`
    - In `FormColonyV2.cs`: remove filter subscription/handler, convert population to `SetItems()`
    - _Requirements: 6.5, 13, 14_

  - [~] 7.4 Build, test, audit, commit
    - Build solution, run tests, run audit
    - _Requirements: 15_

- [ ] 8. Convert FormColonyV2 — DataEntryGridView
  - [~] 8.1 Replace dgvCommodityRequests and dgvItems with DataEntryGridView
    - In `FormColonyV2.Designer.cs`: change `dgvCommodityRequests` type to `DataEntryGridView`, change `dgvItems` type to `DataEntryGridView`
    - No code-behind changes needed
    - _Requirements: 10_

  - [~] 8.2 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/colonies.md` to reflect all FormColonyV2 control changes from tasks 5-8 (FilteredTextComboSet pickers + DataEntryGridView grids)
    - _Requirements: 15_

- [ ] 9. Convert FormStockTargets
  - [~] 9.1 Replace txtEntryFilter + cmbEntry with FilteredTextComboSet
    - In `FormStockTargets.Designer.cs`: change `cmbEntry` type to `FilteredTextComboSet`, remove `txtEntryFilter` declaration/instantiation/Controls.Add/sizing
    - In `FormStockTargets.cs`: remove `txtEntryFilter.TextChanged += TxtEntryFilter_TextChanged` subscription, remove `TxtEntryFilter_TextChanged` handler, convert `PopulateEntryCombo()` to use `cmbEntry.SetItems(planNames, currentValue)`, maintain parallel list of plan UUIDs for lookup via `SelectedFullIndex`
    - _Requirements: 13, 14_

  - [~] 9.2 Replace dgvTargets with DataEntryGridView
    - In `FormStockTargets.Designer.cs`: change `dgvTargets` type to `DataEntryGridView`
    - No code-behind changes needed
    - _Requirements: 9 area (stock targets grid has editable columns)_

  - [~] 9.3 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/stock-targets.md` to reflect control changes
    - _Requirements: 15_

- [ ] 10. Convert FormPricingPlan
  - [~] 10.1 Replace dgvResourcePrices with DataEntryGridView
    - In `FormPricingPlan.Designer.cs`: change `dgvResourcePrices` type to `DataEntryGridView`
    - No code-behind changes needed
    - _Requirements: DataEntryGridView adoption for pricing plan grid_

  - [~] 10.2 Build, test, audit, update mockup, commit
    - Build solution, run tests, run audit
    - Update `spec/mockups/pricing-plans.md` to reflect DataEntryGridView type
    - _Requirements: 15_

- [ ] 11. Final verification
  - [~] 11.1 Run full audit and verify zero new findings
    - Run `node .kiro/tools/audit.js` — all checks must pass
    - Run full test suite — all tests must pass
    - Verify mockup-controls check passes for all converted forms
    - _Requirements: 15_

  - [~] 11.2 Verify no remaining Old_Filter_Pattern sites
    - Search codebase for `TextChanged.*cmb` patterns that match the old filter pattern
    - Confirm all 12 FilteredTextComboSet candidate sites from the design doc have been converted
    - Confirm all 2 DataGridViewFilteredComboBoxColumn candidate sites have been converted
    - Confirm all DataEntryGridView candidate sites have been converted
    - Any remaining sites should be documented as intentionally excluded (per the design doc's "Sites NOT Adopted" table)

## Notes

- Each task group (1-10) converts one form or control and includes a build/test/audit/commit step. This keeps each commit self-contained and reviewable.
- Tasks are ordered from simplest (ColonyStructureV2 — straightforward TextBox+ComboBox swap) to most complex (FormColonyV2 — five pickers with type cascades and BindingSource conversions).
- The FormColonyV2 conversion is split across tasks 5-8 to keep each commit focused: flatpack picker (5), item picker (6), overflow pickers (7), DataEntryGridView grids (8).
- Designer.cs files must be hand-edited — do not use the WinForms visual designer to make these changes.
- The `PopulateItemPicker` shared method in FormDeliveryRoute (task 3) needs its signature refactored since both callers will now pass FilteredTextComboSet instead of ComboBox + ValidatedTextBox.
