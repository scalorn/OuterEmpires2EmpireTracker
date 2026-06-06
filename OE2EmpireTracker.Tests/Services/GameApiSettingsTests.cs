// <copyright file="GameApiSettingsTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class GameApiSettingsTests
    {
        [Test]
        public void RoundTrip_NonDefaultValues_AllPropertiesPreserved()
        {
            var original = new GameApiConnectionSettings
            {
                ServerUrl = "https://api.outerempires2.com",
                PollingIntervalMinutes = 10,
                Enabled = true,
                Tps = 5.0,
            };

            var json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<GameApiConnectionSettings>(json);

            Assert.That(deserialized.ServerUrl, Is.EqualTo("https://api.outerempires2.com"));
            Assert.That(deserialized.PollingIntervalMinutes, Is.EqualTo(10));
            Assert.That(deserialized.Enabled, Is.True);
            Assert.That(deserialized.Tps, Is.EqualTo(5.0));
        }

        [Test]
        public void MissingSection_ProducesDefaults()
        {
            var json = @"{
                ""MainWindow"": { ""Left"": 0, ""Top"": 0, ""Width"": 800, ""Height"": 600 },
                ""Forms"": {},
                ""Thresholds"": {}
            }";

            var prefs = JsonConvert.DeserializeObject<UIPreferences>(json);

            Assert.That(prefs.GameApiConnection, Is.Not.Null);
            Assert.That(prefs.GameApiConnection.ServerUrl, Is.EqualTo(string.Empty));
            Assert.That(prefs.GameApiConnection.PollingIntervalMinutes, Is.EqualTo(5));
            Assert.That(prefs.GameApiConnection.Enabled, Is.False);
            Assert.That(prefs.GameApiConnection.Tps, Is.EqualTo(0.5));
        }

        [Test]
        public void Serialize_UsesPascalCasePropertyNames()
        {
            var settings = new GameApiConnectionSettings
            {
                ServerUrl = "https://example.com",
                PollingIntervalMinutes = 3,
                Enabled = true,
            };

            var json = JsonConvert.SerializeObject(settings);

            Assert.That(json, Does.Contain("\"ServerUrl\""));
            Assert.That(json, Does.Contain("\"PollingIntervalMinutes\""));
            Assert.That(json, Does.Contain("\"Enabled\""));
            Assert.That(json, Does.Contain("\"Tps\""));
            Assert.That(json, Does.Not.Contain("\"serverUrl\""));
            Assert.That(json, Does.Not.Contain("\"pollingIntervalMinutes\""));
            Assert.That(json, Does.Not.Contain("\"enabled\""));
            Assert.That(json, Does.Not.Contain("\"tps\""));
        }

        [Test]
        public void UIPreferences_RoundTrip_GameApiConnectionPreserved()
        {
            var original = new UIPreferences
            {
                GameApiConnection = new GameApiConnectionSettings
                {
                    ServerUrl = "https://game.example.org/api",
                    PollingIntervalMinutes = 15,
                    Enabled = true,
                    Tps = 2.5,
                },
            };

            var json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            Assert.That(deserialized.GameApiConnection, Is.Not.Null);
            Assert.That(deserialized.GameApiConnection.ServerUrl, Is.EqualTo("https://game.example.org/api"));
            Assert.That(deserialized.GameApiConnection.PollingIntervalMinutes, Is.EqualTo(15));
            Assert.That(deserialized.GameApiConnection.Enabled, Is.True);
            Assert.That(deserialized.GameApiConnection.Tps, Is.EqualTo(2.5));
        }

        [Test]
        public void Deserialize_WithoutTpsKey_DefaultsTo05()
        {
            var json = @"{
                ""ServerUrl"": ""https://example.com"",
                ""PollingIntervalMinutes"": 5,
                ""Enabled"": true
            }";

            var settings = JsonConvert.DeserializeObject<GameApiConnectionSettings>(json);

            Assert.That(settings.Tps, Is.EqualTo(0.5));
        }
    }
}
