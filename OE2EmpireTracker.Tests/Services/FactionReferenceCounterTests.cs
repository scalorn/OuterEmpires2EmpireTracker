using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class FactionReferenceCounterTests
    {
        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>(),
                new List<PlayerProfile>(),
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_EmptyUUID_ReturnsZero()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>(),
                new List<PlayerProfile>(),
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences(""), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NoReferences_ReturnsZero()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>
                {
                    new ExternalCharacter { UUID = "ec1", FactionUUID = "other-faction" }
                },
                new List<PlayerProfile>
                {
                    new PlayerProfile { UUID = "pp1", FactionUUID = "other-faction" }
                },
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences("target-faction"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_ExternalCharacterReferences_Counted()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>
                {
                    new ExternalCharacter { UUID = "ec1", FactionUUID = "faction-1" },
                    new ExternalCharacter { UUID = "ec2", FactionUUID = "faction-1" },
                    new ExternalCharacter { UUID = "ec3", FactionUUID = "faction-2" }
                },
                new List<PlayerProfile>(),
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences("faction-1"), Is.EqualTo(2));
        }

        [Test]
        public void CountReferences_PlayerProfileReferences_Counted()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>(),
                new List<PlayerProfile>
                {
                    new PlayerProfile { UUID = "pp1", FactionUUID = "faction-1" },
                    new PlayerProfile { UUID = "pp2", FactionUUID = "faction-2" }
                },
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences("faction-1"), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MixedReferences_SumsAll()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>
                {
                    new ExternalCharacter { UUID = "ec1", FactionUUID = "faction-1" },
                    new ExternalCharacter { UUID = "ec2", FactionUUID = "faction-1" }
                },
                new List<PlayerProfile>
                {
                    new PlayerProfile { UUID = "pp1", FactionUUID = "faction-1" }
                },
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences("faction-1"), Is.EqualTo(3));
        }

        [Test]
        public void CountReferences_NullCollections_HandledGracefully()
        {
            var counter = new FactionReferenceCounter(null, null, null);

            Assert.That(counter.CountReferences("faction-1"), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_EmptyFactionUUIDOnEntities_NotCounted()
        {
            var counter = new FactionReferenceCounter(
                new List<ExternalCharacter>
                {
                    new ExternalCharacter { UUID = "ec1", FactionUUID = "" }
                },
                new List<PlayerProfile>
                {
                    new PlayerProfile { UUID = "pp1", FactionUUID = "" }
                },
                new List<MarketTransaction>());

            Assert.That(counter.CountReferences(""), Is.EqualTo(0));
        }
    }
}