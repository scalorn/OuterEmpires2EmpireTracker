// <copyright file="BlueprintLinkageDiscoveryTest.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    using Blueprint = OE2EmpireTracker.Models.Blueprint;

    /// <summary>
    /// Test to verify FsCheck works when invoked manually via Prop.ForAll instead of [Property] attribute.
    /// </summary>
    [TestFixture]
    public class BlueprintLinkageDiscoveryTest
    {
        [Test]
        public void DiscoveryTest_FsCheckManualInvocation()
        {
            Prop.ForAll<PositiveInt>(x =>
            {
                TestHelper.ResetWithCachedData();
                var pc = PlayerContext.GetInstance();
                var ec = EmpireContext.GetInstance();
                pc.CurrentPlayerUUID = "test";
                var svc = new BlueprintLinkageService(pc, ec);
                var item = new GameApiAssetCargoItem
                {
                    TypeC = "Bp",
                    ResourceName = "Test " + x.Get,
                    Evolution = 1,
                    ShipPartType = "Sh",
                    Properties = new List<GameApiAssetItemProperty>
                    {
                        new GameApiAssetItemProperty
                        {
                            ModTypeId = 1,
                            PropertyName = "test",
                            FriendlyPropertyName = "Test",
                            PropertyValue = 10.0m,
                            OriginalPropertyValue = 5.0m,
                            Unit = "%",
                        },
                    },
                };
                var localItem = new Item { UUID = "uuid-" + x.Get };
                bool result = svc.ProcessItem(item, localItem, "owner");
                return result.ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
