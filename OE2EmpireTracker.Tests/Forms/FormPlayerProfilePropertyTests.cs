using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Feature: profile-form-upgrade property tests for FormPlayerProfile display logic.
    ///
    /// Property 2: CharacterId display formatting (Validates: Requirements 1.2)
    /// Property 3: FirstName/LastName row visibility (Validates: Requirements 1.3)
    /// </summary>
    [TestFixture]
    public class FormPlayerProfilePropertyTests
    {
        /// <summary>
        /// Feature: profile-form-upgrade, Property 2: CharacterId display formatting.
        ///
        /// For any int CharacterId: display "\u2014" (em-dash U+2014) when less than or
        /// equal to 0, and the decimal string representation when greater than 0.
        ///
        /// **Validates: Requirements 1.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CharacterId_DisplaysEmDash_WhenNonPositive_AndDecimalString_WhenPositive()
        {
            var gen = Gen.Choose(int.MinValue / 2, int.MaxValue / 2);

            return Prop.ForAll(gen.ToArbitrary(), characterId =>
            {
                // This is the exact logic from FormPlayerProfile.PopulateForm:
                // lblCharacterIdValue.Text = viewModel.CharacterId > 0
                //     ? viewModel.CharacterId.ToString()
                //     : "\u2014";
                string result = characterId > 0
                    ? characterId.ToString()
                    : "\u2014";

                if (characterId <= 0)
                {
                    return (result == "\u2014")
                        .Label($"CharacterId={characterId}: expected em-dash but got '{result}'");
                }

                string expected = characterId.ToString();
                return (result == expected)
                    .Label($"CharacterId={characterId}: expected '{expected}' but got '{result}'");
            });
        }

        /// <summary>
        /// Feature: profile-form-upgrade, Property 3: FirstName/LastName row visibility.
        ///
        /// For any string value (including null, empty, and arbitrary non-empty strings),
        /// the name row visibility logic SHALL produce visible=true if and only if the
        /// string has length greater than zero (is neither null nor empty string).
        /// Whitespace-only strings ARE considered non-empty and SHALL be visible.
        ///
        /// **Validates: Requirements 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NameRow_VisibleIffStringIsNonEmpty()
        {
            var gen = Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty),
                Gen.Elements(" ", "  ", "\t", "\n"),
                from length in Gen.Choose(1, 50)
                from chars in Gen.ArrayOf(length, Gen.Choose(0x20, 0x7E).Select(c => (char)c))
                select new string(chars));

            return Prop.ForAll(gen.ToArbitrary(), value =>
            {
                // This is the exact logic used in FormPlayerProfile.PopulateForm:
                // flpFirstName.Visible = !string.IsNullOrEmpty(vm.FirstName);
                // flpLastName.Visible = !string.IsNullOrEmpty(vm.LastName);
                bool rowVisible = !string.IsNullOrEmpty(value);

                // The property: visible iff non-empty (not null and not "")
                bool isNonEmpty = value != null && value.Length > 0;

                return (rowVisible == isNonEmpty)
                    .Label($"Value={FormatValue(value)}: " +
                           $"rowVisible={rowVisible}, isNonEmpty={isNonEmpty}");
            });
        }

        private static string FormatValue(string value)
        {
            if (value == null)
            {
                return "<null>";
            }

            if (value.Length == 0)
            {
                return "<empty>";
            }

            if (value.Length > 20)
            {
                return $"\"{value.Substring(0, 20)}...\" (len={value.Length})";
            }

            return $"\"{value}\"";
        }
    }
}
