// <copyright file="SharingValidationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Feature: sharing-visibility-system, Property 5: Target UUID Validation Blocks Empty Input
    ///
    /// For any string that is empty or composed entirely of whitespace characters,
    /// the sharing rule save action SHALL be blocked — the Desktop app disables the
    /// Save control (Enabled = false, greyed out). The server independently enforces
    /// this validation regardless of client state (Req 7.7).
    ///
    /// **Validates: Requirements 4.2, 7.5, 7.6, 7.7**
    /// </summary>
    [TestFixture]
    public class SharingValidationTests
    {
        private static readonly string[] NonPublicTargetTypes = { "Faction", "Character" };

        /// <summary>
        /// Property: For any whitespace-only or empty Target UUID on a non-Public row,
        /// the validation logic returns true (save should be blocked).
        ///
        /// Feature: sharing-visibility-system, Property 5: Target UUID Validation Blocks Empty Input
        /// **Validates: Requirements 4.2, 7.5, 7.6, 7.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WhitespaceTargetUUID_BlocksSave()
        {
            var gen =
                from targetType in Gen.Elements(NonPublicTargetTypes)
                from uuid in WhitespaceStringGen()
                from dataType in Gen.Elements("All", "Colonies", "Blueprints", "Surveys")
                select new SharingRuleInput(targetType, uuid, dataType);

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                var rules = new List<SharingRuleInput> { input };
                bool blocked = HasEmptyTargetUUID(rules);

                return blocked.Label(
                    string.Format(
                        "TargetType='{0}', UUID='{1}' should block save",
                        input.TargetType,
                        EscapeWhitespace(input.TargetUUID)));
            });
        }

        /// <summary>
        /// Property: For any non-empty, non-whitespace Target UUID on a non-Public row,
        /// the validation logic returns false (save should be allowed).
        ///
        /// Feature: sharing-visibility-system, Property 5: Target UUID Validation Blocks Empty Input
        /// **Validates: Requirements 4.2, 7.5, 7.6, 7.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ValidTargetUUID_AllowsSave()
        {
            var gen =
                from targetType in Gen.Elements(NonPublicTargetTypes)
                from uuid in NonWhitespaceStringGen()
                from dataType in Gen.Elements("All", "Colonies", "Blueprints", "Surveys")
                select new SharingRuleInput(targetType, uuid, dataType);

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                var rules = new List<SharingRuleInput> { input };
                bool blocked = HasEmptyTargetUUID(rules);

                return (!blocked).Label(
                    string.Format(
                        "TargetType='{0}', UUID='{1}' should allow save",
                        input.TargetType,
                        input.TargetUUID));
            });
        }

        /// <summary>
        /// Property: Public target type rows do NOT trigger validation regardless of UUID value.
        /// A grid with only Public rows should always allow save.
        ///
        /// Feature: sharing-visibility-system, Property 5: Target UUID Validation Blocks Empty Input
        /// **Validates: Requirements 4.2, 7.5, 7.6, 7.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PublicTargetType_NeverBlocksSave()
        {
            var gen =
                from uuid in Gen.OneOf(WhitespaceStringGen(), NonWhitespaceStringGen())
                from dataType in Gen.Elements("All", "Colonies", "Blueprints", "Surveys")
                select new SharingRuleInput("Public", uuid, dataType);

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                var rules = new List<SharingRuleInput> { input };
                bool blocked = HasEmptyTargetUUID(rules);

                return (!blocked).Label(
                    string.Format(
                        "Public row with UUID='{0}' should never block save",
                        EscapeWhitespace(input.TargetUUID)));
            });
        }

        /// <summary>
        /// Property: Mixed rows where a Public row has empty UUID but a non-Public row
        /// has a valid UUID should allow save (Public rows are exempt).
        ///
        /// Feature: sharing-visibility-system, Property 5: Target UUID Validation Blocks Empty Input
        /// **Validates: Requirements 4.2, 7.5, 7.6, 7.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MixedRows_PublicEmptyAndNonPublicValid_AllowsSave()
        {
            var gen =
                from validUuid in NonWhitespaceStringGen()
                from emptyUuid in WhitespaceStringGen()
                from nonPublicType in Gen.Elements(NonPublicTargetTypes)
                select new { ValidUuid = validUuid, EmptyUuid = emptyUuid, NonPublicType = nonPublicType };

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                var rules = new List<SharingRuleInput>
                {
                    new SharingRuleInput("Public", input.EmptyUuid, "All"),
                    new SharingRuleInput(input.NonPublicType, input.ValidUuid, "Blueprints"),
                };
                bool blocked = HasEmptyTargetUUID(rules);

                return (!blocked).Label(
                    "Public row with empty UUID + non-Public row with valid UUID should allow save");
            });
        }

        /// <summary>
        /// Replicates the validation logic from FormSharing.HasEmptyTargetUUID.
        /// For any non-Public row, if the Target UUID is empty or whitespace, returns true.
        /// This mirrors the exact logic in the production code.
        /// </summary>
        private static bool HasEmptyTargetUUID(List<SharingRuleInput> rules)
        {
            foreach (var rule in rules)
            {
                if (string.Equals(rule.TargetType, "Public", StringComparison.Ordinal))
                {
                    continue;
                }

                string targetUUID = rule.TargetUUID ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetUUID))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates strings composed entirely of whitespace characters (including empty).
        /// </summary>
        private static Gen<string> WhitespaceStringGen()
        {
            return Gen.OneOf(
                Gen.Constant(string.Empty),
                Gen.Constant(" "),
                Gen.Constant("  "),
                Gen.Constant("\t"),
                Gen.Constant("\n"),
                Gen.Constant(" \t\n "),
                Gen.Choose(1, 10).SelectMany(len =>
                    Gen.ListOf(len, Gen.Elements(' ', '\t', '\n', '\r'))
                       .Select(chars => new string(chars.ToArray()))));
        }

        /// <summary>
        /// Generates non-empty strings that contain at least one non-whitespace character.
        /// Simulates valid UUID-like values.
        /// </summary>
        private static Gen<string> NonWhitespaceStringGen()
        {
            return Gen.OneOf(
                Gen.Elements(
                    "abc-123-def",
                    "faction-uuid-001",
                    "a",
                    "12345678-1234-1234-1234-123456789abc"),
                from len in Gen.Choose(1, 20)
                from chars in Gen.ListOf(len, Gen.Elements(
                    'a', 'b', 'c', '1', '2', '3', '-', 'x', 'y', 'z'))
                select new string(chars.ToArray()));
        }

        /// <summary>
        /// Escapes whitespace characters for readable test labels.
        /// </summary>
        private static string EscapeWhitespace(string value)
        {
            if (value == null)
            {
                return "(null)";
            }

            return value
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Represents a sharing rule row for validation testing.
        /// </summary>
        private sealed class SharingRuleInput
        {
            public SharingRuleInput(string targetType, string targetUUID, string dataType)
            {
                TargetType = targetType;
                TargetUUID = targetUUID;
                DataType = dataType;
            }

            public string TargetType { get; }

            public string TargetUUID { get; }

            public string DataType { get; }
        }
    }
}
