// -----------------------------------------------------------------------
// <copyright file="DynamoDbBackendGlobalEntityTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for DynamoDbBackend server-global entity CRUD operations:
    /// ServerFaction, ServerCharacter, ApiToken, MembershipAction, StarSystem.
    /// Satisfies: Req 5, Criteria 1-2.
    /// </summary>
    [TestFixture]
    public class DynamoDbBackendGlobalEntityTests
    {
        private DynamoDbBackend _backend;
        private string _tablePrefix;

        [SetUp]
        public async Task SetUp()
        {
            if (!DynamoDbLocalFixture.IsAvailable)
            {
                Assert.Ignore("DynamoDB Local is not available");
            }

            _tablePrefix = "test_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _backend = new DynamoDbBackend(
                _tablePrefix,
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);
            await _backend.InitializeAsync();
        }

        [Test]
        public async Task ServerFaction_UpsertAndGet()
        {
            // Arrange
            var faction = new ServerFaction
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Faction Alpha",
                Description = "A test faction for integration tests",
                LeaderCharacterUUIDs = new List<string> { "leader-001", "leader-002" },
            };

            // Act
            await _backend.UpsertFactionAsync(faction);
            var retrieved = await _backend.GetFactionAsync(faction.UUID);

            // Assert
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.UUID, Is.EqualTo(faction.UUID));
            Assert.That(retrieved.Name, Is.EqualTo("Test Faction Alpha"));
            Assert.That(retrieved.Description, Is.EqualTo("A test faction for integration tests"));
            Assert.That(retrieved.LeaderCharacterUUIDs, Has.Count.EqualTo(2));
            Assert.That(retrieved.LeaderCharacterUUIDs, Contains.Item("leader-001"));
            Assert.That(retrieved.LeaderCharacterUUIDs, Contains.Item("leader-002"));
        }

        [Test]
        public async Task ServerCharacter_UpsertAndGetAll()
        {
            // Arrange
            var char1 = new ServerCharacter
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Character One",
                FactionUUID = "faction-aaa",
            };

            var char2 = new ServerCharacter
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Character Two",
                FactionUUID = "faction-bbb",
            };

            // Act
            await _backend.UpsertCharacterAsync(char1);
            await _backend.UpsertCharacterAsync(char2);
            var all = await _backend.GetAllCharactersAsync();

            // Assert
            Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(all.Any(c => c.UUID == char1.UUID && c.Name == "Character One"), Is.True);
            Assert.That(all.Any(c => c.UUID == char2.UUID && c.Name == "Character Two"), Is.True);
        }

        [Test]
        public async Task ApiToken_FindByHash()
        {
            // Arrange
            var token = new ApiToken
            {
                Id = Guid.NewGuid().ToString(),
                TokenHash = "sha256_" + Guid.NewGuid().ToString("N"),
                CharacterUUID = "char-xyz",
                Role = TokenRole.Character,
                CreatedUtc = SystemClock.UtcNow,
            };

            // Act
            await _backend.UpsertTokenAsync(token);
            var found = await _backend.FindTokenByHashAsync(token.TokenHash);

            // Assert
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Id, Is.EqualTo(token.Id));
            Assert.That(found.TokenHash, Is.EqualTo(token.TokenHash));
            Assert.That(found.CharacterUUID, Is.EqualTo("char-xyz"));
            Assert.That(found.Role, Is.EqualTo(TokenRole.Character));
        }

        [Test]
        public async Task MembershipAction_UpsertAndDelete()
        {
            // Arrange
            var action = new MembershipAction
            {
                Id = Guid.NewGuid().ToString(),
                FactionUUID = "faction-111",
                CharacterUUID = "char-222",
                Type = MembershipActionType.JoinRequest,
                CreatedUtc = SystemClock.UtcNow,
                ExpiresUtc = SystemClock.UtcNow.AddDays(7),
            };

            // Act — upsert and verify present
            await _backend.UpsertMembershipActionAsync(action);
            var actions = await _backend.GetFactionActionsAsync("faction-111");
            Assert.That(actions.Any(a => a.Id == action.Id), Is.True);

            // Act — delete and verify gone
            await _backend.DeleteMembershipActionAsync(action.Id);
            var actionsAfter = await _backend.GetFactionActionsAsync("faction-111");
            Assert.That(actionsAfter.Any(a => a.Id == action.Id), Is.False);
        }

        [Test]
        public async Task StarSystem_BulkUpsert()
        {
            // Arrange
            var systems = new List<StarSystem>
            {
                new StarSystem
                {
                    Id = 1001,
                    Name = "Sol",
                    X = 100.5m,
                    Y = 200.3m,
                    SpectralClass = "G2V",
                },
                new StarSystem
                {
                    Id = 1002,
                    Name = "Alpha Centauri",
                    X = 150.0m,
                    Y = 250.0m,
                    SpectralClass = "K1V",
                },
                new StarSystem
                {
                    Id = 1003,
                    Name = "Proxima",
                    X = 155.0m,
                    Y = 252.0m,
                    SpectralClass = "M5V",
                },
            };

            // Act
            await _backend.UpsertStarSystemsAsync(systems);
            var retrieved = await _backend.GetAllStarSystemsAsync();

            // Assert
            Assert.That(retrieved.Count, Is.EqualTo(3));
            Assert.That(retrieved.Any(s => s.Id == 1001 && s.Name == "Sol"), Is.True);
            Assert.That(retrieved.Any(s => s.Id == 1002 && s.Name == "Alpha Centauri"), Is.True);
            Assert.That(retrieved.Any(s => s.Id == 1003 && s.Name == "Proxima"), Is.True);
        }
    }
}
