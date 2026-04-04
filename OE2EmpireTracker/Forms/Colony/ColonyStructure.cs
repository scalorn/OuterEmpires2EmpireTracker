using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class ColonyStructure : UserControl
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;
        public Baseline.Colony Colony { get; set; }

        private Baseline.ColonyStructure _colonyStructureData;
        public Baseline.ColonyStructure ColonyStructureData
        {
            get => _colonyStructureData;
            set
            {
                _colonyStructureData = value;
                ViewModel = value != null
                    ? new ColonyStructureViewModel(value, playerContext)
                    : null;
            }
        }

        public ColonyStructureViewModel ViewModel { get; private set; }

        private Data.Blueprint FlatpackBlueprint { get; set; }
        //private double PowerProvided { get; set; }
        //private double PowerRequired { get; set; }

        public bool completionModification = false;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when colony structure state changes")]
        public event EventHandler ColonyStructureDataChanged;
        public ColonyStructure()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            this.SuspendLayout();
            populateStats();
            this.ResumeLayout();
        }

        public void UpdateData()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            this.SuspendLayout();
            FlatpackBlueprint = playerContext.findBlueprint(ColonyStructureData.FlatpackBlueprintUUID);

            if (FlatpackBlueprint != null)
            {
                double powerProvided = 0;
                FlatpackBlueprint.Properties.getDouble("PowerProvided", 0, out powerProvided);
                double powerRequired = 0;
                FlatpackBlueprint.Properties.getDouble("PowerRequired", 0, out powerRequired);

                //PowerProvided = powerProvided;
                //PowerRequired = powerRequired;

                Log.Info("FlatpackBlueprint.BluePrintType = " + FlatpackBlueprint.BluePrintType);
                if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                {
                    handleMiningRigControls();
                }
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                {
                    handleRefineryControls();
                }
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                {
                    handleResearchLabControls();
                }
                else {
                    flpSelection.Visible = false;
                    flpSubSelection.Visible = false;
                    flpCompletionTime.Visible = false;
                }
            }

            chkBuilt.Checked = false;
            chkStaged.Checked = false;
            chkOnline.Checked = false;

            chkWorkDetail1.Visible = false;
            chkWorkDetail2.Visible = false;
            chkWorkDetail3.Visible = false;
            CheckBox[] checkControls = new CheckBox[3];
            checkControls[0] = chkWorkDetail1;
            checkControls[1] = chkWorkDetail2;
            checkControls[2] = chkWorkDetail3;
            int controlIndex = 0;

            if (ColonyStructureData != null)
            {
                chkBuilt.Checked = ViewModel.IsBuilt;
                chkStaged.Checked = ViewModel.IsStaged;
                chkOnline.Checked = ViewModel.IsOnline;

                int index = 1;
                string key = "BlueCollar1";

                while (ViewModel.WorkerKeyExists(key))
                {
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "Blue Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = ViewModel.GetWorkerAssigned(key);
                    controlIndex++;
                    index++;
                    key = "BlueCollar" + index;
                }

                index = 1;
                key = "WhiteCollar1";
                while (ViewModel.WorkerKeyExists(key))
                {
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "White Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = ViewModel.GetWorkerAssigned(key);
                    controlIndex++;
                    index++;
                    key = "WhiteCollar" + index;
                }

                index = 1;
                key = "Specialist1";
                while (ViewModel.WorkerKeyExists(key))
                {
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "Specialist";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = ViewModel.GetWorkerAssigned(key);
                    controlIndex++;
                    index++;
                    key = "Specialist" + index;
                }

                key = "UnassignedBlueCollarDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedBlueCollarPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - Blue Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedBlueCollarPresent;
                    controlIndex++;
                }

                key = "UnassignedWhiteCollarDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedWhiteCollarPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - White Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedWhiteCollarPresent;
                    controlIndex++;
                }

                key = "UnassignedSpecialistDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedSpecialistPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - Specialist";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedSpecialistPresent;
                    controlIndex++;
                }

                bool hasAllWorkers = true;
                for (index = 0; index < controlIndex; index++) {
                    if (checkControls[index].Checked == false)
                    {
                        hasAllWorkers = false;
                    }
                }
                if (ViewModel.IsStaged)
                {
                    flpColonyStructure.BackColor = Color.Yellow;
                }
                else if (ViewModel.IsBuilt)
                {
                    if (!ViewModel.IsOnline)
                    {
                        flpColonyStructure.BackColor = Color.PaleVioletRed;
                    }
                    else
                    {
                        flpColonyStructure.BackColor = hasAllWorkers ? Color.Green : Color.LightGreen;
                    }
                }
                else
                {
                    flpColonyStructure.BackColor = Color.White;
                }
            }

            populateStats();

            this.ResumeLayout();
            guard.release();
        }

        private void handleMiningRigControls()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
                guard.release();
                return;
            }

            bool showSelection = false;
            bool showSubSelection = false;
            bool showCompletionTime = false;
            bool showCmdSubStart = false;
            bool enableCmbSelection = true;
            bool enableCmbSubSelection = true;


            if (ColonyStructureData.ProcessCompletionTime != null)
            {
                showSelection = true;
                showSubSelection = true;
                showCompletionTime = true;
                enableCmbSelection = false;
                enableCmbSubSelection = false;
            }
            else
            {
                showSelection = true;
            }

            if (showSelection && !string.IsNullOrEmpty(ColonyStructureData.MiningSurvey))
            {
                showSubSelection = true;
            }

            if (showSubSelection && (cmbSubSelection.SelectedIndex >= 0 || !string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource)))
            {
                showCmdSubStart = true;
            }

            if (showSelection)
            {
                flpSelection.Visible = true;
                if (cmbSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.MiningSurvey))
                {
                    populateSelectionWithSurveys();
                    if (!string.IsNullOrEmpty(ColonyStructureData.MiningSurvey))
                    {
                        cmbSelection.SelectedValue = ColonyStructureData.MiningSurvey;
                    }
                }
                txtSelectionFilter.Enabled = enableCmbSelection;
                cmbSelection.Enabled = enableCmbSelection;
                cmdStart.Visible = false;
            }
            else
            {
                flpSelection.Visible = false;
            }

            if ( showSubSelection)
            {
                flpSubSelection.Visible = true;
                if (cmbSubSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource))
                {
                    populateSubSelectionWithSurveyResources();
                    if (!string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource))
                    {
                        cmbSubSelection.SelectedValue = ColonyStructureData.MiningSurveyResource;
                    }
                }
                txtSubSelectionFilter.Enabled = enableCmbSubSelection;
                cmbSubSelection.Enabled = enableCmbSubSelection;
                cmdSubStart.Visible = showCmdSubStart;
            }
            else
            {
                flpSubSelection.Visible = false;
            }

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;

                // Show mining progress: "<Rate>/h <Resource> (<Purity>)"
                populateProgressStatus();

                if (timerCountdown.Enabled == false)
                {
                    timerCountdown.Interval = 1000;
                    timerCountdown.Start();
                }
            }
            else
            {
                flpCompletionTime.Visible = false;
                rtbProgressStatus.Text = "";
            }

            guard.release();

            // TODO: FIXME: Changing visibilty isn't triggering a layout call.
            // flpStructureCommands.PerformLayout(); - Does NOT work.
            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void populateProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.MiningSurvey) ||
                string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            Baseline.Survey survey = playerContext.findSurvey(ColonyStructureData.MiningSurvey);
            if (survey == null || !survey.Resources.ContainsKey(ColonyStructureData.MiningSurveyResource))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            SurveyResource resource = survey.Resources[ColonyStructureData.MiningSurveyResource];
            rtbProgressStatus.Text = $"{resource.Amount}/h {resource.Resource} ({resource.Purity})";
        }

        // -----------------------------------------------------------------------
        // Refinery Controls
        // -----------------------------------------------------------------------

        private void handleRefineryControls()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
                guard.release();
                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (ColonyStructureData.ProcessCompletionTime != null)
            {
                showCompletionTime = true;
                enableCmbSelection = false;
            }

            if (!string.IsNullOrEmpty(ColonyStructureData.RefiningResource))
            {
                showCmdStart = true;
            }

            // Selection: unrefined resources from warehouse + actively mined resources
            flpSelection.Visible = true;
            if (cmbSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.RefiningResource))
            {
                populateSelectionWithUnrefinedResources();
                if (!string.IsNullOrEmpty(ColonyStructureData.RefiningResource))
                {
                    string restoreKey = ColonyStructureData.RefiningResource + "|" + ColonyStructureData.RefiningResourcePurity;
                    var recipe = RefiningRecipes.FindByInput(ColonyStructureData.RefiningResource, ColonyStructureData.RefiningResourcePurity);
                    if (recipe != null)
                    {
                        restoreKey += "|S" + recipe.Tier;
                    }
                    cmbSelection.SelectedValue = restoreKey;
                }
            }
            txtSelectionFilter.Enabled = enableCmbSelection;
            cmbSelection.Enabled = enableCmbSelection;
            cmdStart.Visible = showCmdStart && !showCompletionTime;

            // No sub-selection for refinery
            flpSubSelection.Visible = false;

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
                populateRefineryProgressStatus();
                if (timerCountdown.Enabled == false)
                {
                    timerCountdown.Interval = 1000;
                    timerCountdown.Start();
                }
            }
            else
            {
                flpCompletionTime.Visible = false;
                rtbProgressStatus.Text = "";
            }

            guard.release();
            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void populateSelectionWithUnrefinedResources()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            var unrefinedItems = new List<RefinerySelectionItem>();

            // Add unrefined resources from warehouse
            foreach (var itemEntry in Colony.Items.Items.Values)
            {
                if (itemEntry.ItemType == Data.ItemType.ItemTypeEnum.Resource &&
                    !string.IsNullOrEmpty(itemEntry.ResourcePurity) &&
                    itemEntry.ResourcePurity != "Refined")
                {
                    string key = itemEntry.BaseItemTypeID + "|" + itemEntry.ResourcePurity;
                    if (!unrefinedItems.Any(u => u.Key == key))
                    {
                        unrefinedItems.Add(new RefinerySelectionItem
                        {
                            Key = key,
                            DisplayName = $"{itemEntry.Name} ({itemEntry.ResourcePurity})",
                            ResourceName = itemEntry.BaseItemTypeID,
                            Purity = itemEntry.ResourcePurity
                        });
                    }
                }
            }

            // Add resources being actively mined
            if (Colony.Structures != null)
            {
                foreach (var structure in Colony.Structures)
                {
                    if (structure.ProcessCompletionTime != null &&
                        !string.IsNullOrEmpty(structure.MiningSurvey) &&
                        !string.IsNullOrEmpty(structure.MiningSurveyResource))
                    {
                        Baseline.Survey survey = playerContext.findSurvey(structure.MiningSurvey);
                        if (survey != null && survey.Resources.ContainsKey(structure.MiningSurveyResource))
                        {
                            SurveyResource sr = survey.Resources[structure.MiningSurveyResource];
                            if (!string.IsNullOrEmpty(sr.Purity) && sr.Purity != "Refined")
                            {
                                string key = sr.Resource + "|" + sr.Purity;
                                if (!unrefinedItems.Any(u => u.Key == key))
                                {
                                    unrefinedItems.Add(new RefinerySelectionItem
                                    {
                                        Key = key,
                                        DisplayName = $"{sr.Resource} ({sr.Purity})",
                                        ResourceName = sr.Resource,
                                        Purity = sr.Purity
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // Add synthetic recipes whose input resources are available in sufficient quantity
            foreach (var recipe in RefiningRecipes.Recipes)
            {
                var inputItems = Colony.Items.FindResource(recipe.InputResource, recipe.InputPurity);
                int totalAvailable = inputItems.Sum(i => i.Quantity);

                if (totalAvailable >= recipe.ConsumeRate)
                {
                    string key = recipe.InputResource + "|" + recipe.InputPurity + "|S" + recipe.Tier;
                    if (!unrefinedItems.Any(u => u.Key == key))
                    {
                        unrefinedItems.Add(new RefinerySelectionItem
                        {
                            Key = key,
                            DisplayName = $"{recipe.OutputResource} <- {recipe.InputResource} ({recipe.InputPurity})",
                            ResourceName = recipe.InputResource,
                            Purity = recipe.InputPurity
                        });
                    }
                }
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(searchText))
            {
                unrefinedItems = unrefinedItems
                    .Where(item => item.DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            unrefinedItems.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            unrefinedItems.Insert(0, new RefinerySelectionItem { Key = "", DisplayName = "", ResourceName = "", Purity = "" });

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "Key";
            cmbSelection.DataSource = unrefinedItems;
            cmbSelection.SelectedIndex = -1;
        }

        private void populateRefineryProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.RefiningResource) ||
                string.IsNullOrEmpty(ColonyStructureData.RefiningResourcePurity))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            // Check for synthetic recipe
            var recipe = RefiningRecipes.FindByInput(
                ColonyStructureData.RefiningResource, ColonyStructureData.RefiningResourcePurity);

            if (recipe != null)
            {
                rtbProgressStatus.Text = $"{recipe.ConsumeRate}:{recipe.ProduceRate} {recipe.OutputResource}";
            }
            else
            {
                int baseRate = 25;
                int outputRate = GetRefiningOutputRate(ColonyStructureData.RefiningResourcePurity, baseRate);
                rtbProgressStatus.Text = $"{baseRate}:{outputRate} {ColonyStructureData.RefiningResource} ({ColonyStructureData.RefiningResourcePurity})";
            }
        }

        private static int GetRefiningOutputRate(string purity, int baseRate)
        {
            switch (purity)
            {
                case "Low": return baseRate;       // 1x
                case "Medium": return baseRate * 3; // 3x
                case "High": return baseRate * 5;   // 5x
                default: return baseRate;
            }
        }

        /// <summary>
        /// Helper class for refinery resource selection combo box.
        /// </summary>
        private class RefinerySelectionItem
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public string ResourceName { get; set; }
            public string Purity { get; set; }
        }

        // -----------------------------------------------------------------------
        // Research Lab Controls
        // -----------------------------------------------------------------------

        private void handleResearchLabControls()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
                guard.release();
                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (ColonyStructureData.ProcessCompletionTime != null)
            {
                showCompletionTime = true;
                enableCmbSelection = false;
            }

            if (!string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
            {
                showCmdStart = true;
            }

            // Selection: researchable blueprints
            flpSelection.Visible = true;
            if (cmbSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
            {
                populateSelectionWithResearchableBlueprints();
                if (!string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
                {
                    cmbSelection.SelectedValue = ColonyStructureData.ResearchingBlueprintUUID;
                }
            }
            txtSelectionFilter.Enabled = enableCmbSelection;
            cmbSelection.Enabled = enableCmbSelection;
            cmdStart.Visible = showCmdStart && !showCompletionTime;

            flpSubSelection.Visible = false;

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
                populateResearchLabProgressStatus();
                if (timerCountdown.Enabled == false)
                {
                    timerCountdown.Interval = 1000;
                    timerCountdown.Start();
                }
            }
            else
            {
                flpCompletionTime.Visible = false;
                rtbProgressStatus.Text = "";
            }

            guard.release();
            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void populateSelectionWithResearchableBlueprints()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            var items = new List<ResearchSelectionItem>();

            foreach (Data.Blueprint bp in playerContext.blueprintList)
            {
                if (bp.UUID == null) continue;
                if (bp.Evolution >= 15) continue;
                if (!ResearchTimeLookup.CanResearchEvolution(bp.Evolution)) continue;

                bool canResearch = true;
                bp.Properties.getBoolean("CanResearch", true, out canResearch);
                if (!canResearch) continue;

                string display = bp.ExtendedName;
                if (!string.IsNullOrEmpty(searchText) &&
                    display.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                items.Add(new ResearchSelectionItem
                {
                    UUID = bp.UUID,
                    DisplayName = display
                });
            }

            items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, new ResearchSelectionItem { UUID = "", DisplayName = "" });

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "UUID";
            cmbSelection.DataSource = items;
            cmbSelection.SelectedIndex = -1;
        }

        private void populateResearchLabProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            Data.Blueprint bp = playerContext.findBlueprint(ColonyStructureData.ResearchingBlueprintUUID);
            if (bp == null)
            {
                rtbProgressStatus.Text = "";
                return;
            }

            rtbProgressStatus.Text = $"Evo {bp.Evolution}->{bp.Evolution + 1} {bp.Name}";
        }

        private class ResearchSelectionItem
        {
            public string UUID { get; set; }
            public string DisplayName { get; set; }
        }

        private void populateSelectionWithSurveys()
        {
            //cmbSelection.Items.Clear();
            string searchText = txtSelectionFilter.Text;
            if (searchText == null)
            {
                searchText = ""; 
            }

            List<Baseline.Survey> filteredList = new List<Baseline.Survey>(playerContext.surveyList);

            filteredList = filteredList
                .Where(item => string.Equals(item.PlanetName, Colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            filteredList = filteredList
                .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            filteredList.Insert(0, new Baseline.Survey());

            cmbSelection.DisplayMember = "ExtendedName";
            cmbSelection.ValueMember = "UUID";
            cmbSelection.DataSource = filteredList;
            cmbSelection.SelectedIndex = -1;
        }

        private void populateSubSelectionWithSurveyResources()
        {
            //cmbSubSelection.Items.Clear();
            string searchText = txtSubSelectionFilter.Text;
            if (searchText == null)
            {
                searchText = "";
            }

            Baseline.Survey survey = cmbSelection.SelectedItem as Baseline.Survey; 
            if (survey == null)
            {
                return;
            }

            List<SurveyResource> filteredList = survey.Resources.Values.ToList<SurveyResource>();

            filteredList = filteredList
                .Where(item => item.Resource.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            filteredList.Insert(0, new SurveyResource());

            cmbSubSelection.DisplayMember = "ExtendedName";
            cmbSubSelection.ValueMember = "Resource";
            cmbSubSelection.DataSource = filteredList;
            cmbSubSelection.SelectedIndex = -1;
        }

        private void populateStats()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            RtfBuilder builder = new RtfBuilder();

            if (ColonyStructureData != null)
            {
                builder.Append("#" + ColonyStructureData.gameSequence + " ", Color.Black);
            }
            if (FlatpackBlueprint != null)
            {
                builder.Append(FlatpackBlueprint.ExtendedName, Color.Black);

                builder.Append("\n", Color.Black);
            }

            if (ColonyStructureData != null && ColonyStructureData.Statuses != null)
            {
                ColonyStructureData.Statuses.TryGetValue("Actual", out ColonyStructureStatus status);
                if (status != null)
                {
                    ColonyStatusCalculator.populateStatus(builder, status);
                }
                ColonyStructureData.Statuses.TryGetValue("Ideal", out ColonyStructureStatus idealStatus);
                if (idealStatus != null)
                {
                    builder.Append("\n", Color.Black);
                    ColonyStatusCalculator.populateStatus(builder, idealStatus);
                }
            }

            /*
            // Power
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "\nPower: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "" + PowerRequired, Color.Red);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "" + PowerProvided, Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Habitation: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Food: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Entertainment: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Warehouse: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);
            */

            rtbStatus.Rtf = builder.ToRtf();
            guard.release();
        }

        private void rtbStatus_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            // Adjust the height of the RichTextBox to fit the new content rectangle
            // An offset (+10 in this example) may be needed to account for borders/margins
            rtbStatus.Height = e.NewRectangle.Height + 10;
            rtbStatus.Width = e.NewRectangle.Width + 10;
        }

        private void chkWorkDetail1_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string prop = chkWorkDetail1.Tag as string;
            ViewModel.SetWorkerAssigned(prop, chkWorkDetail1.Checked);
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail2_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string prop = chkWorkDetail2.Tag as string;
            ViewModel.SetWorkerAssigned(prop, chkWorkDetail2.Checked);
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail3_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string prop = chkWorkDetail3.Tag as string;
            ViewModel.SetWorkerAssigned(prop, chkWorkDetail3.Checked);
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkBuilt_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.IsBuilt = chkBuilt.Checked;
            if (chkBuilt.Checked) chkStaged.Checked = false;
            else chkOnline.Checked = false;
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkStaged_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.IsStaged = chkStaged.Checked;
            if (chkStaged.Checked) { chkBuilt.Checked = false; chkOnline.Checked = false; }
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkOnline_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.IsOnline = chkOnline.Checked;
            if (chkOnline.Checked) { chkBuilt.Checked = true; chkStaged.Checked = false; }
            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        public class ProgramaticUpdateGuard
        {
            private ColonyStructure _parent;
            private bool _hasLocked;

            public ProgramaticUpdateGuard(ColonyStructure parent)
            {
                _parent = parent;
                _parent._isProgrammaticUpdate++;
                _hasLocked = true;
            }
            public void release()
            {
                if (_hasLocked)
                {
                    _parent._isProgrammaticUpdate--;
                    _hasLocked = false;
                }
            }
            ~ProgramaticUpdateGuard()
            {
                release();
            }
        }

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveUp(Colony);
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.Delete(Colony);
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveDown(Colony);
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void txtSelectionFilter_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
            {
                if (string.IsNullOrEmpty(ColonyStructureData.RefiningResource)) return;

                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
                long secondsUntilNextHour = 3600 - (long)(DateTime.Now - DateTime.Now.Date.AddHours(DateTime.Now.Hour)).TotalSeconds;
                ColonyStructureData.ProcessCompletionTime.StartRepeating(3600, secondsUntilNextHour);
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleRefineryControls();
            }
            else if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
            {
                if (string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID)) return;

                Data.Blueprint bp = playerContext.findBlueprint(ColonyStructureData.ResearchingBlueprintUUID);
                if (bp == null) return;

                long researchSeconds = ResearchTimeLookup.GetResearchTimeSeconds(bp.Evolution);
                if (researchSeconds <= 0) return;

                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
                ColonyStructureData.ProcessCompletionTime.TimeRemaining = researchSeconds;
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleResearchLabControls();
            }
        }

        private void txtSubSelectionFilter_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmdSubStart_Click(object sender, EventArgs e)
        {
            ColonyStructureData.ProcessCompletionTime = new CountDownTime();
            ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
            long secondsUntilNextHour = 3600 - (long)(DateTime.Now - DateTime.Now.Date.AddHours(DateTime.Now.Hour)).TotalSeconds;
            ColonyStructureData.ProcessCompletionTime.StartRepeating(3600, secondsUntilNextHour);
            timerCountdown.Interval = 1000;
            timerCountdown.Start();
            handleMiningRigControls();
        }

        private void timerCountdown_Tick(object sender, EventArgs e)
        {
            if (!completionModification && ColonyStructureData.ProcessCompletionTime != null)
            {
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
            }
        }
        private void txtCompletionTime_Enter(object sender, EventArgs e)
        {
            completionModification = true;
        }

        private void txtCompletionTime_Leave(object sender, EventArgs e)
        {
            completionModification = false;

            if (ColonyStructureData.ProcessCompletionTime != null)
            {
                ColonyStructureData.ProcessCompletionTime.TimeRemainingString = txtCompletionTime.Text;
            }
        }

        private void cmdDone_Click(object sender, EventArgs e)
        {
            // Force completion so Done always processes
            if (ColonyStructureData.ProcessCompletionTime != null)
            {
                if (ColonyStructureData.ProcessCompletionTime.IsRepeating &&
                    ColonyStructureData.ProcessCompletionTime.IntervalsPassed == 0)
                {
                    // Repeating timer: advance StartTime back by one interval
                    ColonyStructureData.ProcessCompletionTime.StartTime =
                        DateTime.Now.AddSeconds(-ColonyStructureData.ProcessCompletionTime.RepeatIntervalSeconds);
                }
                else if (!ColonyStructureData.ProcessCompletionTime.IsRepeating &&
                         ColonyStructureData.ProcessCompletionTime.TimeRemaining > 0)
                {
                    // One-shot timer: set TimeRemaining to 0 so it's expired
                    ColonyStructureData.ProcessCompletionTime.TimeRemaining = 0;
                }
            }

            Colony.ProcessColony();

            timerCountdown.Stop();
            ColonyStructureData.ProcessCompletionTime = null;
            txtCompletionTime.Text = "";
            rtbProgressStatus.Text = "";

            if (FlatpackBlueprint != null)
            {
                if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                    handleMiningRigControls();
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                    handleRefineryControls();
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    handleResearchLabControls();
            }

            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void cmbSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (FlatpackBlueprint != null)
            {
                if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                {
                    string survey = cmbSelection.SelectedValue as string;
                    if (survey != ColonyStructureData.MiningSurvey)
                    {
                        ColonyStructureData.MiningLeftOvers = Decimal.Zero;
                        ColonyStructureData.MiningSurveyResource = null;
                    }
                    ColonyStructureData.MiningSurvey = survey;
                    handleMiningRigControls();
                }
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                {
                    string key = cmbSelection.SelectedValue as string;
                    if (!string.IsNullOrEmpty(key) && key.Contains("|"))
                    {
                        string[] parts = key.Split('|');
                        ColonyStructureData.RefiningResource = parts[0];
                        ColonyStructureData.RefiningResourcePurity = parts[1];
                    }
                    else
                    {
                        ColonyStructureData.RefiningResource = null;
                        ColonyStructureData.RefiningResourcePurity = null;
                    }
                    handleRefineryControls();
                }
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                {
                    string uuid = cmbSelection.SelectedValue as string;
                    ColonyStructureData.ResearchingBlueprintUUID = string.IsNullOrEmpty(uuid) ? null : uuid;
                    handleResearchLabControls();
                }
            }
        }

        private void cmbSubSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (FlatpackBlueprint != null)
            {
                if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                {
                    string surveyResource = cmbSubSelection.SelectedValue as string;
                    if (surveyResource != ColonyStructureData.MiningSurveyResource)
                    {
                        ColonyStructureData.MiningLeftOvers = Decimal.Zero;
                    }
                    ColonyStructureData.MiningSurveyResource = surveyResource;
                    handleMiningRigControls();
                }
            }
        }

        private void ColonyStructure_Layout(object sender, LayoutEventArgs e)
        {
            int width = this.Size.Width - flpMovement.Width - flpStructureDetails.Width;
            flpStructureDetails.Size = new Size(width, flpStructureDetails.Size.Height);

            int height = Math.Max(Math.Max(flpStructureDetails.Height+ flpStructureDetails.Margin.Vertical, flpMovement.Height + flpMovement.Margin.Vertical), flpStructureDetails.Height + flpStructureDetails.Margin.Vertical);
            if (this.Height != height)
            {
                this.Size = new Size(this.Size.Width, height);
            }
        }

        private void flpStructureDetails_Layout(object sender, LayoutEventArgs e)
        {
            int height = 0;
            if (rtbStatus.Visible)
            {
                height += rtbStatus.Size.Height + rtbStatus.Margin.Vertical;
            }
            if (flpStructureCommands.Visible)
            {
                height += flpStructureCommands.Size.Height + flpStructureCommands.Margin.Vertical;
            }
            flpStructureDetails.Size = new Size(flpStructureDetails.Size.Width, height);
        }

        private void flpStructureCommands_Layout(object sender, LayoutEventArgs e)
        {
            int height = 0;
            if (flpSelection.Visible)
            {
                height += flpSelection.Size.Height + flpSelection.Margin.Vertical;
            }
            if (flpSubSelection.Visible)
            {
                height += flpSubSelection.Size.Height + flpSubSelection.Margin.Vertical;
            }
            if (flpCompletionTime.Visible)
            {
                height += flpCompletionTime.Size.Height + flpCompletionTime.Margin.Vertical;
            }
            flpStructureCommands.Size = new Size(flpStructureCommands.Size.Width, height);
        }
    }
}