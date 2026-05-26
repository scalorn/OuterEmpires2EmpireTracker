using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for player profile data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only PlayerProfileService mutates the actual entity.
    /// </summary>
    public class PlayerProfileViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        private ReadOnlyPlayerProfile _original;

        private string _uuid;
        private string _name = string.Empty;
        private string _faction = string.Empty;
        private decimal _totalCredits;
        private int _skillPoints;
        private string _citizenId = string.Empty;
        private string _registrationDate = string.Empty;
        private string _activeTime = string.Empty;
        private int _characterId;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private int _activeTimeMinutes;

        private LocalRankData _publicRank = new LocalRankData();
        private LocalRankData _privateRank = new LocalRankData();
        private LocalRankData _militaryRank = new LocalRankData();
        private Dictionary<string, LocalSkillData> _skills = new Dictionary<string, LocalSkillData>();
        private Dictionary<string, bool> _skillGroups = new Dictionary<string, bool>();

        public PlayerProfileViewModel(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>Gets a value indicating whether this is a new profile not yet saved.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets the profile UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the original snapshot this edit buffer was loaded from.</summary>
        public ReadOnlyPlayerProfile Original => _original;

        public string Name { get => _name; set => _name = value; }

        public string Faction { get => _faction; set => _faction = value; }

        public decimal TotalCredits { get => _totalCredits; set => _totalCredits = value; }

        public int SkillPoints { get => _skillPoints; set => _skillPoints = value; }

        public string CitizenId { get => _citizenId; set => _citizenId = value; }

        public string RegistrationDate { get => _registrationDate; set => _registrationDate = value; }

        public string ActiveTime { get => _activeTime; set => _activeTime = value; }

        public int CharacterId { get => _characterId; set => _characterId = value; }

        public string FirstName { get => _firstName; set => _firstName = value; }

        public string LastName { get => _lastName; set => _lastName = value; }

        public int ActiveTimeMinutes { get => _activeTimeMinutes; set => _activeTimeMinutes = value; }

        public LocalRankData PublicRank => _publicRank;

        public LocalRankData PrivateRank => _privateRank;

        public LocalRankData MilitaryRank => _militaryRank;

        /// <summary>
        /// Gets a value indicating whether any local field differs from the original snapshot.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name)
                        || !string.IsNullOrEmpty(_faction)
                        || _totalCredits != 0
                        || _skillPoints != 0;
                }

                if (_name != _original.Name) return true;
                if (_faction != _original.Faction) return true;
                if (_totalCredits != _original.TotalCredits) return true;
                if (_skillPoints != _original.SkillPoints) return true;
                if (_citizenId != _original.CitizenId) return true;
                if (_registrationDate != _original.RegistrationDate) return true;
                if (_activeTime != _original.ActiveTime) return true;
                if (_characterId != _original.CharacterId) return true;
                if (_firstName != _original.FirstName) return true;
                if (_lastName != _original.LastName) return true;
                if (_activeTimeMinutes != _original.ActiveTimeMinutes) return true;

                if (IsRankDirty(_publicRank, _original.Public)) return true;
                if (IsRankDirty(_privateRank, _original.Private)) return true;
                if (IsRankDirty(_militaryRank, _original.Military)) return true;

                if (IsSkillsDirty()) return true;
                if (IsSkillGroupsDirty()) return true;

                return false;
            }
        }

        /// <summary>
        /// Converts total minutes to a human-readable duration string.
        /// Format: "{D}d {H}h {M}m" with leading-zero omission.
        /// Returns "\u2014" (em-dash) for zero or negative values.
        /// </summary>
        /// <param name="totalMinutes">Total minutes to format.</param>
        /// <returns>Formatted duration string or em-dash.</returns>
        public static string FormatActiveTime(int totalMinutes)
        {
            if (totalMinutes <= 0)
            {
                return "\u2014";
            }

            int days = totalMinutes / 1440;
            int hours = (totalMinutes % 1440) / 60;
            int minutes = totalMinutes % 60;

            if (days > 0)
            {
                return $"{days}d {hours}h {minutes}m";
            }

            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }

            return $"{minutes}m";
        }

        public LocalSkillData GetSkill(string skillName)
        {
            if (!_skills.ContainsKey(skillName))
            {
                _skills[skillName] = new LocalSkillData();
            }

            return _skills[skillName];
        }

        public LocalSkillData GetSkill(SkillName skill) => GetSkill(skill.ToDisplayName());

        public bool GetSkillGroup(SkillGroupName group)
        {
            string key = group.ToDisplayName();
            return _skillGroups.ContainsKey(key) && _skillGroups[key];
        }

        public void SetSkillGroup(SkillGroupName group, bool value)
        {
            _skillGroups[group.ToDisplayName()] = value;
        }

        public bool IsAnySkillTraining()
        {
            return _skills.Values.Any(s => s.TrainingStarted);
        }

        public IReadOnlyList<ReadOnlyPlayerProfile> GetFilteredProfiles(string nameFilter)
        {
            return _playerContext.GetReadOnlyPlayerProfileList()
                .Where(p => string.IsNullOrEmpty(nameFilter) ||
                            p.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Loads field values from a ReadOnlyPlayerProfile snapshot.
        /// Retains the original for dirty comparison.
        /// </summary>
        public void LoadFrom(ReadOnlyPlayerProfile ro)
        {
            if (ro == null)
            {
                throw new ArgumentNullException(nameof(ro));
            }

            _original = ro;
            _uuid = ro.UUID;
            _name = ro.Name;
            _faction = ro.Faction;
            _totalCredits = ro.TotalCredits;
            _skillPoints = ro.SkillPoints;
            _citizenId = ro.CitizenId;
            _registrationDate = ro.RegistrationDate;
            _activeTime = ro.ActiveTime;
            _characterId = ro.CharacterId;
            _firstName = ro.FirstName;
            _lastName = ro.LastName;
            _activeTimeMinutes = ro.ActiveTimeMinutes;

            CopyRank(_publicRank, ro.Public);
            CopyRank(_privateRank, ro.Private);
            CopyRank(_militaryRank, ro.Military);

            _skills.Clear();
            foreach (var kvp in ro.Skills)
            {
                var roSkill = kvp.Value;
                _skills[kvp.Key] = new LocalSkillData
                {
                    Level = roSkill.Level,
                    TrainingStarted = roSkill.TrainingStarted,
                    CompletionStartTime = roSkill.CompletionStartTime,
                    CompletionEndTime = roSkill.CompletionEndTime,
                    EffectDescription = roSkill.EffectDescription,
                    AmountPerLevel = roSkill.AmountPerLevel,
                    TrainingPercentageComplete = roSkill.TrainingPercentageComplete,
                    RemainingMinutes = roSkill.RemainingMinutes,
                };
            }

            _skillGroups.Clear();
            foreach (SkillGroupName group in Enum.GetValues(typeof(SkillGroupName)))
            {
                _skillGroups[group.ToDisplayName()] = ro.GetSkillGroup(group);
            }
        }

        /// <summary>
        /// Resets to empty state for a new profile.
        /// </summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _name = string.Empty;
            _faction = string.Empty;
            _totalCredits = 0;
            _skillPoints = 0;
            _citizenId = string.Empty;
            _registrationDate = string.Empty;
            _activeTime = string.Empty;
            _characterId = 0;
            _firstName = string.Empty;
            _lastName = string.Empty;
            _activeTimeMinutes = 0;
            _publicRank = new LocalRankData();
            _privateRank = new LocalRankData();
            _militaryRank = new LocalRankData();
            _skills.Clear();
            _skillGroups.Clear();
        }

        /// <summary>
        /// Builds an update request carrying both the original snapshot
        /// and the current local state.
        /// </summary>
        public PlayerProfileUpdateRequest BuildUpdateRequest()
        {
            return new PlayerProfileUpdateRequest
            {
                Original = _original,
                Name = _name,
                Faction = _faction,
                TotalCredits = _totalCredits,
                SkillPoints = _skillPoints,
                CitizenId = _citizenId,
                RegistrationDate = _registrationDate,
                ActiveTime = _activeTime,
                PublicRank = _publicRank.Rank,
                PublicCurrentXp = _publicRank.CurrentXp,
                PublicXpToNextLevel = _publicRank.XpToNextLevel,
                PublicRankName = _publicRank.RankName,
                PrivateRank = _privateRank.Rank,
                PrivateCurrentXp = _privateRank.CurrentXp,
                PrivateXpToNextLevel = _privateRank.XpToNextLevel,
                PrivateRankName = _privateRank.RankName,
                MilitaryRank = _militaryRank.Rank,
                MilitaryCurrentXp = _militaryRank.CurrentXp,
                MilitaryXpToNextLevel = _militaryRank.XpToNextLevel,
                MilitaryRankName = _militaryRank.RankName,
                Skills = _skills.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SkillUpdateData
                    {
                        Level = kvp.Value.Level,
                        TrainingStarted = kvp.Value.TrainingStarted,
                        CompletionStartTime = kvp.Value.CompletionStartTime,
                        CompletionEndTime = kvp.Value.CompletionEndTime,
                    }),
                SkillGroups = new Dictionary<string, bool>(_skillGroups),
            };
        }

        /// <summary>
        /// Builds a create request for a new profile.
        /// </summary>
        public PlayerProfileCreateRequest BuildCreateRequest()
        {
            return new PlayerProfileCreateRequest
            {
                Name = _name,
                Faction = _faction,
                TotalCredits = _totalCredits,
                SkillPoints = _skillPoints,
                CitizenId = _citizenId,
                RegistrationDate = _registrationDate,
                ActiveTime = _activeTime,
                PublicRank = _publicRank.Rank,
                PublicCurrentXp = _publicRank.CurrentXp,
                PublicXpToNextLevel = _publicRank.XpToNextLevel,
                PublicRankName = _publicRank.RankName,
                PrivateRank = _privateRank.Rank,
                PrivateCurrentXp = _privateRank.CurrentXp,
                PrivateXpToNextLevel = _privateRank.XpToNextLevel,
                PrivateRankName = _privateRank.RankName,
                MilitaryRank = _militaryRank.Rank,
                MilitaryCurrentXp = _militaryRank.CurrentXp,
                MilitaryXpToNextLevel = _militaryRank.XpToNextLevel,
                MilitaryRankName = _militaryRank.RankName,
                Skills = _skills.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new SkillUpdateData
                    {
                        Level = kvp.Value.Level,
                        TrainingStarted = kvp.Value.TrainingStarted,
                        CompletionStartTime = kvp.Value.CompletionStartTime,
                        CompletionEndTime = kvp.Value.CompletionEndTime,
                    }),
                SkillGroups = new Dictionary<string, bool>(_skillGroups),
            };
        }

        private static void CopyRank(LocalRankData local, ReadOnlyPlayerRank ro)
        {
            local.Rank = ro.Rank;
            local.CurrentXp = ro.CurrentXp;
            local.XpToNextLevel = ro.XpToNextLevel;
            local.RankName = ro.RankName;
        }

        private static bool IsRankDirty(LocalRankData local, ReadOnlyPlayerRank original)
        {
            return local.Rank != original.Rank
                || local.CurrentXp != original.CurrentXp
                || local.XpToNextLevel != original.XpToNextLevel
                || local.RankName != original.RankName;
        }

        private bool IsSkillsDirty()
        {
            var originalSkills = _original.Skills;
            if (_skills.Count != originalSkills.Count)
            {
                return true;
            }

            foreach (var kvp in _skills)
            {
                if (!originalSkills.TryGetValue(kvp.Key, out var roSkill))
                {
                    return true;
                }

                if (kvp.Value.Level != roSkill.Level) return true;
                if (kvp.Value.TrainingStarted != roSkill.TrainingStarted) return true;
                if (kvp.Value.CompletionStartTime != roSkill.CompletionStartTime) return true;
                if (kvp.Value.CompletionEndTime != roSkill.CompletionEndTime) return true;
            }

            return false;
        }

        private bool IsSkillGroupsDirty()
        {
            foreach (SkillGroupName group in Enum.GetValues(typeof(SkillGroupName)))
            {
                string key = group.ToDisplayName();
                bool localVal = _skillGroups.ContainsKey(key) && _skillGroups[key];
                bool origVal = _original.GetSkillGroup(group);
                if (localVal != origVal)
                {
                    return true;
                }
            }

            return false;
        }
    }
}