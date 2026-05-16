using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the supply chain DataGrid.
/// </summary>
public sealed partial class SupplyChainRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _chainUuid = string.Empty;

    [ObservableProperty]
    private string _chainName = string.Empty;

    [ObservableProperty]
    private int _stageCount;
}

/// <summary>
/// Row item for the supply chain stage DataGrid.
/// </summary>
public sealed partial class StageRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private string _stageType = string.Empty;

    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _resourcePurity = string.Empty;

    [ObservableProperty]
    private int _accumulationThreshold;

    [ObservableProperty]
    private string _location = string.Empty;
}

/// <summary>
/// ViewModel for the Supply Chain document tab.
/// Shows supply chains with stage management.
/// </summary>
public sealed partial class SupplyChainViewModel : DocumentViewModel
{
    [ObservableProperty]
    private SupplyChainRowViewModel? _selectedChain;

    [ObservableProperty]
    private StageRowViewModel? _selectedStage;

    public SupplyChainViewModel()
    {
        Title = "Supply Chains";
        LoadData();
    }

    public ObservableCollection<SupplyChainRowViewModel> Chains { get; } = new();

    public ObservableCollection<StageRowViewModel> Stages { get; } = new();

    /// <summary>
    /// Adds a new stage to the stages grid.
    /// </summary>
    [RelayCommand]
    private void AddStage()
    {
        var nextSeq = Stages.Count > 0 ? Stages.Max(s => s.Sequence) + 1 : 1;
        Stages.Add(new StageRowViewModel
        {
            Sequence = nextSeq,
            StageType = "Mine",
            ResourceName = "New Resource",
            ResourcePurity = "Medium",
            AccumulationThreshold = 100,
            Location = string.Empty,
        });
    }

    /// <summary>
    /// Removes the selected stage from the stages grid.
    /// </summary>
    [RelayCommand]
    private void RemoveStage()
    {
        if (SelectedStage is not null)
        {
            Stages.Remove(SelectedStage);
            SelectedStage = null;
        }
    }

    /// <summary>
    /// Saves the stages back to the chain model and persists.
    /// </summary>
    [RelayCommand]
    private void SaveChain()
    {
        if (SelectedChain is null || string.IsNullOrEmpty(SelectedChain.ChainUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(SupplyChainService)) as SupplyChainService;
        if (svc is null)
        {
            return;
        }

        var stages = Stages.Select(row => new SupplyChainStage
        {
            Sequence = row.Sequence,
            StageType = Enum.TryParse<SupplyChainStageType>(row.StageType, out var t) ? t : SupplyChainStageType.Mine,
            ResourceName = row.ResourceName,
            ResourcePurity = row.ResourcePurity,
            AccumulationThreshold = row.AccumulationThreshold,
        }).ToList();

        svc.Update(SelectedChain.ChainUuid, new SupplyChainUpdateRequest
        {
            Name = SelectedChain.ChainName,
            IsActive = true,
            Stages = stages,
        });

        SelectedChain.StageCount = stages.Count;
    }

    partial void OnSelectedChainChanged(SupplyChainRowViewModel? value)
    {
        LoadStagesForChain(value);
    }

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
                ChainUuid = chain.UUID ?? string.Empty,
                ChainName = chain.Name ?? string.Empty,
                StageCount = chain.Stages?.Count ?? 0,
            });
        }

        if (Chains.Count == 0)
        {
            LoadSampleData();
        }

        if (Chains.Count > 0)
        {
            SelectedChain = Chains[0];
        }
    }

    private void LoadStagesForChain(SupplyChainRowViewModel? row)
    {
        Stages.Clear();
        SelectedStage = null;

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStages(row.ChainName);
            return;
        }

        var chain = dataService.SupplyChains
            .FirstOrDefault(c => c.UUID == row.ChainUuid);
        if (chain?.Stages is null || chain.Stages.Count == 0)
        {
            LoadSampleStages(row.ChainName);
            return;
        }

        foreach (var stage in chain.Stages)
        {
            Stages.Add(new StageRowViewModel
            {
                Sequence = stage.Sequence,
                StageType = stage.StageType.ToString(),
                ResourceName = stage.ResourceName,
                ResourcePurity = stage.ResourcePurity,
                AccumulationThreshold = stage.AccumulationThreshold,
                Location = stage.LocationUUID,
            });
        }

        if (Stages.Count == 0)
        {
            LoadSampleStages(row.ChainName);
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

        if (Chains.Count > 0)
        {
            SelectedChain = Chains[0];
        }
    }

    private void LoadSampleStages(string chainName)
    {
        if (chainName == "Iron Production")
        {
            Stages.Add(new StageRowViewModel { Sequence = 1, StageType = "Mine", ResourceName = "Iron", ResourcePurity = "High", AccumulationThreshold = 500, Location = "Colony Alpha" });
            Stages.Add(new StageRowViewModel { Sequence = 2, StageType = "PickUp", ResourceName = "Iron", ResourcePurity = "High", AccumulationThreshold = 1000, Location = "Station Beta" });
            Stages.Add(new StageRowViewModel { Sequence = 3, StageType = "Refine", ResourceName = "Iron", ResourcePurity = "Refined", AccumulationThreshold = 200, Location = "Colony Gamma" });
            Stages.Add(new StageRowViewModel { Sequence = 4, StageType = "Deliver", ResourceName = "Iron", ResourcePurity = "Refined", AccumulationThreshold = 100, Location = "Station Delta" });
        }
        else
        {
            Stages.Add(new StageRowViewModel { Sequence = 1, StageType = "Mine", ResourceName = "Silicon", ResourcePurity = "Medium", AccumulationThreshold = 300, Location = "Colony Epsilon" });
            Stages.Add(new StageRowViewModel { Sequence = 2, StageType = "Refine", ResourceName = "Silicon", ResourcePurity = "Refined", AccumulationThreshold = 150, Location = "Colony Zeta" });
            Stages.Add(new StageRowViewModel { Sequence = 3, StageType = "Mine", ResourceName = "Copper", ResourcePurity = "High", AccumulationThreshold = 200, Location = "Colony Eta" });
            Stages.Add(new StageRowViewModel { Sequence = 4, StageType = "Refine", ResourceName = "Copper", ResourcePurity = "Refined", AccumulationThreshold = 100, Location = "Colony Theta" });
            Stages.Add(new StageRowViewModel { Sequence = 5, StageType = "PickUp", ResourceName = "Electronics", ResourcePurity = string.Empty, AccumulationThreshold = 50, Location = "Station Iota" });
            Stages.Add(new StageRowViewModel { Sequence = 6, StageType = "Deliver", ResourceName = "Electronics", ResourcePurity = string.Empty, AccumulationThreshold = 25, Location = "Station Kappa" });
        }
    }
}
