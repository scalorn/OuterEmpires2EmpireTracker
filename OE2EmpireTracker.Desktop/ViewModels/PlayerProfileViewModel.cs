using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the player profile list.
/// </summary>
public sealed partial class ProfileRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _publicRank = string.Empty;

    [ObservableProperty]
    private int _skillCount;
}

/// <summary>
/// Row item for the skill display grid.
/// </summary>
public sealed partial class SkillRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _skillName = string.Empty;

    [ObservableProperty]
    private int _level;
}

/// <summary>
/// ViewModel for the Player Profile document tab.
/// Shows profiles with skills and ranks.
/// </summary>
public sealed partial class PlayerProfileViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ProfileRowViewModel? _selectedProfile;

    public PlayerProfileViewModel()
    {
        Title = "Player Profile";
        LoadData();
    }

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new ObservableCollection<ProfileRowViewModel>();

    public ObservableCollection<SkillRowViewModel> Skills { get; } = new ObservableCollection<SkillRowViewModel>();

    partial void OnSelectedProfileChanged(ProfileRowViewModel? value)
    {
        LoadSkillsForProfile(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var profile in dataService.PlayerProfiles)
        {
            Profiles.Add(new ProfileRowViewModel
            {
                ProfileName = profile.Name ?? string.Empty,
                PublicRank = profile.Public?.RankName ?? string.Empty,
                SkillCount = profile.Skills?.Count ?? 0,
            });
        }

        if (Profiles.Count == 0)
        {
            LoadSampleData();
        }

        if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
    }

    private void LoadSkillsForProfile(ProfileRowViewModel? profile)
    {
        Skills.Clear();
        if (profile is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleSkills();
            return;
        }

        var playerProfile = dataService.PlayerProfiles
            .FirstOrDefault(p => p.Name == profile.ProfileName);
        if (playerProfile?.Skills is null)
        {
            LoadSampleSkills();
            return;
        }

        foreach (var kvp in playerProfile.Skills)
        {
            Skills.Add(new SkillRowViewModel
            {
                SkillName = kvp.Key,
                Level = kvp.Value.Level,
            });
        }

        if (Skills.Count == 0)
        {
            LoadSampleSkills();
        }
    }

    private void LoadSampleData()
    {
        Profiles.Add(new ProfileRowViewModel
        {
            ProfileName = "Captain Zara",
            PublicRank = "Commander",
            SkillCount = 8,
        });
        Profiles.Add(new ProfileRowViewModel
        {
            ProfileName = "Trader Milo",
            PublicRank = "Merchant",
            SkillCount = 5,
        });
        Profiles.Add(new ProfileRowViewModel
        {
            ProfileName = "Miner Rex",
            PublicRank = "Prospector",
            SkillCount = 6,
        });
    }

    private void LoadSampleSkills()
    {
        Skills.Add(new SkillRowViewModel { SkillName = "Human Resources", Level = 3 });
        Skills.Add(new SkillRowViewModel { SkillName = "Foreman", Level = 5 });
        Skills.Add(new SkillRowViewModel { SkillName = "Builder", Level = 4 });
        Skills.Add(new SkillRowViewModel { SkillName = "Refining Focus", Level = 2 });
    }
}
