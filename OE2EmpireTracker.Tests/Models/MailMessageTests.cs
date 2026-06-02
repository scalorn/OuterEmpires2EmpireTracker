// <copyright file="MailMessageTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Unit tests for MailMessage model defaults and JSON serialization.
    /// Feature: in-game-mail
    /// </summary>
    [TestFixture]
    public class MailMessageTests
    {
        // ---------------------------------------------------------------
        // Default Values
        // ---------------------------------------------------------------

        [Test]
        public void DefaultConstructor_FromNameIsEmpty()
        {
            var msg = new MailMessage();
            Assert.That(msg.FromName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_ToNameIsEmpty()
        {
            var msg = new MailMessage();
            Assert.That(msg.ToName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_MailContentIsEmpty()
        {
            var msg = new MailMessage();
            Assert.That(msg.MailContent, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_LocalReadIsFalse()
        {
            var msg = new MailMessage();
            Assert.That(msg.LocalRead, Is.False);
        }

        [Test]
        public void DefaultConstructor_SentTimeIsEmpty()
        {
            var msg = new MailMessage();
            Assert.That(msg.SentTime, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_SubjectIsEmpty()
        {
            var msg = new MailMessage();
            Assert.That(msg.Subject, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_MailTypeIsNull()
        {
            var msg = new MailMessage();
            Assert.That(msg.MailType, Is.Null);
        }

        [Test]
        public void DefaultConstructor_MailIdIsZero()
        {
            var msg = new MailMessage();
            Assert.That(msg.MailId, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_MailReadIsFalse()
        {
            var msg = new MailMessage();
            Assert.That(msg.MailRead, Is.False);
        }

        // ---------------------------------------------------------------
        // JSON Serialization Round-Trip
        // ---------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_AllFieldsPreserved()
        {
            var original = new MailMessage
            {
                MailId = 1234,
                CharacterIdFrom = 5678,
                FromName = "System",
                CharacterIdTo = 9012,
                ToName = "PlayerName",
                SentTime = "2025-11-15T10:30:00",
                Subject = "Colony Report",
                MailRead = true,
                MailType = "C",
                MailContent = "Your colony has completed construction...",
                LocalRead = true,
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<MailMessage>(json);

            Assert.That(deserialized.MailId, Is.EqualTo(original.MailId));
            Assert.That(deserialized.CharacterIdFrom, Is.EqualTo(original.CharacterIdFrom));
            Assert.That(deserialized.FromName, Is.EqualTo(original.FromName));
            Assert.That(deserialized.CharacterIdTo, Is.EqualTo(original.CharacterIdTo));
            Assert.That(deserialized.ToName, Is.EqualTo(original.ToName));
            Assert.That(deserialized.SentTime, Is.EqualTo(original.SentTime));
            Assert.That(deserialized.Subject, Is.EqualTo(original.Subject));
            Assert.That(deserialized.MailRead, Is.EqualTo(original.MailRead));
            Assert.That(deserialized.MailType, Is.EqualTo(original.MailType));
            Assert.That(deserialized.MailContent, Is.EqualTo(original.MailContent));
            Assert.That(deserialized.LocalRead, Is.EqualTo(original.LocalRead));
        }

        // ---------------------------------------------------------------
        // Nullable MailType Serialization
        // ---------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_MailTypeNull_PreservedAsNull()
        {
            var original = new MailMessage
            {
                MailId = 100,
                MailType = null,
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<MailMessage>(json);

            Assert.That(deserialized.MailType, Is.Null);
        }

        [Test]
        public void JsonRoundTrip_MailTypeColony_PreservedAsC()
        {
            var original = new MailMessage
            {
                MailId = 101,
                MailType = "C",
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<MailMessage>(json);

            Assert.That(deserialized.MailType, Is.EqualTo("C"));
        }

        [Test]
        public void JsonRoundTrip_MailTypeResearch_PreservedAsR()
        {
            var original = new MailMessage
            {
                MailId = 102,
                MailType = "R",
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<MailMessage>(json);

            Assert.That(deserialized.MailType, Is.EqualTo("R"));
        }

        [Test]
        public void JsonRoundTrip_MailTypeSkill_PreservedAsS()
        {
            var original = new MailMessage
            {
                MailId = 103,
                MailType = "S",
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<MailMessage>(json);

            Assert.That(deserialized.MailType, Is.EqualTo("S"));
        }

        [Test]
        public void JsonDeserialization_FromApiFormat_MapsCorrectly()
        {
            string apiJson = @"{
                ""mailId"": 5555,
                ""characterIdFrom"": 100,
                ""fromName"": ""TraderJoe"",
                ""characterIdTo"": 200,
                ""toName"": ""Receiver"",
                ""sentTime"": ""2025-12-01T08:00:00"",
                ""subject"": ""Trade Offer"",
                ""mailRead"": false,
                ""mailType"": null,
                ""mailContent"": ""I have 500 Iron for sale."",
                ""localRead"": false
            }";

            var msg = JsonConvert.DeserializeObject<MailMessage>(apiJson);

            Assert.That(msg.MailId, Is.EqualTo(5555));
            Assert.That(msg.CharacterIdFrom, Is.EqualTo(100));
            Assert.That(msg.FromName, Is.EqualTo("TraderJoe"));
            Assert.That(msg.CharacterIdTo, Is.EqualTo(200));
            Assert.That(msg.ToName, Is.EqualTo("Receiver"));
            Assert.That(msg.SentTime, Is.EqualTo("2025-12-01T08:00:00"));
            Assert.That(msg.Subject, Is.EqualTo("Trade Offer"));
            Assert.That(msg.MailRead, Is.False);
            Assert.That(msg.MailType, Is.Null);
            Assert.That(msg.MailContent, Is.EqualTo("I have 500 Iron for sale."));
            Assert.That(msg.LocalRead, Is.False);
        }
    }
}
