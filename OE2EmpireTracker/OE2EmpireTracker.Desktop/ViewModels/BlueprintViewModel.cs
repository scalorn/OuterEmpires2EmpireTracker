using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Parsers;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the blueprint list.
/// </summary>
public sealed partial class BlueprintRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _blueprintType = string.Empty;

    [ObservableProperty]
    private string _techLevel = string.Empty;

    [ObservableProperty]
    private int _evolution;

    [ObservableProperty]
    private int _shipClass;
}

/// <summary>
/// Row item for the blueprint statistics DataGrid.
/// </summary>
public sealed partial class BlueprintStatRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statName = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>
/// Row item for the blueprint resources DataGrid.
/// </summary>
public sealed partial class BlueprintResourceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _quantity = string.Empty;
}

/// <summary>
/// ViewModel for the Blueprint document tab.
/// Shows blueprint list with detail, statistics, resources, and evolution tabs.
/// Implements E6.2-E6.4: blueprint import with deduplication and government handling.
/// </summary>
public sealed partial class BlueprintViewModel : DocumentViewModel
{
    [ObservableProperty]
    private BlueprintRowViewModel? _selectedBlueprint;

    [ObservableProperty]
    private string _detailName = string.Empty;

    [ObservableProperty]
    private string _detailType = string.Empty;

    [ObservableProperty]
    private string _detailTechLevel = string.Empty;

    [ObservableProperty]
    private int _detailShipClass;

    [ObservableProperty]
    private int _detailEvolution;

    [ObservableProperty]
    private string _computedPriceDisplay = string.Empty;

    [ObservableProperty]
    private bool _hasEvolutionData;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    public BlueprintViewModel()
    {
        Title = "Blueprints";
        LoadData();
    }

    public ObservableCollection<BlueprintRowViewModel> Blueprints { get; } = new ();

    public ObservableCollection<BlueprintStatRowViewModel> Stats { get; } = new ();

    public ObservableCollection<BlueprintResourceRowViewModel> Resources { get; } = new ();

