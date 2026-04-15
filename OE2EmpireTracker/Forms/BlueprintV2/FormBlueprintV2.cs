using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker
{
    /// <summary>
    /// FormBlueprintV2 — Clean rewrite of the blueprint management form.
    /// Built around write-through: the data model (PropertyBag, Resources) is always
    /// the source of truth. The grid is a view, not a store.
    /// </summary>
    public partial class FormBlueprintV2 : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private BlueprintViewModel viewModel;

        // ListView sorting state
        private int _sortColumn = 0;
        private SortOrder _sortOrder = SortOrder.Ascending;

        public FormBlueprintV2()
        {
            InitializeComponent();

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new BlueprintViewModel(new Blueprint(), playerContext);

            InitFilterCombos();
            InitDetailCombos();

            // ListView sorting
            lvwBlueprints.ColumnClick += lvwBlueprints_ColumnClick;
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);

            // Wire filter events
            txtFilter.TextChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterType.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterClass.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterTechLevel.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterEvolution.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            chkFilterEvoAndAbove.CheckedChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            btnClearFilters.Click += btnClearFilters_Click;

            // Wire list selection
            lvwBlueprints.SelectedIndexChanged += lvwBlueprints_SelectedIndexChanged;

            // Wire identity field write-through
            txtName.TextChanged += txtName_TextChanged;
            txtNickName.TextChanged += txtNickName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            txtCopyCost.TextChanged += txtCopyCost_TextChanged;
            cmbBlueprintType.SelectedIndexChanged += cmbBlueprintType_SelectedIndexChanged;
            cmbShipClass.SelectedIndexChanged += cmbShipClass_SelectedIndexChanged;
            cmbTechLevel.SelectedIndexChanged += cmbTechLevel_SelectedIndexChanged;
            cmbEvolution.SelectedIndexChanged += cmbEvolution_SelectedIndexChanged;
            cmbBaseBlueprint.SelectedIndexChanged += cmbBaseBlueprint_SelectedIndexChanged;
            txtFilterBaseBlueprint.TextChanged += txtFilterBaseBlueprint_TextChanged;
            chkGlobalBlueprint.CheckedChanged += chkGlobalBlueprint_CheckedChanged;

            // Wire command buttons
            btnNew.Click += btnNew_Click;
            btnSave.Click += btnSave_Click;
            btnDelete.Click += btnDelete_Click;

            // Subscribe to data events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged += OnBlueprintDataChanged;
            playerContext.PricingDataChanged += OnPricingDataChanged;

            // Layout handlers
            flpSearchList.Layout += flpSearchList_Layout;
            flpBlueprintData.Layout += flpBlueprintData_Layout;

            // Initial population
            RefreshBlueprintList();
            UpdateTitleBarCounts();
        }

        // -----------------------------------------------------------------------
        // Initialization
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates filter combos with local copies (not shared BindingSources)
        /// so filter selections don't affect the detail-panel combos.
        /// Each combo has a blank first entry representing "no filter".
        /// </summary>
        private void InitFilterCombos()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            // Type filter
            cmbFilterType.Items.Add("");
            foreach (BlueprintType bt in empireContext.BlueprintTypeList)
                cmbFilterType.Items.Add(bt.Name);
            cmbFilterType.SelectedIndex = 0;

            // Class filter
            cmbFilterClass.Items.Add("");
            foreach (ShipClass sc in empireContext.ShipClassList)
                cmbFilterClass.Items.Add(sc.Name);
            cmbFilterClass.SelectedIndex = 0;

            // TechLevel filter
            cmbFilterTechLevel.Items.Add("");
            foreach (TechLevel tl in empireContext.TechLevelList)
                cmbFilterTechLevel.Items.Add(tl.Name);
            cmbFilterTechLevel.SelectedIndex = 0;

            // Evolution filter
            cmbFilterEvolution.Items.Add("");
            foreach (string evo in empireContext.EvolutionList)
                cmbFilterEvolution.Items.Add(evo);
            cmbFilterEvolution.SelectedIndex = 0;
        }

        /// <summary>
        /// Configures detail-panel combos with shared BindingSources.
        /// </summary>
        private void InitDetailCombos()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            cmbBlueprintType.DisplayMember = "Name";
            cmbBlueprintType.ValueMember = "Id";
            cmbBlueprintType.DataSource = empireContext.BindingSourceBlueprintType;
            cmbBlueprintType.SelectedIndex = -1;

            cmbShipClass.DisplayMember = "Name";
            cmbShipClass.ValueMember = "Id";
            cmbShipClass.DataSource = empireContext.BindingSourceShipClass;
            cmbShipClass.SelectedIndex = -1;

            cmbTechLevel.DisplayMember = "Name";
            cmbTechLevel.ValueMember = "Name";
            cmbTechLevel.DataSource = empireContext.BindingSourceTechLevel;
            cmbTechLevel.SelectedIndex = -1;

            cmbEvolution.DisplayMember = "Name";
            cmbEvolution.ValueMember = "Name";
            cmbEvolution.DataSource = empireContext.BindingSourceEvolution;
            cmbEvolution.SelectedIndex = 0;

            cmbBaseBlueprint.DisplayMember = "ExtendedName";
            cmbBaseBlueprint.ValueMember = "UUID";
        }

        // -----------------------------------------------------------------------
        // Blueprint List (Task 2.5)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Reads all filter controls, builds a BlueprintFilterCriteria, and populates the list.
        /// </summary>
        private void RefreshBlueprintList()
        {
            string nameFilter = txtFilter.Text;

            var criteria = new BlueprintFilterCriteria();

            if (cmbFilterType.SelectedIndex > 0)
            {
                string typeName = (string)cmbFilterType.SelectedItem;
                var bt = empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == typeName);
                if (bt != null) criteria.BlueprintTypeId = bt.Id;
            }

            if (cmbFilterClass.SelectedIndex > 0)
            {
                string className = (string)cmbFilterClass.SelectedItem;
                var sc = empireContext.ShipClassList.FirstOrDefault(s => s.Name == className);
                if (sc != null) criteria.ShipClassId = sc.Id;
            }

            if (cmbFilterTechLevel.SelectedIndex > 0)
            {
                criteria.TechLevelName = (string)cmbFilterTechLevel.SelectedItem;
            }

            if (cmbFilterEvolution.SelectedIndex > 0)
            {
                string evoStr = (string)cmbFilterEvolution.SelectedItem;
                if (int.TryParse(evoStr, out int evo))
                    criteria.Evolution = evo;
                if (chkFilterEvoAndAbove.Checked)
                    criteria.EvolutionAndAbove = true;
            }

            var results = viewModel.GetFilteredBlueprints(nameFilter, criteria);
            PopulateListView(results);
            UpdateTitleBarCounts();
        }

        /// <summary>
        /// Creates a BlueprintReferenceCounter from the current context data.
        /// </summary>
        private static BlueprintReferenceCounter CreateReferenceCounter()
        {
            var pc = PlayerContext.GetInstance();
            var ec = EmpireContext.GetInstance();

            var colonies = pc?.ColonyList as IEnumerable<Colony> ?? Enumerable.Empty<Colony>();
            var allBlueprints = new List<Blueprint>();
            if (pc?.BlueprintList != null) allBlueprints.AddRange(pc.BlueprintList);
            if (ec?.GlobalBlueprintList != null) allBlueprints.AddRange(ec.GlobalBlueprintList);
            var surveys = pc?.SurveyList as IEnumerable<Survey> ?? Enumerable.Empty<Survey>();

            return new BlueprintReferenceCounter(colonies, allBlueprints, surveys);
        }

        /// <summary>
        /// Populates the ListView with the provided blueprints.
        /// </summary>
        private void PopulateListView(IReadOnlyList<Blueprint> blueprints)
        {
            if (blueprints == null) return;

            var counter = CreateReferenceCounter();
            lvwBlueprints.BeginUpdate();
            lvwBlueprints.Items.Clear();

            foreach (Blueprint bp in blueprints)
            {
                var item = new ListViewItem(bp.BluePrintType ?? "");
                item.Tag = bp;
                item.SubItems.Add(bp.Name ?? "");
                item.SubItems.Add(bp.TechLevel ?? "");
                item.SubItems.Add(bp.Evolution.ToString());
                item.SubItems.Add(bp.NickName ?? "");
                item.SubItems.Add(counter.CountReferences(bp.UUID).TotalCount.ToString());
                lvwBlueprints.Items.Add(item);
            }

            lvwBlueprints.EndUpdate();
        }

        /// <summary>
        /// Selects the blueprint with the given UUID in the list view and scrolls it into view.
        /// </summary>
        private void SelectBlueprintInList(string uuid)
        {
            foreach (ListViewItem item in lvwBlueprints.Items)
            {
                if ((item.Tag as Blueprint)?.UUID == uuid)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private void lvwBlueprints_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
        }

        private void btnClearFilters_Click(object sender, EventArgs e)
        {
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                cmbFilterType.SelectedIndex = 0;
                cmbFilterClass.SelectedIndex = 0;
                cmbFilterTechLevel.SelectedIndex = 0;
                cmbFilterEvolution.SelectedIndex = 0;
                chkFilterEvoAndAbove.Checked = false;
            }
            RefreshBlueprintList();
        }

        // -----------------------------------------------------------------------
        // List Selection -> Populate Form
        // -----------------------------------------------------------------------

        private void lvwBlueprints_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwBlueprints.SelectedItems.Count == 1)
            {
                var blueprint = lvwBlueprints.SelectedItems[0].Tag as Blueprint;
                viewModel.SelectBlueprint(blueprint);

                // Update delete button state
                var counter = CreateReferenceCounter();
                var report = counter.CountReferences(blueprint?.UUID);
                var (enabled, text) = GetDeleteButtonState(report);
                btnDelete.Enabled = enabled;
                btnDelete.Text = text;

                PopulateForm();
            }
            else
            {
                var (enabled, text) = GetDeleteButtonState(null);
                btnDelete.Enabled = enabled;
                btnDelete.Text = text;
            }
        }

        // -----------------------------------------------------------------------
        // New / Save / Delete (Task 2.6)
        // -----------------------------------------------------------------------

        private void btnNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwBlueprints.SelectedItems.Clear();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(viewModel.Name))
            {
                MessageBox.Show("Blueprint name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // All data is already in the model via write-through — just persist
            viewModel.Save(chkGlobalBlueprint.Checked);

            RefreshBlueprintList();
            SelectBlueprintInList(viewModel.Data.UUID);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!btnDelete.Enabled) return;
            if (viewModel.Data.UUID == null) return;

            var result = MessageBox.Show(
                $"Delete blueprint '{viewModel.Data.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            viewModel.Delete();
            viewModel.Reset();
            RefreshBlueprintList();
            lvwBlueprints.SelectedItems.Clear();
            ClearForm();
        }

        // -----------------------------------------------------------------------
        // Identity Field Write-Through (Task 2.7)
        // -----------------------------------------------------------------------

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Name = txtName.Text;
        }

        private void txtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.NickName = txtNickName.Text;
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Description = txtDescription.Text;
        }

        private void txtCopyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int.TryParse(txtCopyCost.Text, out int copyCost);
            viewModel.CopyCost = copyCost;
        }

        private void cmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null) viewModel.BluePrintType = bt.Id;

            // Hide ShipClass and TechLevel for Universal types
            UpdateUniversalVisibility(bt);
        }

        private void cmbShipClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var sc = cmbShipClass.SelectedItem as ShipClass;
            viewModel.Class = sc != null ? sc.Id : 0;
        }

        private void cmbTechLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var tl = cmbTechLevel.SelectedItem as TechLevel;
            viewModel.TechLevel = tl?.Name;
        }

        private void cmbEvolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string evo = cmbEvolution.SelectedItem as string ?? cmbEvolution.Text ?? "0";
            int.TryParse(evo, out int ev);
            viewModel.Evolution = ev;
        }

        private void cmbBaseBlueprint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bp = cmbBaseBlueprint.SelectedItem as Blueprint;
            viewModel.BaseBlueprintUUID = bp?.UUID ?? "";
        }

        private void txtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.DroppedDown = true;
        }

        private void chkGlobalBlueprint_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Global flag is read at save time — no viewModel field to write.
        }

        /// <summary>
        /// Hides ShipClass and TechLevel panels for Universal blueprint types.
        /// </summary>
        private void UpdateUniversalVisibility(BlueprintType bt)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            if (bt != null && bt.Universal)
            {
                flpClassRow.Visible = false;
                cmbShipClass.SelectedIndex = -1;
                flpTechLevelRow.Visible = false;
                cmbTechLevel.SelectedIndex = -1;
            }
            else
            {
                flpClassRow.Visible = true;
                flpTechLevelRow.Visible = true;
            }
        }

        /// <summary>
        /// Updates the base blueprint combo with candidates filtered by current blueprint fields.
        /// </summary>
        private void UpdateBaseBlueprintList()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            string searchText = txtFilterBaseBlueprint.Text;
            var candidates = new List<Blueprint>(viewModel.GetBaseBlueprintCandidates(searchText));

            // Add empty entry at top to allow deselecting
            candidates.Insert(0, new Blueprint());

            var bs = new BindingSource();
            bs.DataSource = candidates;
            cmbBaseBlueprint.DataSource = bs;
        }

        // -----------------------------------------------------------------------
        // Delete Protection (Task 2.8)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the enabled state and text for the delete button based on reference count.
        /// </summary>
        internal static (bool enabled, string text) GetDeleteButtonState(ReferenceReport report)
        {
            if (report == null)
                return (false, "Delete");

            if (report.TotalCount > 0)
                return (false, $"In Use ({report.TotalCount})");

            return (true, "Delete");
        }

        // -----------------------------------------------------------------------
        // Dynamic Title Bar (Task 2.9)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Formats the title bar text with global and player blueprint counts.
        /// </summary>
        public static string FormatTitleBar(int globalCount, int playerCount)
        {
            return $"Blueprints - Global: {globalCount} Player: {playerCount}";
        }

        /// <summary>
        /// Updates the form's title bar with current global and player blueprint counts.
        /// </summary>
        private void UpdateTitleBarCounts()
        {
            int globalCount = empireContext.GlobalBlueprintList?.Count ?? 0;
            int playerCount = 0;
            if (!string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                playerCount = playerContext.GetCurrentPlayerBlueprints().Count;
            }
            this.Text = FormatTitleBar(globalCount, playerCount);
        }

        // -----------------------------------------------------------------------
        // Form Population / Clear
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates all form fields from the current viewModel.
        /// </summary>
        private void PopulateForm()
        {
            if (viewModel.Data.UUID == null) return;

            using var guard = new ProgrammaticUpdateGuard(this);

            // Identity fields
            txtName.Text = viewModel.Data.Name ?? "";
            txtNickName.Text = viewModel.Data.NickName ?? "";
            txtDescription.Text = viewModel.Data.Description ?? "";
            txtCopyCost.Text = viewModel.Data.CopyCost.ToString();

            // Blueprint type
            cmbBlueprintType.SelectedItem = empireContext.FindBlueprintType(viewModel.Data.BluePrintType);
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            UpdateUniversalVisibility(bt);

            // Dependent combos
            cmbShipClass.SelectedItem = empireContext.FindShipClass(viewModel.Data.Class);
            cmbTechLevel.SelectedItem = empireContext.FindTechLevel(viewModel.Data.TechLevel);
            cmbEvolution.SelectedItem = empireContext.FindEvolution(viewModel.Data.Evolution);

            // Base blueprint
            txtFilterBaseBlueprint.Text = "";
            UpdateBaseBlueprintList();
            if (!string.IsNullOrEmpty(viewModel.Data.BaseBlueprintUUID))
                cmbBaseBlueprint.SelectedValue = viewModel.Data.BaseBlueprintUUID;
            else
                cmbBaseBlueprint.SelectedIndex = 0;

            // Global checkbox
            chkGlobalBlueprint.Checked = viewModel.IsGlobal;
        }

        /// <summary>
        /// Clears all form controls and resets the viewModel.
        /// </summary>
        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            txtName.Text = "";
            txtNickName.Text = "";
            txtDescription.Text = "";
            txtCopyCost.Text = "";

            cmbBlueprintType.SelectedIndex = -1;
            cmbShipClass.SelectedIndex = -1;
            cmbTechLevel.SelectedIndex = -1;
            cmbEvolution.SelectedIndex = 0;

            txtFilterBaseBlueprint.Text = "";
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.SelectedIndex = -1;

            chkGlobalBlueprint.Checked = false;

            // Reset delete button
            btnDelete.Enabled = false;
            btnDelete.Text = "Delete";
        }

        // -----------------------------------------------------------------------
        // Data Events
        // -----------------------------------------------------------------------

        private void OnBlueprintDataChanged(object sender, BlueprintDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnBlueprintDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            if (viewModel.Data.UUID == e.BlueprintUUID)
            {
                PopulateForm();
            }
            RefreshBlueprintList();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            lvwBlueprints.Items.Clear();
            viewModel.Reset();
            ClearForm();
            RefreshBlueprintList();
        }

        private void OnPricingDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnPricingDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            // Pricing refresh will be implemented in Phase 5
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged -= OnBlueprintDataChanged;
            playerContext.PricingDataChanged -= OnPricingDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            // ListView fills remaining height after the text filter and filter panel
            int usedHeight = txtFilter.Height + txtFilter.Margin.Top + txtFilter.Margin.Bottom
                           + flpFilterPanel.Height + flpFilterPanel.Margin.Top + flpFilterPanel.Margin.Bottom;
            int availableHeight = flpSearchList.ClientSize.Height - usedHeight
                                - lvwBlueprints.Margin.Top - lvwBlueprints.Margin.Bottom
                                - flpSearchList.Padding.Top - flpSearchList.Padding.Bottom;
            int availableWidth = flpSearchList.ClientSize.Width
                               - lvwBlueprints.Margin.Left - lvwBlueprints.Margin.Right
                               - flpSearchList.Padding.Left - flpSearchList.Padding.Right;
            lvwBlueprints.Size = new System.Drawing.Size(
                Math.Max(100, availableWidth),
                Math.Max(100, availableHeight));
        }

        private void flpBlueprintData_Layout(object sender, LayoutEventArgs e)
        {
            // TabControl fills remaining height after identity, commands, and pricing
            int usedHeight = flpIdentity.Height + flpIdentity.Margin.Top + flpIdentity.Margin.Bottom
                           + flpCommands.Height + flpCommands.Margin.Top + flpCommands.Margin.Bottom
                           + flpPricing.Height + flpPricing.Margin.Top + flpPricing.Margin.Bottom;
            int tabHeight = flpBlueprintData.ClientSize.Height - usedHeight
                          - tabDetailedData.Margin.Top - tabDetailedData.Margin.Bottom
                          - flpBlueprintData.Padding.Top - flpBlueprintData.Padding.Bottom;
            int tabWidth = flpBlueprintData.ClientSize.Width
                         - tabDetailedData.Margin.Left - tabDetailedData.Margin.Right
                         - flpBlueprintData.Padding.Left - flpBlueprintData.Padding.Right;
            tabDetailedData.Size = new System.Drawing.Size(
                Math.Max(100, tabWidth),
                Math.Max(100, tabHeight));
        }
    }
}
