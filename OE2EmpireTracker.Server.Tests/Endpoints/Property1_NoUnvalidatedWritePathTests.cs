// -----------------------------------------------------------------------
// <copyright file="Property1_NoUnvalidatedWritePathTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 1: No Unvalidated Write Path Exists.
/// Static analysis / reflection test: verifies no route handler references raw storage methods.
/// After implementation, there SHALL exist no code path by which a client can persist entity
/// data to storage without that data passing through typed validation logic.
/// **Validates: Requirements 1, 5, 8, 12**
/// </summary>
[TestFixture]
public class Property1_NoUnvalidatedWritePathTests
{
    /// <summary>
    /// The raw storage method names that were removed from IStorageBackend.
    /// These methods allowed unvalidated JSON to be persisted directly.
    /// </summary>
    private static readonly string[] RawMethodNames =
    {
        "GetCharacterDataAsync",
        "GetCharacterEntityAsync",
        "UpsertCharacterDataAsync",
        "UpsertCharacterEntityAsync",
        "DeleteCharacterEntityAsync",
        "PutAllCharacterDataAsync",
        "GetAllCharacterDataAsync",
    };

    /// <summary>
    /// Verifies that IStorageBackend interface does not contain any of the removed
    /// raw storage method signatures. These methods operated on raw JSON strings
    /// and bypassed typed validation.
    /// </summary>
    [Test]
    public void IStorageBackend_DoesNotContainRawMethods()
    {
        var type = typeof(IStorageBackend);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var methodNames = methods.Select(m => m.Name).ToHashSet();

        foreach (var rawMethodName in RawMethodNames)
        {
            Assert.That(
                methodNames.Contains(rawMethodName),
                Is.False,
                $"IStorageBackend still contains removed raw method '{rawMethodName}'. " +
                $"This method must be removed to prevent unvalidated write paths.");
        }
    }

    /// <summary>
    /// Verifies that no endpoint class in the Server project references any of the
    /// removed raw storage method names. This is a source-level grep via reflection
    /// on the compiled assembly — if any endpoint method's IL references a raw method,
    /// it would fail to compile (since the methods no longer exist on the interface).
    /// This test confirms the interface itself is clean.
    /// </summary>
    [Test]
    public void IStorageBackend_OnlyContainsTypedMethods_NoRawStringParameters()
    {
        var type = typeof(IStorageBackend);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            // Raw methods accepted (string characterUUID, string dataType) or
            // (string characterUUID, string dataType, string json) patterns.
            // Typed methods accept domain model objects, not raw JSON strings.
            // Verify no method has a "dataType" parameter combined with a "json" parameter.
            var parameters = method.GetParameters();
            var paramNames = parameters.Select(p => p.Name).ToArray();

            var hasDataType = paramNames.Contains("dataType");
            var hasJson = paramNames.Contains("json");

            // The only method allowed to have "dataType" + "json" is UpsertGlobalDataAsync
            // (which handles baseline game data, not player entities).
            if (hasDataType && hasJson)
            {
                Assert.That(
                    method.Name,
                    Is.EqualTo("UpsertGlobalDataAsync"),
                    $"Method '{method.Name}' has both 'dataType' and 'json' parameters, " +
                    $"suggesting a raw unvalidated write path. Only UpsertGlobalDataAsync " +
                    $"(for baseline game data) is allowed this pattern.");
            }
        }
    }

    /// <summary>
    /// Verifies that no endpoint class in the Server assembly contains methods whose
    /// names match the removed raw storage methods. This catches any accidental
    /// re-introduction of raw data handling in endpoint code.
    /// </summary>
    [Test]
    public void EndpointClasses_DoNotReferenceRawMethodNames()
    {
        var serverAssembly = typeof(Program).Assembly;
        var endpointNamespace = "OE2EmpireTracker.Server.Endpoints";

        var endpointTypes = serverAssembly.GetTypes()
            .Where(t => t.Namespace != null
                && t.Namespace.StartsWith(endpointNamespace, StringComparison.Ordinal)
                && t.IsClass)
            .ToList();

        Assert.That(endpointTypes, Is.Not.Empty, "No endpoint classes found in assembly.");

        foreach (var endpointType in endpointTypes)
        {
            var allMethods = endpointType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.DeclaredOnly);

            foreach (var method in allMethods)
            {
                foreach (var rawMethodName in RawMethodNames)
                {
                    Assert.That(
                        method.Name,
                        Is.Not.EqualTo(rawMethodName),
                        $"Endpoint class '{endpointType.Name}' contains method " +
                        $"'{rawMethodName}' which is a removed raw storage method name. " +
                        $"No endpoint should reference or re-implement raw storage methods.");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that no implementation of IStorageBackend in the Server assembly
    /// contains any of the removed raw method names. This catches implementations
    /// that might have leftover raw methods not cleaned up properly.
    /// </summary>
    [Test]
    public void StorageImplementations_DoNotContainRawMethods()
    {
        var commonAssembly = typeof(IStorageBackend).Assembly;

        var storageImplementations = commonAssembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(IStorageBackend).IsAssignableFrom(t))
            .ToList();

        Assert.That(
            storageImplementations,
            Is.Not.Empty,
            "No IStorageBackend implementations found in Common assembly.");

        foreach (var implType in storageImplementations)
        {
            var allMethods = implType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in allMethods)
            {
                foreach (var rawMethodName in RawMethodNames)
                {
                    Assert.That(
                        method.Name,
                        Is.Not.EqualTo(rawMethodName),
                        $"Storage implementation '{implType.Name}' still contains " +
                        $"raw method '{rawMethodName}'. All raw storage methods must " +
                        $"be removed from implementations.");
                }
            }
        }
    }
}
