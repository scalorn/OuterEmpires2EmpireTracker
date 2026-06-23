// <copyright file="IFactionServerTypedClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Strongly-typed client interface for the Faction Server API.
    /// One async method per endpoint, returning typed domain objects.
    /// </summary>
    public interface IFactionServerTypedClient : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the client is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>Checks server health.</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the server is healthy; otherwise false.</returns>
        Task<bool> CheckHealthAsync(CancellationToken ct = default);

        /// <summary>Gets all server-level factions.</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of server factions.</returns>
        Task<ServerFaction[]> GetFactionsAsync(CancellationToken ct = default);

        /// <summary>Gets all server-level characters.</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of server characters.</returns>
        Task<ServerCharacter[]> GetCharactersAsync(CancellationToken ct = default);

        /// <summary>Creates a character on the server.</summary>
        /// <param name="name">The character name.</param>
        /// <param name="uuid">Optional UUID override.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateCharacterAsync(string name, string uuid = null, CancellationToken ct = default);

        /// <summary>Gets the sync snapshot (factions, characters, timestamp).</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The sync response containing server state.</returns>
        Task<SyncResponse> GetSyncSnapshotAsync(CancellationToken ct = default);

        /// <summary>Exports all character data as a typed PlayerRoot.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The exported player data.</returns>
        Task<PlayerRoot> ExportCharacterDataAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Uploads baseline data (typed BaselineRoot).</summary>
        /// <param name="baseline">The baseline data to upload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UploadBaselineAsync(BaselineRoot baseline, CancellationToken ct = default);

        /// <summary>Bulk imports all character data (typed PlayerRoot).</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="data">The player data to import.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The bulk import result with counts.</returns>
        Task<BulkImportResult> BulkImportAsync(string characterUUID, PlayerRoot data, CancellationToken ct = default);

        /// <summary>Gets sharing rules for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of sharing rules.</returns>
        Task<SharingRuleDto[]> GetSharingRulesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Replaces all sharing rules for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="rules">The sharing rules to set.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PutSharingRulesAsync(string characterUUID, SharingRuleDto[] rules, CancellationToken ct = default);

        /// <summary>Gets all colonies for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of colonies.</returns>
        Task<Colony[]> GetColoniesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all blueprints for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of blueprints.</returns>
        Task<Blueprint[]> GetBlueprintsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all surveys for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of surveys.</returns>
        Task<Survey[]> GetSurveysAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all player profiles for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of player profiles.</returns>
        Task<PlayerProfile[]> GetPlayerProfilesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all delivery routes for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of delivery routes.</returns>
        Task<DeliveryRoute[]> GetDeliveryRoutesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all delivery plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of delivery plans.</returns>
        Task<DeliveryPlan[]> GetDeliveryPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all ships for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of ships.</returns>
        Task<Ship[]> GetShipsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all ship templates for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of ship templates.</returns>
        Task<ShipTemplate[]> GetShipTemplatesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all market listings for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of market listings.</returns>
        Task<MarketListing[]> GetMarketListingsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all market transactions for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of market transactions.</returns>
        Task<MarketTransaction[]> GetMarketTransactionsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all pricing plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of pricing plans.</returns>
        Task<PricingPlan[]> GetPricingPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stock plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of stock plans.</returns>
        Task<StockPlan[]> GetStockPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stock profiles for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of stock profiles.</returns>
        Task<StockProfile[]> GetStockProfilesAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all build plans for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of build plans.</returns>
        Task<BuildPlan[]> GetBuildPlansAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all supply chains for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of supply chains.</returns>
        Task<SupplyChain[]> GetSupplyChainsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all asteroids for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of asteroids.</returns>
        Task<Asteroid[]> GetAsteroidsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all stations for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of stations.</returns>
        Task<Station[]> GetStationsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all faction contacts for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of faction contacts.</returns>
        Task<Faction[]> GetFactionContactsAsync(string characterUUID, CancellationToken ct = default);

        /// <summary>Gets all external characters for a character.</summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Array of external characters.</returns>
        Task<ExternalCharacter[]> GetExternalCharactersAsync(string characterUUID, CancellationToken ct = default);
    }
}
