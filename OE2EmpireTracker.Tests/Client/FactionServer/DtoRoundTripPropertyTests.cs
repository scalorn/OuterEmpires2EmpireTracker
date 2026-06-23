// <copyright file="DtoRoundTripPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Property-based tests for DTO serialization round-trip.
    /// Feature: faction-server-typed-client.
    /// </summary>
    [TestFixture]
    public class DtoRoundTripPropertyTests
    {
        private static readonly JsonSerializerSettings ClientSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// Property 1: DTO Serialization Round-Trip (BulkImportResult).
        /// For any valid BulkImportResult, serializing to JSON then deserializing back
        /// produces an equivalent object.
        /// **Validates: Requirements 4.3, 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoundTrip_BulkImportResult()
        {
            var gen = from keys in Gen.ListOf(
                          from k in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                          select k)
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
                var roundTripped = JsonConvert.DeserializeObject<BulkImportResult>(json);

                Assert.That(roundTripped.Total, Is.EqualTo(dto.Total));
                Assert.That(roundTripped.Imported.Count, Is.EqualTo(dto.Imported.Count));
                foreach (var kvp in dto.Imported)
                {
                    Assert.That(roundTripped.Imported[kvp.Key], Is.EqualTo(kvp.Value));
                }
            });
        }

        /// <summary>
        /// Property 1: DTO Serialization Round-Trip (SyncResponse).
        /// For any valid SyncResponse, serializing to JSON then deserializing back
        /// produces an equivalent object.
        /// **Validates: Requirements 4.3, 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoundTrip_SyncResponse()
        {
            var factionGen = from uuid in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                             from name in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                             from desc in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                             from leaders in Gen.ListOf(
                                 Arb.Generate<NonEmptyString>().Select(s => s.Get))
                             select new ServerFaction
                             {
                                 UUID = uuid,
                                 Name = name,
                                 Description = desc,
                                 LeaderCharacterUUIDs = leaders.ToList(),
                             };

            var characterGen = from uuid in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                               from name in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                               from factionUuid in Gen.OneOf(
                                   Gen.Constant((string)null),
                                   Arb.Generate<NonEmptyString>().Select(s => s.Get))
                               select new ServerCharacter
                               {
                                   UUID = uuid,
                                   Name = name,
                                   FactionUUID = factionUuid,
                               };

            var gen = from factions in Gen.ArrayOf(factionGen)
                      from characters in Gen.ArrayOf(characterGen)
                      from ticks in Gen.Choose(0, int.MaxValue)
                      let ts = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ticks)
                      select new SyncResponse
                      {
                          Factions = factions,
                          Characters = characters,
                          ServerTimestamp = ts,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var rt = JsonConvert.DeserializeObject<SyncResponse>(json);

                Assert.That(rt.Factions.Length, Is.EqualTo(dto.Factions.Length));
                Assert.That(rt.Characters.Length, Is.EqualTo(dto.Characters.Length));
                Assert.That(rt.ServerTimestamp, Is.EqualTo(dto.ServerTimestamp));

                for (int i = 0; i < dto.Factions.Length; i++)
                {
                    Assert.That(rt.Factions[i].UUID, Is.EqualTo(dto.Factions[i].UUID));
                    Assert.That(rt.Factions[i].Name, Is.EqualTo(dto.Factions[i].Name));
                    Assert.That(rt.Factions[i].Description, Is.EqualTo(dto.Factions[i].Description));
                    CollectionAssert.AreEqual(dto.Factions[i].LeaderCharacterUUIDs, rt.Factions[i].LeaderCharacterUUIDs);
                }

                for (int i = 0; i < dto.Characters.Length; i++)
                {
                    Assert.That(rt.Characters[i].UUID, Is.EqualTo(dto.Characters[i].UUID));
                    Assert.That(rt.Characters[i].Name, Is.EqualTo(dto.Characters[i].Name));
                    Assert.That(rt.Characters[i].FactionUUID, Is.EqualTo(dto.Characters[i].FactionUUID));
                }
            });
        }

        /// <summary>
        /// Property 1: DTO Serialization Round-Trip (SharingRuleDto).
        /// For any valid SharingRuleDto, serializing to JSON then deserializing back
        /// produces an equivalent object.
        /// **Validates: Requirements 4.3, 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoundTrip_SharingRuleDto()
        {
            var gen = from id in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from owner in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from target in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from targetType in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from dataType in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from entityUuid in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      select new SharingRuleDto
                      {
                          Id = id,
                          OwnerCharacterUUID = owner,
                          TargetUUID = target,
                          TargetType = targetType,
                          DataType = dataType,
                          EntityUUID = entityUuid,
                      };

            return Prop.ForAll(Arb.From(gen), dto =>
            {
                string json = JsonConvert.SerializeObject(dto, ClientSettings);
                var rt = JsonConvert.DeserializeObject<SharingRuleDto>(json);

                Assert.That(rt.Id, Is.EqualTo(dto.Id));
                Assert.That(rt.OwnerCharacterUUID, Is.EqualTo(dto.OwnerCharacterUUID));
                Assert.That(rt.TargetUUID, Is.EqualTo(dto.TargetUUID));
                Assert.That(rt.TargetType, Is.EqualTo(dto.TargetType));
                Assert.That(rt.DataType, Is.EqualTo(dto.DataType));
                Assert.That(rt.EntityUUID, Is.EqualTo(dto.EntityUUID));
            });
        }
    }
}
