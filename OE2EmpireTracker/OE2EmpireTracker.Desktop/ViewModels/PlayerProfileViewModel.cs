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

    [ObservableProperty]
    private string _uuid = string.Empty;
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
/// Provides CRUD operations via PlayerProfileService.
/// Subscribes to <see cref="PlayerChangedMessage"/> (via base) and
/// <see cref="PlayerProfileDataChangedMessage"/> to auto-refresh on data changes.
/// </summary>
public sealed partial class PlayerProfileViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ProfileRowViewModel? _selectedProfile;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editFaction = string.Empty;

    [ObservableProperty]
    private string _editCredits = string.Empty;

    [ObservableProperty]
    private string _editSkillPoints = string.Empty;

    public PlayerProfileViewModel()
    {
        Title = "Player Profile";

        WeakReferenceMessenger.Default.Register<PlayerProfileDataChangedMessage>(this, (r, m) =>
        {
            ((PlayerProfileViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new ObservableCollection<ProfileRowViewModel>();

    public ObservableCollection<SkillRowViewModel> Skills { get; } = new ObservableCollection<SkillRowViewModel>();

    [RelayCommand]
    private void NewProfile()
    {
        var service = App.Services?.GetService(typeof(PlayerProfileService)) as PlayerProfileService;
        service?.Create(new PlayerProfileCreateRequest
        {
            Name = "New Profile",
            Faction = string.Empty,
        });
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var service = App.Services?.GetService(typeof(PlayerProfileService)) as PlayerProfileService;
        _ = decimal.TryParse(EditCredits, out decimal credits);
        _ = int.TryParse(EditSkillPoints, out int skillPoints);
        service?.Update(SelectedProfile.Uuid, new PlayerProfileUpdateRequest
        {
            Name = EditName,
            Faction = EditFaction,
            TotalCredits = credits,
            SkillPoints = skillPoints,
        });
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var service = App.Services?.GetService(typeof(PlayerProfileService)) as PlayerProfileService;
        service?.Delete(SelectedProfile.Uuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Profiles.Clear();
        Skills.Clear();
        SelectedProfile = null;
        EditName = string.Empty;
        EditFaction = string.Empty;
        EditCredits = string.Empty;
        EditSkillPoints = string.Empty;
        LoadData();
    }

    partial void OnSelectedProfileChanged(ProfileRowViewModel? value)
    {
        if (value is not null)
        {
            EditName = value.ProfileName;

            // Load additional fields from the data model
            var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
            if (dataService is not null && dataService.IsLoaded)
            {
                var profile = dataService.PlayerProfiles
                    .FirstOrDefault(p => p.UUID == value.Uuid);
                if (profile is not null)
                {
                    EditFaction = profile.Faction ?? string.Empty;
                    EditCredits = profile.TotalCredits.ToString();
                    EditSkillPoints = profile.SkillPoints.ToString();
                }
                else
                {
                    EditFaction = string.Empty;
                    EditCredits = string.Empty;
                    EditSkillPoints = string.Empty;
                }
            }
            else
            {
                EditFaction = string.Empty;
                EditCredits = string.Empty;
                EditSkillPoints = string.Empty;
            }
        }
        else
        {
            EditName = string.Empty;
            EditFaction = string.Empty;
            EditCredits = string.Empty;
            EditSkillPoints = string.Empty;
        }

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
                Uuid = profile.UUID ?? string.Empty,
                ProfileName = profile.Name ?? string.Empty,
                PublicRank = profile.Public?.Title ?? string.Empty,
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
            .FirstOrDefault(p => p.UUID == profile.Uuid);
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
