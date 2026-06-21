// <copyright file="EntityPersistenceMap.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Encapsulates the storage backend CRUD delegates for a single per-character entity type.
    /// </summary>
    internal struct EntityPersistenceEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityPersistenceEntry"/> struct.
        /// </summary>
        /// <param name="entityType">The .NET type of the entity.</param>
        /// <param name="load">Delegate that loads all entities of this type for a character.</param>
        /// <param name="upsert">Delegate that upserts a single entity for a character.</param>
        /// <param name="delete">Delegate that deletes an entity by UUID for a character.</param>
        /// <param name="findInList">Delegate that finds an entity by UUID in the in-memory list.</param>
        public EntityPersistenceEntry(
            Type entityType,
            Func<IStorageBackend, string, Task<IReadOnlyList<object>>> load,
            Func<IStorageBackend, string, object, Task> upsert,
            Func<IStorageBackend, string, string, Task> delete,
            Func<PlayerContext, string, object> findInList)
        {
            EntityType = entityType;
            Load = load;
            Upsert = upsert;
            Delete = delete;
            FindInList = findInList;
        }

        /// <summary>
        /// Gets the .NET type of the entity (e.g. typeof(Colony)).
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Gets the delegate that loads all entities of this type for a given character UUID.
        /// Signature: (backend, characterUUID) => Task&lt;IReadOnlyList&lt;object&gt;&gt;.
        /// </summary>
        public Func<IStorageBackend, string, Task<IReadOnlyList<object>>> Load { get; }

        /// <summary>
        /// Gets the delegate that upserts a single entity for a given character UUID.
        /// Signature: (backend, characterUUID, entity) => Task.
        /// </summary>
        public Func<IStorageBackend, string, object, Task> Upsert { get; }

        /// <summary>
        /// Gets the delegate that deletes an entity by UUID for a given character UUID.
        /// Signature: (backend, characterUUID, entityUUID) => Task.
        /// </summary>
        public Func<IStorageBackend, string, string, Task> Delete { get; }

        /// <summary>
        /// Gets the delegate that finds an entity by UUID in the PlayerContext's in-memory list.
        /// Signature: (playerContext, entityUUID) => entity or null.
        /// </summary>
        public Func<PlayerContext, string, object> FindInList { get; }
    }

    /// <summary>
    /// Maps each per-character entity type to its storage backend CRUD delegates.
    /// Eliminates repetitive per-type boilerplate in WriteContext and LoadFromBackend.
    /// </summary>
    internal static class EntityPersistenceMap
    {
        /// <summary>
        /// Gets the array of persistence entries for all 22 per-character entity types.
        /// </summary>
        internal static EntityPersistenceEntry[] Entries { get; } = BuildEntries();

        private static EntityPersistenceEntry[] BuildEntries()
        {
            return new[]
            {
                new EntityPersistenceEntry(
                    typeof(Colony),
                    (b, uuid) => CastList<Colony>(b.GetAllColoniesAsync(uuid)),
                    (b, uuid, e) => b.UpsertColonyAsync(uuid, (Colony)e),
                    (b, uuid, id) => b.DeleteColonyAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Blueprint),
                    (b, uuid) => CastList<Blueprint>(b.GetAllBlueprintsAsync(uuid)),
                    (b, uuid, e) => b.UpsertBlueprintAsync(uuid, (Blueprint)e),
                    (b, uuid, id) => b.DeleteBlueprintAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Survey),
                    (b, uuid) => CastList<Survey>(b.GetAllSurveysAsync(uuid)),
                    (b, uuid, e) => b.UpsertSurveyAsync(uuid, (Survey)e),
                    (b, uuid, id) => b.DeleteSurveyAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(PlayerProfile),
                    (b, uuid) => CastList<PlayerProfile>(b.GetAllPlayerProfilesAsync(uuid)),
                    (b, uuid, e) => b.UpsertPlayerProfileAsync(uuid, (PlayerProfile)e),
                    (b, uuid, id) => b.DeletePlayerProfileAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(DeliveryRoute),
                    (b, uuid) => CastList<DeliveryRoute>(b.GetAllDeliveryRoutesAsync(uuid)),
                    (b, uuid, e) => b.UpsertDeliveryRouteAsync(uuid, (DeliveryRoute)e),
                    (b, uuid, id) => b.DeleteDeliveryRouteAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(DeliveryPlan),
                    (b, uuid) => CastList<DeliveryPlan>(b.GetAllDeliveryPlansAsync(uuid)),
                    (b, uuid, e) => b.UpsertDeliveryPlanAsync(uuid, (DeliveryPlan)e),
                    (b, uuid, id) => b.DeleteDeliveryPlanAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Ship),
                    (b, uuid) => CastList<Ship>(b.GetAllShipsAsync(uuid)),
                    (b, uuid, e) => b.UpsertShipAsync(uuid, (Ship)e),
                    (b, uuid, id) => b.DeleteShipAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(ShipTemplate),
                    (b, uuid) => CastList<ShipTemplate>(b.GetAllShipTemplatesAsync(uuid)),
                    (b, uuid, e) => b.UpsertShipTemplateAsync(uuid, (ShipTemplate)e),
                    (b, uuid, id) => b.DeleteShipTemplateAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Station),
                    (b, uuid) => CastList<Station>(b.GetAllStationsAsync(uuid)),
                    (b, uuid, e) => b.UpsertStationAsync(uuid, (Station)e),
                    (b, uuid, id) => b.DeleteStationAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(MarketListing),
                    (b, uuid) => CastList<MarketListing>(b.GetAllMarketListingsAsync(uuid)),
                    (b, uuid, e) => b.UpsertMarketListingAsync(uuid, (MarketListing)e),
                    (b, uuid, id) => b.DeleteMarketListingAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(MarketTransaction),
                    (b, uuid) => CastList<MarketTransaction>(b.GetAllMarketTransactionsAsync(uuid)),
                    (b, uuid, e) => b.UpsertMarketTransactionAsync(uuid, (MarketTransaction)e),
                    (b, uuid, id) => b.DeleteMarketTransactionAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(PricingPlan),
                    (b, uuid) => CastList<PricingPlan>(b.GetAllPricingPlansAsync(uuid)),
                    (b, uuid, e) => b.UpsertPricingPlanAsync(uuid, (PricingPlan)e),
                    (b, uuid, id) => b.DeletePricingPlanAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(BuildPlan),
                    (b, uuid) => CastList<BuildPlan>(b.GetAllBuildPlansAsync(uuid)),
                    (b, uuid, e) => b.UpsertBuildPlanAsync(uuid, (BuildPlan)e),
                    (b, uuid, id) => b.DeleteBuildPlanAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(StockPlan),
                    (b, uuid) => CastList<StockPlan>(b.GetAllStockPlansAsync(uuid)),
                    (b, uuid, e) => b.UpsertStockPlanAsync(uuid, (StockPlan)e),
                    (b, uuid, id) => b.DeleteStockPlanAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(StockProfile),
                    (b, uuid) => CastList<StockProfile>(b.GetAllStockProfilesAsync(uuid)),
                    (b, uuid, e) => b.UpsertStockProfileAsync(uuid, (StockProfile)e),
                    (b, uuid, id) => b.DeleteStockProfileAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(SupplyChain),
                    (b, uuid) => CastList<SupplyChain>(b.GetAllSupplyChainsAsync(uuid)),
                    (b, uuid, e) => b.UpsertSupplyChainAsync(uuid, (SupplyChain)e),
                    (b, uuid, id) => b.DeleteSupplyChainAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(WarehouseOverflowRule),
                    (b, uuid) => CastList<WarehouseOverflowRule>(b.GetAllWarehouseOverflowRulesAsync(uuid)),
                    (b, uuid, e) => b.UpsertWarehouseOverflowRuleAsync(uuid, (WarehouseOverflowRule)e),
                    (b, uuid, id) => b.DeleteWarehouseOverflowRuleAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Asteroid),
                    (b, uuid) => CastList<Asteroid>(b.GetAllAsteroidsAsync(uuid)),
                    (b, uuid, e) => b.UpsertAsteroidAsync(uuid, (Asteroid)e),
                    (b, uuid, id) => b.DeleteAsteroidAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(BankingTransaction),
                    (b, uuid) => CastList<BankingTransaction>(b.GetAllBankingTransactionsAsync(uuid)),
                    (b, uuid, e) => b.UpsertBankingTransactionAsync(uuid, (BankingTransaction)e),
                    (b, uuid, id) => b.DeleteBankingTransactionAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(MailMessage),
                    (b, uuid) => CastList<MailMessage>(b.GetAllMailMessagesAsync(uuid)),
                    (b, uuid, e) => b.UpsertMailMessageAsync(uuid, (MailMessage)e),
                    (b, uuid, id) => b.DeleteMailMessageAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(Faction),
                    (b, uuid) => CastList<Faction>(b.GetAllFactionsForCharacterAsync(uuid)),
                    (b, uuid, e) => b.UpsertFactionForCharacterAsync(uuid, (Faction)e),
                    (b, uuid, id) => b.DeleteFactionForCharacterAsync(uuid, id)),
                new EntityPersistenceEntry(
                    typeof(ExternalCharacter),
                    (b, uuid) => CastList<ExternalCharacter>(b.GetAllExternalCharactersAsync(uuid)),
                    (b, uuid, e) => b.UpsertExternalCharacterAsync(uuid, (ExternalCharacter)e),
                    (b, uuid, id) => b.DeleteExternalCharacterAsync(uuid, id)),
            };
        }

        /// <summary>
        /// Wraps a typed backend load call into the common object-list return type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="task">The task that returns a typed read-only list.</param>
        /// <returns>A task that returns the list cast to <see cref="IReadOnlyList{Object}"/>.</returns>
        private static async Task<IReadOnlyList<object>> CastList<T>(Task<IReadOnlyList<T>> task)
        {
            var result = await task.ConfigureAwait(false);
            return result.Cast<object>().ToList();
        }
    }
}
