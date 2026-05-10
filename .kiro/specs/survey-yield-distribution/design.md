# Design Document

## Overview

The Survey Yield Distribution feature adds a "Yield Distribution" tab to FormSurvey that renders smooth frequency-distribution curves showing how resource yields are spread across surveys. Multiple resource+purity combinations can be overlaid as separate colored lines for comparison. The feature uses the existing `System.Windows.Forms.DataVisualization.Charting.Chart` control (same as the Evolution Graph in FormBlueprintV2) and follows the static service pattern established by `EvolutionChainService`.

## Architecture

### Component Diagram

```
FormSurvey (UI Layer)
├── Existing: flpSearchList (filters + survey list)
├── Existing: flpSurveyData (survey detail editing)
└── NEW: tabDistribution (TabPage within new TabControl wrapping flpSurveyData)
    ├── Series selection controls (cmbDistResource, cmbDistPurity, btnAddSeries, btnRemoveSeries)
    ├── lstDistSeries (ListBox legend)
    ├── nudBinWidth (NumericUpDown, default=10, range 1-100)
    ├── chartDistribution (Chart control - Spline series)
    └── lblDistMessage (empty state messages)

YieldDistributionService (Service Layer - static, no UI dependency)
├── ComputeDistribution(surveys, resourceName, purity, binWidth) → YieldDistributionResult
└── GetAvailableCombos(surveys) → List of ResourcePurityCombo

Models (Data Layer)
├── YieldDistributionResult (bin midpoints + percentages)
├── DistributionPoint (single X,Y point)
└── ResourcePurityCombo (resource name + purity identifier)
```


### Data Flow

```
User changes filter / adds series
        │
        ▼
FormSurvey.GetFilteredSurveys()  ──→  List<ReadOnlySurvey>
        │
        ▼
YieldDistributionService.ComputeDistribution(
    filteredSurveys, resourceName, purity, binWidth)
        │
        ▼
YieldDistributionResult { Points[], SurveyCount }
        │
        ▼
Chart.Series[seriesName] ← Spline DataPoints
        │
        ▼
Chart renders smooth curve via built-in spline interpolation
```

## Detailed Design

### 1. Models (OE2EmpireTracker/Models/)

#### ResourcePurityCombo.cs

```csharp
namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Identifies a unique resource+purity combination for distribution analysis.
    /// </summary>
    public class ResourcePurityCombo
    {
        public string ResourceName { get; set; } = string.Empty;
        public string Purity { get; set; } = string.Empty;

        public string DisplayName => $"{Purity} {ResourceName}".Trim();

        public override bool Equals(object obj)
        {
            if (obj is ResourcePurityCombo other)
                return string.Equals(ResourceName, other.ResourceName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Purity, other.Purity, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public override int GetHashCode()
        {
            return (ResourceName?.ToLowerInvariant()?.GetHashCode() ?? 0)
                 ^ (Purity?.ToLowerInvariant()?.GetHashCode() ?? 0);
        }

        public override string ToString() => DisplayName;
    }
}
```

#### DistributionPoint.cs

```csharp
namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// A single point on a yield distribution curve.
    /// </summary>
    public class DistributionPoint
    {
        /// <summary>Bin midpoint (X-axis value).</summary>
        public decimal BinMidpoint { get; set; }

        /// <summary>Percentage of surveys in this bin (Y-axis value).</summary>
        public decimal Percentage { get; set; }
    }
}
```

#### YieldDistributionResult.cs

```csharp
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Result of computing a yield distribution for a single resource+purity combo.
    /// </summary>
    public class YieldDistributionResult
    {
        /// <summary>Ordered list of (binMidpoint, percentage) points for the curve.</summary>
        public List<DistributionPoint> Points { get; set; } = new List<DistributionPoint>();

        /// <summary>Number of surveys that contained the resource+purity combo.</summary>
        public int MatchingSurveyCount { get; set; }

        /// <summary>True when fewer than 2 surveys matched (insufficient data).</summary>
        public bool InsufficientData => MatchingSurveyCount < 2;
    }
}
```


