using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Integration tests for SystemGridIndex correctness using actual galaxy data.
    /// Feature: market-integration
    /// **Validates: Requirements 14.1, 14.4**
    /// </summary>
    [TestFixture]
    public class SystemGridIndexTests
    {
        private SystemRepository _repo;
        private SystemGridIndex _gridIndex;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _repo = new SystemRepository();
            string galaxyPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "oe2-galaxy-systems.json");

            if (!File.Exists(galaxyPath))
            {
                // Fallback: workspace root
                galaxyPath = Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "..", "..", "..", "..", "oe2-galaxy-systems.json");
            }

            if (File.Exists(galaxyPath))
            {
                _repo.Load(galaxyPath);
            }
            else
            {
                // Create minimal test data if galaxy file unavailable
                _repo.ReplaceAll(GenerateTestSystems(50));
            }

            _gridIndex = new SystemGridIndex(_repo, 50);
        }

        // -------------------------------------------------------------------
        // 9.8 SystemGridIndex Correctness
        // **Validates: Requirements 14.1, 14.4**
        // ComputeSystemsInRange matches brute-force scan.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property ComputeSystemsInRange_MatchesBruteForceScan()
        {
            if (_repo.Count == 0)
            {
                return true.ToProperty();
            }

            var systemIdArb = Arb.From(
                Gen.Elements(_repo.Systems.Select(s => s.Id).ToArray()));
            var rangeArb = Arb.From(Gen.Choose(10, 300));

            return Prop.ForAll(systemIdArb, rangeArb, (systemId, rangeJas) =>
            {
                // Grid-based result
                HashSet<int> gridResult = _gridIndex.ComputeSystemsInRange(systemId, rangeJas);

                // Brute-force result
                HashSet<int> bruteResult = BruteForceSystemsInRange(systemId, rangeJas);

                // Assert exact match
                bool sameCount = gridResult.Count == bruteResult.Count;
                bool sameContent = gridResult.SetEquals(bruteResult);

                return sameCount && sameContent;
            });
        }

        [Test]
        public void ComputeSystemsInRange_IncludesCenterSystem()
        {
            if (_repo.Count == 0)
            {
                Assert.Inconclusive("No galaxy data available.");
                return;
            }

            int firstId = _repo.Systems[0].Id;
            HashSet<int> result = _gridIndex.ComputeSystemsInRange(firstId, 100);
            Assert.That(result, Does.Contain(firstId));
        }

        [Test]
        public void ComputeSystemsInRange_ZeroRange_ReturnsOnlyCenter()
        {
            if (_repo.Count == 0)
            {
                Assert.Inconclusive("No galaxy data available.");
                return;
            }

            int firstId = _repo.Systems[0].Id;
            HashSet<int> result = _gridIndex.ComputeSystemsInRange(firstId, 0);
            Assert.That(result, Does.Contain(firstId));
            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void ComputeSystemsInRange_InvalidSystem_ReturnsEmpty()
        {
            HashSet<int> result = _gridIndex.ComputeSystemsInRange(-999, 200);
            Assert.That(result, Is.Empty);
        }

        private HashSet<int> BruteForceSystemsInRange(int systemId, int rangeJas)
        {
            const decimal jasScale = 1.5625m;
            var result = new HashSet<int>();

            StarSystem center = _repo.FindById(systemId);
            if (center == null)
            {
                return result;
            }

            decimal rangeCoord = rangeJas * jasScale;
            decimal rangeSquared = rangeCoord * rangeCoord;

            foreach (StarSystem candidate in _repo.Systems)
            {
                decimal dx = candidate.X - center.X;
                decimal dy = candidate.Y - center.Y;
                decimal distSquared = (dx * dx) + (dy * dy);

                if (distSquared <= rangeSquared)
                {
                    result.Add(candidate.Id);
                }
            }

            return result;
        }

        private static List<StarSystem> GenerateTestSystems(int count)
        {
            var rng = new System.Random(42);
            var systems = new List<StarSystem>();
            for (int i = 1; i <= count; i++)
            {
                systems.Add(new StarSystem
                {
                    Id = i,
                    Name = "System" + i,
                    X = (decimal)((rng.NextDouble() * 2000) - 1000),
                    Y = (decimal)((rng.NextDouble() * 2000) - 1000),
                });
            }

            return systems;
        }
    }
}
