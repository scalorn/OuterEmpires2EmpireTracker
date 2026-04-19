using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.BuildPlanner
{
    public partial class FormBuildPlanner : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private BuildPlan _selectedPlan;

        /// <summary>
        /// Delegate to resolve a colony name from its UUID.
        /// Returns the colony name or null if not found.
        /// </summary>
        private Func<string, string> _colonyFinder;

        public FormBuildPlanner()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            _colonyFinder = uuid =>
            {
                var colony = playerContext.GetCurrentPlayerColonies()
                    .FirstOrDefault(c => c.UUID == uuid);
                return colony?.ColonyName;
            };

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 200);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += lvwPlans_ItemSelectionChanged;

            txtPlanFilter.TextChanged += txtPlanFilter_TextChanged;
            txtPlanName.TextChanged += txtPlanName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            chkIsActive.CheckedChanged += chkIsActive_CheckedChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            PopulatePlanList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged += OnBuildPlanDataChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpPlanFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwPlans.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int gridHeight = h - flpPlanName.Height - flpDescription.Height
                - flpIsActive.Height - cmdSave.Height - 30;
            if (gridHeight < 50) gridHeight = 50;
            dgvBuildItems.Size = new System.Drawing.Size(w - 6, gridHeight);
        }

        // -----------------------------------------------------------------------
        // Plan List
        // -----------------------------------------------------------------------

        private void PopulatePlanList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedPlan?.UUID;
            lvwPlans.Items.Clear();

            var plans = playerContext.GetCurrentPlayerBuildPlans();
            string filter = txtPlanFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                plans = plans.Where(p => p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            plans = plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var plan in plans)
            {
                var item = new ListViewItem(plan.Name) { Tag = plan };
                lvwPlans.Items.Add(item);
                if (plan.UUID == selectedUUID)
                    item.Selected = true;
            }
            sw.Stop();
            Log.Info("PERF PopulatePlanList: total={0}ms items={1}",
                sw.ElapsedMilliseconds, plans.Count);
        }

        private void txtPlanFilter_TextChanged(object sender, EventArgs e)
        {
            PopulatePlanList();
        }

        private void lvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is BuildPlan plan)
            {
                _selectedPlan = plan;
                PopulateForm();
            }
            else if (!e.IsSelected && lvwPlans.SelectedItems.Count == 0)
            {
                _selectedPlan = null;
                ClearForm();
            }
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedPlan == null) { ClearForm(); return; }

            txtPlanName.Text = _selectedPlan.Name;
            txtDescription.Text = _selectedPlan.Description;
            chkIsActive.Checked = _selectedPlan.IsActive;

            PopulateBuildItemsGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: total={0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanName.Text = "";
            txtDescription.Text = "";
            chkIsActive.Checked = true;
            dgvBuildItems.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtPlanName.Enabled = enabled;
            txtDescription.Enabled = enabled;
            chkIsActive.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvBuildItems.Enabled = enabled;
        }

        private void PopulateBuildItemsGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvBuildItems.Rows.Clear();

            if (_selectedPlan == null) return;

            foreach (var item in _selectedPlan.Items)
            {
                string location;
                if (string.IsNullOrEmpty(item.BuildLocationUUID))
                {
                    location = "Unallocated";
                }
                else
                {
                    string colonyName = _colonyFinder(item.BuildLocationUUID);
                    location = colonyName ?? item.BuildLocationUUID;
                }

                int rowIdx = dgvBuildItems.Rows.Add(
                    item.ItemName,
                    item.ItemType.ToString(),
                    item.Quantity,
                    item.Status.ToString(),
                    location,
                    item.Notes);
                dgvBuildItems.Rows[rowIdx].Tag = item;
            }
        }

        // -----------------------------------------------------------------------
        // CRUD Operations
        // -----------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Build Plan",
                OwnerUUID = playerContext.CurrentPlayerUUID,
                IsActive = true
            };
            playerContext.BuildPlanList.Add(plan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(plan.UUID);
            _selectedPlan = plan;
            PopulatePlanList();
            PopulateForm();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            var result = MessageBox.Show(
                string.Format("Delete build plan '{0}'?", _selectedPlan.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            string uuid = _selectedPlan.UUID;
            playerContext.BuildPlanList.Remove(_selectedPlan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(uuid);
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            string name = txtPlanName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Plan name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedPlan.Name = name;
            _selectedPlan.Description = txtDescription.Text;
            _selectedPlan.IsActive = chkIsActive.Checked;

            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
            PopulatePlanList();
            Log.Info("Saved build plan '{0}'", _selectedPlan.Name);
        }

        // -----------------------------------------------------------------------
        // Data Model Write-Through
        // -----------------------------------------------------------------------

        private void txtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Name = txtPlanName.Text;
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Description = txtDescription.Text;
        }

        private void chkIsActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.IsActive = chkIsActive.Checked;
        }

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        private void OnBuildPlanDataChanged(object sender, BuildPlanDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnBuildPlanDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            PopulatePlanList();
            if (_selectedPlan != null)
            {
                PopulateForm();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged -= OnBuildPlanDataChanged;
            base.OnFormClosed(e);
        }
    }
}