### 2. YieldDistributionService (OE2EmpireTracker/Services/YieldDistributionService.cs)

Static service class following the EvolutionChainService pattern. No UI dependencies.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Pure-logic service for computing yield distribution data from surveys.
    /// No UI dependencies. Follows the same static pattern as EvolutionChainService.
    /// </summary>
    public static class YieldDistributionService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Computes the yield distribution for a given resource+purity combination
        /// across the provided surveys.
        /// </summary>
        /// <param name="surveys">Filtered survey list (already filtered by type/name/etc).</param>
        /// <param name="resourceName">Resource name to match.</param>
        /// <param name="purity">Purity level to match.</param>
        /// <param name="binWidth">Width of each bin (clamped to 1-100).</param>
        /// <returns>Distribution result with points and survey count.</returns>
        public static YieldDistributionResult ComputeDistribution(
            IReadOnlyList<ReadOnlySurvey> surveys,
            string resourceName,
            string purity,
            int binWidth)
        {
            // Clamp bin width
            binWidth = Math.Max(1, Math.Min(100, binWidth));

            var result = new YieldDistributionResult();

            if (surveys == null || surveys.Count == 0)
                return result;

            // Extract yield values matching the resource+purity combo
            var yields = new List<decimal>();
            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (string.Equals(res.Resource, resourceName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(res.Purity, purity, StringComparison.OrdinalIgnoreCase))
                    {
                        if (decimal.TryParse(res.Amount, out decimal amount))
                        {
                            yields.Add(amount);
                        }
                    }
                }
            }

            result.MatchingSurveyCount = yields.Count;

            if (yields.Count < 2)
                return result;

            // Determine bin range
            decimal minYield = yields.Min();
            decimal maxYield = yields.Max();

            // Align bins to bin-width boundaries
            decimal binStart = Math.Floor(minYield / binWidth) * binWidth;
            decimal binEnd = Math.Ceiling((maxYield + 1) / binWidth) * binWidth;

            // Build bins and count
            int totalYields = yields.Count;
            for (decimal edge = binStart; edge < binEnd; edge += binWidth)
            {
                decimal lower = edge;
                decimal upper = edge + binWidth;
                decimal midpoint = edge + (binWidth / 2.0m);

                int count = yields.Count(y => y >= lower && y < upper);
                decimal percentage = (count / (decimal)totalYields) * 100.0m;

                result.Points.Add(new DistributionPoint
                {
                    BinMidpoint = midpoint,
                    Percentage = percentage
                });
            }

            return result;
        }

        /// <summary>
        /// Returns all distinct resource+purity combinations found across the given surveys.
        /// Useful for populating the series selection dropdowns.
        /// </summary>
        public static List<ResourcePurityCombo> GetAvailableCombos(
            IReadOnlyList<ReadOnlySurvey> surveys)
        {
            if (surveys == null || surveys.Count == 0)
                return new List<ResourcePurityCombo>();

            var combos = new HashSet<ResourcePurityCombo>();

            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (!string.IsNullOrEmpty(res.Resource) && !string.IsNullOrEmpty(res.Purity))
                    {
                        combos.Add(new ResourcePurityCombo
                        {
                            ResourceName = res.Resource,
                            Purity = res.Purity
                        });
                    }
                }
            }

            return combos.OrderBy(c => c.ResourceName)
                         .ThenBy(c => c.Purity)
                         .ToList();
        }
    }
}
```

**Key Design Decisions:**
- Bin alignment: bins start at `floor(minYield / binWidth) * binWidth` so bin edges are always multiples of binWidth. This prevents partial bins at the start.
- Upper-bound exclusive: `y >= lower && y < upper` ensures each yield falls in exactly one bin.
- The service returns raw data; the UI layer handles rendering and color assignment.


### 3. UI Integration (FormSurvey modifications)

#### Tab Structure Change

The current `flpSurveyData` panel (right side of the form) will be wrapped in a TabControl:

- **Tab 1: "Survey Details"** — contains the existing `flpSurveyData` content (survey editing fields, resource grid, command buttons)
- **Tab 2: "Yield Distribution"** — contains the new distribution graph and controls

This is the same pattern used in FormBlueprintV2 where `tabDetailedData` wraps Statistics, Resources, Evolution Graph, and Price Evolution tabs.

#### Yield Distribution Tab Layout

```
┌─────────────────────────────────────────────────────────────┐
│ tabDistribution                                              │
├─────────────────────────────────────────────────────────────┤
│ ┌─ pnlDistControls (FlowLayoutPanel, Top) ────────────────┐ │
│ │ Resource: [cmbDistResource ▼]  Purity: [cmbDistPurity ▼]│ │
│ │ [Add Series]  [Remove Series]   Bin Width: [nudBinWidth]│ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─ lstDistSeries (ListBox, Left, Width=200) ──────────────┐ │
│ │ ● Medium Alkaline Earth Metals                          │ │
│ │ ● High Noble Gases                                      │ │
│ │ ● Low Transition Metals                                 │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─ chartDistribution (Chart, Fill) ───────────────────────┐ │
│ │                                                         │ │
│ │         /\                                              │ │
│ │        /  \      /\                                     │ │
│ │       /    \    /  \                                    │ │
│ │      /      \  /    \                                   │ │
│ │  ───/        \/      \───                               │ │
│ │                                                         │ │
│ │  X: Yield    Y: % of Surveys                           │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─ lblDistMessage (Label, centered, hidden when graph has  ┐ │
│ │   data)                                                  │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Chart Configuration

