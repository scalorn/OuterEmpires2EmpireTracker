using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Services;

/// <summary>
/// Filters intel comments based on caller's visibility rules.
/// Submitter's own private comments are always visible.
/// Faction-shared classified comments visible if caller clearance >= classification level.
/// Unclassified comments visible only if caller has classify_intel capability.
/// </summary>
public static class IntelVisibilityService
{
    /// <summary>
    /// Filters intel comments for a specific caller based on visibility rules.
    /// </summary>
    /// <param name="comments">All comments for the target character.</param>
    /// <param name="shares">All faction shares for those comments.</param>
    /// <param name="callerCharUUID">The caller's character UUID.</param>
    /// <param name="callerFactionUUID">The caller's faction UUID (nullable).</param>
    /// <param name="callerClearanceLevel">The caller's numeric clearance level.</param>
    /// <param name="hasClassifyIntel">Whether caller has classify_intel capability.</param>
    /// <param name="factionClearanceLevels">Faction clearance levels for resolving UUIDs.</param>
    /// <returns>Filtered list of visible comments.</returns>
    public static IReadOnlyList<IntelComment> FilterIntelForCaller(
        IReadOnlyList<IntelComment> comments,
        IReadOnlyList<IntelCommentFactionShare> shares,
        string callerCharUUID,
        string? callerFactionUUID,
        int callerClearanceLevel,
        bool hasClassifyIntel,
        IReadOnlyList<FactionClearanceLevel>? factionClearanceLevels = null)
    {
        var visible = new List<IntelComment>();
        var levelLookup = factionClearanceLevels?
            .ToDictionary(l => l.UUID, l => l.Level)
            ?? new Dictionary<string, int>();

        foreach (var comment in comments)
        {
            if (IsCommentVisible(
                comment, shares, callerCharUUID, callerFactionUUID,
                callerClearanceLevel, hasClassifyIntel, levelLookup))
            {
                visible.Add(comment);
            }
        }

        return visible;
    }

    private static bool IsCommentVisible(
        IntelComment comment,
        IReadOnlyList<IntelCommentFactionShare> shares,
        string callerCharUUID,
        string? callerFactionUUID,
        int callerClearanceLevel,
        bool hasClassifyIntel,
        Dictionary<string, int> levelLookup)
    {
        // Submitter always sees their own comments
        if (comment.SubmitterCharacterUUID == callerCharUUID)
        {
            return true;
        }

        // Non-faction members can only see their own comments
        if (callerFactionUUID == null)
        {
            return false;
        }

        var factionShares = shares
            .Where(s => s.IntelCommentUUID == comment.UUID
                && s.FactionUUID == callerFactionUUID)
            .ToList();

        if (factionShares.Count == 0)
        {
            return false;
        }

        foreach (var share in factionShares)
        {
            if (share.ClassificationLevelUUID == null)
            {
                // Unclassified: only visible to classify_intel holders
                if (hasClassifyIntel)
                {
                    return true;
                }
            }
            else
            {
                // Classified: visible if caller clearance >= classification level
                if (levelLookup.TryGetValue(
                    share.ClassificationLevelUUID, out int requiredLevel))
                {
                    if (callerClearanceLevel >= requiredLevel)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
