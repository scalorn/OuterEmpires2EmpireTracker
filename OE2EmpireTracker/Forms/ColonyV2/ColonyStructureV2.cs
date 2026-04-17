using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.ColonyV2
{
    /// <summary>
    /// Poolable UserControl for displaying a single colony structure.
    /// Designed for create-once, Reset+UpdateData on reuse from Structure_Pool.
    /// </summary>
    public partial class ColonyStructureV2 : UserControl, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        /// <summary>The ViewModel wrapping the current ColonyStructure data.</summary>
        public ColonyStructureViewModel ViewModel { get; set; }

        /// <summary>The parent colony that owns this structure.</summary>
        public Models.Colony Colony { get; set; }

        /// <summary>Cached blueprint reference, set during UpdateData.</summary>
        private Models.Blueprint _blueprint;

        /// <summary>Pre-created worker checkboxes: 0-2 assigned, 3-5 unallocated.</summary>
        private readonly CheckBox[] _workerCheckboxes;

        /// <summary>
        /// Fired when structure data changes. IsStructural=true for add/reorder/delete,
        /// false for worker toggle or state change.
        /// </summary>
        public event EventHandler<ColonyStructureDataChangedEventArgs> ColonyStructureDataChanged;

        public ColonyStructureV2()
        {
            InitializeComponent();
            _workerCheckboxes = new CheckBox[]
            {
                chkWorker1, chkWorker2, chkWorker3,
                chkWorker4, chkWorker5, chkWorker6
            };
        }

        // -----------------------------------------------------------------------
        // IProgrammaticUpdateSource
        // -----------------------------------------------------------------------

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // -----------------------------------------------------------------------
        // 7.2: Reset() — pool reuse
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
            flpTimer.Visible = false;

            // Reset manufacturing sub-controls
            txtQuantity.Visible = false;
            chkStageResources.Visible = false;
            chkStageResources.Checked = false;

            // Detach data
            ViewModel = null;
            Colony = null;
            _blueprint = null;

            // Reset background
            flpColonyStructure.BackColor = SystemColors.Control;
        }

        // -----------------------------------------------------------------------
        // 7.3: UpdateData(Blueprint bp) — full repaint
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

            _blueprint = bp;
            var structureData = ViewModel.Data;

            // --- Header ---
            string bpName = bp != null ? bp.ExtendedName : "(Unknown)";
            lblName.Text = $"{bpName} #{structureData.displaySequence}";

            // --- State checkboxes ---
            chkStaged.Checked = ViewModel.IsStaged;
            chkBuilt.Checked = ViewModel.IsBuilt;
            chkOnline.Checked = ViewModel.IsOnline;

            // --- Status RTF ---
            PopulateStatusRtf();

            // --- Worker checkboxes (7.6 + 7.7) ---
            PopulateWorkerCheckboxes();

            // --- Panel visibility by blueprint type (7.3 table) ---
            SetPanelVisibilityByType();

            // --- Background color (7.4) ---
            UpdateBackgroundColor();

            this.ResumeLayout();
        }

        /// <summary>
        /// Populates the rtbStatus RichTextBox with Actual and Ideal status lines.
        /// </summary>
        private void PopulateStatusRtf()
        {
            var structureData = ViewModel.Data;
            var builder = new RtfBuilder();

            if (structureData.Statuses != null)
            {
                if (structureData.Statuses.TryGetValue(GameConstants.StatusActual, out var actualStatus))
                {
                    builder.Append("Actual: ", Color.Black);
                    ColonyStatusCalculator.PopulateStatus(builder, actualStatus);
                }
                if (structureData.Statuses.TryGetValue(GameConstants.StatusIdeal, out var idealStatus))
                {
                    builder.Append("\n", Color.Black);
                    builder.Append("Ideal:  ", Color.Black);
                    ColonyStatusCalculator.PopulateStatus(builder, idealStatus);
                }
            }

            rtbStatus.Rtf = builder.ToRtf();
        }

        /// <summary>
        /// Sets panel visibility based on blueprint type. Only controls visibility —
        /// actual combo population is deferred to Phase 5 (tasks 12-17).
        /// </summary>
        private void SetPanelVisibilityByType()
        {
            if (_blueprint == null)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;
                flpTimer.Visible = false;
                return;
            }

            string bpType = _blueprint.BluePrintType ?? string.Empty;

            if (bpType == BlueprintTypes.MiningRig)
            {
                flpSurveySelection.Visible = true;
                flpSelection.Visible = true;
                // Start/Done only — hide Qty + StageRes
                flpManufacturing.Visible = true;
                txtQuantity.Visible = false;
                chkStageResources.Visible = false;
                flpTimer.Visible = true;
            }
            else if (bpType == BlueprintTypes.Refinery)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                // Start/Done only
                flpManufacturing.Visible = true;
                txtQuantity.Visible = false;
                chkStageResources.Visible = false;
                flpTimer.Visible = true;
            }
            else if (bpType == BlueprintTypes.ResearchLaboratory)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                // Start/Done only
                flpManufacturing.Visible = true;
                txtQuantity.Visible = false;
                chkStageResources.Visible = false;
                flpTimer.Visible = true;
            }
            else if (bpType == BlueprintTypes.Manufactory)
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                // Qty + StageResources + Start/Done
                flpManufacturing.Visible = true;
                txtQuantity.Visible = true;
                chkStageResources.Visible = true;
                flpTimer.Visible = true;
            }
            else if (bpType.IsCommodityFactory())
            {
                flpSurveySelection.Visible = false;
                flpSelection.Visible = true;
                // Qty + StageResources + Start/Done
                flpManufacturing.Visible = true;
                txtQuantity.Visible = true;
                chkStageResources.Visible = true;
                flpTimer.Visible = true;
            }
            else
            {
                // Other (no process)
                flpSurveySelection.Visible = false;
                flpSelection.Visible = false;
                flpManufacturing.Visible = false;
                flpTimer.Visible = false;
            }
        }

        // -----------------------------------------------------------------------
        // 7.4: UpdateBackgroundColor() — lightweight path
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
                // Not staged, not built
                flpColonyStructure.BackColor = Color.White;
            }
        }

        // -----------------------------------------------------------------------
        // 7.5: State checkboxes — Built, Online, Staged with mutual exclusion
        // -----------------------------------------------------------------------

        private void chkBuilt_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.IsBuilt = chkBuilt.Checked;
            if (chkBuilt.Checked)
            {
                ViewModel.IsStaged = false;
            }
            UpdateData(_blueprint);
            OnColonyStructureDataChanged(structural: false);
        }

        private void chkOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ViewModel.IsOnline = chkOnline.Checked;
            if (chkOnline.Checked)
            {
                ViewModel.IsBuilt = true;
                ViewModel.IsStaged = false;
            }
            UpdateData(_blueprint);
            OnColonyStructureDataChanged(structural: false);
        }

        private void chkStaged_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
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

        /// <summary>
        /// Populates worker checkboxes based on blueprint worker properties.
        /// Checkboxes 0-2 are for assigned workers, 3-5 for unallocated.
        /// </summary>
        private void PopulateWorkerCheckboxes()
        {
            // Hide all first
            for (int i = 0; i < _workerCheckboxes.Length; i++)
            {
                _workerCheckboxes[i].Visible = false;
                _workerCheckboxes[i].Checked = false;
                _workerCheckboxes[i].Enabled = true;
                _workerCheckboxes[i].Tag = null;
            }

            if (ViewModel == null) return;

            int controlIndex = 0;

            // --- 7.6: Assigned Workers ---
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

            // --- 7.7: Unallocated Workers (disabled, read-only) ---
            if (_blueprint != null)
            {
                int unallocatedIndex = 3; // slots 3-5
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
        }

        /// <summary>
        /// Checks whether unallocated workers of the given type are available
        /// based on the ColonyStatusCalculator actual status.
        /// </summary>
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

        /// <summary>
        /// Shared handler for all worker checkboxes (chkWorker1 through chkWorker6).
        /// Writes through to ViewModel and updates background color (not full repaint).
        /// </summary>
        private void chkWorker_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var chk = (CheckBox)sender;
            string key = chk.Tag as string;
            if (string.IsNullOrEmpty(key) || ViewModel == null) return;

            // Only write-through for assigned (enabled) checkboxes, not unallocated (disabled)
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

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveUp(Colony);
            OnColonyStructureDataChanged(structural: true);
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.MoveDown(Colony);
            OnColonyStructureDataChanged(structural: true);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (ViewModel == null || Colony == null) return;
            ViewModel.Delete(Colony);
            OnColonyStructureDataChanged(structural: true);
        }

        /// <summary>
        /// Handle Delete key when the control is focused.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete && ViewModel != null && Colony != null)
            {
                ViewModel.Delete(Colony);
                OnColonyStructureDataChanged(structural: true);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // -----------------------------------------------------------------------
        // Timer tick handler (placeholder — full implementation in Phase 5)
        // -----------------------------------------------------------------------

        private void timerCountdown_Tick(object sender, EventArgs e)
        {
            // Timer tick — countdown display will be fully implemented in Phase 5 tasks.
            // For now just a stub to satisfy the designer event wiring.
        }

        // -----------------------------------------------------------------------
        // RtbStatus auto-resize
        // -----------------------------------------------------------------------

        private void rtbStatus_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            rtbStatus.Height = e.NewRectangle.Height + 10;
            rtbStatus.Width = e.NewRectangle.Width + 10;
        }

        // -----------------------------------------------------------------------
        // Event helper
        // -----------------------------------------------------------------------

        private void OnColonyStructureDataChanged(bool structural)
        {
            ColonyStructureDataChanged?.Invoke(this, new ColonyStructureDataChangedEventArgs(structural));
        }
    }
}
