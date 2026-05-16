using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;
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
/// Represents a skill group with its enabled state and child skills.
/// </summary>
public sealed partial class SkillGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private ObservableCollection<SkillRowViewModel> _skills = new();
}

/// <summary>
/// ViewModel for the Player Profile document tab.
/// Shows profiles with skills grouped by skill group (G4).
/// </summary>
public sealed partial class PlayerProfileViewModel : DocumentViewModel
{
    /// <summary>
    /// Skill tree mapping: group name → skill names in that group.
    /// </summary>
    private static readonly (SkillGroupName Group, SkillName[] Skills)[] SkillTree =
    {
        (SkillGroupName.ColonyDirector, new[] { SkillName.HumanResources, SkillName.Foreman }),
        (SkillGroupName.ColonyFounder, new[] { SkillName.Founder, SkillName.EnergyEfficiency, SkillName.Builder }),
        (SkillGroupName.ColonyOperations, new[] { SkillName.RefiningFocus, SkillName.ProductionFocus, SkillName.ExtractionFocus }),
        (SkillGroupName.Commander, new[] { SkillName.DamageControl }),
        (SkillGroupName.Engineer, new[] { SkillName.EngineeringCapacity }),
        (SkillGroupName.Entrepeneur, new[] { SkillName.SoundsAsAPound, SkillName.SelfMadeMillionaire, SkillName.AAAHealthcare }),
        (SkillGroupName.JobManagement, new[] { SkillName.JobOpportunities, SkillName.ContractManagement }),
        (SkillGroupName.Researcher, new[] { SkillName.ResearchReview, SkillName.ResearchMethods, SkillName.ResearchFocus }),
        (SkillGroupName.Surveyor, new[] { SkillName.SurveyingMethods, SkillName.ScanningMethods, SkillName.Quartermaster }),
        (SkillGroupName.Trader, new[] { SkillName.Broker }),
    };

    [ObservableProperty]
    private ProfileRowViewModel? _selectedProfile;

    public PlayerProfileViewModel()
    {
        Title = "Player Profile";
        LoadData();
    }

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new ObservableCollection<ProfileRowViewModel>();

    public ObservableCollection<SkillRowViewModel> Skills { get; } = new ObservableCollection<SkillRowViewModel>();

    public ObservableCollection<SkillGroupViewModel> SkillGroups { get; } = new ObservableCollection<SkillGroupViewModel>();

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
        SkillGroups.Clear();

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

        // G4: Populate skill groups from the profile's data
        foreach (var (group, skillNames) in SkillTree)
        {
            var groupVm = new SkillGroupViewModel
            {
                GroupName = group.ToDisplayName(),
                IsEnabled = playerProfile.GetSkillGroup(group),
            };

            foreach (var skillName in skillNames)
            {
                string displayName = skillName.ToDisplayName();
                var skill = playerProfile.GetSkill(skillName);
                var skillRow = new SkillRowViewModel
                {
                    SkillName = displayName,
                    Level = skill.Level,
                };

                groupVm.Skills.Add(skillRow);
                Skills.Add(skillRow);
            }

            SkillGroups.Add(groupVm);
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
        // Populate sample skill groups for design-time preview
        var directorGroup = new SkillGroupViewModel
        {
            GroupName = "Colony Director",
            IsEnabled = true,
        };
        directorGroup.Skills.Add(new SkillRowViewModel { SkillName = "Human Resources", Level = 3 });
        directorGroup.Skills.Add(new SkillRowViewModel { SkillName = "Foreman", Level = 5 });
        SkillGroups.Add(directorGroup);

        var founderGroup = new SkillGroupViewModel
        {
            GroupName = "Colony Founder",
            IsEnabled = true,
        };
        founderGroup.Skills.Add(new SkillRowViewModel { SkillName = "Founder", Level = 2 });
        founderGroup.Skills.Add(new SkillRowViewModel { SkillName = "Energy Efficiency", Level = 1 });
        founderGroup.Skills.Add(new SkillRowViewModel { SkillName = "Builder", Level = 4 });
        SkillGroups.Add(founderGroup);

        var opsGroup = new SkillGroupViewModel
        {
            GroupName = "Colony Operations",
            IsEnabled = false,
        };
        opsGroup.Skills.Add(new SkillRowViewModel { SkillName = "Refining Focus", Level = 2 });
        opsGroup.Skills.Add(new SkillRowViewModel { SkillName = "Production Focus", Level = 0 });
        opsGroup.Skills.Add(new SkillRowViewModel { SkillName = "Extraction Focus", Level = 0 });
        SkillGroups.Add(opsGroup);

        // Also populate flat Skills list for backward compat
        foreach (var group in SkillGroups)
        {
            foreach (var skill in group.Skills)
            {
                Skills.Add(skill);
            }
        }
    }
}
