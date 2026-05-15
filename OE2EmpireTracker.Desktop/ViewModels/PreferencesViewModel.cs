using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Preferences document tab.
/// Manages theme selection and application settings.
/// </summary>
public sealed partial class PreferencesViewModel : DocumentViewModel
{
    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private int _processingIntervalSeconds = 60;

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
            _selectedThemeIndex = 0; // System default
        }
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
    private void Save()
    {
        // TODO: Persist preferences to JSON file
    }
}
