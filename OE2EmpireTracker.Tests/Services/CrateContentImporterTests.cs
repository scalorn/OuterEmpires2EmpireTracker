// <copyright file="CrateContentImporterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for CrateContentImporter.
    /// </summary>
    [TestFixture]
    public class CrateContentImporterTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private BlueprintLinkageService blueprintLinkageService;
        private CrateContentImporter importer;

        /// <summary>
        /// Sets up test fixtures.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            empireContext = EmpireContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            blueprintLinkageService = new BlueprintLinkageService(playerContext, empireContext);
            importer = new CrateContentImporter(playerContext, empireContext, blueprintLinkageService);
        }

        // -------------------------------------------------------------------
        // Test: Malformed JSON returns empty result with Success=false
        // Validates: Req 9 AC4 (malformed JSON no exception)
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that malformed JSON does not throw an exception and returns
        /// an empty result with Success set to false.
        /// </summary>
        [Test]
        public void CrateContentImporter_MalformedJson_NoException()
        {
            var parentBag = new ItemBag();
            var visited = new HashSet<int>();

            var result = importer.Import("not valid json {{{{", 999, parentBag, "owner-uuid", visited);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.TotalItems, Is.EqualTo(0));
            Assert.That(result.Imported, Is.EqualTo(0));
        }
    }
}
