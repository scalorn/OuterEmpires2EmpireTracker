// <copyright file="ManualRequestEndpointTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Unit tests for ManualRequestEndpoint metadata.
    /// **Validates: Requirements 1.1, 1.2, 7.3**
    /// </summary>
    [TestFixture]
    public class ManualRequestEndpointTests
    {
        /// <summary>
        /// The All list contains exactly 29 endpoints as specified in requirements.
        /// </summary>
        [Test]
        public void All_ContainsExactly29Endpoints()
        {
            Assert.That(ManualRequestEndpoint.All, Has.Count.EqualTo(29));
        }

        /// <summary>
        /// FindByDisplayName returns the correct endpoint for a known name.
        /// </summary>
        [Test]
        public void FindByDisplayName_KnownName_ReturnsCorrectEndpoint()
        {
            var endpoint = ManualRequestEndpoint.FindByDisplayName("Colony Buildings");

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.DisplayName, Is.EqualTo("Colony Buildings"));
        }

        /// <summary>
        /// FindByDisplayName returns null for an unknown name.
        /// </summary>
        [Test]
        public void FindByDisplayName_UnknownName_ReturnsNull()
        {
            var endpoint = ManualRequestEndpoint.FindByDisplayName("NonExistentEndpoint");

            Assert.That(endpoint, Is.Null);
        }

        /// <summary>
        /// The default endpoint "Location Detail" exists and can be found by name.
        /// **Validates: Requirements 1.2, 7.3**
        /// </summary>
        [Test]
        public void FindByDisplayName_LocationDetail_Exists()
        {
            var endpoint = ManualRequestEndpoint.FindByDisplayName("Location Detail");

            Assert.That(endpoint, Is.Not.Null);
        }

        /// <summary>
        /// Location Detail has the LocationDetail category.
        /// </summary>
        [Test]
        public void LocationDetail_HasLocationDetailCategory()
        {
            var endpoint = ManualRequestEndpoint.FindByDisplayName("Location Detail");

            Assert.That(endpoint.Category, Is.EqualTo(EndpointCategory.LocationDetail));
        }

        /// <summary>
        /// Colony Buildings has SingleId category with "Colony ID" label.
        /// </summary>
        [Test]
        public void ColonyBuildings_HasSingleIdCategory_WithColonyIdLabel()
        {
            var endpoint = ManualRequestEndpoint.FindByDisplayName("Colony Buildings");

            Assert.That(endpoint.Category, Is.EqualTo(EndpointCategory.SingleId));
            Assert.That(endpoint.IdLabel, Is.EqualTo("Colony ID"));
        }
    }
}
