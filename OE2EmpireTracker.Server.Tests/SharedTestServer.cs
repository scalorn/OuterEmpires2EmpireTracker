// -----------------------------------------------------------------------
// <copyright file="SharedTestServer.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;

namespace OE2EmpireTracker.Server.Tests;

/// <summary>
/// Assembly-level shared test server. Creates a single TestServerFactory
/// instance that all integration test fixtures share, eliminating the
/// per-fixture factory startup overhead (~3-5s each × 30 fixtures).
/// </summary>
[SetUpFixture]
public class SharedTestServer
{
    private static TestServerFactory _factory = null!;

    /// <summary>Gets the shared factory instance.</summary>
    public static TestServerFactory Factory => _factory;

    /// <summary>
    /// Creates and seeds the shared test server factory once for all tests.
    /// </summary>
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
    }

    /// <summary>
    /// Disposes the shared test server factory after all tests complete.
    /// </summary>
    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        _factory?.Dispose();
    }
}
