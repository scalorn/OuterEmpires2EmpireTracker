using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for MarketListingService.
    /// Feature: bl-114-market-readonly, Properties 1-4 from the design document.
    /// </summary>
    [TestFixture]
    public class MarketListingServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generators
        // -----------------------------------------------------------------------

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<ItemType.ItemTypeEnum> ItemTypeGen()
        {
            return Gen.Elements(
                ItemType.ItemTypeEnum.None,
                ItemType.ItemTypeEnum.Commodity,
                ItemType.ItemTypeEnum.Resource,
                ItemType.ItemTypeEnum.Blueprint);
        }

        private static Gen<MarketListingCreateRequest> CreateRequestGen()
        {
            return from itemName in SafeStringGen()
                   from itemType in ItemTypeGen()
                   from itemRefId in SafeStringGen()
                   from stationUUID in SafeStringGen()
                   from qty in Gen.Choose(0, 1000)
                   from ppu in Gen.Choose(0, 10000).Select(i => (decimal)i / 100m)
                   from chp in Gen.Choose(0, 100)
                   from mhp in Gen.Choose(0, 100)
                   from mrp in Gen.Choose(0, 100).Select(i => (decimal)i)
                   select new MarketListingCreateRequest
                   {
                       ItemName = itemName,
                       ItemType = itemType,
                       ItemReferenceID = itemRefId,
                       StationUUID = stationUUID,
                       Quantity = qty,
                       PricePerUnit = ppu,
                       CurrentHP = chp,
                       MaxHP = mhp,
                       MaxRepairPercent = mrp,
                   };
        }

        private static Gen<MarketListingUpdateRequest> UpdateRequestGen()
        {
            return from itemName in SafeStringGen()
                   from itemType in ItemTypeGen()
                   from itemRefId in SafeStringGen()
                   from stationUUID in SafeStringGen()
                   from qty in Gen.Choose(0, 1000)
                   from ppu in Gen.Choose(0, 10000).Select(i => (decimal)i / 100m)
                   from chp in Gen.Choose(0, 100)
                   from mhp in Gen.Choose(0, 100)
                   from mrp in Gen.Choose(0, 100).Select(i => (decimal)i)
                   select new MarketListingUpdateRequest
                   {
                       ItemName = itemName,
                       ItemType = itemType,
                       ItemReferenceID = itemRefId,
                       StationUUID = stationUUID,
                       Quantity = qty,
                       PricePerUnit = ppu,
                       CurrentHP = chp,
                       MaxHP = mhp,
                       MaxRepairPercent = mrp,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 1: CreateListing Round-Trip
        // Feature: bl-114-market-readonly, Property 1
        // **Validates: Requirements 4.2, 4.4, 4.9**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property CreateListing_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new MarketListingService(ctx);

                var result = svc.CreateListing(request);

                bool uuidOk = !string.IsNullOrEmpty(result.UUID);
                bool nameOk = result.ItemName == request.ItemName;
                bool typeOk = result.ItemType == request.ItemType;
                bool refIdOk = result.ItemReferenceID == request.ItemReferenceID;
                bool stationOk = result.StationUUID == request.StationUUID;
                bool qtyOk = result.Quantity == request.Quantity;
                bool ppuOk = result.PricePerUnit == request.PricePerUnit;
                bool chpOk = result.CurrentHP == request.CurrentHP;
                bool mhpOk = result.MaxHP == request.MaxHP;
                bool mrpOk = result.MaxRepairPercent == request.MaxRepairPercent;

                return (uuidOk && nameOk && typeOk && refIdOk && stationOk && qtyOk && ppuOk && chpOk && mhpOk && mrpOk)
                    .Label("uuid=" + uuidOk + " name=" + nameOk + " type=" + typeOk + " qty=" + qtyOk + " ppu=" + ppuOk);
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: UpdateListing Round-Trip
        // Feature: bl-114-market-readonly, Property 2
        // **Validates: Requirements 5.3, 5.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property UpdateListing_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new MarketListingService(ctx);

                // Seed a listing
                var seed = new MarketListingCreateRequest { ItemName = "Seed", Quantity = 1 };
                var created = svc.CreateListing(seed);

                var result = svc.UpdateListing(created.UUID, request);

                bool nameOk = result.ItemName == request.ItemName;
                bool typeOk = result.ItemType == request.ItemType;
                bool refIdOk = result.ItemReferenceID == request.ItemReferenceID;
                bool stationOk = result.StationUUID == request.StationUUID;
                bool qtyOk = result.Quantity == request.Quantity;
                bool ppuOk = result.PricePerUnit == request.PricePerUnit;
                bool chpOk = result.CurrentHP == request.CurrentHP;
                bool mhpOk = result.MaxHP == request.MaxHP;
                bool mrpOk = result.MaxRepairPercent == request.MaxRepairPercent;

                return (nameOk && typeOk && refIdOk && stationOk && qtyOk && ppuOk && chpOk && mhpOk && mrpOk)
                    .Label("name=" + nameOk + " type=" + typeOk + " qty=" + qtyOk + " ppu=" + ppuOk);
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: DeleteListing Removes Listing
        // Feature: bl-114-market-readonly, Property 3
        // **Validates: Requirements 6.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property DeleteListing_RemovesListing()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new MarketListingService(ctx);

                var created = svc.CreateListing(request);
                string uuid = created.UUID;

                svc.DeleteListing(uuid);

                var found = ctx.FindMutableMarketListing(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Listing still exists after Delete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 4: RecordSale Decrements Quantity and Creates Transaction
        // Feature: bl-114-market-readonly, Property 4
        // **Validates: Requirements 7.3, 7.4, 7.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property RecordSale_DecrementsQuantityAndCreatesTransaction()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 100)),
                Arb.From(Gen.Choose(0, 10000).Select(i => (decimal)i / 100m)),
                (saleQty, ppu) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new MarketListingService(ctx);

                int listingQty = saleQty + 10; // Ensure enough quantity
                var createReq = new MarketListingCreateRequest
                {
                    ItemName = "TestItem",
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    Quantity = listingQty,
                    PricePerUnit = 5.0m,
                    StationUUID = "station-1",
                };
                var created = svc.CreateListing(createReq);

                var tx = svc.RecordSale(
                    created.UUID,
                    saleQty,
                    ppu,
                    "Buyer",
                    "Faction",
                    "station-1");

                if (tx == null)
                    return false.Label("Transaction was null");

                var updatedListing = ctx.FindMutableMarketListing(created.UUID);
                bool qtyDecremented = updatedListing.Quantity == listingQty - saleQty;
                bool txQtyOk = tx.Quantity == saleQty;
                bool txPpuOk = tx.PricePerUnit == ppu;
                bool txTotalOk = tx.TotalPrice == ppu * saleQty;
                bool txTypeOk = tx.TransactionType == TransactionType.Sell;
                bool txItemOk = tx.ItemName == "TestItem";

                return (qtyDecremented && txQtyOk && txPpuOk && txTotalOk && txTypeOk && txItemOk)
                    .Label("qtyDec=" + qtyDecremented + " txQty=" + txQtyOk + " txPpu=" + txPpuOk + " txTotal=" + txTotalOk + " txType=" + txTypeOk);
            });
        }
    }
}
