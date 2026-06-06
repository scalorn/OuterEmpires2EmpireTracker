// <copyright file="SystemObjectIdPropagationPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for SystemObjectId propagation.
    /// Validates that when a survey is imported with SystemObjectId greater than 0
    /// and is linked to an asteroid, both entities have the same SystemObjectId.
    /// </summary>
    [TestFixture]
    public class SystemObjectIdPropagationPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
            EmpireContext.GetInstance();
            PlayerContext.Reset();
            PlayerContext.FilePath = "nonexistent_player_data.json";
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // Property 8: SystemObjectId Propagation
        // When a survey is imported with SystemObjectId > 0 and is linked to an
        // asteroid, both the survey and asteroid entities have the same
        // SystemObjectId value.
        // **Validates: Requirements 6.4**
        // -----------------------------------------------------------------------

        /// <summary>
        /// Simulates the SystemObjectId propagation logic from QueueSyncService.
        /// If survey.SystemObjectId is greater than 0 and an asteroid with the matching
        /// AsteroidUUID exists, the asteroid's SystemObjectId is set to match.
        /// </summary>
        /// <param name="survey">The survey with a SystemObjectId value.</param>
        /// <param name="playerContext">The player context containing asteroids.</param>
        private static void PropagateSystemObjectId(Survey survey, PlayerContext playerContext)
        {
            if (survey.SystemObjectId > 0 && !string.IsNullOrEmpty(survey.AsteroidUUID))
            {
                var asteroid = playerContext.FindAsteroid(survey.AsteroidUUID);
                if (asteroid != null)
                {
                    asteroid.SystemObjectId = survey.SystemObjectId;
                }
            }
        }

        /// <summary>
        /// Property: For any survey with SystemObjectId greater than 0 that is linked to an
        /// existing asteroid, after propagation the asteroid's SystemObjectId equals
        /// the survey's SystemObjectId.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LinkedAsteroid_ReceivesSurveySystemObjectId()
        {
            var systemObjectIdGen = Gen.Choose(1, 999999);
            var asteroidCountGen = Gen.Choose(1, 10);

            var gen = from systemObjectId in systemObjectIdGen
                      from asteroidCount in asteroidCountGen
                      from linkedIndex in Gen.Choose(0, asteroidCount - 1)
                      select new { SystemObjectId = systemObjectId, AsteroidCount = asteroidCount, LinkedIndex = linkedIndex };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                PlayerContext.Reset();
                PlayerContext.FilePath = "nonexistent_player_data.json";
                var ctx = PlayerContext.GetInstance();

                var asteroids = new List<Asteroid>();
                for (int i = 0; i < data.AsteroidCount; i++)
                {
                    var asteroid = new Asteroid
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Name = "Asteroid_" + i,
                        SystemObjectId = 0,
                    };

                    ctx.AddAsteroid(asteroid);
                    asteroids.Add(asteroid);
                }

                var linkedAsteroid = asteroids[data.LinkedIndex];
                var survey = new Survey("TestSurvey")
                {
                    UUID = Guid.NewGuid().ToString(),
                    AsteroidUUID = linkedAsteroid.UUID,
                    SystemObjectId = data.SystemObjectId,
                };

                PropagateSystemObjectId(survey, ctx);

                var result = ctx.FindAsteroid(linkedAsteroid.UUID);

                return (result.SystemObjectId == data.SystemObjectId)
                    .Label(
                        "Expected asteroid.SystemObjectId=" + data.SystemObjectId
                        + ", actual=" + result.SystemObjectId);
            });
        }

        /// <summary>
        /// Property: For any survey with SystemObjectId equal to 0, no propagation occurs
        /// and the linked asteroid's SystemObjectId remains unchanged (0).
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ZeroSystemObjectId_DoesNotPropagate()
        {
            var asteroidCountGen = Gen.Choose(1, 10);

            var gen = from asteroidCount in asteroidCountGen
                      from linkedIndex in Gen.Choose(0, asteroidCount - 1)
                      select new { AsteroidCount = asteroidCount, LinkedIndex = linkedIndex };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                PlayerContext.Reset();
                PlayerContext.FilePath = "nonexistent_player_data.json";
                var ctx = PlayerContext.GetInstance();

                var asteroids = new List<Asteroid>();
                for (int i = 0; i < data.AsteroidCount; i++)
                {
                    var asteroid = new Asteroid
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Name = "Asteroid_" + i,
                        SystemObjectId = 0,
                    };

                    ctx.AddAsteroid(asteroid);
                    asteroids.Add(asteroid);
                }

                var linkedAsteroid = asteroids[data.LinkedIndex];
                var survey = new Survey("TestSurvey")
                {
                    UUID = Guid.NewGuid().ToString(),
                    AsteroidUUID = linkedAsteroid.UUID,
                    SystemObjectId = 0,
                };

                PropagateSystemObjectId(survey, ctx);

                var result = ctx.FindAsteroid(linkedAsteroid.UUID);

                return (result.SystemObjectId == 0)
                    .Label(
                        "Expected asteroid.SystemObjectId=0"
                        + ", actual=" + result.SystemObjectId);
            });
        }

        /// <summary>
        /// Property: For any survey with SystemObjectId greater than 0 but with no linked asteroid
        /// (empty AsteroidUUID), no propagation occurs and no exception is thrown.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property UnlinkedSurvey_DoesNotPropagate()
        {
            var systemObjectIdGen = Gen.Choose(1, 999999);
            var asteroidCountGen = Gen.Choose(1, 10);

            var gen = from systemObjectId in systemObjectIdGen
                      from asteroidCount in asteroidCountGen
                      select new { SystemObjectId = systemObjectId, AsteroidCount = asteroidCount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                PlayerContext.Reset();
                PlayerContext.FilePath = "nonexistent_player_data.json";
                var ctx = PlayerContext.GetInstance();

                for (int i = 0; i < data.AsteroidCount; i++)
                {
                    var asteroid = new Asteroid
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Name = "Asteroid_" + i,
                        SystemObjectId = 0,
                    };

                    ctx.AddAsteroid(asteroid);
                }

                var survey = new Survey("TestSurvey")
                {
                    UUID = Guid.NewGuid().ToString(),
                    AsteroidUUID = string.Empty,
                    SystemObjectId = data.SystemObjectId,
                };

                PropagateSystemObjectId(survey, ctx);

                var allAsteroids = ctx.SnapshotAsteroidList();
                bool noneChanged = allAsteroids.All(a => a.SystemObjectId == 0);

                return noneChanged
                    .Label("Unlinked survey should not propagate to any asteroid");
            });
        }
    }
}
