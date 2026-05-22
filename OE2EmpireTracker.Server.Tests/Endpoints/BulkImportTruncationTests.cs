// -----------------------------------------------------------------------
// <copyright file="BulkImportTruncationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Unit tests for <see cref="BulkImportValidator"/> error collection and truncation behavior.
/// Validates: Req 9, Criteria 2–5.
/// </summary>
[TestFixture]
public class BulkImportTruncationTests
{
    private const string CharacterUUID = "test-char-uuid";

    /// <summary>
    /// Multiple validation errors are collected (not fail-fast).
    /// Three colonies each missing PlanetName produce exactly 3 errors.
    /// </summary>
    [Test]
    public void ValidateAll_MultipleErrors_CollectsAll()
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = new Colony[]
        {
            new Colony { UUID = "c1", OwnerUUID = CharacterUUID, ColonyName = "Alpha", PlanetName = null },
            new Colony { UUID = "c2", OwnerUUID = CharacterUUID, ColonyName = "Beta", PlanetName = null },
            new Colony { UUID = "c3", OwnerUUID = CharacterUUID, ColonyName = "Gamma", PlanetName = null },
        };

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(3));
        Assert.That(errors, Has.Count.EqualTo(3));
        Assert.That(errors[0].EntityType, Is.EqualTo("Colony"));
        Assert.That(errors[0].Field, Is.EqualTo("PlanetName"));
        Assert.That(errors[1].EntityUUID, Is.EqualTo("c2"));
        Assert.That(errors[2].EntityUUID, Is.EqualTo("c3"));
    }

    /// <summary>
    /// Exactly 100 errors results in truncated=false (threshold is strictly greater than 100).
    /// 100 colonies each missing PlanetName (ColonyName set) produce exactly 100 errors.
    /// </summary>
    [Test]
    public void ValidateAll_Exactly100Errors_TruncatedFalse()
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = CreateInvalidColonies(100);

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(100));
        Assert.That(errors, Has.Count.EqualTo(100));

        // Truncated is true only when totalErrors > 100
        Assert.That(totalErrors > 100, Is.False);
    }

    /// <summary>
    /// More than 100 errors results in truncated=true and only 100 errors returned.
    /// 105 colonies each missing PlanetName produce 105 total errors but only 100 in the list.
    /// </summary>
    [Test]
    public void ValidateAll_Over100Errors_TruncatedTrue()
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = CreateInvalidColonies(105);

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(105));
        Assert.That(errors, Has.Count.EqualTo(100));
        Assert.That(totalErrors > 100, Is.True);
    }

    /// <summary>
    /// A null entity in the array produces an error message referencing the index.
    /// </summary>
    [Test]
    public void ValidateAll_NullEntity_ReturnsErrorAtIndex()
    {
        var playerRoot = new PlayerRoot();
        playerRoot.Colony = new Colony[]
        {
            new Colony { UUID = "c1", OwnerUUID = CharacterUUID, PlanetName = "Earth", ColonyName = "Valid" },
            null,
            new Colony { UUID = "c3", OwnerUUID = CharacterUUID, PlanetName = "Mars", ColonyName = "Also Valid" },
        };

        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(1));
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0].EntityType, Is.EqualTo("Colony"));
        Assert.That(errors[0].Error, Is.EqualTo("Invalid JSON for entity at index 1"));
    }

    private static Colony[] CreateInvalidColonies(int count)
    {
        var colonies = new Colony[count];
        for (int i = 0; i < count; i++)
        {
            colonies[i] = new Colony
            {
                UUID = $"colony-{i}",
                OwnerUUID = CharacterUUID,
                ColonyName = $"Colony {i}",
                PlanetName = null,
            };
        }

        return colonies;
    }
}
