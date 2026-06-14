// <copyright file="IGameApiTypedClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Client.Generated;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Strongly-typed client interface for the Outer Empires 2 game API.
    /// Provides one method per endpoint returning typed DTOs, with built-in
    /// resilience (retry, circuit breaker) and rate limiting.
    /// </summary>
    public interface IGameApiTypedClient : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the circuit breaker is currently open.
        /// </summary>
        bool IsCircuitOpen { get; }

        /// <summary>
        /// Exchanges OAuth2 client credentials for an access token.
        /// </summary>
        Task<TokenResponseDto> ExchangeTokenAsync(string appId, string clientId, string secret, CancellationToken ct = default);

        /// <summary>
        /// Tests connectivity by performing a trial token exchange.
        /// </summary>
        Task<bool> TestConnectionAsync(string appId, string clientId, string secret, CancellationToken ct = default);

        /// <summary>
        /// Gets the accepted jobs for the authenticated character.
        /// </summary>
        Task<AcceptedJobs> GetAcceptedJobsAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets detail for a specific asset location.
        /// </summary>
        Task<AssetLocationDetail> GetAssetLocationDetailAsync(int locationId, string locationType, CancellationToken ct = default);

        /// <summary>
        /// Gets the list of asset locations for the authenticated character.
        /// </summary>
        Task<AssetLocations> GetAssetLocationsAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the banking balance for the authenticated character.
        /// </summary>
        Task<BankingBalance> GetBankingBalanceAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets banking transactions with optional pagination.
        /// </summary>
        Task<BankingTransactions> GetBankingTransactionsAsync(int? offset = null, int? limit = null, CancellationToken ct = default);

        /// <summary>
        /// Gets detail for a specific blueprint.
        /// </summary>
        Task<AssetBlueprint> GetBlueprintDetailAsync(int blueprintId, CancellationToken ct = default);

        /// <summary>
        /// Gets the public character profile for the authenticated character.
        /// </summary>
        Task<PublicCharacter> GetCharacterAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the skills for the authenticated character.
        /// </summary>
        Task<CharacterSkills> GetCharacterSkillsAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the buildings for a specific colony.
        /// </summary>
        Task<ColonyBuildings> GetColonyBuildingsAsync(int colonyId, CancellationToken ct = default);

        /// <summary>
        /// Gets the list of colonies for the authenticated character.
        /// </summary>
        Task<ColonyList> GetColonyListAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets a summary for a specific colony.
        /// </summary>
        Task<ColonySummary> GetColonySummaryAsync(int colonyId, CancellationToken ct = default);

        /// <summary>
        /// Gets the warehouse contents for a specific colony.
        /// </summary>
        Task<ColonyWarehouse> GetColonyWarehouseAsync(int colonyId, CancellationToken ct = default);

        /// <summary>
        /// Gets the workers for a specific colony.
        /// </summary>
        Task<ColonyWorkers> GetColonyWorkersAsync(int colonyId, CancellationToken ct = default);

        /// <summary>
        /// Gets the contents of a specific crate.
        /// </summary>
        Task<AssetCrateContents> GetCrateContentsAsync(int crateId, CancellationToken ct = default);

        /// <summary>
        /// Gets the kill mail list with optional filters and pagination.
        /// </summary>
        Task<KillMailList> GetKillMailListAsync(bool? kills = null, bool? deaths = null, bool? pvp = null, int? offset = null, int? limit = null, CancellationToken ct = default);

        /// <summary>
        /// Gets detail for a specific kill mail.
        /// </summary>
        Task<KillMail> GetKillMailDetailAsync(int killMailId, CancellationToken ct = default);

        /// <summary>
        /// Gets the body of a specific mail message.
        /// </summary>
        Task<MailBody> GetMailBodyAsync(int mailId, CancellationToken ct = default);

        /// <summary>
        /// Gets the mail list with optional pagination and type filtering.
        /// </summary>
        Task<MailList> GetMailListAsync(int? offset = null, int? limit = null, string mailType = null, CancellationToken ct = default);

        /// <summary>
        /// Gets the buy order competitors for the specified market IDs.
        /// </summary>
        Task<MarketCompetitorOrders> GetMarketBuyOrderCompetitorsAsync(string marketIds, CancellationToken ct = default);

        /// <summary>
        /// Gets the character's open market buy orders.
        /// </summary>
        Task<MarketBuyOrders> GetMarketBuyOrdersAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets market items matching the specified type and search criteria.
        /// </summary>
        Task<MarketItems> GetMarketItemsAsync(string type, string search, CancellationToken ct = default);

        /// <summary>
        /// Gets market listings with the specified view and optional filters.
        /// </summary>
        Task<MarketListings> GetMarketListingsAsync(string view, int? range = null, string search = null, CancellationToken ct = default);

        /// <summary>
        /// Gets market price statistics for a specific item type.
        /// </summary>
        Task<MarketPriceStats> GetMarketPricesAsync(string type, long typeId, CancellationToken ct = default);

        /// <summary>
        /// Gets the sell order competitors for the specified market IDs.
        /// </summary>
        Task<MarketCompetitorOrders> GetMarketSellOrderCompetitorsAsync(string marketIds, CancellationToken ct = default);

        /// <summary>
        /// Gets the character's open market sell orders.
        /// </summary>
        Task<MarketSellOrders> GetMarketSellOrdersAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the ship components for a market listing.
        /// </summary>
        Task<MarketShipComponents> GetMarketShipComponentsAsync(long marketId, CancellationToken ct = default);

        /// <summary>
        /// Gets the ship cargo for the authenticated character.
        /// </summary>
        Task<ShipCargo> GetShipCargoAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the ship configuration for the authenticated character.
        /// </summary>
        Task<ShipConfiguration> GetShipConfigurationAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets detail for a specific survey.
        /// </summary>
        Task<AssetSurvey> GetSurveyDetailAsync(int surveyId, CancellationToken ct = default);
    }
}
