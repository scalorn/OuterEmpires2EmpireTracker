# Requirements Document

## Introduction

This feature enhances the Colony Administration tab with warehouse volume visibility, corrected underutilized refining detection, and resource depletion ETA calculations. Together these give the player a quick read on warehouse capacity usage, eliminate false-positive underutilization warnings when stockpiles are sufficient, and provide forward-looking estimates of when refining blocks will exhaust their input resources. The depletion ETA is also surfaced on the Colony Activity form in inactivity mode for empire-wide visibility.

## Glossary

- **Colony_Admin_Report**: The RTF status report displayed on the Colony form's Administration tab, built by `ColonyAdminReportBuilder`.
- **Warehouse_Volume**: The total volume of all items stored in a colony's warehouse, computed as the sum of (quantity × per-unit volume) for each item.
- **Warehouse_Capacity**: The maximum storage volume available in a colony, derived from the sum of online warehouse structure capacities (from `ColonyStructureStatus.WarehouseCapacity`).
- **Underutilized_Refiner**: A refinery that is actively running but whose input resource supply (mining rate + warehouse stockpile) cannot sustain its consumption rate long-term.
- **Resource_Depletion_ETA**: The estimated time until a refining group's input resource stockpile is exhausted, based on the net consumption rate (refining consumption minus mining production) and current warehouse stock.
- **Refining_Group**: A set of active refineries in a colony that share the same input resource and purity.
- **Net_Consumption_Rate**: The hourly rate at which a resource stockpile decreases, calculated as (total refining consumption per hour) minus (total mining production per hour) for a given resource and purity.
- **Stockpile_Sustained_Refiner**: A refinery whose input resource stockpile in the warehouse is large enough to sustain its consumption for a configurable duration, even if mining alone cannot keep up.
- **Underutilization_Threshold**: A user-configurable duration (in hours) stored in Preferences that determines how long a warehouse stockpile must sustain excess refining consumption before the refiner is considered adequately supplied. Default: 24 hours.

## Requirements

### Requirement 1: Warehouse Volume Display

**User Story:** As a colony manager, I want to see the current warehouse volume utilization at a glance, so that I can quickly assess how full my warehouse is and plan accordingly.

#### Acceptance Criteria

1. THE Colony_Admin_Report SHALL display a "Warehouse" section showing utilized volume and maximum capacity in the format "{used} / {max}" using raw unformatted numbers.
2. WHEN the colony has no items in the warehouse but has warehouse capacity greater than zero, THE Colony_Admin_Report SHALL display "0 / {max}" in the Warehouse section.
3. WHEN the colony has no online warehouse structures (capacity is zero), THE Colony_Admin_Report SHALL omit the Warehouse section entirely, including any overflow predictions.
4. THE Colony_Admin_Report SHALL compute utilized volume by summing (quantity × per-unit volume) for every item in the colony's ItemBag, using the same volume constants as the overflow rule system (Resources=1, Commodities=10, Workers=50, Blueprints/Surveys=0, other items use their Volume property).
5. THE Colony_Admin_Report SHALL compute maximum capacity from the colony's calculated `ColonyStructureStatus.WarehouseCapacity` value.
6. THE Colony_Admin_Report SHALL render the Warehouse section between the Inactivity section and the Activity section in the report ordering.
7. WHEN the colony has active overflow rules that are predicted to trigger, THE Colony_Admin_Report SHALL display overflow predictions immediately after the Warehouse volume line within the same section, showing "Overflow at {datetime}" for each predicted trigger using local time format.
8. WHEN an overflow rule's threshold has already been exceeded, THE Colony_Admin_Report SHALL display "Overflow triggered -- {resource} ({purity})" or "Overflow triggered -- Total Warehouse" in the Warehouse section.

### Requirement 2: Fix Underutilized Refining Detection

