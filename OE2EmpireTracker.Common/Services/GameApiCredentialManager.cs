// <copyright file="GameApiCredentialManager.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Manages per-character game API key storage using DPAPI encryption.
    /// Keys are stored in a dedicated secrets file separate from Remote Faction Service credentials.
    /// Protection functions are injected to avoid assembly version conflicts between
    /// .NET Standard 2.0 (Common) and .NET Framework 4.8.1 (main app).
    /// </summary>
    public class GameApiCredentialManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static Func<string, string> _protectFunc;
        private static Func<string, SecureString> _unprotectFunc;

        private readonly string _secretsFilePath;

        private Dictionary<string, string> _protectedKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiCredentialManager"/> class.
        /// Loads existing credentials from the secrets file on construction.
        /// </summary>
        public GameApiCredentialManager()
        {
            _secretsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OE2EmpireTracker",
                "game-api-secrets.dat");
            Load();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiCredentialManager"/> class
        /// with a custom secrets file path. Used for testing.
        /// </summary>
        /// <param name="secretsFilePath">The full path to the secrets file.</param>
        internal GameApiCredentialManager(string secretsFilePath)
        {
            _secretsFilePath = secretsFilePath;
            Load();
        }

        /// <summary>
        /// Gets the full path to the secrets file used by this instance.
        /// </summary>
        internal string SecretsFilePath => _secretsFilePath;

        /// <summary>
        /// Registers the DPAPI protect/unprotect functions. Must be called once at startup
        /// before any credential operations. Typically wired to CredentialStore.Protect/Unprotect.
        /// </summary>
        /// <param name="protectFunc">Function that encrypts a plain string and returns base64 blob.</param>
        /// <param name="unprotectFunc">Function that decrypts a base64 blob and returns SecureString.</param>
        public static void RegisterProtectionFunctions(
            Func<string, string> protectFunc,
            Func<string, SecureString> unprotectFunc)
        {
            _protectFunc = protectFunc ?? throw new ArgumentNullException(nameof(protectFunc));
            _unprotectFunc = unprotectFunc ?? throw new ArgumentNullException(nameof(unprotectFunc));
            Log.Info("GameApiCredentialManager: protection functions registered");
        }

        /// <summary>
        /// Encrypts and stores a game API key for the specified character.
        /// </summary>
        /// <param name="playerUUID">The character's unique identifier.</param>
        /// <param name="plainTextKey">The plain-text API key to encrypt and store.</param>
        public void StoreKey(string playerUUID, string plainTextKey)
        {
            if (string.IsNullOrEmpty(playerUUID))
            {
                throw new ArgumentNullException(nameof(playerUUID));
            }

            if (string.IsNullOrEmpty(plainTextKey))
            {
                throw new ArgumentNullException(nameof(plainTextKey));
            }

            EnsureProtectionRegistered();
            string protectedBase64 = _protectFunc(plainTextKey);
            _protectedKeys[playerUUID] = protectedBase64;
            Log.Info("Stored API key for character {0}", playerUUID);
            Save();
        }

        /// <summary>
        /// Retrieves and decrypts the game API key for the specified character.
        /// </summary>
        /// <param name="playerUUID">The character's unique identifier.</param>
        /// <returns>A read-only <see cref="SecureString"/> containing the decrypted API key.</returns>
        public SecureString GetKey(string playerUUID)
        {
            if (string.IsNullOrEmpty(playerUUID))
            {
                throw new ArgumentNullException(nameof(playerUUID));
            }

            if (!_protectedKeys.ContainsKey(playerUUID))
            {
                return null;
            }

            EnsureProtectionRegistered();
            string protectedBase64 = _protectedKeys[playerUUID];
            return _unprotectFunc(protectedBase64);
        }

        /// <summary>
        /// Removes the stored API key for the specified character and overwrites the secrets file.
        /// </summary>
        /// <param name="playerUUID">The character's unique identifier.</param>
        public void RemoveKey(string playerUUID)
        {
            if (string.IsNullOrEmpty(playerUUID))
            {
                throw new ArgumentNullException(nameof(playerUUID));
            }

            if (_protectedKeys.Remove(playerUUID))
            {
                Log.Info("Removed API key for character {0}", playerUUID);
                Save();
            }
        }

        /// <summary>
        /// Checks whether an API key is stored for the specified character.
        /// </summary>
        /// <param name="playerUUID">The character's unique identifier.</param>
        /// <returns>True if a key exists for the character; otherwise false.</returns>
        public bool HasKey(string playerUUID)
        {
            if (string.IsNullOrEmpty(playerUUID))
            {
                return false;
            }

            return _protectedKeys.ContainsKey(playerUUID);
        }

        /// <summary>
        /// Gets the list of player UUIDs that have configured API keys.
        /// </summary>
        /// <returns>A read-only list of player UUIDs with stored keys.</returns>
        public IReadOnlyList<string> GetConfiguredPlayerUUIDs()
        {
            return new List<string>(_protectedKeys.Keys).AsReadOnly();
        }

        /// <summary>
        /// Throws if protection functions have not been registered.
        /// </summary>
        private static void EnsureProtectionRegistered()
        {
            if (_protectFunc == null || _unprotectFunc == null)
            {
                throw new InvalidOperationException(
                    "GameApiCredentialManager protection functions not registered. " +
                    "Call RegisterProtectionFunctions() at startup.");
            }
        }

        /// <summary>
        /// Loads encrypted credentials from the secrets file.
        /// If the file is corrupted or unreadable, logs the error, discards the file,
        /// and initializes an empty dictionary.
        /// </summary>
        private void Load()
        {
            if (!File.Exists(_secretsFilePath))
            {
                Log.Info("Game API secrets file not found at {0}, starting with empty credentials", _secretsFilePath);
                _protectedKeys = new Dictionary<string, string>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_secretsFilePath);
                _protectedKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (_protectedKeys == null)
                {
                    _protectedKeys = new Dictionary<string, string>();
                }

                Log.Info("Loaded {0} game API credential(s) from {1}", _protectedKeys.Count, _secretsFilePath);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Corrupted game API secrets file at {0}, discarding and starting empty", _secretsFilePath);
                DiscardCorruptFile();
                _protectedKeys = new Dictionary<string, string>();
            }
            catch (IOException ex)
            {
                Log.Error(ex, "I/O error reading game API secrets file at {0}, discarding and starting empty", _secretsFilePath);
                DiscardCorruptFile();
                _protectedKeys = new Dictionary<string, string>();
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "Access denied reading game API secrets file at {0}, starting empty", _secretsFilePath);
                _protectedKeys = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Serializes the credential dictionary to JSON and writes atomically via SafeFileWriter.
        /// </summary>
        private void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_secretsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Log.Info("Created game API secrets directory: {0}", dir);
                }

                string json = JsonConvert.SerializeObject(_protectedKeys, Formatting.Indented);
                SafeFileWriter.WriteAllText(_secretsFilePath, json);
                Log.Debug("Saved {0} game API credential(s) to {1}", _protectedKeys.Count, _secretsFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving game API secrets file to {0}", _secretsFilePath);
                throw;
            }
        }

        /// <summary>
        /// Attempts to delete a corrupted secrets file so a fresh one can be created.
        /// </summary>
        private void DiscardCorruptFile()
        {
            try
            {
                if (File.Exists(_secretsFilePath))
                {
                    File.Delete(_secretsFilePath);
                    Log.Info("Discarded corrupted game API secrets file at {0}", _secretsFilePath);
                }
            }
            catch (IOException ex)
            {
                Log.Warn(ex, "Could not delete corrupted secrets file at {0}", _secretsFilePath);
            }
        }
    }
}
