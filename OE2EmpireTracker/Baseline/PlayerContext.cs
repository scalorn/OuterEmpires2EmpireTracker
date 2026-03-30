using Newtonsoft.Json;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Baseline
{
    public class PlayerContext
    {
        private static PlayerContext Instance;
        private static string filePath = @"..\..\PlayerData.json";

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
        private PlayerContext() : base()
        {
            Instance = this;

            PlayerRoot playerRoot = null;
            if (File.Exists(filePath))
            {
                // Read the file content into a string
                string jsonContent = File.ReadAllText(filePath);
                playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(jsonContent);
            }
            else
            {
                playerRoot = new PlayerRoot();
            }
            initPlayerProfiles(playerRoot);
            initBlueprints(playerRoot);
            initSurveys(playerRoot);
            initColonies(playerRoot);
        }

        public void writeContext()
        {
            PlayerRoot playerRoot = new PlayerRoot();
            playerRoot.PlayerProfile = playerProfileList.ToArray();
            playerRoot.Blueprint = blueprintList.ToArray();
            playerRoot.Survey = surveyList.ToArray();
            playerRoot.Colony = colonyList.ToArray();

            string jsonContent = JsonConvert.SerializeObject(playerRoot, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent);
            Debug.Print("Player.WriteContext done");
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
        public void initBlueprints(PlayerRoot playerRoot)
        {
            List<Blueprint> list = new List<Blueprint>(playerRoot.Blueprint);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            blueprintList = new BindingList<Blueprint>(list);
            // Initialize the BindingSource component
            bindingSourceBlueprint = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceBlueprint.DataSource = blueprintList;
        }
        public Blueprint findBlueprint(string id)
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

        public void initSurveys(PlayerRoot playerRoot)
        {
            List<Survey> list = new List<Survey>(playerRoot.Survey);
            list = list.OrderBy(p => p.PlanetName).ThenBy(p => p.DateTime).ToList();

            surveyList = new BindingList<Survey>(list);
            // Initialize the BindingSource component
            bindingSourceSurvey = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceSurvey.DataSource = surveyList;
        }
        public Survey findSurvey(string id)
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
        public Colony findColony(string id)
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
                        if (structure.CompletionTime != null && structure.CompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.source = CountDownTimeReference.SourceType.Colony;
                            reference.sourceUUID = colony.UUID;
                            reference.internalUUID = structure.UUID;
                            reference.countDownTime = structure.CompletionTime;
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
        public PlayerProfile[] PlayerProfile;
        public Blueprint[] Blueprint;
        public Survey[] Survey;
        public Colony[] Colony;
        public PlayerRoot()
        {
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
