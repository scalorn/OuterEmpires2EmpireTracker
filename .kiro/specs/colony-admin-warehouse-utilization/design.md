# Technical Design Document

## Overview

This feature enhances the Colony Administration tab with warehouse volume visibility, corrected underutilized refining detection, resource depletion ETA calculations, and overflow rule trigger predictions. It modifies existing service-layer classes, adds a shared rate calculator, extends the ActivityType enum, and adds two new preference fields. No new models or forms are created.

## Architecture

The feature follows the existing layered architecture. A new shared `ColonyResourceRateCalculator` eliminates rate calculation duplication across consumers.

```
┌─────────────────────────────────────────────────────────────┐
│ Forms Layer                                                  │
│  FormColonyActivity (new chkOverflow checkbox)               │
│  FormPreferences (two new numeric fields)                    │
│  Colony Admin Tab (consumes RTF from report builder)         │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ Services Layer                                               │
│  ColonyAdminReportBuilder (new: Warehouse section,           │
│                            Resource Depletion section)        │
│  ColonyInactivityCollector (stockpile-aware underutilization, │
│                             depletion ETA rows)              │
│  ColonyActivityCollector (overflow prediction rows)          │
│  ColonyResourceRateCalculator (NEW — shared rate logic)      │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│ Models Layer                                                 │
│  ActivityType (new: OverflowPrediction)                      │
│  ThresholdPreferences (new fields)                           │
│  ColonyStructureStatus (existing WarehouseCapacity)          │
│  WarehouseOverflowRule (existing, read-only access)          │
│  ItemBag (existing FindResource, Items enumeration)          │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### ColonyResourceRateCalculator (NEW)

**File:** `OE2EmpireTracker.Common/Services/ColonyResourceRateCalculator.cs`

Static utility class providing shared rate calculations consumed by the report builder, inactivity collector, and activity collector.

```csharp
public static class ColonyResourceRateCalculator
{
    /// <summary>
    /// Computes the net hourly accumulation rate for a specific resource+purity.
    /// Positive = stockpile growing, negative = stockpile shrinking.
    /// </summary>
    public static decimal GetNetHourlyRate(
        Colony colony, PlayerContext playerContext,
        string resource, string purity);

    /// <summary>
    /// Returns total hourly mining output for a resource+purity across all
    /// active miners in the colony (with ExtractionFocus skill bonus).
    /// </summary>
    public static decimal GetTotalMiningRate(
        Colony colony, PlayerContext playerContext,
        string resource, string purity);

    /// <summary>
    /// Returns total hourly refining consumption for a resource+purity across
    /// all active refiners in the colony.
    /// </summary>
    public static decimal GetTotalRefiningConsumption(
        Colony colony, PlayerContext playerContext,
        string resource, string purity);

    /// <summary>
    /// Returns the net hourly volume growth rate for the entire warehouse,
    /// considering all mining and refining across all resource+purity combinations.
    /// </summary>
    public static decimal GetNetWarehouseVolumeGrowthRate(
        Colony colony, PlayerContext playerContext);

