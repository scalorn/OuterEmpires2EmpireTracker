using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verifies that read-only display forms (FormColonyActivity, FormColonyDailyBuild)
    /// and pure dialog forms (FormAutoFill) do not hold persistent mutable entity references
    /// as class-level fields.
    /// </summary>
    [TestFixture]
    public class ReadOnlyDisplayFormVerificationTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly string[] MutableEntityTypes = new[]
        {
            "Colony",
            "DeliveryRoute",
            "DeliveryPlan",
            "Blueprint",
            "Survey",
            "PlayerProfile",
            "ColonyStructure",
            "Ship",
            "ShipTemplate",
            "Station",
        };

        private static Regex BuildFieldPattern(string typeName)
        {
            return new Regex(
                @"^\s*(private|protected|internal|public)\s+(static\s+)?(readonly\s+)?" + typeName + @"\s+\w+\s*[;=]",
                RegexOptions.Compiled);
        }

        [Test]
        public void FormColonyActivity_NoMutableEntityFields()
        {
            var formFile = Path.Combine(SourceRoot, "Forms", "ColonyActivity", "FormColonyActivity.cs");
            Assert.That(File.Exists(formFile), Is.True, "FormColonyActivity.cs not found");

            var violations = ScanFileForMutableEntityFields(formFile);
            Assert.That(violations, Is.Empty,
                "FormColonyActivity holds mutable entity fields:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FormColonyDailyBuild_NoMutableEntityFields()
        {
            var formFile = Path.Combine(SourceRoot, "Forms", "ColonyDailyBuild", "FormColonyDailyBuild.cs");
            Assert.That(File.Exists(formFile), Is.True, "FormColonyDailyBuild.cs not found");

            var violations = ScanFileForMutableEntityFields(formFile);
            Assert.That(violations, Is.Empty,
                "FormColonyDailyBuild holds mutable entity fields:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FormAutoFill_NoEntityDataAccess()
        {
            var formFile = Path.Combine(SourceRoot, "Forms", "DeliveryRoute", "FormAutoFill.cs");
            Assert.That(File.Exists(formFile), Is.True, "FormAutoFill.cs not found");

            var violations = new List<string>();
            var lines = File.ReadAllLines(formFile);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("using ")) continue;

                if (Regex.IsMatch(lines[i], @"PlayerContext|EmpireContext\.PlayerContext"))
                {
                    violations.Add("Line " + (i + 1) + ": " + lines[i].Trim());
                }
            }

            Assert.That(violations, Is.Empty,
                "FormAutoFill accesses entity context (should only use PreferencesStore):\n" + string.Join("\n", violations));
        }

        private List<string> ScanFileForMutableEntityFields(string filePath)
        {
            var violations = new List<string>();
            var lines = File.ReadAllLines(filePath);

            foreach (var typeName in MutableEntityTypes)
            {
                var pattern = BuildFieldPattern(typeName);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (pattern.IsMatch(lines[i]))
                    {
                        if (lines[i].Contains("ReadOnly" + typeName)) continue;
                        if (lines[i].Contains("IReadOnlyList")) continue;
                        if (lines[i].Contains("List<")) continue;

                        violations.Add(typeName + " field at line " + (i + 1) + ": " + lines[i].Trim());
                    }
                }
            }

            return violations;
        }
    }
}
