using NLog;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
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

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class ColonyStructure : UserControl, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;
        public Models.Colony Colony { get; set; }

        private Models.ColonyStructure _colonyStructureData;
        public Models.ColonyStructure ColonyStructureData
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

        private Models.Blueprint FlatpackBlueprint { get; set; }

        public bool completionModification = false;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when colony structure state changes")]
        public event EventHandler ColonyStructureDataChanged;
        public ColonyStructure()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            this.SuspendLayout();
            PopulateStats();
            this.ResumeLayout();
        }

        public void UpdateData()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            this.SuspendLayout();
            FlatpackBlueprint = playerContext.FindBlueprint(ColonyStructureData.FlatpackBlueprintUUID);
            chkStageResources.Visible = false;

            // Building state â€” structure is transitioning from staged to built
            if (ColonyStructureData.BuildCompletionTime != null &&
                ColonyStructureData.BuildCompletionTime.TimeRemaining > 0)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.BuildCompletionTime.TimeRemainingString;
                rtbProgressStatus.Text = "Building...";
                cmdDone.Visible = true;
                cmdStart.Visible = false;
                chkBuilt.Enabled = false;
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = 1000;
                    timerCountdown.Start();
                }
                flpStructureCommands_Layout(null, null);
                flpStructureDetails_Layout(null, null);
                ColonyStructure_Layout(null, null);
                goto SkipBlueprintHandlers;
            }

            if (FlatpackBlueprint != null)
            {
                decimal powerProvided = 0;
                FlatpackBlueprint.Properties.getDecimal("PowerProvided", 0, out powerProvided);
                decimal powerRequired = 0;
                FlatpackBlueprint.Properties.getDecimal("PowerRequired", 0, out powerRequired);

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
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory)
                {
                    handleManufactoryControls();
                }
                else if (FlatpackBlueprint.BluePrintType.IsCommodityFactory())
                {
                    handleCommodityFactoryControls();
                }
                else {
                    flpSelection.Visible = false;
                    flpManufacturingControls.Visible = false;
                    flpSubSelection.Visible = false;
                    flpCompletionTime.Visible = false;
                }
            }

            SkipBlueprintHandlers:

            // Build button visibility for staged structures
            if (ViewModel != null && ViewModel.IsStaged && !ViewModel.IsBuilt)
            {
                bool siblingBuilding = Colony != null && Colony.Structures.Any(s =>
                    s != ColonyStructureData &&
                    s.BuildCompletionTime != null &&
                    s.BuildCompletionTime.TimeRemaining > 0);
                if (!siblingBuilding)
                {
                    cmdStart.Text = "Build";
                    cmdStart.Visible = true;
                    // Show parent panels but hide sibling controls
                    flpSelection.Visible = true;
                    lblSelection.Visible = false;
                    txtSelectionFilter.Visible = false;
                    cmbSelection.Visible = false;
                    flpManufacturingControls.Visible = true;
                    chkStageResources.Visible = false;
                    txtQuantity.Visible = false;
                    flpSubSelection.Visible = false;
                    flpCompletionTime.Visible = false;
                    flpStructureCommands_Layout(null, null);
                    flpStructureDetails_Layout(null, null);
                    ColonyStructure_Layout(null, null);
                }
            }

            chkBuilt.Checked = false;
            chkBuilt.Enabled = true;
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

                // --- Assigned Workers ---
                foreach (var wt in Models.WorkerDetail.WorkerTypes)
                {
                    int index = 1;
                    string key = wt.WorkerPrefix + index;
                    while (ViewModel.WorkerKeyExists(key))
                    {
                        checkControls[controlIndex].Visible = true;
                        checkControls[controlIndex].Enabled = true;
                        checkControls[controlIndex].Text = wt.DisplayName;
                        checkControls[controlIndex].Tag = key;
                        checkControls[controlIndex].Checked = ViewModel.GetWorkerAssigned(key);
                        controlIndex++;
                        index++;
                        key = wt.WorkerPrefix + index;
                    }
                }

                // --- Unallocated Workers ---
                if (FlatpackBlueprint != null)
                {
                    foreach (var wt in Models.WorkerDetail.WorkerTypes)
                    {
                        if (FlatpackBlueprint.Properties.ContainsKey(wt.UnassignedKey))
                        {
                            bool available = IsUnallocatedWorkerAvailable(wt.DetailKey);
                            checkControls[controlIndex].Visible = true;
                            checkControls[controlIndex].Enabled = false;
                            checkControls[controlIndex].Text = "Support - " + wt.DisplayName;
                            checkControls[controlIndex].Tag = wt.UnassignedKey;
                            checkControls[controlIndex].Checked = available;
                            controlIndex++;
                        }
                    }
                }

                bool hasAllWorkers = true;
                for (int index = 0; index < controlIndex; index++) {
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

            PopulateStats();

            this.ResumeLayout();
        }

        /// <summary>
        /// Lightweight update that only recalculates the background color based on
        /// current worker assignments and build/online state. Used by worker checkbox
        /// handlers to avoid a full UpdateData repaint.
        /// </summary>
        private void UpdateBackgroundColor()
        {
            if (ColonyStructureData == null || ViewModel == null) return;

            CheckBox[] checkControls = { chkWorkDetail1, chkWorkDetail2, chkWorkDetail3 };
            bool hasAllWorkers = true;
            foreach (var chk in checkControls)
            {
                if (chk.Visible && !chk.Checked)
                {
                    hasAllWorkers = false;
                    break;
                }
            }

            if (ViewModel.IsStaged)
                flpColonyStructure.BackColor = Color.Yellow;
            else if (ViewModel.IsBuilt)
            {
                if (!ViewModel.IsOnline)
                    flpColonyStructure.BackColor = Color.PaleVioletRed;
                else
                    flpColonyStructure.BackColor = hasAllWorkers ? Color.Green : Color.LightGreen;
            }
            else
                flpColonyStructure.BackColor = Color.White;
        }

        private void handleMiningRigControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
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
                flpManufacturingControls.Visible = false;
                txtQuantity.Visible = false;
                if (cmbSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.MiningSurvey))
                {
                    PopulateSelectionWithSurveys();
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
                flpManufacturingControls.Visible = false;
            }

            if ( showSubSelection)
            {
                flpSubSelection.Visible = true;
                if (cmbSubSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource))
                {
                    PopulateSubSelectionWithSurveyResources();
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
                PopulateProgressStatus();

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

            // TODO: FIXME: Changing visibilty isn't triggering a layout call.
            // flpStructureCommands.PerformLayout(); - Does NOT work.
            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void PopulateProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.MiningSurvey) ||
                string.IsNullOrEmpty(ColonyStructureData.MiningSurveyResource))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            Models.Survey survey = playerContext.FindSurvey(ColonyStructureData.MiningSurvey);
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
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
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
            flpManufacturingControls.Visible = false;
            txtQuantity.Visible = false;
            PopulateSelectionWithUnrefinedResources();
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
            txtSelectionFilter.Enabled = enableCmbSelection;
            cmbSelection.Enabled = enableCmbSelection;
            cmdStart.Visible = showCmdStart && !showCompletionTime;

            // No sub-selection for refinery
            flpSubSelection.Visible = false;

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
                PopulateRefineryProgressStatus();
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

            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void PopulateSelectionWithUnrefinedResources()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            var unrefinedItems = new List<RefinerySelectionItem>();

            // Add unrefined resources from warehouse
            foreach (var itemEntry in Colony.Items.Items.Values)
            {
                if (itemEntry.ItemType == Models.ItemType.ItemTypeEnum.Resource &&
                    !string.IsNullOrEmpty(itemEntry.ResourcePurity) &&
                    itemEntry.ResourcePurity != GameConstants.PurityRefined)
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
                        Models.Survey survey = playerContext.FindSurvey(structure.MiningSurvey);
                        if (survey != null && survey.Resources.ContainsKey(structure.MiningSurveyResource))
                        {
                            SurveyResource sr = survey.Resources[structure.MiningSurveyResource];
                            if (!string.IsNullOrEmpty(sr.Purity) && sr.Purity != GameConstants.PurityRefined)
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

                // Minimum: enough for at least 1 unit of output
                int perUnitCost = recipe.ConsumeRate / recipe.ProduceRate;
                if (totalAvailable >= perUnitCost)
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

            cmbSelection.DataSource = null;

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "Key";
            cmbSelection.DataSource = unrefinedItems;
            cmbSelection.SelectedIndex = -1;
        }

        private void PopulateRefineryProgressStatus()
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
                int baseRate = GameConstants.RefiningBaseRate;
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
        /// Normalizes time strings from blueprint properties to the format expected by
        /// CountDownTime.TimeRemainingString (e.g. "9 hours" -> "9h", "30 minutes" -> "30m").
        /// </summary>
        private static string NormalizeTimeString(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return timeStr;
            timeStr = System.Text.RegularExpressions.Regex.Replace(timeStr, @"\s*hours?\s*", "h ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            timeStr = System.Text.RegularExpressions.Regex.Replace(timeStr, @"\s*minutes?\s*", "m ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            timeStr = System.Text.RegularExpressions.Regex.Replace(timeStr, @"\s*seconds?\s*", "s ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            timeStr = System.Text.RegularExpressions.Regex.Replace(timeStr, @"\s*days?\s*", "d ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return timeStr.Trim();
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
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
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
            flpManufacturingControls.Visible = false;
            txtQuantity.Visible = false;
            if (cmbSelection.Items.Count <= 1 || !string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
            {
                PopulateSelectionWithResearchableBlueprints();
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
                PopulateResearchLabProgressStatus();
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

            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void PopulateSelectionWithResearchableBlueprints()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            var items = new List<ResearchSelectionItem>();

            foreach (Models.Blueprint bp in playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                if (bp.Evolution >= 15) continue;
                if (!ResearchTimeLookup.CanResearchEvolution(bp.Evolution)) continue;

                bool canResearch = true;
                bp.Properties.getBoolean("Can Research", true, out canResearch);
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

            cmbSelection.DataSource = null;

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "UUID";
            cmbSelection.DataSource = items;
            cmbSelection.SelectedIndex = -1;
        }

        private void PopulateResearchLabProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            Models.Blueprint bp = playerContext.FindBlueprint(ColonyStructureData.ResearchingBlueprintUUID);
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

        // -----------------------------------------------------------------------
        // Manufactory Controls
        // -----------------------------------------------------------------------

        private void handleManufactoryControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
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

            if (!string.IsNullOrEmpty(ColonyStructureData.ManufacturingBlueprintUUID))
            {
                showCmdStart = true;
            }

            // Selection: manufacturable blueprints
            flpSelection.Visible = true;
            flpManufacturingControls.Visible = true;
            lblSelection.Visible = true;
            txtSelectionFilter.Visible = true;
            cmbSelection.Visible = true;
            PopulateSelectionWithManufacturableBlueprints();
            if (!string.IsNullOrEmpty(ColonyStructureData.ManufacturingBlueprintUUID))
            {
                cmbSelection.SelectedValue = ColonyStructureData.ManufacturingBlueprintUUID;
            }
            txtSelectionFilter.Enabled = enableCmbSelection;
            cmbSelection.Enabled = enableCmbSelection;

            // Sub-selection: not used for manufactory
            flpSubSelection.Visible = false;

            // Quantity input â€” only visible when a blueprint is selected
            txtQuantity.Visible = showCmdStart;
            txtQuantity.Enabled = !showCompletionTime;
            if (ColonyStructureData.ManufacturingQuantity > 0)
            {
                txtQuantity.Text = ColonyStructureData.ManufacturingQuantity.ToString();
            }
            else if (string.IsNullOrEmpty(txtQuantity.Text) || !int.TryParse(txtQuantity.Text, out _))
            {
                txtQuantity.Text = "1";
            }
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdSubStart.Visible = false;

            // Stage Resources checkbox: visible for Manufactory, hidden when manufacturing running
            if (showCompletionTime)
            {
                chkStageResources.Visible = false;
            }
            else
            {
                chkStageResources.Visible = true;
                int mfgQty = 0;
                int.TryParse(txtQuantity.Text, out mfgQty);
                chkStageResources.Enabled = !string.IsNullOrEmpty(ColonyStructureData.ManufacturingBlueprintUUID) && mfgQty > 0;
                chkStageResources.Checked = ViewModel.StagingResources;
            }

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
                PopulateManufactoryProgressStatus();
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

            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void PopulateSelectionWithManufacturableBlueprints()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            var items = new List<ResearchSelectionItem>();

            foreach (Models.Blueprint bp in playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;

                bool canManufacture = true;
                bp.Properties.getBoolean("Can Manufacture", true, out canManufacture);
                if (!canManufacture) continue;

                // Must have a ManufactureTime
                string mfgTime;
                bp.Properties.getString("Manufacture Run Time", null, out mfgTime);
                if (string.IsNullOrEmpty(mfgTime)) continue;

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

            cmbSelection.DataSource = null;

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "UUID";
            cmbSelection.DataSource = items;
            cmbSelection.SelectedIndex = -1;
        }

        private void PopulateManufactoryProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.ManufacturingBlueprintUUID))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            Models.Blueprint bp = playerContext.FindBlueprint(ColonyStructureData.ManufacturingBlueprintUUID);
            if (bp == null)
            {
                rtbProgressStatus.Text = "";
                return;
            }

            int displayProgress = Math.Min(ColonyStructureData.ManufacturingCompleted + 1, ColonyStructureData.ManufacturingQuantity);
            rtbProgressStatus.Text = $"({displayProgress}/{ColonyStructureData.ManufacturingQuantity}) {bp.ExtendedName}";
        }

        // -----------------------------------------------------------------------
        // Commodity Factory Controls
        // -----------------------------------------------------------------------

        private void handleCommodityFactoryControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSelection.Visible = false;
                flpManufacturingControls.Visible = false;
                flpSubSelection.Visible = false;
                flpCompletionTime.Visible = false;
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

            if (!string.IsNullOrEmpty(ColonyStructureData.ManufacturingCommodityName))
            {
                showCmdStart = true;
            }

            // Selection: commodities filtered by CommodityIndustry
            flpSelection.Visible = true;
            flpManufacturingControls.Visible = true;
            lblSelection.Visible = true;
            txtSelectionFilter.Visible = true;
            cmbSelection.Visible = true;
            PopulateSelectionWithCommodities();
            if (!string.IsNullOrEmpty(ColonyStructureData.ManufacturingCommodityName))
            {
                cmbSelection.SelectedValue = ColonyStructureData.ManufacturingCommodityName;
            }
            txtSelectionFilter.Enabled = enableCmbSelection;
            cmbSelection.Enabled = enableCmbSelection;

            flpSubSelection.Visible = false;

            // Quantity input (number of cycles)
            txtQuantity.Visible = showCmdStart;
            txtQuantity.Enabled = !showCompletionTime;
            if (ColonyStructureData.ManufacturingQuantity > 0)
            {
                txtQuantity.Text = ColonyStructureData.ManufacturingQuantity.ToString();
            }
            else if (string.IsNullOrEmpty(txtQuantity.Text) || !int.TryParse(txtQuantity.Text, out _))
            {
                txtQuantity.Text = "1";
            }
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdSubStart.Visible = false;

            // Stage Resources checkbox: visible for CommodityFactory, hidden when manufacturing running
            if (showCompletionTime)
            {
                chkStageResources.Visible = false;
            }
            else
            {
                chkStageResources.Visible = true;
                int mfgQty = 0;
                int.TryParse(txtQuantity.Text, out mfgQty);
                chkStageResources.Enabled = !string.IsNullOrEmpty(ColonyStructureData.ManufacturingCommodityName) && mfgQty > 0;
                chkStageResources.Checked = ViewModel.StagingResources;
            }

            if (showCompletionTime)
            {
                flpCompletionTime.Visible = true;
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
                PopulateCommodityFactoryProgressStatus();
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

            flpStructureCommands_Layout(null, null);
            flpStructureDetails_Layout(null, null);
            ColonyStructure_Layout(null, null);
        }

        private void PopulateSelectionWithCommodities()
        {
            string searchText = txtSelectionFilter.Text ?? "";

            // Get the CommodityIndustry from the flatpack blueprint
            string industryFilter = "";
            if (FlatpackBlueprint != null)
            {
                FlatpackBlueprint.Properties.getString("Commodity Industry", "", out industryFilter);
            }

            var items = new List<CommoditySelectionItem>();
            foreach (var commodity in Models.Commodity.Commodities)
            {
                if (string.IsNullOrEmpty(commodity.Name)) continue;

                // Filter by CommodityIndustry if set
                if (!string.IsNullOrEmpty(industryFilter))
                {
                    var industry = Models.CommodityIndustry.CommodityIndustryMapByEnum.ContainsKey(commodity.CommodityIndustry)
                        ? Models.CommodityIndustry.CommodityIndustryMapByEnum[commodity.CommodityIndustry]
                        : null;
                    if (industry == null || industry.Name != industryFilter) continue;
                }

                string display = commodity.ExtendedName;
                if (!string.IsNullOrEmpty(searchText) &&
                    display.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                items.Add(new CommoditySelectionItem
                {
                    Name = commodity.Name,
                    DisplayName = display
                });
            }

            items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, new CommoditySelectionItem { Name = "", DisplayName = "" });

            cmbSelection.DataSource = null;

            cmbSelection.DisplayMember = "DisplayName";
            cmbSelection.ValueMember = "Name";
            cmbSelection.DataSource = items;
            cmbSelection.SelectedIndex = -1;
        }

        private void PopulateCommodityFactoryProgressStatus()
        {
            if (ColonyStructureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(ColonyStructureData.ManufacturingCommodityName))
            {
                rtbProgressStatus.Text = "";
                return;
            }

            int displayProgress = Math.Min(ColonyStructureData.ManufacturingCompleted + 1, ColonyStructureData.ManufacturingQuantity);
            rtbProgressStatus.Text = $"({displayProgress}/{ColonyStructureData.ManufacturingQuantity}) {ColonyStructureData.ManufacturingCommodityName} x{GameConstants.CommoditiesPerCycle}";
        }

        private class CommoditySelectionItem
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }

        private void PopulateSelectionWithSurveys()
        {
            //cmbSelection.Items.Clear();
            string searchText = txtSelectionFilter.Text;
            if (searchText == null)
            {
                searchText = ""; 
            }

            List<Models.Survey> filteredList = new List<Models.Survey>(playerContext.SurveyList);

            filteredList = filteredList
                .Where(item => string.Equals(item.PlanetName, Colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            filteredList = filteredList
                .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            filteredList.Insert(0, new Models.Survey());

            cmbSelection.DataSource = null;

            cmbSelection.DisplayMember = "ExtendedName";
            cmbSelection.ValueMember = "UUID";
            cmbSelection.DataSource = filteredList;
            cmbSelection.SelectedIndex = -1;
        }

        private void PopulateSubSelectionWithSurveyResources()
        {
            //cmbSubSelection.Items.Clear();
            string searchText = txtSubSelectionFilter.Text;
            if (searchText == null)
            {
                searchText = "";
            }

            Models.Survey survey = cmbSelection.SelectedItem as Models.Survey; 
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

        private bool IsUnallocatedWorkerAvailable(string workerDetailID)
        {
            if (ColonyStructureData == null) return false;

            // Check this structure's actual status â€” the calculator determined availability
            // during its pass with locks cleared, so it's the authoritative answer
            ColonyStructureStatus status;
            if (ColonyStructureData.Statuses.TryGetValue(GameConstants.StatusActual, out status))
            {
                switch (workerDetailID)
                {
                    case "BlueCollarDetail": return status.UnallocatedBlueCollarPresent;
                    case "WhiteCollarDetail": return status.UnallocatedWhiteCollarPresent;
                    case "SpecialistDetail": return status.UnallocatedSpecialistPresent;
                }
            }

            return false;
        }

        private void PopulateStats()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            RtfBuilder builder = new RtfBuilder();

            if (ColonyStructureData != null)
            {
                builder.Append("#" + ColonyStructureData.displaySequence + " ", Color.Black);
            }
            if (FlatpackBlueprint != null)
            {
                builder.Append(FlatpackBlueprint.ExtendedName, Color.Black);

                builder.Append("\n", Color.Black);
            }

            if (ColonyStructureData != null && ColonyStructureData.Statuses != null)
            {
                ColonyStructureData.Statuses.TryGetValue(GameConstants.StatusActual, out ColonyStructureStatus status);
                if (status != null)
                {
                    ColonyStatusCalculator.PopulateStatus(builder, status);
                }
                ColonyStructureData.Statuses.TryGetValue(GameConstants.StatusIdeal, out ColonyStructureStatus idealStatus);
                if (idealStatus != null)
                {
                    builder.Append("\n", Color.Black);
                    ColonyStatusCalculator.PopulateStatus(builder, idealStatus);
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
            UpdateBackgroundColor();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail2_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string prop = chkWorkDetail2.Tag as string;
            ViewModel.SetWorkerAssigned(prop, chkWorkDetail2.Checked);
            UpdateBackgroundColor();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail3_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string prop = chkWorkDetail3.Tag as string;
            ViewModel.SetWorkerAssigned(prop, chkWorkDetail3.Checked);
            UpdateBackgroundColor();
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

        private void chkStageResources_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.StagingResources = chkStageResources.Checked;

            // Persist the quantity from the UI to the data model when staging
            if (chkStageResources.Checked)
            {
                int qty = 1;
                int.TryParse(txtQuantity.Text, out qty);
                if (qty <= 0) qty = 1;
                ColonyStructureData.ManufacturingQuantity = qty;
            }

            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void txtQuantity_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int qty = 0;
            int.TryParse(txtQuantity.Text, out qty);
            if (qty > 0)
                ColonyStructureData.ManufacturingQuantity = qty;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveUp(Colony);
            ColonyStructureDataChanged?.Invoke(null, e);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.Delete(Colony);
            ColonyStructureDataChanged?.Invoke(null, e);
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveDown(Colony);
            ColonyStructureDataChanged?.Invoke(null, e);
        }

        private void txtSelectionFilter_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            // Handle Build button for staged structures
            if (ViewModel != null && ViewModel.IsStaged && !ViewModel.IsBuilt && cmdStart.Text == "Build")
            {
                // Check single-build constraint
                if (Colony != null && Colony.Structures.Any(s =>
                    s != ColonyStructureData &&
                    s.BuildCompletionTime != null &&
                    s.BuildCompletionTime.TimeRemaining > 0))
                    return;

                // Look up Builder skill level
                int builderLevel = 0;
                if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
                {
                    var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                    if (owner != null)
                        builderLevel = owner.GetSkill(SkillName.Builder).Level;
                }

                long buildSeconds = BuildTimeCalculator.Calculate(builderLevel);

                ViewModel.IsStaged = false;
                ColonyStructureData.BuildCompletionTime = new CountDownTime();
                ColonyStructureData.BuildCompletionTime.TimeRemaining = buildSeconds;

                ColonyStructureDataChanged?.Invoke(this, e);
                return;
            }

            if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
            {
                if (string.IsNullOrEmpty(ColonyStructureData.RefiningResource)) return;

                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
                long secondsUntilNextHour = GameConstants.SecondsPerHour - (long)(DateTime.Now - DateTime.Now.Date.AddHours(DateTime.Now.Hour)).TotalSeconds;
                ColonyStructureData.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleRefineryControls();
                ColonyStructureDataChanged?.Invoke(this, e);
            }
            else if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
            {
                if (string.IsNullOrEmpty(ColonyStructureData.ResearchingBlueprintUUID)) return;

                Models.Blueprint bp = playerContext.FindBlueprint(ColonyStructureData.ResearchingBlueprintUUID);
                if (bp == null) return;

                long researchSeconds = ResearchTimeLookup.GetResearchTimeSeconds(bp.Evolution);
                if (researchSeconds <= 0) return;

                // Apply ResearchFocus skill multiplier
                int researchFocusLevel = 0;
                if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
                {
                    var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                    if (owner != null)
                        researchFocusLevel = owner.GetSkill(SkillName.ResearchFocus).Level;
                }
                researchSeconds = Math.Max(1, (long)(researchSeconds * (1.0 - researchFocusLevel * 0.03)));

                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
                ColonyStructureData.ProcessCompletionTime.TimeRemaining = researchSeconds;
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleResearchLabControls();
                ColonyStructureDataChanged?.Invoke(this, e);
            }
            else if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory)
            {
                if (string.IsNullOrEmpty(ColonyStructureData.ManufacturingBlueprintUUID)) return;

                Models.Blueprint bp = playerContext.FindBlueprint(ColonyStructureData.ManufacturingBlueprintUUID);
                if (bp == null) return;

                // Parse manufacture time from blueprint properties
                string mfgTimeStr;
                bp.Properties.getString("Manufacture Run Time", null, out mfgTimeStr);
                if (string.IsNullOrEmpty(mfgTimeStr)) return;

                // Normalize time format: "9 hours" -> "9h", "30 minutes" -> "30m", etc.
                mfgTimeStr = NormalizeTimeString(mfgTimeStr);

                // Parse using CountDownTime's TimeRemainingString parser
                CountDownTime tempTimer = new CountDownTime();
                tempTimer.TimeRemainingString = mfgTimeStr;
                long mfgSeconds = tempTimer.TimeRemaining;
                if (mfgSeconds <= 0) return;

                // Apply ProductionFocus skill multiplier
                int productionFocusLevel = 0;
                if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
                {
                    var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                    if (owner != null)
                        productionFocusLevel = owner.GetSkill(SkillName.ProductionFocus).Level;
                }
                mfgSeconds = Math.Max(1, (long)(mfgSeconds * (1.0 - productionFocusLevel * 0.03)));

                // Parse quantity from txtQuantity
                int qty = 1;
                int.TryParse(txtQuantity.Text, out qty);
                if (qty <= 0) qty = 1;

                ColonyStructureData.ManufacturingQuantity = qty;
                ColonyStructureData.ManufacturingCompleted = 0;
                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
                ColonyStructureData.ProcessCompletionTime.StartRepeating(mfgSeconds);
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleManufactoryControls();
                ColonyStructureDataChanged?.Invoke(this, e);
            }
            else if (FlatpackBlueprint != null && FlatpackBlueprint.BluePrintType.IsCommodityFactory())
            {
                if (string.IsNullOrEmpty(ColonyStructureData.ManufacturingCommodityName)) return;

                // Parse quantity (number of cycles) from txtQuantity
                int qty = 1;
                int.TryParse(txtQuantity.Text, out qty);
                if (qty <= 0) qty = 1;

                ColonyStructureData.ManufacturingQuantity = qty;
                ColonyStructureData.ManufacturingCompleted = 0;
                ColonyStructureData.ProcessCompletionTime = new CountDownTime();
                ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;

                // Apply ProductionFocus skill multiplier to commodity cycle time
                long commodityCycleSeconds = GameConstants.CommodityCycleSeconds;
                int commProductionFocusLevel = 0;
                if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
                {
                    var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                    if (owner != null)
                        commProductionFocusLevel = owner.GetSkill(SkillName.ProductionFocus).Level;
                }
                commodityCycleSeconds = Math.Max(1, (long)(commodityCycleSeconds * (1.0 - commProductionFocusLevel * 0.03)));

                ColonyStructureData.ProcessCompletionTime.StartRepeating(commodityCycleSeconds);
                timerCountdown.Interval = 1000;
                timerCountdown.Start();
                handleCommodityFactoryControls();
                ColonyStructureDataChanged?.Invoke(this, e);
            }
        }

        private void txtSubSelectionFilter_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmdSubStart_Click(object sender, EventArgs e)
        {
            ColonyStructureData.ProcessCompletionTime = new CountDownTime();
            ColonyStructureData.ProcessCompletionTime.StartTime = DateTime.Now;
            long secondsUntilNextHour = GameConstants.SecondsPerHour - (long)(DateTime.Now - DateTime.Now.Date.AddHours(DateTime.Now.Hour)).TotalSeconds;
            ColonyStructureData.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);
            timerCountdown.Interval = 1000;
            timerCountdown.Start();
            handleMiningRigControls();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void timerCountdown_Tick(object sender, EventArgs e)
        {
            if (!completionModification && ColonyStructureData.ProcessCompletionTime != null)
            {
                txtCompletionTime.Text = ColonyStructureData.ProcessCompletionTime.TimeRemainingString;
            }
            else if (!completionModification && ColonyStructureData.BuildCompletionTime != null)
            {
                txtCompletionTime.Text = ColonyStructureData.BuildCompletionTime.TimeRemainingString;
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
            else if (ColonyStructureData.BuildCompletionTime != null)
            {
                ColonyStructureData.BuildCompletionTime.TimeRemainingString = txtCompletionTime.Text;
            }

            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void cmdDone_Click(object sender, EventArgs e)
        {
            // Handle Build completion (BuildCompletionTime)
            if (ColonyStructureData.BuildCompletionTime != null)
            {
                lock (Colony.ProcessingLock)
                {
                    if (ColonyStructureData.BuildCompletionTime.TimeRemaining > 0)
                    {
                        ColonyStructureData.BuildCompletionTime.TimeRemaining = 0;
                    }
                    Colony.ProcessColony();
                }
                timerCountdown.Stop();
                txtCompletionTime.Text = "";
                rtbProgressStatus.Text = "";
                UpdateData();
                ColonyStructureDataChanged?.Invoke(this, e);
                return;
            }

            lock (Colony.ProcessingLock)
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
            }

            // For manufactories/commodity factories with remaining cycles, keep the timer running
            bool keepTimer = false;
            if (FlatpackBlueprint != null &&
                FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory &&
                ColonyStructureData.ManufacturingCompleted < ColonyStructureData.ManufacturingQuantity)
            {
                keepTimer = true;
            }
            if (FlatpackBlueprint != null &&
                FlatpackBlueprint.BluePrintType.IsCommodityFactory() &&
                ColonyStructureData.ManufacturingCompleted < ColonyStructureData.ManufacturingQuantity)
            {
                keepTimer = true;
            }

            if (!keepTimer)
            {
                timerCountdown.Stop();
                ColonyStructureData.ProcessCompletionTime = null;
                txtCompletionTime.Text = "";
                rtbProgressStatus.Text = "";
            }

            if (FlatpackBlueprint != null)
            {
                if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                    handleMiningRigControls();
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                    handleRefineryControls();
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    handleResearchLabControls();
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory)
                    handleManufactoryControls();
                else if (FlatpackBlueprint.BluePrintType.IsCommodityFactory())
                    handleCommodityFactoryControls();
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
                else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory)
                {
                    string uuid = cmbSelection.SelectedValue as string;
                    ColonyStructureData.ManufacturingBlueprintUUID = string.IsNullOrEmpty(uuid) ? null : uuid;
                    ColonyStructureData.ManufacturingCompleted = 0;
                    handleManufactoryControls();
                }
                else if (FlatpackBlueprint.BluePrintType.IsCommodityFactory())
                {
                    string name = cmbSelection.SelectedValue as string;
                    ColonyStructureData.ManufacturingCommodityName = string.IsNullOrEmpty(name) ? null : name;
                    ColonyStructureData.ManufacturingCompleted = 0;
                    handleCommodityFactoryControls();
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

            int height = Math.Max(
                Math.Max(
                    flpStructureDetails.Height + flpStructureDetails.Margin.Vertical,
                    flpMovement.Height + flpMovement.Margin.Vertical),
                flpStructureStatus.Height + flpStructureStatus.Margin.Vertical);
            // Account for flpColonyStructure border (FixedSingle = 2px)
            height += 2;
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
                // Adjust flpSelection height based on whether manufacturing controls row is visible
                int selHeight = 29;
                if (flpManufacturingControls.Visible)
                    selHeight = 58;
                flpSelection.Size = new Size(flpSelection.Size.Width, selHeight);

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
