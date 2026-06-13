// <copyright file="ManualRequestValidationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Unit tests for ManualRequestValidation.ValidateInputs covering all
    /// endpoint categories and validation rules.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
    /// </summary>
    [TestFixture]
    public class ManualRequestValidationTests
    {
        private ManualRequestEndpoint singleIdEndpoint;
        private ManualRequestEndpoint marketViewEndpoint;
        private ManualRequestEndpoint marketCompetitorsEndpoint;
        private ManualRequestEndpoint parameterlessEndpoint;

        [SetUp]
        public void SetUp()
        {
            this.singleIdEndpoint = ManualRequestEndpoint.FindByDisplayName("Colony Buildings");
            this.marketViewEndpoint = ManualRequestEndpoint.FindByDisplayName("Market Listings");
            this.marketCompetitorsEndpoint = ManualRequestEndpoint.FindByDisplayName("Market Buy Competitors");
            this.parameterlessEndpoint = ManualRequestEndpoint.FindByDisplayName("Character");
        }

        /// <summary>
        /// Empty ID is rejected for a single-ID endpoint with the correct error message.
        ///
        /// **Validates: Requirements 3.1**
        /// </summary>
        [Test]
        public void ValidateInputs_EmptyId_RejectedForSingleIdEndpoint()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.singleIdEndpoint,
                string.Empty,
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Error: ID must be a number"));
        }

        /// <summary>
        /// Non-numeric ID is rejected for a single-ID endpoint with the correct error message.
        ///
        /// **Validates: Requirements 3.2**
        /// </summary>
        [Test]
        public void ValidateInputs_NonNumericId_RejectedForSingleIdEndpoint()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.singleIdEndpoint,
                "abc",
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Error: ID must be a number"));
        }

        /// <summary>
        /// Valid numeric ID is accepted for a single-ID endpoint.
        ///
        /// **Validates: Requirements 3.5**
        /// </summary>
        [Test]
        public void ValidateInputs_ValidNumericId_AcceptedForSingleIdEndpoint()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.singleIdEndpoint,
                "42",
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        /// <summary>
        /// Empty View is rejected for a MarketView endpoint with the correct error message.
        ///
        /// **Validates: Requirements 3.3**
        /// </summary>
        [Test]
        public void ValidateInputs_EmptyView_RejectedForMarketViewEndpoint()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.marketViewEndpoint,
                string.Empty,
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Error: View is required"));
        }

        /// <summary>
        /// Empty Market IDs is rejected for a MarketCompetitors endpoint with the correct error message.
        ///
        /// **Validates: Requirements 3.4**
        /// </summary>
        [Test]
        public void ValidateInputs_EmptyMarketIds_RejectedForMarketCompetitorsEndpoint()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.marketCompetitorsEndpoint,
                string.Empty,
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Error: Market IDs are required"));
        }

        /// <summary>
        /// Parameterless endpoint always passes validation regardless of input values.
        ///
        /// **Validates: Requirements 3.5**
        /// </summary>
        [Test]
        public void ValidateInputs_ParameterlessEndpoint_AlwaysValid()
        {
            var result = ManualRequestValidation.ValidateInputs(
                this.parameterlessEndpoint,
                string.Empty,
                string.Empty,
                string.Empty);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
        }
    }
}
