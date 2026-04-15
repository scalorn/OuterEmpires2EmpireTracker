using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
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

        // Statistics grid structure cache key: "{typeId}|{extraKeysHash}"
        private string _cachedGridKey;

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
            btnImport.Click += btnImport_Click;

            // Wire statistics grid events
            dgvStatistics.CellValueChanged += dgvStatistics_CellValueChanged;
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            dgvStatistics.CurrentCellDirtyStateChanged += dgvStatistics_CurrentCellDirtyStateChanged;

            // Configure resources grid combo
            colResource.DisplayMember = "Name";
            colResource.ValueMember = "Name";
            colResource.DataSource = empireContext.BindingSourceResource;

            // Wire resources grid events
            dgvResources.CellValueChanged += dgvResources_CellValueChanged;
            dgvResources.CellValidating += dgvResources_CellValidating;
            btnAddResource.Click += btnAddResource_Click;
            btnDeleteResource.Click += btnDeleteResource_Click;

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
        // Individual Import (Task 4.1)
        // -----------------------------------------------------------------------

        private void btnImport_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML found on clipboard. Copy the blueprint page from the game first.",
                    "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Validate clipboard contains blueprint data (individual or resources-only)
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string htmlFragment = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                var detected = ClipboardContentDetector.Detect(htmlFragment);
                if (detected != ClipboardContentDetector.ContentType.Blueprint &&
                    detected != ClipboardContentDetector.ContentType.Survey &&
                    detected != ClipboardContentDetector.ContentType.Unknown)
                {
                    string found = ClipboardContentDetector.GetDescription(detected);
                    MessageBox.Show($"The clipboard contains {found}, not blueprint data.\n\nCopy the blueprint page from the game browser first.",
                        "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var scanner = new BlueprintScanner();
                var tempBP = scanner.ParseClipboardToTemp();

                if (tempBP == null)
                    return;

                BlueprintImportHandler.LogParsedBlueprint(tempBP);

                // Classify the import via the shared service
                var importType = BlueprintImportHandler.ClassifyImport(tempBP);
                Log.Info("  ImportType: {0}", importType);

                // Resources-only import: merge into selected blueprint
                if (importType == BlueprintImportHandler.ImportType.ResourcesOnly)
                {
                    if (string.IsNullOrEmpty(viewModel.Data.UUID))
                    {
                        MessageBox.Show(
                            "Please select or import a blueprint first, then import the resources tab.",
                            "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    MarketBlueprintImporter.MergeResourcesOnly(viewModel.Data, tempBP);

                    // Persist to the correct list
                    bool resGlobal = empireContext.GlobalBlueprintList.Any(b => b.UUID == viewModel.Data.UUID);
                    if (resGlobal)
                        empireContext.WriteContext();
                    else
                        playerContext.WriteContext();

                    // Notify, refresh, re-select
                    playerContext.OnBlueprintDataChanged(viewModel.Data.UUID);
                    RefreshBlueprintList();
                    SelectBlueprintInList(viewModel.Data.UUID);
                    PopulateForm();
                    Log.Info("Resources-only import merged into selected blueprint: {0} UUID={1}",
                        viewModel.Data.Name, viewModel.Data.UUID);
                    return;
                }

                // NoName fallback: use scanner.ProcessClipboard directly
                if (importType == BlueprintImportHandler.ImportType.NoName)
                {
                    Log.Warn("  No name parsed from clipboard -- using fallback direct import");
                    scanner.ProcessClipboard(viewModel.Data);
                    if (string.IsNullOrEmpty(viewModel.Data.UUID))
                    {
                        bool fallbackGlobal = viewModel.Data.Evolution == 0
                            && string.IsNullOrEmpty(viewModel.Data.OwnerUUID);
                        viewModel.Data.UUID = fallbackGlobal
                            ? DeterministicUUID.Generate(viewModel.Data)
                            : Guid.NewGuid().ToString();
                    }
                    PopulateForm();
                    Log.Info("Blueprint imported from clipboard (fallback, no name parsed)");
                    return;
                }

                // Full import — delegate routing and merge to BlueprintImportHandler
                var findResult = BlueprintImportHandler.FindTarget(
                    tempBP, viewModel.Data, playerContext, empireContext);

                var importedBP = BlueprintImportHandler.MergeAndPersist(
                    findResult, tempBP, playerContext, empireContext);

                // Refresh UI, select imported blueprint
                viewModel.SelectBlueprint(importedBP);
                RefreshBlueprintList();
                SelectBlueprintInList(importedBP.UUID);
                PopulateForm();

                // Auto-select best base blueprint match
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    if (string.IsNullOrEmpty(importedBP.BaseBlueprintUUID) && cmbBaseBlueprint.Items.Count > 1)
                    {
                        cmbBaseBlueprint.SelectedIndex = 1;
                        var bp = cmbBaseBlueprint.SelectedItem as Models.Blueprint;
                        viewModel.BaseBlueprintUUID = bp?.UUID ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing blueprint from clipboard");
                MessageBox.Show("Failed to import blueprint: " + ex.Message,
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            // Rebuild statistics grid for the new type
            RefreshStatisticsGrid();
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
        // Statistics Grid (Tasks 3.1–3.4)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the statistics grid structure if the BlueprintType or extra PropertyBag
        /// keys have changed, then populates values. Uses caching key "{typeId}|{extraKeys}"
        /// to avoid unnecessary rebuilds when only values change.
        /// </summary>
        private void RefreshStatisticsGrid()
        {
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            string[] definedProps = bt?.Properties ?? Array.Empty<string>();

            // Find extra properties in PropertyBag not in the type definition
            var definedSet = new HashSet<string>(definedProps, StringComparer.Ordinal);
            var extraProps = viewModel.Data.Properties.Properties.Keys
                .Where(k => !definedSet.Contains(k) && !k.StartsWith("_"))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string gridKey = (bt?.Id ?? "") + "|" + string.Join(",", extraProps);

            if (gridKey != _cachedGridKey)
            {
                RebuildStatisticsGrid(definedProps, extraProps);
                _cachedGridKey = gridKey;
            }

            PopulateStatisticsValues();
        }

        /// <summary>
        /// Rebuilds the statistics grid rows from scratch. Defined properties first,
        /// then extra/unknown properties. Handles CheckBox, ComboBox, and text cell types.
        /// </summary>
        private void RebuildStatisticsGrid(string[] definedProps, string[] extraProps)
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            try { dgvStatistics.EndEdit(); } catch { }
            dgvStatistics.Rows.Clear();
            dgvStatistics.Columns.Clear();

            // Rebuild columns: Property (read-only text) + CurrentValue (editable)
            var colProp = new DataGridViewTextBoxColumn
            {
                Name = "Property",
                HeaderText = "Property",
                ReadOnly = true,
                Width = 180
            };
            dgvStatistics.Columns.Add(colProp);

            // CurrentValue column — placeholder, cells are swapped per-row below
            var colVal = new DataGridViewTextBoxColumn
            {
                Name = "CurrentValue",
                HeaderText = "Value",
                Width = 150
            };
            dgvStatistics.Columns.Add(colVal);

            // Add rows for defined properties
            foreach (string property in definedProps)
            {
                AddStatisticsRow(property);
            }

            // Add rows for extra/unknown properties (logged at WARN)
            foreach (string property in extraProps)
            {
                Log.Warn("Extra property '{0}' on '{1}' (not in {2} type definition)",
                    property, viewModel.Data.Name ?? "(new)", 
                    (cmbBlueprintType.SelectedItem as BlueprintType)?.Id ?? "unknown");
                AddStatisticsRow(property);
            }

            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
        }

        /// <summary>
        /// Adds a single row to the statistics grid, swapping the CurrentValue cell
        /// to CheckBox or ComboBox as needed based on BlueprintPropertyValidation.
        /// </summary>
        private void AddStatisticsRow(string property)
        {
            int rowIndex = dgvStatistics.Rows.Add();
            var row = dgvStatistics.Rows[rowIndex];
            row.Cells["Property"].Value = property;
            row.Cells["Property"].Tag = property;

            var propType = BlueprintPropertyValidation.GetPropertyType(property);
            if (propType == PropertyValueType.ComboBox)
            {
                var comboCell = new DataGridViewComboBoxCell();
                comboCell.DataSource = BlueprintPropertyValidation.GetComboBoxDataSource(property);
                comboCell.FlatStyle = FlatStyle.Flat;
                row.Cells["CurrentValue"] = comboCell;
            }
            else if (propType == PropertyValueType.CheckBox)
            {
                var checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = false;
                row.Cells["CurrentValue"] = checkCell;
            }
        }

        /// <summary>
        /// Reads values from the PropertyBag and fills the statistics grid cells.
        /// </summary>
        private void PopulateStatisticsValues()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string property = row.Cells["Property"].Tag as string;
                if (string.IsNullOrEmpty(property)) continue;

                viewModel.GetProperty(property, "", out string value);
                if (value == null) value = "";

                var propType = BlueprintPropertyValidation.GetPropertyType(property);
                if (propType == PropertyValueType.CheckBox)
                {
                    bool.TryParse(value, out bool boolVal);
                    row.Cells["CurrentValue"].Value = boolVal;
                }
                else
                {
                    row.Cells["CurrentValue"].Value = value;
                }
            }
        }

        /// <summary>
        /// Write-through: non-empty values written to PropertyBag, empty clears the key.
        /// </summary>
        private void dgvStatistics_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;

            // Only handle the CurrentValue column
            var col = dgvStatistics.Columns[e.ColumnIndex];
            if (col.Name != "CurrentValue") return;

            string propName = dgvStatistics.Rows[e.RowIndex].Cells["Property"].Tag as string;
            if (string.IsNullOrEmpty(propName)) return;

            object cellValue = dgvStatistics.Rows[e.RowIndex].Cells["CurrentValue"].Value;
            string strValue = cellValue is bool ? cellValue.ToString() : cellValue as string;

            if (string.IsNullOrEmpty(strValue))
                viewModel.Data.Properties.Remove(propName);
            else
                viewModel.SetProperty(propName, strValue);
        }

        /// <summary>
        /// Validates cell input using BlueprintPropertyValidation patterns.
        /// </summary>
        private void dgvStatistics_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var col = dgvStatistics.Columns[e.ColumnIndex];
            if (col.Name != "CurrentValue") return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(value)) return; // Allow empty

            string propertyName = dgvStatistics.Rows[e.RowIndex].Cells["Property"].Tag as string;
            if (string.IsNullOrEmpty(propertyName)) return;

            string pattern = BlueprintPropertyValidation.GetValidationPattern(propertyName);
            if (pattern == null) return; // Unknown — no validation

            if (!Regex.IsMatch(value, pattern))
            {
                e.Cancel = true;
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                var propType = BlueprintPropertyValidation.GetPropertyType(propertyName);
                dgvStatistics.Rows[e.RowIndex].ErrorText = $"{propertyName} must be a valid {propType}";
            }
            else
            {
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvStatistics.Rows[e.RowIndex].ErrorText = "";
            }
        }

        /// <summary>
        /// Commits CheckBox and ComboBox edits immediately so CellValueChanged fires.
        /// </summary>
        private void dgvStatistics_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvStatistics.IsCurrentCellDirty)
                dgvStatistics.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // -----------------------------------------------------------------------
        // Resources Grid (Tasks 3.5–3.6)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates the resources grid from Blueprint.Resources.
        /// </summary>
        private void PopulateResourcesGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;

            foreach (var resource in viewModel.GetResources())
            {
                int rowIndex = dgvResources.Rows.Add();
                var row = dgvResources.Rows[rowIndex];
                row.Cells["colResource"].Value = resource.Key;
                row.Cells["colAmount"].Value = resource.Value;
            }
        }

        /// <summary>
        /// Write-through: resource edits written to Blueprint.Resources immediately.
        /// </summary>
        private void dgvResources_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;

            string resourceName = dgvResources.Rows[e.RowIndex].Cells["colResource"].Value as string;
            string amount = dgvResources.Rows[e.RowIndex].Cells["colAmount"].Value as string;

            if (!string.IsNullOrEmpty(resourceName))
                viewModel.SetResource(resourceName, amount ?? "0");
        }

        /// <summary>
        /// Validates the Amount column as integer.
        /// </summary>
        private void dgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var col = dgvResources.Columns[e.ColumnIndex];
            if (col.Name != "colAmount") return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!int.TryParse(value, out _))
            {
                e.Cancel = true;
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                dgvResources.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
            }
            else
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
            }
        }

        /// <summary>
        /// Adds an empty row and a placeholder entry in Blueprint.Resources.
        /// </summary>
        private void btnAddResource_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvResources.Rows.Add();
            dgvResources.Rows[rowIndex].Cells["colAmount"].Value = "0";
        }

        /// <summary>
        /// Removes the selected row and its entry from Blueprint.Resources.
        /// </summary>
        private void btnDeleteResource_Click(object sender, EventArgs e)
        {
            if (dgvResources.CurrentRow == null) return;
            int rowIndex = dgvResources.CurrentRow.Index;

            string resourceName = dgvResources.Rows[rowIndex].Cells["colResource"].Value as string;
            if (!string.IsNullOrEmpty(resourceName))
                viewModel.Data.Resources.Remove(resourceName);

            dgvResources.Rows.RemoveAt(rowIndex);
        }

        /// <summary>
        /// Clears the resources grid.
        /// </summary>
        private void ClearResourcesGrid()
        {
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;
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

            // Statistics and Resources grids
            RefreshStatisticsGrid();
            PopulateResourcesGrid();
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

            // Clear grids
            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            try { dgvStatistics.EndEdit(); } catch { }
            dgvStatistics.Rows.Clear();
            dgvStatistics.Columns.Clear();
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            _cachedGridKey = null;

            ClearResourcesGrid();

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
