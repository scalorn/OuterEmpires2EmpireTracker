using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Common.Interfaces
{
    /// <summary>
    /// Unified abstraction for data persistence.
    /// All backends implement this interface so consumers are storage-agnostic.
    /// </summary>
    public partial interface IStorageBackend
    {
        // ═══════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Initializes the backend (creates files/tables/connections as needed).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// Validates that the backend can be reached and is operational.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the connection is valid; false otherwise.</returns>
        Task<bool> ValidateConnectionAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns metadata about this backend instance.
        /// </summary>
        /// <returns>A <see cref="StorageInfo"/> describing the backend type and location.</returns>
        StorageInfo GetStorageInfo();

        // ═══════════════════════════════════════════════════════════
        // Server Factions
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets a faction by UUID, or null if not found.</summary>
        /// <param name="uuid">The faction UUID.</param>
        /// <returns>The faction, or null.</returns>
        Task<ServerFaction> GetFactionAsync(string uuid);

        /// <summary>Gets all factions.</summary>
        /// <returns>A read-only list of all factions.</returns>
        Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync();

        /// <summary>Creates or updates a faction.</summary>
        /// <param name="faction">The faction to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionAsync(ServerFaction faction);

        /// <summary>Deletes a faction by UUID.</summary>
        /// <param name="uuid">The faction UUID to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionAsync(string uuid);

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets a character by UUID, or null if not found.</summary>
        /// <param name="uuid">The character UUID.</param>
        /// <returns>The character, or null.</returns>
        Task<ServerCharacter> GetCharacterAsync(string uuid);

        /// <summary>Gets all characters.</summary>
        /// <returns>A read-only list of all characters.</returns>
        Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync();

        /// <summary>Creates or updates a character.</summary>
        /// <param name="character">The character to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterAsync(ServerCharacter character);

        /// <summary>Deletes a character by UUID.</summary>
        /// <param name="uuid">The character UUID to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterAsync(string uuid);

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets a global data blob by data type key.</summary>
        /// <param name="dataType">The data type identifier.</param>
        /// <returns>The JSON string, or null if not found.</returns>
        Task<string> GetGlobalDataAsync(string dataType);

        /// <summary>Creates or updates a global data blob.</summary>
        /// <param name="dataType">The data type identifier.</param>
        /// <param name="json">The JSON content to store.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertGlobalDataAsync(string dataType, string json);

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all star systems.</summary>
        /// <returns>A read-only list of all star systems.</returns>
        Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync();

        /// <summary>Creates or updates star systems in bulk.</summary>
        /// <param name="systems">The star systems to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems);

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets colony summaries for a specific star system.</summary>
        /// <param name="systemId">The star system ID.</param>
        /// <returns>A read-only list of colony summaries in the system.</returns>
        Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId);

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <summary>Finds a token by its hash value.</summary>
        /// <param name="tokenHash">The token hash to search for.</param>
        /// <returns>The matching token, or null if not found.</returns>
        Task<ApiToken> FindTokenByHashAsync(string tokenHash);

        /// <summary>Gets all API tokens.</summary>
        /// <returns>A read-only list of all tokens.</returns>
        Task<IReadOnlyList<ApiToken>> GetAllTokensAsync();

        /// <summary>Creates or updates a token.</summary>
        /// <param name="token">The token to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertTokenAsync(ApiToken token);

        /// <summary>Deletes a token by ID.</summary>
        /// <param name="id">The token ID to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteTokenAsync(string id);

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all membership actions for a faction.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of membership actions.</returns>
        Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID);

        /// <summary>Creates or updates a membership action.</summary>
        /// <param name="action">The membership action to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertMembershipActionAsync(MembershipAction action);

        /// <summary>Deletes a membership action by ID.</summary>
        /// <param name="id">The action ID to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteMembershipActionAsync(string id);

        /// <summary>Deletes all membership actions that have expired before the given cutoff.</summary>
        /// <param name="cutoff">The cutoff date. Actions expiring before this are deleted.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteExpiredActionsAsync(DateTime cutoff);

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all sharing rules for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of sharing rules.</returns>
        Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID);

        /// <summary>Creates or updates sharing rules for a character (replaces all).</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="rules">The complete set of sharing rules.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules);

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets preferences for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>The preferences, or null if not found.</returns>
        Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID);

        /// <summary>Creates or updates character preferences.</summary>
        /// <param name="prefs">The preferences to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs);
    }

    /// <summary>
    /// Describes the type and location of a storage backend.
    /// </summary>
    public class StorageInfo
    {
        /// <summary>Gets or sets the backend type name.</summary>
        public string BackendType { get; set; } = string.Empty;

        /// <summary>Gets or sets the storage location (file path or connection string).</summary>
        public string Location { get; set; } = string.Empty;
    }
}
