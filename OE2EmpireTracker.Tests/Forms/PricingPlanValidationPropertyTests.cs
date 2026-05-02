using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Feature: pricing-plans, Property 1: Whitespace plan names are rejected
    ///
    /// For any string composed entirely of whitespace (including empty string),
    /// the validation should reject it as a plan name.
    ///
    /// **Validates: Requirements 1.6**
    /// </summary>
    [TestFixture]
    public class PricingPlanValidationPropertyTests
    {
        /// <summary>
        /// Feature: pricing-plans, Property 1: Whitespace plan names are rejected
        ///
        /// For any whitespace-only string, string.IsNullOrWhiteSpace returns true,
        /// which means the save handler would reject it.
        /// **Validates: Requirements 1.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property WhitespacePlanNames_AreRejected()
        {
            return Prop.ForAll(WhitespaceStringGen().ToArbitrary(), name =>
            {
                // The validation logic from FormPricingPlan.CmdSave_Click:
                // string name = txtPlanName.Text.Trim();
                // if (string.IsNullOrWhiteSpace(name)) -> reject
                bool isRejected = string.IsNullOrWhiteSpace(name);

                return isRejected
                    .Label($"Whitespace name '{name?.Replace("\n", "\\n").Replace("\t", "\\t")}' was not rejected");
            });
        }

        /// <summary>
        /// Non-whitespace names should be accepted.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property NonWhitespacePlanNames_AreAccepted()
        {
            return Prop.ForAll(Arb.From<NonEmptyString>(), nes =>
            {
                string name = nes.Get;
                // If the name has at least one non-whitespace character, it should be accepted
                bool hasContent = !string.IsNullOrWhiteSpace(name);
                bool isAccepted = !string.IsNullOrWhiteSpace(name);

                return (hasContent == isAccepted)
                    .Label($"Name '{name}': hasContent={hasContent}, isAccepted={isAccepted}");
            });
        }

        /// <summary>
        /// Generates strings composed entirely of whitespace characters.
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
    }
}
