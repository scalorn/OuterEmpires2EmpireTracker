// <copyright file="GameApiMetricsCollectorOutstandingTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for GameApiMetricsCollector outstanding non-negativity
    /// and history boundedness.
    /// Feature: game-api-status-form
    /// Validates: Requirements 5.1, 1.5
    /// </summary>
    [TestFixture]
    public class GameApiMetricsCollectorOutstandingTests
    {
        [SetUp]
        public void SetUp()
        {
            GameApiMetricsCollector.Reset();
            GameApiMetricsCollector.Initialize();
            SystemClock.UtcNowFunc = () => new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            GameApiMetricsCollector.Reset();
            SystemClock.Reset();
        }

        // -----------------------------------------------------------------------
        // Property 2: Outstanding Non-Negativity
        // Outstanding count never goes negative after random interleaved
        // OnRequestStarted/OnRequestCompleted/OnRequestFailed sequences
        // including more completions than starts.
        // **Validates: Requirements 5.1**
        // -----------------------------------------------------------------------

        [Test]
        public void OutstandingNeverNegative()
        {
            var operationGen = Gen.Frequency(
                new WeightAndValue<Gen<int>>(2, Gen.Constant(0)),
                new WeightAndValue<Gen<int>>(4, Gen.Constant(1)),
                new WeightAndValue<Gen<int>>(3, Gen.Constant(2)));

            var sequenceGen =
                from ops in Gen.ListOf(operationGen)
                select ops.ToArray();

            Prop.ForAll(
                sequenceGen.ToArbitrary(),
                operations =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();
                    var collector = GameApiMetricsCollector.Instance;
                    bool allNonNegative = true;

                    foreach (var op in operations)
                    {
                        switch (op)
                        {
                            case 0:
                                collector.OnRequestStarted("/test", "GET");
                                break;
                            case 1:
                                collector.OnRequestCompleted("/test", "GET", 200, 50, 0, 256);
                                break;
                            default:
                                collector.OnRequestFailed("/test", "GET", "Timeout", "timed out");
                                break;
                        }

                        var snapshot = collector.GetCurrentSnapshot();
                        if (snapshot.OutstandingRequests < 0)
                        {
                            allNonNegative = false;
                            break;
                        }
                    }

                    return allNonNegative.ToProperty();
                }).QuickCheckThrowOnFailure();
        }

        // -----------------------------------------------------------------------
        // Property 3: History Boundedness
        // GetHistory().Count <= 500 after generating 1-2000 random request
        // completions.
        // **Validates: Requirements 1.5**
        // -----------------------------------------------------------------------

        [Test]
        public void HistoryNeverExceeds500()
        {
            var countGen =
                from n in Gen.Choose(1, 2000)
                select n;

            Prop.ForAll(
                countGen.ToArbitrary(),
                n =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();
                    var collector = GameApiMetricsCollector.Instance;

                    for (int i = 0; i < n; i++)
                    {
                        collector.OnRequestCompleted("/test/" + i, "GET", 200, 50, 0, 128);
                    }

                    var history = collector.GetHistory();
                    return (history.Count <= 500).ToProperty();
                }).QuickCheckThrowOnFailure();
        }
    }
}
