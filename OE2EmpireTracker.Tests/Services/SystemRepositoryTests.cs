// <copyright file="SystemRepositoryTests.cs" company="OE2EmpireTracker">
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
    /// Property-based and unit tests for SystemRepository.
    /// Feature: systems-model
    /// </summary>
    [TestFixture]
    public class SystemRepositoryTests
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
        // Generator: List of StarSystems with unique IDs
        // ---------------------------------------------------------------

        private static Gen<List<StarSystem>> GenUniqueIdSystems()
        {
            return Gen.Choose(1, 20).SelectMany(count =>
                Gen.ListOf(count, Arb.From(StarSystemTests.StarSystemArbitraries.StarSystemArbitrary()).Generator)
                   .Select(systems =>
                   {
                       var list = systems.ToList();
                       for (int i = 0; i < list.Count; i++)
                       {
                           list[i].Id = i + 1;
                       }

                       return list;
                   }));
        }

        private static Gen<List<StarSystem>> GenUniqueNameSystems()
        {
            return Gen.Choose(1, 15).SelectMany(count =>
                Gen.ListOf(count, Arb.From(StarSystemTests.StarSystemArbitraries.StarSystemArbitrary()).Generator)
                   .Select(systems =>
                   {
                       var list = systems.ToList();
                       for (int i = 0; i < list.Count; i++)
                       {
                           list[i].Id = i + 1;
                           list[i].Name = $"System_{i + 1}";
                       }

                       return list;
                   }));
        }

        // ---------------------------------------------------------------
        // Property 3: ID lookup correctness
        // For any set of StarSystems loaded, FindById(system.Id) returns
        // that exact system.
        // **Validates: Requirements 3.1**
        // ---------------------------------------------------------------

        [Test]
        public void Property3_IdLookupCorrectness()
        {
            Prop.ForAll(
                GenUniqueIdSystems().ToArbitrary(),
                systems =>
                {
                    var repo = LoadRepo(systems);
                    return systems.All(s =>
                    {
                        var found = repo.FindById(s.Id);
                        return found != null &&
                               found.Id == s.Id &&
                               found.Name == s.Name;
                    });
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 4: Name lookup is case-insensitive
        // For any set of StarSystems with unique names, FindByName with
        // any case variation returns the correct system.
        // **Validates: Requirements 3.2, 5.1**
        // ---------------------------------------------------------------

        [Test]
        public void Property4_NameLookupCaseInsensitive()
        {
            Prop.ForAll(
                GenUniqueNameSystems().ToArbitrary(),
                systems =>
                {
                    var repo = LoadRepo(systems);
                    return systems.All(s =>
                    {
                        var upper = repo.FindByName(s.Name.ToUpperInvariant());
                        var lower = repo.FindByName(s.Name.ToLowerInvariant());
                        return upper != null && lower != null &&
                               upper.Id == s.Id && lower.Id == s.Id;
                    });
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 5: Count equals loaded systems
        // For any list of N StarSystems loaded, Count property equals N.
        // **Validates: Requirements 3.3**
        // ---------------------------------------------------------------

        [Test]
        public void Property5_CountEqualsLoadedSystems()
        {
            Prop.ForAll(
                GenUniqueIdSystems().ToArbitrary(),
                systems =>
                {
                    var repo = LoadRepo(systems);
                    return repo.Count == systems.Count;
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 6: Grid filter returns only matching systems
        // For any grid filter parameters, all returned systems match the
        // filter and no matching system is excluded.
        // **Validates: Requirements 3.4**
        // ---------------------------------------------------------------

        [Test]
        public void Property6_GridFilterReturnsOnlyMatchingSystems()
        {
            var gen = from systems in GenUniqueIdSystems()
                      from q in Gen.Elements<int?>(null, 1, 2, 3, 4)
                      from s in Gen.Elements<int?>(null, 1, 2, 3, 4)
                      from r in Gen.Elements<int?>(null, 1, 2, 3, 4)
                      from l in Gen.Elements<int?>(null, 1, 2, 3, 4)
                      select new { Systems = systems, Q = q, S = s, R = r, L = l };

            Prop.ForAll(
                gen.ToArbitrary(),
                input =>
                {
                    var repo = LoadRepo(input.Systems);
                    var results = repo.FindByGrid(input.Q, input.S, input.R, input.L).ToList();

                    var expected = input.Systems.Where(sys =>
                        (!input.Q.HasValue || sys.Quadrant == input.Q.Value) &&
                        (!input.S.HasValue || sys.Sector == input.S.Value) &&
                        (!input.R.HasValue || sys.Region == input.R.Value) &&
                        (!input.L.HasValue || sys.Locality == input.L.Value)).ToList();

                    return results.Count == expected.Count &&
                           results.All(r => expected.Any(e => e.Id == r.Id));
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 7: Partial name search returns only containing matches
        // For any non-empty search string, all returned systems contain
        // that string (case-insensitive) and no containing system is excluded.
        // **Validates: Requirements 3.5**
        // ---------------------------------------------------------------

        [Test]
        public void Property7_PartialNameSearchReturnsOnlyContainingMatches()
        {
            var gen = from systems in GenUniqueNameSystems()
                      from searchStr in Gen.Elements("System", "stem", "1", "ystem_")
                      select new { Systems = systems, Search = searchStr };

            Prop.ForAll(
                gen.ToArbitrary(),
                input =>
                {
                    var repo = LoadRepo(input.Systems);
                    var results = repo.SearchByName(input.Search).ToList();

                    var expected = input.Systems.Where(sys =>
                        sys.Name.IndexOf(input.Search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                    bool allMatch = results.All(r =>
                        r.Name.IndexOf(input.Search, StringComparison.OrdinalIgnoreCase) >= 0);
                    bool noneExcluded = results.Count == expected.Count;

                    return allMatch && noneExcluded;
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 10: Infrastructure filters return exactly matching systems
        // FindWithSpaceport returns exactly systems where HasSpaceport is true
        // FindWithStarbase returns exactly systems where HasStarbase is true
        // FindWithInfrastructure returns exactly systems where any flag is true
        // **Validates: Requirements 6.1, 6.2, 6.3**
        // ---------------------------------------------------------------

        [Test]
        public void Property10_InfrastructureFiltersReturnExactlyMatchingSystems()
        {
            Prop.ForAll(
                GenUniqueIdSystems().ToArbitrary(),
                systems =>
                {
                    var repo = LoadRepo(systems);

                    var spaceportResults = repo.FindWithSpaceport().ToList();
                    var starbaseResults = repo.FindWithStarbase().ToList();
                    var infraResults = repo.FindWithInfrastructure().ToList();

                    var expectedSpaceport = systems.Where(s => s.HasSpaceport).ToList();
                    var expectedStarbase = systems.Where(s => s.HasStarbase).ToList();
                    var expectedInfra = systems.Where(s =>
                        s.HasOrbital || s.HasSpaceport || s.HasStarbase).ToList();

                    return spaceportResults.Count == expectedSpaceport.Count &&
                           starbaseResults.Count == expectedStarbase.Count &&
                           infraResults.Count == expectedInfra.Count &&
                           spaceportResults.All(r => r.HasSpaceport) &&
                           starbaseResults.All(r => r.HasStarbase) &&
                           infraResults.All(r =>
                               r.HasOrbital || r.HasSpaceport || r.HasStarbase);
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 11: Faction query returns correct systems and summary
        // FindByFaction returns exactly systems with matching FactionId
        // GetFactionSummary includes every distinct non-zero FactionId
        // with correct count.
        // **Validates: Requirements 7.1, 7.2, 7.3**
        // ---------------------------------------------------------------

        [Test]
        public void Property11_FactionQueryReturnsCorrectSystemsAndSummary()
        {
            Prop.ForAll(
                GenUniqueIdSystems().ToArbitrary(),
                systems =>
                {
                    var repo = LoadRepo(systems);

                    var factionIds = systems.Where(s => s.FactionId != 0)
                                           .Select(s => s.FactionId)
                                           .Distinct()
                                           .ToList();

                    bool factionLookupCorrect = factionIds.All(fid =>
                    {
                        var results = repo.FindByFaction(fid).ToList();
                        var expected = systems.Where(s => s.FactionId == fid).ToList();
                        return results.Count == expected.Count &&
                               results.All(r => r.FactionId == fid);
                    });

                    var summary = repo.GetFactionSummary().ToList();
                    bool summaryCorrect = factionIds.All(fid =>
                        summary.Any(s => s.FactionId == fid &&
                            s.Count == systems.Count(sys => sys.FactionId == fid)));

                    return factionLookupCorrect && summaryCorrect;
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 14: Mutation updates values and fires event
        // For any system and valid new mutable property values,
        // UpdateSystem applies changes and fires SystemDataChanged
        // exactly once.
        // **Validates: Requirements 12.1, 12.3**
        // ---------------------------------------------------------------

        [Test]
        public void Property14_MutationUpdatesValuesAndFiresEvent()
        {
            var gen = from systems in GenUniqueIdSystems().Where(s => s.Count > 0)
                      from newFid in Gen.Choose(0, 100)
                      from newOrbital in Arb.Generate<bool>()
                      from newSpaceport in Arb.Generate<bool>()
                      from newStarbase in Arb.Generate<bool>()
                      select new
                      {
                          Systems = systems,
                          NewFid = newFid,
                          NewOrbital = newOrbital,
                          NewSpaceport = newSpaceport,
                          NewStarbase = newStarbase,
                      };

            Prop.ForAll(
                gen.ToArbitrary(),
                input =>
                {
                    var repo = LoadRepo(input.Systems);
                    var target = input.Systems[0];
                    int eventCount = 0;
                    repo.SystemDataChanged += (sender, args) => eventCount++;

                    repo.UpdateSystem(target.Id, s =>
                    {
                        s.FactionId = input.NewFid;
                        s.FactionName = input.NewFid > 0
                            ? $"Faction{input.NewFid}"
                            : string.Empty;
                        s.HasOrbital = input.NewOrbital;
                        s.HasSpaceport = input.NewSpaceport;
                        s.HasStarbase = input.NewStarbase;
                    });

                    var updated = repo.FindById(target.Id);
                    return updated.FactionId == input.NewFid &&
                           updated.HasOrbital == input.NewOrbital &&
                           updated.HasSpaceport == input.NewSpaceport &&
                           updated.HasStarbase == input.NewStarbase &&
                           eventCount == 1;
                }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Unit Tests: Error handling (Task 2.14)
        // ---------------------------------------------------------------

        /// <summary>
        /// Load with missing file: 0 systems, no crash.
        /// </summary>
        [Test]
        public void Load_MissingFile_ZeroSystems()
        {
            var repo = new SystemRepository();
            repo.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json"));

            Assert.That(repo.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Load with empty file: 0 systems, no crash.
        /// </summary>
        [Test]
        public void Load_EmptyFile_ZeroSystems()
        {
            var tempFile = Path.GetTempFileName();
            _tempFiles.Add(tempFile);
            File.WriteAllText(tempFile, string.Empty);

            var repo = new SystemRepository();
            repo.Load(tempFile);

            Assert.That(repo.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Load with malformed JSON: 0 systems, no crash.
        /// </summary>
        [Test]
        public void Load_MalformedJson_ZeroSystems()
        {
            var tempFile = Path.GetTempFileName();
            _tempFiles.Add(tempFile);
            File.WriteAllText(tempFile, "{ this is not valid json [[[");

            var repo = new SystemRepository();
            repo.Load(tempFile);

            Assert.That(repo.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Load with duplicate names: first-wins in name index.
        /// </summary>
        [Test]
        public void Load_DuplicateNames_FirstWins()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem
                {
                    Id = 1, Name = "Sol", X = 10m, Y = 20m,
                    Quadrant = 1, Sector = 1, Region = 1, Locality = 1,
                    SpectralClass = "G",
                },
                new StarSystem
                {
                    Id = 2, Name = "Sol", X = 30m, Y = 40m,
                    Quadrant = 2, Sector = 2, Region = 2, Locality = 2,
                    SpectralClass = "K",
                },
            };

            var repo = LoadRepo(systems);

            Assert.That(repo.Count, Is.EqualTo(2));
            var found = repo.FindByName("Sol");
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Id, Is.EqualTo(1));
        }

        /// <summary>
        /// UpdateSystem with unknown ID: no-op, no event fired.
        /// </summary>
        [Test]
        public void UpdateSystem_UnknownId_NoOpNoEvent()
        {
            var systems = new List<StarSystem>
            {
                new StarSystem
                {
                    Id = 1, Name = "Alpha", X = 0m, Y = 0m,
                    Quadrant = 1, Sector = 1, Region = 1, Locality = 1,
                    SpectralClass = "M",
                },
            };

            var repo = LoadRepo(systems);
            int eventCount = 0;
            repo.SystemDataChanged += (sender, args) => eventCount++;

            repo.UpdateSystem(999, s => s.FactionId = 42);

            Assert.That(eventCount, Is.EqualTo(0));
        }

        /// <summary>
        /// ReplaceAll replaces data and fires event.
        /// </summary>
        [Test]
        public void ReplaceAll_ReplacesDataAndFiresEvent()
        {
            var initial = new List<StarSystem>
            {
                new StarSystem
                {
                    Id = 1, Name = "Alpha", X = 0m, Y = 0m,
                    Quadrant = 1, Sector = 1, Region = 1, Locality = 1,
                    SpectralClass = "M",
                },
            };

            var repo = LoadRepo(initial);
            int eventCount = 0;
            repo.SystemDataChanged += (sender, args) => eventCount++;

            var replacement = new List<StarSystem>
            {
                new StarSystem
                {
                    Id = 10, Name = "Beta", X = 5m, Y = 5m,
                    Quadrant = 2, Sector = 2, Region = 2, Locality = 2,
                    SpectralClass = "K",
                },
                new StarSystem
                {
                    Id = 20, Name = "Gamma", X = 10m, Y = 10m,
                    Quadrant = 3, Sector = 3, Region = 3, Locality = 3,
                    SpectralClass = "G",
                },
            };

            repo.ReplaceAll(replacement);

            Assert.That(repo.Count, Is.EqualTo(2));
            Assert.That(repo.FindById(1), Is.Null);
            Assert.That(repo.FindById(10), Is.Not.Null);
            Assert.That(repo.FindById(20), Is.Not.Null);
            Assert.That(repo.FindByName("Beta"), Is.Not.Null);
            Assert.That(eventCount, Is.EqualTo(1));
        }
    }
}
