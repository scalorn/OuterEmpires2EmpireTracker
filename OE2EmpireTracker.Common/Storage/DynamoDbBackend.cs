// -----------------------------------------------------------------------
// <copyright file="DynamoDbBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// DynamoDB-based implementation of <see cref="IStorageBackend"/>.
    /// Uses single-table design with PK = "EntityType#UUID" and SK = "DataType".
    /// Suitable for serverless or AWS-native deployments.
    /// </summary>
    public class DynamoDbBackend : IStorageBackend
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private readonly string _tableName;
        private readonly AmazonDynamoDBClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamoDbBackend"/> class.
        /// </summary>
        /// <param name="tableName">The DynamoDB table name.</param>
        /// <param name="region">The AWS region name (e.g. "us-east-1").</param>
        public DynamoDbBackend(string tableName, string region)
        {
            _tableName = tableName ?? "OE2EmpireTracker";
            _client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region ?? "us-east-1"));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamoDbBackend"/> class
        /// with a custom service URL (for DynamoDB Local testing).
        /// </summary>
        /// <param name="tableName">The DynamoDB table name.</param>
        /// <param name="region">The AWS region name (e.g. "us-east-1").</param>
        /// <param name="serviceUrl">The DynamoDB service endpoint URL (e.g. "http://localhost:8111").</param>
        public DynamoDbBackend(string tableName, string region, string serviceUrl)
        {
            _tableName = tableName ?? "OE2EmpireTracker";
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region ?? "us-east-1"),
                ServiceURL = serviceUrl,
            };
            _client = new AmazonDynamoDBClient(config);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Lifecycle
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                await _client.DescribeTableAsync(_tableName, ct);
                Log.Info("DynamoDbBackend connected to table {0}", _tableName);
            }
            catch (ResourceNotFoundException)
            {
                try
                {
                    Log.Info("Creating DynamoDB table {0}", _tableName);
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

                    Log.Info("DynamoDB table {0} created", _tableName);
                }
                catch (Exception ex) when (!(ex is StorageLoadException))
                {
                    throw new StorageLoadException("DynamoDB", _tableName, "Failed to create DynamoDB table during initialization", ex);
                }
            }
            catch (Exception ex) when (!(ex is StorageLoadException))
            {
                throw new StorageLoadException("DynamoDB", _tableName, "Failed to connect to DynamoDB during initialization", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                await _client.DescribeTableAsync(_tableName, ct);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "DynamoDB storage validation failed");
                return false;
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo { BackendType = "DynamoDB", Location = _tableName };
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Factions
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<ServerFaction> GetFactionAsync(string uuid)
        {
            var json = await GetItemDataAsync($"Faction#{uuid}", "Faction");
            return json != null ? JsonConvert.DeserializeObject<ServerFaction>(json) : null;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            var items = await ScanByPrefixAsync("Faction#", "Faction");
            return items
                .Select(j => JsonConvert.DeserializeObject<ServerFaction>(j))
                .Where(f => f != null)
                .Cast<ServerFaction>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertFactionAsync(ServerFaction faction)
        {
            await PutItemDataAsync(
                $"Faction#{faction.UUID}",
                "Faction",
                JsonConvert.SerializeObject(faction, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionAsync(string uuid)
        {
            await DeleteItemAsync($"Faction#{uuid}", "Faction");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Characters
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            var json = await GetItemDataAsync($"Character#{uuid}", "Character");
            return json != null ? JsonConvert.DeserializeObject<ServerCharacter>(json) : null;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            var items = await ScanByPrefixAsync("Character#", "Character");
            return items
                .Select(j => JsonConvert.DeserializeObject<ServerCharacter>(j))
                .Where(c => c != null)
                .Cast<ServerCharacter>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterAsync(ServerCharacter character)
        {
            await PutItemDataAsync(
                $"Character#{character.UUID}",
                "Character",
                JsonConvert.SerializeObject(character, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterAsync(string uuid)
        {
            await DeleteItemAsync($"Character#{uuid}", "Character");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Global Data
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<string> GetGlobalDataAsync(string dataType)
        {
            return await GetItemDataAsync("Global#Data", dataType);
        }

        /// <inheritdoc/>
        public async Task UpsertGlobalDataAsync(string dataType, string json)
        {
            await PutItemDataAsync("Global#Data", dataType, json);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Star Systems
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            var json = await GetItemDataAsync("Global#Data", "StarSystems");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<StarSystem>>(json) ?? new List<StarSystem>();
            }

            return new List<StarSystem>();
        }

        /// <inheritdoc/>
        public async Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            await PutItemDataAsync(
                "Global#Data",
                "StarSystems",
                JsonConvert.SerializeObject(systems, SerializerSettings));
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Colony Summaries
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            // Load star systems to resolve system name
            var systemsJson = await GetItemDataAsync("Global#Data", "StarSystems");
            if (systemsJson == null)
            {
                return Array.Empty<ColonySummary>();
            }

            var systems = JsonConvert.DeserializeObject<List<StarSystem>>(systemsJson) ?? new List<StarSystem>();
            var system = systems.FirstOrDefault(s => s.Id == systemId);
            if (system == null)
            {
                return Array.Empty<ColonySummary>();
            }

            // Scan all colonies across all characters
            var items = await ScanByPrefixAsync("Char#", "Colony#");
            var results = new List<ColonySummary>();
            foreach (var json in items)
            {
                var colony = JsonConvert.DeserializeObject<Colony>(json);
                if (colony != null && colony.SystemId == systemId)
                {
                    results.Add(new ColonySummary
                    {
                        ColonyName = colony.ColonyName ?? string.Empty,
                        Size = colony.Structures?.Count ?? 0,
                        PlanetName = colony.PlanetName ?? string.Empty,
                    });
                }
            }

            return results;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Tokens
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<ApiToken> FindTokenByHashAsync(string tokenHash)
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

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            var items = await ScanByPrefixAsync("Token#", "Token");
            return items
                .Select(j => JsonConvert.DeserializeObject<ApiToken>(j))
                .Where(t => t != null)
                .Cast<ApiToken>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertTokenAsync(ApiToken token)
        {
            await PutItemDataAsync(
                $"Token#{token.Id}",
                "Token",
                JsonConvert.SerializeObject(token, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteTokenAsync(string id)
        {
            await DeleteItemAsync($"Token#{id}", "Token");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Membership Actions
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
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

        /// <inheritdoc/>
        public async Task UpsertMembershipActionAsync(MembershipAction action)
        {
            await PutItemDataAsync(
                $"MemberAction#{action.Id}",
                "MemberAction",
                JsonConvert.SerializeObject(action, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteMembershipActionAsync(string id)
        {
            await DeleteItemAsync($"MemberAction#{id}", "MemberAction");
        }

        /// <inheritdoc/>
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Sharing Rules
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            var json = await GetItemDataAsync($"SharingRules#{characterUUID}", "SharingRules");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<SharingRule>>(json) ?? new List<SharingRule>();
            }

            return Array.Empty<SharingRule>();
        }

        /// <inheritdoc/>
        public async Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            await PutItemDataAsync(
                $"SharingRules#{characterUUID}",
                "SharingRules",
                JsonConvert.SerializeObject(rules, SerializerSettings));
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Preferences
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            var json = await GetItemDataAsync($"CharPrefs#{characterUUID}", "CharPrefs");
            return json != null ? JsonConvert.DeserializeObject<CharacterPreferences>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            await PutItemDataAsync(
                $"CharPrefs#{prefs.CharacterUUID}",
                "CharPrefs",
                JsonConvert.SerializeObject(prefs, SerializerSettings));
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Faction Permission Entities (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#{factionUUID}", "Capability#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionCapability>(j)).Where(c => c != null).Cast<FactionCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            await PutItemDataAsync($"FactionPerm#{capability.FactionUUID}", $"Capability#{capability.UUID}", JsonConvert.SerializeObject(capability, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"FactionPerm#{factionUUID}", $"Capability#{capabilityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#{factionUUID}", "ClearanceLevel#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionClearanceLevel>(j)).Where(c => c != null).Cast<FactionClearanceLevel>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            await PutItemDataAsync($"FactionPerm#{level.FactionUUID}", $"ClearanceLevel#{level.UUID}", JsonConvert.SerializeObject(level, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            await DeleteItemAsync($"FactionPerm#{factionUUID}", $"ClearanceLevel#{levelUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#{factionUUID}", "Group#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionPermissionGroup>(j)).Where(c => c != null).Cast<FactionPermissionGroup>().ToList();
        }

        /// <inheritdoc/>
        public async Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            var json = await GetItemDataAsync($"FactionPerm#{factionUUID}", $"Group#{groupUUID}");
            return json != null ? JsonConvert.DeserializeObject<FactionPermissionGroup>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            await PutItemDataAsync($"FactionPerm#{group.FactionUUID}", $"Group#{group.UUID}", JsonConvert.SerializeObject(group, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            await DeleteItemAsync($"FactionPerm#{factionUUID}", $"Group#{groupUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#Group#{groupUUID}", "GroupCap#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionGroupCapability>(j)).Where(c => c != null).Cast<FactionGroupCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            await PutItemDataAsync($"FactionPerm#Group#{item.GroupUUID}", $"GroupCap#{item.CapabilityUUID}", JsonConvert.SerializeObject(item, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"FactionPerm#Group#{groupUUID}", $"GroupCap#{capabilityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#Group#{groupUUID}", "GroupRule#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionGroupSharingRule>(j)).Where(c => c != null).Cast<FactionGroupSharingRule>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            await PutItemDataAsync($"FactionPerm#Group#{rule.GroupUUID}", $"GroupRule#{rule.UUID}", JsonConvert.SerializeObject(rule, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            await DeleteItemAsync($"FactionPerm#Group#{groupUUID}", $"GroupRule#{ruleUUID}");
        }

        /// <inheritdoc/>
        public async Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            var json = await GetItemDataAsync($"FactionPerm#{factionUUID}", $"Member#{characterUUID}");
            return json != null ? JsonConvert.DeserializeObject<FactionMemberPermissions>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            await PutItemDataAsync($"FactionPerm#{perms.FactionUUID}", $"Member#{perms.CharacterUUID}", JsonConvert.SerializeObject(perms, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#{factionUUID}", "Member#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionMemberPermissions>(j)).Where(c => c != null).Cast<FactionMemberPermissions>().ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            var items = await ScanByPrefixAsync($"FactionPerm#{factionUUID}", $"MemberCap#{characterUUID}#");
            return items.Select(j => JsonConvert.DeserializeObject<FactionMemberCapability>(j)).Where(c => c != null).Cast<FactionMemberCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            await PutItemDataAsync($"FactionPerm#{item.FactionUUID}", $"MemberCap#{item.CharacterUUID}#{item.CapabilityUUID}", JsonConvert.SerializeObject(item, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"FactionPerm#{factionUUID}", $"MemberCap#{characterUUID}#{capabilityUUID}");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Permission Entities (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#{characterUUID}", "Capability#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterCapability>(j)).Where(c => c != null).Cast<CharacterCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            await PutItemDataAsync($"CharPerm#{capability.OwnerCharacterUUID}", $"Capability#{capability.UUID}", JsonConvert.SerializeObject(capability, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"CharPerm#{characterUUID}", $"Capability#{capabilityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#{characterUUID}", "ClearanceLevel#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterClearanceLevel>(j)).Where(c => c != null).Cast<CharacterClearanceLevel>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            await PutItemDataAsync($"CharPerm#{level.OwnerCharacterUUID}", $"ClearanceLevel#{level.UUID}", JsonConvert.SerializeObject(level, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            await DeleteItemAsync($"CharPerm#{characterUUID}", $"ClearanceLevel#{levelUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#{characterUUID}", "Group#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterPermissionGroup>(j)).Where(c => c != null).Cast<CharacterPermissionGroup>().ToList();
        }

        /// <inheritdoc/>
        public async Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            var json = await GetItemDataAsync($"CharPerm#{characterUUID}", $"Group#{groupUUID}");
            return json != null ? JsonConvert.DeserializeObject<CharacterPermissionGroup>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            await PutItemDataAsync($"CharPerm#{group.OwnerCharacterUUID}", $"Group#{group.UUID}", JsonConvert.SerializeObject(group, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            await DeleteItemAsync($"CharPerm#{characterUUID}", $"Group#{groupUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#Group#{groupUUID}", "GroupCap#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterGroupCapability>(j)).Where(c => c != null).Cast<CharacterGroupCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            await PutItemDataAsync($"CharPerm#Group#{item.GroupUUID}", $"GroupCap#{item.CapabilityUUID}", JsonConvert.SerializeObject(item, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"CharPerm#Group#{groupUUID}", $"GroupCap#{capabilityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#Group#{groupUUID}", "GroupRule#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterGroupSharingRule>(j)).Where(c => c != null).Cast<CharacterGroupSharingRule>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            await PutItemDataAsync($"CharPerm#Group#{rule.GroupUUID}", $"GroupRule#{rule.UUID}", JsonConvert.SerializeObject(rule, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            await DeleteItemAsync($"CharPerm#Group#{groupUUID}", $"GroupRule#{ruleUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#{ownerCharacterUUID}", "Grantee#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterGranteePermissions>(j)).Where(c => c != null).Cast<CharacterGranteePermissions>().ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            await PutItemDataAsync($"CharPerm#{perms.OwnerCharacterUUID}", $"Grantee#{perms.GranteeUUID}", JsonConvert.SerializeObject(perms, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            await DeleteItemAsync($"CharPerm#{ownerCharacterUUID}", $"Grantee#{granteeUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            var items = await ScanByPrefixAsync($"CharPerm#{ownerCharacterUUID}", $"GranteeCap#{granteeUUID}#");
            return items.Select(j => JsonConvert.DeserializeObject<CharacterGranteeCapability>(j)).Where(c => c != null).Cast<CharacterGranteeCapability>().ToList();
        }

        /// <inheritdoc/>
        public async Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            await PutItemDataAsync($"CharPerm#{item.OwnerCharacterUUID}", $"GranteeCap#{item.GranteeUUID}#{item.CapabilityUUID}", JsonConvert.SerializeObject(item, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            await DeleteItemAsync($"CharPerm#{ownerCharacterUUID}", $"GranteeCap#{granteeUUID}#{capabilityUUID}");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Intel and Audit (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            var items = await ScanByPrefixAsync($"Intel#Target#{targetCharacterUUID}", "Comment#");
            return items
                .Select(j => JsonConvert.DeserializeObject<IntelComment>(j))
                .Where(c => c != null)
                .Cast<IntelComment>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            var json = await GetItemDataAsync($"Intel#Comment#{commentUUID}", "Data");
            return json != null ? JsonConvert.DeserializeObject<IntelComment>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertIntelCommentAsync(IntelComment comment)
        {
            var json = JsonConvert.SerializeObject(comment, SerializerSettings);
            await PutItemDataAsync($"Intel#Target#{comment.TargetCharacterUUID}", $"Comment#{comment.UUID}", json);
            await PutItemDataAsync($"Intel#Comment#{comment.UUID}", "Data", json);
        }

        /// <inheritdoc/>
        public async Task DeleteIntelCommentAsync(string commentUUID)
        {
            var comment = await GetIntelCommentAsync(commentUUID);
            if (comment != null)
            {
                await DeleteItemAsync($"Intel#Target#{comment.TargetCharacterUUID}", $"Comment#{comment.UUID}");
            }

            await DeleteItemAsync($"Intel#Comment#{commentUUID}", "Data");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            var items = await ScanByPrefixAsync($"Intel#Comment#{commentUUID}", "Share#");
            return items
                .Select(j => JsonConvert.DeserializeObject<IntelCommentFactionShare>(j))
                .Where(c => c != null)
                .Cast<IntelCommentFactionShare>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            var items = await ScanByPrefixAsync($"Intel#Faction#{factionUUID}", "Share#");
            return items
                .Select(j => JsonConvert.DeserializeObject<IntelCommentFactionShare>(j))
                .Where(c => c != null)
                .Cast<IntelCommentFactionShare>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            var json = JsonConvert.SerializeObject(share, SerializerSettings);
            await PutItemDataAsync($"Intel#Comment#{share.IntelCommentUUID}", $"Share#{share.UUID}", json);
            await PutItemDataAsync($"Intel#Faction#{share.FactionUUID}", $"Share#{share.UUID}", json);
        }

        /// <inheritdoc/>
        public async Task DeleteIntelShareAsync(string shareUUID)
        {
            // Find the share in all Intel#Comment# entries to get its details
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "begins_with(PK, :pk) AND SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue("Intel#Comment#"),
                    [":sk"] = new AttributeValue($"Share#{shareUUID}"),
                },
            };

            var scanResponse = await _client.ScanAsync(scanRequest);
            if (scanResponse.Items.Count > 0)
            {
                var item = scanResponse.Items[0];
                var json = item["Data"].S;
                var share = JsonConvert.DeserializeObject<IntelCommentFactionShare>(json);
                if (share != null)
                {
                    await DeleteItemAsync($"Intel#Comment#{share.IntelCommentUUID}", $"Share#{share.UUID}");
                    await DeleteItemAsync($"Intel#Faction#{share.FactionUUID}", $"Share#{share.UUID}");
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
        {
            var items = await ScanByPrefixAsync("Audit#Entries", "Entry#");
            var entries = items
                .Select(j => JsonConvert.DeserializeObject<PermissionAuditEntry>(j))
                .Where(e => e != null)
                .Cast<PermissionAuditEntry>();

            if (startDate.HasValue)
            {
                entries = entries.Where(e => e.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                entries = entries.Where(e => e.Timestamp <= endDate.Value);
            }

            if (actionType.HasValue)
            {
                entries = entries.Where(e => e.ActionType == actionType.Value);
            }

            if (!string.IsNullOrEmpty(actorUUID))
            {
                entries = entries.Where(e => e.ActorCharacterUUID == actorUUID);
            }

            if (!string.IsNullOrEmpty(targetUUID))
            {
                entries = entries.Where(e => e.TargetCharacterUUID == targetUUID);
            }

            return entries.ToList();
        }

        /// <inheritdoc/>
        public async Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            await PutItemDataAsync(
                "Audit#Entries",
                $"Entry#{entry.UUID}",
                JsonConvert.SerializeObject(entry, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "PK = :pk AND begins_with(SK, :sk)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue("Audit#Entries"),
                    [":sk"] = new AttributeValue("Entry#"),
                },
            };

            var scanResponse = await _client.ScanAsync(scanRequest);
            foreach (var item in scanResponse.Items)
            {
                var json = item["Data"].S;
                var entry = JsonConvert.DeserializeObject<PermissionAuditEntry>(json);
                if (entry != null && entry.Timestamp < cutoff)
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Colony#");
            return items
                .Select(j => JsonConvert.DeserializeObject<Colony>(j))
                .Where(c => c != null)
                .Cast<Colony>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Colony#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Colony>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"Colony#{entity.UUID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Colony#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Blueprint#");
            return items
                .Select(j => JsonConvert.DeserializeObject<Blueprint>(j))
                .Where(c => c != null)
                .Cast<Blueprint>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Blueprint#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Blueprint>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"Blueprint#{entity.UUID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Blueprint#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Survey#");
            return items
                .Select(j => JsonConvert.DeserializeObject<Survey>(j))
                .Where(c => c != null)
                .Cast<Survey>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Survey#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Survey>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"Survey#{entity.SurveyID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Survey#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "PlayerProfile#");
            return items
                .Select(j => JsonConvert.DeserializeObject<PlayerProfile>(j))
                .Where(c => c != null)
                .Cast<PlayerProfile>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"PlayerProfile#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<PlayerProfile>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"PlayerProfile#{entity.UUID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"PlayerProfile#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "DeliveryRoute#");
            return items
                .Select(j => JsonConvert.DeserializeObject<DeliveryRoute>(j))
                .Where(c => c != null)
                .Cast<DeliveryRoute>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"DeliveryRoute#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<DeliveryRoute>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"DeliveryRoute#{entity.UUID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"DeliveryRoute#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "DeliveryPlan#");
            return items
                .Select(j => JsonConvert.DeserializeObject<DeliveryPlan>(j))
                .Where(c => c != null)
                .Cast<DeliveryPlan>()
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"DeliveryPlan#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<DeliveryPlan>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            await PutItemDataAsync(
                $"Char#{characterUUID}",
                $"DeliveryPlan#{entity.UUID}",
                JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"DeliveryPlan#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Ship#");
            return items.Select(j => JsonConvert.DeserializeObject<Ship>(j)).Where(c => c != null).Cast<Ship>().ToList();
        }

        /// <inheritdoc/>
        public async Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Ship#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Ship>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"Ship#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Ship#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "ShipTemplate#");
            return items.Select(j => JsonConvert.DeserializeObject<ShipTemplate>(j)).Where(c => c != null).Cast<ShipTemplate>().ToList();
        }

        /// <inheritdoc/>
        public async Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"ShipTemplate#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<ShipTemplate>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"ShipTemplate#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"ShipTemplate#{entityUUID}");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline / Global Lookup Data (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        /// <inheritdoc/>
        public async Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "MarketListing#");
            return items.Select(j => JsonConvert.DeserializeObject<MarketListing>(j)).Where(c => c != null).Cast<MarketListing>().ToList();
        }

        /// <inheritdoc/>
        public async Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"MarketListing#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<MarketListing>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"MarketListing#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"MarketListing#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "MarketTransaction#");
            return items.Select(j => JsonConvert.DeserializeObject<MarketTransaction>(j)).Where(c => c != null).Cast<MarketTransaction>().ToList();
        }

        /// <inheritdoc/>
        public async Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"MarketTransaction#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<MarketTransaction>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"MarketTransaction#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"MarketTransaction#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "PricingPlan#");
            return items.Select(j => JsonConvert.DeserializeObject<PricingPlan>(j)).Where(c => c != null).Cast<PricingPlan>().ToList();
        }

        /// <inheritdoc/>
        public async Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"PricingPlan#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<PricingPlan>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"PricingPlan#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"PricingPlan#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "StockPlan#");
            return items.Select(j => JsonConvert.DeserializeObject<StockPlan>(j)).Where(c => c != null).Cast<StockPlan>().ToList();
        }

        /// <inheritdoc/>
        public async Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"StockPlan#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<StockPlan>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"StockPlan#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"StockPlan#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "StockProfile#");
            return items.Select(j => JsonConvert.DeserializeObject<StockProfile>(j)).Where(c => c != null).Cast<StockProfile>().ToList();
        }

        /// <inheritdoc/>
        public async Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"StockProfile#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<StockProfile>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"StockProfile#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"StockProfile#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "BuildPlan#");
            return items.Select(j => JsonConvert.DeserializeObject<BuildPlan>(j)).Where(c => c != null).Cast<BuildPlan>().ToList();
        }

        /// <inheritdoc/>
        public async Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"BuildPlan#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<BuildPlan>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"BuildPlan#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"BuildPlan#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "SupplyChain#");
            return items.Select(j => JsonConvert.DeserializeObject<SupplyChain>(j)).Where(c => c != null).Cast<SupplyChain>().ToList();
        }

        /// <inheritdoc/>
        public async Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"SupplyChain#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<SupplyChain>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"SupplyChain#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"SupplyChain#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Asteroid#");
            return items.Select(j => JsonConvert.DeserializeObject<Asteroid>(j)).Where(c => c != null).Cast<Asteroid>().ToList();
        }

        /// <inheritdoc/>
        public async Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Asteroid#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Asteroid>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"Asteroid#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Asteroid#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "Station#");
            return items.Select(j => JsonConvert.DeserializeObject<Station>(j)).Where(c => c != null).Cast<Station>().ToList();
        }

        /// <inheritdoc/>
        public async Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"Station#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Station>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertStationAsync(string characterUUID, Station entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"Station#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"Station#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "FactionContact#");
            return items.Select(j => JsonConvert.DeserializeObject<Faction>(j)).Where(c => c != null).Cast<Faction>().ToList();
        }

        /// <inheritdoc/>
        public async Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"FactionContact#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<Faction>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"FactionContact#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"FactionContact#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "ExternalCharacter#");
            return items.Select(j => JsonConvert.DeserializeObject<ExternalCharacter>(j)).Where(c => c != null).Cast<ExternalCharacter>().ToList();
        }

        /// <inheritdoc/>
        public async Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"ExternalCharacter#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<ExternalCharacter>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"ExternalCharacter#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"ExternalCharacter#{entityUUID}");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // WarehouseOverflowRule, MailMessage, BankingTransaction (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "WarehouseOverflowRule#");
            return items.Select(j => JsonConvert.DeserializeObject<WarehouseOverflowRule>(j)).Where(c => c != null).Cast<WarehouseOverflowRule>().ToList();
        }

        /// <inheritdoc/>
        public async Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"WarehouseOverflowRule#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<WarehouseOverflowRule>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"WarehouseOverflowRule#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"WarehouseOverflowRule#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "MailMessage#");
            return items.Select(j => JsonConvert.DeserializeObject<MailMessage>(j)).Where(c => c != null).Cast<MailMessage>().ToList();
        }

        /// <inheritdoc/>
        public async Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"MailMessage#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<MailMessage>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"MailMessage#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"MailMessage#{entityUUID}");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            var items = await ScanByPrefixAsync($"Char#{characterUUID}", "BankingTransaction#");
            return items.Select(j => JsonConvert.DeserializeObject<BankingTransaction>(j)).Where(c => c != null).Cast<BankingTransaction>().ToList();
        }

        /// <inheritdoc/>
        public async Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            var json = await GetItemDataAsync($"Char#{characterUUID}", $"BankingTransaction#{entityUUID}");
            return json != null ? JsonConvert.DeserializeObject<BankingTransaction>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            await PutItemDataAsync($"Char#{characterUUID}", $"BankingTransaction#{entity.UUID}", JsonConvert.SerializeObject(entity, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            await DeleteItemAsync($"Char#{characterUUID}", $"BankingTransaction#{entityUUID}");
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline Data (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public async Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "GameConstants");
            return json != null ? JsonConvert.DeserializeObject<BaselineGameConstants>(json) : null;
        }

        /// <inheritdoc/>
        public async Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            await PutItemDataAsync("Baseline#Data", "GameConstants", JsonConvert.SerializeObject(constants, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "BlueprintTypes");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<BlueprintType>>(json) ?? new List<BlueprintType>();
            }

            return new List<BlueprintType>();
        }

        /// <inheritdoc/>
        public async Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            await PutItemDataAsync("Baseline#Data", "BlueprintTypes", JsonConvert.SerializeObject(types, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "ShipClasses");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<ShipClass>>(json) ?? new List<ShipClass>();
            }

            return new List<ShipClass>();
        }

        /// <inheritdoc/>
        public async Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            await PutItemDataAsync("Baseline#Data", "ShipClasses", JsonConvert.SerializeObject(classes, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "TechLevels");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<TechLevel>>(json) ?? new List<TechLevel>();
            }

            return new List<TechLevel>();
        }

        /// <inheritdoc/>
        public async Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            await PutItemDataAsync("Baseline#Data", "TechLevels", JsonConvert.SerializeObject(levels, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "Commodities");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<Commodity>>(json) ?? new List<Commodity>();
            }

            return new List<Commodity>();
        }

        /// <inheritdoc/>
        public async Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            await PutItemDataAsync("Baseline#Data", "Commodities", JsonConvert.SerializeObject(commodities, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "RefiningRecipes");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<RefiningRecipe>>(json) ?? new List<RefiningRecipe>();
            }

            return new List<RefiningRecipe>();
        }

        /// <inheritdoc/>
        public async Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            await PutItemDataAsync("Baseline#Data", "RefiningRecipes", JsonConvert.SerializeObject(recipes, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "ResearchTimes");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<ResearchTimeEntry>>(json) ?? new List<ResearchTimeEntry>();
            }

            return new List<ResearchTimeEntry>();
        }

        /// <inheritdoc/>
        public async Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            await PutItemDataAsync("Baseline#Data", "ResearchTimes", JsonConvert.SerializeObject(entries, SerializerSettings));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            var json = await GetItemDataAsync("Baseline#Data", "PropertyTypeDefinitions");
            if (json != null)
            {
                return JsonConvert.DeserializeObject<List<PropertyTypeDefinition>>(json) ?? new List<PropertyTypeDefinition>();
            }

            return new List<PropertyTypeDefinition>();
        }

        /// <inheritdoc/>
        public async Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            await PutItemDataAsync("Baseline#Data", "PropertyTypeDefinitions", JsonConvert.SerializeObject(definitions, SerializerSettings));
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Private Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private async Task<string> GetItemDataAsync(string pk, string sk)
        {
            try
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
            catch (Exception ex)
            {
                throw new StorageLoadException("DynamoDB", _tableName, $"Failed to read item PK={pk} SK={sk} from DynamoDB", ex);
            }
        }

        private async Task PutItemDataAsync(string pk, string sk, string data)
        {
            try
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
            catch (Exception ex)
            {
                throw new StorageWriteException("DynamoDB", $"PutItem PK={pk} SK={sk}", "Failed to write item to DynamoDB", ex);
            }
        }

        private async Task DeleteItemAsync(string pk, string sk)
        {
            try
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
            catch (Exception ex)
            {
                throw new StorageWriteException("DynamoDB", $"DeleteItem PK={pk} SK={sk}", "Failed to delete item from DynamoDB", ex);
            }
        }

        private async Task<List<string>> ScanByPrefixAsync(string pkPrefix, string skValue)
        {
            try
            {
                var filterExpression = "begins_with(PK, :pk)";
                var expressionValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue(pkPrefix),
                };

                if (skValue != null)
                {
                    filterExpression += " AND begins_with(SK, :sk)";
                    expressionValues[":sk"] = new AttributeValue(skValue);
                }

                var scanRequest = new ScanRequest
                {
                    TableName = _tableName,
                    FilterExpression = filterExpression,
                    ExpressionAttributeValues = expressionValues,
                };

                var results = new List<string>();
                ScanResponse response = null;
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
            catch (Exception ex)
            {
                throw new StorageLoadException("DynamoDB", _tableName, $"Failed to scan by prefix PK={pkPrefix} from DynamoDB", ex);
            }
        }
    }
}
