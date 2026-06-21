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
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            throw new NotImplementedException();
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
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
            => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Permission Entities (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
            => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Intel and Audit (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
            => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline / Global Lookup Data (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // WarehouseOverflowRule, MailMessage, BankingTransaction (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline Data (stubs)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions) => throw new NotImplementedException();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Private Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private async Task<string> GetItemDataAsync(string pk, string sk)
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

        private async Task<List<string>> ScanByPrefixAsync(string pkPrefix, string skValue)
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
    }
}
