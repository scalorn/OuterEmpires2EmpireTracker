// <copyright file="GameApiCredentialManagerTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based and unit tests for GameApiCredentialManager.
    /// Feature: game-api-integration
    /// **Validates: Correctness Property 1 (Credential Confidentiality), Correctness Property 7 (Credential Isolation)**
    /// </summary>
    [TestFixture]
    public class GameApiCredentialManagerTests
    {
        private string _tempDir;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Register DPAPI protection functions for the credential manager
            GameApiCredentialManager.RegisterProtectionFunctions(
                OE2EmpireTracker.Client.CredentialStore.Protect,
                OE2EmpireTracker.Client.CredentialStore.Unprotect);
        }

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OE2Tests_CredMgr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Property: Store/retrieve round-trip — for any non-empty UUID and non-empty key,
        /// storing then retrieving returns the same value.
        /// **Validates: Requirements 1.1, 1.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property StoreKey_ThenGetKey_ReturnsOriginalValue()
        {
            var uuidGen = from g in Arb.Generate<Guid>()
                          select g.ToString();

            var keyGen = from chars in Gen.ArrayOf(
                             Gen.Choose(33, 126).Select(i => (char)i))
                         where chars.Length > 0
                         select new string(chars);

            return Prop.ForAll(
                Arb.From(uuidGen),
                Arb.From(keyGen),
                (uuid, apiKey) =>
                {
                    string secretsPath = Path.Combine(_tempDir, Guid.NewGuid().ToString("N") + ".dat");
                    var manager = new GameApiCredentialManager(secretsPath);

                    manager.StoreKey(uuid, apiKey);
                    SecureString retrieved = manager.GetKey(uuid);

                    string decrypted = SecureStringToString(retrieved);
                    return (decrypted == apiKey)
                        .Label($"Expected '{apiKey}' but got '{decrypted}'");
                });
        }

        /// <summary>
        /// Property: Remove isolation — removing key A does not affect key B.
        /// For any two distinct UUIDs with stored keys, removing one preserves the other.
        /// **Validates: Requirements 1.2, 1.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property RemoveKey_DoesNotAffectOtherKeys()
        {
            var pairGen = from guidA in Arb.Generate<Guid>()
                          from guidB in Arb.Generate<Guid>()
                          where guidA != guidB
                          select Tuple.Create(guidA.ToString(), guidB.ToString());

            var keyPairGen = from charsA in Gen.ArrayOf(
                                 Gen.Choose(33, 126).Select(i => (char)i))
                             from charsB in Gen.ArrayOf(
                                 Gen.Choose(33, 126).Select(i => (char)i))
                             where charsA.Length > 0 && charsB.Length > 0
                             select Tuple.Create(new string(charsA), new string(charsB));

            return Prop.ForAll(
                Arb.From(pairGen),
                Arb.From(keyPairGen),
                (uuids, keys) =>
                {
                    string uuidA = uuids.Item1;
                    string uuidB = uuids.Item2;
                    string keyA = keys.Item1;
                    string keyB = keys.Item2;

                    string secretsPath = Path.Combine(_tempDir, Guid.NewGuid().ToString("N") + ".dat");
                    var manager = new GameApiCredentialManager(secretsPath);

                    manager.StoreKey(uuidA, keyA);
                    manager.StoreKey(uuidB, keyB);

                    // Remove key A
                    manager.RemoveKey(uuidA);

                    // Key B should still be retrievable and unchanged
                    bool hasB = manager.HasKey(uuidB);
                    SecureString retrievedB = manager.GetKey(uuidB);
                    string decryptedB = SecureStringToString(retrievedB);

                    bool aGone = !manager.HasKey(uuidA);

                    return (hasB && decryptedB == keyB && aGone)
                        .Label($"hasB={hasB}, keyB matches={decryptedB == keyB}, aGone={aGone}");
                });
        }

        /// <summary>
        /// Test: Corruption recovery — if the secrets file contains invalid JSON,
        /// the manager initializes with empty state and continues working.
        /// **Validates: Requirements 1.4**
        /// </summary>
        [Test]
        public void CorruptedFile_InitializesEmpty_AndContinuesWorking()
        {
            string secretsPath = Path.Combine(_tempDir, "corrupt-secrets.dat");
            File.WriteAllText(secretsPath, "{{{{not valid json at all!!!!}}}}");

            var manager = new GameApiCredentialManager(secretsPath);

            // Should have no keys
            Assert.That(manager.GetConfiguredPlayerUUIDs(), Is.Empty);

            // Should still be able to store and retrieve
            string uuid = Guid.NewGuid().ToString();
            string key = "test-api-key-after-corruption";
            manager.StoreKey(uuid, key);

            Assert.That(manager.HasKey(uuid), Is.True);
            string retrieved = SecureStringToString(manager.GetKey(uuid));
            Assert.That(retrieved, Is.EqualTo(key));
        }

        /// <summary>
        /// Test: Corruption recovery with truncated JSON.
        /// </summary>
        [Test]
        public void TruncatedJsonFile_InitializesEmpty()
        {
            string secretsPath = Path.Combine(_tempDir, "truncated-secrets.dat");
            File.WriteAllText(secretsPath, "{\"abc\": \"def");

            var manager = new GameApiCredentialManager(secretsPath);

            Assert.That(manager.GetConfiguredPlayerUUIDs(), Is.Empty);
        }

        /// <summary>
        /// Test: File location validation — the default secrets file path is under
        /// %LOCALAPPDATA%\OE2EmpireTracker\.
        /// </summary>
        [Test]
        public void DefaultFilePath_IsUnderLocalAppData()
        {
            // Use the default constructor to verify the path
            var manager = new GameApiCredentialManager();
            string expectedBase = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OE2EmpireTracker");

            Assert.That(manager.SecretsFilePath, Does.StartWith(expectedBase));
            Assert.That(manager.SecretsFilePath, Does.EndWith("game-api-secrets.dat"));
        }

        /// <summary>
        /// Test: Store/retrieve persists across instances (reload from file).
        /// </summary>
        [Test]
        public void StoreKey_PersistsAcrossInstances()
        {
            string secretsPath = Path.Combine(_tempDir, "persist-test.dat");
            string uuid = Guid.NewGuid().ToString();
            string apiKey = "persistent-key-value-12345";

            // Store in first instance
            var manager1 = new GameApiCredentialManager(secretsPath);
            manager1.StoreKey(uuid, apiKey);

            // Create new instance from same file
            var manager2 = new GameApiCredentialManager(secretsPath);
            Assert.That(manager2.HasKey(uuid), Is.True);

            string retrieved = SecureStringToString(manager2.GetKey(uuid));
            Assert.That(retrieved, Is.EqualTo(apiKey));
        }

        /// <summary>
        /// Test: GetKey returns null for unknown UUID.
        /// </summary>
        [Test]
        public void GetKey_UnknownUUID_ReturnsNull()
        {
            string secretsPath = Path.Combine(_tempDir, "unknown-test.dat");
            var manager = new GameApiCredentialManager(secretsPath);

            SecureString result = manager.GetKey(Guid.NewGuid().ToString());
            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// Converts a SecureString to a plain string for test assertions.
        /// </summary>
        private static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }
    }
}
