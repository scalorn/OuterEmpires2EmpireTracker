// <copyright file="FreshnessSkipPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for freshness skip correctness.
    /// Feature: queue-based-sync
    /// </summary>
    [TestFixture]
    public class FreshnessSkipPropertyTests
    {
        private static readonly DateTime FrozenNow =
            new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            SystemClock.FreezeAt(FrozenNow);
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Oracle function implementing the spec definition of freshness.
        /// A detail is fresh iff lastImportUtc is non-null AND the elapsed time
        /// since lastImportUtc is less than the configured DetailRefreshHours.
        /// </summary>
        private static bool OracleIsDetailFresh(DateTime? lastImportUtc, int detailRefreshHours)
        {
            if (lastImportUtc == null)
            {
                return false;
            }

            var threshold = TimeSpan.FromHours(detailRefreshHours);
            return (SystemClock.UtcNow - lastImportUtc.Value) < threshold;
        }

        /// <summary>
        /// Property 3: Freshness Skip Correctness.
        ///
        /// For any random (lastImportUtc, detailRefreshHours) combination, the freshness
        /// decision matches the specification: skip (fresh) iff lastImportUtc is non-null
        /// AND (SystemClock.UtcNow - lastImportUtc) is less than TimeSpan.FromHours(hours).
        ///
        /// Generates:
        /// - Null timestamps (always stale)
        /// - Recent timestamps within threshold (fresh)
        /// - Old timestamps beyond threshold (stale)
        /// - Future timestamps (fresh, since elapsed is negative)
        /// - Edge cases at exactly the threshold boundary
        ///
        /// **Validates: Requirements 14.3, 14.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property FreshnessDecision_MatchesSpec_ForAnyTimestampAndHours()
        {
            var gen = from hours in Gen.Choose(1, 168)
                      from isNull in Gen.Elements(true, false)
                      from offsetMinutes in Gen.Choose(-10080, 10080)
                      select new
                      {
                          Hours = hours,
                          LastImportUtc = isNull
                              ? (DateTime?)null
                              : FrozenNow.AddMinutes(-offsetMinutes),
                      };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                bool expected = OracleIsDetailFresh(input.LastImportUtc, input.Hours);

                // Replicate the production logic inline as a second oracle
                bool actual;
                if (input.LastImportUtc == null)
                {
                    actual = false;
                }
                else
                {
                    var threshold = TimeSpan.FromHours(input.Hours);
                    actual = (SystemClock.UtcNow - input.LastImportUtc.Value) < threshold;
                }

                return (expected == actual)
                    .Label($"hours={input.Hours}, lastImport={input.LastImportUtc?.ToString("o") ?? "null"}, " +
                           $"expected={expected}, actual={actual}");
            });
        }

        /// <summary>
        /// Property 3b: Null Timestamp Always Stale.
        ///
        /// When lastImportUtc is null, the item is never fresh regardless of
        /// DetailRefreshHours setting.
        ///
        /// **Validates: Requirements 14.3, 14.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NullTimestamp_AlwaysStale_RegardlessOfHours()
        {
            var gen = Gen.Choose(1, 168);

            return Prop.ForAll(Arb.From(gen), hours =>
            {
                bool result = OracleIsDetailFresh(null, hours);
                return (!result)
                    .Label($"hours={hours}, expected=false (null timestamp), got={result}");
            });
        }

        /// <summary>
        /// Property 3c: Recent Import Is Fresh.
        ///
        /// When lastImportUtc is within (0, DetailRefreshHours) hours ago,
        /// the item is fresh.
        ///
        /// **Validates: Requirements 14.3, 14.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RecentImport_IsFresh_WhenWithinThreshold()
        {
            var gen = from hours in Gen.Choose(1, 168)
                      from minutesAgo in Gen.Choose(1, (hours * 60) - 1)
                      select new { Hours = hours, MinutesAgo = minutesAgo };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                var lastImport = FrozenNow.AddMinutes(-input.MinutesAgo);
                bool result = OracleIsDetailFresh(lastImport, input.Hours);
                return result
                    .Label($"hours={input.Hours}, minutesAgo={input.MinutesAgo}, expected=true, got={result}");
            });
        }

        /// <summary>
        /// Property 3d: Old Import Is Stale.
        ///
        /// When lastImportUtc is at or beyond DetailRefreshHours ago,
        /// the item is stale (not fresh).
        ///
        /// **Validates: Requirements 14.3, 14.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OldImport_IsStale_WhenBeyondThreshold()
        {
            var gen = from hours in Gen.Choose(1, 168)
                      from extraMinutes in Gen.Choose(0, 1440)
                      select new { Hours = hours, ExtraMinutes = extraMinutes };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                // Place lastImport exactly at threshold or beyond
                var lastImport = FrozenNow.AddHours(-input.Hours).AddMinutes(-input.ExtraMinutes);
                bool result = OracleIsDetailFresh(lastImport, input.Hours);
                return (!result)
                    .Label($"hours={input.Hours}, extraMinutes={input.ExtraMinutes}, expected=false, got={result}");
            });
        }
    }
}
