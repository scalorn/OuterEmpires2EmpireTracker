// <copyright file="ColonyMergeServiceColonyIdTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for Colony.ColonyId default and ColonyMergeService ColonyId population.
    /// Validates: Req 7, Criteria 1-2.
    /// </summary>
    [TestFixture]
    public class ColonyMergeServiceColonyIdTests
    {
        private const string OwnerUUID = "owner-uuid-colonyid-test";

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => new System.DateTime(2025, 1, 15, 12, 0, 0, System.DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Colony.ColonyId defaults to 0 on a freshly constructed Colony.
        /// Validates: Req 7.1.
        /// </summary>
        [Test]
        public void Colony_ColonyId_DefaultsToZero()
        {
            var colony = new Colony();

            Assert.That(colony.ColonyId, Is.EqualTo(0));
        }

        /// <summary>
        /// ColonyMergeService sets ColonyId on newly created colonies via CreateColonyFromApi.
        /// Validates: Req 7.2.
        /// </summary>
        [Test]
        public void MergeColonyList_NewColony_SetsColonyId()
        {
            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 777,
                    ColonyName = "New Colony",
                    SystemObjectName = "Planet Omega",
                    SystemName = "Tau Ceti",
                    SystemId = 12,
                    ColonySize = 2,
                    Distance = 8.5,
                },
            };
            var localColonies = new List<Colony>();

            ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(localColonies.Count, Is.EqualTo(1));
            Assert.That(localColonies[0].ColonyId, Is.EqualTo(777));
        }

        /// <summary>
        /// ColonyMergeService sets ColonyId on matched (updated) colonies via MergeAllColonyFields.
        /// Validates: Req 7.2.
        /// </summary>
        [Test]
        public void MergeColonyList_ExistingColony_SetsColonyId()
        {
            var existingColony = new Colony
            {
                UUID = "existing-colonyid-uuid",
                OwnerUUID = OwnerUUID,
                PlanetName = "Planet Sigma",
                SystemName = "Rigel",
                ColonyName = "Sigma Colony",
                ColonyId = 0,
            };
            var localColonies = new List<Colony> { existingColony };

            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 999,
                    ColonyName = "Sigma Colony",
                    SystemObjectName = "Planet Sigma",
                    SystemName = "Rigel",
                    SystemId = 5,
                    ColonySize = 3,
                    Distance = 10.0,
                },
            };

            ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(existingColony.ColonyId, Is.EqualTo(999));
        }
    }
}
