using System;

#nullable enable

namespace OE2EmpireTracker.Common.Models
{
    /// <summary>Identifies whether a grantee is a character or a faction.</summary>
    public enum GranteeType
    {
        /// <summary>The grantee is an individual character.</summary>
        Character,

        /// <summary>The grantee is an entire faction (all members receive access).</summary>
        Faction,
    }

    /// <summary>The type of permission change recorded in an audit entry.</summary>
    public enum PermissionActionType
    {
        /// <summary>A capability was granted to a character.</summary>
        CapabilityGranted,

        /// <summary>A capability was revoked from a character.</summary>
        CapabilityRevoked,

        /// <summary>A character was assigned to a permission group.</summary>
        GroupAssigned,

        /// <summary>A character was removed from a permission group.</summary>
        GroupRemoved,

        /// <summary>A character's clearance level was changed.</summary>
        ClearanceChanged,
    }

    /// <summary>Categorizes the type of data governed by sharing rules.</summary>
    public enum DataType
    {
        /// <summary>Blueprint data.</summary>
        Blueprints,

        /// <summary>Colony data.</summary>
        Colonies,

        /// <summary>Survey data.</summary>
        Surveys,

        /// <summary>Delivery route data.</summary>
        DeliveryRoutes,

        /// <summary>Delivery plan data.</summary>
        DeliveryPlans,

        /// <summary>Build plan data.</summary>
        BuildPlans,

        /// <summary>Ship data.</summary>
        Ships,

        /// <summary>Ship template data.</summary>
        ShipTemplates,

        /// <summary>Station data.</summary>
        Stations,

        /// <summary>Asteroid data.</summary>
        Asteroids,

        /// <summary>Market listing data.</summary>
        MarketListings,

        /// <summary>Market transaction data.</summary>
        MarketTransactions,

        /// <summary>Pricing plan data.</summary>
        PricingPlans,

        /// <summary>Stock plan data.</summary>
        StockPlans,

        /// <summary>Stock profile data.</summary>
        StockProfiles,

        /// <summary>Supply chain data.</summary>
        SupplyChains,
    }

    /// <summary>A named capability permission scoped to a faction.</summary>
    public class FactionCapability
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>A clearance level defining tiered data visibility within a faction.</summary>
    public class FactionClearanceLevel
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the clearance level number.</summary>
        public int Level { get; set; }

