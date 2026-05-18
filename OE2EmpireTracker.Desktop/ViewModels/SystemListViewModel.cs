using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the star system DataGrid.
/// </summary>
public sealed partial class SystemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _systemName = string.Empty;

    [ObservableProperty]
    private string _quadrant = string.Empty;

    [ObservableProperty]
    private string _sector = string.Empty;

    [ObservableProperty]
    private string _spectralClass = string.Empty;

    [ObservableProperty]
    private string _factionName = string.Empty;

    [ObservableProperty]
    private bool _hasSpaceport;

    [ObservableProperty]
    private bool _hasStarbase;
}

/// <summary>
/// ViewModel for the Star Systems document tab.
/// Shows a DataGrid of all star systems with a detail/editing panel.
/// </summary>
public sealed partial class SystemListViewModel : DocumentViewModel
{
    private readonly ILogger<SystemListViewModel>? _logger;

    [ObservableProperty]
    private SystemRowViewModel? _selectedSystem;

    // Detail panel fields (read-only display)
    [ObservableProperty]
    private string _detailName = string.Empty;

    [ObservableProperty]
    private string _detailCoordinates = string.Empty;

    [ObservableProperty]
    private string _detailQuadrant = string.Empty;

    [ObservableProperty]
    private string _detailSector = string.Empty;

    [ObservableProperty]
    private string _detailSpectralClass = string.Empty;

    // Editable fields
    [ObservableProperty]
    private string _editFactionName = string.Empty;

    [ObservableProperty]
    private bool _editHasSpaceport;

    [ObservableProperty]
    private bool _editHasStarbase;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SystemListViewModel()
    {
        Title = "Systems";
        _logger = App.Services?.GetService<ILogger<SystemListViewModel>>();
        LoadData();
    }

