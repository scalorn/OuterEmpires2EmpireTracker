using System;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Models;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Preferences document tab.
/// Manages theme selection and the nine configurable threshold values.
/// </summary>
public sealed partial class PreferencesViewModel : DocumentViewModel
{
    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private string _structureCountYellow = "60";

    [ObservableProperty]
    private string _structureCountRed = "66";

    [ObservableProperty]
    private string _workerRequestDueYellow = string.Empty;

    [ObservableProperty]
    private string _workerRequestDueRed = string.Empty;

    [ObservableProperty]
    private string _colonyImportStalenessYellow = string.Empty;

    [ObservableProperty]
    private string _colonyImportStalenessRed = string.Empty;

    [ObservableProperty]
    private string _backgroundProcessingInterval = string.Empty;

    [ObservableProperty]
    private string _adminReportRefresh = string.Empty;

    [ObservableProperty]
    private string _countdownDisplayRefresh = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public PreferencesViewModel()
    {
        Title = "Preferences";

        // Initialize theme index from current app theme
        var currentTheme = Application.Current?.RequestedThemeVariant;
        if (currentTheme == ThemeVariant.Dark)
        {
            _selectedThemeIndex = 1;
        }
        else if (currentTheme == ThemeVariant.Light)
        {
            _selectedThemeIndex = 2;
        }
        else
        {
            _selectedThemeIndex = 0;
        }

        LoadFromStore();
    }

    public string[] ThemeOptions { get; } = new[] { "System Default", "Dark", "Light" };

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        app.RequestedThemeVariant = value switch
        {
            1 => ThemeVariant.Dark,
            2 => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }

    [RelayCommand]
    private void Ok()
    {
        if (!Validate())
        {
            return;
        }

        var store = App.Services?.GetService<PreferencesStore>();
        if (store is null)
        {
            return;
        }

        var prefs = new ThresholdPreferences
        {
            StructureCountYellow = int.Parse(StructureCountYellow),
            StructureCountRed = int.Parse(StructureCountRed),
        };

        CountdownFormatParser.TryParse(WorkerRequestDueYellow, out long wdyVal);
        prefs.WorkerRequestDueYellow = wdyVal;

        CountdownFormatParser.TryParse(WorkerRequestDueRed, out long wdrVal);
        prefs.WorkerRequestDueRed = wdrVal;

        CountdownFormatParser.TryParse(ColonyImportStalenessYellow, out long cisyVal);
        prefs.ColonyImportStalenessYellow = cisyVal;

        CountdownFormatParser.TryParse(ColonyImportStalenessRed, out long cisrVal);
        prefs.ColonyImportStalenessRed = cisrVal;

        CountdownFormatParser.TryParse(BackgroundProcessingInterval, out long bpiVal);
        prefs.BackgroundProcessingIntervalSeconds = bpiVal;

        CountdownFormatParser.TryParse(AdminReportRefresh, out long arrVal);
        prefs.AdminReportRefreshSeconds = arrVal;

        CountdownFormatParser.TryParse(CountdownDisplayRefresh, out long cdrVal);
        prefs.CountdownDisplayRefreshSeconds = cdrVal;

        store.Save(prefs);
        ValidationError = string.Empty;
    }

    [RelayCommand]
    private void Cancel()
    {
        LoadFromStore();
        ValidationError = string.Empty;
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new ThresholdPreferences();
        PopulateFromThresholds(defaults);
        ValidationError = string.Empty;
    }

    private void LoadFromStore()
    {
        var store = App.Services?.GetService<PreferencesStore>();
        if (store is not null)
        {
            PopulateFromThresholds(store.Thresholds);
        }
        else
        {
            PopulateFromThresholds(new ThresholdPreferences());
        }
    }

    private void PopulateFromThresholds(ThresholdPreferences t)
    {
        StructureCountYellow = t.StructureCountYellow.ToString();
        StructureCountRed = t.StructureCountRed.ToString();
        WorkerRequestDueYellow = CountdownFormatParser.FormatSeconds(t.WorkerRequestDueYellow);
        WorkerRequestDueRed = CountdownFormatParser.FormatSeconds(t.WorkerRequestDueRed);
        ColonyImportStalenessYellow = CountdownFormatParser.FormatSeconds(t.ColonyImportStalenessYellow);
        ColonyImportStalenessRed = CountdownFormatParser.FormatSeconds(t.ColonyImportStalenessRed);
        BackgroundProcessingInterval = CountdownFormatParser.FormatSeconds(t.BackgroundProcessingIntervalSeconds);
        AdminReportRefresh = CountdownFormatParser.FormatSeconds(t.AdminReportRefreshSeconds);
        CountdownDisplayRefresh = CountdownFormatParser.FormatSeconds(t.CountdownDisplayRefreshSeconds);
    }

    private bool Validate()
    {
        // Structure count: must be positive integers, yellow < red
        if (!int.TryParse(StructureCountYellow, out int scYellow) || scYellow <= 0)
        {
            ValidationError = "Structure Count Yellow must be a positive integer.";
            return false;
        }

        if (!int.TryParse(StructureCountRed, out int scRed) || scRed <= 0)
        {
            ValidationError = "Structure Count Red must be a positive integer.";
            return false;
        }

        if (scYellow >= scRed)
        {
            ValidationError = "Structure Count Yellow must be less than Red.";
            return false;
        }

        // Worker request due: must parse, both positive, yellow > red
        if (!CountdownFormatParser.TryParse(WorkerRequestDueYellow, out long wdyVal) || wdyVal <= 0)
        {
            ValidationError = "Worker Request Due Yellow must be a positive duration.";
            return false;
        }

        if (!CountdownFormatParser.TryParse(WorkerRequestDueRed, out long wdrVal) || wdrVal <= 0)
        {
            ValidationError = "Worker Request Due Red must be a positive duration.";
            return false;
        }

        if (wdyVal <= wdrVal)
        {
            ValidationError = "Worker Request Due Yellow must be greater than Red (larger window triggers first).";
            return false;
        }

        // Colony import staleness: must parse, both positive, yellow < red
        if (!CountdownFormatParser.TryParse(ColonyImportStalenessYellow, out long cisyVal) || cisyVal <= 0)
        {
            ValidationError = "Colony Import Staleness Yellow must be a positive duration.";
            return false;
        }

        if (!CountdownFormatParser.TryParse(ColonyImportStalenessRed, out long cisrVal) || cisrVal <= 0)
        {
            ValidationError = "Colony Import Staleness Red must be a positive duration.";
            return false;
        }

        if (cisyVal >= cisrVal)
        {
            ValidationError = "Colony Import Staleness Yellow must be less than Red.";
            return false;
        }

        // Background processing interval: must parse, positive
        if (!CountdownFormatParser.TryParse(BackgroundProcessingInterval, out long bpiVal) || bpiVal <= 0)
        {
            ValidationError = "Background Processing Interval must be a positive duration.";
            return false;
        }

        // Admin report refresh: must parse, positive
        if (!CountdownFormatParser.TryParse(AdminReportRefresh, out long arrVal) || arrVal <= 0)
        {
            ValidationError = "Admin Report Refresh must be a positive duration.";
            return false;
        }

        // Countdown display refresh: must parse, >= 1 second
        if (!CountdownFormatParser.TryParse(CountdownDisplayRefresh, out long cdrVal) || cdrVal < 1)
        {
            ValidationError = "Countdown Display Refresh must be at least 1 second.";
            return false;
        }

        ValidationError = string.Empty;
        return true;
    }
}
