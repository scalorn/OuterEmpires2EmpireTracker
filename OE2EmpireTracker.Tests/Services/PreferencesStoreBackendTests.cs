// <copyright file="PreferencesStoreBackendTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for PreferencesStore storage config resolution.
    /// Satisfies: Req 5, Criteria 1-8.
    /// </summary>
    [TestFixture]
    public class PreferencesStoreBackendTests
    {
        private PreferencesStore store;

        [SetUp]
        public void SetUp()
        {
            PreferencesStore.Reset();
            store = PreferencesStore.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesStore.Reset();
        }

        [TestCase("JsonSingleFile", StorageBackendType.JsonSingleFile)]
        [TestCase("JsonMultiFile", StorageBackendType.JsonMultiFile)]
        [TestCase("Sqlite", StorageBackendType.Sqlite)]
        [TestCase("DynamoDb", StorageBackendType.DynamoDb)]
        [TestCase("Postgres", StorageBackendType.Postgres)]
        public void ParseStorageBackendType_ValidValues_ParsesCorrectly(
            string input,
            StorageBackendType expected)
        {
            var result = store.ParseStorageBackendType(input);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("jsonsinglefile")]
        [TestCase("JSONSINGLEFILE")]
        [TestCase("jsonSingleFile")]
        public void ParseStorageBackendType_CaseInsensitive_ParsesCorrectly(string input)
        {
            var result = store.ParseStorageBackendType(input);

            Assert.That(result, Is.EqualTo(StorageBackendType.JsonSingleFile));
        }

        [TestCase("InvalidBackend")]
        [TestCase("FlatFile")]
        [TestCase("MongoDB")]
        public void ParseStorageBackendType_UnrecognizedType_FallsBackToJsonSingleFile(string input)
        {
            var result = store.ParseStorageBackendType(input);

            Assert.That(result, Is.EqualTo(StorageBackendType.JsonSingleFile));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ParseStorageBackendType_NullOrEmpty_FallsBackToJsonSingleFile(string input)
        {
            var result = store.ParseStorageBackendType(input);

            Assert.That(result, Is.EqualTo(StorageBackendType.JsonSingleFile));
        }

        [Test]
        public void ResolveStorageConfig_JsonSingleFile_DefaultPath_UsesBaseDirectory()
        {
            store.Preferences.StorageBackendType = "JsonSingleFile";
            store.Preferences.StoragePath = null;

            var config = store.ResolveStorageConfig();

            Assert.That(config.ConnectionString, Is.EqualTo(AppDomain.CurrentDomain.BaseDirectory));
        }

        [Test]
        public void ResolveStorageConfig_JsonMultiFile_DefaultPath_UsesBaseDirectory()
        {
            store.Preferences.StorageBackendType = "JsonMultiFile";
            store.Preferences.StoragePath = null;

            var config = store.ResolveStorageConfig();

            Assert.That(config.ConnectionString, Is.EqualTo(AppDomain.CurrentDomain.BaseDirectory));
        }

        [Test]
        public void ResolveStorageConfig_Sqlite_DefaultPath_UsesLocalAppData()
        {
            store.Preferences.StorageBackendType = "Sqlite";
            store.Preferences.StoragePath = null;

            var config = store.ResolveStorageConfig();

            string expected = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OE2EmpireTracker");
            Assert.That(config.ConnectionString, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveStorageConfig_JsonSingleFile_CustomPath_UsesCustomPath()
        {
            store.Preferences.StorageBackendType = "JsonSingleFile";
            store.Preferences.StoragePath = @"C:\Custom\Path";

            var config = store.ResolveStorageConfig();

            Assert.That(config.ConnectionString, Is.EqualTo(@"C:\Custom\Path"));
        }

        [Test]
        public void ResolveStorageConfig_Sqlite_CustomPath_UsesCustomPath()
        {
            store.Preferences.StorageBackendType = "Sqlite";
            store.Preferences.StoragePath = @"D:\Data\MyDb";

            var config = store.ResolveStorageConfig();

            Assert.That(config.ConnectionString, Is.EqualTo(@"D:\Data\MyDb"));
        }

        [Test]
        public void ResolveStorageConfig_DynamoDb_PopulatesRegionAndPrefix()
        {
            store.Preferences.StorageBackendType = "DynamoDb";
            store.Preferences.StorageAwsRegion = "us-west-2";
            store.Preferences.StorageTablePrefix = "oe2-prod-";

            var config = store.ResolveStorageConfig();

            Assert.That(config.AwsRegion, Is.EqualTo("us-west-2"));
            Assert.That(config.TablePrefix, Is.EqualTo("oe2-prod-"));
        }

        [Test]
        public void ResolveStorageConfig_Postgres_PopulatesConnectionString()
        {
            store.Preferences.StorageBackendType = "Postgres";
            store.Preferences.StorageConnectionString = "Host=localhost;Database=oe2;Username=admin";

            var config = store.ResolveStorageConfig();

            Assert.That(
                config.ConnectionString,
                Is.EqualTo("Host=localhost;Database=oe2;Username=admin"));
        }

        [Test]
        public void ResolveStorageConfig_EmptyStoragePath_UsesDefault()
        {
            store.Preferences.StorageBackendType = "JsonSingleFile";
            store.Preferences.StoragePath = string.Empty;

            var config = store.ResolveStorageConfig();

            Assert.That(config.ConnectionString, Is.EqualTo(AppDomain.CurrentDomain.BaseDirectory));
        }
    }
}
