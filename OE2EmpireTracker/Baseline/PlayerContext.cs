using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Baseline
{
    public class PlayerContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static PlayerContext Instance;
        public static string FilePath { get; set; } = @"..\..\PlayerData.json";

        private string _currentPlayerUUID = string.Empty;

        /// <summary>
        /// UUID of the currently selected player. Forms filter data by this value.
        /// </summary>
        public string CurrentPlayerUUID
        {
            get => _currentPlayerUUID;
            set
            {
                if (_currentPlayerUUID != value)
                {
                    _currentPlayerUUID = value ?? string.Empty;
                    Log.Info("Current player changed to {0}", _currentPlayerUUID);
                    CurrentPlayerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Fired when CurrentPlayerUUID changes. Forms subscribe to refresh their data.
        /// </summary>
        public event EventHandler CurrentPlayerChanged;

        /// <summary>
        /// Fired when the player profile list changes (add/rename/delete).
        /// MainWindow subscribes to refresh the player dropdown.
        /// </summary>
        public event EventHandler PlayerProfilesChanged;

        /// <summary>
        /// Notifies subscribers that the player profile list has changed.
        /// </summary>
        public void OnPlayerProfilesChanged()
        {
            PlayerProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns the PlayerProfile for the currently selected player, or null.
        /// </summary>
        public PlayerProfile CurrentPlayer
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPlayerUUID)) return null;
                return playerProfileList.FirstOrDefault(p => p.UUID == _currentPlayerUUID);
            }
        }

        public BindingList<PlayerProfile> playerProfileList;
        public BindingSource bindingSourcePlayerProfile;
        public BindingList<Blueprint> blueprintList;
        public BindingSource bindingSourceBlueprint;
        public BindingList<Survey> surveyList;
        public BindingSource bindingSourceSurvey;
        public BindingList<Colony> colonyList;
        public BindingSource bindingSourceColony;

        public IEnumerable<CountDownTimeReference> ActiveCountdowns => AllCountdownSources()
            .Where(c => c.countDownTime.TimeRemaining > 0)
            .OrderBy(c => c.countDownTime.TimeRemaining);

        public static PlayerContext getInstance()
        {
            if (Instance == null)
            {
                Instance = new PlayerContext();
            }
            return Instance;
        }

        public static void Reset()
        {
            Instance = null;
        }

        private PlayerContext() : base()
        {
            Instance = this;

            PlayerRoot playerRoot = null;
            if (File.Exists(FilePath))
            {
                Log.Info("Loading player data from {0}", FilePath);
                string jsonContent = File.ReadAllText(FilePath);
                playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(jsonContent);
                Log.Info("Loaded {0} profiles, {1} blueprints, {2} surveys, {3} colonies",
                    playerRoot.PlayerProfile.Length, playerRoot.Blueprint.Length,
                    playerRoot.Survey.Length, playerRoot.Colony.Length);
            }
            else
            {
                Log.Warn("Player data file not found at {0}, starting with empty data", FilePath);
                playerRoot = new PlayerRoot();
            }
            initPlayerProfiles(playerRoot);
            InitBlueprints(playerRoot);
            InitSurveys(playerRoot);
            initColonies(playerRoot);

            // Migrate and restore current player
            MigrateOwnerUUIDs();
            RestoreCurrentPlayer(playerRoot.CurrentPlayerUUID);
        }

        public void writeContext()
        {
            PlayerRoot playerRoot = new PlayerRoot();
            playerRoot.CurrentPlayerUUID = _currentPlayerUUID;
            playerRoot.PlayerProfile = playerProfileList.ToArray();
            playerRoot.Blueprint = blueprintList.ToArray();
            playerRoot.Survey = surveyList.ToArray();
            playerRoot.Colony = colonyList.ToArray();

            string jsonContent = JsonConvert.SerializeObject(playerRoot, Formatting.Indented);
            File.WriteAllText(FilePath, jsonContent);
            Log.Info("Player data saved to {0}", FilePath);
        }
        public void initPlayerProfiles(PlayerRoot playerRoot)
        {
            List<PlayerProfile> list = new List<PlayerProfile>(playerRoot.PlayerProfile);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            playerProfileList = new BindingList<PlayerProfile>(list);
            // Initialize the BindingSource component
            bindingSourcePlayerProfile = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourcePlayerProfile.DataSource = playerProfileList;
        }
        public void InitBlueprints(PlayerRoot playerRoot)
        {
            List<Blueprint> list = new List<Blueprint>(playerRoot.Blueprint);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            blueprintList = new BindingList<Blueprint>(list);
            // Initialize the BindingSource component
            bindingSourceBlueprint = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceBlueprint.DataSource = blueprintList;
        }
        public Blueprint FindBlueprint(string id)
        {
            var filteredList = blueprintList
                .Where(item => item.UUID == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }
            return null;
        }

        public void InitSurveys(PlayerRoot playerRoot)
        {
            List<Survey> list = new List<Survey>(playerRoot.Survey);
            list = list.OrderBy(p => p.PlanetName).ThenBy(p => p.DateTime).ToList();

            surveyList = new BindingList<Survey>(list);
            // Initialize the BindingSource component
            bindingSourceSurvey = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceSurvey.DataSource = surveyList;
        }
        public Survey FindSurvey(string id)
        {
            var filteredList = surveyList
                .Where(item => item.UUID == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }
            return null;
        }

        public void initColonies(PlayerRoot playerRoot)
        {
            List<Colony> list = new List<Colony>(playerRoot.Colony);
            list = list.OrderBy(p => p.PlanetName).ToList();

            colonyList = new BindingList<Colony>(list);
            // Initialize the BindingSource component
            bindingSourceColony = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceColony.DataSource = colonyList;
        }
        public Colony FindColony(string id)
        {
            var filteredList = colonyList
                .Where(item => item.UUID == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }
            return null;
        }

        // -----------------------------------------------------------------------
        // Player Selection & Migration
        // -----------------------------------------------------------------------

        /// <summary>
        /// Auto-assigns empty OwnerUUID on colonies, blueprints, and surveys
        /// to the first player profile (alphabetically). Handles migration of
        /// existing save files that predate multi-player support.
        /// </summary>
        private void MigrateOwnerUUIDs()
        {
            if (playerProfileList.Count == 0) return;

            string firstPlayerUUID = playerProfileList[0].UUID;
            if (string.IsNullOrEmpty(firstPlayerUUID)) return;

            int migrated = 0;
            foreach (var colony in colonyList)
            {
                if (string.IsNullOrEmpty(colony.OwnerUUID))
                {
                    colony.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }
            foreach (var blueprint in blueprintList)
            {
                if (string.IsNullOrEmpty(blueprint.OwnerUUID))
                {
                    blueprint.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }
            foreach (var survey in surveyList)
            {
                if (string.IsNullOrEmpty(survey.OwnerUUID))
                {
                    survey.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }

            if (migrated > 0)
            {
                Log.Info("Migrated {0} items to player {1}", migrated, playerProfileList[0].Name);
            }
        }

        /// <summary>
        /// Restores the current player from the saved UUID, falling back to
        /// the first player if the saved UUID is invalid or empty.
        /// Does not fire CurrentPlayerChanged (called during construction).
        /// </summary>
        private void RestoreCurrentPlayer(string savedUUID)
        {
            if (!string.IsNullOrEmpty(savedUUID) &&
                playerProfileList.Any(p => p.UUID == savedUUID))
            {
                _currentPlayerUUID = savedUUID;
            }
            else if (playerProfileList.Count > 0)
            {
                _currentPlayerUUID = playerProfileList[0].UUID ?? string.Empty;
            }
            Log.Info("Current player restored: {0}", _currentPlayerUUID);
        }

        /// <summary>
        /// Returns colonies owned by the current player.
        /// </summary>
        public List<Colony> GetCurrentPlayerColonies()
        {
            return colonyList.Where(c => c.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns blueprints owned by the current player.
        /// </summary>
        public List<Blueprint> GetCurrentPlayerBlueprints()
        {
            return blueprintList.Where(b => b.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns surveys owned by the current player.
        /// </summary>
        public List<Survey> GetCurrentPlayerSurveys()
        {
            return surveyList.Where(s => s.OwnerUUID == _currentPlayerUUID).ToList();
        }


        public List<CountDownTimeReference> AllCountdownSources()
        {
            List<CountDownTimeReference> countdowns = new List<CountDownTimeReference>();
            foreach (var player in playerProfileList)
            {
                if (player.Skills != null)
                {
                    foreach (var skill in player.Skills)
                    {
                        if (skill.Value.CompletionTime != null && skill.Value.CompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.source = CountDownTimeReference.SourceType.Player;
                            reference.sourceUUID = player.UUID;
                            reference.internalUUID = skill.Key;
                            reference.countDownTime = skill.Value.CompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }
            foreach (var colony in colonyList)
            {
                if (colony.Structures != null)
                {
                    foreach (var structure in colony.Structures)
                    {
                        if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.source = CountDownTimeReference.SourceType.Colony;
                            reference.sourceUUID = colony.UUID;
                            reference.internalUUID = structure.UUID;
                            reference.countDownTime = structure.ProcessCompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }
            return countdowns;
        }
    }

    public class PlayerRoot
    {
        public string CurrentPlayerUUID;
        public PlayerProfile[] PlayerProfile;
        public Blueprint[] Blueprint;
        public Survey[] Survey;
        public Colony[] Colony;
        public PlayerRoot()
        {
            CurrentPlayerUUID = string.Empty;
            PlayerProfile = new PlayerProfile[0];
            Blueprint = new Blueprint[0];
            Survey = new Survey[0];
            Colony = new Colony[0];
        }
    }

    public class CountDownTimeReference
    {
        public enum SourceType
        {
            None,
            Player,
            Colony
        }

        public SourceType source { get; set; }
        public string sourceUUID { get; set; }
        public string internalUUID { get; set; }
        public CountDownTime countDownTime { get; set; }
    }

}
