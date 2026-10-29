// -----------------------------------------------------------------------
// <copyright file="OfflineGameApiClientStub.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;

namespace OE2EmpireTracker.Tests.Helpers
{
    /// <summary>
    /// An <see cref="IGameApiTypedClient"/> whose every call fails immediately with the
    /// same exception a dead endpoint produces, without touching the network.
    /// </summary>
    /// <remarks>
    /// Tests of scheduler bookkeeping -- round-robin index advancement and fairness --
    /// care only that a sync attempt was made and failed. Pointing a real client at a
    /// closed port achieves that too, but costs seconds per call: the .NET Framework
    /// handler dual-stack connects, the retry policy backs off, and the token bucket
    /// throttles. Across a property test driving hundreds of cycles that is hours.
    ///
    /// This stub reproduces the observable behaviour exactly -- the scheduler catches
    /// <see cref="HttpRequestException"/> from the token exchange and records the cycle
    /// as failed -- so the tests exercise the same code path with no wall-clock cost.
    /// </remarks>
    public class OfflineGameApiClientStub : IGameApiTypedClient
    {
        /// <summary>
        /// Gets the number of calls made, so a test can assert an attempt happened.
        /// </summary>
        public int CallCount { get; private set; }

        /// <inheritdoc/>
        public bool IsCircuitOpen => false;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public virtual void InvalidateToken(string clientId, string secret)
        {
        }

        /// <inheritdoc/>
        public virtual Task<TokenResponseDto> ExchangeTokenAsync(string appId, string clientId, string secret, CancellationToken ct = default) => this.Unreachable<TokenResponseDto>();

        /// <inheritdoc/>
        public Task<bool> TestConnectionAsync(string appId, string clientId, string secret, CancellationToken ct = default) => this.Unreachable<bool>();

        /// <inheritdoc/>
        public Task<AcceptedJobs> GetAcceptedJobsAsync(CancellationToken ct = default) => this.Unreachable<AcceptedJobs>();

        /// <inheritdoc/>
        public Task<AssetLocationDetail> GetAssetLocationDetailAsync(int locationId, string locationType, CancellationToken ct = default) => this.Unreachable<AssetLocationDetail>();

        /// <inheritdoc/>
        public Task<AssetLocations> GetAssetLocationsAsync(CancellationToken ct = default) => this.Unreachable<AssetLocations>();

        /// <inheritdoc/>
        public Task<BankingBalance> GetBankingBalanceAsync(CancellationToken ct = default) => this.Unreachable<BankingBalance>();

        /// <inheritdoc/>
        public Task<BankingTransactions> GetBankingTransactionsAsync(int? offset = null, int? limit = null, CancellationToken ct = default) => this.Unreachable<BankingTransactions>();

        /// <inheritdoc/>
        public Task<AssetBlueprint> GetBlueprintDetailAsync(int blueprintId, CancellationToken ct = default) => this.Unreachable<AssetBlueprint>();

        /// <inheritdoc/>
        public Task<PublicCharacter> GetCharacterAsync(CancellationToken ct = default) => this.Unreachable<PublicCharacter>();

        /// <inheritdoc/>
        public Task<CharacterSkills> GetCharacterSkillsAsync(CancellationToken ct = default) => this.Unreachable<CharacterSkills>();

        /// <inheritdoc/>
        public Task<ColonyBuildings> GetColonyBuildingsAsync(int colonyId, CancellationToken ct = default) => this.Unreachable<ColonyBuildings>();

        /// <inheritdoc/>
        public Task<ColonyList> GetColonyListAsync(CancellationToken ct = default) => this.Unreachable<ColonyList>();

        /// <inheritdoc/>
        public Task<ColonySummary> GetColonySummaryAsync(int colonyId, CancellationToken ct = default) => this.Unreachable<ColonySummary>();

        /// <inheritdoc/>
        public Task<ColonyWarehouse> GetColonyWarehouseAsync(int colonyId, CancellationToken ct = default) => this.Unreachable<ColonyWarehouse>();

        /// <inheritdoc/>
        public Task<ColonyWorkers> GetColonyWorkersAsync(int colonyId, CancellationToken ct = default) => this.Unreachable<ColonyWorkers>();

        /// <inheritdoc/>
        public Task<AssetCrateContents> GetCrateContentsAsync(int crateId, CancellationToken ct = default) => this.Unreachable<AssetCrateContents>();

        /// <inheritdoc/>
        public Task<KillMailList> GetKillMailListAsync(bool? kills = null, bool? deaths = null, bool? pvp = null, int? offset = null, int? limit = null, CancellationToken ct = default) => this.Unreachable<KillMailList>();

        /// <inheritdoc/>
        public Task<KillMail> GetKillMailDetailAsync(int killMailId, CancellationToken ct = default) => this.Unreachable<KillMail>();

        /// <inheritdoc/>
        public Task<MailBody> GetMailBodyAsync(int mailId, CancellationToken ct = default) => this.Unreachable<MailBody>();

        /// <inheritdoc/>
        public Task<MailList> GetMailListAsync(int? offset = null, int? limit = null, string mailType = null, CancellationToken ct = default) => this.Unreachable<MailList>();

        /// <inheritdoc/>
        public Task<MarketCompetitorOrders> GetMarketBuyOrderCompetitorsAsync(string marketIds, CancellationToken ct = default) => this.Unreachable<MarketCompetitorOrders>();

        /// <inheritdoc/>
        public Task<MarketBuyOrders> GetMarketBuyOrdersAsync(CancellationToken ct = default) => this.Unreachable<MarketBuyOrders>();

        /// <inheritdoc/>
        public Task<MarketItems> GetMarketItemsAsync(string type, string search, CancellationToken ct = default) => this.Unreachable<MarketItems>();

        /// <inheritdoc/>
        public Task<MarketListings> GetMarketListingsAsync(string view, int? range = null, string search = null, CancellationToken ct = default) => this.Unreachable<MarketListings>();

        /// <inheritdoc/>
        public Task<MarketPriceStats> GetMarketPricesAsync(string type, long typeId, CancellationToken ct = default) => this.Unreachable<MarketPriceStats>();

        /// <inheritdoc/>
        public Task<MarketCompetitorOrders> GetMarketSellOrderCompetitorsAsync(string marketIds, CancellationToken ct = default) => this.Unreachable<MarketCompetitorOrders>();

        /// <inheritdoc/>
        public Task<MarketSellOrders> GetMarketSellOrdersAsync(CancellationToken ct = default) => this.Unreachable<MarketSellOrders>();

        /// <inheritdoc/>
        public Task<MarketShipComponents> GetMarketShipComponentsAsync(long marketId, CancellationToken ct = default) => this.Unreachable<MarketShipComponents>();

        /// <inheritdoc/>
        public Task<ShipCargo> GetShipCargoAsync(CancellationToken ct = default) => this.Unreachable<ShipCargo>();

        /// <inheritdoc/>
        public Task<ShipConfiguration> GetShipConfigurationAsync(CancellationToken ct = default) => this.Unreachable<ShipConfiguration>();

        /// <inheritdoc/>
        public Task<AssetSurvey> GetSurveyDetailAsync(int surveyId, CancellationToken ct = default) => this.Unreachable<AssetSurvey>();

        private Task<T> Unreachable<T>()
        {
            this.CallCount++;
            return Task.FromException<T>(
                new HttpRequestException("OfflineGameApiClientStub: no server is configured for this test."));
        }
    }
}
