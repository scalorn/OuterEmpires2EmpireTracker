using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Represents a navigation item in the sidebar.
/// </summary>
public sealed class NavItem
{
    /// <summary>Gets or sets the display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the icon text.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Gets or sets the document type identifier.</summary>
    public string DocumentType { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for the navigation sidebar tool panel.
/// </summary>
public sealed partial class NavigationViewModel : ToolViewModel
{
    private readonly MainWindowViewModel _main;

    public NavigationViewModel(MainWindowViewModel main)
    {
        _main = main;
        Title = "Navigation";

        Items = new ObservableCollection<NavItem>
        {
            new NavItem { Label = "Colonies", Icon = "🏠", DocumentType = "ColonyList" },
            new NavItem { Label = "Blueprints", Icon = "📋", DocumentType = "BlueprintList" },
            new NavItem { Label = "Surveys", Icon = "🔍", DocumentType = "SurveyList" },
            new NavItem { Label = "Ships", Icon = "🚀", DocumentType = "ShipList" },
            new NavItem { Label = "Delivery", Icon = "📦", DocumentType = "DeliveryList" },
            new NavItem { Label = "Market", Icon = "💰", DocumentType = "MarketList" },
            new NavItem { Label = "Profile", Icon = "👤", DocumentType = "Profile" },
            new NavItem { Label = "Systems", Icon = "⭐", DocumentType = "SystemList" },
            new NavItem { Label = "About", Icon = "ℹ️", DocumentType = "About" },
        };
    }

    /// <summary>Gets the navigation items.</summary>
    public ObservableCollection<NavItem> Items { get; }

    [RelayCommand]
    private void OpenDocument(NavItem? item)
    {
        if (item is null)
        {
            return;
        }

        _main.OpenDocument(item.DocumentType, item.Label);
    }
}
