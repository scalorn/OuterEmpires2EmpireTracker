using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// DynamoDB-based implementation of <see cref="IStorageBackend"/>.
/// Uses single-table design with PK = "EntityType#UUID" and SK = "DataType".
/// Suitable for serverless or AWS-native deployments.
/// </summary>
public class DynamoStorageBackend : IStorageBackend
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly string _tableName;
    private readonly AmazonDynamoDBClient _client;
    private readonly ILogger<DynamoStorageBackend> _logger;

    public DynamoStorageBackend(IConfiguration configuration, ILogger<DynamoStorageBackend> logger)
    {
        _tableName = configuration["Storage:DynamoTableName"] ?? "OE2EmpireTracker";
        var region = configuration["Storage:DynamoRegion"] ?? "us-east-1";
        _client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
        _logger = logger;
    }

    // --- Lifecycle ---

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await _client.DescribeTableAsync(_tableName, ct);
            _logger.LogInformation("DynamoStorageBackend connected to table {Table}", _tableName);
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogInformation("Creating DynamoDB table {Table}", _tableName);
            await _client.CreateTableAsync(new CreateTableRequest
            {
                TableName = _tableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("PK", KeyType.HASH),
                    new KeySchemaElement("SK", KeyType.RANGE),
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("PK", ScalarAttributeType.S),
                    new AttributeDefinition("SK", ScalarAttributeType.S),
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
            }, ct);

            // Wait for table to become active
            bool active = false;
            while (!active)
            {
                await Task.Delay(1000, ct);
                var desc = await _client.DescribeTableAsync(_tableName, ct);
                active = desc.Table.TableStatus == TableStatus.ACTIVE;
            }

            _logger.LogInformation("DynamoDB table {Table} created", _tableName);
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await _client.DescribeTableAsync(_tableName, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DynamoDB storage validation failed");
            return false;
        }
    }

    // --- Factions ---

    public async Task<ServerFaction?> GetFactionAsync(string uuid)
    {
        var json = await GetItemDataAsync($"Faction#{uuid}", "Faction");
        return json != null ? JsonConvert.DeserializeObject<ServerFaction>(json) : null;
    }

    public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
    {
        var items = await ScanByPrefixAsync("Faction#", "Faction");
        return items
            .Select(j => JsonConvert.DeserializeObject<ServerFaction>(j))
            .Where(f => f != null)
            .Cast<ServerFaction>()
            .ToList();
    }

    public async Task UpsertFactionAsync(ServerFaction faction)
    {
        await PutItemDataAsync(
            $"Faction#{faction.UUID}",
            "Faction",
            JsonConvert.SerializeObject(faction, SerializerSettings));
    }

    public async Task DeleteFactionAsync(string uuid)
    {
        await DeleteItemAsync($"Faction#{uuid}", "Faction");
    }

    // --- Characters ---

    public async Task<ServerCharacter?> GetCharacterAsync(string uuid)
    {
        var json = await GetItemDataAsync($"Character#{uuid}", "Character");
        return json != null ? JsonConvert.DeserializeObject<ServerCharacter>(json) : null;
    }

    public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
    {
        var items = await ScanByPrefixAsync("Character#", "Character");
        return items
            .Select(j => JsonConvert.DeserializeObject<ServerCharacter>(j))
            .Where(c => c != null)
            .Cast<ServerCharacter>()
            .ToList();
    }

    public async Task UpsertCharacterAsync(ServerCharacter character)
    {
        await PutItemDataAsync(
            $"Character#{character.UUID}",
            "Character",
            JsonConvert.SerializeObject(character, SerializerSettings));
    }

    public async Task DeleteCharacterAsync(string uuid)
    {
        await DeleteItemAsync($"Character#{uuid}", "Character");
    }

    // --- Global/Baseline Data ---

    public async Task<string?> GetGlobalDataAsync(string dataType)
    {
        return await GetItemDataAsync("Global#Data", dataType);
    }

    public async Task UpsertGlobalDataAsync(string dataType, string json)
    {
        await PutItemDataAsync("Global#Data", dataType, json);
    }

    // --- Tokens ---

    public async Task<ApiToken?> FindTokenByHashAsync(string tokenHash)
    {
        var items = await ScanByPrefixAsync("Token#", "Token");
        foreach (var json in items)
        {
            var token = JsonConvert.DeserializeObject<ApiToken>(json);
            if (token?.TokenHash == tokenHash)
            {
                return token;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
    {
        var items = await ScanByPrefixAsync("Token#", "Token");
        return items
            .Select(j => JsonConvert.DeserializeObject<ApiToken>(j))
            .Where(t => t != null)
            .Cast<ApiToken>()
            .ToList();
    }

    public async Task UpsertTokenAsync(ApiToken token)
    {
        await PutItemDataAsync(
            $"Token#{token.Id}",
            "Token",
            JsonConvert.SerializeObject(token, SerializerSettings));
    }

    public async Task DeleteTokenAsync(string id)
    {
        await DeleteItemAsync($"Token#{id}", "Token");
    }

    // --- Membership Actions ---

    public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
    {
        // Scan for all membership actions and filter by FactionUUID in the data
        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk) AND SK = :sk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue("MemberAction#"),
                [":sk"] = new AttributeValue("MemberAction"),
            },
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        var list = new List<MembershipAction>();
        foreach (var item in scanResponse.Items)
        {
            var json = item["Data"].S;
            var action = JsonConvert.DeserializeObject<MembershipAction>(json);
            if (action?.FactionUUID == factionUUID)
            {
                list.Add(action);
            }
        }

        return list;
    }

    public async Task UpsertMembershipActionAsync(MembershipAction action)
    {
        await PutItemDataAsync(
            $"MemberAction#{action.Id}",
            "MemberAction",
            JsonConvert.SerializeObject(action, SerializerSettings));
    }

    public async Task DeleteMembershipActionAsync(string id)
    {
        await DeleteItemAsync($"MemberAction#{id}", "MemberAction");
    }

    public async Task DeleteExpiredActionsAsync(DateTime cutoff)
    {
        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk) AND SK = :sk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue("MemberAction#"),
                [":sk"] = new AttributeValue("MemberAction"),
            },
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        foreach (var item in scanResponse.Items)
        {
            var json = item["Data"].S;
            var action = JsonConvert.DeserializeObject<MembershipAction>(json);
            if (action != null && action.ExpiresUtc <= cutoff)
            {
                await _client.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = item["PK"],
                        ["SK"] = item["SK"],
                    },
                });
            }
        }
    }

    // --- Sharing Rules ---

    public async Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
    {
        var json = await GetItemDataAsync($"SharingRules#{characterUUID}", "SharingRules");
        if (json != null)
        {
            return JsonConvert.DeserializeObject<List<SharingRule>>(json) ?? new List<SharingRule>();
        }

        return Array.Empty<SharingRule>();
    }

    public async Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
    {
        await PutItemDataAsync(
            $"SharingRules#{characterUUID}",
            "SharingRules",
            JsonConvert.SerializeObject(rules, SerializerSettings));
    }

    // --- Character Preferences ---

    public async Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID)
    {
        var json = await GetItemDataAsync($"CharPrefs#{characterUUID}", "CharPrefs");
        return json != null ? JsonConvert.DeserializeObject<CharacterPreferences>(json) : null;
    }

    public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
    {
        await PutItemDataAsync(
            $"CharPrefs#{prefs.CharacterUUID}",
            "CharPrefs",
            JsonConvert.SerializeObject(prefs, SerializerSettings));
    }

    // --- Faction Permission Entities ---

    public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        => throw new NotImplementedException();

    public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        => throw new NotImplementedException();

    public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task<FactionPermissionGroup?> GetFactionGroupAsync(string factionUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        => throw new NotImplementedException();

    public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        => throw new NotImplementedException();

    public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        => throw new NotImplementedException();

    public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        => throw new NotImplementedException();

    public Task<FactionMemberPermissions?> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        => throw new NotImplementedException();

    public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        => throw new NotImplementedException();

    public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        => throw new NotImplementedException();

    // --- Character Permission Entities ---

    public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        => throw new NotImplementedException();

    public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        => throw new NotImplementedException();

    public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task<CharacterPermissionGroup?> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        => throw new NotImplementedException();

    public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        => throw new NotImplementedException();

    public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        => throw new NotImplementedException();

    public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        => throw new NotImplementedException();

    public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        => throw new NotImplementedException();

    public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        => throw new NotImplementedException();

    public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        => throw new NotImplementedException();

    // --- Intel and Audit ---

    public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        => throw new NotImplementedException();

    public Task<IntelComment?> GetIntelCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task UpsertIntelCommentAsync(IntelComment comment)
        => throw new NotImplementedException();

    public Task DeleteIntelCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        => throw new NotImplementedException();

    public Task DeleteIntelShareAsync(string shareUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string? actorUUID = null, string? targetUUID = null)
        => throw new NotImplementedException();

    public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        => throw new NotImplementedException();

    public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        => throw new NotImplementedException();

    // --- Typed Entity CRUD (per-character domain entities) --- NOT YET IMPLEMENTED ---

    public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Colony?> GetColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertColonyAsync(string characterUUID, Colony entity) => throw new NotImplementedException();
    public Task DeleteColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Blueprint?> GetBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity) => throw new NotImplementedException();
    public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Survey?> GetSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertSurveyAsync(string characterUUID, Survey entity) => throw new NotImplementedException();
    public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<PlayerProfile?> GetPlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => throw new NotImplementedException();
    public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<DeliveryRoute?> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => throw new NotImplementedException();
    public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<DeliveryPlan?> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => throw new NotImplementedException();
    public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Ship?> GetShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertShipAsync(string characterUUID, Ship entity) => throw new NotImplementedException();
    public Task DeleteShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<ShipTemplate?> GetShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => throw new NotImplementedException();
    public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<MarketListing?> GetMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => throw new NotImplementedException();
    public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<MarketTransaction?> GetMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => throw new NotImplementedException();
    public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<PricingPlan?> GetPricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => throw new NotImplementedException();
    public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<StockPlan?> GetStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => throw new NotImplementedException();
    public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<StockProfile?> GetStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => throw new NotImplementedException();
    public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<BuildPlan?> GetBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => throw new NotImplementedException();
    public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<SupplyChain?> GetSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => throw new NotImplementedException();
    public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Asteroid?> GetAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => throw new NotImplementedException();
    public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Station?> GetStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStationAsync(string characterUUID, Station entity) => throw new NotImplementedException();
    public Task DeleteStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Faction?> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => throw new NotImplementedException();
    public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => throw new NotImplementedException();
    public Task<ExternalCharacter?> GetExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => throw new NotImplementedException();
    public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    // --- Private Helpers ---

    private async Task<string?> GetItemDataAsync(string pk, string sk)
    {
        var response = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue(pk),
                ["SK"] = new AttributeValue(sk),
            },
        });

        if (response.Item == null || !response.Item.ContainsKey("Data"))
        {
            return null;
        }

        return response.Item["Data"].S;
    }

    private async Task PutItemDataAsync(string pk, string sk, string data)
    {
        await _client.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue(pk),
                ["SK"] = new AttributeValue(sk),
                ["Data"] = new AttributeValue(data),
            },
        });
    }

    private async Task DeleteItemAsync(string pk, string sk)
    {
        await _client.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue(pk),
                ["SK"] = new AttributeValue(sk),
            },
        });
    }

    private async Task<List<string>> ScanByPrefixAsync(string pkPrefix, string? skValue)
    {
        var filterExpression = "begins_with(PK, :pk)";
        var expressionValues = new Dictionary<string, AttributeValue>
        {
            [":pk"] = new AttributeValue(pkPrefix),
        };

        if (skValue != null)
        {
            filterExpression += " AND SK = :sk";
            expressionValues[":sk"] = new AttributeValue(skValue);
        }

        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = filterExpression,
            ExpressionAttributeValues = expressionValues,
        };

        var results = new List<string>();
        ScanResponse? response = null;
        do
        {
            if (response?.LastEvaluatedKey?.Count > 0)
            {
                scanRequest.ExclusiveStartKey = response.LastEvaluatedKey;
            }

            response = await _client.ScanAsync(scanRequest);
            foreach (var item in response.Items)
            {
                if (item.ContainsKey("Data"))
                {
                    results.Add(item["Data"].S);
                }
            }
        }
        while (response.LastEvaluatedKey?.Count > 0);

        return results;
    }

}