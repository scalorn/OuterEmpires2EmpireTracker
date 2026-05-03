using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for survey data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only SurveyService mutates the actual entity.
    /// </summary>
    public class SurveyViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Snapshot loaded from - kept for dirty comparison
        private ReadOnlySurvey _original;

        // Local edit state - disconnected from entity
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _planetName = string.Empty;
        private string _systemName = string.Empty;
        private string _surveyID = string.Empty;
        private string _nickName = string.Empty;
        private string _scannedBy = string.Empty;
        private string _dateTime = string.Empty;
        private string _scannerBlueprintUUID = string.Empty;
        private string _asteroidUUID = string.Empty;
        private SurveyType _surveyType = SurveyType.Planet;
        private Dictionary<string, SurveyResource> _resources = new Dictionary<string, SurveyResource>();
        private Dictionary<string, string> _properties = new Dictionary<string, string>();
        /// <summary>Initializes a new instance of the <see cref="SurveyViewModel"/> class.</summary>
        public SurveyViewModel()
        {
        }

        // -----------------------------------------------------------------------
        // Read-only state
        // -----------------------------------------------------------------------

        /// <summary>Gets a value indicating whether this is a new survey not yet saved.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets the survey UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the owner UUID.</summary>
        public string OwnerUUID => _ownerUUID;

        /// <summary>Gets the original snapshot this edit buffer was loaded from.</summary>
        public ReadOnlySurvey Original => _original;

        // -----------------------------------------------------------------------
        // Editable scalar fields - local edit state
        // -----------------------------------------------------------------------

        public string PlanetName
        {
            get => _planetName;
            set => _planetName = value;
        }

        public string SystemName
        {
            get => _systemName;
            set => _systemName = value;
        }

        public string SurveyID
        {
            get => _surveyID;
            set => _surveyID = value;
        }

        public string NickName
        {
            get => _nickName;
            set => _nickName = value;
        }

        public string ScannedBy
        {
            get => _scannedBy;
            set => _scannedBy = value;
        }

        public string DateTime
        {
            get => _dateTime;
            set => _dateTime = value;
        }

        public string DisplayDateTime => SurveyDateTimeParser.FormatForDisplay(_dateTime);

        public string ScannerBlueprintUUID
        {
            get => _scannerBlueprintUUID;
            set => _scannerBlueprintUUID = value;
        }

        public string AsteroidUUID
        {
            get => _asteroidUUID;
            set => _asteroidUUID = value;
        }

        public SurveyType SurveyTypeValue
        {
            get => _surveyType;
            set => _surveyType = value;
        }

        // -----------------------------------------------------------------------
        // Editable collections
        // -----------------------------------------------------------------------

        public Dictionary<string, SurveyResource> Resources => _resources;

        public Dictionary<string, string> Properties => _properties;

        // -----------------------------------------------------------------------
        // Sensor reading convenience accessors (stored in Properties)
        // -----------------------------------------------------------------------

        public string SensorAbundance
        {
            get => _properties.TryGetValue("SensorAbundance", out var v) ? v : string.Empty;
            set => _properties["SensorAbundance"] = value;
        }

        public string PurityModifier
        {
            get => _properties.TryGetValue("PurityModifier", out var v) ? v : string.Empty;
            set => _properties["PurityModifier"] = value;
        }

        public string ScanLevel
        {
            get => _properties.TryGetValue("ScanLevel", out var v) ? v : string.Empty;
            set => _properties["ScanLevel"] = value;
        }

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether any local field differs from the original snapshot.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    // New survey - dirty once any field has a non-default value
                    return !string.IsNullOrEmpty(_planetName)
                        || !string.IsNullOrEmpty(_systemName)
                        || !string.IsNullOrEmpty(_surveyID)
                        || !string.IsNullOrEmpty(_nickName)
                        || !string.IsNullOrEmpty(_scannedBy)
                        || !string.IsNullOrEmpty(_dateTime)
                        || !string.IsNullOrEmpty(_scannerBlueprintUUID)
                        || _surveyType != SurveyType.Planet
                        || _resources.Count > 0
                        || _properties.Count > 0;
                }

                // Scalar fields
                if (_planetName != (_original.PlanetName ?? string.Empty))
                {
                    return true;
                }

                if (_systemName != (_original.SystemName ?? string.Empty))
                {
                    return true;
                }

                if (_surveyID != (_original.SurveyID ?? string.Empty))
                {
                    return true;
                }

                if (_nickName != (_original.NickName ?? string.Empty))
                {
                    return true;
                }

                if (_scannedBy != (_original.ScannedBy ?? string.Empty))
                {
                    return true;
                }

                if (_dateTime != (_original.DateTime ?? string.Empty))
                {
                    return true;
                }

                if (_scannerBlueprintUUID != (_original.ScannerBlueprintUUID ?? string.Empty))
                {
                    return true;
                }

                if (_asteroidUUID != (_original.AsteroidUUID ?? string.Empty))
                {
                    return true;
                }

                if (_surveyType != _original.SurveyType)
                {
                    return true;
                }

                // Collections
                if (!ResourcesEqual(_resources, _original.Resources))
                {
                    return true;
                }

                if (!PropertiesEqual(_properties, _original.Properties))
                {
                    return true;
                }

                return false;
            }
        }

        // -----------------------------------------------------------------------
        // LoadFrom / Reset
        // -----------------------------------------------------------------------

        /// <summary>
        /// Loads field values from a ReadOnlySurvey snapshot.
        /// Retains the original for dirty comparison.
        /// </summary>
        public void LoadFrom(ReadOnlySurvey ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID;
            _planetName = ro.PlanetName ?? string.Empty;
            _systemName = ro.SystemName ?? string.Empty;
            _surveyID = ro.SurveyID ?? string.Empty;
            _nickName = ro.NickName ?? string.Empty;
            _scannedBy = ro.ScannedBy ?? string.Empty;
            _dateTime = ro.DateTime ?? string.Empty;
            _scannerBlueprintUUID = ro.ScannerBlueprintUUID ?? string.Empty;
            _asteroidUUID = ro.AsteroidUUID ?? string.Empty;
            _surveyType = ro.SurveyType;

            // Deep copy Resources (each SurveyResource is cloned)
            _resources = DeepCopyResources(ro.Resources);

            // Deep copy Properties
            _properties = new Dictionary<string, string>();
            foreach (var kvp in ro.Properties)
            {
                _properties[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Resets to empty state for a new survey.
        /// </summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _planetName = string.Empty;
            _systemName = string.Empty;
            _surveyID = string.Empty;
            _nickName = string.Empty;
            _scannedBy = string.Empty;
            _dateTime = string.Empty;
            _scannerBlueprintUUID = string.Empty;
            _asteroidUUID = string.Empty;
            _surveyType = SurveyType.Planet;
            _resources = new Dictionary<string, SurveyResource>();
            _properties = new Dictionary<string, string>();
        }

        // -----------------------------------------------------------------------
        // Build request DTOs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds an update request carrying both the original snapshot
        /// and the current local state.
        /// </summary>
        public SurveyUpdateRequest BuildUpdateRequest()
        {
            return new SurveyUpdateRequest
            {
                Original = _original,
                PlanetName = _planetName,
                SystemName = _systemName,
                SurveyID = _surveyID,
                NickName = _nickName,
                ScannedBy = _scannedBy,
                DateTime = _dateTime,
                ScannerBlueprintUUID = _scannerBlueprintUUID,
                AsteroidUUID = _asteroidUUID,
                SurveyType = _surveyType,
                Resources = DeepCopyResources(_resources),
                Properties = new Dictionary<string, string>(_properties),
            };
        }

        /// <summary>
        /// Builds a create request for a new survey.
        /// </summary>
        public SurveyCreateRequest BuildCreateRequest()
        {
            return new SurveyCreateRequest
            {
                PlanetName = _planetName,
                SystemName = _systemName,
                SurveyID = _surveyID,
                NickName = _nickName,
                ScannedBy = _scannedBy,
                DateTime = _dateTime,
                ScannerBlueprintUUID = _scannerBlueprintUUID,
                AsteroidUUID = _asteroidUUID,
                SurveyType = _surveyType,
                Resources = DeepCopyResources(_resources),
                Properties = new Dictionary<string, string>(_properties),
            };
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private static bool ResourcesEqual(
            Dictionary<string, SurveyResource> local,
            IReadOnlyDictionary<string, ReadOnlySurveyResource> original)
        {
            if (local.Count != original.Count)
            {
                return false;
            }

            foreach (var kvp in local)
            {
                if (!original.TryGetValue(kvp.Key, out ReadOnlySurveyResource origRes))
                {
                    return false;
                }

                if (kvp.Value.Resource != origRes.Resource)
                {
                    return false;
                }

                if (kvp.Value.Purity != origRes.Purity)
                {
                    return false;
                }

                if (kvp.Value.Amount != origRes.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PropertiesEqual(
            Dictionary<string, string> local,
            IReadOnlyDictionary<string, string> original)
        {
            if (local.Count != original.Count)
            {
                return false;
            }

            foreach (var kvp in local)
            {
                if (!original.TryGetValue(kvp.Key, out string origVal))
                {
                    return false;
                }

                if (kvp.Value != origVal)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, SurveyResource> DeepCopyResources(
            IReadOnlyDictionary<string, ReadOnlySurveyResource> source)
        {
            var copy = new Dictionary<string, SurveyResource>();
            foreach (var kvp in source)
            {
                copy[kvp.Key] = new SurveyResource(kvp.Value.Resource, kvp.Value.Purity, kvp.Value.Amount);
            }

            return copy;
        }

        private static Dictionary<string, SurveyResource> DeepCopyResources(
            Dictionary<string, SurveyResource> source)
        {
            var copy = new Dictionary<string, SurveyResource>();
            foreach (var kvp in source)
            {
                copy[kvp.Key] = new SurveyResource(kvp.Value.Resource, kvp.Value.Purity, kvp.Value.Amount);
            }

            return copy;
        }
    }
}
