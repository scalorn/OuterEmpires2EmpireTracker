using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a Survey data object and exposes typed properties,
    /// hiding all direct data access from the UI layer.
    /// </summary>
    public class SurveyViewModel
    {
        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;
        private Survey _survey;

        public Survey Data => _survey;

        public SurveyViewModel(Survey survey, PlayerContext playerContext, EmpireContext empireContext)
        {
            _survey = survey ?? throw new ArgumentNullException(nameof(survey));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _empireContext = empireContext ?? throw new ArgumentNullException(nameof(empireContext));
        }

        // -----------------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------------

        public string PlanetName { get => _survey.PlanetName; set => _survey.PlanetName = value; }
        public string SystemName { get => _survey.SystemName; set => _survey.SystemName = value; }
        public string SurveyID { get => _survey.SurveyID; set => _survey.SurveyID = value; }
        public string NickName { get => _survey.NickName; set => _survey.NickName = value; }
        public string ScannedBy { get => _survey.ScannedBy; set => _survey.ScannedBy = value; }
        public string DateTime { get => _survey.DateTime; set => _survey.DateTime = value; }
        public string DisplayDateTime => SurveyDateTimeParser.FormatForDisplay(_survey.DateTime);
        public string ScannerBlueprintUUID { get => _survey.ScannerBlueprintUUID; set => _survey.ScannerBlueprintUUID = value; }
        public string UUID => _survey.UUID;

        // -----------------------------------------------------------------------
        // Properties (sensor readings)
        // -----------------------------------------------------------------------

        public string SensorAbundance
        {
            get => _survey.Properties.ContainsKey("SensorAbundance") ? _survey.Properties["SensorAbundance"] : "";
            set => _survey.Properties["SensorAbundance"] = value;
        }

        public string PurityModifier
        {
            get => _survey.Properties.ContainsKey("PurityModifier") ? _survey.Properties["PurityModifier"] : "";
            set => _survey.Properties["PurityModifier"] = value;
        }

        public string ScanLevel
        {
            get => _survey.Properties.ContainsKey("ScanLevel") ? _survey.Properties["ScanLevel"] : "";
            set => _survey.Properties["ScanLevel"] = value;
        }

        // -----------------------------------------------------------------------
        // Resources
        // -----------------------------------------------------------------------

        public void ClearResources() => _survey.Resources.Clear();

        public void SetResource(string name, SurveyResource resource)
        {
            _survey.Resources[name] = resource;
        }

        public IEnumerable<KeyValuePair<string, SurveyResource>> GetResources() => _survey.Resources;

        // -----------------------------------------------------------------------
        // Scanner blueprint lookup
        // -----------------------------------------------------------------------

        public Blueprint FindScannerBlueprint()
        {
            if (string.IsNullOrEmpty(_survey.ScannerBlueprintUUID)) return null;
            return _playerContext.FindBlueprint(_survey.ScannerBlueprintUUID);
        }

        public IReadOnlyList<Blueprint> GetFilteredScannerBlueprints(string nameFilter)
        {
            BlueprintType scanners = _empireContext.FindBlueprintType("SystemObjectScanner");
            var list = _playerContext.GetAllBlueprints()
                .Where(b => b.BluePrintType == scanners.Id)
                .ToList();

            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(b => b.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            list.Insert(0, new Blueprint());
            return list.AsReadOnly();
        }

        // -----------------------------------------------------------------------
        // List filtering
        // -----------------------------------------------------------------------

        public IReadOnlyList<Survey> GetFilteredSurveys(string nameFilter, string resourceFilter = "",
            SurveyType? typeFilter = null, string purityFilter = "", int minAmount = 0)
        {
            var list = _playerContext.GetCurrentPlayerSurveys();
            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(s => s.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            if (!string.IsNullOrEmpty(resourceFilter))
            {
                list = list
                    .Where(s => s.Resources.Values.Any(r =>
                        string.Equals(r.Resource, resourceFilter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            if (typeFilter.HasValue)
            {
                list = list.Where(s => s.SurveyType == typeFilter.Value).ToList();
            }
            if (!string.IsNullOrEmpty(purityFilter))
            {
                list = list
                    .Where(s => s.Resources.Values.Any(r =>
                        string.Equals(r.Purity, purityFilter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            if (minAmount > 0)
            {
                list = list
                    .Where(s => s.Resources.Values.Any(r =>
                    {
                        if (decimal.TryParse(r.Amount, out decimal amt))
                            return amt >= minAmount;
                        return false;
                    }))
                    .ToList();
            }
            return list.AsReadOnly();
        }

        // -----------------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------------

        public void Save()
        {
            if (string.IsNullOrEmpty(_survey.UUID))
            {
                _survey.UUID = Guid.NewGuid().ToString();
                _playerContext.SurveyList.Add(_survey);
            }
            if (string.IsNullOrEmpty(_survey.OwnerUUID))
            {
                _survey.OwnerUUID = _playerContext.CurrentPlayerUUID;
            }
            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(_survey.UUID);
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(_survey.UUID)) return;
            string deletedUUID = _survey.UUID;
            _playerContext.SurveyList.Remove(_survey);
            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(deletedUUID);
        }

        /// <summary>
        /// Resets the ViewModel to point at a new blank survey.
        /// </summary>
        public void Reset()
        {
            _survey = new Survey();
        }

        /// <summary>
        /// Switches the ViewModel to point at a different survey.
        /// </summary>
        public void SelectSurvey(Survey survey)
        {
            _survey = survey ?? new Survey();
        }
    }
}
