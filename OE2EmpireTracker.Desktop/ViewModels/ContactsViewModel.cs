using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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
/// Shows factions and external characters.
/// </summary>
public sealed partial class ContactsViewModel : DocumentViewModel
{
    public ContactsViewModel()
    {
        Title = "Contacts";
        LoadData();
    }

    public ObservableCollection<FactionRowViewModel> Factions { get; } = new ObservableCollection<FactionRowViewModel>();

    public ObservableCollection<CharacterRowViewModel> Characters { get; } = new ObservableCollection<CharacterRowViewModel>();

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