- **Chart Type**: `SeriesChartType.Spline` — provides smooth curve interpolation through bin midpoint/percentage points (same approach as evolution graph uses for line series)
- **Chart Area**: Single `ChartArea` named "DistributionArea"
  - X-axis: Title = "Yield" (or "Yield/cycle" for asteroids), Minimum/Maximum auto-scaled to data range
  - Y-axis: Title = "% of Surveys", Minimum = 0, Maximum = auto (with 10% headroom)
- **Series**: One `Series` per active ResourcePurityCombo
  - ChartType = Spline
  - BorderWidth = 2 (readable line thickness)
  - Color assigned from WongPalette (shared from FormBlueprintV2 or extracted to a shared constant)
- **Legend**: Built-in Chart Legend enabled, positioned at Bottom

#### Color Assignment

Reuse the existing `WongPalette` from FormBlueprintV2. To avoid duplication, the palette will be moved to a shared location (`Constants/ChartColors.cs`) and referenced by both forms.

Colors are assigned by index: first series gets color 0, second gets color 1, etc. When a series is removed and re-added, it gets the next available index (no color reuse tracking needed for the expected use case of <10 simultaneous series).


### 4. FormSurvey Integration Details

#### New Fields

```csharp
// Yield Distribution tab
private TabControl tabSurveyContent;
private TabPage tabSurveyDetails;
private TabPage tabDistribution;
private FlowLayoutPanel pnlDistControls;
private ComboBox cmbDistResource;
private ComboBox cmbDistPurity;
private Button btnAddSeries;
private Button btnRemoveSeries;
private NumericUpDown nudBinWidth;
private ListBox lstDistSeries;
private System.Windows.Forms.DataVisualization.Charting.Chart chartDistribution;
private Label lblDistMessage;

// Series tracking
private List<(ResourcePurityCombo Combo, int ColorIndex)> _activeSeries 
    = new List<(ResourcePurityCombo, int)>();
private int _nextColorIndex = 0;
```

#### Key Methods

