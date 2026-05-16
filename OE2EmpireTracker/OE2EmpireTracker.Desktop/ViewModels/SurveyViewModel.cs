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
/// Row item for the survey list.
/// </summary>
public sealed partial class SurveyRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planetName = string.Empty;

    [ObservableProperty]
    private string _surveyType = string.Empty;

    [ObservableProperty]
    private int _resourceCount;
}

/// <summary>
/// Row item for the survey resource DataGrid.
/// </summary>
public sealed partial class SurveyResourceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _purity = string.Empty;

    [ObservableProperty]
    private string _yield = string.Empty;

    [ObservableProperty]
    private string _maxReserve = string.Empty;
}

/// <summary>
/// ViewModel for the Survey document tab.
/// Shows surveys with resource details.
/// </summary>
public sealed partial class SurveyViewModel : DocumentViewModel
{
    [ObservableProperty]
    private SurveyRowViewModel? _selectedSurvey;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    public SurveyViewModel()
    {
        Title = "Surveys";
        LoadData();
    }

    public ObservableCollection<SurveyRowViewModel> Surveys { get; } = new ObservableCollection<SurveyRowViewModel>();

    public ObservableCollection<SurveyResourceRowViewModel> Resources { get; } = new ObservableCollection<SurveyResourceRowViewModel>();

    /// <summary>
    /// Imports survey data from the clipboard HTML and adds a new survey.
    /// Implements F4.2-F4.3: asteroid auto-detection and max reserve extraction.
    /// </summary>
    [RelayCommand]
    private async Task ImportClipboardAsync()
    {
        var clipboardService = App.Services?.GetService(typeof(IClipboardService)) as IClipboardService;
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var asteroidService = App.Services?.GetService(typeof(AsteroidService)) as AsteroidService;
        var loggerFactory = App.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory;

        if (clipboardService is null || dataService is null || loggerFactory is null)
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

        var parser = new SurveyParser(loggerFactory.CreateLogger<SurveyParser>());
        var survey = parser.ParseSurveyHtml(fragment);
        if (survey is null)
        {
            ImportStatus = "Failed to parse survey HTML";
            return;
        }

        survey.UUID = Guid.NewGuid().ToString();
        survey.OwnerUUID = dataService.CurrentPlayerUUID;

        // F4.2-F4.3: Asteroid auto-detection and max reserve extraction
        if (survey.SurveyType == SurveyType.Asteroid && asteroidService is not null)
        {
            LinkOrCreateAsteroid(survey, dataService, asteroidService);
        }

        dataService.AddSurvey(survey);
        dataService.OnSurveyDataChanged(survey.UUID);
        dataService.WriteContext();

        // Add to the local collection
        Surveys.Add(new SurveyRowViewModel
        {
            PlanetName = survey.PlanetName ?? string.Empty,
            SurveyType = survey.SurveyType.ToString(),
            ResourceCount = survey.Resources?.Count ?? 0,
        });

        SelectedSurvey = Surveys.LastOrDefault();
        ImportStatus = $"Imported survey for {survey.PlanetName}";
    }

