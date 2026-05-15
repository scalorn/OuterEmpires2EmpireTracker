using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the colony DataGrid. Represents one colony in the list.
/// </summary>
public sealed partial class ColonyRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _colonyName = string.Empty;

    [ObservableProperty]
    private string _planetName = string.Empty;

    [ObservableProperty]
    private string _systemName = string.Empty;

    [ObservableProperty]
    private int _structureCount;

    [ObservableProperty]
    private string _lastImport = string.Empty;
}

/// <summary>
/// ViewModel for the Colony List document tab.
/// Shows a DataGrid of all colonies for smoke-test validation.
/// </summary>
public sealed partial class ColonyListViewModel : DocumentViewModel
{
    public ColonyListViewModel()
    {
        Title = "Colonies";
        LoadSampleData();
    }

    public ObservableCollection<ColonyRowViewModel> Colonies { get; } = new ObservableCollection<ColonyRowViewModel>();

    private void LoadSampleData()
    {
        // Sample data for smoke test — validates DataGrid rendering,
        // sorting, column resize, and selection.
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Alpha Prime",
            PlanetName = "Kepler-442b",
            SystemName = "Kepler-442",
            StructureCount = 8,
            LastImport = "2026-05-14 18:30",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Mining Outpost Gamma",
            PlanetName = "Proxima b",
            SystemName = "Proxima Centauri",
            StructureCount = 3,
            LastImport = "2026-05-13 09:15",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Refinery Hub",
            PlanetName = "TRAPPIST-1e",
            SystemName = "TRAPPIST-1",
            StructureCount = 12,
            LastImport = "2026-05-15 07:00",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Research Station Omega",
            PlanetName = "Ross 128 b",
            SystemName = "Ross 128",
            StructureCount = 5,
            LastImport = "2026-05-12 22:45",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Commodity Factory Delta",
            PlanetName = "LHS 1140 b",
            SystemName = "LHS 1140",
            StructureCount = 15,
            LastImport = "2026-05-15 11:20",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Forward Base Epsilon",
            PlanetName = "Wolf 1061 c",
            SystemName = "Wolf 1061",
            StructureCount = 6,
            LastImport = "2026-05-14 03:10",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Trade Hub Zeta",
            PlanetName = "Gliese 667 Cc",
            SystemName = "Gliese 667 C",
            StructureCount = 9,
            LastImport = "2026-05-11 16:55",
        });
        Colonies.Add(new ColonyRowViewModel
        {
            ColonyName = "Deep Space Relay",
            PlanetName = "HD 40307 g",
            SystemName = "HD 40307",
            StructureCount = 2,
            LastImport = "2026-05-10 08:30",
        });
    }
}