```csharp
/// <summary>
/// Refreshes the distribution graph for all active series using current filters.
/// Called when: filters change, series added/removed, bin width changes.
/// </summary>
private void RefreshDistributionGraph()
{
    var sw = Stopwatch.StartNew();
    chartDistribution.Series.Clear();

    var filteredSurveys = GetFilteredSurveys();
    SurveyType? surveyType = GetSelectedSurveyType();

    // Check empty states
    if (!surveyType.HasValue)
    {
        ShowDistributionMessage("Select Planet or Asteroid survey type to view distributions.");
        return;
    }

    if (filteredSurveys.Count == 0)
    {
        ShowDistributionMessage("No surveys match the current filter criteria.");
        return;
    }

    if (_activeSeries.Count == 0)
    {
        ShowDistributionMessage("Add a resource + purity combination to view its yield distribution.");
        return;
    }

    HideDistributionMessage();
    int binWidth = (int)nudBinWidth.Value;

    // Update X-axis label based on survey type
    var chartArea = chartDistribution.ChartAreas[0];
    chartArea.AxisX.Title = surveyType == SurveyType.Asteroid ? "Yield/cycle" : "Yield";

    foreach (var (combo, colorIndex) in _activeSeries)
    {
        var result = YieldDistributionService.ComputeDistribution(
            filteredSurveys, combo.ResourceName, combo.Purity, binWidth);

        if (result.InsufficientData)
            continue; // Skip series with insufficient data (< 2 surveys)

        var series = new Series(combo.DisplayName)
        {
            ChartType = SeriesChartType.Spline,
            BorderWidth = 2,
            Color = ChartColors.WongPalette[colorIndex % ChartColors.WongPalette.Length]
        };

        foreach (var point in result.Points)
        {
            series.Points.AddXY((double)point.BinMidpoint, (double)point.Percentage);
        }

        chartDistribution.Series.Add(series);
    }

    sw.Stop();
    Log.Info("PERF RefreshDistributionGraph: {0}ms", sw.ElapsedMilliseconds);
}

/// <summary>
/// Populates the purity combo based on the selected resource, filtered to
/// purities that actually exist in the current filtered survey set.
/// </summary>
private void PopulateDistPurityCombo()
{
    var filteredSurveys = GetFilteredSurveys();
    var combos = YieldDistributionService.GetAvailableCombos(filteredSurveys);
    string selectedResource = cmbDistResource.SelectedItem?.ToString();

    cmbDistPurity.Items.Clear();
    var purities = combos
        .Where(c => string.Equals(c.ResourceName, selectedResource, StringComparison.OrdinalIgnoreCase))
        .Select(c => c.Purity)
        .Distinct()
        .OrderBy(p => p);

    foreach (var p in purities)
        cmbDistPurity.Items.Add(p);

    if (cmbDistPurity.Items.Count > 0)
        cmbDistPurity.SelectedIndex = 0;
}
```

#### Event Wiring

| Event | Handler | Action |
|-------|---------|--------|
| `cmbSurveyType.SelectedIndexChanged` | existing + new | Also calls `RefreshDistributionGraph()` |
| `txtSurveyFilter.TextChanged` | existing + new | Also calls `RefreshDistributionGraph()` |
| `cmbResource.SelectedIndexChanged` | existing + new | Also calls `RefreshDistributionGraph()` |
| `cmbPurityFilter.SelectedIndexChanged` | existing + new | Also calls `RefreshDistributionGraph()` |
| `txtMinAmount.TextChanged` | existing + new | Also calls `RefreshDistributionGraph()` |
| `btnAddSeries.Click` | new | Adds combo to `_activeSeries`, refreshes |
| `btnRemoveSeries.Click` | new | Removes selected from `_activeSeries`, refreshes |
| `nudBinWidth.ValueChanged` | new | Calls `RefreshDistributionGraph()` |
| `cmbDistResource.SelectedIndexChanged` | new | Calls `PopulateDistPurityCombo()` |