**User Story:** As a colony manager, I want the underutilized refining display to account for warehouse stockpiles when determining if a refinery is truly underutilized, so that I am not shown false warnings when I have abundant resources.

#### Acceptance Criteria

1. WHEN determining if a refiner is underutilized, THE ColonyInactivityCollector SHALL consider the warehouse stockpile of the input resource as a supply buffer in addition to the mining rate.
2. WHEN the warehouse stockpile for a refining group's input resource can sustain the excess consumption (consumption minus mining output) for at least the configured Underutilization_Threshold duration, THE ColonyInactivityCollector SHALL treat the refiner as adequately supplied and exclude it from the underutilized list.
3. WHEN the warehouse stockpile cannot sustain the excess consumption for the configured Underutilization_Threshold duration, THE ColonyInactivityCollector SHALL flag the refiner as underutilized with the existing display format.
4. WHEN mining output alone meets or exceeds total refining consumption for a resource group, THE ColonyInactivityCollector SHALL not flag any refiner in that group as underutilized regardless of stockpile level.
5. THE ColonyInactivityCollector SHALL use the same stockpile lookup method (`FindResource` on the colony's ItemBag) as the existing implementation.
6. THE ColonyInactivityCollector SHALL apply the corrected logic to both the Colony Admin tab report and the Colony Activity form's inactivity mode, since both consume the same service method.

### Requirement 3: Resource Depletion ETA in Admin Report

**User Story:** As a colony manager, I want to see how long each refining group can continue operating before running out of input resources, so that I can plan resupply or adjust refining before resources are exhausted.

#### Acceptance Criteria

1. THE Colony_Admin_Report SHALL display a "Resource Depletion" section showing the estimated time until each refining group exhausts its input resource.
2. WHEN a refining group's total consumption rate exceeds the mining output rate for the same resource and purity, THE Colony_Admin_Report SHALL calculate the depletion ETA as: warehouse stockpile divided by (hourly consumption rate minus hourly mining rate), representing the combined consumption of all refineries in the group.
3. WHEN a refining group's mining output meets or exceeds its consumption rate, THE Colony_Admin_Report SHALL display "Sustained" for that resource group instead of a time estimate.
4. WHEN the warehouse stockpile for a refining group's input resource is zero AND the refining group's total consumption rate exceeds the mining output rate, THE Colony_Admin_Report SHALL display "Depleted" for that resource group.
5. THE Colony_Admin_Report SHALL format the depletion ETA using the same time formatting as other countdown displays in the report (days, hours, minutes).
6. THE Colony_Admin_Report SHALL render the Resource Depletion section after the Refining section in the report ordering.
7. WHEN the colony has no active refineries but has built refineries (online or offline), THE Colony_Admin_Report SHALL still display the Resource Depletion section if stockpile data exists for refining resources.
8. THE Colony_Admin_Report SHALL compute hourly consumption rate from the refining group's per-cycle consumption rate multiplied by the number of cycles per hour (1 cycle per hour for standard refining).

### Requirement 4: Resource Depletion ETA in Colony Activity Form

**User Story:** As an empire manager, I want to see resource depletion ETAs across all my colonies in the Colony Activity form, so that I can identify which colonies need attention without clicking through each one individually.

#### Acceptance Criteria

1. WHEN the Colony Activity form is in inactivity mode, THE ColonyInactivityCollector SHALL emit ActivityRow entries for each refining group that has a finite depletion ETA (consumption exceeds mining).
2. THE ColonyInactivityCollector SHALL set the ActivityRow Type to `ActivityType.Refining` for depletion ETA rows.
3. THE ColonyInactivityCollector SHALL set the ProcessDetails to a format showing the depletion time estimate (e.g., "Depletion: 4d 12h -- Resource (Purity)").
4. WHEN a refining group is sustained (mining meets or exceeds consumption), THE ColonyInactivityCollector SHALL not emit a depletion ETA row for that group.
5. WHEN a refining group's stockpile is depleted (zero stock AND consumption exceeds mining), THE ColonyInactivityCollector SHALL emit a row with ProcessDetails indicating "Depleted -- Resource (Purity)".
6. THE Colony Activity form SHALL display depletion ETA rows when the Refining activity type checkbox is checked in inactivity mode.

### Requirement 5: Overflow Rule Trigger Prediction

**User Story:** As an empire manager, I want to see when a colony is predicted to trigger its overflow rules based on current mining and refining rates, so that I can proactively manage logistics before warehouses overflow.

#### Acceptance Criteria

1. THE ColonyActivityCollector SHALL emit ActivityRow entries for each active overflow rule that is predicted to trigger within a configurable time horizon.
2. WHEN a SpecificResource overflow rule exists and the net accumulation rate (mining output minus refining consumption) for that resource and purity is positive, THE ColonyActivityCollector SHALL calculate the time until the stockpile reaches the trigger threshold as: (threshold - current stockpile) divided by net hourly accumulation rate.
3. WHEN a TotalWarehouse overflow rule exists and the net total warehouse volume growth rate is positive, THE ColonyActivityCollector SHALL calculate the time until total volume reaches the trigger threshold.
4. WHEN an overflow rule's threshold has already been exceeded (current stock or volume is at or above threshold), THE ColonyActivityCollector SHALL emit a row with ProcessDetails indicating "Overflow triggered -- {resource} ({purity})" or "Overflow triggered -- Total Warehouse" regardless of the configured prediction horizon.
5. WHEN an overflow rule is predicted to trigger within the configured horizon, THE ColonyActivityCollector SHALL emit a row with ProcessDetails showing the estimated time until trigger (e.g., "Overflow in 2d 5h -- {resource} ({purity})").
6. WHEN an overflow rule is not predicted to trigger within the configured horizon (net rate is zero, negative, or time exceeds horizon), THE ColonyActivityCollector SHALL not emit a row for that rule.
7. THE ColonyActivityCollector SHALL only evaluate active overflow rules (IsActive = true).
8. THE ColonyActivityCollector SHALL set the ActivityRow Type to a new `ActivityType.OverflowPrediction` value for overflow prediction rows.
9. THE Colony Activity form SHALL include an "Overflow" checkbox visible only in active mode, controlling display of overflow prediction rows.
10. THE Colony Activity form SHALL hide the "Overflow" checkbox in inactivity mode.

### Requirement 6: Overflow Prediction Horizon Preference

**User Story:** As a colony manager, I want to configure how far into the future the tool looks for overflow predictions, so that I only see warnings that are actionable within my planning window.

#### Acceptance Criteria

1. THE Preferences form SHALL include a numeric field for the overflow prediction horizon under the Thresholds section, labeled "Overflow prediction horizon hours".
2. THE Preferences form SHALL default the overflow prediction horizon to 48 hours.
3. THE Preferences form SHALL accept values between 1 and 336 (1 hour to 14 days) for the overflow prediction horizon.
4. WHEN the overflow prediction horizon preference is changed, THE ColonyInactivityCollector SHALL use the updated value on the next report refresh without requiring application restart.

### Requirement 7: Underutilization Threshold Preference

**User Story:** As a colony manager, I want to configure how long a warehouse stockpile must sustain refining before the tool considers it "adequately supplied," so that I can tune the sensitivity of underutilization warnings to my play style.

#### Acceptance Criteria

1. THE Preferences form SHALL include a numeric field for the Underutilization_Threshold under the Thresholds section, labeled "Underutilized refining stockpile hours".
2. THE Preferences form SHALL default the Underutilization_Threshold to 24 hours.
3. THE Preferences form SHALL accept values between 1 and 168 (1 hour to 7 days) for the Underutilization_Threshold.
4. WHEN the Underutilization_Threshold preference is changed, THE ColonyInactivityCollector SHALL use the updated value on the next report refresh without requiring application restart.
