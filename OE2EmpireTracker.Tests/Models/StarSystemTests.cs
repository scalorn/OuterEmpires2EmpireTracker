// <copyright file="StarSystemTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Property-based and unit tests for StarSystem serialization.
    /// Feature: systems-model
    /// </summary>
    [TestFixture]
    public class StarSystemTests
    {
        private static readonly JsonSerializerSettings SystemJsonSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
        };

        // ---------------------------------------------------------------
        // Arbitrary generator for StarSystem
        // ---------------------------------------------------------------

        /// <summary>
        /// Provides a custom FsCheck Arbitrary for StarSystem generation.
        /// </summary>
        public static class StarSystemArbitraries
        {
            /// <summary>
            /// Generates arbitrary StarSystem instances with valid field values.
            /// </summary>
            /// <returns>An Arbitrary for StarSystem.</returns>
            public static Arbitrary<StarSystem> StarSystemArbitrary()
            {
                return Arb.From(
                    from id in Arb.Generate<PositiveInt>()
                    from name in Gen.Elements("Alpha", "Beta", "Gamma", "Delta", "Epsilon")
                                   .Select(prefix => $"{prefix} {id.Get}")
                    from x in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                    from y in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                    from q in Gen.Choose(1, 4)
                    from s in Gen.Choose(1, 4)
                    from r in Gen.Choose(1, 4)
                    from l in Gen.Choose(1, 4)
                    from spectral in Gen.Elements("M", "K", "G", "F", "W", "X")
                    from fid in Gen.Choose(0, 100)
                    from hasOrbital in Arb.Generate<bool>()
                    from hasSpaceport in Arb.Generate<bool>()
                    from hasStarbase in Arb.Generate<bool>()
                    select new StarSystem
                    {
                        Id = id.Get,
                        Name = name,
                        X = x,
                        Y = y,
                        Quadrant = q,
                        Sector = s,
                        Region = r,
                        Locality = l,
                        SpectralClass = spectral,
                        FactionId = fid,
                        FactionName = fid > 0 ? $"Faction{fid}" : string.Empty,
                        FactionColor = fid > 0 ? "#FF0000" : string.Empty,
                        HasOrbital = hasOrbital,
                        HasSpaceport = hasSpaceport,
                        HasStarbase = hasStarbase,
                    });
            }
        }

        // ---------------------------------------------------------------
        // Property 1: Serialization round-trip
        // For any valid StarSystem, serialize to JSON with
        // DefaultValueHandling.Ignore then deserialize back; all 15
        // properties must be equivalent.
        // **Validates: Requirements 9.1, 1.10**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(StarSystemArbitraries) })]
        public Property SerializationRoundTrip_PreservesAllProperties(StarSystem system)
        {
            var json = JsonConvert.SerializeObject(system, SystemJsonSettings);
            var deserialized = JsonConvert.DeserializeObject<StarSystem>(json);

            return (deserialized.Id == system.Id &&
                    deserialized.Name == system.Name &&
                    deserialized.X == system.X &&
                    deserialized.Y == system.Y &&
                    deserialized.Quadrant == system.Quadrant &&
                    deserialized.Sector == system.Sector &&
                    deserialized.Region == system.Region &&
                    deserialized.Locality == system.Locality &&
                    deserialized.SpectralClass == system.SpectralClass &&
                    deserialized.FactionId == system.FactionId &&
                    deserialized.FactionName == system.FactionName &&
                    deserialized.FactionColor == system.FactionColor &&
                    deserialized.HasOrbital == system.HasOrbital &&
                    deserialized.HasSpaceport == system.HasSpaceport &&
                    deserialized.HasStarbase == system.HasStarbase).ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 2: Serialization omits default values
        // For any StarSystem with FactionId=0, empty FactionName/FactionColor,
        // and all infrastructure flags false, serialized JSON must NOT
        // contain keys "fid", "fn", "fc", "o", "sp", "sb".
        // **Validates: Requirements 9.4**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(StarSystemArbitraries) })]
        public Property SerializationOmitsDefaultValues(StarSystem system)
        {
            // Force all default-value properties to their defaults
            system.FactionId = 0;
            system.FactionName = string.Empty;
            system.FactionColor = string.Empty;
            system.HasOrbital = false;
            system.HasSpaceport = false;
            system.HasStarbase = false;

            var json = JsonConvert.SerializeObject(system, SystemJsonSettings);

            return (!json.Contains("\"fid\"") &&
                    !json.Contains("\"fn\"") &&
                    !json.Contains("\"fc\"") &&
                    !json.Contains("\"o\"") &&
                    !json.Contains("\"sp\"") &&
                    !json.Contains("\"sb\"")).ToProperty();
        }

        // ---------------------------------------------------------------
        // Unit Tests: Serialization edge cases (Task 1.5)
        // ---------------------------------------------------------------

        /// <summary>
        /// Deserialization with missing optional fields defaults to
        /// empty string, zero, or false.
        /// </summary>
        [Test]
        public void Deserialize_MissingOptionalFields_DefaultsToEmptyOrZeroOrFalse()
        {
            // JSON with only immutable fields, no faction or infrastructure
            var json = @"{""id"":42,""n"":""Sol"",""x"":100.5,""y"":-200.3,""q"":1,""s"":2,""r"":3,""l"":4,""st"":""G""}";

            var system = JsonConvert.DeserializeObject<StarSystem>(json);

            Assert.That(system.Id, Is.EqualTo(42));
            Assert.That(system.Name, Is.EqualTo("Sol"));
            Assert.That(system.X, Is.EqualTo(100.5m));
            Assert.That(system.Y, Is.EqualTo(-200.3m));
            Assert.That(system.Quadrant, Is.EqualTo(1));
            Assert.That(system.Sector, Is.EqualTo(2));
            Assert.That(system.Region, Is.EqualTo(3));
            Assert.That(system.Locality, Is.EqualTo(4));
            Assert.That(system.SpectralClass, Is.EqualTo("G"));
            Assert.That(system.FactionId, Is.EqualTo(0));
            Assert.That(system.FactionName, Is.EqualTo(string.Empty));
            Assert.That(system.FactionColor, Is.EqualTo(string.Empty));
            Assert.That(system.HasOrbital, Is.False);
            Assert.That(system.HasSpaceport, Is.False);
            Assert.That(system.HasStarbase, Is.False);
        }

        /// <summary>
        /// Compact JSON property names match the source data format.
        /// </summary>
        [Test]
        public void Serialize_CompactPropertyNames_MatchSourceFormat()
        {
            var system = new StarSystem
            {
                Id = 1,
                Name = "TestStar",
                X = 10.5m,
                Y = -20.3m,
                Quadrant = 2,
                Sector = 3,
                Region = 1,
                Locality = 4,
                SpectralClass = "K",
                FactionId = 5,
                FactionName = "TestFaction",
                FactionColor = "#00FF00",
                HasOrbital = true,
                HasSpaceport = true,
                HasStarbase = false,
            };

            var json = JsonConvert.SerializeObject(system, SystemJsonSettings);

            Assert.That(json, Does.Contain("\"id\":1"));
            Assert.That(json, Does.Contain("\"n\":\"TestStar\""));
            Assert.That(json, Does.Contain("\"x\":10.5"));
            Assert.That(json, Does.Contain("\"y\":-20.3"));
            Assert.That(json, Does.Contain("\"q\":2"));
            Assert.That(json, Does.Contain("\"s\":3"));
            Assert.That(json, Does.Contain("\"r\":1"));
            Assert.That(json, Does.Contain("\"l\":4"));
            Assert.That(json, Does.Contain("\"st\":\"K\""));
            Assert.That(json, Does.Contain("\"fid\":5"));
            Assert.That(json, Does.Contain("\"fn\":\"TestFaction\""));
            Assert.That(json, Does.Contain("\"fc\":\"#00FF00\""));
            Assert.That(json, Does.Contain("\"o\":true"));
            Assert.That(json, Does.Contain("\"sp\":true"));
            // HasStarbase is false (default), should be omitted
            Assert.That(json, Does.Not.Contain("\"sb\""));
        }

        /// <summary>
        /// DefaultValue attributes produce smaller JSON for unclaimed systems
        /// with no infrastructure.
        /// </summary>
        [Test]
        public void Serialize_UnclaimedSystem_ProducesSmallerJson()
        {
            var claimed = new StarSystem
            {
                Id = 1,
                Name = "Claimed",
                X = 0m,
                Y = 0m,
                Quadrant = 1,
                Sector = 1,
                Region = 1,
                Locality = 1,
                SpectralClass = "M",
                FactionId = 10,
                FactionName = "BigFaction",
                FactionColor = "#AABBCC",
                HasOrbital = true,
                HasSpaceport = true,
                HasStarbase = true,
            };

            var unclaimed = new StarSystem
            {
                Id = 2,
                Name = "Unclaimed",
                X = 0m,
                Y = 0m,
                Quadrant = 1,
                Sector = 1,
                Region = 1,
                Locality = 1,
                SpectralClass = "M",
                FactionId = 0,
                FactionName = string.Empty,
                FactionColor = string.Empty,
                HasOrbital = false,
                HasSpaceport = false,
                HasStarbase = false,
            };

            var claimedJson = JsonConvert.SerializeObject(claimed, SystemJsonSettings);
            var unclaimedJson = JsonConvert.SerializeObject(unclaimed, SystemJsonSettings);

            Assert.That(unclaimedJson.Length, Is.LessThan(claimedJson.Length));
        }
    }
}
