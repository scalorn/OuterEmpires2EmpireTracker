using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the faction DataGrid.
/// </summary>
public sealed partial class FactionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _factionName = string.Empty;

    [ObservableProperty]
    private int _memberCount;

    /// <summary>Gets or sets the UUID of the faction.</summary>
    public string FactionUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the external character DataGrid.
/// </summary>
public sealed partial class CharacterRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _factionName = string.Empty;
}

/// <summary>
/// ViewModel for the Contacts document tab.
/// Shows factions and external characters with delete protection.
/// </summary>
public sealed partial class ContactsViewModel : DocumentViewModel
{
    [ObservableProperty]
    private FactionRowViewModel? _selectedFaction;

    [ObservableProperty]
    private string _deleteError = string.Empty;

    public ContactsViewModel()
    {
        Title = "Contacts";
        LoadData();
    }

    public ObservableCollection<FactionRowViewModel> Factions { get; } = new ObservableCollection<FactionRowViewModel>();

    public ObservableCollection<CharacterRowViewModel> Characters { get; } = new ObservableCollection<CharacterRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Factions.Clear();
        Characters.Clear();
        SelectedFaction = null;
        DeleteError = string.Empty;
        LoadData();
    }

    /// <summary>Deletes the selected faction with reference count protection.</summary>
    [RelayCommand]
    private void DeleteFaction()
    {
        if (SelectedFaction is null)
        {
            return;
        }

        var contactsService = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        if (contactsService is null)
        {
            return;
        }

        string? error = contactsService.DeleteFaction(SelectedFaction.FactionUuid);
        if (error is not null)
        {
            DeleteError = error;
            return;
        }

        DeleteError = string.Empty;
        RefreshData();
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var factions = dataService.Factions;
        var characters = dataService.ExternalCharacters;

        foreach (var faction in factions)
        {
            var memberCount = characters.Count(c => c.FactionUUID == faction.UUID);
            Factions.Add(new FactionRowViewModel
            {
                FactionUuid = faction.UUID ?? string.Empty,
                FactionName = faction.Name ?? string.Empty,
                MemberCount = memberCount,
            });
        }

        foreach (var character in characters)
        {
            var faction = factions.FirstOrDefault(f => f.UUID == character.FactionUUID);
            Characters.Add(new CharacterRowViewModel
            {
                CharacterName = character.Name ?? string.Empty,
                FactionName = faction?.Name ?? string.Empty,
            });
        }

        if (Factions.Count == 0 && Characters.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Factions.Add(new FactionRowViewModel
        {
            FactionName = "Galactic Trade Union",
            MemberCount = 5,
        });
        Factions.Add(new FactionRowViewModel
        {
            FactionName = "Frontier Alliance",
            MemberCount = 3,
        });
        Characters.Add(new CharacterRowViewModel
        {
            CharacterName = "Commander Vex",
            FactionName = "Galactic Trade Union",
        });
        Characters.Add(new CharacterRowViewModel
        {
            CharacterName = "Pilot Nova",
            FactionName = "Frontier Alliance",
        });
    }
}
