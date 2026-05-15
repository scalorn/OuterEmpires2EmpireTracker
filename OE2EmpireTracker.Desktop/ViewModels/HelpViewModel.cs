namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Help document tab.
/// Displays help topics with markdown rendering.
/// </summary>
public sealed class HelpViewModel : DocumentViewModel
{
    public HelpViewModel()
    {
        Title = "Help";
    }

    public string Content => "# OE2 Empire Tracker Help\n\n" +
        "## Getting Started\n\n" +
        "Use the navigation sidebar to open different feature areas.\n\n" +
        "## Features\n\n" +
        "- **Colonies** — Manage colony structures, mining, refining, research, manufacturing\n" +
        "- **Blueprints** — Track blueprint scanning, evolution, and manufacturing\n" +
        "- **Surveys** — Import and view planet resource surveys\n" +
        "- **Ships** — Configure ship templates and track ship instances\n" +
        "- **Delivery** — Plan and execute delivery routes between colonies\n" +
        "- **Market** — Track market listings and transactions\n" +
        "- **Profile** — Manage player profiles, skills, and ranks\n" +
        "- **Systems** — View star systems and asteroids\n\n" +
        "## Keyboard Shortcuts\n\n" +
        "- **Ctrl+Tab** — Switch between open tabs\n" +
        "- **Ctrl+W** — Close current tab\n" +
        "- **F5** — Refresh current view\n";
}
