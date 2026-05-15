using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ShipBuildService enhanced stats computation.
    /// Feature: ship-enhanced-stats
    /// </summary>
    [TestFixture]
    public class ShipStatsUnitTests
    {
        private static ReadOnlyBlueprint MakeHull(decimal mass, decimal fuelCap = 0m, decimal engAvail = 0m)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("TestHull")
            {
                UUID = "hull-1",
                BluePrintType = "Hull",
                Class = 4,
                Properties = new PropertyBag()
            };
            bp.Properties.SetProperty(BlueprintPropertyKeys.Mass, mass);
            bp.Properties.SetProperty(BlueprintPropertyKeys.FuelCapacity, fuelCap);
            bp.Properties.SetProperty(BlueprintPropertyKeys.EngCapacityAvailable, engAvail);
            return new ReadOnlyBlueprint(bp);
        }

        private static ReadOnlyBlueprint MakeComponent(
            string uuid, string type, decimal mass,
            decimal engReq = 0m, decimal pp = 0m, decimal rr = 0m,
            decimal draw = 0m, decimal accel = 0m, decimal rot = 0m,
            decimal fuelPerJAS = 0m, decimal chargeTime = 0m)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Comp") { UUID = uuid, BluePrintType = type, Properties = new PropertyBag() };
            bp.Properties.SetProperty(BlueprintPropertyKeys.Mass, mass);
            if (engReq > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.EngCapacityRequired, engReq);
            if (pp > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerProvided, pp);
            if (rr > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerRegenRate, rr);
            if (draw > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerDrawPerSecond, draw);
            if (accel > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.Acceleration, accel);
            if (rot > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.RotationalThrust, rot);
            if (fuelPerJAS > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.FuelPerJASPerMass, fuelPerJAS);
            if (chargeTime > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.JumpChargeTime, chargeTime);
            return new ReadOnlyBlueprint(bp);
        }

        private static ShipStats ComputeWith(ReadOnlyBlueprint hull, params ReadOnlyBlueprint[] comps)
        {
            var lookup = comps.ToDictionary(c => c.UUID, c => c);
            var slots = comps.Select((c, i) => new ShipComponentSlot
            {
                SlotType = c.BluePrintType,
                SlotIndex = i,
                BlueprintUUID = c.UUID
            }).ToList();
            return ShipBuildService.ComputeStats(hull, slots, uuid => lookup.ContainsKey(uuid) ? lookup[uuid] : null);
        }

        /// <summary>6.1: Zero-component build (hull only).</summary>
        [Test]
        public void HullOnly_AllDerivedStatsAreZero()
        {
            var hull = MakeHull(1000m, 500m, 800m);
            var stats = ShipBuildService.ComputeStats(hull, null, id => null);

            Assert.That(stats.TotalMass, Is.EqualTo(1000m));
            Assert.That(stats.ShipType, Is.EqualTo("TestHull"));
            Assert.That(stats.ShipClass, Is.EqualTo(4));
            Assert.That(stats.AccelerationFactor, Is.EqualTo(0m));
            Assert.That(stats.TurnRate, Is.EqualTo(0m));
            Assert.That(stats.JumpFuelPerJAS, Is.EqualTo(0m));
            Assert.That(stats.JumpFuelRange, Is.EqualTo(0m));
            Assert.That(stats.ShieldUptime, Is.EqualTo(0m));
            Assert.That(stats.WeaponSustainTime, Is.EqualTo(0m));
            Assert.That(stats.EngCapacityAvailable, Is.EqualTo(800m));
        }

        /// <summary>6.2: Full combat build.</summary>
        [Test]
        public void FullCombatBuild_SustainabilityComputedCorrectly()
        {
            var hull = MakeHull(1000m, 500m, 2000m);
            var reactor = MakeComponent("r1", BlueprintTypes.Reactor, 50m, pp: 1000m, rr: 20m);
            var shield = MakeComponent("s1", BlueprintTypes.Shield, 30m, draw: 25m);
            var weapon = MakeComponent("w1", "Beamer/Small", 20m, draw: 15m);

            var stats = ComputeWith(hull, reactor, shield, weapon);

            // Shield: rr(20) < draw(25), uptime = pp(1000) / (25-20) = 200
            Assert.That(stats.ShieldUptime, Is.EqualTo(200m));
            // Weapon: rr(20) > draw(15), sustainable
            Assert.That(stats.WeaponSustainTime, Is.EqualTo(-1m));
        }

        /// <summary>6.3: Mining build.</summary>
        [Test]
        public void MiningBuild_SustainabilityPerType()
        {
            var hull = MakeHull(1000m, 500m, 2000m);
            var reactor = MakeComponent("r1", BlueprintTypes.Reactor, 50m, pp: 500m, rr: 30m);
            var laser1 = MakeComponent("l1", BlueprintTypes.MiningLaser, 20m, draw: 10m);
            var laser2 = MakeComponent("l2", BlueprintTypes.MiningLaser, 20m, draw: 10m);

            var stats = ComputeWith(hull, reactor, laser1, laser2);

            Assert.That(stats.MiningSustainByType.Count, Is.EqualTo(1));
            Assert.That(stats.MiningSustainByType[0].Count, Is.EqualTo(2));
            // SustainableCount = rr(30) / draw(10) = 3
            Assert.That(stats.MiningSustainByType[0].SustainableCount, Is.EqualTo(3m));
        }

        /// <summary>6.4: Jump build.</summary>
        [Test]
        public void JumpBuild_RangeAndChargeTimePopulated()
        {
            var hull = MakeHull(1000m, 500m, 2000m);
            var reactor = MakeComponent("r1", BlueprintTypes.Reactor, 50m, pp: 500m, rr: 20m);
            var jd = MakeComponent("jd1", BlueprintTypes.JumpDrive, 100m, fuelPerJAS: 0.5m);
            var nc = MakeComponent("nc1", BlueprintTypes.NavComp, 30m, chargeTime: 12m);

            var stats = ComputeWith(hull, reactor, jd, nc);

            // TotalMass = 1000+50+100+30 = 1180
            // JumpFuelPerJAS = 0.5 * 1180 = 590
            Assert.That(stats.JumpFuelPerJAS, Is.EqualTo(590m));
            // JumpFuelRange = 500 / 590
            Assert.That(stats.JumpFuelRange, Is.EqualTo(500m / 590m));
            Assert.That(stats.JumpChargeTime, Is.EqualTo(12m));
        }

        /// <summary>6.5: Over-budget engineering.</summary>
        [Test]
        public void OverBudgetEngineering_UsedExceedsAvailable()
        {
            var hull = MakeHull(1000m, 500m, 100m); // only 100 eng available
            var comp1 = MakeComponent("c1", BlueprintTypes.Reactor, 50m, engReq: 60m);
            var comp2 = MakeComponent("c2", BlueprintTypes.Shield, 30m, engReq: 50m);

            var stats = ComputeWith(hull, comp1, comp2);

            Assert.That(stats.EngCapacityUsed, Is.EqualTo(110m));
            Assert.That(stats.EngCapacityAvailable, Is.EqualTo(100m));
            Assert.That(stats.EngCapacityUsed, Is.GreaterThan(stats.EngCapacityAvailable));
        }

        /// <summary>6.6: Sustainable shields.</summary>
        [Test]
        public void SustainableShields_UptimeIsNegativeOne()
        {
            var hull = MakeHull(1000m, 500m, 2000m);
            var reactor = MakeComponent("r1", BlueprintTypes.Reactor, 50m, pp: 1000m, rr: 50m);
            var shield = MakeComponent("s1", BlueprintTypes.Shield, 30m, draw: 10m);

            var stats = ComputeWith(hull, reactor, shield);

            // rr(50) >= draw(10), sustainable
            Assert.That(stats.ShieldUptime, Is.EqualTo(-1m));
        }

        /// <summary>6.7: Unsustainable weapons.</summary>
        [Test]
        public void UnsustainableWeapons_SustainTimeIsFinite()
        {
            var hull = MakeHull(1000m, 500m, 2000m);
            var reactor = MakeComponent("r1", BlueprintTypes.Reactor, 50m, pp: 500m, rr: 5m);
            var w1 = MakeComponent("w1", "Beamer/Small", 20m, draw: 10m);
            var w2 = MakeComponent("w2", "Beamer/Small", 20m, draw: 10m);
            var w3 = MakeComponent("w3", "Beamer/Small", 20m, draw: 10m);

            var stats = ComputeWith(hull, reactor, w1, w2, w3);

            // TotalWeaponDraw = 30, rr = 5, pp = 500
            // WeaponSustainTime = 500 / (30 - 5) = 20
            Assert.That(stats.WeaponSustainTime, Is.EqualTo(20m));
            Assert.That(stats.WeaponSustainTime, Is.GreaterThan(0m));
        }
    }
}
