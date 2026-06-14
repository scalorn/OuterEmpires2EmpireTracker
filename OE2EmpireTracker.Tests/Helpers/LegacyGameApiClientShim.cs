// <copyright file="LegacyGameApiClientShim.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Temporary compatibility shim that provides the old GameApiClient interface
    /// by delegating to GameApiTypedClient. Exists solely to allow test code to
    /// compile until task 10.x migrates tests to use IGameApiTypedClient directly.
    /// </summary>
    public class GameApiClient : IGameApiTypedClient
    {
        private readonly GameApiTypedClient _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiClient"/> class.
        /// </summary>
        /// <param name="serverUrl">Base URL of the game API server.</param>
        public GameApiClient(string serverUrl)
        {
            _inner = new GameApiTypedClient(serverUrl, "test-app-id", 0.9);
        }

        /// <inheritdoc/>
        public bool IsCircuitOpen => _inner.IsCircuitOpen;

        /// <summary>
        /// Legacy token exchange returning a tuple for backward compatibility.
        /// </summary>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        /// <param name="secret">The per-character secret.</param>
        /// <returns>A result containing the token response or an error message.</returns>
        public virtual async Task<(bool Success, GameApiTokenResponse Token, string ErrorMessage)> ExchangeTokenAsync(
            string appId,
            string clientId,
            string secret)
        {
            try
            {
                var dto = await _inner.ExchangeTokenAsync(appId, clientId, secret).ConfigureAwait(false);
                var response = new GameApiTokenResponse
                {
                    AccessToken = dto.AccessToken,
                    ExpiresIn = dto.ExpiresIn,
                    CharacterId = dto.CharacterId,
                };
                return (true, response, null);
            }
            catch (ApiHttpException ex)
            {
                return (false, null, "HTTP " + ex.StatusCode);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        /// <inheritdoc/>
        Task<TokenResponseDto> IGameApiTypedClient.ExchangeTokenAsync(string appId, string clientId, string secret, CancellationToken ct)
        {
            return _inner.ExchangeTokenAsync(appId, clientId, secret, ct);
        }

        /// <inheritdoc/>
        public Task<bool> TestConnectionAsync(string appId, string clientId, string secret, CancellationToken ct = default)
        {
            return _inner.TestConnectionAsync(appId, clientId, secret, ct);
        }

        /// <inheritdoc/>
        public Task<AcceptedJobs> GetAcceptedJobsAsync(CancellationToken ct = default) => _inner.GetAcceptedJobsAsync(ct);

        /// <inheritdoc/>
        public Task<AssetLocationDetail> GetAssetLocationDetailAsync(int locationId, string locationType, CancellationToken ct = default)
            => _inner.GetAssetLocationDetailAsync(locationId, locationType, ct);

        /// <inheritdoc/>
        public Task<AssetLocations> GetAssetLocationsAsync(CancellationToken ct = default) => _inner.GetAssetLocationsAsync(ct);

        /// <inheritdoc/>
        public Task<BankingBalance> GetBankingBalanceAsync(CancellationToken ct = default) => _inner.GetBankingBalanceAsync(ct);

        /// <inheritdoc/>
        public Task<BankingTransactions> GetBankingTransactionsAsync(int? offset = null, int? limit = null, CancellationToken ct = default)
            => _inner.GetBankingTransactionsAsync(offset, limit, ct);

        /// <inheritdoc/>
        public Task<AssetBlueprint> GetBlueprintDetailAsync(int blueprintId, CancellationToken ct = default)
            => _inner.GetBlueprintDetailAsync(blueprintId, ct);

        /// <inheritdoc/>
        public Task<PublicCharacter> GetCharacterAsync(CancellationToken ct = default) => _inner.GetCharacterAsync(ct);

        /// <inheritdoc/>
        public Task<CharacterSkills> GetCharacterSkillsAsync(CancellationToken ct = default) => _inner.GetCharacterSkillsAsync(ct);

        /// <inheritdoc/>
        public Task<ColonyBuildings> GetColonyBuildingsAsync(int colonyId, CancellationToken ct = default)
            => _inner.GetColonyBuildingsAsync(colonyId, ct);

        /// <inheritdoc/>
        public Task<ColonyList> GetColonyListAsync(CancellationToken ct = default) => _inner.GetColonyListAsync(ct);

        /// <inheritdoc/>
        public Task<ColonySummary> GetColonySummaryAsync(int colonyId, CancellationToken ct = default)
            => _inner.GetColonySummaryAsync(colonyId, ct);

        /// <inheritdoc/>
        public Task<ColonyWarehouse> GetColonyWarehouseAsync(int colonyId, CancellationToken ct = default)
            => _inner.GetColonyWarehouseAsync(colonyId, ct);

        /// <inheritdoc/>
        public Task<ColonyWorkers> GetColonyWorkersAsync(int colonyId, CancellationToken ct = default)
            => _inner.GetColonyWorkersAsync(colonyId, ct);

        /// <inheritdoc/>
        public Task<AssetCrateContents> GetCrateContentsAsync(int crateId, CancellationToken ct = default)
            => _inner.GetCrateContentsAsync(crateId, ct);

        /// <inheritdoc/>
        public Task<KillMailList> GetKillMailListAsync(bool? kills = null, bool? deaths = null, bool? pvp = null, int? offset = null, int? limit = null, CancellationToken ct = default)
            => _inner.GetKillMailListAsync(kills, deaths, pvp, offset, limit, ct);

        /// <inheritdoc/>
        public Task<KillMail> GetKillMailDetailAsync(int killMailId, CancellationToken ct = default)
            => _inner.GetKillMailDetailAsync(killMailId, ct);

        /// <inheritdoc/>
        public Task<MailBody> GetMailBodyAsync(int mailId, CancellationToken ct = default) => _inner.GetMailBodyAsync(mailId, ct);

        /// <inheritdoc/>
        public Task<MailList> GetMailListAsync(int? offset = null, int? limit = null, string mailType = null, CancellationToken ct = default)
            => _inner.GetMailListAsync(offset, limit, mailType, ct);

        /// <inheritdoc/>
        public Task<MarketCompetitorOrders> GetMarketBuyOrderCompetitorsAsync(string marketIds, CancellationToken ct = default)
            => _inner.GetMarketBuyOrderCompetitorsAsync(marketIds, ct);

        /// <inheritdoc/>
        public Task<MarketBuyOrders> GetMarketBuyOrdersAsync(CancellationToken ct = default) => _inner.GetMarketBuyOrdersAsync(ct);

        /// <inheritdoc/>
        public Task<MarketItems> GetMarketItemsAsync(string type, string search, CancellationToken ct = default)
            => _inner.GetMarketItemsAsync(type, search, ct);

        /// <inheritdoc/>
        public Task<MarketListings> GetMarketListingsAsync(string view, int? range = null, string search = null, CancellationToken ct = default)
            => _inner.GetMarketListingsAsync(view, range, search, ct);

        /// <inheritdoc/>
        public Task<MarketPriceStats> GetMarketPricesAsync(string type, long typeId, CancellationToken ct = default)
            => _inner.GetMarketPricesAsync(type, typeId, ct);

        /// <inheritdoc/>
        public Task<MarketCompetitorOrders> GetMarketSellOrderCompetitorsAsync(string marketIds, CancellationToken ct = default)
            => _inner.GetMarketSellOrderCompetitorsAsync(marketIds, ct);

        /// <inheritdoc/>
        public Task<MarketSellOrders> GetMarketSellOrdersAsync(CancellationToken ct = default) => _inner.GetMarketSellOrdersAsync(ct);

        /// <inheritdoc/>
        public Task<MarketShipComponents> GetMarketShipComponentsAsync(long marketId, CancellationToken ct = default)
            => _inner.GetMarketShipComponentsAsync(marketId, ct);

        /// <inheritdoc/>
        public Task<ShipCargo> GetShipCargoAsync(CancellationToken ct = default) => _inner.GetShipCargoAsync(ct);

        /// <inheritdoc/>
        public Task<ShipConfiguration> GetShipConfigurationAsync(CancellationToken ct = default) => _inner.GetShipConfigurationAsync(ct);

        /// <inheritdoc/>
        public Task<AssetSurvey> GetSurveyDetailAsync(int surveyId, CancellationToken ct = default)
            => _inner.GetSurveyDetailAsync(surveyId, ct);

        /// <inheritdoc/>
        public void Dispose()
        {
            _inner?.Dispose();
        }
    }
}
