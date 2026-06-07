// <copyright file="AssetTypeCodesTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Constants
{
    [TestFixture]
    public class AssetTypeCodesTests
    {
        // -------------------------------------------------------------------
        // Cargo Item TypeC Constants (swagger-confirmed)
        // -------------------------------------------------------------------

        [Test]
        public void Blueprint_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Blueprint, Is.EqualTo("Bp"));
        }

        [Test]
        public void Crate_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Crate, Is.EqualTo("Cr"));
        }

        [Test]
        public void Survey_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Survey, Is.EqualTo("Sc"));
        }

        [Test]
        public void ShipPart_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.ShipPart, Is.EqualTo("S"));
        }

        [Test]
        public void Flatpack_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Flatpack, Is.EqualTo("F"));
        }

        [Test]
        public void Workforce_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Workforce, Is.EqualTo("W"));
        }

        [Test]
        public void Resource_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Resource, Is.EqualTo("R"));
        }

        [Test]
        public void Ammunition_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Ammunition, Is.EqualTo("A"));
        }

        [Test]
        public void Deployable_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Deployable, Is.EqualTo("D"));
        }

        [Test]
        public void Share_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Share, Is.EqualTo("Sh"));
        }

        [Test]
        public void CommodityL_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.CommodityL, Is.EqualTo("L"));
        }

        // -------------------------------------------------------------------
        // Cargo Item TypeC Constants (discovered in real API data)
        // -------------------------------------------------------------------

        [Test]
        public void Commodity_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Commodity, Is.EqualTo("C"));
        }

        [Test]
        public void ShipHull_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.ShipHull, Is.EqualTo("SH"));
        }

        // -------------------------------------------------------------------
        // Location TypeC Constants
        // -------------------------------------------------------------------

        [Test]
        public void Colony_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Colony, Is.EqualTo("Co"));
        }

        [Test]
        public void Station_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Station, Is.EqualTo("St"));
        }

        [Test]
        public void Ship_HasExpectedValue()
        {
            Assert.That(AssetTypeCodes.Ship, Is.EqualTo("Sh"));
        }

        // -------------------------------------------------------------------
        // MapAssetTypeC — maps each cargo constant to correct ItemTypeEnum
        // -------------------------------------------------------------------

        [Test]
        public void MapAssetTypeC_Blueprint_ReturnsBlueprint()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Blueprint),
                Is.EqualTo(ItemType.ItemTypeEnum.Blueprint));
        }

        [Test]
        public void MapAssetTypeC_Crate_ReturnsCrate()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Crate),
                Is.EqualTo(ItemType.ItemTypeEnum.Crate));
        }

        [Test]
        public void MapAssetTypeC_Survey_ReturnsSurvey()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Survey),
                Is.EqualTo(ItemType.ItemTypeEnum.Survey));
        }

        [Test]
        public void MapAssetTypeC_ShipPart_ReturnsShipPart()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.ShipPart),
                Is.EqualTo(ItemType.ItemTypeEnum.ShipPart));
        }

        [Test]
        public void MapAssetTypeC_Flatpack_ReturnsFlatpack()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Flatpack),
                Is.EqualTo(ItemType.ItemTypeEnum.Flatpack));
        }

        [Test]
        public void MapAssetTypeC_Workforce_ReturnsWorkDetail()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Workforce),
                Is.EqualTo(ItemType.ItemTypeEnum.WorkDetail));
        }

        [Test]
        public void MapAssetTypeC_Resource_ReturnsResource()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Resource),
                Is.EqualTo(ItemType.ItemTypeEnum.Resource));
        }

        [Test]
        public void MapAssetTypeC_Ammunition_ReturnsMunition()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Ammunition),
                Is.EqualTo(ItemType.ItemTypeEnum.Munition));
        }

        [Test]
        public void MapAssetTypeC_Commodity_ReturnsCommodity()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Commodity),
                Is.EqualTo(ItemType.ItemTypeEnum.Commodity));
        }

        [Test]
        public void MapAssetTypeC_Share_ReturnsShare()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.Share),
                Is.EqualTo(ItemType.ItemTypeEnum.Share));
        }

        [Test]
        public void MapAssetTypeC_ShipHull_ReturnsShipHull()
        {
            Assert.That(
                AssetMergeService.MapAssetTypeC(AssetTypeCodes.ShipHull),
                Is.EqualTo(ItemType.ItemTypeEnum.ShipHull));
        }
    }
}
