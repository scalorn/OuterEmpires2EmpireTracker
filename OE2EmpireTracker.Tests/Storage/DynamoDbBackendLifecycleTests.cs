// -----------------------------------------------------------------------
// <copyright file="DynamoDbBackendLifecycleTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for DynamoDbBackend lifecycle operations: InitializeAsync,
    /// ValidateConnectionAsync, GetStorageInfo, and table creation.
    /// Satisfies: Req 5, Criteria 1-2, 4.
    /// </summary>
    [TestFixture]
    public class DynamoDbBackendLifecycleTests
    {
        private const string TestTableName = "oe2lifecycle_test";

        [SetUp]
        public void SetUp()
        {
            if (!DynamoDbLocalFixture.IsAvailable)
            {
                Assert.Ignore("DynamoDB Local is not available");
            }
        }

        [Test]
        public async Task InitializeAsync_CreatesTablesSuccessfully()
        {
            var backend = new DynamoDbBackend(
                TestTableName + "_init",
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);

            Assert.DoesNotThrowAsync(async () => await backend.InitializeAsync());
        }

        [Test]
        public async Task ValidateConnectionAsync_ReturnsTrue()
        {
            var backend = new DynamoDbBackend(
                TestTableName + "_validate",
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);

            await backend.InitializeAsync();
            var result = await backend.ValidateConnectionAsync();

            Assert.That(result, Is.True);
        }

        [Test]
        public void GetStorageInfo_ReturnsCorrectType()
        {
            var backend = new DynamoDbBackend(
                TestTableName + "_info",
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);

            var info = backend.GetStorageInfo();

            Assert.That(info.BackendType, Is.EqualTo("DynamoDB"));
            Assert.That(info.Location, Is.EqualTo(TestTableName + "_info"));
        }

        [Test]
        public async Task InitializeAsync_AlreadyInitialized_NoError()
        {
            var backend = new DynamoDbBackend(
                TestTableName + "_double",
                "us-east-1",
                DynamoDbLocalFixture.ServiceUrl);

            await backend.InitializeAsync();

            Assert.DoesNotThrowAsync(async () => await backend.InitializeAsync());
        }
    }
}
