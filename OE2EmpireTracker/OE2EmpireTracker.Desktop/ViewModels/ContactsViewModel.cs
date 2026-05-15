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
/// Row item for the faction DataGrid.
/// </summary>
public sealed partial class FactionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _factionUuid = string.Empty;

    [ObservableProperty]
    private string _factionName = string.Empty;

    [ObservableProperty]
    private int _memberCount;
}

/// <summary>
/// Row item for the external character DataGrid.
/// </summary>
public sealed partial class CharacterRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _characterUuid = string.Empty;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _factionName = string.Empty;
}

/// <summary>
/// ViewModel for the Contacts document tab.
/// Shows factions and external characters with CRUD operations.
/// </summary>
public sealed partial class ContactsViewModel : DocumentViewModel
{
    [ObservableProperty]
    private FactionRowViewModel? _selectedFaction;

    [ObservableProperty]
    private CharacterRowViewModel? _selectedCharacter;

    [ObservableProperty]
    private string _editFactionName = string.Empty;

    [ObservableProperty]
    private string _editCharacterName = string.Empty;

    public ContactsViewModel()
    {
        Title = "Contacts";

        WeakReferenceMessenger.Default.Register<ContactDataChangedMessage>(this, (r, m) =>
        {
            ((ContactsViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<FactionRowViewModel> Factions { get; } = new ObservableCollection<FactionRowViewModel>();

    public ObservableCollection<CharacterRowViewModel> Characters { get; } = new ObservableCollection<CharacterRowViewModel>();

    [RelayCommand]
    private void NewFaction()
    {
        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.CreateFaction(new FactionCreateRequest { Name = "New Faction" });
    }

    [RelayCommand]
    private void SaveFaction()
    {
        if (SelectedFaction is null || string.IsNullOrEmpty(SelectedFaction.FactionUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.UpdateFaction(SelectedFaction.FactionUuid, new FactionUpdateRequest { Name = EditFactionName });
    }

    [RelayCommand]
    private void DeleteFaction()
    {
        if (SelectedFaction is null || string.IsNullOrEmpty(SelectedFaction.FactionUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.DeleteFaction(SelectedFaction.FactionUuid);
    }

    [RelayCommand]
    private void NewCharacter()
    {
        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.CreateCharacter(new ExternalCharacterCreateRequest { Name = "New Character" });
    }

    [RelayCommand]
    private void SaveCharacter()
    {
        if (SelectedCharacter is null || string.IsNullOrEmpty(SelectedCharacter.CharacterUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.UpdateCharacter(SelectedCharacter.CharacterUuid, new ExternalCharacterUpdateRequest { Name = EditCharacterName });
    }

    [RelayCommand]
    private void DeleteCharacter()
    {
        if (SelectedCharacter is null || string.IsNullOrEmpty(SelectedCharacter.CharacterUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ContactsService)) as ContactsService;
        svc?.DeleteCharacter(SelectedCharacter.CharacterUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Factions.Clear();
        Characters.Clear();
        SelectedFaction = null;
        SelectedCharacter = null;
        EditFactionName = string.Empty;
        EditCharacterName = string.Empty;
        LoadData();
    }

    partial void OnSelectedFactionChanged(FactionRowViewModel? value)
    {
        EditFactionName = value?.FactionName ?? string.Empty;
    }

    partial void OnSelectedCharacterChanged(CharacterRowViewModel? value)
    {
        EditCharacterName = value?.CharacterName ?? string.Empty;
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
                CharacterUuid = character.UUID ?? string.Empty,
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
