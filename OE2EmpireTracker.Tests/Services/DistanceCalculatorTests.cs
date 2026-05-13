// <copyright file="DistanceCalculatorTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Tests.Models;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based and unit tests for DistanceCalculator.
    /// Feature: systems-model
    /// </summary>
    [TestFixture]
    public class DistanceCalculatorTests
    {
        private static readonly JsonSerializerSettings CompactSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        private readonly List<string> _tempFiles = new List<string>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            _tempFiles.Clear();
        }

        // ---------------------------------------------------------------
        // Helper: Load a SystemRepository from a list of StarSystems
        // ---------------------------------------------------------------

        private SystemRepository LoadRepo(List<StarSystem> systems)
        {
            var tempFile = Path.GetTempFileName();
            _tempFiles.Add(tempFile);
            var json = JsonConvert.SerializeObject(systems, CompactSettings);
            File.WriteAllText(tempFile, json);
            var repo = new SystemRepository();
            repo.Load(tempFile);
            return repo;
        }

        // ---------------------------------------------------------------
        // Property 8: Distance is symmetric, non-negative, and zero for
        // identical points
        // **Validates: Requirements 4.1**
        // ---------------------------------------------------------------

        [Test]
        public void Property8_Distance_IsSymmetric()
        {
            var arb = StarSystemTests.StarSystemArbitraries.StarSystemArbitrary();

            var prop = Prop.ForAll(arb, arb, (a, b) =>
            {
                decimal distAB = DistanceCalculator.Calculate(a, b);
                decimal distBA = DistanceCalculator.Calculate(b, a);
                return distAB == distBA;
            });

            prop.QuickCheckThrowOnFailure();
        }

        [Test]
        public void Property8_Distance_IsNonNegative()
        {
            var arb = StarSystemTests.StarSystemArbitraries.StarSystemArbitrary();

            var prop = Prop.ForAll(arb, arb, (a, b) =>
            {
                decimal dist = DistanceCalculator.Calculate(a, b);
                return dist >= 0;
            });

            prop.QuickCheckThrowOnFailure();
        }

        [Test]
        public void Property8_Distance_IdenticalPoints_ReturnsZero()
        {
            var arb = StarSystemTests.StarSystemArbitraries.StarSystemArbitrary();

            var prop = Prop.ForAll(arb, a =>
            {
                decimal dist = DistanceCalculator.Calculate(a, a);
                return dist == 0m;
            });

            prop.QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 9: Route distance equals sum of consecutive leg
        // distances
        // **Validates: Requirements 4.6**
        // ---------------------------------------------------------------

        [Test]
        public void Property9_RouteDistance_EqualsSumOfLegs()
        {
            var systemsGen = Gen.Choose(2, 10).SelectMany(count =>
                Gen.ListOf(count, Arb.From(StarSystemTests.StarSystemArbitraries.StarSystemArbitrary()).Generator)
                   .Select(systems =>
                   {
                       var list = systems.ToList();
                       for (int i = 0; i < list.Count; i++)
                       {
                           list[i].Id = i + 1;
                           list[i].Name = $"RouteSystem_{i + 1}";
                       }

                       return list;
                   }));

            var arb = Arb.From(systemsGen, Shrink.Default<List<StarSystem>>());

            var prop = Prop.ForAll(arb, systems =>
            {
                var repo = LoadRepo(systems);
                var ids = systems.Select(s => s.Id).ToList();

                decimal routeDistance = DistanceCalculator.CalculateRoute(ids, repo);

                decimal sumOfLegs = 0;
                for (int i = 0; i < systems.Count - 1; i++)
                {
                    sumOfLegs += DistanceCalculator.Calculate(systems[i], systems[i + 1]);
                }

                return Math.Abs(routeDistance - sumOfLegs) < 0.0000001m;
            });

            prop.QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Unit tests: DistanceCalculator error cases
        // Satisfies: Req 4, Criteria 4, 7
        // ---------------------------------------------------------------

        [Test]
        public void Calculate_UnresolvableSystemId_ReturnsNegativeOne()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "Alpha", X = 10m, Y = 20m },
            };
            var repo = LoadRepo(systems);

            decimal result = DistanceCalculator.Calculate(1, 999, repo);

            Assert.That(result, Is.EqualTo(-1m));
        }

        [Test]
        public void Calculate_NullSystemA_ReturnsNegativeOne()
        {
            var b = new StarSystem { Id = 2, Name = "Beta", X = 5m, Y = 5m };

            decimal result = DistanceCalculator.Calculate(null, b);

            Assert.That(result, Is.EqualTo(-1m));
        }

        [Test]
        public void Calculate_NullSystemB_ReturnsNegativeOne()
        {
            var a = new StarSystem { Id = 1, Name = "Alpha", X = 5m, Y = 5m };

            decimal result = DistanceCalculator.Calculate(a, null);

            Assert.That(result, Is.EqualTo(-1m));
        }

        [Test]
        public void CalculateRoute_EmptyList_ReturnsZero()
        {
            var repo = LoadRepo(new List<StarSystem>());

            decimal result = DistanceCalculator.CalculateRoute(new List<int>(), repo);

            Assert.That(result, Is.EqualTo(0m));
        }

        [Test]
        public void CalculateRoute_SingleSystem_ReturnsZero()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "Alpha", X = 10m, Y = 20m },
            };
            var repo = LoadRepo(systems);

            decimal result = DistanceCalculator.CalculateRoute(new List<int> { 1 }, repo);

            Assert.That(result, Is.EqualTo(0m));
        }

        [Test]
        public void CalculateRoute_UnresolvableLeg_SkipsAndReturnsSumOfResolvable()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "A", X = 0m, Y = 0m },
                new StarSystem { Id = 2, Name = "B", X = 3m, Y = 4m },
                new StarSystem { Id = 3, Name = "C", X = 6m, Y = 8m },
            };
            var repo = LoadRepo(systems);

            // Route: 1 -> 999 -> 3 (leg 1->999 skipped, leg 999->3 skipped)
            decimal result = DistanceCalculator.CalculateRoute(
                new List<int> { 1, 999, 3 }, repo);

            // Only resolvable legs contribute; both legs involving 999 are skipped
            Assert.That(result, Is.EqualTo(0m));
        }

        [Test]
        public void CalculateRoute_MixedResolvableAndUnresolvable_ReturnsSumOfResolvableLegs()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem { Id = 1, Name = "A", X = 0m, Y = 0m },
                new StarSystem { Id = 2, Name = "B", X = 3m, Y = 4m },
                new StarSystem { Id = 3, Name = "C", X = 6m, Y = 8m },
            };
            var repo = LoadRepo(systems);

            // Route: 1 -> 2 -> 999 -> 3
            // Leg 1->2: resolvable (distance = 5)
            // Leg 2->999: skipped (unresolvable)
            // Leg 999->3: skipped (unresolvable)
            decimal result = DistanceCalculator.CalculateRoute(
                new List<int> { 1, 2, 999, 3 }, repo);

            decimal expectedLeg1 = DistanceCalculator.Calculate(systems[0], systems[1]);
            Assert.That(result, Is.EqualTo(expectedLeg1));
        }

        [Test]
        public void CalculateRoute_NullList_ReturnsZero()
        {
            var repo = LoadRepo(new List<StarSystem>());

            decimal result = DistanceCalculator.CalculateRoute(null, repo);

            Assert.That(result, Is.EqualTo(0m));
        }

        [Test]
        public void CalculateRoute_NullRepo_ReturnsZero()
        {
            decimal result = DistanceCalculator.CalculateRoute(
                new List<int> { 1, 2 }, null);

            Assert.That(result, Is.EqualTo(0m));
        }
    }
}
