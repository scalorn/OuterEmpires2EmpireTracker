namespace OE2EmpireTracker.Server.Storage;

/// <summary>A named capability permission scoped to a faction.</summary>
public class FactionCapability
{
    public string UUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>A clearance level defining tiered data visibility within a faction.</summary>
public class FactionClearanceLevel
{
    public string UUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>A permission group that bundles capabilities and sharing rules for faction members.</summary>
public class FactionPermissionGroup
{
    public string UUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultClearanceLevelUUID { get; set; } = string.Empty;
}

/// <summary>Junction linking a faction permission group to a granted capability.</summary>
public class FactionGroupCapability
{
    public string GroupUUID { get; set; } = string.Empty;
    public string CapabilityUUID { get; set; } = string.Empty;
}

/// <summary>A sharing rule template within a faction group that gates data visibility by clearance.</summary>
public class FactionGroupSharingRule
{
    public string UUID { get; set; } = string.Empty;
    public string GroupUUID { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public string? EntityUUID { get; set; }
    public string MinClearanceLevelUUID { get; set; } = string.Empty;
}

/// <summary>Per-member permission assignment linking a character to a group and clearance level within a faction.</summary>
public class FactionMemberPermissions
{
    public string CharacterUUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public string? GroupUUID { get; set; }
    public string ClearanceLevelUUID { get; set; } = string.Empty;
}

/// <summary>An individual capability grant to a faction member outside of group membership.</summary>
public class FactionMemberCapability
{
    public string CharacterUUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public string CapabilityUUID { get; set; } = string.Empty;
}

/// <summary>Identifies whether a grantee is a character or a faction.</summary>
public enum GranteeType
{
    /// <summary>The grantee is an individual character.</summary>
    Character,

    /// <summary>The grantee is an entire faction (all members receive access).</summary>
    Faction
}

/// <summary>A named capability permission scoped to a character.</summary>
public class CharacterCapability
{
    public string UUID { get; set; } = string.Empty;
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>A clearance level defining tiered data visibility within a character's shared data.</summary>
public class CharacterClearanceLevel
{
    public string UUID { get; set; } = string.Empty;
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>A permission group that bundles capabilities and sharing rules for a character's grantees.</summary>
public class CharacterPermissionGroup
{
    public string UUID { get; set; } = string.Empty;
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultClearanceLevelUUID { get; set; } = string.Empty;
}

/// <summary>Junction linking a character permission group to a granted capability.</summary>
public class CharacterGroupCapability
{
    public string GroupUUID { get; set; } = string.Empty;
    public string CapabilityUUID { get; set; } = string.Empty;
}

/// <summary>A sharing rule within a character group that defines what data the character shares outward.</summary>
public class CharacterGroupSharingRule
{
    public string UUID { get; set; } = string.Empty;
    public string GroupUUID { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public string? EntityUUID { get; set; }
}

/// <summary>Per-grantee permission assignment linking a character or faction to a group and clearance level.</summary>
public class CharacterGranteePermissions
{
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public GranteeType GranteeType { get; set; }
    public string GranteeUUID { get; set; } = string.Empty;
    public string? GroupUUID { get; set; }
    public string? ClearanceLevelUUID { get; set; }
}

/// <summary>An individual capability grant to a grantee outside of group membership.</summary>
public class CharacterGranteeCapability
{
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public GranteeType GranteeType { get; set; }
    public string GranteeUUID { get; set; } = string.Empty;
    public string CapabilityUUID { get; set; } = string.Empty;
}

/// <summary>A private intel comment about an external character, shareable with factions.</summary>
public class IntelComment
{
    public string UUID { get; set; } = string.Empty;
    public string TargetCharacterUUID { get; set; } = string.Empty;
    public string SubmitterCharacterUUID { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

/// <summary>Junction linking an intel comment to a faction it has been shared with, including classification state.</summary>
public class IntelCommentFactionShare
{
    public string UUID { get; set; } = string.Empty;
    public string IntelCommentUUID { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;

    /// <summary>The clearance level assigned during classification. Null means pending review/unclassified.</summary>
    public string? ClassificationLevelUUID { get; set; }

    /// <summary>The character who classified this comment. Null until reviewed.</summary>
    public string? ClassifiedByCharacterUUID { get; set; }

    public DateTime SharedUtc { get; set; }

    /// <summary>When the comment was classified. Null until classified.</summary>
    public DateTime? ClassifiedUtc { get; set; }
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
    ClearanceChanged
}

/// <summary>An append-only audit log entry recording a permission change.</summary>
public class PermissionAuditEntry
{
    public string UUID { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ActorCharacterUUID { get; set; } = string.Empty;
    public string TargetCharacterUUID { get; set; } = string.Empty;
    public PermissionActionType ActionType { get; set; }
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
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
    SupplyChains
}
