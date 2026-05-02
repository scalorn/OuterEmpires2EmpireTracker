using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Verification tests that ensure PlayerProfile, PlayerSkill, and PlayerRank
    /// entities are only mutated by allowed code.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Requirements 22.1, 22.2, 22.3
    /// </summary>
    [TestFixture]
    public class PlayerProfileMutationGuardTests
    {
        private static readonly string SourceRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "OE2EmpireTracker"));

        /// <summary>
        /// Requirement 22.1: No direct PlayerProfile entity mutation in form or ViewModel.
        /// Scans FormPlayerProfile and PlayerProfileViewModel for patterns like
        /// profile.Name = or _profile.Faction = that indicate direct entity mutation.
        /// </summary>
        [Test]
        public void PlayerProfile_NoDirectEntityMutation_InFormOrViewModel()
        {
            // Patterns that indicate direct mutation of a PlayerProfile entity
            // (not ViewModel local fields, not service code)
            var entityMutationPatterns = new[]
            {
                @"_profile\.\w+\s*=",
                @"profile\.\w+\s*=[^=]",
                @"\.Data\.\w+\s*=[^=]",
            };

            // Files to scan: the form and ViewModel that were migrated
            var filesToScan = new[]
            {
                Path.Combine("Forms", "PlayerProfile", "FormPlayerProfile.cs"),
                Path.Combine("ViewModels", "PlayerProfileViewModel.cs"),
            };

            var violations = ScanFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(
                violations,
                Is.Empty,
                "Found direct PlayerProfile entity mutation in form/ViewModel:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 22.2: No direct PlayerSkill entity mutation in PlayerSkillBlock.
        /// </summary>
        [Test]
        public void PlayerSkill_NoDirectEntityMutation_InSkillBlock()
        {
            var entityMutationPatterns = new[]
            {
                @"_playerSkill\.\w+\s*=",
                @"playerSkill\.\w+\s*=[^=]",
                @"PlayerSkill\s+\w+\s*=\s*",
            };

            var filesToScan = new[]
            {
                Path.Combine("Forms", "PlayerProfile", "PlayerSkillBlock.cs"),
            };

            var violations = ScanFilesForPatterns(filesToScan, entityMutationPatterns);

            Assert.That(violations, Is.Empty,
                "Found direct PlayerSkill entity mutation in PlayerSkillBlock:\n" +
                string.Join("\n", violations));
        }

        /// <summary>
        /// Requirement 22.3: ViewModel does not expose mutable Data property.
        /// </summary>
        [Test]
        public void ViewModel_DoesNotExposeDataProperty()
        {
            var filePath = Path.Combine(SourceRoot, "ViewModels", "PlayerProfileViewModel.cs");
            var content = File.ReadAllText(filePath);

            Assert.That(content, Does.Not.Contain("public PlayerProfile Data"),
                "ViewModel still exposes mutable Data property");
            Assert.That(content, Does.Not.Contain("public PlayerProfile _profile"),
                "ViewModel still exposes mutable _profile field");
        }

        private List<string> ScanFilesForPatterns(string[] relativeFiles, string[] regexPatterns)
        {
            var violations = new List<string>();
            var compiled = regexPatterns.Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

            foreach (var relFile in relativeFiles)
            {
                var fullPath = Path.Combine(SourceRoot, relFile);
                if (!File.Exists(fullPath)) continue;

                var lines = File.ReadAllLines(fullPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*")) continue;

                    foreach (var rx in compiled)
                    {
                        if (rx.IsMatch(line))
                        {
                            violations.Add(string.Format("{0}({1}): {2}", relFile, i + 1, line));
                        }
                    }
                }
            }

            return violations;
        }
    }
}
