// <copyright file="OverflowRuleExpansionTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for overflow rule expansion: backward compatibility and default values.
    /// </summary>
    [TestFixture]
    public class OverflowRuleExpansionTests
    {
        // ===== 11.1 — Backward compatibility tests =====

        /// <summary>
        /// Validates: Req 5, Criterion 5.1 — Missing RuleType defaults to SpecificResource.
        /// </summary>
        [Test]
        public void Deserialize_MissingRuleType_DefaultsToSpecificResource()
        {
            string json = @"{
                ""UUID"": ""test-uuid"",
                ""ResourceName"": ""Iron"",
                ""ResourcePurity"": ""High"",
                ""TriggerThreshold"": 100,
                ""DestinationUUID"": ""dest-uuid"",
                ""DeliveryRouteUUID"": ""route-uuid""
            }";

            var rule = JsonConvert.DeserializeObject<WarehouseOverflowRule>(json);

            Assert.That(rule.RuleType, Is.EqualTo(OverflowRuleType.SpecificResource));
        }

        /// <summary>
        /// Validates: Req 5, Criterion 5.2 — Integer threshold accepted as decimal.
        /// </summary>
        [Test]
        public void Deserialize_IntegerThreshold_AcceptedAsDecimal()
        {
            string json = @"{""TriggerThreshold"": 500}";

            var rule = JsonConvert.DeserializeObject<WarehouseOverflowRule>(json);

            Assert.That(rule.TriggerThreshold, Is.EqualTo(500m));
        }

        /// <summary>
        /// Validates: Req 5, Criterion 5.3 — Existing JSON property names preserved exactly.
        /// </summary>
        [Test]
        public void Serialize_PreservesExistingPropertyNames()
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = "test",
                ResourceName = "Iron",
                ResourcePurity = "High",
                TriggerThreshold = 100m,
                DestinationUUID = "dest",
                DeliveryRouteUUID = "route",
            };

            string json = JsonConvert.SerializeObject(rule);

            Assert.That(json, Does.Contain("\"ResourceName\""));
            Assert.That(json, Does.Contain("\"ResourcePurity\""));
            Assert.That(json, Does.Contain("\"TriggerThreshold\""));
            Assert.That(json, Does.Contain("\"DestinationType\""));
            Assert.That(json, Does.Contain("\"DestinationUUID\""));
            Assert.That(json, Does.Contain("\"DeliveryRouteUUID\""));
        }

        // ===== 11.2 — Default values and unknown RuleType tests =====

        /// <summary>
        /// Validates: Req 1, Criterion 1.3 — New rule defaults to SpecificResource.
        /// </summary>
        [Test]
        public void NewRule_DefaultsToSpecificResource()
        {
            var rule = new WarehouseOverflowRule();

            Assert.That(rule.RuleType, Is.EqualTo(OverflowRuleType.SpecificResource));
        }

        /// <summary>
        /// Validates: Req 1, Criterion 1.3 — New rule default threshold is zero.
        /// </summary>
        [Test]
        public void NewRule_DefaultThresholdIsZero()
        {
            var rule = new WarehouseOverflowRule();

            Assert.That(rule.TriggerThreshold, Is.EqualTo(0m));
        }

        /// <summary>
        /// Validates: Req 8, Criterion 8.2 — Unknown RuleType string throws during deserialization.
        /// Newtonsoft.Json with StringEnumConverter throws JsonSerializationException for unknown values.
        /// </summary>
        [Test]
        public void Deserialize_UnknownRuleType_ThrowsJsonSerializationException()
        {
            string json = @"{""RuleType"": ""UnknownFutureType""}";

            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<WarehouseOverflowRule>(json));
        }
    }
}
