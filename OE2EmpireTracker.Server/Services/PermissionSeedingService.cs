using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Services;

/// <summary>
/// Seeds default permission entities (capabilities, clearance levels) when a faction
/// or character scope is first created.
/// </summary>
public static class PermissionSeedingService
{
    /// <summary>
    /// Creates the 9 default faction capabilities when a faction is first created.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="factionUUID">The UUID of the newly created faction.</param>
    public static async Task SeedFactionCapabilitiesAsync(IStorageBackend storage, string factionUUID)
    {
        var defaults = new (string Name, string Description)[]
        {
            ("global_data_write", "Can write to global/baseline data (normally Owner-only)"),
            ("server_processing_admin", "Can enable/disable server-side processing"),
            ("export_any_character", "Can export any character's data (normally Owner-only)"),
            ("manage_faction_members", "Can accept/reject join requests and remove members (normally Leader-only)"),
            ("classify_intel", "Can review and classify unclassified intel comments shared with the faction"),
            ("server_side_processing", "Character's colonies are processed server-side (opt-in behavior)"),
            ("real_time_push", "Character receives WebSocket push events (opt-in behavior)"),
            ("bulk_export", "Character can use the bulk export endpoint"),
            ("market_analytics", "Character can access aggregated market analytics endpoints"),
        };

        foreach (var (name, description) in defaults)
        {
            var capability = new FactionCapability
            {
                UUID = Guid.NewGuid().ToString(),
                FactionUUID = factionUUID,
                Name = name,
                Description = description,
            };

            await storage.UpsertFactionCapabilityAsync(capability);
        }
    }

    /// <summary>
    /// Creates the 5 default clearance levels when a faction is first created.
    /// Levels: 1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="factionUUID">The UUID of the newly created faction.</param>
    public static async Task SeedFactionClearanceLevelsAsync(IStorageBackend storage, string factionUUID)
    {
        var defaults = new (int Level, string Name, string Description)[]
        {
            (1, "Recruit", "New faction member with minimal access"),
            (2, "Member", "Standard faction member"),
            (3, "Officer", "Trusted member with elevated access"),
            (4, "Command", "Senior member with high-level access"),
            (5, "Leader", "Faction leadership with full access"),
        };

        foreach (var (level, name, description) in defaults)
        {
            var clearanceLevel = new FactionClearanceLevel
            {
                UUID = Guid.NewGuid().ToString(),
                FactionUUID = factionUUID,
                Level = level,
                Name = name,
                Description = description,
            };

            await storage.UpsertFactionClearanceLevelAsync(clearanceLevel);
        }
    }

    /// <summary>
    /// Creates the 5 default clearance levels when a character scope first needs them.
    /// Levels: 1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="characterUUID">The UUID of the character.</param>
    public static async Task SeedCharacterClearanceLevelsAsync(IStorageBackend storage, string characterUUID)
    {
        var defaults = new (int Level, string Name, string Description)[]
        {
            (1, "Recruit", "Basic access level"),
            (2, "Member", "Standard access level"),
            (3, "Officer", "Elevated access level"),
            (4, "Command", "High-level access"),
            (5, "Leader", "Full access"),
        };

        foreach (var (level, name, description) in defaults)
        {
            var clearanceLevel = new CharacterClearanceLevel
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerCharacterUUID = characterUUID,
                Level = level,
                Name = name,
                Description = description,
            };

            await storage.UpsertCharacterClearanceLevelAsync(clearanceLevel);
        }
    }
}
