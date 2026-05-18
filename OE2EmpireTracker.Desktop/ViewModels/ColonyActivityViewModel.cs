using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the colony activity DataGrid.
/// </summary>
public sealed partial class ActivityRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _colonyName = string.Empty;

    [ObservableProperty]
    private string _structureName = string.Empty;

    [ObservableProperty]
    private string _activityType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _timeRemaining = string.Empty;

    /// <summary>Gets or sets the end time for countdown calculation.</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// ViewModel for the Colony Activity document tab.
/// Shows active timers across all colonies (mining, refining, research, manufacturing).
/// Includes a DispatcherTimer for live countdown refresh and filter checkboxes.
/// </summary>
public sealed partial class ColonyActivityViewModel : DocumentViewModel
{
    private readonly DispatcherTimer _countdownTimer;

    [ObservableProperty]
    private bool _showMining = true;

    [ObservableProperty]
    private bool _showRefining = true;

    [ObservableProperty]
    private bool _showResearch = true;

    [ObservableProperty]
    private bool _showManufacturing = true;

    public ColonyActivityViewModel()
    {
        Title = "Colony Activity";

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();

        LoadData();
    }

    public ObservableCollection<ActivityRowViewModel> Activities { get; } = new ObservableCollection<ActivityRowViewModel>();

    /// <summary>All activities before filtering.</summary>
    private ObservableCollection<ActivityRowViewModel> AllActivities { get; } = new ObservableCollection<ActivityRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Activities.Clear();
        AllActivities.Clear();
        LoadData();
    }

    partial void OnShowMiningChanged(bool value) => ApplyFilters();

    partial void OnShowRefiningChanged(bool value) => ApplyFilters();

    partial void OnShowResearchChanged(bool value) => ApplyFilters();

    partial void OnShowManufacturingChanged(bool value) => ApplyFilters();

    private void ApplyFilters()
    {
        Activities.Clear();
        foreach (var activity in AllActivities)
        {
            if (ShouldShow(activity))
            {
                Activities.Add(activity);
            }
        }
    }

    private bool ShouldShow(ActivityRowViewModel activity)
    {
        return activity.ActivityType switch
        {
            "Mining" => ShowMining,
            "Refining" => ShowRefining,
            "Research" => ShowResearch,
            "Manufacturing" => ShowManufacturing,
            _ => true,
        };
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        var now = OE2EmpireTracker.Services.SystemClock.UtcNow;
        foreach (var activity in Activities)
        {
            if (activity.EndTime.HasValue)
            {
                var remaining = activity.EndTime.Value - now;
                if (remaining <= TimeSpan.Zero)
                {
                    activity.TimeRemaining = "00:00:00";
                    activity.Status = "Complete";
                }
                else
                {
                    activity.TimeRemaining = remaining.ToString(@"hh\:mm\:ss");
                }
            }
        }
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            ApplyFilters();
            return;
        }

        var colonies = dataService.GetCurrentPlayerColonies();
        foreach (var colony in colonies)
        {
            if (colony.Structures is null)
            {
                continue;
            }

            foreach (var structure in colony.Structures)
            {
                AddStructureActivity(colony, structure);
            }
        }

        if (AllActivities.Count == 0)
        {
            LoadSampleData();
        }

        ApplyFilters();
    }

    private void AddStructureActivity(Colony colony, ColonyStructure structure)
    {
        string activityType = string.Empty;
        string structureName = $"Structure #{structure.BuildingID}";
        DateTime? endTime = null;

        if (structure.ProcessCompletionTime is not null)
        {
            endTime = structure.ProcessCompletionTime.EndTime;
        }

        if (!string.IsNullOrEmpty(structure.MiningSurveyResource))
        {
            activityType = "Mining";
            structureName = $"Mining Rig #{structure.BuildingID}";
        }
        else if (!string.IsNullOrEmpty(structure.RefiningResource))
        {
            activityType = "Refining";
            structureName = $"Refinery #{structure.BuildingID}";
        }
        else if (!string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
        {
            activityType = "Research";
            structureName = $"Research Lab #{structure.BuildingID}";
        }
        else if (!string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
        {
            activityType = "Manufacturing";
            structureName = $"Manufactory #{structure.BuildingID}";
        }

        if (string.IsNullOrEmpty(activityType))
        {
            return;
        }

        var now = OE2EmpireTracker.Services.SystemClock.UtcNow;
        var remaining = endTime.HasValue ? endTime.Value - now : TimeSpan.Zero;
        string status = remaining <= TimeSpan.Zero ? "Complete" : "Active";
        string timeStr = remaining <= TimeSpan.Zero ? "00:00:00" : remaining.ToString(@"hh\:mm\:ss");

        AllActivities.Add(new ActivityRowViewModel
        {
            ColonyName = colony.ColonyName ?? string.Empty,
            StructureName = structureName,
            ActivityType = activityType,
            Status = status,
            TimeRemaining = timeStr,
            EndTime = endTime,
        });
    }

    private void LoadSampleData()
    {
        AllActivities.Add(new ActivityRowViewModel
        {
            ColonyName = "Alpha Prime",
            StructureName = "Mining Rig #1",
            ActivityType = "Mining",
            Status = "Active",
            TimeRemaining = "02:15:30",
            EndTime = OE2EmpireTracker.Services.SystemClock.UtcNow.AddHours(2).AddMinutes(15).AddSeconds(30),
        });
        AllActivities.Add(new ActivityRowViewModel
        {
            ColonyName = "Alpha Prime",
            StructureName = "Refinery #1",
            ActivityType = "Refining",
            Status = "Active",
            TimeRemaining = "01:45:00",
            EndTime = OE2EmpireTracker.Services.SystemClock.UtcNow.AddHours(1).AddMinutes(45),
        });
        AllActivities.Add(new ActivityRowViewModel
        {
            ColonyName = "Refinery Hub",
            StructureName = "Research Lab",
            ActivityType = "Research",
            Status = "Complete",
            TimeRemaining = "00:00:00",
        });
        AllActivities.Add(new ActivityRowViewModel
        {
            ColonyName = "Refinery Hub",
            StructureName = "Manufactory #2",
            ActivityType = "Manufacturing",
            Status = "Active",
            TimeRemaining = "04:30:15",
            EndTime = OE2EmpireTracker.Services.SystemClock.UtcNow.AddHours(4).AddMinutes(30).AddSeconds(15),
        });
    }
}
