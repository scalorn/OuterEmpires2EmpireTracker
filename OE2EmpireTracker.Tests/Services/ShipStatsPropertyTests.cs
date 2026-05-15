using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for ShipBuildService enhanced stats computation.
    /// Feature: ship-enhanced-stats
    /// </summary>
    [TestFixture]
    public class ShipStatsPropertyTests
    {
        private static ReadOnlyBlueprint CreateHullBp(decimal mass, decimal fuelCapacity, decimal engCapAvail)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("TestHull")
            {
                UUID = Guid.NewGuid().ToString(),
                BluePrintType = "Hull",
                Class = 3,
                Properties = new PropertyBag()
            };
            bp.Properties.SetProperty(BlueprintPropertyKeys.Mass, mass);
            bp.Properties.SetProperty(BlueprintPropertyKeys.FuelCapacity, fuelCapacity);
            bp.Properties.SetProperty(BlueprintPropertyKeys.EngCapacityAvailable, engCapAvail);
            return new ReadOnlyBlueprint(bp);
        }

        private static ReadOnlyBlueprint CreateComponentBp(
            string type,
            decimal mass,
            decimal engReq = 0m,
            decimal powerProvided = 0m,
            decimal powerRegenRate = 0m,
            decimal powerDraw = 0m,
            decimal acceleration = 0m,
            decimal rotThrust = 0m,
            decimal fuelPerJASPerMass = 0m,
            decimal jumpChargeTime = 0m)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Component")
            {
                UUID = Guid.NewGuid().ToString(),
                BluePrintType = type,
                Properties = new PropertyBag()
            };
            bp.Properties.SetProperty(BlueprintPropertyKeys.Mass, mass);
            bp.Properties.SetProperty(BlueprintPropertyKeys.EngCapacityRequired, engReq);
            if (powerProvided > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerProvided, powerProvided);
            if (powerRegenRate > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerRegenRate, powerRegenRate);
            if (powerDraw > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.PowerDrawPerSecond, powerDraw);
            if (acceleration > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.Acceleration, acceleration);
            if (rotThrust > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.RotationalThrust, rotThrust);
            if (fuelPerJASPerMass > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.FuelPerJASPerMass, fuelPerJASPerMass);
            if (jumpChargeTime > 0) bp.Properties.SetProperty(BlueprintPropertyKeys.JumpChargeTime, jumpChargeTime);
            return new ReadOnlyBlueprint(bp);
        }

        private static ShipStats Compute(ReadOnlyBlueprint hull, params ReadOnlyBlueprint[] components)
        {
            var lookup = components.ToDictionary(c => c.UUID, c => c);
            var slots = components.Select((c, i) => new ShipComponentSlot
            {
                SlotType = c.BluePrintType,
                SlotIndex = i,
                BlueprintUUID = c.UUID
            }).ToList();
            return ShipBuildService.ComputeStats(hull, slots, uuid => lookup.ContainsKey(uuid) ? lookup[uuid] : null);
        }

        /// <summary>
        /// Property 1: Additive stats are sums of component values.
        /// **Validates: Requirements 2.1, 7.1, 7.2, 9.1, 12.1, 14.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AdditiveStatsAreSumsOfComponentValues()
        {
            var gen =
                from hullMass in Gen.Choose(100, 5000).Select(x => (decimal)x)
                from reactorMass in Gen.Choose(10, 200).Select(x => (decimal)x)
                from shieldMass in Gen.Choose(10, 100).Select(x => (decimal)x)
                from weaponMass in Gen.Choose(10, 100).Select(x => (decimal)x)
                from engReq1 in Gen.Choose(10, 100).Select(x => (decimal)x)
                from engReq2 in Gen.Choose(10, 100).Select(x => (decimal)x)
                from engReq3 in Gen.Choose(10, 100).Select(x => (decimal)x)
                from pp in Gen.Choose(100, 2000).Select(x => (decimal)x)
                from rr in Gen.Choose(1, 50).Select(x => (decimal)x)
                from shieldDraw in Gen.Choose(1, 30).Select(x => (decimal)x)
                from weaponDraw in Gen.Choose(1, 30).Select(x => (decimal)x)
                select new { hullMass, reactorMass, shieldMass, weaponMass, engReq1, engReq2, engReq3, pp, rr, shieldDraw, weaponDraw };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(data.hullMass, 500m, 1000m);
                var reactor = CreateComponentBp(BlueprintTypes.Reactor, data.reactorMass, data.engReq1, data.pp, data.rr);
                var shield = CreateComponentBp(BlueprintTypes.Shield, data.shieldMass, data.engReq2, powerDraw: data.shieldDraw);
                var weapon = CreateComponentBp("Beamer/Small", data.weaponMass, data.engReq3, powerDraw: data.weaponDraw);
                var stats = Compute(hull, reactor, shield, weapon);

                var expectedMass = data.hullMass + data.reactorMass + data.shieldMass + data.weaponMass;
                var expectedEng = data.engReq1 + data.engReq2 + data.engReq3;

                return stats.TotalMass == expectedMass
                    && stats.EngCapacityUsed == expectedEng
                    && stats.PowerProvided == data.pp
                    && stats.PowerRegenRate == data.rr
                    && stats.ShieldPowerDraw == data.shieldDraw
                    && stats.TotalWeaponPowerDraw == data.weaponDraw;
            });
        }

        /// <summary>
        /// Property 2: Acceleration factor formula.
        /// **Validates: Requirements 3.1, 3.2, 9.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AccelerationFactorFormula()
        {
            var gen =
                from hullMass in Gen.Choose(500, 5000).Select(x => (decimal)x)
                from driveMass in Gen.Choose(10, 200).Select(x => (decimal)x)
                from accel in Gen.Choose(0, 500).Select(x => (decimal)x)
                select new { hullMass, driveMass, accel };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(data.hullMass, 200m, 1000m);
                var drive = CreateComponentBp("MainDrive", data.driveMass, acceleration: data.accel);
                var stats = Compute(hull, drive);

                var totalMass = data.hullMass + data.driveMass;
                var expected = data.accel > 0 && totalMass > 0 ? data.accel / totalMass : 0m;
                return stats.AccelerationFactor == expected;
            });
        }

        /// <summary>
        /// Property 3: Turn rate formula.
        /// **Validates: Requirements 4.1, 4.2, 9.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TurnRateFormula()
        {
            var gen =
                from hullMass in Gen.Choose(500, 5000).Select(x => (decimal)x)
                from thrusterMass in Gen.Choose(10, 100).Select(x => (decimal)x)
                from rotThrust in Gen.Choose(0, 300).Select(x => (decimal)x)
                select new { hullMass, thrusterMass, rotThrust };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(data.hullMass, 200m, 1000m);
                var thruster = CreateComponentBp("Thruster", data.thrusterMass, rotThrust: data.rotThrust);
                var stats = Compute(hull, thruster);

                var totalMass = data.hullMass + data.thrusterMass;
                var expected = data.rotThrust > 0 && totalMass > 0 ? data.rotThrust / totalMass : 0m;
                return stats.TurnRate == expected;
            });
        }

        /// <summary>
        /// Property 4: Jump fuel per JAS formula.
        /// **Validates: Requirements 5.1, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property JumpFuelPerJASFormula()
        {
            var gen =
                from hullMass in Gen.Choose(500, 5000).Select(x => (decimal)x)
                from jdMass in Gen.Choose(10, 200).Select(x => (decimal)x)
                from fuelPerJAS in Gen.Choose(0, 100).Select(x => x / 100m)
                select new { hullMass, jdMass, fuelPerJAS };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(data.hullMass, 500m, 1000m);
                var jd = CreateComponentBp(BlueprintTypes.JumpDrive, data.jdMass, fuelPerJASPerMass: data.fuelPerJAS);
                var stats = Compute(hull, jd);

                var totalMass = data.hullMass + data.jdMass;
                var expected = data.fuelPerJAS * totalMass;
                return stats.JumpFuelPerJAS == expected;
            });
        }

        /// <summary>
        /// Property 5: Jump fuel range formula.
        /// **Validates: Requirements 5.2, 5.3, 9.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property JumpFuelRangeFormula()
        {
            var gen =
                from hullMass in Gen.Choose(500, 5000).Select(x => (decimal)x)
                from jdMass in Gen.Choose(10, 200).Select(x => (decimal)x)
                from fuelCap in Gen.Choose(100, 2000).Select(x => (decimal)x)
                from fuelPerJAS in Gen.Choose(1, 100).Select(x => x / 100m)
                select new { hullMass, jdMass, fuelCap, fuelPerJAS };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(data.hullMass, data.fuelCap, 1000m);
                var jd = CreateComponentBp(BlueprintTypes.JumpDrive, data.jdMass, fuelPerJASPerMass: data.fuelPerJAS);
                var stats = Compute(hull, jd);

                var totalMass = data.hullMass + data.jdMass;
                var jfpj = data.fuelPerJAS * totalMass;
                var expectedRange = jfpj > 0 ? data.fuelCap / jfpj : 0m;
                return stats.JumpFuelRange == expectedRange;
            });
        }

        /// <summary>
        /// Property 6: Shield uptime formula.
        /// **Validates: Requirements 12.2, 12.3, 7.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ShieldUptimeFormula()
        {
            var gen =
                from pp in Gen.Choose(100, 2000).Select(x => (decimal)x)
                from rr in Gen.Choose(1, 100).Select(x => (decimal)x)
                from shieldDraw in Gen.Choose(1, 100).Select(x => (decimal)x)
                select new { pp, rr, shieldDraw };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(1000m, 500m, 1000m);
                var reactor = CreateComponentBp(BlueprintTypes.Reactor, 50m, powerProvided: data.pp, powerRegenRate: data.rr);
                var shield = CreateComponentBp(BlueprintTypes.Shield, 30m, powerDraw: data.shieldDraw);
                var stats = Compute(hull, reactor, shield);

                if (data.rr >= data.shieldDraw)
                {
                    return stats.ShieldUptime == -1m;
                }

                var expected = data.pp / (data.shieldDraw - data.rr);
                return stats.ShieldUptime == expected;
            });
        }

        /// <summary>
        /// Property 7: Mining sustainability formula.
        /// **Validates: Requirements 13.2, 13.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MiningSustainabilityFormula()
        {
            var gen =
                from rr in Gen.Choose(1, 100).Select(x => (decimal)x)
                from laserDraw in Gen.Choose(1, 50).Select(x => (decimal)x)
                select new { rr, laserDraw };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(1000m, 500m, 1000m);
                var reactor = CreateComponentBp(BlueprintTypes.Reactor, 50m, powerProvided: 500m, powerRegenRate: data.rr);
                var laser = CreateComponentBp(BlueprintTypes.MiningLaser, 20m, powerDraw: data.laserDraw);
                var stats = Compute(hull, reactor, laser);

                var expectedSustain = data.laserDraw > 0 ? data.rr / data.laserDraw : 0m;
                return stats.MiningSustainByType.Count == 1
                    && stats.MiningSustainByType[0].SustainableCount == expectedSustain;
            });
        }

        /// <summary>
        /// Property 8: Weapon sustainability formula.
        /// **Validates: Requirements 14.2, 14.4, 14.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WeaponSustainabilityFormula()
        {
            var gen =
                from pp in Gen.Choose(100, 2000).Select(x => (decimal)x)
                from rr in Gen.Choose(1, 100).Select(x => (decimal)x)
                from weaponDraw in Gen.Choose(1, 50).Select(x => (decimal)x)
                from weaponCount in Gen.Choose(1, 4)
                select new { pp, rr, weaponDraw, weaponCount };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hull = CreateHullBp(1000m, 500m, 1000m);
                var reactor = CreateComponentBp(BlueprintTypes.Reactor, 50m, powerProvided: data.pp, powerRegenRate: data.rr);
                var weapons = Enumerable.Range(0, data.weaponCount)
                    .Select(_ => CreateComponentBp("Beamer/Small", 20m, powerDraw: data.weaponDraw))
                    .ToArray();

                var allComponents = new[] { reactor }.Concat(weapons).ToArray();
                var stats = Compute(hull, allComponents);

                var totalDraw = data.weaponDraw * data.weaponCount;
                var expectedPerType = data.weaponDraw > 0 ? data.rr / data.weaponDraw : 0m;

                bool aggregateCorrect;
                if (data.rr >= totalDraw)
                {
                    aggregateCorrect = stats.WeaponSustainTime == -1m;
                }
                else
                {
                    aggregateCorrect = stats.WeaponSustainTime == data.pp / (totalDraw - data.rr);
                }

                var perTypeCorrect = stats.WeaponSustainByType.Count == 1
                    && stats.WeaponSustainByType[0].SustainableCount == expectedPerType
                    && stats.WeaponSustainByType[0].Count == data.weaponCount;

                return aggregateCorrect && perTypeCorrect;
            });
        }
    }
}
