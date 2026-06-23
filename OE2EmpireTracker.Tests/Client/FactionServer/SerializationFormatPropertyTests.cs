// <copyright file="SerializationFormatPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Property-based tests for DTO serialization format.
    /// Feature: faction-server-typed-client
    /// </summary>
    [TestFixture]
    public class SerializationFormatPropertyTests
    {
        private static readonly JsonSerializerSettings ClientSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// Property 3: CamelCase Wire Format (BulkImportResult).
        /// All top-level JSON keys start with a lowercase character after serialization.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AllTopLevelKeys_StartWithLowercase_BulkImportResult()
        {
            var gen = from keys in Gen.ListOf(
                          Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from values in Gen.ListOf(Gen.Choose(0, 10000))
                      from total in Gen.Choose(0, 100000)
                      let dict = keys.Zip(values, (k, v) => new { k, v })
                          .GroupBy(x => x.k)
                          .ToDictionary(g => g.Key, g => g.First().v)
                      select new BulkImportResult
                      {
                          Imported = dict,
                          Total = total,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var obj = JObject.Parse(json);
                var keys = obj.Properties().Select(p => p.Name).ToList();

                foreach (string key in keys)
                {
                    Assert.That(char.IsLower(key[0]), Is.True,
                        $"Key '{key}' does not start with lowercase");
                }
            });
        }

        /// <summary>
        /// Property 3: CamelCase Wire Format (SyncResponse).
        /// All top-level JSON keys start with a lowercase character after serialization.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AllTopLevelKeys_StartWithLowercase_SyncResponse()
        {
            var gen = from factionsNull in Arb.Generate<bool>()
                      from charsNull in Arb.Generate<bool>()
                      select new SyncResponse
                      {
                          Factions = factionsNull ? null : new OE2EmpireTracker.Common.Models.ServerFaction[0],
                          Characters = charsNull ? null : new OE2EmpireTracker.Common.Models.ServerCharacter[0],
                          ServerTimestamp = SystemClock.UtcNow,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var obj = JObject.Parse(json);
                var keys = obj.Properties().Select(p => p.Name).ToList();

                foreach (string key in keys)
                {
                    Assert.That(char.IsLower(key[0]), Is.True,
                        $"Key '{key}' does not start with lowercase");
                }
            });
        }

        /// <summary>
        /// Property 3: CamelCase Wire Format (SharingRuleDto).
        /// All top-level JSON keys start with a lowercase character after serialization.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AllTopLevelKeys_StartWithLowercase_SharingRuleDto()
        {
            var gen = from id in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from owner in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from target in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from targetType in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from dataType in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from entityUUID in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      select new SharingRuleDto
                      {
                          Id = id,
                          OwnerCharacterUUID = owner,
                          TargetUUID = target,
                          TargetType = targetType,
                          DataType = dataType,
                          EntityUUID = entityUUID,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var obj = JObject.Parse(json);
                var keys = obj.Properties().Select(p => p.Name).ToList();

                foreach (string key in keys)
                {
                    Assert.That(char.IsLower(key[0]), Is.True,
                        $"Key '{key}' does not start with lowercase");
                }
            });
        }

        /// <summary>
        /// Property 4: Null Property Omission.
        /// For any SharingRuleDto with one or more nullable properties set to null,
        /// serialized JSON shall not contain keys for null-valued properties.
        /// **Validates: Requirements 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NullProperties_AreOmitted_SharingRuleDto()
        {
            var gen = from id in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from owner in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from target in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from targetType in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from dataType in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      from entityUUID in Gen.OneOf(Gen.Constant((string)null), Arb.Generate<NonEmptyString>().Select(s => s.Get))
                      where id == null || owner == null || target == null || targetType == null || dataType == null || entityUUID == null
                      select new SharingRuleDto
                      {
                          Id = id,
                          OwnerCharacterUUID = owner,
                          TargetUUID = target,
                          TargetType = targetType,
                          DataType = dataType,
                          EntityUUID = entityUUID,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var obj = JObject.Parse(json);
                var properties = obj.Properties().Select(p => p.Name).ToList();

                if (dto.Id == null)
                {
                    Assert.That(properties, Does.Not.Contain("id"));
                }

                if (dto.OwnerCharacterUUID == null)
                {
                    Assert.That(properties, Does.Not.Contain("ownerCharacterUUID"));
                }

                if (dto.TargetUUID == null)
                {
                    Assert.That(properties, Does.Not.Contain("targetUUID"));
                }

                if (dto.TargetType == null)
                {
                    Assert.That(properties, Does.Not.Contain("targetType"));
                }

                if (dto.DataType == null)
                {
                    Assert.That(properties, Does.Not.Contain("dataType"));
                }

                if (dto.EntityUUID == null)
                {
                    Assert.That(properties, Does.Not.Contain("entityUUID"));
                }
            });
        }

        /// <summary>
        /// Property 4: Null Property Omission (SyncResponse).
        /// For any SyncResponse with nullable array properties set to null,
        /// serialized JSON shall not contain keys for null-valued properties.
        /// **Validates: Requirements 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NullProperties_AreOmitted_SyncResponse()
        {
            var gen = from factionsNull in Arb.Generate<bool>()
                      from charsNull in Arb.Generate<bool>()
                      where factionsNull || charsNull
                      select new SyncResponse
                      {
                          Factions = factionsNull ? null : new OE2EmpireTracker.Common.Models.ServerFaction[0],
                          Characters = charsNull ? null : new OE2EmpireTracker.Common.Models.ServerCharacter[0],
                          ServerTimestamp = SystemClock.UtcNow,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var obj = JObject.Parse(json);
                var properties = obj.Properties().Select(p => p.Name).ToList();

                if (dto.Factions == null)
                {
                    Assert.That(properties, Does.Not.Contain("factions"));
                }

                if (dto.Characters == null)
                {
                    Assert.That(properties, Does.Not.Contain("characters"));
                }
            });
        }
    }
}