### 5. Shared Constants (OE2EmpireTracker/Constants/ChartColors.cs)

Extract the WongPalette to a shared location so both FormBlueprintV2 and FormSurvey can reference it without duplication:

```csharp
using System.Drawing;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Shared colorblind-friendly chart palette (extended Wong palette).
    /// Used by evolution graphs and yield distribution graphs.
    /// </summary>
    public static class ChartColors
    {
        public static readonly Color[] WongPalette = new Color[]
        {
            ColorTranslator.FromHtml("#E69F00"), // orange
            ColorTranslator.FromHtml("#56B4E9"), // sky blue
            ColorTranslator.FromHtml("#009E73"), // bluish green
            ColorTranslator.FromHtml("#B8860B"), // dark goldenrod
            ColorTranslator.FromHtml("#0072B2"), // blue
            ColorTranslator.FromHtml("#D55E00"), // vermillion
            ColorTranslator.FromHtml("#CC79A7"), // reddish purple
            ColorTranslator.FromHtml("#000000"), // black
            ColorTranslator.FromHtml("#808080"), // grey
            ColorTranslator.FromHtml("#F2CF80"), // light orange
            ColorTranslator.FromHtml("#ABD9F4"), // light sky blue
            ColorTranslator.FromHtml("#80CEB9"), // light bluish green
            ColorTranslator.FromHtml("#DAA520"), // goldenrod
            ColorTranslator.FromHtml("#80B8D8"), // light blue
            ColorTranslator.FromHtml("#EAAF80"), // light vermillion
            ColorTranslator.FromHtml("#E5BCD3"), // light reddish purple
        };
    }
}
```

Note: Black is moved to index 7 (from index 0 in the original) since distribution curves on a white background are more readable starting with orange/blue. FormBlueprintV2's `WongPalette` field will be updated to reference `ChartColors.WongPalette`.

## Correctness Properties

These properties define the formal specification that the implementation must satisfy. Each will be validated via property-based testing (FsCheck).

### Property 1: Type Filtering Correctness

**Statement:** For any set of surveys containing both Planet and Asteroid types, when ComputeDistribution is called with surveys filtered to a single SurveyType, the extracted yields come exclusively from surveys of that type.

**Test approach:** Generate mixed-type survey lists, filter to one type before calling the service, verify no yields from the excluded type appear in the result.

### Property 2: Resource+Purity Extraction Accuracy

**Statement:** For any survey set and any resource+purity combination, the number of yield values extracted equals the count of SurveyResource entries matching that exact resource name and purity (case-insensitive) with parseable Amount values.

**Test approach:** Generate surveys with known resource entries, call ComputeDistribution, verify MatchingSurveyCount equals the expected count.

### Property 3: Percentage Sum Invariant

**Statement:** For any non-empty distribution result (MatchingSurveyCount >= 2), the sum of all Percentage values across all bins equals 100% (within floating-point tolerance of ±0.01%).

**Test approach:** Generate surveys with random yields, compute distribution, assert sum of percentages ≈ 100.

### Property 4: Bin Midpoint Correctness

**Statement:** For any bin with edges [lower, upper) where width = binWidth, the midpoint equals lower + binWidth/2.

**Test approach:** For random binWidth values and yield ranges, verify each point's BinMidpoint equals the expected formula.

### Property 5: Bin Coverage Completeness

**Statement:** For any set of yields, every yield value falls within exactly one bin's [lower, upper) range, and no bin exists that contains zero yields AND is outside the [min, max] yield range.

**Test approach:** Generate random yields, compute distribution, verify every yield maps to exactly one bin.

### Property 6: Bin Width Clamping

**Statement:** For any integer input binWidth, the effective bin width used is always clamped to [1, 100]. Values < 1 become 1, values > 100 become 100.

**Test approach:** Generate arbitrary integers (including negatives, zero, large values), verify the service produces valid results with effective width in [1, 100].

