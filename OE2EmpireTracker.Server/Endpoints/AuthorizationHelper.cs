using System.Security.Claims;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Shared authorization logic for hardened endpoints.
/// Evaluates access using the permission system (ownership, grantees, sharing rules, faction membership).
/// </summary>
public static class AuthorizationHelper
{
    /// <summary>
    /// Determines whether the current caller can access the specified character's data.
    /// Evaluation order: owner check, grantee check, faction grantee check, sharing rule check, Owner role check.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="characterUuid">The target character UUID.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>True if access is allowed; false otherwise.</returns>
    public static async Task<bool> CanAccessCharacterData(
        HttpContext httpContext,
        string characterUuid,
        IStorageBackend storage)
    {
        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");

        // 1. Is caller the character owner?
        if (callerCharUUID == characterUuid)
        {
            return true;
        }

        // 2. Is caller listed in CharacterGranteePermissions (GranteeType=Character)?
        var grantees = await storage.GetCharacterGranteesAsync(characterUuid);

        if (!string.IsNullOrEmpty(callerCharUUID))
        {
            var isDirectGrantee = grantees.Any(g =>
                g.GranteeType == GranteeType.Character &&
                g.GranteeUUID == callerCharUUID);

            if (isDirectGrantee)
            {
                return true;
            }
        }

        // 3. Is caller's faction listed in CharacterGranteePermissions (GranteeType=Faction)?
        var factionGrantees = grantees
            .Where(g => g.GranteeType == GranteeType.Faction)
            .Select(g => g.GranteeUUID)
            .ToList();

        if (factionGrantees.Count > 0 && !string.IsNullOrEmpty(callerCharUUID))
        {
            foreach (var factionUUID in factionGrantees)
            {
                var membership = await storage.GetFactionMemberPermissionsAsync(
                    factionUUID, callerCharUUID);
                if (membership != null)
                {
                    return true;
                }
            }
        }

        // 4. Has the character created a SharingRule targeting the caller?
        var sharingRules = await storage.GetSharingRulesForCharacterAsync(characterUuid);

        if (!string.IsNullOrEmpty(callerCharUUID))
        {
            // Check if any sharing rule targets the caller's character directly
            var hasCharacterRule = sharingRules.Any(r =>
                r.TargetType == SharingTargetType.Character &&
                r.TargetUUID == callerCharUUID);

            if (hasCharacterRule)
            {
                return true;
            }

            // Check if any sharing rule targets a faction the caller belongs to
            var factionRuleTargets = sharingRules
                .Where(r => r.TargetType == SharingTargetType.Faction)
                .Select(r => r.TargetUUID)
                .ToList();

            foreach (var factionUUID in factionRuleTargets)
            {
                var membership = await storage.GetFactionMemberPermissionsAsync(
                    factionUUID, callerCharUUID);
                if (membership != null)
                {
                    return true;
                }
            }
        }

        // 5. Is caller Owner?
        if (httpContext.User.IsInRole("Owner"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified character is a member of the specified faction.
    /// </summary>
    /// <param name="characterUUID">The character UUID to check.</param>
    /// <param name="factionUUID">The faction UUID to check membership in.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>True if a FactionMemberPermissions record exists; false otherwise.</returns>
    public static async Task<bool> IsFactionMember(
        string characterUUID,
        string factionUUID,
        IStorageBackend storage)
    {
        var membership = await storage.GetFactionMemberPermissionsAsync(factionUUID, characterUUID);
        return membership != null;
    }

    /// <summary>
    /// Determines whether the current caller has the Owner role.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>True if the caller is in the Owner role; false otherwise.</returns>
    public static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole("Owner");
    }

    /// <summary>
    /// Extracts the caller's CharacterUUID from the token claims.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The CharacterUUID claim value, or null if not present.</returns>
    public static string? GetCallerCharacterUUID(HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue("CharacterUUID");
    }
}