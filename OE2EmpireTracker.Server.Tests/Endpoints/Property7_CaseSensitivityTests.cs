// -----------------------------------------------------------------------
// <copyright file="Property7_CaseSensitivityTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 7: Case Sensitivity Enforcement.
/// Verifies that when JSON property names have wrong casing, case-sensitive
/// deserialization treats them as missing fields and validation fails.
/// **Validates: Requirements 7.1, 7.2**
/// </summary>
[TestFixture]
public class Property7_CaseSensitivityTests
{
    private const string CharacterUUID = "char-uuid-test";

    private static readonly JsonSerializerOptions CaseSensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    /// <summary>
    /// Colony with wrong-cased "planetName" instead of "PlanetName" is treated as missing
    /// after case-sensitive deserialization, causing validation to fail.
    /// </summary>
    /// <param name="wrongCaseProperty">The incorrectly-cased property name.</param>
    /// <param name="expectedMissingField">The field name expected in the validation error.</param>
    [TestCase("planetName", "PlanetName", Description = "camelCase planetName → PlanetName missing")]
    [TestCase("colonyName", "ColonyName", Description = "camelCase colonyName → ColonyName missing")]
    [TestCase("PLANETNAME", "PlanetName", Description = "ALL CAPS PLANETNAME → PlanetName missing")]
    public void CaseSensitivity_Colony_WrongCasing_ProducesValidationError(
        string wrongCaseProperty,
        string expectedMissingField)
    {
        // Build JSON with one property in wrong casing; other required fields correct
        string json;
        if (expectedMissingField == "PlanetName")
        {
            json = $$"""
                {
                    "Colony": [
                        {
                            "UUID": "colony-1",
                            "OwnerUUID": "{{CharacterUUID}}",
                            "{{wrongCaseProperty}}": "Earth",
                            "ColonyName": "Base Alpha"
                        }
                    ]
                }
                """;
        }
        else
        {
            json = $$"""
                {
                    "Colony": [
                        {
                            "UUID": "colony-1",
                            "OwnerUUID": "{{CharacterUUID}}",
                            "PlanetName": "Earth",
                            "{{wrongCaseProperty}}": "Base Alpha"
                        }
                    ]
                }
                """;
        }

        var playerRoot = JsonSerializer.Deserialize<PlayerRoot>(json, CaseSensitiveOptions)!;
        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.GreaterThan(0), "Expected validation errors for wrong casing");
        Assert.That(
            errors.Any(e => e.Field == expectedMissingField),
            Is.True,
            $"Expected error for missing field '{expectedMissingField}' due to case mismatch");
    }

    /// <summary>
    /// Blueprint with wrong-cased "name" instead of "Name" is treated as missing
    /// after case-sensitive deserialization, causing validation to fail.
    /// </summary>
    [TestCase("name", "Name", Description = "lowercase name → Name missing")]
    [TestCase("bluePrintType", "BluePrintType", Description = "camelCase bluePrintType → BluePrintType missing")]
    public void CaseSensitivity_Blueprint_WrongCasing_ProducesValidationError(
        string wrongCaseProperty,
        string expectedMissingField)
    {
        string json;
        if (expectedMissingField == "Name")
        {
            json = $$"""
                {
                    "Blueprint": [
                        {
                            "UUID": "bp-1",
                            "OwnerUUID": "{{CharacterUUID}}",
                            "{{wrongCaseProperty}}": "Laser Mk1",
                            "BluePrintType": "Weapon"
                        }
                    ]
                }
                """;
        }
        else
        {
            json = $$"""
                {
                    "Blueprint": [
                        {
                            "UUID": "bp-1",
                            "OwnerUUID": "{{CharacterUUID}}",
                            "Name": "Laser Mk1",
                            "{{wrongCaseProperty}}": "Weapon"
                        }
                    ]
                }
                """;
        }

        var playerRoot = JsonSerializer.Deserialize<PlayerRoot>(json, CaseSensitiveOptions)!;
        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.GreaterThan(0), "Expected validation errors for wrong casing");
        Assert.That(
            errors.Any(e => e.Field == expectedMissingField),
            Is.True,
            $"Expected error for missing field '{expectedMissingField}' due to case mismatch");
    }

    /// <summary>
    /// Verifies that correct PascalCase property names pass validation
    /// (control test to confirm the test setup is valid).
    /// </summary>
    [Test]
    public void CaseSensitivity_CorrectCasing_PassesValidation()
    {
        var json = $$"""
            {
                "Colony": [
                    {
                        "UUID": "colony-1",
                        "OwnerUUID": "{{CharacterUUID}}",
                        "PlanetName": "Earth",
                        "ColonyName": "Base Alpha"
                    }
                ],
                "Blueprint": [
                    {
                        "UUID": "bp-1",
                        "OwnerUUID": "{{CharacterUUID}}",
                        "Name": "Laser Mk1",
                        "BluePrintType": "Weapon"
                    }
                ]
            }
            """;

        var playerRoot = JsonSerializer.Deserialize<PlayerRoot>(json, CaseSensitiveOptions)!;
        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.EqualTo(0), "Correct casing should produce zero validation errors");
        Assert.That(errors, Is.Empty);
    }

    /// <summary>
    /// Verifies that multiple casing mutations in a single entity produce
    /// multiple validation errors (one per missing required field).
    /// </summary>
    [Test]
    public void CaseSensitivity_MultipleMutations_ProducesMultipleErrors()
    {
        // Both PlanetName and ColonyName are wrong-cased
        var json = $$"""
            {
                "Colony": [
                    {
                        "UUID": "colony-1",
                        "OwnerUUID": "{{CharacterUUID}}",
                        "planetname": "Earth",
                        "colonyname": "Base Alpha"
                    }
                ]
            }
            """;

        var playerRoot = JsonSerializer.Deserialize<PlayerRoot>(json, CaseSensitiveOptions)!;
        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, CharacterUUID);

        Assert.That(totalErrors, Is.GreaterThanOrEqualTo(2), "Expected at least 2 errors for 2 wrong-cased fields");
        Assert.That(errors.Any(e => e.Field == "PlanetName"), Is.True);
        Assert.That(errors.Any(e => e.Field == "ColonyName"), Is.True);
    }
}
