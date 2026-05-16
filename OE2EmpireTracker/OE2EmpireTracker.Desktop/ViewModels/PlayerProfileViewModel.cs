using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Parsers;
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
/// ViewModel for the Player Profile document tab.
/// Shows profiles with skills and ranks.
/// </summary>
public sealed partial class PlayerProfileViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ProfileRowViewModel? _selectedProfile;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    public PlayerProfileViewModel()
    {
        Title = "Player Profile";
        LoadData();
    }

    public ObservableCollection<ProfileRowViewModel> Profiles { get; } = new();

    public ObservableCollection<SkillRowViewModel> Skills { get; } = new();

    /// <summary>
    /// Imports a player profile from clipboard HTML data.
    /// Extracts the HTML fragment, parses it, and either updates an existing profile
    /// (matched by name) or creates a new one.
    /// </summary>
    [RelayCommand]
    private async Task ImportClipboardAsync()
    {
        var clipboardService = App.Services?.GetService(typeof(IClipboardService)) as IClipboardService;
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var loggerFactory = App.Services?.GetService(typeof(ILoggerFactory)) as ILoggerFactory;

        if (clipboardService is null || dataService is null || loggerFactory is null)
        {
            ImportStatus = "Services not available";
            return;
        }

        string? html = await clipboardService.GetHtmlAsync();
        if (string.IsNullOrEmpty(html))
        {
            ImportStatus = "No HTML on clipboard";
            return;
        }

        string fragment = HtmlClipboardHelper.ExtractHtmlFragment(html);
        if (string.IsNullOrEmpty(fragment))
        {
            ImportStatus = "Could not extract HTML fragment";
            return;
        }

        var parser = new PlayerProfileParser(loggerFactory.CreateLogger<PlayerProfileParser>());
        var parsed = parser.ParseHtml(fragment);
        if (parsed is null)
        {
            ImportStatus = "Failed to parse profile HTML";
            return;
        }

        // Match existing profile by name, or create new
        var existing = dataService.PlayerProfiles
            .FirstOrDefault(p => string.Equals(
                p.Name, parsed.Name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            // Update existing profile
            existing.Faction = parsed.Faction;
            existing.TotalCredits = parsed.TotalCredits;
            existing.SkillPoints = parsed.SkillPoints;
            existing.CitizenId = parsed.CitizenId;
            existing.RegistrationDate = parsed.RegistrationDate;
            existing.ActiveTime = parsed.ActiveTime;
            existing.Public.Rank = parsed.Public.Rank;
            existing.Public.CurrentXP = parsed.Public.CurrentXP;
            existing.Public.NextXP = parsed.Public.NextXP;
            existing.Public.Title = parsed.Public.Title;
            existing.Private.Rank = parsed.Private.Rank;
            existing.Private.CurrentXP = parsed.Private.CurrentXP;
            existing.Private.NextXP = parsed.Private.NextXP;
            existing.Private.Title = parsed.Private.Title;
            existing.Military.Rank = parsed.Military.Rank;
            existing.Military.CurrentXP = parsed.Military.CurrentXP;
            existing.Military.NextXP = parsed.Military.NextXP;
            existing.Military.Title = parsed.Military.Title;

            // Merge skills
            foreach (var kvp in parsed.Skills)
            {
                var skill = existing.GetSkill(kvp.Key);
                skill.Level = kvp.Value.Level;
                skill.TrainingStarted = kvp.Value.TrainingStarted;
            }

            dataService.IsDirty = true;
            dataService.OnPlayerProfileDataChanged(existing.UUID);
            dataService.WriteContext();
            ImportStatus = $"Updated profile: {existing.Name}";
        }
        else
        {
            // Create new profile
            parsed.UUID = Guid.NewGuid().ToString();
            dataService.AddPlayerProfile(parsed);
            dataService.OnPlayerProfileDataChanged(parsed.UUID);
            dataService.WriteContext();
            ImportStatus = $"Created profile: {parsed.Name}";
        }

        RefreshProfiles();
    }

    partial void OnSelectedProfileChanged(ProfileRowViewModel? value)
    {
        LoadSkillsForProfile(value);
    }

    private void RefreshProfiles()
    {
        Profiles.Clear();
        Skills.Clear();

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
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

        if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
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
