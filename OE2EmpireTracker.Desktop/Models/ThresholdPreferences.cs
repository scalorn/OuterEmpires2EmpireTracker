namespace OE2EmpireTracker.Desktop.Models;

/// <summary>
/// Stores the nine configurable threshold values for warnings and timers.
/// All time-based values are stored as seconds.
/// </summary>
public sealed class ThresholdPreferences
{
    /// <summary>Gets or sets the structure count yellow warning threshold.</summary>
    public int StructureCountYellow { get; set; } = 60;

    /// <summary>Gets or sets the structure count red warning threshold.</summary>
    public int StructureCountRed { get; set; } = 66;

    /// <summary>Gets or sets the worker request due yellow warning (seconds). Default 2 days.</summary>
    public long WorkerRequestDueYellow { get; set; } = 172800;

    /// <summary>Gets or sets the worker request due red warning (seconds). Default 1 day.</summary>
    public long WorkerRequestDueRed { get; set; } = 86400;

    /// <summary>Gets or sets the colony import staleness yellow warning (seconds). Default 5 days.</summary>
    public long ColonyImportStalenessYellow { get; set; } = 432000;

    /// <summary>Gets or sets the colony import staleness red warning (seconds). Default 6 days.</summary>
    public long ColonyImportStalenessRed { get; set; } = 518400;

    /// <summary>Gets or sets the background processing interval (seconds). Default 60.</summary>
    public long BackgroundProcessingIntervalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the admin report refresh interval (seconds). Default 60.</summary>
    public long AdminReportRefreshSeconds { get; set; } = 60;

    /// <summary>Gets or sets the countdown display refresh rate (seconds). Default 1.</summary>
    public long CountdownDisplayRefreshSeconds { get; set; } = 1;
}
