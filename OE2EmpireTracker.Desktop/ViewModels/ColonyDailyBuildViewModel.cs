using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the daily build list.
/// </summary>
public sealed partial class DailyBuildRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _destination = string.Empty;
}

/// <summary>
/// ViewModel for the Colony Daily Build document tab (R2).
/// Shows what items need to be delivered today based on active delivery plans.
/// </summary>
public sealed partial class ColonyDailyBuildViewModel : DocumentViewModel
{
    public ColonyDailyBuildViewModel()
    {
        Title = "Daily Build";

        WeakReferenceMessenger.Default.Register<DeliveryDataChangedMessage>(this, (r, _) =>
        {
            ((ColonyDailyBuildViewModel)r).RefreshData();
        });

        WeakReferenceMessenger.Default.Register<ColonyDataChangedMessage>(this, (r, _) =>
        {
            ((ColonyDailyBuildViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<DailyBuildRowViewModel> Items { get; } = new ();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Items.Clear();
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

        var activePlans = dataService.DeliveryPlans
            .Where(p => !p.Completed)
            .ToList();

        if (activePlans.Count == 0)
        {
            LoadSampleData();
            return;
        }

        foreach (var plan in activePlans)
        {
            if (plan.Stops is null)
            {
                continue;
            }

            foreach (var stop in plan.Stops.OrderBy(s => s.Sequence))
            {
                string destName = ResolveDestinationName(stop.DestinationUUID, dataService);

                foreach (var item in stop.DropOff.Where(d => !d.Delivered))
                {
                    Items.Add(new DailyBuildRowViewModel
                    {
                        PlanName = plan.Name ?? string.Empty,
                        ItemName = item.ExtendedName ?? item.Name ?? string.Empty,
                        Quantity = item.Quantity,
                        Destination = destName,
                    });
                }
            }
        }

        if (Items.Count == 0)
        {
            LoadSampleData();
        }
    }

    private string ResolveDestinationName(string? uuid, DataService dataService)
    {
        if (string.IsNullOrEmpty(uuid))
        {
            return "(unknown)";
        }

        var colony = dataService.Colonies.FirstOrDefault(c => c.UUID == uuid);
        if (colony is not null)
        {
            return !string.IsNullOrEmpty(colony.ColonyName)
                ? colony.ColonyName
                : colony.PlanetName ?? uuid;
        }

        var station = dataService.Stations.FirstOrDefault(s => s.UUID == uuid);
        if (station is not null)
        {
            return station.Name ?? uuid;
        }

        return uuid;
    }

    private void LoadSampleData()
    {
        Items.Add(new DailyBuildRowViewModel
        {
            PlanName = "Weekly Supply",
            ItemName = "Iron (High)",
            Quantity = 500,
            Destination = "Colony Alpha",
        });
        Items.Add(new DailyBuildRowViewModel
        {
            PlanName = "Weekly Supply",
            ItemName = "Electronics",
            Quantity = 100,
            Destination = "Colony Beta",
        });
    }
}
