namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the About document tab.
/// </summary>
public sealed class AboutViewModel : DocumentViewModel
{
    public AboutViewModel()
    {
        Title = "About";
    }

    public string AppName => "OE2 Empire Tracker";

    public string Version => "2.0.0-preview";

    public string Description => "Cross-platform empire management tool for Outer Empires 2. " +
        "Built with Avalonia UI and .NET 8.";

    public string Framework => "Avalonia UI 11.2 + .NET 8";
}
