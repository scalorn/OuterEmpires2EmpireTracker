namespace OE2EmpireTracker.Server.Storage;

/// <summary>Token role levels.</summary>
public enum TokenRole
{
    Owner,
    FactionLeader,
    Character,
}

/// <summary>Rate limit configuration per token.</summary>
public class RateLimitConfig
{
    public int RequestsPerMinute { get; set; } = 60;
}

/// <summary>API token stored server-side (hash only, never plaintext).</summary>
public class ApiToken
{
    public string Id { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string? CharacterUUID { get; set; }
    public TokenRole Role { get; set; }
    public string? FactionUUID { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUsedUtc { get; set; }
    public bool IsRevoked { get; set; }
    public RateLimitConfig RateLimits { get; set; } = new RateLimitConfig();
}

/// <summary>Membership action type.</summary>
public enum MembershipActionType
{
    JoinRequest,
    Invitation,
}

/// <summary>Pending faction membership request or invitation.</summary>
public class MembershipAction
{
    public string Id { get; set; } = string.Empty;
    public string FactionUUID { get; set; } = string.Empty;
    public string CharacterUUID { get; set; } = string.Empty;
    public MembershipActionType Type { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

/// <summary>Sharing target type.</summary>
public enum SharingTargetType
{
    Faction,
    Character,
}

/// <summary>A single sharing rule granting access to data.</summary>
public class SharingRule
{
    public string Id { get; set; } = string.Empty;
    public string OwnerCharacterUUID { get; set; } = string.Empty;
    public string TargetUUID { get; set; } = string.Empty;
    public SharingTargetType TargetType { get; set; }
    public string? DataType { get; set; }
    public string? EntityUUID { get; set; }
}

/// <summary>Character preferences stored server-side.</summary>
public class CharacterPreferences
{
    public string CharacterUUID { get; set; } = string.Empty;
    public bool ServerProcessing { get; set; }
}

/// <summary>Metadata attached to entities for conflict resolution.</summary>
public class EntityMetadata
{
    public DateTime LastModifiedUtc { get; set; }
    public string? ModifiedByTokenId { get; set; }
}

/// <summary>Server-side faction with leadership tracking.</summary>
public class ServerFaction
{
    public string UUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> LeaderCharacterUUIDs { get; set; } = new List<string>();
    public EntityMetadata Metadata { get; set; } = new EntityMetadata();
}

/// <summary>Server-side character with metadata.</summary>
public class ServerCharacter
{
    public string UUID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FactionUUID { get; set; }
    public EntityMetadata Metadata { get; set; } = new EntityMetadata();
}
