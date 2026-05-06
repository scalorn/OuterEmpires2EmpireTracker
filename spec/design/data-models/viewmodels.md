# Data Models — ViewModels

## Filter Criteria

### BlueprintFilterCriteria

`BlueprintFilterCriteria` holds the current filter state for the blueprint list view — text filter, type filter, tech level filter, and evolution range. Used by FormBlueprintV2 to persist and apply list filtering.

## ViewModels

### BlueprintViewModel

`BlueprintViewModel` is the ViewModel for FormBlueprintV2, wrapping a Blueprint model and exposing typed properties for UI binding, computed display values (ExtendedName, property summaries), and edit operations (Save, Delete, Import).

### PricingPlanViewModel

PricingPlanViewModel is the ViewModel for FormPricingPlan, serving as a disconnected edit buffer for PricingPlan entities. Copies all fields from a ReadOnlyPricingPlan into local state, tracks dirty status, and builds PricingPlanUpdateRequest/PricingPlanCreateRequest DTOs for the PricingPlanService. See .kiro/specs/bl-123-pricingplan-readonly/design.md for full design.
