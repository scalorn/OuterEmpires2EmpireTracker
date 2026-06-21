// -----------------------------------------------------------------------
// <copyright file="StorageExceptionTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for StorageLoadException, StorageWriteException, and
    /// StorageCorruptionException constructors and property assignment.
    /// Satisfies: Req 10, Criteria 1-4.
    /// </summary>
    [TestFixture]
    public class StorageExceptionTests
    {
        [Test]
        public void StorageLoadException_FullConstructor_SetsAllProperties()
        {
            var inner = new IOException("disk full");

            var ex = new StorageLoadException("Sqlite", "/path/to/db", "Load failed", inner);

            Assert.That(ex.BackendType, Is.EqualTo("Sqlite"));
            Assert.That(ex.Location, Is.EqualTo("/path/to/db"));
            Assert.That(ex.Message, Is.EqualTo("Load failed"));
            Assert.That(ex.InnerException, Is.SameAs(inner));
        }

        [Test]
        public void StorageLoadException_BackendType_ReturnsCorrectValue()
        {
            var ex = new StorageLoadException("JsonMultiFile", "C:\\data", "err", null);

            Assert.That(ex.BackendType, Is.EqualTo("JsonMultiFile"));
        }

        [Test]
        public void StorageLoadException_Location_ReturnsCorrectValue()
        {
            var ex = new StorageLoadException("Postgres", "host=localhost;db=oe2", "err", null);

            Assert.That(ex.Location, Is.EqualTo("host=localhost;db=oe2"));
        }

        [Test]
        public void StorageLoadException_InnerException_IsPreserved()
        {
            var inner = new InvalidOperationException("corrupted");

            var ex = new StorageLoadException("DynamoDB", "us-east-1/table", "err", inner);

            Assert.That(ex.InnerException, Is.SameAs(inner));
        }

        [Test]
        public void StorageWriteException_FullConstructor_SetsAllProperties()
        {
            var inner = new TimeoutException("connection timed out");

            var ex = new StorageWriteException("Postgres", "INSERT colony", "Write failed", inner);

            Assert.That(ex.BackendType, Is.EqualTo("Postgres"));
            Assert.That(ex.Operation, Is.EqualTo("INSERT colony"));
            Assert.That(ex.Message, Is.EqualTo("Write failed"));
            Assert.That(ex.InnerException, Is.SameAs(inner));
        }

        [Test]
        public void StorageWriteException_BackendType_ReturnsCorrectValue()
        {
            var ex = new StorageWriteException("Sqlite", "UPDATE", "err", null);

            Assert.That(ex.BackendType, Is.EqualTo("Sqlite"));
        }

        [Test]
        public void StorageWriteException_Operation_ReturnsCorrectValue()
        {
            var ex = new StorageWriteException("JsonSingleFile", "SaveAll", "err", null);

            Assert.That(ex.Operation, Is.EqualTo("SaveAll"));
        }

        [Test]
        public void StorageWriteException_InnerException_IsPreserved()
        {
            var inner = new UnauthorizedAccessException("read-only");

            var ex = new StorageWriteException("JsonMultiFile", "WriteEntity", "err", inner);

            Assert.That(ex.InnerException, Is.SameAs(inner));
        }

        [Test]
        public void StorageCorruptionException_FullConstructor_SetsAllProperties()
        {
            var migrationErr = new InvalidOperationException("migration failed");
            var rollbackErr = new IOException("rollback failed");

            var ex = new StorageCorruptionException("Sqlite", migrationErr, rollbackErr);

            Assert.That(ex.BackendType, Is.EqualTo("Sqlite"));
            Assert.That(ex.MigrationError, Is.SameAs(migrationErr));
            Assert.That(ex.RollbackError, Is.SameAs(rollbackErr));
        }

        [Test]
        public void StorageCorruptionException_Message_ContainsBothErrorDescriptions()
        {
            var migrationErr = new InvalidOperationException("schema v2 failed");
            var rollbackErr = new IOException("cannot revert");

            var ex = new StorageCorruptionException("Postgres", migrationErr, rollbackErr);

            Assert.That(ex.Message, Does.Contain("schema v2 failed"));
            Assert.That(ex.Message, Does.Contain("cannot revert"));
        }

        [Test]
        public void StorageCorruptionException_BackendType_ReturnsCorrectValue()
        {
            var ex = new StorageCorruptionException(
                "DynamoDB",
                new Exception("m"),
                new Exception("r"));

            Assert.That(ex.BackendType, Is.EqualTo("DynamoDB"));
        }

        [Test]
        public void StorageCorruptionException_MigrationError_ReturnsMigrationException()
        {
            var migrationErr = new InvalidOperationException("bad migration");

            var ex = new StorageCorruptionException("Sqlite", migrationErr, new Exception("r"));

            Assert.That(ex.MigrationError, Is.SameAs(migrationErr));
        }

        [Test]
        public void StorageCorruptionException_RollbackError_ReturnsRollbackException()
        {
            var rollbackErr = new IOException("disk failure");

            var ex = new StorageCorruptionException("Sqlite", new Exception("m"), rollbackErr);

            Assert.That(ex.RollbackError, Is.SameAs(rollbackErr));
        }

        [Test]
        public void StorageCorruptionException_InnerException_IsMigrationError()
        {
            var migrationErr = new InvalidOperationException("migration broke");
            var rollbackErr = new IOException("rollback broke");

            var ex = new StorageCorruptionException("Postgres", migrationErr, rollbackErr);

            Assert.That(ex.InnerException, Is.SameAs(migrationErr));
        }
    }
}
