using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class AsteroidMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AsteroidService.cs",
            "Asteroid.cs",
            "PlayerContext.cs",
            "SerializationSorter.cs",
            "SurveyImportHelper.cs",
        };

        [Test]
        public void Asteroid_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[]
            {
                @"asteroid\.Name\s*=[^=]",
                @"asteroid\.SystemName\s*=[^=]",
                @"asteroid\.UUID\s*=[^=]",
                @"asteroid\.Reserves\s*=[^=]",
            };
            var compiled = patterns.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            var violations = ScanSourceFilesForPatterns(compiled, AllowedMutators);
            Assert.That(violations, Is.Empty, "Found direct Asteroid mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        private List<string> ScanSourceFilesForPatterns(Regex[] patterns, HashSet<string> allowedFiles)
        {
            var violations = new List<string>();
            foreach (var file in Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("Designer.cs") && !allowedFiles.Contains(Path.GetFileName(f))))
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var pattern in patterns)
                    {
                        if (pattern.IsMatch(lines[i]))
                        {
                            violations.Add(Path.GetFileName(file) + "(" + (i + 1) + "): " + lines[i].Trim());
                        }
                    }
                }
            }

            return violations;
        }
    }
}
