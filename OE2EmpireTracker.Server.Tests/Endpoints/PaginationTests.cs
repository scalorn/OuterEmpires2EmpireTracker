// -----------------------------------------------------------------------
// <copyright file="PaginationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Server.Tests;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 2: Pagination Slice Correctness.
/// For any total result set of N items and any valid page (>=1) and pageSize (1-100)
/// parameters, the paginated response SHALL contain exactly min(pageSize, N - (page-1)*pageSize)
/// items starting at offset (page-1)*pageSize, and totalCount SHALL equal N.
/// If pageSize exceeds 100, it SHALL be clamped to 100. If page is less than 1,
/// it SHALL be treated as 1.
/// **Validates: Requirements 1.4, 1.5**
/// Feature: sharing-visibility-system, Property 2: Pagination Slice Correctness.
/// </summary>
[TestFixture]
public class PaginationTests
{
    private TestServerFactory _factory = null!;
    private string _characterUUID = null!;

    /// <summary>
    /// Sets up the test server factory and seeds a character with Public sharing rules.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();

        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        _characterUUID = Guid.NewGuid().ToString();

        // Create a character with a Public sharing rule for Blueprints
        storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = _characterUUID,
            Name = "PaginationTestChar",
        }).GetAwaiter().GetResult();

        storage.UpsertSharingRulesAsync(_characterUUID, new List<SharingRule>
        {
            new SharingRule
            {
                Id = "pagination-rule-1",
                OwnerCharacterUUID = _characterUUID,
                TargetType = SharingTargetType.Public,
                TargetUUID = "public",
                DataType = null,
            },
        }).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Property: For any entity count N (0-150) and valid page/pageSize parameters,
    /// the response contains exactly min(pageSize, max(0, N - (page-1)*pageSize)) items.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PaginatedResponse_ContainsCorrectItemCount()
    {
        return Prop.ForAll(
            PaginationScenarioArbitrary(),
            scenario =>
            {
                RunItemCountProperty(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any entity count N and any page/pageSize, totalCount always equals N.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PaginatedResponse_TotalCountEqualsN()
    {
        return Prop.ForAll(
            PaginationScenarioArbitrary(),
            scenario =>
            {
                RunTotalCountProperty(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: If pageSize exceeds 100, it is clamped to 100.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PageSize_ClampedToMax100()
    {
        return Prop.ForAll(
            LargePageSizeArbitrary(),
            scenario =>
            {
                RunPageSizeClampProperty(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: If page is less than 1, it is treated as 1.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property Page_ClampedToMin1()
    {
        return Prop.ForAll(
            NegativePageArbitrary(),
            scenario =>
            {
                RunPageClampProperty(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: The returned items are the correct slice at offset (page-1)*pageSize.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PaginatedResponse_ReturnsCorrectSlice()
    {
        return Prop.ForAll(
            PaginationScenarioArbitrary(),
            scenario =>
            {
                RunSliceCorrectnessProperty(scenario).GetAwaiter().GetResult();
            });
    }

    // ===== Generators =====

    private static Arbitrary<PaginationScenario> PaginationScenarioArbitrary()
    {
        var gen = from entityCount in Gen.Choose(0, 150)
                  from page in Gen.Choose(1, 10)
                  from pageSize in Gen.Choose(1, 100)
                  select new PaginationScenario
                  {
                      EntityCount = entityCount,
                      Page = page,
                      PageSize = pageSize,
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<PaginationScenario> LargePageSizeArbitrary()
    {
        var gen = from entityCount in Gen.Choose(10, 150)
                  from page in Gen.Choose(1, 3)
                  from pageSize in Gen.Choose(101, 500)
                  select new PaginationScenario
                  {
                      EntityCount = entityCount,
                      Page = page,
                      PageSize = pageSize,
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<PaginationScenario> NegativePageArbitrary()
    {
        var gen = from entityCount in Gen.Choose(1, 50)
                  from page in Gen.Choose(-10, 0)
                  from pageSize in Gen.Choose(1, 50)
                  select new PaginationScenario
                  {
                      EntityCount = entityCount,
                      Page = page,
                      PageSize = pageSize,
                  };
        return Arb.From(gen);
    }

    private static List<string> ExtractUUIDs(JsonElement itemsArray)
    {
        var uuids = new List<string>();
        foreach (var item in itemsArray.EnumerateArray())
        {
            if (item.TryGetProperty("uuid", out var uuidProp))
            {
                var uuid = uuidProp.GetString();
                if (uuid != null)
                {
                    uuids.Add(uuid);
                }
            }
        }

        return uuids;
    }

    // ===== Property Implementations =====

    private async Task RunItemCountProperty(PaginationScenario scenario)
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var blueprintUUIDs = await SeedBlueprints(storage, scenario.EntityCount);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync(
                $"/api/v1/public/blueprints?page={scenario.Page}&pageSize={scenario.PageSize}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = json.GetProperty("items");
            var actualCount = items.GetArrayLength();

            var effectivePage = Math.Max(1, scenario.Page);
            var effectivePageSize = Math.Clamp(scenario.PageSize, 1, 100);
            var offset = (effectivePage - 1) * effectivePageSize;
            var expectedCount = Math.Min(effectivePageSize, Math.Max(0, scenario.EntityCount - offset));

            Assert.That(
                actualCount,
                Is.EqualTo(expectedCount),
                $"N={scenario.EntityCount}, page={scenario.Page}, pageSize={scenario.PageSize}: " +
                $"expected {expectedCount} items but got {actualCount}");
        }
        finally
        {
            await CleanupBlueprints(storage, blueprintUUIDs);
        }
    }

    private async Task RunTotalCountProperty(PaginationScenario scenario)
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var blueprintUUIDs = await SeedBlueprints(storage, scenario.EntityCount);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync(
                $"/api/v1/public/blueprints?page={scenario.Page}&pageSize={scenario.PageSize}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var totalCount = json.GetProperty("totalCount").GetInt32();

            Assert.That(
                totalCount,
                Is.EqualTo(scenario.EntityCount),
                $"N={scenario.EntityCount}: totalCount should be {scenario.EntityCount} " +
                $"but got {totalCount}");
        }
        finally
        {
            await CleanupBlueprints(storage, blueprintUUIDs);
        }
    }

    private async Task RunPageSizeClampProperty(PaginationScenario scenario)
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var blueprintUUIDs = await SeedBlueprints(storage, scenario.EntityCount);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync(
                $"/api/v1/public/blueprints?page={scenario.Page}&pageSize={scenario.PageSize}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = json.GetProperty("items");
            var actualCount = items.GetArrayLength();

            // pageSize > 100 should be clamped to 100
            var effectivePageSize = 100;
            var effectivePage = Math.Max(1, scenario.Page);
            var offset = (effectivePage - 1) * effectivePageSize;
            var expectedCount = Math.Min(effectivePageSize, Math.Max(0, scenario.EntityCount - offset));

            Assert.That(
                actualCount,
                Is.EqualTo(expectedCount),
                $"N={scenario.EntityCount}, page={scenario.Page}, pageSize={scenario.PageSize} (>100): " +
                $"expected {expectedCount} items (clamped to 100) but got {actualCount}");
        }
        finally
        {
            await CleanupBlueprints(storage, blueprintUUIDs);
        }
    }

    private async Task RunPageClampProperty(PaginationScenario scenario)
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var blueprintUUIDs = await SeedBlueprints(storage, scenario.EntityCount);

        try
        {
            using var client = _factory.CreateClient();

            // Request with page < 1
            var response = await client.GetAsync(
                $"/api/v1/public/blueprints?page={scenario.Page}&pageSize={scenario.PageSize}");
            response.EnsureSuccessStatusCode();

            // Also request with page=1 to compare
            var responsePage1 = await client.GetAsync(
                $"/api/v1/public/blueprints?page=1&pageSize={scenario.PageSize}");
            responsePage1.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var jsonPage1 = await responsePage1.Content.ReadFromJsonAsync<JsonElement>();

            var items = json.GetProperty("items");
            var itemsPage1 = jsonPage1.GetProperty("items");

            // When page < 1, it should be treated as page=1
            Assert.That(
                items.GetArrayLength(),
                Is.EqualTo(itemsPage1.GetArrayLength()),
                $"page={scenario.Page} should behave as page=1");

            // Verify the UUIDs match
            var uuids = ExtractUUIDs(items);
            var uuidsPage1 = ExtractUUIDs(itemsPage1);
            Assert.That(uuids, Is.EqualTo(uuidsPage1),
                $"page={scenario.Page} should return same items as page=1");
        }
        finally
        {
            await CleanupBlueprints(storage, blueprintUUIDs);
        }
    }

    private async Task RunSliceCorrectnessProperty(PaginationScenario scenario)
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var blueprintUUIDs = await SeedBlueprints(storage, scenario.EntityCount);

        try
        {
            using var client = _factory.CreateClient();

            // Get all items (page=1, pageSize=100 may not be enough for >100 items)
            // Fetch multiple pages to build the full ordered list
            var allUUIDs = new List<string>();
            var fetchPage = 1;
            while (true)
            {
                var fetchResponse = await client.GetAsync(
                    $"/api/v1/public/blueprints?page={fetchPage}&pageSize=100");
                fetchResponse.EnsureSuccessStatusCode();
                var fetchJson = await fetchResponse.Content.ReadFromJsonAsync<JsonElement>();
                var fetchItems = fetchJson.GetProperty("items");
                if (fetchItems.GetArrayLength() == 0)
                {
                    break;
                }

                allUUIDs.AddRange(ExtractUUIDs(fetchItems));
                if (fetchItems.GetArrayLength() < 100)
                {
                    break;
                }

                fetchPage++;
            }

            // Now request the specific page
            var response = await client.GetAsync(
                $"/api/v1/public/blueprints?page={scenario.Page}&pageSize={scenario.PageSize}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = json.GetProperty("items");
            var returnedUUIDs = ExtractUUIDs(items);

            // Compute expected slice
            var effectivePage = Math.Max(1, scenario.Page);
            var effectivePageSize = Math.Clamp(scenario.PageSize, 1, 100);
            var offset = (effectivePage - 1) * effectivePageSize;
            var expectedSlice = allUUIDs.Skip(offset).Take(effectivePageSize).ToList();

            Assert.That(
                returnedUUIDs,
                Is.EqualTo(expectedSlice),
                $"N={scenario.EntityCount}, page={scenario.Page}, pageSize={scenario.PageSize}: " +
                $"slice at offset {offset} does not match");
        }
        finally
        {
            await CleanupBlueprints(storage, blueprintUUIDs);
        }
    }

    // ===== Instance Helpers =====

    private async Task<List<string>> SeedBlueprints(IStorageBackend storage, int count)
    {
        var uuids = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var uuid = Guid.NewGuid().ToString();
            uuids.Add(uuid);
            var bp = new Blueprint { UUID = uuid, Name = $"BP-{i:D4}" };
            await storage.UpsertBlueprintAsync(_characterUUID, bp);
        }

        return uuids;
    }

    private async Task CleanupBlueprints(IStorageBackend storage, List<string> uuids)
    {
        foreach (var uuid in uuids)
        {
            await storage.DeleteBlueprintAsync(_characterUUID, uuid);
        }
    }

    // ===== Nested Types =====

    private sealed class PaginationScenario
    {
        public int EntityCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public override string ToString()
        {
            return $"Pagination(N={EntityCount}, page={Page}, pageSize={PageSize})";
        }
    }
}
