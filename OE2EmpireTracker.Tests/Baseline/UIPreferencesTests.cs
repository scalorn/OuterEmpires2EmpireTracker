using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class UIPreferencesTests
    {
        [Test]
        public void RoundTrip_FullPreferences_ProducesEquivalentStructure()
        {
            var original = new UIPreferences
            {
                MainWindow = new WindowPosition { Left = 100, Top = 50, Width = 1200, Height = 800 },
                Forms = new Dictionary<string, Dictionary<string, WindowState>>
                {
                    ["FormColony"] = new Dictionary<string, WindowState>
                    {
                        ["1"] = new WindowState
                        {
                            Position = new WindowPosition { Left = 10, Top = 20, Width = 800, Height = 600 },
                            FormState = new FormControlState
                            {
                                FilterTexts = new Dictionary<string, string> { ["txtFilter"] = "Earth" },
                                ComboSelections = new Dictionary<string, ComboState>
                                {
                                    ["cmbType"] = new ComboState { SelectedValue = "Mining", SelectedIndex = 2 }
                                },
                                Grids = new Dictionary<string, GridState>
                                {
                                    ["dgvResources"] = new GridState
                                    {
                                        SortColumnName = "Resource",
                                        SortDirection = "Ascending",
                                        Columns = new Dictionary<string, GridColumnState>
                                        {
                                            ["Resource"] = new GridColumnState { Width = 150, DisplayIndex = 0 },
                                            ["Purity"] = new GridColumnState { Width = 80, DisplayIndex = 1 }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            // MainWindow round-trip
            Assert.That(deserialized.MainWindow, Is.Not.Null);
            Assert.That(deserialized.MainWindow.Left, Is.EqualTo(100));
            Assert.That(deserialized.MainWindow.Top, Is.EqualTo(50));
            Assert.That(deserialized.MainWindow.Width, Is.EqualTo(1200));
            Assert.That(deserialized.MainWindow.Height, Is.EqualTo(800));

            // Forms dictionary round-trip
            Assert.That(deserialized.Forms, Contains.Key("FormColony"));
            Assert.That(deserialized.Forms["FormColony"], Contains.Key("1"));

            var ws = deserialized.Forms["FormColony"]["1"];
            Assert.That(ws.Position.Left, Is.EqualTo(10));
            Assert.That(ws.Position.Top, Is.EqualTo(20));
            Assert.That(ws.Position.Width, Is.EqualTo(800));
            Assert.That(ws.Position.Height, Is.EqualTo(600));

            // FormControlState round-trip
            Assert.That(ws.FormState.FilterTexts["txtFilter"], Is.EqualTo("Earth"));
            Assert.That(ws.FormState.ComboSelections["cmbType"].SelectedValue, Is.EqualTo("Mining"));
            Assert.That(ws.FormState.ComboSelections["cmbType"].SelectedIndex, Is.EqualTo(2));

            // GridState round-trip
            var grid = ws.FormState.Grids["dgvResources"];
            Assert.That(grid.SortColumnName, Is.EqualTo("Resource"));
            Assert.That(grid.SortDirection, Is.EqualTo("Ascending"));
            Assert.That(grid.Columns["Resource"].Width, Is.EqualTo(150));
            Assert.That(grid.Columns["Resource"].DisplayIndex, Is.EqualTo(0));
            Assert.That(grid.Columns["Purity"].Width, Is.EqualTo(80));
            Assert.That(grid.Columns["Purity"].DisplayIndex, Is.EqualTo(1));
        }

        [Test]
        public void RoundTrip_EmptyPreferences_ProducesEquivalentStructure()
        {
            var original = new UIPreferences();

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            Assert.That(deserialized.MainWindow, Is.Null);
            Assert.That(deserialized.Forms, Is.Not.Null);
            Assert.That(deserialized.Forms.Count, Is.EqualTo(0));
        }

        [Test]
        public void RoundTrip_NullFormState_PreservesNull()
        {
            var original = new UIPreferences
            {
                Forms = new Dictionary<string, Dictionary<string, WindowState>>
                {
                    ["FormSurvey"] = new Dictionary<string, WindowState>
                    {
                        ["1"] = new WindowState
                        {
                            Position = new WindowPosition { Left = 0, Top = 0, Width = 400, Height = 300 },
                            FormState = null
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            var ws = deserialized.Forms["FormSurvey"]["1"];
            Assert.That(ws.Position.Width, Is.EqualTo(400));
            Assert.That(ws.FormState, Is.Null);
        }

        [Test]
        public void Deserialize_UnknownKeysInJson_AreIgnored()
        {
            var json = @"{
                ""MainWindow"": { ""Left"": 50, ""Top"": 25, ""Width"": 1000, ""Height"": 700, ""FutureField"": true },
                ""Forms"": {},
                ""SomeUnknownTopLevelKey"": ""should be ignored"",
                ""AnotherUnknown"": 42
            }";

            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.MainWindow.Left, Is.EqualTo(50));
            Assert.That(deserialized.MainWindow.Width, Is.EqualTo(1000));
            Assert.That(deserialized.Forms.Count, Is.EqualTo(0));
        }

        [Test]
        public void RoundTrip_MultipleFormTypes_AllPreserved()
        {
            var original = new UIPreferences
            {
                Forms = new Dictionary<string, Dictionary<string, WindowState>>
                {
                    ["FormColony"] = new Dictionary<string, WindowState>
                    {
                        ["1"] = new WindowState { Position = new WindowPosition { Left = 10, Top = 10, Width = 500, Height = 400 } }
                    },
                    ["FormBlueprint"] = new Dictionary<string, WindowState>
                    {
                        ["1"] = new WindowState { Position = new WindowPosition { Left = 20, Top = 20, Width = 600, Height = 500 } },
                        ["2"] = new WindowState { Position = new WindowPosition { Left = 30, Top = 30, Width = 700, Height = 600 } }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);

            Assert.That(deserialized.Forms.Count, Is.EqualTo(2));
            Assert.That(deserialized.Forms["FormColony"].Count, Is.EqualTo(1));
            Assert.That(deserialized.Forms["FormBlueprint"].Count, Is.EqualTo(2));
            Assert.That(deserialized.Forms["FormBlueprint"]["2"].Position.Width, Is.EqualTo(700));
        }
    }
}
