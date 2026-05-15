using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
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
/// ViewModel for the Supply Chain document tab.
/// Shows supply chains with CRUD operations.
/// </summary>
public sealed partial class SupplyChainViewModel : DocumentViewModel
{
    [ObservableProperty]
    private SupplyChainRowViewModel? _selectedChain;

    [ObservableProperty]
    private string _editChainName = string.Empty;

    public SupplyChainViewModel()
    {
        Title = "Supply Chains";

        WeakReferenceMessenger.Default.Register<SupplyChainDataChangedMessage>(this, (r, m) =>
        {
            ((SupplyChainViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<SupplyChainRowViewModel> Chains { get; } = new ObservableCollection<SupplyChainRowViewModel>();

    [RelayCommand]
    private void NewChain()
    {
        var svc = App.Services?.GetService(typeof(SupplyChainService)) as SupplyChainService;
        svc?.Create(new SupplyChainCreateRequest { Name = "New Supply Chain" });
    }

    [RelayCommand]
    private void SaveChain()
    {
        if (SelectedChain is null || string.IsNullOrEmpty(SelectedChain.ChainUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(SupplyChainService)) as SupplyChainService;
        svc?.Update(SelectedChain.ChainUuid, new SupplyChainUpdateRequest { Name = EditChainName });
    }

    [RelayCommand]
    private void DeleteChain()
    {
        if (SelectedChain is null || string.IsNullOrEmpty(SelectedChain.ChainUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(SupplyChainService)) as SupplyChainService;
        svc?.Delete(SelectedChain.ChainUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Chains.Clear();
        SelectedChain = null;
        EditChainName = string.Empty;
        LoadData();
    }

    partial void OnSelectedChainChanged(SupplyChainRowViewModel? value)
    {
        EditChainName = value?.ChainName ?? string.Empty;
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
