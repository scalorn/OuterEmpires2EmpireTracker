using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the supply chain DataGrid.
/// </summary>
public sealed partial class SupplyChainRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _chainName = string.Empty;

    [ObservableProperty]
    private int _stageCount;
}

/// <summary>
/// ViewModel for the Supply Chain document tab.
/// Shows supply chains with stage counts.
/// </summary>
public sealed partial class SupplyChainViewModel : DocumentViewModel
{
    public SupplyChainViewModel()
    {
        Title = "Supply Chains";
        LoadData();
    }

    public ObservableCollection<SupplyChainRowViewModel> Chains { get; } = new ObservableCollection<SupplyChainRowViewModel>();

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var chain in dataService.SupplyChains)
        {
            Chains.Add(new SupplyChainRowViewModel
            {
                ChainName = chain.Name ?? string.Empty,
                StageCount = chain.Stages?.Count ?? 0,
            });
        }

        if (Chains.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Chains.Add(new SupplyChainRowViewModel
        {
            ChainName = "Iron Production",
            StageCount = 4,
        });
        Chains.Add(new SupplyChainRowViewModel
        {
            ChainName = "Electronics Pipeline",
            StageCount = 6,
        });
    }
}
