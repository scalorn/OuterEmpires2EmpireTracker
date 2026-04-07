using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a PlayerProfile data object and exposes typed properties,
    /// hiding all direct data access from the UI layer.
    /// </summary>
    public class PlayerProfileViewModel
    {
        private readonly PlayerContext _playerContext;
        private PlayerProfile _profile;

        public PlayerProfile Data => _profile;

        public PlayerProfileViewModel(PlayerProfile profile, PlayerContext playerContext)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // -----------------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------------

        public string Name
        {
            get => _profile.Name;
            set => _profile.Name = value;
        }

        public string Faction
        {
            get => _profile.Faction;
            set => _profile.Faction = value;
        }

        public decimal TotalCredits
        {
            get => _profile.TotalCredits;
            set => _profile.TotalCredits = value;
        }

        public int SkillPoints
        {
            get => _profile.SkillPoints;
            set => _profile.SkillPoints = value;
        }

        // -----------------------------------------------------------------------
        // Ranks
        // -----------------------------------------------------------------------

        public PlayerRank PublicRank => _profile.Public;
        public PlayerRank PrivateRank => _profile.Private;
        public PlayerRank MilitaryRank => _profile.Military;

        // -----------------------------------------------------------------------
        // Skill groups
        // -----------------------------------------------------------------------

        public bool GetSkillGroup(SkillGroupName group) => _profile.GetSkillGroup(group);

        public void SetSkillGroup(SkillGroupName group, bool value) => _profile.SetSkillGroup(group, value);

        // -----------------------------------------------------------------------
        // Skills
        // -----------------------------------------------------------------------

        public PlayerSkill GetSkill(SkillName skill) => _profile.GetSkill(skill);

        public bool IsAnySkillTraining()
        {
            foreach (var entry in _profile.Skills)
            {
                if (entry.Value.TrainingStarted) return true;
            }
            return false;
        }

        // -----------------------------------------------------------------------
        // Profile list
        // -----------------------------------------------------------------------

        public IReadOnlyList<PlayerProfile> GetFilteredProfiles(string nameFilter)
        {
            return _playerContext.playerProfileList
                .Where(p => string.IsNullOrEmpty(nameFilter) ||
                            p.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList()
                .AsReadOnly();
        }

        // -----------------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------------

        public void Save()
        {
            if (string.IsNullOrEmpty(_profile.UUID))
            {
                _profile.UUID = Guid.NewGuid().ToString();
                _playerContext.playerProfileList.Add(_profile);
            }
            _playerContext.writeContext();
            _playerContext.OnPlayerProfilesChanged();
            _playerContext.OnPlayerProfileDataChanged(_profile.UUID);
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(_profile.UUID)) return;
            string deletedUUID = _profile.UUID;
            _playerContext.playerProfileList.Remove(_profile);

            // Cascade delete: remove all data owned by this player
            _playerContext.CascadeDeletePlayer(deletedUUID);

            _playerContext.writeContext();
            _playerContext.OnPlayerProfilesChanged();
            _playerContext.OnPlayerProfileDataChanged(deletedUUID);
        }

        /// <summary>
        /// Resets the ViewModel to point at a new blank profile.
        /// </summary>
        public void Reset()
        {
            _profile = new PlayerProfile();
        }

        /// <summary>
        /// Switches the ViewModel to point at a different profile.
        /// </summary>
        public void SelectProfile(PlayerProfile profile)
        {
            _profile = profile ?? new PlayerProfile();
        }
    }
}
