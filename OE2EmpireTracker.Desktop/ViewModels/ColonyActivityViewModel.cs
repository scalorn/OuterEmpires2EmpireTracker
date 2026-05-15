using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

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
}

/// <summary>
/// ViewModel for the Colony Activity document tab.
/// Shows active timers across all colonies (mining, refining, research, manufacturing).
/// </summary>
public sealed partial class ColonyActivityViewModel : DocumentViewModel
{
    public ColonyActivityViewModel()
    {
        Title = "Colony Activity";
        LoadSampleData();
    }

    public ObservableCollection<ActivityRowViewModel> Activities { get; } = new ObservableCollection<ActivityRowViewModel>();

    private void LoadSampleData()
    {
        Activities.Add(new ActivityRowViewModel
        {
            ColonyName = "Alpha Prime",
            StructureName = "Mining Rig #1",
            ActivityType = "Mining",
            Status = "Active",
            TimeRemaining = "02:15:30",
        });
        Activities.Add(new ActivityRowViewModel
        {
            ColonyName = "Alpha Prime",
            StructureName = "Refinery #1",
            ActivityType = "Refining",
            Status = "Active",
            TimeRemaining = "01:45:00",
        });
        Activities.Add(new ActivityRowViewModel
        {
            ColonyName = "Refinery Hub",
            StructureName = "Research Lab",
            ActivityType = "Research",
            Status = "Complete",
            TimeRemaining = "00:00:00",
        });
        Activities.Add(new ActivityRowViewModel
        {
            ColonyName = "Refinery Hub",
            StructureName = "Manufactory #2",
            ActivityType = "Manufacturing",
            Status = "Active",
            TimeRemaining = "04:30:15",
        });
    }
}
