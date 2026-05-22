// -----------------------------------------------------------------------
// <copyright file="Property4_ErrorResponseCompletenessTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 4: Error Response Completeness.
/// For N entities with errors (N ≤ 100), the error response contains exactly N entries
/// and truncated=false. For N > 100, the response contains exactly 100 entries and
/// truncated=true.
/// **Validates: Requirements 9, Criteria 1, 2, 4**.
/// </summary>
[TestFixture]
public class Property4_ErrorResponseCompletenessTests
{
    private const string CharacterUUID = "prop4-char-uuid";

    /// <summary>
    /// For N ≤ 100 invalid entities, the error response contains exactly N entries
    /// and truncated is false (totalErrors does not exceed 100).
    /// </summary>
    /// <param name="n">Number of invalid entities to generate.</param>
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(99)]
    [TestCase(100)]
    public void ValidateAll_NErrorsAtOrBelowLimit_ReturnsExactlyNErrors(int n)
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = CreateColoniesWithOneErrorEach(n);

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(n), $"Expected totalErrors={n} for {n} invalid entities");
        Assert.That(errors.Count, Is.EqualTo(n), $"Expected errors.Count={n} for {n} invalid entities");
        Assert.That(totalErrors > 100, Is.False, "truncated should be false when totalErrors <= 100");
    }

    /// <summary>
    /// For N > 100 invalid entities, the error response contains exactly 100 entries
    /// and truncated is true (totalErrors exceeds 100).
    /// </summary>
    /// <param name="n">Number of invalid entities to generate.</param>
    [TestCase(101)]
    [TestCase(150)]
    [TestCase(200)]
    public void ValidateAll_NErrorsAboveLimit_Returns100ErrorsAndTruncated(int n)
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = CreateColoniesWithOneErrorEach(n);

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(n), $"Expected totalErrors={n} for {n} invalid entities");
        Assert.That(errors.Count, Is.EqualTo(100), "Expected errors.Count=100 when totalErrors > 100");
        Assert.That(totalErrors > 100, Is.True, "truncated should be true when totalErrors > 100");
    }

    /// <summary>
    /// Creates an array of colonies where each colony has exactly one validation error
    /// (PlanetName is null, but ColonyName is set so only one error per entity).
    /// </summary>
    private static Colony[] CreateColoniesWithOneErrorEach(int count)
    {
        var colonies = new Colony[count];
        for (int i = 0; i < count; i++)
        {
            colonies[i] = new Colony
            {
                UUID = $"colony-{i:D4}",
                OwnerUUID = CharacterUUID,
                ColonyName = $"Colony {i}",
                PlanetName = null,
            };
        }

        return colonies;
    }
}
