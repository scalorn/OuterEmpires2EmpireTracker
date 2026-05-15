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

    [ObservableProperty]
    private string _uuid = string.Empty;
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
/// Provides CRUD operations via SurveyService.
/// Subscribes to <see cref="PlayerChangedMessage"/> (via base) and
/// <see cref="SurveyDataChangedMessage"/> to auto-refresh on data changes.
/// </summary>
public sealed partial class SurveyViewModel : DocumentViewModel
{
    [ObservableProperty]
    private SurveyRowViewModel? _selectedSurvey;

    [ObservableProperty]
    private string _editPlanetName = string.Empty;

    [ObservableProperty]
    private string _editSystemName = string.Empty;

    [ObservableProperty]
    private string _editSurveyId = string.Empty;

    [ObservableProperty]
    private string _editNickName = string.Empty;

    public SurveyViewModel()
    {
        Title = "Surveys";

        WeakReferenceMessenger.Default.Register<SurveyDataChangedMessage>(this, (r, m) =>
        {
            ((SurveyViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<SurveyRowViewModel> Surveys { get; } = new ObservableCollection<SurveyRowViewModel>();

    public ObservableCollection<SurveyResourceRowViewModel> Resources { get; } = new ObservableCollection<SurveyResourceRowViewModel>();

    [RelayCommand]
    private void NewSurvey()
    {
        var service = App.Services?.GetService(typeof(SurveyService)) as SurveyService;
        service?.Create(new SurveyCreateRequest
        {
            PlanetName = "New Survey",
            SystemName = string.Empty,
            SurveyID = string.Empty,
            NickName = string.Empty,
        });
    }

    [RelayCommand]
    private void SaveSurvey()
    {
        if (SelectedSurvey is null)
        {
            return;
        }

        var service = App.Services?.GetService(typeof(SurveyService)) as SurveyService;
        service?.Update(SelectedSurvey.Uuid, new SurveyUpdateRequest
        {
            PlanetName = EditPlanetName,
            SystemName = EditSystemName,
            SurveyID = EditSurveyId,
            NickName = EditNickName,
        });
    }

    [RelayCommand]
    private void DeleteSurvey()
    {
        if (SelectedSurvey is null)
        {
            return;
        }

        var service = App.Services?.GetService(typeof(SurveyService)) as SurveyService;
        service?.Delete(SelectedSurvey.Uuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Surveys.Clear();
        Resources.Clear();
        SelectedSurvey = null;
        EditPlanetName = string.Empty;
        EditSystemName = string.Empty;
        EditSurveyId = string.Empty;
        EditNickName = string.Empty;
        LoadData();
    }

    partial void OnSelectedSurveyChanged(SurveyRowViewModel? value)
    {
        if (value is not null)
        {
            EditPlanetName = value.PlanetName;

            // Load additional fields from the data model
            var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
            if (dataService is not null && dataService.IsLoaded)
            {
                var survey = dataService.Surveys.FirstOrDefault(s => s.UUID == value.Uuid);
                if (survey is not null)
                {
                    EditSystemName = survey.SystemName ?? string.Empty;
                    EditSurveyId = survey.SurveyID ?? string.Empty;
                    EditNickName = survey.NickName ?? string.Empty;
                }
                else
                {
                    EditSystemName = string.Empty;
                    EditSurveyId = string.Empty;
                    EditNickName = string.Empty;
                }
            }
            else
            {
                EditSystemName = string.Empty;
                EditSurveyId = string.Empty;
                EditNickName = string.Empty;
            }
        }
        else
        {
            EditPlanetName = string.Empty;
            EditSystemName = string.Empty;
            EditSurveyId = string.Empty;
            EditNickName = string.Empty;
        }

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
                Uuid = survey.UUID ?? string.Empty,
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
            .FirstOrDefault(s => s.UUID == survey.Uuid);
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
