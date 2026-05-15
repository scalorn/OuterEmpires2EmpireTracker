using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

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

    [ObservableProperty]
    private string _colonyUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Colony List document tab.
/// Shows a DataGrid of all colonies with real data from DataService.
/// Subscribes to <see cref="PlayerChangedMessage"/> (via base) and
/// <see cref="ColonyDataChangedMessage"/> to auto-refresh on data changes.
/// </summary>
public sealed partial class ColonyListViewModel : DocumentViewModel
{
    public ColonyListViewModel()
    {
        Title = "Colonies";

        // Subscribe to colony-specific data changes
        WeakReferenceMessenger.Default.Register<ColonyDataChangedMessage>(this, (r, m) =>
        {
            ((ColonyListViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<ColonyRowViewModel> Colonies { get; } = new ObservableCollection<ColonyRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Colonies.Clear();
        LoadData();
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var colonies = dataService.GetCurrentPlayerColonies();
        if (colonies.Count == 0)
        {
            // Fall back to all colonies if no current player
            colonies = dataService.Colonies.ToList();
        }

        foreach (var colony in colonies)
        {
            Colonies.Add(new ColonyRowViewModel
            {
                ColonyUuid = colony.UUID ?? string.Empty,
                ColonyName = colony.ColonyName ?? string.Empty,
                PlanetName = colony.PlanetName ?? string.Empty,
                SystemName = colony.SystemName ?? string.Empty,
                StructureCount = colony.Structures?.Count ?? 0,
                LastImport = colony.LastImportDateTime ?? string.Empty,
            });
        }

        // If no real data, show sample data
        if (Colonies.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
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
    }
}
