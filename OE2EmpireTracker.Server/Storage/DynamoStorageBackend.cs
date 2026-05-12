using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    // --- Character Data (raw JSON) ---

    public async Task<string?> GetCharacterDataAsync(string characterUUID, string dataType)
    {
        return await GetItemDataAsync($"CharCollection#{characterUUID}", dataType);
    }

    public async Task<string?> GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        return await GetItemDataAsync($"CharEntity#{characterUUID}#{dataType}", entityUUID);
    }

    public async Task UpsertCharacterDataAsync(string characterUUID, string dataType, string json)
    {
        // Store the collection blob
        await PutItemDataAsync($"CharCollection#{characterUUID}", dataType, json);

        // Delete existing entities for this character+dataType
        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue($"CharEntity#{characterUUID}#{dataType}"),
            },
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        foreach (var item in scanResponse.Items)
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

        // Parse and insert individual entities
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var array = JArray.Parse(json);
                foreach (var item in array)
                {
                    var entityUuid = item["UUID"]?.ToString();
                    if (string.IsNullOrEmpty(entityUuid))
                    {
                        continue;
                    }

                    await PutItemDataAsync(
                        $"CharEntity#{characterUUID}#{dataType}",
                        entityUuid,
                        item.ToString(Formatting.Indented));
                }
            }
            catch (JsonReaderException)
            {
                // Not a JSON array — just store as collection blob only
            }
        }
    }

    public async Task UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json)
    {
        await PutItemDataAsync($"CharEntity#{characterUUID}#{dataType}", entityUUID, json);
        await RebuildCollectionBlobAsync(characterUUID, dataType);
    }

    public async Task DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        await DeleteItemAsync($"CharEntity#{characterUUID}#{dataType}", entityUUID);
        await RebuildCollectionBlobAsync(characterUUID, dataType);
    }

    public async Task<string?> GetAllCharacterDataAsync(string characterUUID)
    {
        var items = await ScanByPrefixAsync($"CharCollection#{characterUUID}", null);
        if (items.Count == 0)
        {
            return null;
        }

        // We need the SK (dataType) and Data for each item
        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue($"CharCollection#{characterUUID}"),
            },
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        var result = new JObject();
        foreach (var item in scanResponse.Items)
        {
            var dataType = item["SK"].S;
            var data = item["Data"].S;
            result[dataType] = JToken.Parse(data);
        }

        return result.HasValues ? result.ToString(Formatting.Indented) : null;
    }

    public async Task PutAllCharacterDataAsync(string characterUUID, string json)
    {
        var obj = JObject.Parse(json);
        foreach (var prop in obj.Properties())
        {
            await UpsertCharacterDataAsync(characterUUID, prop.Name, prop.Value.ToString(Formatting.Indented));
        }
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

    private async Task RebuildCollectionBlobAsync(string characterUUID, string dataType)
    {
        var scanRequest = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "begins_with(PK, :pk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue($"CharEntity#{characterUUID}#{dataType}"),
            },
        };

        var scanResponse = await _client.ScanAsync(scanRequest);
        var array = new JArray();
        foreach (var item in scanResponse.Items)
        {
            if (item.ContainsKey("Data"))
            {
                array.Add(JToken.Parse(item["Data"].S));
            }
        }

        await PutItemDataAsync(
            $"CharCollection#{characterUUID}",
            dataType,
            array.ToString(Formatting.Indented));
    }
}
