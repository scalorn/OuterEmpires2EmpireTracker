// <copyright file="GameApiMetricsCollectorCounterTests.cs" company="OE2EmpireTracker">
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
    /// Property-based tests for GameApiMetricsCollector counter consistency.
    /// Feature: game-api-status-form
    /// Validates: Requirements 3.2, 3.3, 3.5
    /// </summary>
    [TestFixture]
    public class GameApiMetricsCollectorCounterTests
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
        // Property 1: Counter Consistency
        // TotalRequests == SuccessCount + ClientErrorCount + ServerErrorCount + ExceptionCount
        // after random sequences of OnRequestCompleted/OnRequestFailed with various status codes.
        // **Validates: Requirements 3.2**
        // -----------------------------------------------------------------------

        [Test]
        public void TotalRequests_EqualsSumOfFourCategories_AfterRandomSequence()
        {
            var actionGen =
                from statusCode in Gen.Frequency(
                    new WeightAndValue<Gen<int>>(5, Gen.Choose(200, 299)),
                    new WeightAndValue<Gen<int>>(2, Gen.Choose(300, 399)),
                    new WeightAndValue<Gen<int>>(2, Gen.Choose(400, 499)),
                    new WeightAndValue<Gen<int>>(1, Gen.Choose(500, 599)),
                    new WeightAndValue<Gen<int>>(1, Gen.Constant(0)))
                select statusCode;

            var sequenceGen =
                from actions in Gen.ListOf(actionGen)
                select actions.ToArray();

            Prop.ForAll(
                sequenceGen.ToArbitrary(),
                statusCodes =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();
                    var collector = GameApiMetricsCollector.Instance;

                    foreach (var code in statusCodes)
                    {
                        if (code == 0)
                        {
                            collector.OnRequestFailed("/test", "GET", "Timeout", "timed out");
                        }
                        else
                        {
                            collector.OnRequestCompleted("/test", "GET", code, 100, 0, 512);
                        }
                    }

                    var snapshot = collector.GetCurrentSnapshot();
                    var sum = snapshot.SuccessCount + snapshot.ClientErrorCount
                            + snapshot.ServerErrorCount + snapshot.ExceptionCount;

                    return (snapshot.TotalRequests == sum).ToProperty();
                }).QuickCheckThrowOnFailure();
        }

        // -----------------------------------------------------------------------
        // Property 6: Rate Limited Sub-Counter
        // Every 429 response increments both ClientErrorCount and RateLimitedCount;
        // RateLimitedCount <= ClientErrorCount always holds.
        // **Validates: Requirements 3.3**
        // -----------------------------------------------------------------------

        [Test]
        public void RateLimited429_IncrementsClientErrorAndRateLimited_SubCounterNeverExceedsParent()
        {
            var actionGen =
                from statusCode in Gen.Frequency(
                    new WeightAndValue<Gen<int>>(3, Gen.Choose(200, 299)),
                    new WeightAndValue<Gen<int>>(2, Gen.Choose(400, 428)),
                    new WeightAndValue<Gen<int>>(3, Gen.Constant(429)),
                    new WeightAndValue<Gen<int>>(1, Gen.Choose(430, 499)),
                    new WeightAndValue<Gen<int>>(1, Gen.Choose(500, 599)))
                select statusCode;

            var sequenceGen =
                from actions in Gen.ListOf(actionGen)
                select actions.ToArray();

            Prop.ForAll(
                sequenceGen.ToArbitrary(),
                statusCodes =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();
                    var collector = GameApiMetricsCollector.Instance;

                    long expectedRateLimited = 0;
                    long expectedClientErrors = 0;

                    foreach (var code in statusCodes)
                    {
                        collector.OnRequestCompleted("/test", "GET", code, 50, 0, 256);

                        if (code >= 400 && code < 500)
                        {
                            expectedClientErrors++;
                        }

                        if (code == 429)
                        {
                            expectedRateLimited++;
                        }
                    }

                    var snapshot = collector.GetCurrentSnapshot();

                    var rateLimitedCorrect = snapshot.RateLimitedCount == expectedRateLimited;
                    var clientErrorsCorrect = snapshot.ClientErrorCount == expectedClientErrors;
                    var subCounterInvariant = snapshot.RateLimitedCount <= snapshot.ClientErrorCount;

                    return (rateLimitedCorrect && clientErrorsCorrect && subCounterInvariant).ToProperty();
                }).QuickCheckThrowOnFailure();
        }

        // -----------------------------------------------------------------------
        // Property: Status codes outside 2xx/4xx/5xx (e.g. 3xx) are classified
        // as success and increment SuccessCount.
        // **Validates: Requirements 3.5**
        // -----------------------------------------------------------------------

        [Test]
        public void StatusCodes3xx_ClassifiedAsSuccess_IncrementsSuccessCount()
        {
            var codeGen =
                from code in Gen.Choose(300, 399)
                select code;

            var sequenceGen =
                from codes in Gen.ListOf(codeGen)
                where codes.Any()
                select codes.ToArray();

            Prop.ForAll(
                sequenceGen.ToArbitrary(),
                statusCodes =>
                {
                    GameApiMetricsCollector.Reset();
                    GameApiMetricsCollector.Initialize();
                    var collector = GameApiMetricsCollector.Instance;

                    foreach (var code in statusCodes)
                    {
                        collector.OnRequestCompleted("/test", "GET", code, 30, 0, 128);
                    }

                    var snapshot = collector.GetCurrentSnapshot();

                    var allSuccess = snapshot.SuccessCount == statusCodes.Length;
                    var noClientErrors = snapshot.ClientErrorCount == 0;
                    var noServerErrors = snapshot.ServerErrorCount == 0;
                    var noExceptions = snapshot.ExceptionCount == 0;
                    var totalCorrect = snapshot.TotalRequests == statusCodes.Length;

                    return (allSuccess && noClientErrors && noServerErrors && noExceptions && totalCorrect).ToProperty();
                }).QuickCheckThrowOnFailure();
        }
    }
}
