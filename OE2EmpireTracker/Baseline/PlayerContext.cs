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

        public BindingList<Blueprint> blueprintList;
        public BindingSource bindingSourceBlueprint;
        public BindingList<Survey> surveyList;
        public BindingSource bindingSourceSurvey;

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
            initBlueprints(playerRoot);
            initSurveys(playerRoot);

            //blueprintList.Add(new Blueprint());
            //Debug.Print("Blueprint Count " + playerRoot.Blueprint.Length);
        }

        public void writeContext()
        {
            PlayerRoot playerRoot = new PlayerRoot();
            playerRoot.Blueprint = blueprintList.ToArray();
            playerRoot.Survey = surveyList.ToArray();

            string jsonContent = JsonConvert.SerializeObject(playerRoot,Formatting.Indented);
            File.WriteAllText(filePath, jsonContent);
            Debug.Print("Player.WriteContext done");
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
    }


    public class PlayerRoot
    {
        public Blueprint[] Blueprint;
        public Survey[] Survey;
        public PlayerRoot()
        {
            Blueprint = new Blueprint[0];
            Survey = new Survey[0];
        }
    }

}
