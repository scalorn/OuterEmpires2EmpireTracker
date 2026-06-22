using System;
using System.IO;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    public class PreferencesStore
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PreferencesStore _instance;

        private readonly string _filePath;

        private UIPreferences _preferences;

        private PreferencesStore()
        {
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OE2EmpireTracker",
                "UIPreferences.json");
            Load();
        }

        public UIPreferences Preferences => _preferences;

        public static PreferencesStore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PreferencesStore();
            }

            return _instance;
        }

        public static void Reset()
        {
            _instance = null;
        }

        /// <summary>
        /// Returns the mail sync interval in milliseconds, clamped to [1, 60] minutes.
        /// </summary>
        public static int GetMailSyncIntervalMs()
        {
            var prefs = PreferencesStore.GetInstance().Preferences;
            int minutes = Math.Max(1, Math.Min(60, prefs.MailSyncIntervalMinutes));
            return minutes * 60 * 1000;
        }

        /// <summary>
        /// Parses the storage backend type string from preferences.
        /// Falls back to <see cref="StorageBackendType.JsonSingleFile"/> with a warning if unrecognized.
        /// </summary>
        /// <param name="value">The string value to parse (case-insensitive).</param>
        /// <returns>The parsed <see cref="StorageBackendType"/> value.</returns>
        public StorageBackendType ParseStorageBackendType(string value)
        {
            if (Enum.TryParse<StorageBackendType>(value, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            Log.Warn("Unrecognized StorageBackendType '{0}', falling back to JsonSingleFile", value);
            return StorageBackendType.JsonSingleFile;
        }

        /// <summary>
        /// Resolves the full storage backend configuration from current preferences,
        /// applying default paths when no explicit path is configured.
        /// </summary>
        /// <returns>A <see cref="StorageBackendConfig"/> populated for the configured backend type.</returns>
        public StorageBackendConfig ResolveStorageConfig()
        {
            var type = ParseStorageBackendType(Preferences.StorageBackendType);
            var config = new StorageBackendConfig();

            switch (type)
            {
                case Common.Interfaces.StorageBackendType.JsonSingleFile:
                case Common.Interfaces.StorageBackendType.JsonMultiFile:
                    config.ConnectionString = !string.IsNullOrEmpty(Preferences.StoragePath)
                        ? Preferences.StoragePath
                        : AppDomain.CurrentDomain.BaseDirectory;
                    break;

                case Common.Interfaces.StorageBackendType.Sqlite:
                    string sqliteDir = !string.IsNullOrEmpty(Preferences.StoragePath)
                        ? Preferences.StoragePath
                        : Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "OE2EmpireTracker");
                    if (!Directory.Exists(sqliteDir))
                    {
                        Directory.CreateDirectory(sqliteDir);
                    }

                    config.ConnectionString = Path.Combine(sqliteDir, "OE2EmpireTracker.db");
                    break;

                case Common.Interfaces.StorageBackendType.DynamoDb:
                    config.AwsRegion = Preferences.StorageAwsRegion;
                    config.TablePrefix = Preferences.StorageTablePrefix;
                    break;

                case Common.Interfaces.StorageBackendType.Postgres:
                    config.ConnectionString = Preferences.StorageConnectionString;
                    break;
            }

            return config;
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.Info("Created preferences directory {0}", directory);
                }

                string json = JsonConvert.SerializeObject(_preferences, JsonSettings.SerializerSettings);
                SafeFileWriter.WriteAllText(_filePath, json);
                Log.Debug("Saved UI preferences to {0}", _filePath);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "I/O error saving UIPreferences file {0}", _filePath);
            }
        }

        public WindowState GetWindowState(string formTypeKey, int windowNumber)
        {
            string key = windowNumber.ToString();

            if (!_preferences.Forms.ContainsKey(formTypeKey))
            {
                _preferences.Forms[formTypeKey] = new System.Collections.Generic.Dictionary<string, WindowState>();
            }

            var formWindows = _preferences.Forms[formTypeKey];
            if (!formWindows.ContainsKey(key))
            {
                formWindows[key] = new WindowState();
            }

            return formWindows[key];
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                Log.Info("UIPreferences file not found at {0}, starting with empty preferences", _filePath);
                _preferences = new UIPreferences();
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                _preferences = JsonConvert.DeserializeObject<UIPreferences>(json);
                if (_preferences == null)
                {
                    _preferences = new UIPreferences();
                }

                if (_preferences.Thresholds == null)
                {
                    _preferences.Thresholds = new ThresholdPreferences();
                }

                if (_preferences.ServerConnection == null)
                {
                    _preferences.ServerConnection = new Client.ServerConnectionSettings();
                }

                Log.Info("Loaded UI preferences from {0}", _filePath);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Malformed JSON in UIPreferences file {0}, starting with empty preferences", _filePath);
                _preferences = new UIPreferences();
            }
            catch (IOException ex)
            {
                Log.Error(ex, "I/O error reading UIPreferences file {0}, starting with empty preferences", _filePath);
                _preferences = new UIPreferences();
            }
        }
    }
}
