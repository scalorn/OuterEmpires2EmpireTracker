namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Placeholder document for features not yet implemented.
/// Shows a "Coming Soon" message in the document tab.
/// </summary>
public sealed class PlaceholderViewModel : DocumentViewModel
{
    public PlaceholderViewModel(string title)
    {
        Title = title;
    }

    public string Message => $"{Title} — not yet implemented. This is a placeholder for the smoke test.";
}