    /// <summary>
    /// Imports a blueprint from clipboard HTML.
    /// E6.2: Deduplicates by Name+Evolution+Type+Class+TechLevel.
    /// E6.3: Preserves NickName, CopyCost, BaseBlueprintUUID on update.
    /// E6.4: Sets OwnerUUID to empty for Government seller.
    /// </summary>
    [RelayCommand]
    private async Task ImportClipboardAsync()
    {
        var clipboardService = App.Services?.GetService(typeof(IClipboardService)) as IClipboardService;
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var blueprintService = App.Services?.GetService(typeof(BlueprintService)) as BlueprintService;
        var loggerFactory = App.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory;

        if (clipboardService is null || dataService is null || blueprintService is null || loggerFactory is null)
        {
            ImportStatus = "Services not available";
            return;
        }

        string? html = await clipboardService.GetHtmlAsync();
        if (string.IsNullOrEmpty(html))
        {
            ImportStatus = "No HTML on clipboard";
            return;
        }

        string fragment = HtmlClipboardHelper.ExtractHtmlFragment(html);
        if (string.IsNullOrEmpty(fragment))
        {
            ImportStatus = "Could not extract HTML fragment";
            return;
        }

        var scanner = new BlueprintScanner(loggerFactory.CreateLogger<BlueprintScanner>());
        var parsed = scanner.ProcessHtml(fragment);
        if (parsed is null)
        {
            ImportStatus = "Failed to parse blueprint HTML";
            return;
        }

        // E6.4: Check if seller is "Government" — set OwnerUUID to empty
        bool isGovernment = IsGovernmentSeller(fragment);
        string ownerUuid = isGovernment ? string.Empty : dataService.CurrentPlayerUUID;

        // E6.2: Check for existing blueprint by Name+Evolution+Type+Class+TechLevel
        var existing = dataService.Blueprints.FirstOrDefault(b =>
            string.Equals(b.Name, parsed.Name, StringComparison.OrdinalIgnoreCase) &&
            b.Evolution == parsed.Evolution &&
            string.Equals(b.BluePrintType, parsed.BluePrintType, StringComparison.OrdinalIgnoreCase) &&
            b.Class == parsed.Class &&
            string.Equals(b.TechLevel, parsed.TechLevel, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            // E6.3: Update existing, preserving NickName, CopyCost, BaseBlueprintUUID
            var properties = new Dictionary<string, string>();
            foreach (var key in parsed.Properties.Properties.Keys)
            {
                parsed.Properties.GetString(key, string.Empty, out string val);
                properties[key] = val;
            }

            blueprintService.Update(existing.UUID, new BlueprintUpdateRequest
            {
                Name = parsed.Name,
                NickName = existing.NickName,
                Description = parsed.Description,
                BluePrintType = parsed.BluePrintType,
                Evolution = parsed.Evolution,
                TechLevel = parsed.TechLevel,
                Class = parsed.Class,
                CopyCost = existing.CopyCost,
                BaseBlueprintUUID = existing.BaseBlueprintUUID,
                Properties = properties,
                Resources = parsed.Resources is not null
                    ? new Dictionary<string, string>(parsed.Resources)
                    : null,
            });

            ImportStatus = $"Updated existing blueprint: {parsed.Name}";
        }
        else
        {
            // Create new blueprint
            var properties = new Dictionary<string, string>();
            foreach (var key in parsed.Properties.Properties.Keys)
            {
                parsed.Properties.GetString(key, string.Empty, out string val);
                properties[key] = val;
            }

            var request = new BlueprintCreateRequest
            {
                Name = parsed.Name,
                NickName = string.Empty,
                Description = parsed.Description,
                BluePrintType = parsed.BluePrintType,
                Evolution = parsed.Evolution,
                TechLevel = parsed.TechLevel,
                Class = parsed.Class,
                CopyCost = 0,
                BaseBlueprintUUID = string.Empty,
                Properties = properties,
                Resources = parsed.Resources is not null
                    ? new Dictionary<string, string>(parsed.Resources)
                    : null,
            };

            blueprintService.Create(request);

            // E6.4: If government, clear the OwnerUUID on the newly created blueprint
            if (isGovernment)
            {
                var created = dataService.Blueprints.LastOrDefault(b =>
                    string.Equals(b.Name, parsed.Name, StringComparison.OrdinalIgnoreCase) &&
                    b.Evolution == parsed.Evolution);
                if (created is not null)
                {
                    created.OwnerUUID = string.Empty;
                    dataService.IsDirty = true;
                    dataService.WriteContext();
                }
            }

            ImportStatus = $"Imported new blueprint: {parsed.Name}";
        }

        // Refresh the list
        RefreshBlueprintList(dataService);
    }

    /// <summary>
    /// Checks if the HTML fragment indicates a Government seller.
    /// </summary>
    private static bool IsGovernmentSeller(string htmlFragment)
    {
        return htmlFragment.Contains("Government", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshBlueprintList(DataService dataService)
    {
        Blueprints.Clear();
        var blueprints = dataService.GetCurrentPlayerBlueprints();
        foreach (var bp in blueprints)
        {
            Blueprints.Add(new BlueprintRowViewModel
            {
                Name = bp.Name ?? string.Empty,
                BlueprintType = bp.BluePrintType ?? string.Empty,
                TechLevel = bp.TechLevel ?? string.Empty,
                Evolution = bp.Evolution,
                ShipClass = bp.Class,
            });
        }

        if (Blueprints.Count > 0)
        {
            SelectedBlueprint = Blueprints.LastOrDefault();
        }
    }

    partial void OnSelectedBlueprintChanged(BlueprintRowViewModel? value)
    {
        LoadBlueprintDetail(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var blueprints = dataService.GetCurrentPlayerBlueprints();
        if (blueprints.Count == 0)
        {
            LoadSampleData();
            return;
        }

        foreach (var bp in blueprints)
        {
            Blueprints.Add(new BlueprintRowViewModel
            {
                Name = bp.Name ?? string.Empty,
                BlueprintType = bp.BluePrintType ?? string.Empty,
                TechLevel = bp.TechLevel ?? string.Empty,
                Evolution = bp.Evolution,
                ShipClass = bp.Class,
            });
        }

        if (Blueprints.Count > 0)
        {
            SelectedBlueprint = Blueprints[0];
        }
    }

    private void LoadBlueprintDetail(BlueprintRowViewModel? row)
    {
        Stats.Clear();
        Resources.Clear();
        ComputedPriceDisplay = string.Empty;
        HasEvolutionData = false;

        if (row is null)
        {
            DetailName = string.Empty;
            DetailType = string.Empty;
            DetailTechLevel = string.Empty;
            DetailShipClass = 0;
            DetailEvolution = 0;
            return;
        }

        DetailName = row.Name;
        DetailType = row.BlueprintType;
        DetailTechLevel = row.TechLevel;
        DetailShipClass = row.ShipClass;
        DetailEvolution = row.Evolution;

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStats(row.Name);
            LoadSampleResources(row.Name);
            return;
        }

        var bp = dataService.GetCurrentPlayerBlueprints()
            .FirstOrDefault(b => b.Name == row.Name && b.Evolution == row.Evolution);
        if (bp is null)
        {
            LoadSampleStats(row.Name);
            LoadSampleResources(row.Name);
            HasEvolutionData = true;
            return;
        }

        foreach (var key in bp.Properties.Properties.Keys)
        {
            if (key.StartsWith("_"))
            {
                continue;
            }

            bp.Properties.GetString(key, string.Empty, out string val);
            Stats.Add(new BlueprintStatRowViewModel { StatName = key, Value = val });
        }

        foreach (var kvp in bp.Resources)
        {
            Resources.Add(new BlueprintResourceRowViewModel
            {
                ResourceName = kvp.Key,
                Quantity = kvp.Value,
            });
        }

        if (Stats.Count == 0)
        {
            LoadSampleStats(row.Name);
        }

        if (Resources.Count == 0)
        {
            LoadSampleResources(row.Name);
        }

        HasEvolutionData = Stats.Count > 0;

        UpdateComputedPrice(bp);
    }

    private void UpdateComputedPrice(Blueprint bp)
    {
        var calculator = App.Services?.GetService(typeof(PriceCalculator)) as PriceCalculator;
        if (calculator is null)
        {
            ComputedPriceDisplay = string.Empty;
            return;
        }

        var plan = calculator.GetFirstPlanForCurrentPlayer();
        if (plan is null)
        {
            ComputedPriceDisplay = string.Empty;
            return;
        }

        var result = calculator.ComputeBlueprintPrice(bp, plan, 0m);
        if (result.IsComplete)
        {
            ComputedPriceDisplay = $"{result.Price:N2} credits";
        }
        else
        {
            ComputedPriceDisplay = $"{result.Price:N2} credits (Incomplete)";
        }
    }

    private void LoadSampleData()
    {
        Blueprints.Add(new BlueprintRowViewModel { Name = "Laser Mk1", BlueprintType = "Weapon", TechLevel = "T1", Evolution = 3, ShipClass = 1 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Shield Generator", BlueprintType = "Component", TechLevel = "T2", Evolution = 1, ShipClass = 2 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Mining Rig Alpha", BlueprintType = "MiningRig", TechLevel = "T1", Evolution = 5, ShipClass = 0 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Cargo Pod XL", BlueprintType = "Component", TechLevel = "T3", Evolution = 2, ShipClass = 3 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Refinery Standard", BlueprintType = "Refinery", TechLevel = "T1", Evolution = 4, ShipClass = 0 });

        if (Blueprints.Count > 0)
        {
            SelectedBlueprint = Blueprints[0];
        }
    }

    private void LoadSampleStats(string name)
    {
        if (name == "Laser Mk1")
        {
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Damage", Value = "45" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Range", Value = "120" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Power Draw", Value = "15" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Mass", Value = "8" });
        }
        else
        {
            Stats.Add(new BlueprintStatRowViewModel { StatName = "HP", Value = "200" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Mass", Value = "12" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Volume", Value = "5" });
        }
    }

    private void LoadSampleResources(string name)
    {
        if (name == "Laser Mk1")
        {
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Iron (Refined)", Quantity = "50" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Copper (Refined)", Quantity = "30" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Crystal (Refined)", Quantity = "10" });
        }
        else
        {
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Iron (Refined)", Quantity = "80" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Titanium (Refined)", Quantity = "40" });
        }
    }
}
