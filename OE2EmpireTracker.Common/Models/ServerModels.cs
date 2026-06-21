using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Common.Models
{
    /// <summary>
    /// Token role levels.
    /// </summary>
    public enum TokenRole
    {
        /// <summary>Owner-level access.</summary>
        Owner,

        /// <summary>Faction leader access.</summary>
        FactionLeader,

        /// <summary>Character-level access.</summary>
        Character,
    }

    /// <summary>
    /// Membership action type.
    /// </summary>
    public enum MembershipActionType
    {
        /// <summary>A request to join a faction.</summary>
        JoinRequest,

        /// <summary>An invitation to join a faction.</summary>
        Invitation,
    }

    /// <summary>
    /// Sharing target type.
    /// </summary>
    public enum SharingTargetType
    {
        /// <summary>Shared with a faction.</summary>
        Faction,

        /// <summary>Shared with a specific character.</summary>
        Character,

        /// <summary>Shared publicly.</summary>
        Public,
    }

    /// <summary>
    /// Rate limit configuration per token.
    /// </summary>
    public class RateLimitConfig
    {
        /// <summary>
        /// Gets or sets the maximum requests per minute. Default is 300 (5 TPS sliding window).
        /// </summary>
        public int RequestsPerMinute { get; set; } = 300;
    }

    /// <summary>
    /// API token stored server-side (hash only, never plaintext).
    /// </summary>
    public class ApiToken
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the token hash.</summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>Gets or sets the associated character UUID.</summary>
        public string CharacterUUID { get; set; }

        /// <summary>Gets or sets the token role.</summary>
        public TokenRole Role { get; set; }

        /// <summary>Gets or sets the associated faction UUID.</summary>
        public string FactionUUID { get; set; }

        /// <summary>Gets or sets the creation timestamp.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>Gets or sets the last used timestamp.</summary>
        public DateTime? LastUsedUtc { get; set; }

        /// <summary>Gets or sets a value indicating whether the token is revoked.</summary>
        public bool IsRevoked { get; set; }

        /// <summary>Gets or sets the rate limit configuration.</summary>
        public RateLimitConfig RateLimits { get; set; } = new RateLimitConfig();
    }

    /// <summary>
    /// Pending faction membership request or invitation.
    /// </summary>
    public class MembershipAction
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the character UUID.</summary>
        public string CharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the action type.</summary>
        public MembershipActionType Type { get; set; }

        /// <summary>Gets or sets the creation timestamp.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>Gets or sets the expiration timestamp.</summary>
        public DateTime ExpiresUtc { get; set; }
    }

    /// <summary>
    /// A single sharing rule granting access to data.
    /// </summary>
    public class SharingRule
    {
        /// <summary>Gets or sets the unique identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the owner character UUID.</summary>
        public string OwnerCharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the target UUID.</summary>
        public string TargetUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the sharing target type.</summary>
        public SharingTargetType TargetType { get; set; }

        /// <summary>Gets or sets the optional data type filter.</summary>
        public string DataType { get; set; }

        /// <summary>Gets or sets the optional entity UUID filter.</summary>
        public string EntityUUID { get; set; }
    }

    /// <summary>
    /// Character preferences stored server-side.
    /// </summary>
    public class CharacterPreferences
    {
        /// <summary>Gets or sets the character UUID.</summary>
        public string CharacterUUID { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether server processing is enabled.</summary>
        public bool ServerProcessing { get; set; }
    }

    /// <summary>
    /// Metadata attached to entities for conflict resolution.
    /// </summary>
    public class EntityMetadata
    {
        /// <summary>Gets or sets the last modified timestamp.</summary>
        public DateTime LastModifiedUtc { get; set; }

        /// <summary>Gets or sets the token ID that last modified the entity.</summary>
        public string ModifiedByTokenId { get; set; }
    }

    /// <summary>
    /// Server-side faction with leadership tracking.
    /// </summary>
    public class ServerFaction
    {
        /// <summary>Gets or sets the faction UUID.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the leader character UUIDs.</summary>
        public List<string> LeaderCharacterUUIDs { get; set; } = new List<string>();

        /// <summary>Gets or sets the entity metadata.</summary>
        public EntityMetadata Metadata { get; set; } = new EntityMetadata();
    }

    /// <summary>
    /// Server-side character with metadata.
    /// </summary>
    public class ServerCharacter
    {
        /// <summary>Gets or sets the character UUID.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the character name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the faction UUID.</summary>
        public string FactionUUID { get; set; }

        /// <summary>Gets or sets the entity metadata.</summary>
        public EntityMetadata Metadata { get; set; } = new EntityMetadata();
    }

    /// <summary>
    /// Public colony summary (name and size only).
    /// </summary>
    public class ColonySummary
    {
        /// <summary>Gets or sets the colony name.</summary>
        public string ColonyName { get; set; } = string.Empty;

        /// <summary>Gets or sets the colony size.</summary>
        public int Size { get; set; }

        /// <summary>Gets or sets the planet name.</summary>
        public string PlanetName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Public asteroid summary for system view.
    /// </summary>
    public class AsteroidSummary
    {
        /// <summary>Gets or sets the asteroid UUID.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the asteroid name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the star system ID.</summary>
        public int SystemId { get; set; }
    }

    /// <summary>
    /// Public blueprint summary — excludes private fields like OwnerUUID and Resources.
    /// </summary>
    public class BlueprintSummary
    {
        /// <summary>Gets or sets the blueprint UUID.</summary>
        public string UUID { get; set; } = string.Empty;

        /// <summary>Gets or sets the blueprint name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the blueprint nickname.</summary>
        public string NickName { get; set; } = string.Empty;

        /// <summary>Gets or sets the blueprint type.</summary>
        public string BluePrintType { get; set; } = string.Empty;

        /// <summary>Gets or sets the tech level.</summary>
        public string TechLevel { get; set; } = string.Empty;

        /// <summary>Gets or sets the evolution level.</summary>
        public int Evolution { get; set; }

        /// <summary>Gets or sets the class level.</summary>
        public int Class { get; set; }

        /// <summary>Gets or sets the description.</summary>
        public string Description { get; set; } = string.Empty;
    }
}
