using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ContactsMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        private static readonly HashSet<string> AllowedFactionMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ContactsService.cs",
            "Faction.cs",
            "PlayerContext.cs",
        };

        private static readonly HashSet<string> AllowedCharacterMutators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ContactsService.cs",
            "ExternalCharacter.cs",
            "PlayerContext.cs",
        };

        [Test]
        public void Faction_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[] { @"faction\.Name\s*=[^=]", @"faction\.Description\s*=[^=]", @"faction\.UUID\s*=[^=]" };
            var compiled = patterns.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            var violations = ScanSourceFilesForPatterns(compiled, AllowedFactionMutators);
            Assert.That(violations, Is.Empty, "Found direct Faction mutation in non-allowed files:\n" + string.Join("\n", violations));
        }

        [Test]
        public void ExternalCharacter_ScalarPropertySets_OnlyInAllowedFiles()
        {
            var patterns = new[] { @"character\.Name\s*=[^=]", @"character\.FactionUUID\s*=[^=]", @"character\.UUID\s*=[^=]" };
            var compiled = patterns.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            var violations = ScanSourceFilesForPatterns(compiled, AllowedCharacterMutators);
            Assert.That(violations, Is.Empty, "Found direct ExternalCharacter mutation in non-allowed files:\n" + string.Join("\n", violations));
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