### Property 7: Insufficient Data Detection

**Statement:** When fewer than 2 surveys contain the specified resource+purity combination, InsufficientData is true and Points is empty.

**Test approach:** Generate 0-1 matching surveys, verify InsufficientData flag and empty points.

### Property 8: Axis Range Coverage

**Statement:** The X-axis range (min bin midpoint to max bin midpoint) encompasses all yield values in the input data. No yield value falls outside the bin range.

**Test approach:** Generate random yields, verify min(yields) >= first bin lower edge and max(yields) < last bin upper edge.

### Property 9: GetAvailableCombos Completeness

**Statement:** For any survey set, GetAvailableCombos returns exactly the set of distinct (ResourceName, Purity) pairs found across all survey resources (case-insensitive deduplication), excluding entries with empty resource or purity.

**Test approach:** Generate surveys with known resources, verify the returned combos match the expected distinct set.


## Testing Strategy

### Property-Based Tests (FsCheck + NUnit)

Each correctness property above gets a dedicated PBT test with 100+ iterations. Tests use FsCheck generators to produce:
- Random survey lists with varying resource/purity/amount combinations
- Random bin widths (including edge cases: 0, 1, 100, 101, negative)
- Mixed SurveyType surveys

Test file: `OE2EmpireTracker.Tests/Services/YieldDistributionServiceTests.cs`

### Example-Based Tests (NUnit)

Supplement PBT with deterministic edge-case tests:
- Empty survey list → empty result
- Single survey → InsufficientData = true
- All yields identical → single bin at 100%
- Yields spanning exactly one bin width → single bin
- Yields at bin boundaries → correct bin assignment
- Non-numeric Amount values → skipped gracefully

### UI Tests

- Verify tab appears and is selectable
- Verify empty state messages display correctly for each condition
- Verify series add/remove updates the legend and chart

## Requirements Traceability

| Requirement | Design Element |
|-------------|---------------|
| Req 1: Tab Placement | TabControl wrapping flpSurveyData, tabDistribution TabPage |
| Req 2: Survey Type Segregation | GetSelectedSurveyType() check in RefreshDistributionGraph, empty state message |
| Req 3: Input Data Source | GetFilteredSurveys() reuse, filter change event wiring |
| Req 4: Series Selection | cmbDistResource, cmbDistPurity, btnAddSeries, btnRemoveSeries, lstDistSeries |
| Req 5: Distribution Calculation | YieldDistributionService.ComputeDistribution() |
| Req 6: Bin Size Configuration | nudBinWidth (NumericUpDown, range 1-100, default 10) |
| Req 7: Graph Axes | ChartArea axis titles, auto-scaling |
| Req 8: Smooth Curve Rendering | SeriesChartType.Spline, BorderWidth=2, WongPalette colors |
| Req 9: Empty State | lblDistMessage with conditional messages |
| Req 10: Asteroid Cycle Rate | AxisX.Title conditional on SurveyType |
| Req 11: Service Architecture | YieldDistributionService static class, no System.Windows.Forms dependency |

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `Models/ResourcePurityCombo.cs` | New | Resource+purity identifier model |
| `Models/DistributionPoint.cs` | New | Single graph point model |
| `Models/YieldDistributionResult.cs` | New | Distribution computation result model |
| `Services/YieldDistributionService.cs` | New | Static distribution calculation service |
| `Constants/ChartColors.cs` | New | Shared Wong palette for charts |
| `Forms/Survey/FormSurvey.cs` | Modified | Add distribution tab logic, event wiring |
| `Forms/Survey/FormSurvey.Designer.cs` | Modified | Add TabControl, distribution tab controls |
| `Forms/BlueprintV2/FormBlueprintV2.cs` | Modified | Reference ChartColors.WongPalette instead of local field |
| `Tests/Services/YieldDistributionServiceTests.cs` | New | PBT + example tests |