    /// <summary>
    /// Computes total warehouse volume using standard volume constants.
    /// </summary>
    public static decimal ComputeWarehouseVolume(Colony colony);
}
```

**Rationale:** Mining rate and refining consumption calculations are currently duplicated in `ColonyInactivityCollector`, `ColonyAdminReportBuilder.RenderMiningAggregation`, and `ColonyAdminReportBuilder.RenderRefiningAggregation`. This feature adds two more consumers. Extracting to a shared calculator eliminates duplication.


### ColonyAdminReportBuilder (Modified)

**File:** `OE2EmpireTracker/Services/ColonyAdminReportBuilder.cs`

New methods added:

- `RenderWarehouseSection(RtfBuilder, Colony, PlayerContext, bool) → bool` — Renders warehouse volume and overflow predictions.
- `RenderResourceDepletionSection(RtfBuilder, Colony, PlayerContext, bool) → bool` — Renders depletion ETAs after the Refining section.

Report section ordering changes to:
1. Building
2. Commodity Requests
3. Inactivity
4. **Warehouse** (NEW — between Inactivity and Activity)
5. Activity (Manufacturing, Commodity Manufacturing, Research, Mining, Refining)
6. **Resource Depletion** (NEW — after Refining aggregation)

### ColonyInactivityCollector (Modified)

**File:** `OE2EmpireTracker.Common/Services/ColonyInactivityCollector.cs`

Changes:
- `CollectUnderutilizedRefiners` — Replace simple `stockpile >= consumeRate` check with time-based sustainability check using `UnderutilizedRefiningStockpileHours` preference.
- New method `CollectDepletionETAs(Colony, PlayerContext, List<ActivityRow>)` — Emits depletion ETA rows for refining groups where consumption exceeds mining.

### ColonyActivityCollector (Modified)

**File:** `OE2EmpireTracker.Common/Services/ColonyActivityCollector.cs`

New method:
- `CollectOverflowPredictions(Colony, PlayerContext, List<ActivityRow>)` — Evaluates active overflow rules and emits prediction rows within the configured horizon.

### ActivityType Enum (Modified)

**File:** `OE2EmpireTracker.Common/Services/ActivityType.cs`

```csharp
public enum ActivityType
{
    Building,
    Manufacturing,
    CommodityManufacturing,
    CommodityRequest,
    Research,
    Mining,
    Refining,
    ColonyImportStaleness,
    OverflowPrediction       // NEW
}
```

### FormColonyActivity (Modified)

**File:** `OE2EmpireTracker/Forms/ColonyActivity/FormColonyActivity.cs` + `.Designer.cs`

- Add `chkOverflow` checkbox to `flpFilters`.
- Wire `chkOverflow.CheckedChanged += ChkFilter_CheckedChanged`.
- `chkOverflow.Visible = !inactivityMode` (visible only in active mode).
- In `GetActiveTypes()`: include `ActivityType.OverflowPrediction` when `chkOverflow.Checked` and not in inactivity mode.

### FormPreferences (Modified)

**File:** `OE2EmpireTracker/Forms/Preferences/FormPreferences.cs` + `.Designer.cs`

- Add `nudOverflowHorizon` (NumericUpDown, Min=1, Max=336, Default=48) with label "Overflow prediction horizon hours".
- Add `nudUnderutilizedStockpile` (NumericUpDown, Min=1, Max=168, Default=24) with label "Underutilized refining stockpile hours".
- Both placed in the Thresholds section.

## Data Models

### ThresholdPreferences (Modified)

**File:** `OE2EmpireTracker.Common/Models/UIPreferences.cs`

New properties:

```csharp
/// <summary>
/// Hours into the future to predict overflow rule triggers. Default 48h.
/// Valid range: 1-336 (1 hour to 14 days).
/// </summary>
public int OverflowPredictionHorizonHours { get; set; } = 48;

