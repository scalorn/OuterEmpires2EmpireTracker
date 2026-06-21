// -----------------------------------------------------------------------
// <copyright file="DynamoDbBackendErrorTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for DynamoDbBackend error handling when the DynamoDB endpoint
    /// is unreachable. Verifies StorageLoadException and StorageWriteException
    /// are thrown with correct details.
    /// Satisfies: Req 5, Criterion 5.
    /// </summary>
    [TestFixture]
    public class DynamoDbBackendErrorTests
    {
        private const string UnreachableEndpoint = "http://localhost:59999";
        private const string TestTable = "error_test_table";

        [Test]
        public void InitializeAsync_UnreachableEndpoint_ThrowsStorageLoadException()
        {
            var backend = new DynamoDbBackend(TestTable, "us-east-1", UnreachableEndpoint);

            var ex = Assert.ThrowsAsync<StorageLoadException>(
                () => backend.InitializeAsync());

            Assert.That(ex.BackendType, Is.EqualTo("DynamoDB"));
            Assert.That(ex.InnerException, Is.Not.Null);
        }

        [Test]
        public void UpsertFactionAsync_UnreachableEndpoint_ThrowsStorageWriteException()
        {
            var backend = new DynamoDbBackend(TestTable, "us-east-1", UnreachableEndpoint);

            var faction = new ServerFaction
            {
                UUID = "test-faction-uuid",
                Name = "Test Faction",
            };

            var ex = Assert.ThrowsAsync<StorageWriteException>(
                () => backend.UpsertFactionAsync(faction));

            Assert.That(ex.BackendType, Is.EqualTo("DynamoDB"));
            Assert.That(ex.InnerException, Is.Not.Null);
        }

        [Test]
        public async Task ValidateConnectionAsync_UnreachableEndpoint_ReturnsFalse()
        {
            var backend = new DynamoDbBackend(TestTable, "us-east-1", UnreachableEndpoint);

            var result = await backend.ValidateConnectionAsync();

            Assert.That(result, Is.False);
        }
    }
}
