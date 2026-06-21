using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Services;

/// <summary>
/// Computes effective permissions for a faction member by combining
/// role permissions, group capabilities, and individual capability grants.
/// </summary>
public static class PermissionResolver
{
    /// <summary>
    /// Computes the effective capability names for a character within a faction.
    /// Result is the additive union of: group capabilities + individual capabilities.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="factionUUID">The faction UUID.</param>
    /// <param name="characterUUID">The character UUID.</param>
    /// <returns>List of capability names the character effectively has.</returns>
    public static async Task<IReadOnlyList<string>> GetEffectiveCapabilitiesAsync(
        IStorageBackend storage,
        string factionUUID,
        string characterUUID)
    {
        var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get all faction capabilities for name resolution
        var factionCaps = await storage.GetFactionCapabilitiesAsync(factionUUID);
        var capLookup = factionCaps.ToDictionary(c => c.UUID, c => c.Name);

        // Individual capabilities
        var memberCaps = await storage.GetFactionMemberCapabilitiesAsync(
            factionUUID, characterUUID);
        foreach (var mc in memberCaps)
        {
            if (capLookup.TryGetValue(mc.CapabilityUUID, out var name))
            {
                capabilities.Add(name);
            }
        }

        // Group capabilities
        var memberPerms = await storage.GetFactionMemberPermissionsAsync(
            factionUUID, characterUUID);
        if (memberPerms?.GroupUUID != null)
        {
            var groupCaps = await storage.GetFactionGroupCapabilitiesAsync(
                memberPerms.GroupUUID);
            foreach (var gc in groupCaps)
            {
                if (capLookup.TryGetValue(gc.CapabilityUUID, out var name))
                {
                    capabilities.Add(name);
                }
            }
        }

        return capabilities.ToList();
    }

    /// <summary>
    /// Computes the effective sharing rules for a character within a faction.
    /// Result is the group sharing template rules that apply to the member.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="factionUUID">The faction UUID.</param>
    /// <param name="characterUUID">The character UUID.</param>
    /// <returns>List of sharing rules the character effectively has access to.</returns>
    public static async Task<IReadOnlyList<FactionGroupSharingRule>> GetEffectiveSharingRulesAsync(
        IStorageBackend storage,
        string factionUUID,
        string characterUUID)
    {
        var rules = new List<FactionGroupSharingRule>();

        var memberPerms = await storage.GetFactionMemberPermissionsAsync(
            factionUUID, characterUUID);
        if (memberPerms?.GroupUUID != null)
        {
            var groupRules = await storage.GetFactionGroupSharingRulesAsync(
                memberPerms.GroupUUID);
            rules.AddRange(groupRules);
        }

        return rules;
    }
}