/// <summary>
/// Hours of stockpile sustainability required before a refiner is considered
/// adequately supplied despite mining shortfall. Default 24h.
/// Valid range: 1-168 (1 hour to 7 days).
/// </summary>
public int UnderutilizedRefiningStockpileHours { get; set; } = 24;
```

Validation added to `ThresholdPreferences.Validate()`:
- `OverflowPredictionHorizonHours` must be 1-336.
- `UnderutilizedRefiningStockpileHours` must be 1-168.

### Existing Models Used (No Changes)

- `Colony.Items` (ItemBag) — warehouse contents
- `ColonyStructureStatus.WarehouseCapacity` — max capacity from online warehouse structures
- `WarehouseOverflowRule` — overflow rule definitions (IsActive, ColonyUUID, RuleType, TriggerThreshold, ResourceName, ResourcePurity)
- `OverflowRuleType` — SpecificResource or TotalWarehouse
- `ActivityRow` — existing structure for activity/inactivity rows


## Detailed Design

### Warehouse Volume Computation

Volume is computed per item using type-specific constants:

| Item Type | Volume Per Unit | Constant |
|-----------|----------------|----------|
| Resource | 1 | `GameConstants.VolumeResource` |
| Commodity | 10 | `GameConstants.VolumeCommodity` |
| WorkDetail | 50 | `GameConstants.VolumeWorkDetail` |
| Blueprint | 0 | `GameConstants.VolumeBlueprint` |
| Survey | 0 | `GameConstants.VolumeSurvey` |
| Other | `item.Volume` | Per-item property |

Formula: `totalVolume = Σ(item.Quantity × VolumeForType(item.ItemType))` for all items in `colony.Items`.

### Underutilization Detection (Corrected)

**Current logic:** A refiner is exempt from underutilization if `stockpile >= consumeRate` (one cycle's worth).

**New logic:** A refining group is exempt if the stockpile can sustain the group's excess consumption for the configured threshold duration:

```
excessConsumption = totalGroupConsumption - totalGroupMiningOutput  (hourly)
requiredStockpile = excessConsumption × UnderutilizedRefiningStockpileHours
exempt = (stockpile >= requiredStockpile)
```

The check moves from per-refiner to per-group level. Once the group is exempt, ALL refiners in that group are excluded from underutilization warnings.

### Resource Depletion ETA Calculation

For each refining group where consumption > mining:

```
netConsumptionRate = totalConsumption - totalMiningOutput  (hourly)
depletionHours = stockpile / netConsumptionRate
depletionSeconds = depletionHours × 3600
formattedETA = ActivityRow.FormatSeconds(depletionSeconds)
```

Edge cases:
- `stockpile == 0` → "Depleted"
- `mining >= consumption` → "Sustained" (no row emitted in Colony Activity form)
- Refining cycle rate: 1 cycle per hour (standard), so per-cycle consumption = hourly consumption.

### Overflow Prediction Calculation

For SpecificResource rules:
```
netRate = GetTotalMiningRate(resource, purity) - GetTotalRefiningConsumption(resource, purity)
if (currentStockpile >= threshold) → "triggered"
if (netRate <= 0) → skip (won't overflow)
hoursUntilTrigger = (threshold - currentStockpile) / netRate
if (hoursUntilTrigger <= horizonHours) → emit prediction row
```

For TotalWarehouse rules:
```
netVolumeRate = GetNetWarehouseVolumeGrowthRate(colony)
if (currentVolume >= threshold) → "triggered"
if (netVolumeRate <= 0) → skip
hoursUntilTrigger = (threshold - currentVolume) / netVolumeRate
if (hoursUntilTrigger <= horizonHours) → emit prediction row
```

### Preference Reading Pattern

Both new preferences are read fresh from `PreferencesStore.GetInstance().Preferences.Thresholds` on each collector/builder invocation. No caching, no restart required. This matches the existing pattern for `AdminRefreshIntervalSeconds` and other threshold values.

## Error Handling

- **Missing overflow rules:** If `GetCurrentPlayerOverflowRules()` returns empty, overflow prediction sections are simply omitted.
- **Zero capacity:** If `WarehouseCapacity == 0`, the Warehouse section is omitted entirely (no division by zero risk).
- **Zero net rate:** If net accumulation rate is zero or negative, overflow predictions are skipped (no division by zero).
- **Zero excess consumption:** If mining meets or exceeds consumption, depletion section shows "Sustained" (no division by zero).
- **Null colony items:** If `colony.Items` is null (shouldn't happen per model constructor), volume defaults to 0.
- **Invalid preference values:** Validation in `ThresholdPreferences.Validate()` prevents out-of-range values from being saved. If somehow loaded with invalid values (corrupt JSON), the defaults (48h, 24h) are used via property initializers.

## Correctness Properties

### Property 1: Warehouse Volume Accuracy
**Validates: Requirements 1.4, 1.5**
For any colony C with items I₁...Iₙ: `ComputeWarehouseVolume(C) == Σᵢ(Iᵢ.Quantity × VolumeForType(Iᵢ.ItemType))` using the defined volume constants table.

### Property 2: Depletion ETA Consistency
**Validates: Requirements 3.2, 3.3, 3.4**
For any refining group G with hourly consumption rate `c`, hourly mining rate `m`, and stockpile `s`: if `c > m` and `s > 0`, then `depletionHours == s / (c - m)`. If `s == 0` and `c > m`, status is "Depleted". If `m >= c`, status is "Sustained".

### Property 3: Underutilization Threshold Correctness
**Validates: Requirements 2.1, 2.2, 2.3, 2.4**
A refining group with excess consumption `e = c - m` (where `c > m`) is NOT flagged as underutilized if and only if: `stockpile >= e × thresholdHours`.

### Property 4: Overflow Prediction Accuracy
**Validates: Requirements 5.2, 5.3, 5.4, 5.5, 5.6**
For a SpecificResource rule with threshold T, current stockpile S, and net accumulation rate R: if `S >= T`, status is "triggered". If `R > 0` and `S < T`, `predictedHours == (T - S) / R`. If `R <= 0`, no prediction is emitted.

### Property 5: Overflow Checkbox Visibility Invariant
**Validates: Requirements 5.9, 5.10**
At all times after a mode change: `chkOverflow.Visible == !chkShowInactive.Checked`.

## Files Modified Summary

| File | Change Type |
|------|-------------|
| `OE2EmpireTracker.Common/Services/ActivityType.cs` | Modified — add `OverflowPrediction` |
| `OE2EmpireTracker.Common/Models/UIPreferences.cs` | Modified — add 2 threshold properties + validation |
| `OE2EmpireTracker.Common/Services/ColonyInactivityCollector.cs` | Modified — stockpile-aware underutilization + depletion ETAs |
| `OE2EmpireTracker.Common/Services/ColonyActivityCollector.cs` | Modified — overflow prediction rows |
| `OE2EmpireTracker.Common/Services/ColonyResourceRateCalculator.cs` | **NEW** — shared rate calculations |
| `OE2EmpireTracker/Services/ColonyAdminReportBuilder.cs` | Modified — Warehouse + Resource Depletion sections |
| `OE2EmpireTracker/Forms/ColonyActivity/FormColonyActivity.cs` | Modified — overflow checkbox wiring |
| `OE2EmpireTracker/Forms/ColonyActivity/FormColonyActivity.Designer.cs` | Modified — overflow checkbox control |
| `OE2EmpireTracker/Forms/Preferences/FormPreferences.cs` | Modified — two new numeric fields |
| `OE2EmpireTracker/Forms/Preferences/FormPreferences.Designer.cs` | Modified — two new NumericUpDown controls |

## Testing Strategy

1. **ColonyResourceRateCalculator unit tests** — verify rate calculations with known colony setups (multiple miners, multiple refiners, mixed purities, ExtractionFocus skill bonus).
2. **Underutilization threshold tests** — verify refiners are/aren't flagged based on stockpile vs threshold × excess consumption.
3. **Depletion ETA tests** — verify correct ETA calculation, "Sustained" and "Depleted" edge cases, zero stockpile, zero excess.
4. **Overflow prediction tests** — verify trigger detection, time calculation, horizon filtering, both rule types.
5. **Warehouse volume tests** — verify volume computation with mixed item types using constants.
6. **Property-based tests** — generate random colony configurations and verify Properties 1-4 hold.
7. **Integration tests** — verify report builder produces expected RTF sections for known colony states.
8. **Preference validation tests** — verify boundary values (1, 336, 168) and out-of-range rejection.
