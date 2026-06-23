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

        /// <summary>
        /// Returns all character UUIDs that have stored player data in this backend.
        /// Used by the migration service to discover which characters to migrate.
        /// </summary>
        /// <returns>A read-only list of distinct character UUIDs.</returns>
        Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync();

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

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Colony
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all colonies for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of colonies.</returns>
        Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID);

        /// <summary>Gets a colony by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The colony UUID.</param>
        /// <returns>The colony, or null if not found.</returns>
        Task<Colony> GetColonyAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a colony for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The colony to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertColonyAsync(string characterUUID, Colony entity);

        /// <summary>Deletes a colony for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The colony UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteColonyAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all blueprints for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of blueprints.</returns>
        Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID);

        /// <summary>Gets a blueprint by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The blueprint UUID.</param>
        /// <returns>The blueprint, or null if not found.</returns>
        Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a blueprint for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The blueprint to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertBlueprintAsync(string characterUUID, Blueprint entity);

        /// <summary>Deletes a blueprint for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The blueprint UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteBlueprintAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all surveys for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of surveys.</returns>
        Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID);

        /// <summary>Gets a survey by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The survey UUID.</param>
        /// <returns>The survey, or null if not found.</returns>
        Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a survey for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The survey to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertSurveyAsync(string characterUUID, Survey entity);

        /// <summary>Deletes a survey for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The survey UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteSurveyAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all player profiles for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of player profiles.</returns>
        Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID);

        /// <summary>Gets a player profile by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The profile UUID.</param>
        /// <returns>The player profile, or null if not found.</returns>
        Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a player profile for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The player profile to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity);

        /// <summary>Deletes a player profile for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The profile UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeletePlayerProfileAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all delivery routes for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of delivery routes.</returns>
        Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID);

        /// <summary>Gets a delivery route by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The route UUID.</param>
        /// <returns>The delivery route, or null if not found.</returns>
        Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a delivery route for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The delivery route to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity);

        /// <summary>Deletes a delivery route for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The route UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all delivery plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of delivery plans.</returns>
        Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID);

        /// <summary>Gets a delivery plan by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>The delivery plan, or null if not found.</returns>
        Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a delivery plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The delivery plan to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity);

        /// <summary>Deletes a delivery plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Ship
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all ships for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of ships.</returns>
        Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID);

        /// <summary>Gets a ship by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The ship UUID.</param>
        /// <returns>The ship, or null if not found.</returns>
        Task<Ship> GetShipAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a ship for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The ship to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertShipAsync(string characterUUID, Ship entity);

        /// <summary>Deletes a ship for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The ship UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteShipAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ShipTemplate
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all ship templates for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of ship templates.</returns>
        Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID);

        /// <summary>Gets a ship template by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The template UUID.</param>
        /// <returns>The ship template, or null if not found.</returns>
        Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a ship template for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The ship template to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity);

        /// <summary>Deletes a ship template for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The template UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteShipTemplateAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketListing
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all market listings for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of market listings.</returns>
        Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID);

        /// <summary>Gets a market listing by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The listing UUID.</param>
        /// <returns>The market listing, or null if not found.</returns>
        Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a market listing for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The market listing to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertMarketListingAsync(string characterUUID, MarketListing entity);

        /// <summary>Deletes a market listing for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The listing UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteMarketListingAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketTransaction
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all market transactions for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of market transactions.</returns>
        Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID);

        /// <summary>Gets a market transaction by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The transaction UUID.</param>
        /// <returns>The market transaction, or null if not found.</returns>
        Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a market transaction for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The market transaction to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity);

        /// <summary>Deletes a market transaction for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The transaction UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PricingPlan
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all pricing plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of pricing plans.</returns>
        Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID);

        /// <summary>Gets a pricing plan by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>The pricing plan, or null if not found.</returns>
        Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a pricing plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The pricing plan to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity);

        /// <summary>Deletes a pricing plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeletePricingPlanAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockPlan
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all stock plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of stock plans.</returns>
        Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID);

        /// <summary>Gets a stock plan by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>The stock plan, or null if not found.</returns>
        Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a stock plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The stock plan to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertStockPlanAsync(string characterUUID, StockPlan entity);

        /// <summary>Deletes a stock plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteStockPlanAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockProfile
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all stock profiles for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of stock profiles.</returns>
        Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID);

        /// <summary>Gets a stock profile by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The profile UUID.</param>
        /// <returns>The stock profile, or null if not found.</returns>
        Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a stock profile for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The stock profile to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertStockProfileAsync(string characterUUID, StockProfile entity);

        /// <summary>Deletes a stock profile for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The profile UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteStockProfileAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BuildPlan
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all build plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of build plans.</returns>
        Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID);

        /// <summary>Gets a build plan by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>The build plan, or null if not found.</returns>
        Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a build plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The build plan to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity);

        /// <summary>Deletes a build plan for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The plan UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteBuildPlanAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — SupplyChain
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all supply chains for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of supply chains.</returns>
        Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID);

        /// <summary>Gets a supply chain by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The supply chain UUID.</param>
        /// <returns>The supply chain, or null if not found.</returns>
        Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a supply chain for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The supply chain to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity);

        /// <summary>Deletes a supply chain for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The supply chain UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteSupplyChainAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Asteroid
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all asteroids for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of asteroids.</returns>
        Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID);

        /// <summary>Gets an asteroid by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The asteroid UUID.</param>
        /// <returns>The asteroid, or null if not found.</returns>
        Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates an asteroid for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The asteroid to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertAsteroidAsync(string characterUUID, Asteroid entity);

        /// <summary>Deletes an asteroid for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The asteroid UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsteroidAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Station
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all stations for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of stations.</returns>
        Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID);

        /// <summary>Gets a station by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The station UUID.</param>
        /// <returns>The station, or null if not found.</returns>
        Task<Station> GetStationAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a station for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The station to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertStationAsync(string characterUUID, Station entity);

        /// <summary>Deletes a station for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The station UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteStationAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Faction (contacts, distinct from ServerFaction)
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all faction contacts for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of faction contacts.</returns>
        Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID);

        /// <summary>Gets a faction contact by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The faction UUID.</param>
        /// <returns>The faction contact, or null if not found.</returns>
        Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a faction contact for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The faction to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity);

        /// <summary>Deletes a faction contact for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The faction UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ExternalCharacter
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all external characters for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of external characters.</returns>
        Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID);

        /// <summary>Gets an external character by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The external character UUID.</param>
        /// <returns>The external character, or null if not found.</returns>
        Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates an external character for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The external character to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity);

        /// <summary>Deletes an external character for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The external character UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — WarehouseOverflowRule
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all warehouse overflow rules for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of overflow rules.</returns>
        Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID);

        /// <summary>Gets a warehouse overflow rule by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The rule UUID.</param>
        /// <returns>The overflow rule, or null if not found.</returns>
        Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a warehouse overflow rule for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The overflow rule to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity);

        /// <summary>Deletes a warehouse overflow rule for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The rule UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MailMessage
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all mail messages for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of mail messages.</returns>
        Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID);

        /// <summary>Gets a mail message by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The message UUID.</param>
        /// <returns>The mail message, or null if not found.</returns>
        Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a mail message for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The mail message to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertMailMessageAsync(string characterUUID, MailMessage entity);

        /// <summary>Deletes a mail message for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The message UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteMailMessageAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BankingTransaction
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all banking transactions for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of banking transactions.</returns>
        Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID);

        /// <summary>Gets a banking transaction by UUID for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The transaction UUID.</param>
        /// <returns>The banking transaction, or null if not found.</returns>
        Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID);

        /// <summary>Creates or updates a banking transaction for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entity">The banking transaction to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity);

        /// <summary>Deletes a banking transaction for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="entityUUID">The transaction UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID);

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all capabilities for a faction.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of faction capabilities.</returns>
        Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID);

        /// <summary>Creates or updates a faction capability.</summary>
        /// <param name="capability">The capability to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionCapabilityAsync(FactionCapability capability);

        /// <summary>Deletes a faction capability.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID);

        /// <summary>Gets all clearance levels for a faction.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of clearance levels.</returns>
        Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID);

        /// <summary>Creates or updates a faction clearance level.</summary>
        /// <param name="level">The clearance level to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level);

        /// <summary>Deletes a faction clearance level.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="levelUUID">The level UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID);

        /// <summary>Gets all permission groups for a faction.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of permission groups.</returns>
        Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID);

        /// <summary>Gets a single faction permission group.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>The group, or null if not found.</returns>
        Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID);

        /// <summary>Creates or updates a faction permission group.</summary>
        /// <param name="group">The permission group to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionGroupAsync(FactionPermissionGroup group);

        /// <summary>Deletes a faction permission group.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionGroupAsync(string factionUUID, string groupUUID);

        /// <summary>Gets all capabilities assigned to a faction permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A read-only list of group capabilities.</returns>
        Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID);

        /// <summary>Adds a capability to a faction permission group.</summary>
        /// <param name="item">The group capability to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddFactionGroupCapabilityAsync(FactionGroupCapability item);

        /// <summary>Removes a capability from a faction permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID);

        /// <summary>Gets all sharing rules for a faction permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A read-only list of group sharing rules.</returns>
        Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID);

        /// <summary>Creates or updates a faction group sharing rule.</summary>
        /// <param name="rule">The sharing rule to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule);

        /// <summary>Deletes a faction group sharing rule.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <param name="ruleUUID">The rule UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID);

        /// <summary>Gets faction member permissions for a specific character.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>The member permissions, or null if not found.</returns>
        Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID);

        /// <summary>Creates or updates faction member permissions.</summary>
        /// <param name="perms">The member permissions to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms);

        /// <summary>Gets all faction members' permissions.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of all members' permissions.</returns>
        Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID);

        /// <summary>Gets all capabilities directly granted to a faction member.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of member capabilities.</returns>
        Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID);

        /// <summary>Adds a capability to a faction member.</summary>
        /// <param name="item">The member capability to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddFactionMemberCapabilityAsync(FactionMemberCapability item);

        /// <summary>Removes a capability from a faction member.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID);

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all capabilities for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of character capabilities.</returns>
        Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID);

        /// <summary>Creates or updates a character capability.</summary>
        /// <param name="capability">The capability to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterCapabilityAsync(CharacterCapability capability);

        /// <summary>Deletes a character capability.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID);

        /// <summary>Gets all clearance levels for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of clearance levels.</returns>
        Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID);

        /// <summary>Creates or updates a character clearance level.</summary>
        /// <param name="level">The clearance level to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level);

        /// <summary>Deletes a character clearance level.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="levelUUID">The level UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID);

        /// <summary>Gets all permission groups for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>A read-only list of permission groups.</returns>
        Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID);

        /// <summary>Gets a single character permission group.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>The group, or null if not found.</returns>
        Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID);

        /// <summary>Creates or updates a character permission group.</summary>
        /// <param name="group">The permission group to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterGroupAsync(CharacterPermissionGroup group);

        /// <summary>Deletes a character permission group.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID);

        /// <summary>Gets all capabilities assigned to a character permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A read-only list of group capabilities.</returns>
        Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID);

        /// <summary>Adds a capability to a character permission group.</summary>
        /// <param name="item">The group capability to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item);

        /// <summary>Removes a capability from a character permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID);

        /// <summary>Gets all sharing rules for a character permission group.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <returns>A read-only list of group sharing rules.</returns>
        Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID);

        /// <summary>Creates or updates a character group sharing rule.</summary>
        /// <param name="rule">The sharing rule to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule);

        /// <summary>Deletes a character group sharing rule.</summary>
        /// <param name="groupUUID">The group UUID.</param>
        /// <param name="ruleUUID">The rule UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID);

        /// <summary>Gets all grantee permissions for a character.</summary>
        /// <param name="ownerCharacterUUID">The owner character UUID.</param>
        /// <returns>A read-only list of grantee permissions.</returns>
        Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID);

        /// <summary>Creates or updates grantee permissions.</summary>
        /// <param name="perms">The grantee permissions to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms);

        /// <summary>Deletes grantee permissions.</summary>
        /// <param name="ownerCharacterUUID">The owner character UUID.</param>
        /// <param name="granteeUUID">The grantee UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID);

        /// <summary>Gets all capabilities granted to a specific grantee.</summary>
        /// <param name="ownerCharacterUUID">The owner character UUID.</param>
        /// <param name="granteeUUID">The grantee UUID.</param>
        /// <returns>A read-only list of grantee capabilities.</returns>
        Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID);

        /// <summary>Adds a capability to a grantee.</summary>
        /// <param name="item">The grantee capability to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item);

        /// <summary>Removes a capability from a grantee.</summary>
        /// <param name="ownerCharacterUUID">The owner character UUID.</param>
        /// <param name="granteeUUID">The grantee UUID.</param>
        /// <param name="capabilityUUID">The capability UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID);

        // ═══════════════════════════════════════════════════════════
        // Intel
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets all intel comments for a target character.</summary>
        /// <param name="targetCharacterUUID">The target character UUID.</param>
        /// <returns>A read-only list of intel comments.</returns>
        Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID);

        /// <summary>Gets an intel comment by UUID.</summary>
        /// <param name="commentUUID">The comment UUID.</param>
        /// <returns>The intel comment, or null if not found.</returns>
        Task<IntelComment> GetIntelCommentAsync(string commentUUID);

        /// <summary>Creates or updates an intel comment.</summary>
        /// <param name="comment">The intel comment to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertIntelCommentAsync(IntelComment comment);

        /// <summary>Deletes an intel comment.</summary>
        /// <param name="commentUUID">The comment UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteIntelCommentAsync(string commentUUID);

        /// <summary>Gets all intel shares for a comment.</summary>
        /// <param name="commentUUID">The comment UUID.</param>
        /// <returns>A read-only list of faction shares.</returns>
        Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID);

        /// <summary>Gets all intel shares for a faction.</summary>
        /// <param name="factionUUID">The faction UUID.</param>
        /// <returns>A read-only list of faction shares.</returns>
        Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID);

        /// <summary>Creates or updates an intel faction share.</summary>
        /// <param name="share">The share to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertIntelShareAsync(IntelCommentFactionShare share);

        /// <summary>Deletes an intel faction share.</summary>
        /// <param name="shareUUID">The share UUID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteIntelShareAsync(string shareUUID);

        // ═══════════════════════════════════════════════════════════
        // Audit
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets permission audit entries with optional filters.</summary>
        /// <param name="startDate">Optional start date filter.</param>
        /// <param name="endDate">Optional end date filter.</param>
        /// <param name="actionType">Optional action type filter.</param>
        /// <param name="actorUUID">Optional actor UUID filter.</param>
        /// <param name="targetUUID">Optional target UUID filter.</param>
        /// <returns>A read-only list of matching audit entries.</returns>
        Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null);

        /// <summary>Appends a permission audit entry.</summary>
        /// <param name="entry">The audit entry to append.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry);

        /// <summary>Deletes expired audit entries before a cutoff date.</summary>
        /// <param name="cutoff">The cutoff date.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteExpiredAuditEntriesAsync(DateTime cutoff);

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data
        // ═══════════════════════════════════════════════════════════

        /// <summary>Gets the baseline game constants.</summary>
        /// <returns>The game constants, or null if not set.</returns>
        Task<BaselineGameConstants> GetBaselineGameConstantsAsync();

        /// <summary>Creates or updates the baseline game constants.</summary>
        /// <param name="constants">The game constants to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants);

        /// <summary>Gets all blueprint type definitions.</summary>
        /// <returns>A read-only list of blueprint types.</returns>
        Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync();

        /// <summary>Creates or updates blueprint type definitions in bulk.</summary>
        /// <param name="types">The blueprint types to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types);

        /// <summary>Gets all ship class definitions.</summary>
        /// <returns>A read-only list of ship classes.</returns>
        Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync();

        /// <summary>Creates or updates ship class definitions in bulk.</summary>
        /// <param name="classes">The ship classes to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes);

        /// <summary>Gets all tech level definitions.</summary>
        /// <returns>A read-only list of tech levels.</returns>
        Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync();

        /// <summary>Creates or updates tech level definitions in bulk.</summary>
        /// <param name="levels">The tech levels to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels);

        /// <summary>Gets all commodity definitions.</summary>
        /// <returns>A read-only list of commodities.</returns>
        Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync();

        /// <summary>Creates or updates commodity definitions in bulk.</summary>
        /// <param name="commodities">The commodities to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities);

        /// <summary>Gets all refining recipe definitions.</summary>
        /// <returns>A read-only list of refining recipes.</returns>
        Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync();

        /// <summary>Creates or updates refining recipe definitions in bulk.</summary>
        /// <param name="recipes">The refining recipes to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes);

        /// <summary>Gets all research time entries.</summary>
        /// <returns>A read-only list of research time entries.</returns>
        Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync();

        /// <summary>Creates or updates research time entries in bulk.</summary>
        /// <param name="entries">The research time entries to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries);

        /// <summary>Gets all property type definitions.</summary>
        /// <returns>A read-only list of property type definitions.</returns>
        Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync();

        /// <summary>Creates or updates property type definitions in bulk.</summary>
        /// <param name="definitions">The property type definitions to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions);

        /// <summary>Gets all global (baseline) blueprints.</summary>
        /// <returns>A read-only list of global blueprints.</returns>
        Task<IReadOnlyList<Blueprint>> GetAllGlobalBlueprintsAsync();

        /// <summary>Creates or updates global (baseline) blueprints in bulk.</summary>
        /// <param name="blueprints">The global blueprints to upsert.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertGlobalBlueprintsAsync(IReadOnlyList<Blueprint> blueprints);
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