    /// <summary>
    /// Links an asteroid survey to an existing asteroid or creates a new one (F4.2).
    /// Updates asteroid reserves from parsed max reserves (F4.3).
    /// </summary>
    private static void LinkOrCreateAsteroid(Survey survey, DataService dataService, AsteroidService asteroidService)
    {
        // Find existing asteroid by SystemName + PlanetName
        var existing = dataService.Asteroids
            .FirstOrDefault(a =>
                string.Equals(a.SystemName, survey.SystemName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Name, survey.PlanetName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            survey.AsteroidUUID = existing.UUID;

            // Update reserves from parsed max reserves
            if (survey.ParsedMaxReserves is not null && survey.ParsedMaxReserves.Count > 0)
            {
                var reserves = BuildReservesFromSurvey(survey);
                asteroidService.Update(existing.UUID, new AsteroidUpdateRequest
                {
                    Name = existing.Name,
                    SystemName = existing.SystemName,
                    Reserves = reserves,
                });
            }
        }
        else
        {
            // Create new asteroid
            var reserves = survey.ParsedMaxReserves is not null && survey.ParsedMaxReserves.Count > 0
                ? BuildReservesFromSurvey(survey)
                : new List<AsteroidReserve>();

            var request = new AsteroidCreateRequest
            {
                Name = survey.PlanetName ?? string.Empty,
                SystemName = survey.SystemName ?? string.Empty,
                Reserves = reserves,
            };

            asteroidService.Create(request);

            // Find the newly created asteroid to get its UUID
            var created = dataService.Asteroids
                .FirstOrDefault(a =>
                    string.Equals(a.SystemName, survey.SystemName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.Name, survey.PlanetName, StringComparison.OrdinalIgnoreCase));
            if (created is not null)
            {
                survey.AsteroidUUID = created.UUID;
            }
        }
    }

    /// <summary>
    /// Builds AsteroidReserve list from survey's ParsedMaxReserves and Resources.
    /// </summary>
    private static List<AsteroidReserve> BuildReservesFromSurvey(Survey survey)
    {
        var reserves = new List<AsteroidReserve>();
        if (survey.ParsedMaxReserves is null)
        {
            return reserves;
        }

        foreach (var kvp in survey.ParsedMaxReserves)
        {
            string purity = string.Empty;
            if (survey.Resources is not null &&
                survey.Resources.TryGetValue(kvp.Key, out var resource))
            {
                purity = resource.Purity;
            }

            reserves.Add(new AsteroidReserve
            {
                ResourceName = kvp.Key,
                Purity = purity,
                MaxReserve = kvp.Value,
            });
        }

        return reserves;
    }

    partial void OnSelectedSurveyChanged(SurveyRowViewModel? value)
    {
        LoadResourcesForSurvey(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var survey in dataService.Surveys)
        {
            Surveys.Add(new SurveyRowViewModel
            {
                PlanetName = survey.PlanetName ?? string.Empty,
                SurveyType = survey.SurveyType.ToString(),
                ResourceCount = survey.Resources?.Count ?? 0,
            });
        }

        if (Surveys.Count == 0)
        {
            LoadSampleData();
        }

        if (Surveys.Count > 0)
        {
            SelectedSurvey = Surveys[0];
        }
    }

    private void LoadResourcesForSurvey(SurveyRowViewModel? survey)
    {
        Resources.Clear();
        if (survey is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleResources(survey.PlanetName);
            return;
        }

        var surveyModel = dataService.Surveys
            .FirstOrDefault(s => s.PlanetName == survey.PlanetName);
        if (surveyModel?.Resources is null)
        {
            LoadSampleResources(survey.PlanetName);
            return;
        }

        // F3.3: Look up linked asteroid for max reserve display
        Asteroid? linkedAsteroid = null;
        if (surveyModel.SurveyType == SurveyType.Asteroid &&
            !string.IsNullOrEmpty(surveyModel.AsteroidUUID))
        {
            linkedAsteroid = dataService.Asteroids
                .FirstOrDefault(a => a.UUID == surveyModel.AsteroidUUID);
        }

        foreach (var kvp in surveyModel.Resources)
        {
            string maxReserve = string.Empty;

            // F3.3: Populate MaxReserve for asteroid surveys
            if (linkedAsteroid?.Reserves is not null)
            {
                var reserve = linkedAsteroid.Reserves
                    .FirstOrDefault(r =>
                        string.Equals(r.ResourceName, kvp.Value.Resource, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.Purity, kvp.Value.Purity, StringComparison.OrdinalIgnoreCase));
                if (reserve is not null && reserve.MaxReserve > 0)
                {
                    maxReserve = reserve.MaxReserve.ToString("N0");
                }
            }

            Resources.Add(new SurveyResourceRowViewModel
            {
                ResourceName = kvp.Value.Resource ?? kvp.Key,
                Purity = kvp.Value.Purity ?? string.Empty,
                Yield = kvp.Value.Amount ?? string.Empty,
                MaxReserve = maxReserve,
            });
        }

        if (Resources.Count == 0)
        {
            LoadSampleResources(survey.PlanetName);
        }
    }

    private void LoadSampleData()
    {
        Surveys.Add(new SurveyRowViewModel { PlanetName = "Terra Nova", SurveyType = "Planet", ResourceCount = 5 });
        Surveys.Add(new SurveyRowViewModel { PlanetName = "Kepler-7b", SurveyType = "Planet", ResourceCount = 4 });
        Surveys.Add(new SurveyRowViewModel { PlanetName = "Asteroid X-42", SurveyType = "Asteroid", ResourceCount = 3 });
    }

    private void LoadSampleResources(string planetName)
    {
        if (planetName == "Terra Nova")
        {
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Iron", Purity = "High", Yield = "120" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Copper", Purity = "Medium", Yield = "85" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Silicon", Purity = "Low", Yield = "45" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Gold", Purity = "High", Yield = "30" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Titanium", Purity = "Medium", Yield = "60" });
        }
        else if (planetName == "Kepler-7b")
        {
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Platinum", Purity = "High", Yield = "95" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Uranium", Purity = "Low", Yield = "20" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Carbon", Purity = "Medium", Yield = "110" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Helium-3", Purity = "High", Yield = "75" });
        }
        else
        {
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Nickel", Purity = "Medium", Yield = "55", MaxReserve = "7,123" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Cobalt", Purity = "High", Yield = "40", MaxReserve = "6,998" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Iron", Purity = "Low", Yield = "90", MaxReserve = "5,400" });
        }
    }
}
