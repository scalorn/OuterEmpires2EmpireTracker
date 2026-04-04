using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
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
        public string SurveyID { get => _survey.SurveyID; set => _survey.SurveyID = value; }
        public string NickName { get => _survey.NickName; set => _survey.NickName = value; }
        public string ScannedBy { get => _survey.ScannedBy; set => _survey.ScannedBy = value; }
        public string DateTime { get => _survey.DateTime; set => _survey.DateTime = value; }
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
            var list = _playerContext.blueprintList
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

        public IReadOnlyList<Survey> GetFilteredSurveys(string nameFilter)
        {
            var list = new List<Survey>(_playerContext.surveyList);
            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(s => s.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
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
                _playerContext.surveyList.Add(_survey);
            }
            _playerContext.writeContext();
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(_survey.UUID)) return;
            _playerContext.surveyList.Remove(_survey);
            _playerContext.writeContext();
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
