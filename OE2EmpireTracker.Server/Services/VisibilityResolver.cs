using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Server.Services;

/// <summary>
/// Resolves what data a faction member can see using the two-layer visibility system.
/// Layer 1: What characters shared outward (CharacterGroupSharingRules).
/// Layer 2: What the faction's clearance rules permit (FactionGroupSharingRules + clearance).
/// Result: intersection of both layers.
/// </summary>
public static class VisibilityResolver
{
    /// <summary>
    /// Resolves the visible data types and entities for a faction member.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="factionUUID">The faction UUID.</param>
    /// <param name="characterUUID">The character UUID of the viewer.</param>
    /// <returns>The set of visible data descriptors.</returns>
    public static async Task<IReadOnlyList<VisibleDataEntry>> ResolveVisibleDataAsync(
        IStorageBackend storage,
        string factionUUID,
        string characterUUID)
    {
        var result = new List<VisibleDataEntry>();

        // Get the member's permissions (group + clearance)
        var memberPerms = await storage.GetFactionMemberPermissionsAsync(
            factionUUID, characterUUID);
        if (memberPerms == null)
        {
            return result;
        }

        // Resolve member's numeric clearance level
        int memberClearance = 0;
        if (!string.IsNullOrEmpty(memberPerms.ClearanceLevelUUID))
        {
            var levels = await storage.GetFactionClearanceLevelsAsync(factionUUID);
            var level = levels.FirstOrDefault(
                l => l.UUID == memberPerms.ClearanceLevelUUID);
            if (level != null)
            {
                memberClearance = level.Level;
            }
        }

        // Layer 2: What the faction's clearance rules permit
        var permittedByFaction = new HashSet<string>();
        if (memberPerms.GroupUUID != null)
        {
            var factionRules = await storage.GetFactionGroupSharingRulesAsync(
                memberPerms.GroupUUID);
            var factionLevels = await storage.GetFactionClearanceLevelsAsync(
                factionUUID);
            var levelLookup = factionLevels.ToDictionary(
                l => l.UUID, l => l.Level);

            foreach (var rule in factionRules)
            {
                if (levelLookup.TryGetValue(
                    rule.MinClearanceLevelUUID, out int minLevel))
                {
                    if (memberClearance >= minLevel)
                    {
                        var key = BuildKey(rule.DataType, rule.EntityUUID);
                        permittedByFaction.Add(key);
                    }
                }
            }
        }

        if (permittedByFaction.Count == 0)
        {
            return result;
        }

        // Layer 1: What characters shared outward to this faction
        var allCharacters = await storage.GetAllCharactersAsync();
        var sharedByCharacters = new HashSet<string>();

        foreach (var character in allCharacters)
        {
            var grantees = await storage.GetCharacterGranteesAsync(character.UUID);
            var factionGrantee = grantees.FirstOrDefault(g =>
                g.GranteeType == GranteeType.Faction
                && g.GranteeUUID == factionUUID
                && g.GroupUUID != null);

            if (factionGrantee == null)
            {
                continue;
            }

            var charRules = await storage.GetCharacterGroupSharingRulesAsync(
                factionGrantee.GroupUUID!);
            foreach (var rule in charRules)
            {
                var key = BuildKey(rule.DataType, rule.EntityUUID);
                sharedByCharacters.Add(key);
            }
        }

        // Result: intersection of both layers
        foreach (var key in permittedByFaction)
        {
            if (sharedByCharacters.Contains(key))
            {
                var parts = ParseKey(key);
                result.Add(new VisibleDataEntry
                {
                    DataType = parts.DataType,
                    EntityUUID = parts.EntityUUID,
                });
            }
        }

        return result;
    }

    private static string BuildKey(string? dataType, string? entityUUID)
    {
        return $"{dataType ?? "*"}|{entityUUID ?? "*"}";
    }

    private static (string? DataType, string? EntityUUID) ParseKey(string key)
    {
        var parts = key.Split('|', 2);
        var dataType = parts[0] == "*" ? null : parts[0];
        var entityUUID = parts.Length > 1 && parts[1] != "*" ? parts[1] : null;
        return (dataType, entityUUID);
    }
}

/// <summary>
/// Represents a visible data entry (DataType + optional EntityUUID).
/// </summary>
public class VisibleDataEntry
{
    /// <summary>Gets or sets the data type category (nullable for wildcard).</summary>
    public string? DataType { get; set; }

    /// <summary>Gets or sets the specific entity UUID (nullable for category-level).</summary>
    public string? EntityUUID { get; set; }
}