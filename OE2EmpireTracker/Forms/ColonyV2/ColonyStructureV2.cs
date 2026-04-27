using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.ColonyV2
{
    /// <summary>
    /// Poolable UserControl for displaying a single colony structure.
    /// Designed for create-once, Reset+UpdateData on reuse from Structure_Pool.
    /// </summary>
    public partial class ColonyStructureV2 : UserControl, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>Player context for looking up surveys, blueprints, profiles.</summary>
        private readonly PlayerContext _playerContext;

        /// <summary>Pre-created worker checkboxes: 0-2 assigned, 3-5 unallocated.</summary>
        private readonly CheckBox[] _workerCheckboxes;

        private int _isProgrammaticUpdate = 0;

        /// <summary>Parallel list of Survey objects matching cmbSurvey display items, for UUID lookup via SelectedFullIndex.</summary>
        private List<Models.Survey> _surveyItems = new List<Models.Survey>();

        /// <summary>Parallel list of value strings matching cmbSelection display items, for key/UUID lookup via SelectedFullIndex.</summary>
        private List<string> _selectionValues = new List<string>();

        /// <summary>True while the user is manually editing txtCompletionTime.</summary>
        private bool _completionModification = false;

        /// <summary>Cached blueprint reference, set during UpdateData.</summary>
        private Models.Blueprint _blueprint;

        public ColonyStructureV2()
        {
            InitializeComponent();
            _playerContext = EmpireContext.PlayerContext;
            _workerCheckboxes = new CheckBox[]
            {
                chkWorker1, chkWorker2, chkWorker3,
                chkWorker4, chkWorker5, chkWorker6
            };

            // Wire event handlers for survey/selection/sub-selection combos and filters
            cmbSurvey.SelectedItemChanged += CmbSurvey_SelectedItemChanged;
            cmbSelection.SelectedItemChanged += CmbSelection_SelectedItemChanged;
            cmdStart.Click += CmdStart_Click;
            cmdDone.Click += CmdDone_Click;
            txtCompletionTime.Enter += TxtCompletionTime_Enter;
            txtCompletionTime.Leave += TxtCompletionTime_Leave;
            chkStageResources.CheckedChanged += ChkStageResources_CheckedChanged;
            txtQuantity.TextChanged += TxtQuantity_TextChanged;
        }

        /// <summary>
        /// Fired when structure data changes. IsStructural=true for add/reorder/delete,
        /// false for worker toggle or state change.
        /// </summary>
        public event EventHandler<ColonyStructureDataChangedEventArgs> ColonyStructureDataChanged;

        /// <summary>The ViewModel wrapping the current ColonyStructure data.</summary>
        public ColonyStructureViewModel ViewModel { get; set; }

        /// <summary>The parent colony that owns this structure.</summary>
        public Models.Colony Colony { get; set; }

        /// <summary>Whether the countdown timer is currently running.</summary>
        public bool TimerRunning => timerCountdown.Enabled;

        /// <summary>Stops the countdown timer without a full Reset.</summary>
        public void StopTimer() { if (timerCountdown.Enabled) timerCountdown.Stop(); }

        // -----------------------------------------------------------------------
        // IProgrammaticUpdateSource
        // -----------------------------------------------------------------------

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // -----------------------------------------------------------------------
        // 7.2: Reset() â€” pool reuse
        // -----------------------------------------------------------------------

        /// <summary>
        /// Resets the control for pool reuse. Clears all fields, hides optional panels,
        /// detaches ViewModel and Colony references, stops timer.
        /// </summary>
        public void Reset()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            // Stop timer
            if (timerCountdown.Enabled)
                timerCountdown.Stop();

            // Clear text fields
            lblName.Text = string.Empty;
            rtbStatus.Text = string.Empty;
            txtCompletionTime.Text = string.Empty;
            rtbProgressStatus.Text = string.Empty;

            // Clear combo data
            cmbSurvey.SetItems(new List<string>(), null);
            _surveyItems.Clear();
            cmbSelection.SetItems(new List<string>(), null);
            _selectionValues.Clear();

            // Uncheck all state checkboxes
            chkStaged.Checked = false;
            chkBuilt.Checked = false;
            chkOnline.Checked = false;

            // Hide and reset all worker checkboxes
            for (int i = 0; i < _workerCheckboxes.Length; i++)
            {
                _workerCheckboxes[i].Visible = false;
                _workerCheckboxes[i].Checked = false;
                _workerCheckboxes[i].Enabled = true;
                _workerCheckboxes[i].Text = string.Empty;
                _workerCheckboxes[i].Tag = null;
            }

            // Hide optional panels
            flpSurveySelection.Visible = false;
            flpSelection.Visible = false;
            flpManufacturing.Visible = false;

            // Reset manufacturing sub-controls
            txtQuantity.Visible = false;
            txtQuantity.Text = string.Empty;
            chkStageResources.Visible = false;
            chkStageResources.Checked = false;
            cmdStart.Visible = false;
            cmdDone.Visible = false;

            // Detach data
            ViewModel = null;
            Colony = null;
            _blueprint = null;
            _completionModification = false;

            // Reset background
            flpColonyStructure.BackColor = SystemColors.Control;
        }

        // -----------------------------------------------------------------------
        // 7.3: UpdateData(Blueprint bp) â€” full repaint
        // -----------------------------------------------------------------------

        /// <summary>
        /// Full repaint of the control with the given pre-resolved blueprint.
        /// Sets header, status RTF, worker checkboxes, and panel visibility by type.
        /// </summary>
        public void UpdateData(Models.Blueprint bp)
        {
            if (ViewModel == null) return;

            using var guard = new ProgrammaticUpdateGuard(this);
            this.SuspendLayout();
            flpColonyStructure.SuspendLayout();

            _blueprint = bp;
            var structureData = ViewModel.Data;

            // --- Header ---
            string bpName = bp != null ? bp.ExtendedName : "(Unknown)";
            lblName.Text = $"{bpName} #{structureData.DisplaySequence}";
            Log.Debug("V2.UpdateData: blueprint={0} uuid={1} seq={2}", bpName, structureData.UUID, structureData.DisplaySequence);

            // --- State checkboxes ---
            chkStaged.Checked = ViewModel.IsStaged;
            chkBuilt.Checked = ViewModel.IsBuilt;
            chkOnline.Checked = ViewModel.IsOnline;

            var udSw = System.Diagnostics.Stopwatch.StartNew();

            // --- Status RTF ---
            PopulateStatusRtf();
            long tRtf = udSw.ElapsedMilliseconds;

            // --- Worker checkboxes (7.6 + 7.7) ---
            PopulateWorkerCheckboxes();
            long tWorkers = udSw.ElapsedMilliseconds;

            // --- Building state: structure transitioning from staged to built ---
            if (structureData.BuildCompletionTime != null &&
                structureData.BuildCompletionTime.TimeRemaining > 0)
            {
                HandleBuildingState();
                UpdateBackgroundColor();
                flpColonyStructure.ResumeLayout(false);
                this.ResumeLayout();
                udSw.Stop();
                Log.Debug(
                    "V2.UpdateData PERF: {0} rtf={1}ms workers={2}ms building total={3}ms",
                    bpName,
                    tRtf,
                    tWorkers - tRtf,
                    udSw.ElapsedMilliseconds);
                Log.Debug(
                    "V2.UpdateData LAYOUT [{0}]: " + "Header={1} vis={2} | Status={3} vis={4} | Workers={5} vis={6} | " + "Survey={7} vis={8} | Selection={9} vis={10} | Mfg={11} vis={12} | " + "Cmds={13} vis={14} | flpCS.Size={15}",
                    bpName,
                    flpHeader.Location,
                    flpHeader.Visible,
                    rtbStatus.Location,
                    rtbStatus.Visible,
                    flpWorkers.Location,
                    flpWorkers.Visible,
                    flpSurveySelection.Location,
                    flpSurveySelection.Visible,
                    flpSelection.Location,
                    flpSelection.Visible,
                    flpManufacturing.Location,
                    flpManufacturing.Visible,
                    flpStructureCommands.Location,
                    flpStructureCommands.Visible,
                    flpColonyStructure.Size);
                return;
            }

            // --- Panel visibility and controls by blueprint type ---
            if (bp != null)
            {
                string bpType = bp.BluePrintType ?? string.Empty;
                if (bpType == BlueprintTypes.MiningRig)
                    HandleMiningRigControls();
                else if (bpType == BlueprintTypes.Refinery)
                    HandleRefineryControls();
                else if (bpType == BlueprintTypes.ResearchLaboratory)
                    HandleResearchLabControls();
                else if (bpType == BlueprintTypes.Manufactory)
                    HandleManufactoryControls();
                else if (bpType.IsCommodityFactory())
                    HandleCommodityFactoryControls();
                else
                    SetPanelVisibilityByType();
            }
            else
            {
                SetPanelVisibilityByType();
            }

            long tControls = udSw.ElapsedMilliseconds;

            // --- Build button for staged structures ---
            HandleStagedBuildButton();

            // --- Background color (7.4) ---
            UpdateBackgroundColor();

            udSw.Stop();
            if (udSw.ElapsedMilliseconds > 5)
            {
                Log.Debug(
                    "V2.UpdateData PERF: {0} rtf={1}ms workers={2}ms controls={3}ms total={4}ms",
                    bpName,
                    tRtf,
                    tWorkers - tRtf,
                    tControls - tWorkers,
                    udSw.ElapsedMilliseconds);
            }

            flpColonyStructure.ResumeLayout(false);
            this.ResumeLayout();

            // Diagnostic: log panel positions and visibility
            Log.Debug(
                "V2.UpdateData LAYOUT [{0}]: " + "Header={1} vis={2} | Status={3} vis={4} | Workers={5} vis={6} | " + "Survey={7} vis={8} | Selection={9} vis={10} | Mfg={11} vis={12} | " + "Cmds={13} vis={14} | flpCS.Size={15}",
                bpName,
                flpHeader.Location,
                flpHeader.Visible,
                rtbStatus.Location,
                rtbStatus.Visible,
                flpWorkers.Location,
                flpWorkers.Visible,
                flpSurveySelection.Location,
                flpSurveySelection.Visible,
                flpSelection.Location,
                flpSelection.Visible,
                flpManufacturing.Location,
                flpManufacturing.Visible,
                flpStructureCommands.Location,
                flpStructureCommands.Visible,
                flpColonyStructure.Size);
        }

        // -----------------------------------------------------------------------
        // 7.4: UpdateBackgroundColor() â€” lightweight path
        // -----------------------------------------------------------------------

        /// <summary>
        /// Lightweight update that only recalculates background color based on
        /// current state and worker assignments. No full repaint.
        /// </summary>
        public void UpdateBackgroundColor()
        {
            if (ViewModel == null)
            {
                flpColonyStructure.BackColor = Color.White;
                return;
            }

            if (ViewModel.IsStaged)
            {
                flpColonyStructure.BackColor = Color.Yellow;
            }
            else if (ViewModel.IsBuilt && !ViewModel.IsOnline)
            {
                flpColonyStructure.BackColor = Color.PaleVioletRed;
            }
            else if (ViewModel.IsOnline)
            {
                bool hasAllWorkers = true;
                for (int i = 0; i < _workerCheckboxes.Length; i++)
                {
                    if (_workerCheckboxes[i].Visible && !_workerCheckboxes[i].Checked)
                    {
                        hasAllWorkers = false;
                        break;
                    }
                }

                flpColonyStructure.BackColor = hasAllWorkers ? Color.Green : Color.LightGreen;
            }
            else
            {
                flpColonyStructure.BackColor = Color.White;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete && ViewModel != null && Colony != null)
            {
                // Don't intercept Delete when a text input or combo has focus
                var focused = FindFocusedControl(this);
                if (focused is TextBox || focused is ComboBox || focused is RichTextBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                ViewModel.Delete(Colony);
                OnColonyStructureDataChanged(structural: true);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Normalizes time strings from blueprint properties to the format expected by
        /// CountDownTime.TimeRemainingString (e.g. "9 hours" -> "9h", "30 minutes" -> "30m").
        /// </summary>
        private static string NormalizeTimeString(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return timeStr;
            timeStr = Regex.Replace(timeStr, @"\s*hours?\s*", "h ", RegexOptions.IgnoreCase);
            timeStr = Regex.Replace(timeStr, @"\s*minutes?\s*", "m ", RegexOptions.IgnoreCase);
            timeStr = Regex.Replace(timeStr, @"\s*seconds?\s*", "s ", RegexOptions.IgnoreCase);
            timeStr = Regex.Replace(timeStr, @"\s*days?\s*", "d ", RegexOptions.IgnoreCase);
            return timeStr.Trim();
        }

        private static string BuildStatusPlainText(ColonyStructure structureData)
        {
            if (structureData.Statuses == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            if (structureData.Statuses.TryGetValue(GameConstants.StatusActual, out var actual))
            {
                sb.Append("Actual: ");
                sb.AppendFormat(
                    "Power:{0}/{1} Hab:{2}/{3} Food:{4}/{5} Ent:{6}/{7} WH:{8}/{9}",
                    actual.PowerRequired,
                    actual.PowerProvided,
                    actual.HabitationRequired,
                    actual.HabitationProvision,
                    actual.FoodRequired,
                    actual.FoodProvision,
                    actual.EntertainmentRequired,
                    actual.EntertainmentProvided,
                    actual.WarehouseRequired,
                    actual.WarehouseCapacity);
            }

            if (structureData.Statuses.TryGetValue(GameConstants.StatusIdeal, out var ideal))
            {
                sb.Append("\nIdeal:  ");
                sb.AppendFormat(
                    "Power:{0}/{1} Hab:{2}/{3} Food:{4}/{5} Ent:{6}/{7} WH:{8}/{9}",
                    ideal.PowerRequired,
                    ideal.PowerProvided,
                    ideal.HabitationRequired,
                    ideal.HabitationProvision,
                    ideal.FoodRequired,
                    ideal.FoodProvision,
                    ideal.EntertainmentRequired,
                    ideal.EntertainmentProvided,
                    ideal.WarehouseRequired,
                    ideal.WarehouseCapacity);
            }

            return sb.ToString();
        }

        private static Control FindFocusedControl(Control parent)
        {
            if (parent == null || !parent.ContainsFocus) return null;
            foreach (Control child in parent.Controls)
            {
                if (child.Focused) return child;
                if (child.ContainsFocus) return FindFocusedControl(child);
            }

            return null;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private int GetCountdownIntervalMs()
        {
            int intervalMs = (int)(PreferencesStore.GetInstance().Preferences.Thresholds.CountdownRefreshRateSeconds * 1000);
            return Math.Max(intervalMs, 1000);
        }

        /// <summary>
        /// Populates the rtbStatus RichTextBox with Actual and Ideal status lines.
        /// </summary>
        private void PopulateStatusRtf()
        {
            var structureData = ViewModel.Data;
            rtbStatus.Text = BuildStatusPlainText(structureData);
        }

        /// <summary>
        /// Sets panel visibility based on blueprint type for types not yet fully implemented
        /// (Manufactory, CommodityFactory, Other). Mining/Refinery/Research have their own handlers.
        /// </summary>
        private void SetPanelVisibilityByType()
        {
            if (_blueprint == null)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            string bpType = _blueprint.BluePrintType ?? string.Empty;

            if (bpType == BlueprintTypes.Manufactory)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                flpManufacturing.Visible = true;
                txtQuantity.Visible = true;
                chkStageResources.Visible = true;
            }
            else if (bpType.IsCommodityFactory())
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                flpManufacturing.Visible = true;
                txtQuantity.Visible = true;
                chkStageResources.Visible = true;
            }
            else
            {
                // Other (no process)
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;
            }
        }

        // -----------------------------------------------------------------------
        // Building state handler
        // -----------------------------------------------------------------------

        private void HandleBuildingState()
        {
            var structureData = ViewModel.Data;

            flpSurveySelection.Visible = false;
            flpSelection.Visible = false;
            flpManufacturing.Visible = true;
            cmdStart.Visible = false;
            cmdDone.Visible = true;
            txtQuantity.Visible = false;
            chkStageResources.Visible = false;
            chkBuilt.Enabled = false;

            txtCompletionTime.Text = structureData.BuildCompletionTime.TimeRemainingString;
            rtbProgressStatus.Text = "Building...";

            if (!timerCountdown.Enabled)
            {
                timerCountdown.Interval = GetCountdownIntervalMs();
                timerCountdown.Start();
            }
        }

        /// <summary>
        /// Shows Build button for staged structures when no sibling is building.
        /// </summary>
        private void HandleStagedBuildButton()
        {
            if (ViewModel == null || !ViewModel.IsStaged || ViewModel.IsBuilt) return;

            bool siblingBuilding = Colony != null && Colony.Structures.Any(s =>
                s != ViewModel.Data &&
                s.BuildCompletionTime != null &&
                s.BuildCompletionTime.TimeRemaining > 0);

            if (!siblingBuilding)
            {
                cmdStart.Text = "Build";
                cmdStart.Visible = true;
                cmdDone.Visible = false;
                flpManufacturing.Visible = true;
                txtQuantity.Visible = false;
                chkStageResources.Visible = false;
            }
        }

        // -----------------------------------------------------------------------
        // Task 12: Mining Rig Controls
        // -----------------------------------------------------------------------

        private void HandleMiningRigControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var structureData = ViewModel.Data;
            Log.Debug(
                "V2.HandleMiningRigControls: survey={0} resource={1} process={2}",
                structureData.MiningSurvey ?? "(none)",
                structureData.MiningSurveyResource ?? "(none)",
                structureData.ProcessCompletionTime != null ? "active" : "idle");

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            bool showSelection = false;
            bool showCompletionTime = false;
            bool enableCmbSurvey = true;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (structureData.ProcessCompletionTime != null)
            {
                showSelection = true;
                showCompletionTime = true;
                enableCmbSurvey = false;
                enableCmbSelection = false;
            }
            else
            {
                showSelection = true;
            }

            if (!string.IsNullOrEmpty(structureData.MiningSurvey))
            {
                showSelection = true;
            }

            if (showSelection && (cmbSelection.SelectedFullIndex >= 0 || !string.IsNullOrEmpty(structureData.MiningSurveyResource)))
            {
                showCmdStart = true;
            }

            // Survey selection row
            flpSurveySelection.Visible = true;
            // Only populate combos if there's an active process or existing selection (perf: defer for idle)
            bool hasActiveProcess = structureData.ProcessCompletionTime != null;
            bool hasSelection = !string.IsNullOrEmpty(structureData.MiningSurvey);
            if (hasActiveProcess && hasSelection)
            {
                // Active process: just show the selected item, don't build full list
                SetSingleItemSurveyCombo(structureData.MiningSurvey);
                SetSingleItemResourceCombo(structureData.MiningSurveyResource);
            }
            else
            {
                PopulateSurveyCombo(hasSelection ? structureData.MiningSurvey : null);
                if (hasSelection)
                    PopulateResourceComboFromSurvey(structureData.MiningSurveyResource);
            }

            cmbSurvey.Enabled = enableCmbSurvey;

            // Resource selection row
            if (!string.IsNullOrEmpty(structureData.MiningSurvey))
            {
                flpSelection.Visible = true;
                cmbSelection.Enabled = enableCmbSelection;
            }
            else
            {
                flpSelection.Visible = false;
            }

            // Manufacturing row: Start/Done buttons only (no qty/stage)
            flpManufacturing.Visible = showCompletionTime;
            txtQuantity.Visible = false;
            chkStageResources.Visible = false;
            cmdStart.Text = "Start";
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdDone.Visible = showCompletionTime;

            // Timer row
            if (showCompletionTime)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
                PopulateMiningProgressStatus();
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = GetCountdownIntervalMs();
                    timerCountdown.Start();
                }
            }
            else
            {
                rtbProgressStatus.Text = string.Empty;
            }
        }

        private void PopulateSurveyCombo(string currentSurveyUUID = null)
        {
            var filteredList = new List<Models.Survey>(_playerContext.SurveyList);

            filteredList = filteredList
                .Where(item => Colony != null && string.Equals(item.PlanetName, Colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            filteredList.Sort((a, b) => string.Compare(a.ExtendedName, b.ExtendedName, StringComparison.OrdinalIgnoreCase));
            filteredList.Insert(0, new Models.Survey());

            _surveyItems = filteredList;
            var displayNames = filteredList.Select(s => s.ExtendedName ?? string.Empty).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentSurveyUUID))
            {
                var match = filteredList.FirstOrDefault(s => s.UUID == currentSurveyUUID);
                if (match != null) currentDisplay = match.ExtendedName;
            }

            cmbSurvey.SetItems(displayNames, currentDisplay);
        }

        /// <summary>
        /// Sets cmbSurvey to display a single survey item without building the full list.
        /// Used when the process is active and the combo is disabled.
        /// </summary>
        private void SetSingleItemSurveyCombo(string surveyUUID)
        {
            var survey = _playerContext.FindSurvey(surveyUUID);
            var items = new List<Models.Survey>();
            if (survey != null) items.Add(survey);
            else items.Add(new Models.Survey { PlanetName = surveyUUID });
            _surveyItems = items;
            var displayNames = items.Select(s => s.ExtendedName ?? string.Empty).ToList();
            cmbSurvey.SetItems(displayNames, displayNames.FirstOrDefault());
        }

        /// <summary>
        /// Sets cmbSelection to display a single resource item without building the full list.
        /// Used when the mining process is active and the combo is disabled.
        /// </summary>
        private void SetSingleItemResourceCombo(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                cmbSelection.SetItems(new List<string>(), null);
                _selectionValues.Clear();
                return;
            }

            _selectionValues = new List<string> { resource };
            cmbSelection.SetItems(new List<string> { resource }, resource);
        }

        /// <summary>
        /// Sets cmbSelection to display a single item (by key/display) without building the full list.
        /// Used when a process is active and the combo is disabled.
        /// </summary>
        private void SetSingleItemSelectionCombo(string key, string displayName)
        {
            _selectionValues = new List<string> { key };
            cmbSelection.SetItems(new List<string> { displayName }, displayName);
        }

        private void PopulateResourceComboFromSurvey(string currentResource = null)
        {
            int surveyIdx = cmbSurvey.SelectedFullIndex;
            Models.Survey survey = (surveyIdx >= 0 && surveyIdx < _surveyItems.Count) ? _surveyItems[surveyIdx] : null;
            if (survey == null || survey.Resources == null)
            {
                cmbSelection.SetItems(new List<string>(), null);
                _selectionValues.Clear();
                return;
            }

            var resources = survey.Resources.Values.ToList();
            resources.Sort((a, b) => string.Compare(a.Resource, b.Resource, StringComparison.OrdinalIgnoreCase));
            resources.Insert(0, new SurveyResource());

            _selectionValues = resources.Select(r => r.Resource ?? string.Empty).ToList();
            var displayNames = resources.Select(r => r.ExtendedName ?? string.Empty).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentResource))
            {
                var match = resources.FirstOrDefault(r => r.Resource == currentResource);
                if (match != null) currentDisplay = match.ExtendedName;
            }

            cmbSelection.SetItems(displayNames, currentDisplay);
        }

        private void PopulateMiningProgressStatus()
        {
            var structureData = ViewModel.Data;
            if (structureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(structureData.MiningSurvey) ||
                string.IsNullOrEmpty(structureData.MiningSurveyResource))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            Models.Survey survey = _playerContext.FindSurvey(structureData.MiningSurvey);
            if (survey == null || !survey.Resources.ContainsKey(structureData.MiningSurveyResource))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            SurveyResource resource = survey.Resources[structureData.MiningSurveyResource];
            rtbProgressStatus.Text = $"{resource.Amount}/h {resource.Resource} ({resource.Purity})";
        }

        // -----------------------------------------------------------------------
        // Task 13: Refinery Controls
        // -----------------------------------------------------------------------

        private void HandleRefineryControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var structureData = ViewModel.Data;
            Log.Debug(
                "V2.HandleRefineryControls: resource={0} purity={1} process={2}",
                structureData.RefiningResource ?? "(none)",
                structureData.RefiningResourcePurity ?? "(none)",
                structureData.ProcessCompletionTime != null ? "active" : "idle");

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (structureData.ProcessCompletionTime != null)
            {
                showCompletionTime = true;
                enableCmbSelection = false;
            }

            if (!string.IsNullOrEmpty(structureData.RefiningResource))
            {
                showCmdStart = true;
            }

            // No survey selection for refinery
            flpSurveySelection.Visible = false;

            // Selection: unrefined resources from warehouse + actively mined resources + synthetic recipes
            flpSelection.Visible = true;
            bool hasActiveProcess = structureData.ProcessCompletionTime != null;
            bool hasSelection = !string.IsNullOrEmpty(structureData.RefiningResource);
            if (hasActiveProcess && hasSelection)
            {
                // Active process: just show the selected item, don't build full list
                string displayKey = structureData.RefiningResource + "|" + structureData.RefiningResourcePurity;
                var recipe = RefiningRecipes.FindByInput(structureData.RefiningResource, structureData.RefiningResourcePurity);
                if (recipe != null) displayKey += "|S" + recipe.Tier;
                string displayName = structureData.RefiningResource + " (" + (structureData.RefiningResourcePurity ?? string.Empty) + ")";
                if (recipe != null) displayName = recipe.OutputResource + " (S" + recipe.Tier + ")";

                _selectionValues = new List<string> { displayKey };
                cmbSelection.SetItems(new List<string> { displayName }, displayName);
            }
            else
            {
                string restoreKey = null;
                if (hasSelection)
                {
                    restoreKey = structureData.RefiningResource + "|" + structureData.RefiningResourcePurity;
                    var recipe = RefiningRecipes.FindByInput(structureData.RefiningResource, structureData.RefiningResourcePurity);
                    if (recipe != null) restoreKey += "|S" + recipe.Tier;
                }

                PopulateSelectionWithUnrefinedResources(restoreKey);
            }

            cmbSelection.Enabled = enableCmbSelection;

            // Manufacturing row: Start/Done only (no qty/stage)
            flpManufacturing.Visible = showCompletionTime;
            txtQuantity.Visible = false;
            chkStageResources.Visible = false;
            cmdStart.Text = "Start";
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdDone.Visible = showCompletionTime;

            // Timer row
            if (showCompletionTime)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
                PopulateRefineryProgressStatus();
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = GetCountdownIntervalMs();
                    timerCountdown.Start();
                }
            }
            else
            {
                rtbProgressStatus.Text = string.Empty;
            }
        }

        private void PopulateSelectionWithUnrefinedResources(string currentKey = null)
        {
            var unrefinedItems = new List<RefinerySelectionItem>();

            // Add unrefined resources from warehouse
            if (Colony != null && Colony.Items != null)
            {
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
                            Models.Survey survey = _playerContext.FindSurvey(structure.MiningSurvey);
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
            }

            unrefinedItems.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            unrefinedItems.Insert(0, new RefinerySelectionItem { Key = string.Empty, DisplayName = string.Empty, ResourceName = string.Empty, Purity = string.Empty });

            _selectionValues = unrefinedItems.Select(i => i.Key).ToList();
            var displayNames = unrefinedItems.Select(i => i.DisplayName).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentKey))
            {
                var match = unrefinedItems.FirstOrDefault(i => i.Key == currentKey);
                if (match != null) currentDisplay = match.DisplayName;
            }

            cmbSelection.SetItems(displayNames, currentDisplay);
        }

        private void PopulateRefineryProgressStatus()
        {
            var structureData = ViewModel.Data;
            if (structureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(structureData.RefiningResource) ||
                string.IsNullOrEmpty(structureData.RefiningResourcePurity))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            var recipe = RefiningRecipes.FindByInput(
                structureData.RefiningResource, structureData.RefiningResourcePurity);

            if (recipe != null)
            {
                rtbProgressStatus.Text = $"{recipe.ConsumeRate}:{recipe.ProduceRate} {recipe.OutputResource}";
            }
            else
            {
                int baseRate = GameConstants.RefiningBaseRate;
                int outputRate = GameConstants.GetRefiningOutputRate(structureData.RefiningResourcePurity, baseRate);
                rtbProgressStatus.Text = $"{baseRate}:{outputRate} {structureData.RefiningResource} ({structureData.RefiningResourcePurity})";
            }
        }

        // -----------------------------------------------------------------------
        // Task 14: Research Lab Controls
        // -----------------------------------------------------------------------

        private void HandleResearchLabControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var structureData = ViewModel.Data;
            Log.Debug(
                "V2.HandleResearchLabControls: blueprintUUID={0} process={1}",
                structureData.ResearchingBlueprintUUID ?? "(none)",
                structureData.ProcessCompletionTime != null ? "active" : "idle");

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (structureData.ProcessCompletionTime != null)
            {
                // Clear orphaned research state when blueprint UUID is null or blueprint not found
                if (string.IsNullOrEmpty(structureData.ResearchingBlueprintUUID)
                    || _playerContext.FindBlueprint(structureData.ResearchingBlueprintUUID) == null)
                {
                    Log.Warn(
                        "ResearchLab {0}: clearing orphaned research state (blueprint={1})",
                        structureData.UUID,
                        structureData.ResearchingBlueprintUUID ?? "(null)");
                    structureData.ProcessCompletionTime = null;
                    structureData.ResearchingBlueprintUUID = null;
                }
                else
                {
                    showCompletionTime = true;
                    enableCmbSelection = false;
                }
            }

            if (!string.IsNullOrEmpty(structureData.ResearchingBlueprintUUID))
            {
                showCmdStart = true;
            }

            // No survey selection for research lab
            flpSurveySelection.Visible = false;

            // Selection: researchable blueprints
            flpSelection.Visible = true;
            bool hasActiveProcess = structureData.ProcessCompletionTime != null;
            bool hasSelection = !string.IsNullOrEmpty(structureData.ResearchingBlueprintUUID);
            if (hasActiveProcess && hasSelection)
            {
                // Active process: just show the selected blueprint, don't build full list
                var bp = _playerContext.FindBlueprint(structureData.ResearchingBlueprintUUID);
                string displayName = bp != null ? bp.ExtendedName : structureData.ResearchingBlueprintUUID;
                SetSingleItemSelectionCombo(structureData.ResearchingBlueprintUUID, displayName);
            }
            else
            {
                PopulateSelectionWithResearchableBlueprints(hasSelection ? structureData.ResearchingBlueprintUUID : null);
            }

            cmbSelection.Enabled = enableCmbSelection;

            // Manufacturing row: Start/Done only (no qty/stage)
            flpManufacturing.Visible = showCompletionTime;
            txtQuantity.Visible = false;
            chkStageResources.Visible = false;
            cmdStart.Text = "Start";
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdDone.Visible = showCompletionTime;

            // Timer row
            if (showCompletionTime)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
                PopulateResearchLabProgressStatus();
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = GetCountdownIntervalMs();
                    timerCountdown.Start();
                }
            }
            else
            {
                rtbProgressStatus.Text = string.Empty;
            }
        }

        private void PopulateSelectionWithResearchableBlueprints(string currentUUID = null)
        {
            var items = new List<ResearchSelectionItem>();

            foreach (Models.Blueprint bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                if (bp.Evolution >= 15) continue;
                if (!ResearchTimeLookup.CanResearchEvolution(bp.Evolution)) continue;

                bool canResearch = true;
                bp.Properties.GetBoolean("Can Research", true, out canResearch);
                if (!canResearch) continue;

                string display = bp.ExtendedName;

                items.Add(new ResearchSelectionItem
                {
                    UUID = bp.UUID,
                    DisplayName = display
                });
            }

            items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, new ResearchSelectionItem { UUID = string.Empty, DisplayName = string.Empty });

            _selectionValues = items.Select(i => i.UUID).ToList();
            var displayNames = items.Select(i => i.DisplayName).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentUUID))
            {
                var match = items.FirstOrDefault(i => i.UUID == currentUUID);
                if (match != null) currentDisplay = match.DisplayName;
            }

            cmbSelection.SetItems(displayNames, currentDisplay);
        }

        private void PopulateResearchLabProgressStatus()
        {
            var structureData = ViewModel.Data;
            if (structureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(structureData.ResearchingBlueprintUUID))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            Models.Blueprint bp = _playerContext.FindBlueprint(structureData.ResearchingBlueprintUUID);
            if (bp == null)
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            rtbProgressStatus.Text = $"Evo {bp.Evolution}->{bp.Evolution + 1} {bp.Name}";
        }

        // -----------------------------------------------------------------------
        // Task 15: Manufactory Controls
        // -----------------------------------------------------------------------

        private void HandleManufactoryControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var structureData = ViewModel.Data;
            Log.Debug(
                "V2.HandleManufactoryControls: blueprintUUID={0} qty={1} process={2}",
                structureData.ManufacturingBlueprintUUID ?? "(none)",
                structureData.ManufacturingQuantity,
                structureData.ProcessCompletionTime != null ? "active" : "idle");

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (structureData.ProcessCompletionTime != null)
            {
                // Clear orphaned manufacturing state when blueprint UUID is null or not found
                if (string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID)
                    || _playerContext.FindBlueprint(structureData.ManufacturingBlueprintUUID) == null)
                {
                    Log.Warn(
                        "Manufactory {0}: clearing orphaned manufacturing state (blueprint={1})",
                        structureData.UUID,
                        structureData.ManufacturingBlueprintUUID ?? "(null)");
                    structureData.ProcessCompletionTime = null;
                    structureData.ManufacturingBlueprintUUID = null;
                    structureData.ManufacturingQuantity = 0;
                    structureData.ManufacturingCompleted = 0;
                }
                else
                {
                    showCompletionTime = true;
                    enableCmbSelection = false;
                }
            }

            if (!string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID))
            {
                showCmdStart = true;
            }

            // No survey selection for manufactory
            flpSurveySelection.Visible = false;

            // Selection: manufacturable blueprints
            flpSelection.Visible = true;
            bool hasActiveProcess = structureData.ProcessCompletionTime != null;
            bool hasSelection = !string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID);
            if (hasActiveProcess && hasSelection)
            {
                // Active process: just show the selected blueprint, don't build full list
                var bp = _playerContext.FindBlueprint(structureData.ManufacturingBlueprintUUID);
                string displayName = bp != null ? bp.ExtendedName : structureData.ManufacturingBlueprintUUID;
                SetSingleItemSelectionCombo(structureData.ManufacturingBlueprintUUID, displayName);
            }
            else
            {
                PopulateSelectionWithManufacturableBlueprints(hasSelection ? structureData.ManufacturingBlueprintUUID : null);
            }

            cmbSelection.Enabled = enableCmbSelection;

            // Manufacturing row: qty, stage resources, start/done
            flpManufacturing.Visible = showCompletionTime;
            txtQuantity.Visible = showCmdStart;
            txtQuantity.Enabled = !showCompletionTime;
            if (structureData.ManufacturingQuantity > 0)
            {
                txtQuantity.Text = structureData.ManufacturingQuantity.ToString();
            }
            else if (string.IsNullOrEmpty(txtQuantity.Text) || !int.TryParse(txtQuantity.Text, out _))
            {
                txtQuantity.Text = "1";
            }

            cmdStart.Text = "Start";
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdDone.Visible = showCompletionTime;

            // Stage Resources checkbox: visible when not manufacturing
            if (showCompletionTime)
            {
                chkStageResources.Visible = false;
            }
            else
            {
                chkStageResources.Visible = true;
                int mfgQty = 0;
                int.TryParse(txtQuantity.Text, out mfgQty);
                chkStageResources.Enabled = !string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID) && mfgQty > 0;
                chkStageResources.Checked = ViewModel.StagingResources;
            }

            // Timer row
            if (showCompletionTime)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
                PopulateManufactoryProgressStatus();
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = GetCountdownIntervalMs();
                    timerCountdown.Start();
                }
            }
            else
            {
                rtbProgressStatus.Text = string.Empty;
            }
        }

        private void PopulateSelectionWithManufacturableBlueprints(string currentUUID = null)
        {
            var items = new List<ResearchSelectionItem>();

            foreach (Models.Blueprint bp in _playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;

                bool canManufacture = true;
                bp.Properties.GetBoolean("Can Manufacture", true, out canManufacture);
                if (!canManufacture) continue;

                string display = bp.ExtendedName;

                items.Add(new ResearchSelectionItem
                {
                    UUID = bp.UUID,
                    DisplayName = display
                });
            }

            items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, new ResearchSelectionItem { UUID = string.Empty, DisplayName = string.Empty });

            _selectionValues = items.Select(i => i.UUID).ToList();
            var displayNames = items.Select(i => i.DisplayName).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentUUID))
            {
                var match = items.FirstOrDefault(i => i.UUID == currentUUID);
                if (match != null) currentDisplay = match.DisplayName;
            }

            cmbSelection.SetItems(displayNames, currentDisplay);
        }

        private void PopulateManufactoryProgressStatus()
        {
            var structureData = ViewModel.Data;
            if (structureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            Models.Blueprint bp = _playerContext.FindBlueprint(structureData.ManufacturingBlueprintUUID);
            if (bp == null)
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            int displayProgress = Math.Min(structureData.ManufacturingCompleted + 1, structureData.ManufacturingQuantity);
            rtbProgressStatus.Text = $"({displayProgress}/{structureData.ManufacturingQuantity}) {bp.ExtendedName}";
        }

        // -----------------------------------------------------------------------
        // Task 16: Commodity Factory Controls
        // -----------------------------------------------------------------------

        private void HandleCommodityFactoryControls()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var structureData = ViewModel.Data;
            Log.Debug(
                "V2.HandleCommodityFactoryControls: commodity={0} qty={1} process={2}",
                structureData.ManufacturingCommodityName ?? "(none)",
                structureData.ManufacturingQuantity,
                structureData.ProcessCompletionTime != null ? "active" : "idle");

            if (!ViewModel.IsBuilt || !ViewModel.IsOnline)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;

                return;
            }

            bool showCompletionTime = false;
            bool enableCmbSelection = true;
            bool showCmdStart = false;

            if (structureData.ProcessCompletionTime != null)
            {
                // Clear orphaned state when commodity name is null
                if (string.IsNullOrEmpty(structureData.ManufacturingCommodityName))
                {
                    Log.Warn(
                        "CommodityFactory {0}: clearing orphaned ProcessCompletionTime (no ManufacturingCommodityName)",
                        structureData.UUID);
                    structureData.ProcessCompletionTime = null;
                    structureData.ManufacturingQuantity = 0;
                    structureData.ManufacturingCompleted = 0;
                }
                else
                {
                    showCompletionTime = true;
                    enableCmbSelection = false;
                }
            }

            if (!string.IsNullOrEmpty(structureData.ManufacturingCommodityName))
            {
                showCmdStart = true;
            }

            // No survey selection for commodity factory
            flpSurveySelection.Visible = false;

            // Selection: commodities filtered by CommodityIndustry
            flpSelection.Visible = true;
            bool hasActiveProcess = structureData.ProcessCompletionTime != null;
            bool hasSelection = !string.IsNullOrEmpty(structureData.ManufacturingCommodityName);
            if (hasActiveProcess && hasSelection)
            {
                // Active process: just show the selected commodity, don't build full list
                _selectionValues = new List<string> { structureData.ManufacturingCommodityName };
                cmbSelection.SetItems(new List<string> { structureData.ManufacturingCommodityName }, structureData.ManufacturingCommodityName);
            }
            else
            {
                PopulateSelectionWithCommodities(hasSelection ? structureData.ManufacturingCommodityName : null);
            }

            cmbSelection.Enabled = enableCmbSelection;

            // Manufacturing row: qty, stage resources, start/done
            flpManufacturing.Visible = showCompletionTime;
            txtQuantity.Visible = showCmdStart;
            txtQuantity.Enabled = !showCompletionTime;
            if (structureData.ManufacturingQuantity > 0)
            {
                txtQuantity.Text = structureData.ManufacturingQuantity.ToString();
            }
            else if (string.IsNullOrEmpty(txtQuantity.Text) || !int.TryParse(txtQuantity.Text, out _))
            {
                txtQuantity.Text = "1";
            }

            cmdStart.Text = "Start";
            cmdStart.Visible = showCmdStart && !showCompletionTime;
            cmdDone.Visible = showCompletionTime;

            // Stage Resources checkbox: visible when not manufacturing
            if (showCompletionTime)
            {
                chkStageResources.Visible = false;
            }
            else
            {
                chkStageResources.Visible = true;
                int mfgQty = 0;
                int.TryParse(txtQuantity.Text, out mfgQty);
                chkStageResources.Enabled = !string.IsNullOrEmpty(structureData.ManufacturingCommodityName) && mfgQty > 0;
                chkStageResources.Checked = ViewModel.StagingResources;
            }

            // Timer row
            if (showCompletionTime)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
                PopulateCommodityFactoryProgressStatus();
                if (!timerCountdown.Enabled)
                {
                    timerCountdown.Interval = GetCountdownIntervalMs();
                    timerCountdown.Start();
                }
            }
            else
            {
                rtbProgressStatus.Text = string.Empty;
            }
        }

        private void PopulateSelectionWithCommodities(string currentName = null)
        {
            // Get the CommodityIndustry from the flatpack blueprint
            string industryFilter = string.Empty;
            if (_blueprint != null)
            {
                _blueprint.Properties.GetString("Commodity Industry", string.Empty, out industryFilter);
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

                items.Add(new CommoditySelectionItem
                {
                    Name = commodity.Name,
                    DisplayName = display
                });
            }

            items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, new CommoditySelectionItem { Name = string.Empty, DisplayName = string.Empty });

            _selectionValues = items.Select(i => i.Name).ToList();
            var displayNames = items.Select(i => i.DisplayName).ToList();

            string currentDisplay = null;
            if (!string.IsNullOrEmpty(currentName))
            {
                var match = items.FirstOrDefault(i => i.Name == currentName);
                if (match != null) currentDisplay = match.DisplayName;
            }

            cmbSelection.SetItems(displayNames, currentDisplay);
        }

        private void PopulateCommodityFactoryProgressStatus()
        {
            var structureData = ViewModel.Data;
            if (structureData.ProcessCompletionTime == null ||
                string.IsNullOrEmpty(structureData.ManufacturingCommodityName))
            {
                rtbProgressStatus.Text = string.Empty;
                return;
            }

            int displayProgress = Math.Min(structureData.ManufacturingCompleted + 1, structureData.ManufacturingQuantity);
            rtbProgressStatus.Text = $"({displayProgress}/{structureData.ManufacturingQuantity}) {structureData.ManufacturingCommodityName} x{GameConstants.CommoditiesPerCycle}";
        }

        // -----------------------------------------------------------------------
        // 7.5: State checkboxes â€” Built, Online, Staged with mutual exclusion
        // -----------------------------------------------------------------------

        private void ChkBuilt_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            Log.Debug("V2.ChkBuilt_CheckedChanged: new={0}", chkBuilt.Checked);
            ViewModel.IsBuilt = chkBuilt.Checked;
            if (chkBuilt.Checked)
            {
                ViewModel.IsStaged = false;
            }

            UpdateData(_blueprint);
            OnColonyStructureDataChanged(structural: false);
        }

        private void ChkOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            Log.Debug("V2.ChkOnline_CheckedChanged: new={0}", chkOnline.Checked);
            ViewModel.IsOnline = chkOnline.Checked;
            if (chkOnline.Checked)
            {
                ViewModel.IsBuilt = true;
                ViewModel.IsStaged = false;
            }

            UpdateData(_blueprint);
            OnColonyStructureDataChanged(structural: false);
        }

        private void ChkStaged_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            Log.Debug("V2.ChkStaged_CheckedChanged: new={0}", chkStaged.Checked);
            ViewModel.IsStaged = chkStaged.Checked;
            if (chkStaged.Checked)
            {
                ViewModel.IsBuilt = false;
                ViewModel.IsOnline = false;
            }

            UpdateData(_blueprint);
            OnColonyStructureDataChanged(structural: false);
        }

        // -----------------------------------------------------------------------
        // 7.6: Worker checkboxes with write-through
        // 7.7: Unallocated worker checkboxes (disabled, read-only)
        // -----------------------------------------------------------------------

        private void PopulateWorkerCheckboxes()
        {
            flpWorkers.SuspendLayout();

            for (int i = 0; i < _workerCheckboxes.Length; i++)
            {
                _workerCheckboxes[i].Visible = false;
                _workerCheckboxes[i].Checked = false;
                _workerCheckboxes[i].Enabled = true;
                _workerCheckboxes[i].Tag = null;
            }

            if (ViewModel == null)
            {
                flpWorkers.ResumeLayout(false);
                return;
            }

            int controlIndex = 0;

            // Assigned Workers
            foreach (var wt in WorkerDetail.WorkerTypes)
            {
                int index = 1;
                string key = wt.WorkerPrefix + index;
                while (ViewModel.WorkerKeyExists(key) && controlIndex < 3)
                {
                    _workerCheckboxes[controlIndex].Visible = true;
                    _workerCheckboxes[controlIndex].Enabled = true;
                    _workerCheckboxes[controlIndex].Text = wt.DisplayName;
                    _workerCheckboxes[controlIndex].Tag = key;
                    _workerCheckboxes[controlIndex].Checked = ViewModel.GetWorkerAssigned(key);
                    controlIndex++;
                    index++;
                    key = wt.WorkerPrefix + index;
                }
            }

            // Unallocated Workers (disabled, read-only)
            if (_blueprint != null)
            {
                int unallocatedIndex = 3;
                foreach (var wt in WorkerDetail.WorkerTypes)
                {
                    if (_blueprint.Properties.ContainsKey(wt.UnassignedPropertyKey) && unallocatedIndex < 6)
                    {
                        bool available = IsUnallocatedWorkerAvailable(wt.DetailKey);
                        _workerCheckboxes[unallocatedIndex].Visible = true;
                        _workerCheckboxes[unallocatedIndex].Enabled = false;
                        _workerCheckboxes[unallocatedIndex].Text = "Support - " + wt.DisplayName;
                        _workerCheckboxes[unallocatedIndex].Tag = wt.UnassignedPropertyKey;
                        _workerCheckboxes[unallocatedIndex].Checked = available;
                        unallocatedIndex++;
                    }
                }
            }

            flpWorkers.ResumeLayout(false);
        }

        private bool IsUnallocatedWorkerAvailable(string workerDetailID)
        {
            if (ViewModel == null) return false;
            var structureData = ViewModel.Data;

            if (structureData.Statuses.TryGetValue(GameConstants.StatusActual, out var status))
            {
                return status.GetUnallocatedPresent(workerDetailID);
            }

            return false;
        }

        private void ChkWorker_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var chk = (CheckBox)sender;
            string key = chk.Tag as string;
            if (string.IsNullOrEmpty(key) || ViewModel == null) return;

            if (chk.Enabled)
            {
                ViewModel.SetWorkerAssigned(key, chk.Checked);
            }

            UpdateBackgroundColor();
            OnColonyStructureDataChanged(structural: false);
        }

        // -----------------------------------------------------------------------
        // 7.8: Up/Down/Delete structure command buttons + Delete key
        // -----------------------------------------------------------------------

        private void CmdUp_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveUp(Colony);
            OnColonyStructureDataChanged(structural: true, scrollToStructureUUID: ViewModel.Data.UUID);
        }

        private void CmdDown_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveDown(Colony);
            OnColonyStructureDataChanged(structural: true, scrollToStructureUUID: ViewModel.Data.UUID);
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.Delete(Colony);
            OnColonyStructureDataChanged(structural: true);
        }

        // -----------------------------------------------------------------------
        // Survey and selection handlers
        // -----------------------------------------------------------------------

        private void CmbSurvey_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (ViewModel == null) return;

            var structureData = ViewModel.Data;
            int idx = cmbSurvey.SelectedFullIndex;
            string survey = (idx >= 0 && idx < _surveyItems.Count) ? _surveyItems[idx].UUID : null;
            Log.Debug("V2.CmbSurvey_SelectedItemChanged: old={0} new={1}", structureData.MiningSurvey ?? "(none)", survey ?? "(none)");
            if (survey != structureData.MiningSurvey)
            {
                structureData.MiningLeftOvers = decimal.Zero;
                structureData.MiningSurveyResource = null;
            }

            structureData.MiningSurvey = survey;
            // Only refresh controls if a real survey was selected
            if (!string.IsNullOrEmpty(survey))
                HandleMiningRigControls();
        }

        private void CmbSelection_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (ViewModel == null || _blueprint == null) return;

            var structureData = ViewModel.Data;
            int idx = cmbSelection.SelectedFullIndex;
            string value = (idx >= 0 && idx < _selectionValues.Count) ? _selectionValues[idx] : null;

            if (_blueprint.BluePrintType == BlueprintTypes.MiningRig)
            {
                Log.Debug("V2.CmbSelection_SelectedItemChanged: type=MiningRig old={0} new={1}", structureData.MiningSurveyResource ?? "(none)", value ?? "(none)");
                if (value != structureData.MiningSurveyResource)
                {
                    structureData.MiningLeftOvers = decimal.Zero;
                }

                structureData.MiningSurveyResource = value;
                // Only refresh controls if a real item was selected (not the empty placeholder)
                if (!string.IsNullOrEmpty(value))
                    HandleMiningRigControls();
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.Refinery)
            {
                Log.Debug(
                    "V2.CmbSelection_SelectedItemChanged: type=Refinery old={0}|{1} new={2}",
                    structureData.RefiningResource ?? "(none)",
                    structureData.RefiningResourcePurity ?? "(none)",
                    value ?? "(none)");
                if (!string.IsNullOrEmpty(value) && value.Contains("|"))
                {
                    string[] parts = value.Split('|');
                    structureData.RefiningResource = parts[0];
                    structureData.RefiningResourcePurity = parts[1];
                }
                else
                {
                    structureData.RefiningResource = null;
                    structureData.RefiningResourcePurity = null;
                }
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
            {
                Log.Debug(
                    "V2.CmbSelection_SelectedItemChanged: type=ResearchLab old={0} new={1}",
                    structureData.ResearchingBlueprintUUID ?? "(none)",
                    value ?? "(none)");
                structureData.ResearchingBlueprintUUID = string.IsNullOrEmpty(value) ? null : value;
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.Manufactory)
            {
                Log.Debug(
                    "V2.CmbSelection_SelectedItemChanged: type=Manufactory old={0} new={1}",
                    structureData.ManufacturingBlueprintUUID ?? "(none)",
                    value ?? "(none)");
                structureData.ManufacturingBlueprintUUID = string.IsNullOrEmpty(value) ? null : value;
                structureData.ManufacturingCompleted = 0;
            }
            else if (_blueprint.BluePrintType.IsCommodityFactory())
            {
                Log.Debug(
                    "V2.CmbSelection_SelectedItemChanged: type=CommodityFactory old={0} new={1}",
                    structureData.ManufacturingCommodityName ?? "(none)",
                    value ?? "(none)");
                structureData.ManufacturingCommodityName = string.IsNullOrEmpty(value) ? null : value;
                structureData.ManufacturingCompleted = 0;
            }
        }

        // -----------------------------------------------------------------------
        // Start button handler
        // -----------------------------------------------------------------------

        private void CmdStart_Click(object sender, EventArgs e)
        {
            if (ViewModel == null) return;
            var structureData = ViewModel.Data;

            // Handle Build button for staged structures
            if (ViewModel.IsStaged && !ViewModel.IsBuilt && cmdStart.Text == "Build")
            {
                Log.Debug("V2.CmdStart_Click: type=Build structure={0}", structureData.UUID);
                HandleBuildStart();
                return;
            }

            if (_blueprint == null) return;

            if (_blueprint.BluePrintType == BlueprintTypes.MiningRig)
            {
                Log.Debug("V2.CmdStart_Click: type=MiningRig structure={0}", structureData.UUID);
                HandleMiningStart();
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.Refinery)
            {
                Log.Debug("V2.CmdStart_Click: type=Refinery structure={0}", structureData.UUID);
                HandleRefineryStart();
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
            {
                Log.Debug("V2.CmdStart_Click: type=ResearchLab structure={0}", structureData.UUID);
                HandleResearchStart();
            }
            else if (_blueprint.BluePrintType == BlueprintTypes.Manufactory)
            {
                Log.Debug("V2.CmdStart_Click: type=Manufactory structure={0}", structureData.UUID);
                HandleManufactoryStart();
            }
            else if (_blueprint.BluePrintType.IsCommodityFactory())
            {
                Log.Debug("V2.CmdStart_Click: type=CommodityFactory structure={0}", structureData.UUID);
                HandleCommodityStart();
            }
        }

        private void HandleBuildStart()
        {
            var structureData = ViewModel.Data;

            // Check single-build constraint
            if (Colony != null && Colony.Structures.Any(s =>
                s != structureData &&
                s.BuildCompletionTime != null &&
                s.BuildCompletionTime.TimeRemaining > 0))
                return;

            // Look up Builder skill level
            int builderLevel = 0;
            if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
            {
                var owner = _playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                if (owner != null)
                    builderLevel = owner.GetSkill(SkillName.Builder).Level;
            }

            long buildSeconds = BuildTimeCalculator.Calculate(builderLevel);

            ViewModel.IsStaged = false;
            structureData.BuildCompletionTime = new CountDownTime();
            structureData.BuildCompletionTime.TimeRemaining = buildSeconds;

            OnColonyStructureDataChanged(structural: false);
        }

        private void HandleMiningStart()
        {
            var structureData = ViewModel.Data;

            structureData.ProcessCompletionTime = new CountDownTime();
            structureData.ProcessCompletionTime.StartTime = SystemClock.UtcNow;
            long secondsUntilNextHour = GameConstants.SecondsPerHour - (long)(SystemClock.UtcNow - SystemClock.UtcNow.Date.AddHours(SystemClock.UtcNow.Hour)).TotalSeconds;
            structureData.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);
            timerCountdown.Interval = GetCountdownIntervalMs();
            timerCountdown.Start();
            HandleMiningRigControls();
            OnColonyStructureDataChanged(structural: false);
        }

        private void HandleRefineryStart()
        {
            var structureData = ViewModel.Data;
            if (string.IsNullOrEmpty(structureData.RefiningResource)) return;

            structureData.ProcessCompletionTime = new CountDownTime();
            structureData.ProcessCompletionTime.StartTime = SystemClock.UtcNow;
            long secondsUntilNextHour = GameConstants.SecondsPerHour - (long)(SystemClock.UtcNow - SystemClock.UtcNow.Date.AddHours(SystemClock.UtcNow.Hour)).TotalSeconds;
            structureData.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);
            timerCountdown.Interval = GetCountdownIntervalMs();
            timerCountdown.Start();
            HandleRefineryControls();
            OnColonyStructureDataChanged(structural: false);
        }

        private void HandleResearchStart()
        {
            var structureData = ViewModel.Data;
            if (string.IsNullOrEmpty(structureData.ResearchingBlueprintUUID)) return;

            Models.Blueprint bp = _playerContext.FindBlueprint(structureData.ResearchingBlueprintUUID);
            if (bp == null) return;

            long researchSeconds = ResearchTimeLookup.GetResearchTimeSeconds(bp.Evolution);
            if (researchSeconds <= 0) return;

            // Apply ResearchFocus skill multiplier (3% per level)
            int researchFocusLevel = 0;
            if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
            {
                var owner = _playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                if (owner != null)
                    researchFocusLevel = owner.GetSkill(SkillName.ResearchFocus).Level;
            }

            researchSeconds = Math.Max(1, (long)(researchSeconds * (1.0 - (researchFocusLevel * 0.03))));

            structureData.ProcessCompletionTime = new CountDownTime();
            structureData.ProcessCompletionTime.StartTime = SystemClock.UtcNow;
            structureData.ProcessCompletionTime.TimeRemaining = researchSeconds;
            timerCountdown.Interval = GetCountdownIntervalMs();
            timerCountdown.Start();
            HandleResearchLabControls();
            OnColonyStructureDataChanged(structural: false);
        }

        private void HandleManufactoryStart()
        {
            var structureData = ViewModel.Data;
            if (string.IsNullOrEmpty(structureData.ManufacturingBlueprintUUID)) return;

            Models.Blueprint bp = _playerContext.FindBlueprint(structureData.ManufacturingBlueprintUUID);
            if (bp == null) return;

            // Parse manufacture time from blueprint properties (default 1s if absent)
            string mfgTimeStr;
            bp.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, "1s", out mfgTimeStr);
            if (string.IsNullOrEmpty(mfgTimeStr)) mfgTimeStr = "1s";

            // Normalize time format: "9 hours" -> "9h", "30 minutes" -> "30m", etc.
            mfgTimeStr = NormalizeTimeString(mfgTimeStr);

            // Parse using CountDownTime's TimeRemainingString parser
            CountDownTime tempTimer = new CountDownTime();
            tempTimer.TimeRemainingString = mfgTimeStr;
            long mfgSeconds = tempTimer.TimeRemaining;
            if (mfgSeconds <= 0) return;

            // Apply ProductionFocus skill multiplier (3% per level)
            int productionFocusLevel = 0;
            if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
            {
                var owner = _playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                if (owner != null)
                    productionFocusLevel = owner.GetSkill(SkillName.ProductionFocus).Level;
            }

            mfgSeconds = Math.Max(1, (long)(mfgSeconds * (1.0 - (productionFocusLevel * 0.03))));

            // Parse quantity from txtQuantity
            int qty = 1;
            int.TryParse(txtQuantity.Text, out qty);
            if (qty <= 0) qty = 1;

            structureData.ManufacturingQuantity = qty;
            structureData.ManufacturingCompleted = 0;
            structureData.ProcessCompletionTime = new CountDownTime();
            structureData.ProcessCompletionTime.StartTime = SystemClock.UtcNow;
            structureData.ProcessCompletionTime.StartRepeating(mfgSeconds);
            timerCountdown.Interval = GetCountdownIntervalMs();
            timerCountdown.Start();
            HandleManufactoryControls();
            OnColonyStructureDataChanged(structural: false);
        }

        private void HandleCommodityStart()
        {
            var structureData = ViewModel.Data;
            if (string.IsNullOrEmpty(structureData.ManufacturingCommodityName)) return;

            // Parse quantity (number of cycles) from txtQuantity
            int qty = 1;
            int.TryParse(txtQuantity.Text, out qty);
            if (qty <= 0) qty = 1;

            structureData.ManufacturingQuantity = qty;
            structureData.ManufacturingCompleted = 0;
            structureData.ProcessCompletionTime = new CountDownTime();
            structureData.ProcessCompletionTime.StartTime = SystemClock.UtcNow;

            // Apply ProductionFocus skill multiplier to commodity cycle time
            long commodityCycleSeconds = GameConstants.CommodityCycleSeconds;
            int productionFocusLevel = 0;
            if (Colony != null && !string.IsNullOrEmpty(Colony.OwnerUUID))
            {
                var owner = _playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == Colony.OwnerUUID);
                if (owner != null)
                    productionFocusLevel = owner.GetSkill(SkillName.ProductionFocus).Level;
            }

            commodityCycleSeconds = Math.Max(1, (long)(commodityCycleSeconds * (1.0 - (productionFocusLevel * 0.03))));

            structureData.ProcessCompletionTime.StartRepeating(commodityCycleSeconds);
            timerCountdown.Interval = GetCountdownIntervalMs();
            timerCountdown.Start();
            HandleCommodityFactoryControls();
            OnColonyStructureDataChanged(structural: false);
        }

        // -----------------------------------------------------------------------
        // Done button handler
        // -----------------------------------------------------------------------

        private void CmdDone_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            var structureData = ViewModel.Data;

            // Handle Build completion (BuildCompletionTime)
            if (structureData.BuildCompletionTime != null)
            {
                Log.Debug("V2.CmdDone_Click: type=Build structure={0}", structureData.UUID);
                if (!Colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
                {
                    Log.Warn("ColonyStructureV2: write lock timeout on colony {0}", Colony.UUID);
                    return;
                }

                try
                {
                    if (structureData.BuildCompletionTime.TimeRemaining > 0)
                    {
                        structureData.BuildCompletionTime.TimeRemaining = 0;
                    }

                    Colony.ProcessColony();
                }
                finally
                {
                    Colony.ColonyLock.ExitWriteLock();
                }

                timerCountdown.Stop();
                txtCompletionTime.Text = string.Empty;
                rtbProgressStatus.Text = string.Empty;
                UpdateData(_blueprint);
                OnColonyStructureDataChanged(structural: false);
                return;
            }

            // Handle process completion
            if (!Colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("ColonyStructureV2: write lock timeout on colony {0}", Colony.UUID);
                return;
            }

            try
            {
                Log.Debug(
                    "V2.CmdDone_Click: type=Process structure={0} bpType={1}",
                    structureData.UUID,
                    _blueprint?.BluePrintType ?? "(none)");
                if (structureData.ProcessCompletionTime != null)
                {
                    if (structureData.ProcessCompletionTime.IsRepeating &&
                        structureData.ProcessCompletionTime.IntervalsPassed == 0)
                    {
                        structureData.ProcessCompletionTime.StartTime =
                            SystemClock.UtcNow.AddSeconds(-structureData.ProcessCompletionTime.RepeatIntervalSeconds);
                    }
                    else if (!structureData.ProcessCompletionTime.IsRepeating &&
                             structureData.ProcessCompletionTime.TimeRemaining > 0)
                    {
                        structureData.ProcessCompletionTime.TimeRemaining = 0;
                    }
                }

                Colony.ProcessColony();
            }
            finally
            {
                Colony.ColonyLock.ExitWriteLock();
            }

            // For manufactories/commodity factories with remaining cycles, keep the timer running
            bool keepTimer = false;
            if (_blueprint != null &&
                _blueprint.BluePrintType == BlueprintTypes.Manufactory &&
                structureData.ManufacturingCompleted < structureData.ManufacturingQuantity)
            {
                keepTimer = true;
            }

            if (_blueprint != null &&
                _blueprint.BluePrintType.IsCommodityFactory() &&
                structureData.ManufacturingCompleted < structureData.ManufacturingQuantity)
            {
                keepTimer = true;
            }

            if (!keepTimer)
            {
                timerCountdown.Stop();
                structureData.ProcessCompletionTime = null;
                txtCompletionTime.Text = string.Empty;
                rtbProgressStatus.Text = string.Empty;
            }

            // Refresh the appropriate controls
            if (_blueprint != null)
            {
                if (_blueprint.BluePrintType == BlueprintTypes.MiningRig)
                    HandleMiningRigControls();
                else if (_blueprint.BluePrintType == BlueprintTypes.Refinery)
                    HandleRefineryControls();
                else if (_blueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    HandleResearchLabControls();
                else if (_blueprint.BluePrintType == BlueprintTypes.Manufactory)
                    HandleManufactoryControls();
                else if (_blueprint.BluePrintType.IsCommodityFactory())
                    HandleCommodityFactoryControls();
            }

            OnColonyStructureDataChanged(structural: false);
        }

        // -----------------------------------------------------------------------
        // Timer tick handler
        // -----------------------------------------------------------------------

        private void TimerCountdown_Tick(object sender, EventArgs e)
        {
            if (ViewModel == null) return;
            var structureData = ViewModel.Data;

            if (!_completionModification && structureData.ProcessCompletionTime != null)
            {
                txtCompletionTime.Text = structureData.ProcessCompletionTime.TimeRemainingString;
            }
            else if (!_completionModification && structureData.BuildCompletionTime != null)
            {
                txtCompletionTime.Text = structureData.BuildCompletionTime.TimeRemainingString;
            }
        }

        // -----------------------------------------------------------------------
        // Countdown manual edit support
        // -----------------------------------------------------------------------

        private void TxtCompletionTime_Enter(object sender, EventArgs e)
        {
            _completionModification = true;
        }

        private void TxtCompletionTime_Leave(object sender, EventArgs e)
        {
            _completionModification = false;
            if (ViewModel == null) return;
            var structureData = ViewModel.Data;

            if (structureData.ProcessCompletionTime != null)
            {
                structureData.ProcessCompletionTime.TimeRemainingString = txtCompletionTime.Text;
            }
            else if (structureData.BuildCompletionTime != null)
            {
                structureData.BuildCompletionTime.TimeRemainingString = txtCompletionTime.Text;
            }

            OnColonyStructureDataChanged(structural: false);
        }

        // -----------------------------------------------------------------------
        // Stage Resources and Quantity handlers
        // -----------------------------------------------------------------------

        private void ChkStageResources_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (ViewModel == null) return;

            ViewModel.StagingResources = chkStageResources.Checked;

            if (chkStageResources.Checked)
            {
                int qty = 1;
                int.TryParse(txtQuantity.Text, out qty);
                if (qty <= 0) qty = 1;
                ViewModel.Data.ManufacturingQuantity = qty;
            }

            OnColonyStructureDataChanged(structural: false);
        }

        private void TxtQuantity_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (ViewModel == null) return;

            int qty = 0;
            int.TryParse(txtQuantity.Text, out qty);
            if (qty > 0)
                ViewModel.Data.ManufacturingQuantity = qty;
        }

        // -----------------------------------------------------------------------
        // RtbStatus auto-resize
        // -----------------------------------------------------------------------

        private void RtbStatus_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            rtbStatus.Height = e.NewRectangle.Height + 10;
            rtbStatus.Width = e.NewRectangle.Width + 10;
        }

        // -----------------------------------------------------------------------
        // Event helper
        // -----------------------------------------------------------------------

        private void OnColonyStructureDataChanged(bool structural, string scrollToStructureUUID = null)
        {
            ColonyStructureDataChanged?.Invoke(this, new ColonyStructureDataChangedEventArgs(structural, scrollToStructureUUID));
        }

        // -----------------------------------------------------------------------
        // Helper classes for combo box data binding
        // -----------------------------------------------------------------------

        private class RefinerySelectionItem
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public string ResourceName { get; set; }
            public string Purity { get; set; }
        }

        private class ResearchSelectionItem
        {
            public string UUID { get; set; }
            public string DisplayName { get; set; }
        }

        private class CommoditySelectionItem
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
