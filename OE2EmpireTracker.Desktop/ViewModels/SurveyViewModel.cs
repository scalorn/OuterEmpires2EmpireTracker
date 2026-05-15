using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

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

    public SurveyViewModel()
    {
        Title = "Surveys";
        LoadData();
    }

    public ObservableCollection<SurveyRowViewModel> Surveys { get; } = new ObservableCollection<SurveyRowViewModel>();

    public ObservableCollection<SurveyResourceRowViewModel> Resources { get; } = new ObservableCollection<SurveyResourceRowViewModel>();

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