        /// <summary>Gets or sets the level name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the level description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>A permission group that bundles capabilities and sharing rules for faction members.</summary>
    public class FactionPermissionGroup
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the group name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the group description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the default clearance level UUID.</summary>
        public string DefaultClearanceLevelUUID { get; set; } = string.Empty;
    }

    /// <summary>Junction linking a faction permission group to a granted capability.</summary>
    public class FactionGroupCapability
    {
        /// <summary>Gets or sets the group UUID.</summary>
        public string GroupUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability UUID.</summary>
        public string CapabilityUUID { get; set; } = string.Empty;
    }

    /// <summary>A sharing rule template within a faction group that gates data visibility by clearance.</summary>
    public class FactionGroupSharingRule
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the group UUID.</summary>
        public string GroupUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional data type filter.</summary>
        public string? DataType { get; set; }

        /// <summary>Gets or sets the optional entity UUID filter.</summary>
        public string? EntityUUID { get; set; }

        /// <summary>Gets or sets the minimum clearance level UUID.</summary>
        public string MinClearanceLevelUUID { get; set; } = string.Empty;
    }

    /// <summary>An individual capability grant to a faction member outside of group membership.</summary>
    public class FactionMemberCapability
    {
        /// <summary>Gets or sets the character UUID.</summary>
        public string CharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability UUID.</summary>
        public string CapabilityUUID { get; set; } = string.Empty;
    }

    /// <summary>Per-member permission assignment linking a character to a group and clearance level within a faction.</summary>
    public class FactionMemberPermissions
    {
        /// <summary>Gets or sets the character UUID.</summary>
        public string CharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional group UUID.</summary>
        public string? GroupUUID { get; set; }

        /// <summary>Gets or sets the clearance level UUID.</summary>
        public string ClearanceLevelUUID { get; set; } = string.Empty;
    }

    /// <summary>A named capability permission scoped to a character.</summary>
    public class CharacterCapability
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>A clearance level defining tiered data visibility within a character's shared data.</summary>
    public class CharacterClearanceLevel
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the clearance level number.</summary>
        public int Level { get; set; }

        /// <summary>Gets or sets the level name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the level description.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>A permission group that bundles capabilities and sharing rules for a character's grantees.</summary>
    public class CharacterPermissionGroup
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the group name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the group description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the default clearance level UUID.</summary>
        public string DefaultClearanceLevelUUID { get; set; } = string.Empty;
    }

    /// <summary>Junction linking a character permission group to a granted capability.</summary>
    public class CharacterGroupCapability
    {
        /// <summary>Gets or sets the group UUID.</summary>
        public string GroupUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability UUID.</summary>
        public string CapabilityUUID { get; set; } = string.Empty;
    }

    /// <summary>A sharing rule within a character group that defines what data the character shares outward.</summary>
    public class CharacterGroupSharingRule
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the group UUID.</summary>
        public string GroupUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional data type filter.</summary>
        public string? DataType { get; set; }

        /// <summary>Gets or sets the optional entity UUID filter.</summary>
        public string? EntityUUID { get; set; }
    }

    /// <summary>Per-grantee permission assignment linking a character or faction to a group and clearance level.</summary>
    public class CharacterGranteePermissions
    {
        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the grantee type.</summary>
        public GranteeType GranteeType { get; set; }

        /// <summary>Gets or sets the grantee UUID.</summary>
        public string GranteeUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional group UUID.</summary>
        public string? GroupUUID { get; set; }

        /// <summary>Gets or sets the optional clearance level UUID.</summary>
        public string? ClearanceLevelUUID { get; set; }
    }

    /// <summary>An individual capability grant to a grantee outside of group membership.</summary>
    public class CharacterGranteeCapability
    {
        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the grantee type.</summary>
        public GranteeType GranteeType { get; set; }

        /// <summary>Gets or sets the grantee UUID.</summary>
        public string GranteeUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the capability UUID.</summary>
        public string CapabilityUUID { get; set; } = string.Empty;
    }

    /// <summary>A private intel comment about an external character, shareable with factions.</summary>
    public class IntelComment
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the target character UUID.</summary>
        public string TargetCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the submitter character UUID.</summary>
        public string SubmitterCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the comment text.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Gets or sets the creation timestamp.</summary>
        public DateTime CreatedUtc { get; set; }
    }

    /// <summary>Junction linking an intel comment to a faction it has been shared with, including classification state.</summary>
    public class IntelCommentFactionShare
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the intel comment UUID.</summary>
        public string IntelCommentUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the clearance level assigned during classification. Null means pending review.</summary>
        public string? ClassificationLevelUUID { get; set; }

        /// <summary>Gets or sets the character who classified this comment. Null until reviewed.</summary>
        public string? ClassifiedByCharacterUUID { get; set; }

        /// <summary>Gets or sets the share timestamp.</summary>
        public DateTime SharedUtc { get; set; }

        /// <summary>Gets or sets when the comment was classified. Null until classified.</summary>
        public DateTime? ClassifiedUtc { get; set; }
    }

    /// <summary>An append-only audit log entry recording a permission change.</summary>
    public class PermissionAuditEntry
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the event timestamp.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Gets or sets the actor character UUID.</summary>
        public string ActorCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the target character UUID.</summary>
        public string TargetCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the action type.</summary>
        public PermissionActionType ActionType { get; set; }

        /// <summary>Gets or sets the old value before the change.</summary>
        public string OldValue { get; set; } = string.Empty;

        /// <summary>Gets or sets the new value after the change.</summary>
        public string NewValue { get; set; } = string.Empty;
    }
}
