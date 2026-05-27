// <copyright file="GameApiMetricsCollectorTpsResetTests.cs" company="OE2EmpireTracker">
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
    /// Property-based tests for GameApiMetricsCollector TPS window accuracy and reset idempotency.
    /// Feature: game-api-status-form
    /// Validates: Requirements 2.1, 3.4, 8.1
    /// </summary>
    [TestFixture]
    public class GameApiMetricsCollectorTpsResetTests
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
        // Property 4: TPS Window Accuracy
        // TPS equals N/60 (within tolerance) when N requests are recorded within
        // a frozen 60-second window using SystemClock.
        // **Validates: Requirements 2.1**
        // -----------------------------------------------------------------------

        [Test]
        public void Tps_EqualsNDividedBy60_WhenNRequestsRecordedInFrozenWindow()
        {
            var nGen = from n in Gen.Choose(1, 500) select n;

            Prop.ForAll(
                nGen.ToArbitrary(),
                n =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();

                    var fixedTime = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
                    SystemClock.UtcNowFunc = () => fixedTime;

                    var collector = GameApiMetricsCollector.Instance;

                    for (int i = 0; i < n; i++)
                    {
                        collector.OnRequestCompleted("/test", "GET", 200, 50, 0, 256);
                    }

                    var snapshot = collector.GetCurrentSnapshot();
                    decimal expectedTps = n / 60m;
                    decimal tolerance = 0.001m;

                    return (Math.Abs(snapshot.CurrentTps - expectedTps) <= tolerance)
                        .ToProperty();
                }).QuickCheckThrowOnFailure();
        }

        // -----------------------------------------------------------------------
        // Property 5: Reset Idempotency
        // Calling ResetCounters() twice produces same state as calling once —
        // all counters zero, history empty, TPS samples cleared.
        // **Validates: Requirements 3.4, 8.1**
        // -----------------------------------------------------------------------

        [Test]
        public void ResetCounters_CalledTwice_ProducesSameStateAsCalledOnce()
        {
            var requestCountGen = from n in Gen.Choose(1, 200) select n;
            var failCountGen = from n in Gen.Choose(0, 50) select n;

            var inputGen =
                from requests in requestCountGen
                from failures in failCountGen
                select new { Requests = requests, Failures = failures };

            Prop.ForAll(
                inputGen.ToArbitrary(),
                input =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();

                    var fixedTime = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
                    SystemClock.UtcNowFunc = () => fixedTime;

                    var collector = GameApiMetricsCollector.Instance;

                    // Generate random metrics
                    for (int i = 0; i < input.Requests; i++)
                    {
                        collector.OnRequestCompleted("/api/data", "GET", 200, 100, 64, 1024);
                    }

                    for (int i = 0; i < input.Failures; i++)
                    {
                        collector.OnRequestFailed("/api/fail", "POST", "Timeout", "timed out");
                    }

                    // First reset
                    collector.ResetCounters();
                    var snapshot1 = collector.GetCurrentSnapshot();
                    var history1 = collector.GetHistory();
                    var tpsSamples1 = collector.GetTpsSamples();

                    // Second reset
                    collector.ResetCounters();
                    var snapshot2 = collector.GetCurrentSnapshot();
                    var history2 = collector.GetHistory();
                    var tpsSamples2 = collector.GetTpsSamples();

                    // Assert both states are identical
                    bool countersMatch =
                        snapshot1.TotalRequests == snapshot2.TotalRequests
                        && snapshot1.SuccessCount == snapshot2.SuccessCount
                        && snapshot1.ClientErrorCount == snapshot2.ClientErrorCount
                        && snapshot1.ServerErrorCount == snapshot2.ServerErrorCount
                        && snapshot1.ExceptionCount == snapshot2.ExceptionCount
                        && snapshot1.RateLimitedCount == snapshot2.RateLimitedCount
                        && snapshot1.BytesSent == snapshot2.BytesSent
                        && snapshot1.BytesReceived == snapshot2.BytesReceived
                        && snapshot1.CurrentTps == snapshot2.CurrentTps;

                    bool allCountersZero =
                        snapshot2.TotalRequests == 0
                        && snapshot2.SuccessCount == 0
                        && snapshot2.ClientErrorCount == 0
                        && snapshot2.ServerErrorCount == 0
                        && snapshot2.ExceptionCount == 0
                        && snapshot2.RateLimitedCount == 0
                        && snapshot2.BytesSent == 0
                        && snapshot2.BytesReceived == 0
                        && snapshot2.CurrentTps == 0;

                    bool historyEmpty = history1.Count == 0 && history2.Count == 0;

                    bool tpsSamplesCleared =
                        tpsSamples1.All(s => s == 0)
                        && tpsSamples2.All(s => s == 0);

                    return (countersMatch && allCountersZero && historyEmpty && tpsSamplesCleared)
                        .ToProperty();
                }).QuickCheckThrowOnFailure();
        }
    }
}
