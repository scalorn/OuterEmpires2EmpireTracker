using System;
using System.IO;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    public class PreferencesStore
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _filePath;

        private static PreferencesStore _instance;

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
