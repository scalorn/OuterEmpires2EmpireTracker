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
/// Row item for the ship template DataGrid.
/// </summary>
public sealed partial class ShipTemplateRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _templateUuid = string.Empty;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _hullClass = string.Empty;

    [ObservableProperty]
    private int _slotCount;
}

/// <summary>
/// ViewModel for the Ship Template document tab.
/// Shows ship templates with CRUD operations.
/// </summary>
public sealed partial class ShipTemplateViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ShipTemplateRowViewModel? _selectedTemplate;

    [ObservableProperty]
    private string _editTemplateName = string.Empty;

    public ShipTemplateViewModel()
    {
        Title = "Ship Templates";

        WeakReferenceMessenger.Default.Register<ShipTemplateDataChangedMessage>(this, (r, m) =>
        {
            ((ShipTemplateViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<ShipTemplateRowViewModel> Templates { get; } = new ObservableCollection<ShipTemplateRowViewModel>();

    [RelayCommand]
    private void NewTemplate()
    {
        var svc = App.Services?.GetService(typeof(ShipTemplateService)) as ShipTemplateService;
        svc?.Create(new ShipTemplateCreateRequest { Name = "New Template" });
    }

    [RelayCommand]
    private void SaveTemplate()
    {
        if (SelectedTemplate is null || string.IsNullOrEmpty(SelectedTemplate.TemplateUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ShipTemplateService)) as ShipTemplateService;
        svc?.Update(SelectedTemplate.TemplateUuid, new ShipTemplateUpdateRequest { Name = EditTemplateName });
    }

    [RelayCommand]
    private void DeleteTemplate()
    {
        if (SelectedTemplate is null || string.IsNullOrEmpty(SelectedTemplate.TemplateUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ShipTemplateService)) as ShipTemplateService;
        svc?.Delete(SelectedTemplate.TemplateUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Templates.Clear();
        SelectedTemplate = null;
        EditTemplateName = string.Empty;
        LoadData();
    }

    partial void OnSelectedTemplateChanged(ShipTemplateRowViewModel? value)
    {
        EditTemplateName = value?.TemplateName ?? string.Empty;
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var template in dataService.ShipTemplates)
        {
            Templates.Add(new ShipTemplateRowViewModel
            {
                TemplateUuid = template.UUID ?? string.Empty,
                TemplateName = template.Name ?? string.Empty,
                HullClass = template.HullBlueprintUUID ?? string.Empty,
                SlotCount = template.Components?.Count ?? 0,
            });
        }

        if (Templates.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Templates.Add(new ShipTemplateRowViewModel
        {
            TemplateName = "Scout Mk I",
            HullClass = "Light Fighter",
            SlotCount = 4,
        });
        Templates.Add(new ShipTemplateRowViewModel
        {
            TemplateName = "Hauler Mk II",
            HullClass = "Freighter",
            SlotCount = 8,
        });
    }
}
