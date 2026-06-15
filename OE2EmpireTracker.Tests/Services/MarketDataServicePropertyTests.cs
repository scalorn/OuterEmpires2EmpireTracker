using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using ApiMarketListing = OE2EmpireTracker.Common.Client.Generated.MarketListing;
using ApiMarketListings = OE2EmpireTracker.Common.Client.Generated.MarketListings;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for MarketDataService merge, stale removal,
    /// deduplication, and private sale visibility.
    /// Feature: market-integration
    /// </summary>
    [TestFixture]
    public class MarketDataServicePropertyTests
    {
        private PlayerContext _ctx;
        private SystemRepository _repo;
        private SystemGridIndex _gridIndex;
        private MarketDataService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());

            _repo = new SystemRepository();
            _repo.ReplaceAll(new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                new StarSystem { Id = 2, Name = "Beta", X = 10m, Y = 10m },
                new StarSystem { Id = 3, Name = "Gamma", X = 100m, Y = 100m },
            });

            _gridIndex = new SystemGridIndex(_repo, 50);
            _svc = new MarketDataService(_ctx, _gridIndex);
        }

        // -------------------------------------------------------------------
        // Generators
        // -------------------------------------------------------------------

        private static Gen<long> MarketIdGen()
        {
            return Gen.Choose(1, 100000).Select(i => (long)i);
        }

        private static Gen<string> CharUuidGen()
        {
            return Gen.Elements("char-aaa", "char-bbb", "char-ccc");
        }

        private static Gen<ApiMarketListing> ApiListingGen()
        {
            return from marketId in MarketIdGen()
                   from price in Gen.Choose(1, 99999).Select(p => (double)p / 100.0)
                   from amount in Gen.Choose(1, 10000)
                   from buyOrder in Arb.Generate<bool>()
                   select new ApiMarketListing
                   {
                       MarketId = marketId,
                       Price = price,
                       AmountRemaining = amount,
                       BuyOrder = buyOrder,
                       Type = "R",
                       TypeId = 1,
                       Description = "Test Resource",
                       LocationName = "Station Alpha",
                       SellerName = "Seller",
                       SellerFactionTag = "[TST]",
                       PrivateSale = false,
                       GroupA = "High",
                   };
        }

        private static Gen<List<ApiMarketListing>> UniqueListingsGen(int minCount, int maxCount)
        {
            return from count in Gen.Choose(minCount, maxCount)
                   from listings in Gen.ListOf(count, ApiListingGen())
                   let deduped = listings
                       .GroupBy(l => l.MarketId)
                       .Select(g => g.First())
                       .ToList()
                   where deduped.Count >= minCount
                   select deduped;
        }

        // -------------------------------------------------------------------
        // 9.1 Merge Idempotency Property
        // **Validates: Requirements 1.2**
        // Syncing the same DTO twice produces an identical dataset.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeIdempotency_SyncingSameDtoTwice_ProducesIdenticalDataset()
        {
            return Prop.ForAll(
                Arb.From(UniqueListingsGen(1, 20)),
                Arb.From(CharUuidGen()),
                (listings, charUuid) =>
                {
                    // Arrange — fresh context each run
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    var dto = new ApiMarketListings
                    {
                        Listings = listings,
                    };

                    // Act — sync the same DTO twice
                    svc.ProcessMarketListings(dto, charUuid, 1, 200);
                    List<MarketListing> afterFirst = ctx.SnapshotMarketListingList();

                    svc.ProcessMarketListings(dto, charUuid, 1, 200);
                    List<MarketListing> afterSecond = ctx.SnapshotMarketListingList();

                    // Assert — same count and same MarketIds with same prices
                    bool sameCount = afterFirst.Count == afterSecond.Count;
                    bool sameIds = afterFirst.All(f =>
                        afterSecond.Any(s =>
                            s.MarketId == f.MarketId &&
                            s.PricePerUnit == f.PricePerUnit &&
                            s.Quantity == f.Quantity));

                    return sameCount && sameIds;
                });
        }

        // -------------------------------------------------------------------
        // 9.2 Stale Removal Soundness Property
        // **Validates: Requirements 1.4, 14.2**
        // Every removed order was within range AND absent from fresh results.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StaleRemovalSoundness_RemovedOrdersWereInRangeAndAbsent()
        {
            return Prop.ForAll(
                Arb.From(UniqueListingsGen(3, 15)),
                Arb.From(Gen.Choose(1, 5).Select(n => n)),
                (listings, removeCount) =>
                {
                    // Arrange
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                        new StarSystem { Id = 2, Name = "Beta", X = 5m, Y = 5m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    // Seed all listings with SystemId=1 (in range)
                    var dto = new ApiMarketListings { Listings = listings };
                    svc.ProcessMarketListings(dto, "char-1", 1, 200);

                    // Set SystemId on all seeded listings
                    foreach (var ml in ctx.SnapshotMarketListingList())
                    {
                        ml.SystemId = 1;
                    }

                    ctx.InvalidateMarketListingByMarketIdCache();

                    // Build fresh set that excludes some orders
                    int actualRemove = Math.Min(removeCount, listings.Count - 1);
                    var freshIds = new HashSet<long>(
                        listings.Skip(actualRemove).Select(l => l.MarketId));

                    var removedMarketIds = new HashSet<long>(
                        listings.Take(actualRemove).Select(l => l.MarketId));

                    var metadata = new CharacterSyncMetadata
                    {
                        CharacterUUID = "char-1",
                        SystemId = 1,
                        RangeJas = 200,
                        SystemsInRange = new HashSet<int> { 1, 2 },
                    };

                    List<MarketListing> before = ctx.SnapshotMarketListingList();

                    // Act
                    svc.RemoveStaleOrders(freshIds, "char-1", metadata);

                    List<MarketListing> after = ctx.SnapshotMarketListingList();
                    var removedItems = before
                        .Where(b => !after.Any(a => a.MarketId == b.MarketId))
                        .ToList();

                    // Assert: every removed order was in-range AND absent from fresh
                    return removedItems.All(r =>
                        r.SystemId.HasValue &&
                        metadata.SystemsInRange.Contains(r.SystemId.Value) &&
                        !freshIds.Contains(r.MarketId.Value));
                });
        }

        // -------------------------------------------------------------------
        // 9.3 Stale Removal Completeness Property
        // **Validates: Requirements 14.1, 14.2**
        // Every in-range order absent from fresh results IS removed.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StaleRemovalCompleteness_AllInRangeAbsentOrdersAreRemoved()
        {
            return Prop.ForAll(
                Arb.From(UniqueListingsGen(3, 15)),
                Arb.From(Gen.Choose(1, 5).Select(n => n)),
                (listings, removeCount) =>
                {
                    // Arrange
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    var dto = new ApiMarketListings { Listings = listings };
                    svc.ProcessMarketListings(dto, "char-1", 1, 200);

                    // Set SystemId on all seeded listings
                    foreach (var ml in ctx.SnapshotMarketListingList())
                    {
                        ml.SystemId = 1;
                    }

                    ctx.InvalidateMarketListingByMarketIdCache();

                    int actualRemove = Math.Min(removeCount, listings.Count - 1);
                    var freshIds = new HashSet<long>(
                        listings.Skip(actualRemove).Select(l => l.MarketId));

                    var expectedRemovedIds = new HashSet<long>(
                        listings.Take(actualRemove).Select(l => l.MarketId));

                    var metadata = new CharacterSyncMetadata
                    {
                        CharacterUUID = "char-1",
                        SystemId = 1,
                        RangeJas = 200,
                        SystemsInRange = new HashSet<int> { 1 },
                    };

                    // Act
                    svc.RemoveStaleOrders(freshIds, "char-1", metadata);

                    List<MarketListing> after = ctx.SnapshotMarketListingList();
                    var remainingIds = new HashSet<long>(
                        after.Where(a => a.MarketId.HasValue).Select(a => a.MarketId.Value));

                    // Assert: none of the expected-removed IDs remain
                    return expectedRemovedIds.All(id => !remainingIds.Contains(id));
                });
        }

        // -------------------------------------------------------------------
        // 9.4 Out-of-Range Preservation Property
        // **Validates: Requirements 1.5, 14.3**
        // Orders outside range are never removed or modified after sync.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OutOfRangePreservation_OrdersOutsideRangeNeverRemoved()
        {
            return Prop.ForAll(
                Arb.From(UniqueListingsGen(2, 10)),
                Arb.From(UniqueListingsGen(2, 10)),
                (inRangeListings, outOfRangeListings) =>
                {
                    // Arrange
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                        new StarSystem { Id = 99, Name = "FarAway", X = 9999m, Y = 9999m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    // Deduplicate across both sets
                    var usedIds = new HashSet<long>(inRangeListings.Select(l => l.MarketId));
                    var safeOutOfRange = outOfRangeListings
                        .Where(l => !usedIds.Contains(l.MarketId))
                        .ToList();
                    if (safeOutOfRange.Count == 0) return true;

                    // Seed in-range orders
                    var inDto = new ApiMarketListings { Listings = inRangeListings };
                    svc.ProcessMarketListings(inDto, "char-1", 1, 200);
                    foreach (var ml in ctx.SnapshotMarketListingList())
                    {
                        ml.SystemId = 1;
                    }

                    // Seed out-of-range orders (SystemId=99)
                    var outDto = new ApiMarketListings { Listings = safeOutOfRange };
                    svc.ProcessMarketListings(outDto, "char-2", 99, 200);
                    foreach (var ml in ctx.SnapshotMarketListingList()
                        .Where(m => safeOutOfRange.Any(s => s.MarketId == m.MarketId)))
                    {
                        ml.SystemId = 99;
                    }

                    ctx.InvalidateMarketListingByMarketIdCache();

                    var outOfRangeIds = new HashSet<long>(safeOutOfRange.Select(l => l.MarketId));

                    // Fresh results are empty — all in-range orders should be removed
                    var freshIds = new HashSet<long>();
                    var metadata = new CharacterSyncMetadata
                    {
                        CharacterUUID = "char-1",
                        SystemId = 1,
                        RangeJas = 50,
                        SystemsInRange = new HashSet<int> { 1 },
                    };

                    // Act
                    svc.RemoveStaleOrders(freshIds, "char-1", metadata);

                    List<MarketListing> after = ctx.SnapshotMarketListingList();
                    var remainingIds = new HashSet<long>(
                        after.Where(a => a.MarketId.HasValue).Select(a => a.MarketId.Value));

                    // Assert: all out-of-range orders still exist
                    return outOfRangeIds.All(id => remainingIds.Contains(id));
                });
        }

        // -------------------------------------------------------------------
        // 9.5 Deduplication by MarketId Property
        // **Validates: Requirements 1.2**
        // No two listings share the same non-null MarketId after syncs.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeduplicationByMarketId_NoTwoListingsShareSameMarketId()
        {
            return Prop.ForAll(
                Arb.From(UniqueListingsGen(2, 15)),
                Arb.From(UniqueListingsGen(2, 15)),
                (batch1, batch2) =>
                {
                    // Arrange
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    // Act — sync two batches (may have overlapping MarketIds)
                    var dto1 = new ApiMarketListings { Listings = batch1 };
                    svc.ProcessMarketListings(dto1, "char-1", 1, 200);

                    var dto2 = new ApiMarketListings { Listings = batch2 };
                    svc.ProcessMarketListings(dto2, "char-2", 1, 200);

                    // Assert — no duplicate non-null MarketIds
                    List<MarketListing> all = ctx.SnapshotMarketListingList();
                    var marketIds = all
                        .Where(m => m.MarketId.HasValue)
                        .Select(m => m.MarketId.Value)
                        .ToList();

                    return marketIds.Count == marketIds.Distinct().Count();
                });
        }

        // -------------------------------------------------------------------
        // 9.6 Private Sale Isolation Property
        // **Validates: Requirements 2.2, 2.3**
        // Private sale visible only to seller and named buyer.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PrivateSaleIsolation_VisibleOnlyToSellerAndNamedBuyer()
        {
            return Prop.ForAll(
                Arb.From(MarketIdGen()),
                Arb.From(Gen.Choose(1, 999).Select(p => (double)p / 10.0)),
                (marketId, price) =>
                {
                    // Arrange
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());
                    var repo = new SystemRepository();
                    repo.ReplaceAll(new List<StarSystem>
                    {
                        new StarSystem { Id = 1, Name = "Alpha", X = 0m, Y = 0m },
                    });
                    var grid = new SystemGridIndex(repo, 50);
                    var svc = new MarketDataService(ctx, grid);

                    string sellerUUID = "seller-uuid";
                    string buyerUUID = "buyer-uuid";
                    string unrelatedUUID = "unrelated-uuid";
                    string buyerName = "BuyerChar";

                    // Add player profiles so visibility resolution works
                    ctx.AddPlayerProfile(new PlayerProfile
                    {
                        UUID = sellerUUID,
                        Name = "SellerChar",
                    });
                    ctx.AddPlayerProfile(new PlayerProfile
                    {
                        UUID = buyerUUID,
                        Name = buyerName,
                    });
                    ctx.AddPlayerProfile(new PlayerProfile
                    {
                        UUID = unrelatedUUID,
                        Name = "OtherChar",
                    });

                    // Add a private sale listing
                    var privateListing = new MarketListing
                    {
                        UUID = Guid.NewGuid().ToString(),
                        MarketId = marketId,
                        BuyOrder = false,
                        PrivateSale = true,
                        OwnerUUID = sellerUUID,
                        SyncedByCharacterUUID = sellerUUID,
                        BuyerName = buyerName,
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        BaseItemTypeID = "Noble Gases",
                        ResourcePurity = "High",
                        ItemName = "Noble Gases (High)",
                        PricePerUnit = (decimal)price,
                        Quantity = 100,
                    };
                    ctx.AddMarketListing(privateListing);

                    // Act — get visible orders for each character
                    var sellerVisible = svc.GetVisibleOrders(sellerUUID);
                    var buyerVisible = svc.GetVisibleOrders(buyerUUID);
                    var unrelatedVisible = svc.GetVisibleOrders(unrelatedUUID);

                    // Assert
                    bool sellerSees = sellerVisible.Any(v => v.MarketId == marketId);
                    bool buyerSees = buyerVisible.Any(v => v.MarketId == marketId);
                    bool unrelatedDoesNotSee = !unrelatedVisible.Any(v => v.MarketId == marketId);

                    return sellerSees && buyerSees && unrelatedDoesNotSee;
                });
        }
    }
}
