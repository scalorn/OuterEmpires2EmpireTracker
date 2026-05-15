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

    [ObservableProperty]
    private SurveyResourceRowViewModel? _selectedResource;

    [ObservableProperty]
    private bool _isDirty;

    public SurveyViewModel()
    {
        Title = "Surveys";
        LoadData();
    }

    public ObservableCollection<SurveyRowViewModel> Surveys { get; } = new ObservableCollection<SurveyRowViewModel>();

    public ObservableCollection<SurveyResourceRowViewModel> Resources { get; } = new ObservableCollection<SurveyResourceRowViewModel>();

    /// <summary>
    /// Imports survey data from the clipboard HTML and adds a new survey.
    /// </summary>
    [RelayCommand]
    private async Task ImportClipboardAsync()
    {
        var clipboardService = App.Services?.GetService(typeof(IClipboardService)) as IClipboardService;
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
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
    /// Adds a new empty resource row to the Resources grid.
    /// </summary>
    [RelayCommand]
    private void AddResource()
    {
        Resources.Add(new SurveyResourceRowViewModel { ResourceName = "New Resource", Purity = "Medium", Yield = "0" });
        IsDirty = true;
    }

    /// <summary>
    /// Removes the selected resource row from the Resources grid.
    /// </summary>
    [RelayCommand]
    private void RemoveResource()
    {
        if (SelectedResource is not null)
        {
            Resources.Remove(SelectedResource);
            SelectedResource = null;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Saves resources back to the survey model via the service layer.
    /// </summary>
    [RelayCommand]
    private void SaveSurvey()
    {
        if (SelectedSurvey is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var surveyService = App.Services?.GetService(typeof(SurveyService)) as SurveyService;
        if (dataService is null || !dataService.IsLoaded || surveyService is null)
        {
            return;
        }

        var surveyModel = dataService.Surveys
            .FirstOrDefault(s => s.PlanetName == SelectedSurvey.PlanetName);
        if (surveyModel is null)
        {
            return;
        }

        // Build resources from the grid
        var resources = new Dictionary<string, SurveyResource>();
        foreach (var row in Resources)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceName))
            {
                resources[row.ResourceName] = new SurveyResource(row.ResourceName, row.Purity, row.Yield);
            }
        }

        var request = new SurveyUpdateRequest
        {
            PlanetName = surveyModel.PlanetName,
            SystemName = surveyModel.SystemName,
            SurveyID = surveyModel.SurveyID,
            NickName = surveyModel.NickName,
            ScannedBy = surveyModel.ScannedBy,
            DateTime = surveyModel.DateTime,
            ScannerBlueprintUUID = surveyModel.ScannerBlueprintUUID,
            AsteroidUUID = surveyModel.AsteroidUUID,
            SurveyType = surveyModel.SurveyType,
            Resources = resources,
        };

        surveyService.Update(surveyModel.UUID, request);
        SelectedSurvey.ResourceCount = Resources.Count;
        IsDirty = false;
    }

    /// <summary>
    /// Marks the survey as dirty when a cell is edited.
    /// </summary>
    [RelayCommand]
    private void MarkDirty()
    {
        IsDirty = true;
    }

    partial void OnSelectedSurveyChanged(SurveyRowViewModel? value)
    {
        LoadResourcesForSurvey(value);
        IsDirty = false;
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

        foreach (var kvp in surveyModel.Resources)
        {
            Resources.Add(new SurveyResourceRowViewModel
            {
                ResourceName = kvp.Value.Resource ?? kvp.Key,
                Purity = kvp.Value.Purity ?? string.Empty,
                Yield = kvp.Value.Amount ?? string.Empty,
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
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Nickel", Purity = "Medium", Yield = "55" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Cobalt", Purity = "High", Yield = "40" });
            Resources.Add(new SurveyResourceRowViewModel { ResourceName = "Iron", Purity = "Low", Yield = "90" });
        }
    }
}
