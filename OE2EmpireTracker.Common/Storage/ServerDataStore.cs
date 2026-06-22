// -----------------------------------------------------------------------
// <copyright file="ServerDataStore.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// Container for server-global data persisted by <see cref="JsonSingleFileBackend"/>
    /// in a dedicated ServerData.json file alongside player data. Holds factions,
    /// characters, tokens, permissions, intel, and audit entries.
    /// </summary>
    internal class ServerDataStore
    {
        /// <summary>Gets or sets the server factions.</summary>
        public List<ServerFaction> ServerFactions { get; set; } = new List<ServerFaction>();

        /// <summary>Gets or sets the server characters.</summary>
        public List<ServerCharacter> ServerCharacters { get; set; } = new List<ServerCharacter>();

        /// <summary>Gets or sets the star systems.</summary>
        public List<StarSystem> StarSystems { get; set; } = new List<StarSystem>();

        /// <summary>Gets or sets the API tokens.</summary>
        public List<ApiToken> Tokens { get; set; } = new List<ApiToken>();

        /// <summary>Gets or sets the membership actions.</summary>
        public List<MembershipAction> MembershipActions { get; set; } = new List<MembershipAction>();

        /// <summary>Gets or sets the global key-value data store.</summary>
        public Dictionary<string, string> GlobalData { get; set; } = new Dictionary<string, string>();

        /// <summary>Gets or sets the sharing rules keyed by character UUID.</summary>
        public Dictionary<string, List<SharingRule>> SharingRules { get; set; } = new Dictionary<string, List<SharingRule>>();

        /// <summary>Gets or sets the character preferences keyed by character UUID.</summary>
        public Dictionary<string, CharacterPreferences> CharacterPreferences { get; set; } = new Dictionary<string, CharacterPreferences>();

        /// <summary>Gets or sets the faction capabilities.</summary>
        public List<FactionCapability> FactionCapabilities { get; set; } = new List<FactionCapability>();

        /// <summary>Gets or sets the faction clearance levels.</summary>
        public List<FactionClearanceLevel> FactionClearanceLevels { get; set; } = new List<FactionClearanceLevel>();

        /// <summary>Gets or sets the faction permission groups.</summary>
        public List<FactionPermissionGroup> FactionGroups { get; set; } = new List<FactionPermissionGroup>();

        /// <summary>Gets or sets the faction group capabilities.</summary>
        public List<FactionGroupCapability> FactionGroupCapabilities { get; set; } = new List<FactionGroupCapability>();

        /// <summary>Gets or sets the faction group sharing rules.</summary>
        public List<FactionGroupSharingRule> FactionGroupSharingRules { get; set; } = new List<FactionGroupSharingRule>();

        /// <summary>Gets or sets the faction member permissions.</summary>
        public List<FactionMemberPermissions> FactionMemberPermissions { get; set; } = new List<FactionMemberPermissions>();

        /// <summary>Gets or sets the faction member capabilities.</summary>
        public List<FactionMemberCapability> FactionMemberCapabilities { get; set; } = new List<FactionMemberCapability>();

        /// <summary>Gets or sets the character capabilities.</summary>
        public List<CharacterCapability> CharacterCapabilities { get; set; } = new List<CharacterCapability>();

        /// <summary>Gets or sets the character clearance levels.</summary>
        public List<CharacterClearanceLevel> CharacterClearanceLevels { get; set; } = new List<CharacterClearanceLevel>();

        /// <summary>Gets or sets the character permission groups.</summary>
        public List<CharacterPermissionGroup> CharacterGroups { get; set; } = new List<CharacterPermissionGroup>();

        /// <summary>Gets or sets the character group capabilities.</summary>
        public List<CharacterGroupCapability> CharacterGroupCapabilities { get; set; } = new List<CharacterGroupCapability>();

        /// <summary>Gets or sets the character group sharing rules.</summary>
        public List<CharacterGroupSharingRule> CharacterGroupSharingRules { get; set; } = new List<CharacterGroupSharingRule>();

        /// <summary>Gets or sets the character grantee permissions.</summary>
        public List<CharacterGranteePermissions> CharacterGranteePermissions { get; set; } = new List<CharacterGranteePermissions>();

        /// <summary>Gets or sets the character grantee capabilities.</summary>
        public List<CharacterGranteeCapability> CharacterGranteeCapabilities { get; set; } = new List<CharacterGranteeCapability>();

        /// <summary>Gets or sets the intel comments.</summary>
        public List<IntelComment> IntelComments { get; set; } = new List<IntelComment>();

        /// <summary>Gets or sets the intel comment faction shares.</summary>
        public List<IntelCommentFactionShare> IntelShares { get; set; } = new List<IntelCommentFactionShare>();

        /// <summary>Gets or sets the permission audit entries.</summary>
        public List<PermissionAuditEntry> AuditEntries { get; set; } = new List<PermissionAuditEntry>();
    }
}