    public ObservableCollection<SystemRowViewModel> Systems { get; } = new ObservableCollection<SystemRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        LoadData();
    }

    partial void OnSelectedSystemChanged(SystemRowViewModel? value)
    {
        if (value is null)
        {
            HasSelection = false;
            DetailName = string.Empty;
            DetailCoordinates = string.Empty;
            DetailQuadrant = string.Empty;
            DetailSector = string.Empty;
            DetailSpectralClass = string.Empty;
            EditFactionName = string.Empty;
            EditHasSpaceport = false;
            EditHasStarbase = false;
            return;
        }

        HasSelection = true;
        var repo = App.Services?.GetService<SystemRepository>();
        var system = repo?.FindById(value.Id);
        if (system is not null)
        {
            DetailName = system.Name;
            DetailCoordinates = $"{system.X:F2}, {system.Y:F2}";
            DetailQuadrant = system.Quadrant.ToString();
            DetailSector = system.Sector.ToString();
            DetailSpectralClass = system.SpectralClass;
            EditFactionName = system.FactionName;
            EditHasSpaceport = system.HasSpaceport;
            EditHasStarbase = system.HasStarbase;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterSystems(value);
    }

    [RelayCommand]
    private void SaveSystem()
    {
        if (SelectedSystem is null)
        {
            return;
        }

        var repo = App.Services?.GetService<SystemRepository>();
        if (repo is null)
        {
            return;
        }

        var success = true;
        try
        {
            repo.UpdateSystem(
                SelectedSystem.Id,
                s =>
                {
                    s.FactionName = EditFactionName;
                    s.HasSpaceport = EditHasSpaceport;
                    s.HasStarbase = EditHasStarbase;
                });
        }
        catch
        {
            success = false;
        }

        if (success)
        {
            SelectedSystem.FactionName = EditFactionName;
            SelectedSystem.HasSpaceport = EditHasSpaceport;
            SelectedSystem.HasStarbase = EditHasStarbase;
            StatusMessage = $"Saved changes to {SelectedSystem.SystemName}";
            _logger?.LogInformation("Saved system {Id}: {Name}", SelectedSystem.Id, SelectedSystem.SystemName);
        }
        else
        {
            StatusMessage = "Failed to save system changes";
        }
    }

    [RelayCommand]
    private async Task ReimportAsync()
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel is null)
        {
            StatusMessage = "Cannot open file picker";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select oe2-galaxy-systems.json",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
            },
        });

        if (files.Count == 0)
        {
            return;
        }

        var filePath = files[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(filePath))
        {
            StatusMessage = "Invalid file path";
            return;
        }

        var repo = App.Services?.GetService<SystemRepository>();
        if (repo is null)
        {
            return;
        }

        var count = ImportGalaxyFile(repo, filePath);
        if (count >= 0)
        {
            StatusMessage = $"Imported {count:N0} systems from galaxy file";
            _logger?.LogInformation("Re-imported {Count} systems from {Path}", count, filePath);
            LoadData();
        }
        else
        {
            StatusMessage = "Import failed — check log for details";
        }
    }

    private void LoadData()
    {
        Systems.Clear();

        var repo = App.Services?.GetService<SystemRepository>();
        if (repo is null || repo.Count == 0)
        {
            // Try to load from known path
            var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
            string? dir = null;
            if (!string.IsNullOrEmpty(dataService?.CurrentFilePath))
            {
                dir = System.IO.Path.GetDirectoryName(dataService.CurrentFilePath);
            }

            if (dir is not null)
            {
                var systemPath = System.IO.Path.Combine(dir, "SystemData.json");
                if (System.IO.File.Exists(systemPath))
                {
                    repo?.Load(systemPath);
                }
            }
        }

        if (repo is null || repo.Count == 0)
        {
            StatusMessage = "No system data loaded";
            return;
        }

        PopulateSystems(repo.Systems);
        StatusMessage = $"{Systems.Count:N0} systems loaded";
    }

    private void FilterSystems(string filter)
    {
        Systems.Clear();

        var repo = App.Services?.GetService<SystemRepository>();
        if (repo is null || repo.Count == 0)
        {
            return;
        }

        IEnumerable<StarSystem> filtered;
        if (string.IsNullOrWhiteSpace(filter))
        {
            filtered = repo.Systems;
        }
        else
        {
            filtered = repo.Systems
                .Where(s => s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        PopulateSystems(filtered);
        StatusMessage = $"{Systems.Count:N0} systems shown";
    }

    private void PopulateSystems(IEnumerable<StarSystem> systems)
    {
        foreach (var s in systems.OrderBy(s => s.Name))
        {
            Systems.Add(new SystemRowViewModel
            {
                Id = s.Id,
                SystemName = s.Name,
                Quadrant = $"Q{s.Quadrant}",
                Sector = $"S{s.Sector}",
                SpectralClass = s.SpectralClass,
                FactionName = s.FactionName,
                HasSpaceport = s.HasSpaceport,
                HasStarbase = s.HasStarbase,
            });
        }
    }

    private int ImportGalaxyFile(SystemRepository repo, string sourcePath)
    {
        try
        {
            var json = System.IO.File.ReadAllText(sourcePath);
            var rawSystems = Newtonsoft.Json.JsonConvert.DeserializeObject<
                System.Collections.Generic.List<GalaxyImportEntry>>(json);
            if (rawSystems is null)
            {
                return -1;
            }

            var systems = rawSystems.Select(r => new StarSystem
            {
                Id = r.Id,
                Name = r.Name,
                X = r.X,
                Y = r.Y,
                Quadrant = r.Quadrant,
                Sector = r.Sector,
                Region = r.Region,
                Locality = r.Locality,
                SpectralClass = r.SpectralClass,
                FactionId = r.FactionId,
                FactionName = r.FactionName,
                FactionColor = r.FactionColor,
                HasOrbital = r.Orbital != 0,
                HasSpaceport = r.Spaceport != 0,
                HasStarbase = r.Starbase != 0,
            }).ToList();

            repo.ReplaceAll(systems);
            return systems.Count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to import galaxy data from {Path}", sourcePath);
            return -1;
        }
    }

    private sealed class GalaxyImportEntry
    {
        [Newtonsoft.Json.JsonProperty("id")]
        public int Id { get; set; }

        [Newtonsoft.Json.JsonProperty("n")]
        public string Name { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("x")]
        public decimal X { get; set; }

        [Newtonsoft.Json.JsonProperty("y")]
        public decimal Y { get; set; }

        [Newtonsoft.Json.JsonProperty("q")]
        public int Quadrant { get; set; }

        [Newtonsoft.Json.JsonProperty("s")]
        public int Sector { get; set; }

        [Newtonsoft.Json.JsonProperty("r")]
        public int Region { get; set; }

        [Newtonsoft.Json.JsonProperty("l")]
        public int Locality { get; set; }

        [Newtonsoft.Json.JsonProperty("st")]
        public string SpectralClass { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("fid")]
        public int FactionId { get; set; }

        [Newtonsoft.Json.JsonProperty("fn")]
        public string FactionName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("fc")]
        public string FactionColor { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("o")]
        public int Orbital { get; set; }

        [Newtonsoft.Json.JsonProperty("sp")]
        public int Spaceport { get; set; }

        [Newtonsoft.Json.JsonProperty("sb")]
        public int Starbase { get; set; }
    }
}
