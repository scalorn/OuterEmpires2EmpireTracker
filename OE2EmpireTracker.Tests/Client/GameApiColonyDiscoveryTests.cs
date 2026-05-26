// <copyright file="GameApiColonyDiscoveryTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// API data discovery test fixture for colony endpoints.
    /// Marked Explicit — requires real game API credentials via environment variables.
    /// Produces raw JSON output and a mapping report for data model validation.
    /// Feature: colony-api-sync
    /// **Validates: Requirements 16.1, 16.2, 16.3, 16.4, 16.5, 16.6**
    /// </summary>
    [TestFixture]
    [Explicit("Requires real game API credentials in OE2_APP_ID, OE2_CLIENT_ID, OE2_SECRET environment variables")]
    public class GameApiColonyDiscoveryTests
    {
        private static readonly string TestResultsDir = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestResults");

        private GameApiClient client;
        private string appId;
        private string accessToken;

        /// <summary>
        /// Reads credentials from environment variables, authenticates, and obtains an access token.
        /// Skips all tests if credentials are not configured.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            this.appId = Environment.GetEnvironmentVariable("OE2_APP_ID");
            string clientId = Environment.GetEnvironmentVariable("OE2_CLIENT_ID");
            string secret = Environment.GetEnvironmentVariable("OE2_SECRET");

            if (string.IsNullOrEmpty(this.appId) ||
                string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(secret))
            {
                Assert.Ignore("Game API credentials not configured. Set OE2_APP_ID, OE2_CLIENT_ID, OE2_SECRET environment variables.");
            }

            this.client = new GameApiClient("https://oe2-pub-api-dev.azure-api.net");

            var tokenResult = await this.client.ExchangeTokenAsync(this.appId, clientId, secret).ConfigureAwait(false);
            if (!tokenResult.Success)
            {
                Assert.Ignore("Token exchange failed: " + tokenResult.ErrorMessage);
            }

            this.accessToken = tokenResult.Token.AccessToken;

            if (!Directory.Exists(TestResultsDir))
            {
                Directory.CreateDirectory(TestResultsDir);
            }
        }

        /// <summary>
        /// Disposes the GameApiClient after all tests complete.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            this.client?.Dispose();
        }

        /// <summary>
        /// Authenticates and pulls the colony list, writing raw JSON to TestResults/colony-list-raw.json.
        /// </summary>
        [Test]
        [Order(1)]
        public async Task PullColonyList()
        {
            var result = await this.client.GetColonyListAsync(this.appId, this.accessToken).ConfigureAwait(false);

            Assert.That(result.Success, Is.True, "GetColonyListAsync failed. Json: " + (result.Json ?? "(null)"));
            Assert.That(result.Json, Is.Not.Null.And.Not.Empty, "Colony list JSON is empty");

            string outputPath = Path.Combine(TestResultsDir, "colony-list-raw.json");
            File.WriteAllText(outputPath, FormatJson(result.Json), Encoding.UTF8);

            TestContext.WriteLine("Colony list written to: " + outputPath);
            TestContext.WriteLine("Response length: " + result.Json.Length + " characters");

            // Quick sanity check — deserialize to verify structure
            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyListResponse>>(result.Json);
            Assert.That(envelope, Is.Not.Null, "Failed to deserialize colony list envelope");
            Assert.That(envelope.Success, Is.True, "API returned success=false: " + envelope.ReturnString);
            Assert.That(envelope.Data, Is.Not.Null, "Colony list data is null");

            TestContext.WriteLine("Colonies returned: " + envelope.Data.Colonies.Count);
            foreach (var colony in envelope.Data.Colonies)
            {
                TestContext.WriteLine(
                    "  [{0}] {1} on {2} in {3} (RemoteAccess={4})",
                    colony.ColonyId,
                    colony.ColonyName,
                    colony.SystemObjectName,
                    colony.SystemName,
                    colony.RemoteAccess);
            }
        }

        /// <summary>
        /// Pulls buildings for the first colony with RemoteAccess greater than 0.
        /// Writes raw JSON to TestResults/colony-buildings-raw.json.
        /// </summary>
        [Test]
        [Order(2)]
        public async Task PullColonyBuildings()
        {
            int colonyId = await GetFirstRemoteAccessColonyId().ConfigureAwait(false);

            var result = await this.client.GetColonyBuildingsAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);

            Assert.That(result.Success, Is.True, "GetColonyBuildingsAsync failed for colonyId=" + colonyId + ". Json: " + (result.Json ?? "(null)"));
            Assert.That(result.Json, Is.Not.Null.And.Not.Empty, "Buildings JSON is empty");

            string outputPath = Path.Combine(TestResultsDir, "colony-buildings-raw.json");
            File.WriteAllText(outputPath, FormatJson(result.Json), Encoding.UTF8);

            TestContext.WriteLine("Buildings written to: " + outputPath);
            TestContext.WriteLine("Response length: " + result.Json.Length + " characters");

            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyBuildingsResponse>>(result.Json);
            Assert.That(envelope, Is.Not.Null, "Failed to deserialize buildings envelope");
            Assert.That(envelope.Success, Is.True, "API returned success=false: " + envelope.ReturnString);
            Assert.That(envelope.Data, Is.Not.Null, "Buildings data is null");

            TestContext.WriteLine("Buildings returned: " + envelope.Data.Buildings.Count);
            foreach (var building in envelope.Data.Buildings)
            {
                TestContext.WriteLine(
                    "  [{0}] typeId={1} name={2} online={3} statusId={4}",
                    building.BuildingId,
                    building.ColonyBuildingTypeId,
                    building.BlueprintDesignName,
                    building.BuildingOnline,
                    building.StatusId);
            }
        }

        /// <summary>
        /// Pulls warehouse contents for the first colony with RemoteAccess greater than 0.
        /// Writes raw JSON to TestResults/colony-warehouse-raw.json.
        /// </summary>
        [Test]
        [Order(3)]
        public async Task PullColonyWarehouse()
        {
            int colonyId = await GetFirstRemoteAccessColonyId().ConfigureAwait(false);

            var result = await this.client.GetColonyWarehouseAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);

            Assert.That(result.Success, Is.True, "GetColonyWarehouseAsync failed for colonyId=" + colonyId + ". Json: " + (result.Json ?? "(null)"));
            Assert.That(result.Json, Is.Not.Null.And.Not.Empty, "Warehouse JSON is empty");

            string outputPath = Path.Combine(TestResultsDir, "colony-warehouse-raw.json");
            File.WriteAllText(outputPath, FormatJson(result.Json), Encoding.UTF8);

            TestContext.WriteLine("Warehouse written to: " + outputPath);
            TestContext.WriteLine("Response length: " + result.Json.Length + " characters");

            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWarehouseResponse>>(result.Json);
            Assert.That(envelope, Is.Not.Null, "Failed to deserialize warehouse envelope");
            Assert.That(envelope.Success, Is.True, "API returned success=false: " + envelope.ReturnString);
            Assert.That(envelope.Data, Is.Not.Null, "Warehouse data is null");

            TestContext.WriteLine("Warehouse items returned: " + envelope.Data.Contents.Count);
            foreach (var item in envelope.Data.Contents)
            {
                TestContext.WriteLine(
                    "  typeC={0} typeId={1} name={2} amount={3}",
                    item.TypeC,
                    item.TypeId,
                    item.ResourceName,
                    item.Amount);
            }
        }

        /// <summary>
        /// Deserializes the saved JSON responses, compares API fields to local model fields,
        /// and produces a mapping report at .kiro/specs/colony-api-sync/api-mapping-report.md.
        /// </summary>
        [Test]
        [Order(4)]
        public void ProduceMappingReport()
        {
            string colonyListPath = Path.Combine(TestResultsDir, "colony-list-raw.json");
            string buildingsPath = Path.Combine(TestResultsDir, "colony-buildings-raw.json");
            string warehousePath = Path.Combine(TestResultsDir, "colony-warehouse-raw.json");

            Assert.That(File.Exists(colonyListPath), Is.True, "colony-list-raw.json not found. Run PullColonyList first.");
            Assert.That(File.Exists(buildingsPath), Is.True, "colony-buildings-raw.json not found. Run PullColonyBuildings first.");
            Assert.That(File.Exists(warehousePath), Is.True, "colony-warehouse-raw.json not found. Run PullColonyWarehouse first.");

            string colonyListJson = File.ReadAllText(colonyListPath);
            string buildingsJson = File.ReadAllText(buildingsPath);
            string warehouseJson = File.ReadAllText(warehousePath);

            var colonyEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyListResponse>>(colonyListJson);
            var buildingsEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyBuildingsResponse>>(buildingsJson);
            var warehouseEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWarehouseResponse>>(warehouseJson);

            var report = new StringBuilder();
            report.AppendLine("# API Mapping Report");
            report.AppendLine();
            report.AppendLine("Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            report.AppendLine();

            // Section 1: TypeC values discovered
            AppendTypeCSection(report, warehouseEnvelope.Data);

            // Section 2: StatusId values discovered
            AppendStatusIdSection(report, buildingsEnvelope.Data);

            // Section 3: ColonyBuildingTypeId values discovered
            AppendBuildingTypeIdSection(report, buildingsEnvelope.Data);

            // Section 4: Colony list field comparison
            AppendColonyFieldComparison(report, colonyListJson);

            // Section 5: Buildings field comparison
            AppendBuildingsFieldComparison(report, buildingsJson);

            // Section 6: Warehouse field comparison
            AppendWarehouseFieldComparison(report, warehouseJson);

            string reportContent = report.ToString();

            // Write to spec directory
            string specDir = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", ".kiro", "specs", "colony-api-sync"));
            if (!Directory.Exists(specDir))
            {
                Directory.CreateDirectory(specDir);
            }

            string reportPath = Path.Combine(specDir, "api-mapping-report.md");
            File.WriteAllText(reportPath, reportContent, Encoding.UTF8);

            TestContext.WriteLine("Mapping report written to: " + reportPath);
            TestContext.WriteLine(reportContent);
        }

        private async Task<int> GetFirstRemoteAccessColonyId()
        {
            string colonyListPath = Path.Combine(TestResultsDir, "colony-list-raw.json");

            string json;
            if (File.Exists(colonyListPath))
            {
                json = File.ReadAllText(colonyListPath);
            }
            else
            {
                var result = await this.client.GetColonyListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                Assert.That(result.Success, Is.True, "GetColonyListAsync failed");
                json = result.Json;
            }

            var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyListResponse>>(json);
            Assert.That(envelope?.Data?.Colonies, Is.Not.Null.And.Not.Empty, "No colonies in response");

            var colony = envelope.Data.Colonies.FirstOrDefault(c => c.RemoteAccess > 0);
            Assert.That(colony, Is.Not.Null, "No colony with RemoteAccess > 0 found");

            TestContext.WriteLine("Using colony: [{0}] {1} (RemoteAccess={2})", colony.ColonyId, colony.ColonyName, colony.RemoteAccess);
            return colony.ColonyId;
        }

        private static string FormatJson(string json)
        {
            try
            {
                var obj = JToken.Parse(json);
                return obj.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private static void AppendTypeCSection(StringBuilder report, GameApiColonyWarehouseResponse warehouse)
        {
            report.AppendLine("## TypeC Values Discovered");
            report.AppendLine();
            report.AppendLine("These are the `typeC` codes found in warehouse items. This is the key discovery");
            report.AppendLine("for mapping API item types to the local `ItemType` enum.");
            report.AppendLine();
            report.AppendLine("| TypeC | Count | Example ResourceName |");
            report.AppendLine("|-------|-------|---------------------|");

            if (warehouse?.Contents == null || warehouse.Contents.Count == 0)
            {
                report.AppendLine("| (none) | 0 | — |");
            }
            else
            {
                var groups = warehouse.Contents
                    .GroupBy(i => i.TypeC ?? "(null)")
                    .OrderByDescending(g => g.Count());

                foreach (var group in groups)
                {
                    string example = group.First().ResourceName ?? "(unnamed)";
                    report.AppendLine(string.Format("| `{0}` | {1} | {2} |", group.Key, group.Count(), example));
                }
            }

            report.AppendLine();
        }

        private static void AppendStatusIdSection(StringBuilder report, GameApiColonyBuildingsResponse buildings)
        {
            report.AppendLine("## StatusId Values Discovered");
            report.AppendLine();
            report.AppendLine("These are the `statusId` values found in buildings. Maps to Built/Online/Constructing state.");
            report.AppendLine();
            report.AppendLine("| StatusId | Count | Example Building |");
            report.AppendLine("|----------|-------|-----------------|");

            if (buildings?.Buildings == null || buildings.Buildings.Count == 0)
            {
                report.AppendLine("| (none) | 0 | — |");
            }
            else
            {
                var groups = buildings.Buildings
                    .GroupBy(b => b.StatusId)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    string example = group.First().BlueprintDesignName ?? "(unnamed)";
                    report.AppendLine(string.Format("| {0} | {1} | {2} |", group.Key, group.Count(), example));
                }
            }

            report.AppendLine();
        }

        private static void AppendBuildingTypeIdSection(StringBuilder report, GameApiColonyBuildingsResponse buildings)
        {
            report.AppendLine("## ColonyBuildingTypeId Values Discovered");
            report.AppendLine();
            report.AppendLine("| ColonyBuildingTypeId | Count | Example Building |");
            report.AppendLine("|---------------------|-------|-----------------|");

            if (buildings?.Buildings == null || buildings.Buildings.Count == 0)
            {
                report.AppendLine("| (none) | 0 | — |");
            }
            else
            {
                var groups = buildings.Buildings
                    .GroupBy(b => b.ColonyBuildingTypeId)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    string example = group.First().BlueprintDesignName ?? "(unnamed)";
                    report.AppendLine(string.Format("| {0} | {1} | {2} |", group.Key, group.Count(), example));
                }
            }

            report.AppendLine();
        }

        private static void AppendColonyFieldComparison(StringBuilder report, string colonyListJson)
        {
            report.AppendLine("## Colony List Field Comparison");
            report.AppendLine();

            var parsed = JObject.Parse(colonyListJson);
            var dataToken = parsed["data"];
            JArray coloniesArray = null;

            if (dataToken != null && dataToken["colonies"] is JArray arr)
            {
                coloniesArray = arr;
            }

            if (coloniesArray == null || coloniesArray.Count == 0)
            {
                report.AppendLine("No colony data to analyze.");
                report.AppendLine();
                return;
            }

            // Get all JSON field names from first colony
            var apiFields = GetAllFieldNames(coloniesArray[0] as JObject);

            // Get local model properties
            var localProps = typeof(Colony).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Get DTO properties for comparison
            var dtoProps = typeof(GameApiColonyListItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            report.AppendLine("### API Fields (from raw JSON)");
            report.AppendLine();
            report.AppendLine("| JSON Field | DTO Property | Local Model Property | Notes |");
            report.AppendLine("|-----------|-------------|---------------------|-------|");

            foreach (string field in apiFields.OrderBy(f => f))
            {
                string dtoMatch = FindDtoMatch(field, dtoProps);
                string localMatch = FindLocalColonyMatch(field);
                string notes = string.Empty;

                if (string.IsNullOrEmpty(dtoMatch))
                {
                    notes = "NOT IN DTO";
                }

                if (string.IsNullOrEmpty(localMatch))
                {
                    notes += string.IsNullOrEmpty(notes) ? "NOT IN LOCAL MODEL" : ", NOT IN LOCAL MODEL";
                }

                report.AppendLine(string.Format("| `{0}` | {1} | {2} | {3} |",
                    field,
                    dtoMatch ?? "—",
                    localMatch ?? "—",
                    notes));
            }

            report.AppendLine();
        }

        private static void AppendBuildingsFieldComparison(StringBuilder report, string buildingsJson)
        {
            report.AppendLine("## Buildings Field Comparison");
            report.AppendLine();

            var parsed = JObject.Parse(buildingsJson);
            var dataToken = parsed["data"];
            JArray buildingsArray = null;

            if (dataToken != null && dataToken["buildings"] is JArray arr)
            {
                buildingsArray = arr;
            }

            if (buildingsArray == null || buildingsArray.Count == 0)
            {
                report.AppendLine("No buildings data to analyze.");
                report.AppendLine();
                return;
            }

            var apiFields = GetAllFieldNames(buildingsArray[0] as JObject);

            var dtoProps = typeof(GameApiColonyBuilding).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var localProps = typeof(ColonyStructure).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            report.AppendLine("### API Fields (from raw JSON)");
            report.AppendLine();
            report.AppendLine("| JSON Field | DTO Property | Local Model Property | Notes |");
            report.AppendLine("|-----------|-------------|---------------------|-------|");

            foreach (string field in apiFields.OrderBy(f => f))
            {
                string dtoMatch = FindDtoMatch(field, dtoProps);
                string localMatch = localProps.Contains(field) ? field : FindLocalBuildingMatch(field);
                string notes = string.Empty;

                if (string.IsNullOrEmpty(dtoMatch))
                {
                    notes = "NOT IN DTO";
                }

                if (string.IsNullOrEmpty(localMatch))
                {
                    notes += string.IsNullOrEmpty(notes) ? "NOT IN LOCAL MODEL" : ", NOT IN LOCAL MODEL";
                }

                report.AppendLine(string.Format("| `{0}` | {1} | {2} | {3} |",
                    field,
                    dtoMatch ?? "—",
                    localMatch ?? "—",
                    notes));
            }

            report.AppendLine();

            // Local-only fields
            report.AppendLine("### Local-Only Fields (ColonyStructure properties not in API)");
            report.AppendLine();
            var apiFieldSet = new HashSet<string>(apiFields, StringComparer.OrdinalIgnoreCase);
            var localOnlyFields = localProps.Where(p => !apiFieldSet.Contains(p) && !HasKnownMapping(p)).OrderBy(p => p);

            foreach (string field in localOnlyFields)
            {
                report.AppendLine("- " + field);
            }

            report.AppendLine();
        }

        private static void AppendWarehouseFieldComparison(StringBuilder report, string warehouseJson)
        {
            report.AppendLine("## Warehouse Field Comparison");
            report.AppendLine();

            var parsed = JObject.Parse(warehouseJson);
            var dataToken = parsed["data"];
            JArray contentsArray = null;

            if (dataToken != null && dataToken["contents"] is JArray arr)
            {
                contentsArray = arr;
            }

            if (contentsArray == null || contentsArray.Count == 0)
            {
                report.AppendLine("No warehouse data to analyze.");
                report.AppendLine();
                return;
            }

            var apiFields = GetAllFieldNames(contentsArray[0] as JObject);

            var dtoProps = typeof(GameApiWarehouseItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var localProps = typeof(Item).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            report.AppendLine("### API Fields (from raw JSON)");
            report.AppendLine();
            report.AppendLine("| JSON Field | DTO Property | Local Model Property | Notes |");
            report.AppendLine("|-----------|-------------|---------------------|-------|");

            foreach (string field in apiFields.OrderBy(f => f))
            {
                string dtoMatch = FindDtoMatch(field, dtoProps);
                string localMatch = localProps.Contains(field) ? field : FindLocalItemMatch(field);
                string notes = string.Empty;

                if (string.IsNullOrEmpty(dtoMatch))
                {
                    notes = "NOT IN DTO";
                }

                if (string.IsNullOrEmpty(localMatch))
                {
                    notes += string.IsNullOrEmpty(notes) ? "NOT IN LOCAL MODEL" : ", NOT IN LOCAL MODEL";
                }

                report.AppendLine(string.Format("| `{0}` | {1} | {2} | {3} |",
                    field,
                    dtoMatch ?? "—",
                    localMatch ?? "—",
                    notes));
            }

            report.AppendLine();

            // Local-only fields
            report.AppendLine("### Local-Only Fields (Item properties not in API)");
            report.AppendLine();
            var apiFieldSet = new HashSet<string>(apiFields, StringComparer.OrdinalIgnoreCase);
            var localOnlyFields = localProps.Where(p => !apiFieldSet.Contains(p) && !HasKnownItemMapping(p)).OrderBy(p => p);

            foreach (string field in localOnlyFields)
            {
                report.AppendLine("- " + field);
            }

            report.AppendLine();
        }

        private static List<string> GetAllFieldNames(JObject obj)
        {
            if (obj == null)
            {
                return new List<string>();
            }

            return obj.Properties().Select(p => p.Name).ToList();
        }

        private static string FindDtoMatch(string jsonField, Dictionary<string, PropertyInfo> dtoProps)
        {
            // Direct match (case-insensitive)
            if (dtoProps.ContainsKey(jsonField))
            {
                return dtoProps[jsonField].Name;
            }

            // Check JsonProperty attributes
            foreach (var kvp in dtoProps)
            {
                var jsonPropAttr = kvp.Value.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropAttr != null &&
                    string.Equals(jsonPropAttr.PropertyName, jsonField, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.Name;
                }
            }

            return null;
        }

        private static string FindLocalColonyMatch(string apiField)
        {
            // Known mappings from API field names to local Colony property names
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "colonyId", "(no local equivalent — uses UUID)" },
                { "colonyName", "ColonyName" },
                { "systemObjectName", "PlanetName" },
                { "systemName", "SystemName" },
                { "systemId", "SystemId" },
                { "colonySize", "ColonySize" },
                { "remoteAccess", "(used for flow control only)" },
                { "distance", "Distance" },
                { "surfaceVariation", "SurfaceVariation" },
                { "atmosVariation", "AtmosVariation" },
                { "hexValue", "HexValue" },
                { "systemObjectTypeName", "SystemObjectTypeName" },
                { "imagePreFix", "ImagePreFix" },
                { "manufacturingBlocked", "ManufacturingBlocked" },
                { "workerCurrentAttitude", "WorkerCurrentAttitude" },
                { "contentmentIndex", "ContentmentIndex" },
                { "hasManufacturing", "(activity flag — not stored)" },
                { "manufacturingInProgress", "(activity flag — not stored)" },
                { "hasMining", "(activity flag — not stored)" },
                { "miningInProgress", "(activity flag — not stored)" },
                { "hasRefining", "(activity flag — not stored)" },
                { "refiningInProgress", "(activity flag — not stored)" },
                { "hasResearch", "(activity flag — not stored)" },
                { "researchInProgress", "(activity flag — not stored)" },
            };

            string result;
            return mappings.TryGetValue(apiField, out result) ? result : null;
        }

        private static string FindLocalBuildingMatch(string apiField)
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "buildingId", "BuildingID" },
                { "colonyBuildingTypeId", "ColonyBuildingTypeId" },
                { "blueprintDesignName", "FlatpackBlueprintUUID (via name lookup)" },
                { "buildingOnline", "Properties[Online]" },
                { "statusId", "Properties[Built] (derived)" },
                { "constructingBuildingFinish", "BuildCompletionTime" },
                { "resourceName", "MiningSurveyResource" },
                { "resourceId", "ResourceId" },
                { "resourceIcon", "ResourceIcon" },
                { "maxRate", "(not stored locally)" },
                { "nextFinish", "ProcessCompletionTime" },
                { "manufactureNumber", "ManufacturingQuantity" },
                { "manufactureAmountPerRun", "ManufactureAmountPerRun" },
                { "durabilityCurrent", "DurabilityCurrent" },
                { "durabilityMax", "DurabilityMax" },
                { "opsStatusEffects", "OpsStatusEffects" },
                { "industries", "Industries" },
                { "detailsRequired", "DetailsRequired" },
                { "supportDetailsRequired", "SupportDetailsRequired" },
                { "buildingAttributes", "BuildingAttributes" },
                { "extraProperties", "ExtraProperties" },
            };

            string result;
            return mappings.TryGetValue(apiField, out result) ? result : null;
        }

        private static string FindLocalItemMatch(string apiField)
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", "GameItemId" },
                { "typeId", "BaseItemTypeID" },
                { "amount", "Quantity" },
                { "resourceName", "Name" },
                { "typeC", "ItemType (mapped)" },
                { "icon", "(not stored locally)" },
                { "jobRef", "JobRef" },
                { "jobDeliveryLoc", "JobDeliveryLoc" },
                { "healthPercentage", "HealthPercentage" },
                { "lastRepairHealthPercentage", "LastRepairHealthPercentage" },
                { "evolution", "Evolution" },
                { "mass", "Mass" },
                { "volume", "Volume" },
                { "properties", "ItemProperties" },
                { "jobName", "JobName" },
                { "jobTrack", "JobTrack" },
                { "shipPartType", "ShipPartType" },
            };

            string result;
            return mappings.TryGetValue(apiField, out result) ? result : null;
        }

        private static bool HasKnownMapping(string localProp)
        {
            // Properties that have known API mappings (just different names)
            var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BuildingID",
                "ColonyBuildingTypeId",
                "ResourceId",
                "ResourceIcon",
                "ManufactureAmountPerRun",
                "DurabilityCurrent",
                "DurabilityMax",
                "OpsStatusEffects",
                "Industries",
                "DetailsRequired",
                "SupportDetailsRequired",
                "BuildingAttributes",
                "ExtraProperties",
            };

            return mapped.Contains(localProp);
        }

        private static bool HasKnownItemMapping(string localProp)
        {
            var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GameItemId",
                "Quantity",
                "Name",
                "ItemType",
                "JobRef",
                "JobDeliveryLoc",
                "HealthPercentage",
                "LastRepairHealthPercentage",
                "Evolution",
                "Mass",
                "Volume",
                "ItemProperties",
                "JobName",
                "JobTrack",
                "ShipPartType",
            };

            return mapped.Contains(localProp);
        }
    }
}
