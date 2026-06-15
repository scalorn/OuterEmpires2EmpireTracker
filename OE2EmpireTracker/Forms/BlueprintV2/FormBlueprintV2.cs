using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker
{
    /// <summary>
    /// FormBlueprintV2 â€” Clean rewrite of the blueprint management form.
    /// Built around write-through: the data model (PropertyBag, Resources) is always
    /// the source of truth. The grid is a view, not a store.
    /// </summary>
    public partial class FormBlueprintV2 : Form, IProgrammaticUpdateSource
    {
        /// <summary>
        /// Extended 16-color Wong palette (8 base + 8 lighter tints) for colorblind-friendly chart lines.
        /// Delegates to the shared <see cref="ChartColors.WongPalette"/> constant.
        /// </summary>
        internal static readonly Color[] WongPalette = ChartColors.WongPalette;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private EmpireContext empireContext;

        private PlayerContext playerContext;

        private BlueprintViewModel viewModel;

        private BlueprintService _blueprintService;

        // ListView sorting state
        private int _sortColumn = 0;

        private SortOrder _sortOrder = SortOrder.Ascending;

        // Statistics grid structure cache key: "{typeId}|{extraKeysHash}"
        private string _cachedGridKey;

        // Parallel list of ReadOnlyBlueprint objects for base blueprint UUID lookup via SelectedFullIndex
        private List<ReadOnlyBlueprint> _baseBlueprintList = new List<ReadOnlyBlueprint>();

        // Parallel list of pricing plan UUIDs for SelectedFullIndex lookup
        private List<string> _pricingPlanList = new List<string>();

        // Tracks the previously selected blueprint UUID for unsaved-changes cancel/restore
        private string _previousSelectedUUID;

        public FormBlueprintV2()
        {
            InitializeComponent();

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new BlueprintViewModel(new Blueprint(), playerContext);
            _blueprintService = new BlueprintService(playerContext, empireContext);

            InitFilterCombos();
            InitDetailCombos();

            // ListView sorting
            lvwBlueprints.ColumnClick += LvwBlueprints_ColumnClick;
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);

            // Wire filter events
            txtFilter.TextChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterType.SelectedItemChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterClass.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterTechLevel.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterEvolution.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            chkFilterEvoAndAbove.CheckedChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            btnClearFilters.Click += BtnClearFilters_Click;

            // Wire list selection
            lvwBlueprints.SelectedIndexChanged += LvwBlueprints_SelectedIndexChanged;

            // Wire identity field write-through
            txtName.TextChanged += TxtName_TextChanged;
            txtNickName.TextChanged += TxtNickName_TextChanged;
            txtDescription.TextChanged += TxtDescription_TextChanged;
            txtCopyCost.TextChanged += TxtCopyCost_TextChanged;
            cmbBlueprintType.SelectedItemChanged += CmbBlueprintType_SelectedItemChanged;
            cmbShipClass.SelectedIndexChanged += CmbShipClass_SelectedIndexChanged;
            cmbTechLevel.SelectedIndexChanged += CmbTechLevel_SelectedIndexChanged;
            cmbEvolution.SelectedIndexChanged += CmbEvolution_SelectedIndexChanged;
            cmbBaseBlueprint.SelectedItemChanged += CmbBaseBlueprint_SelectedItemChanged;
            chkGlobalBlueprint.CheckedChanged += ChkGlobalBlueprint_CheckedChanged;

            // Wire command buttons
            btnNew.Click += BtnNew_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnImport.Click += BtnImport_Click;
            btnImportMarket.Click += BtnImportMarket_Click;
            btnImportCrate.Click += BtnImportCrate_Click;

            // Wire statistics grid events
            dgvStatistics.CellValueChanged += DgvStatistics_CellValueChanged;
            dgvStatistics.CellValidating += DgvStatistics_CellValidating;
            dgvStatistics.CurrentCellDirtyStateChanged += DgvStatistics_CurrentCellDirtyStateChanged;

            // Configure resources grid combo
            var resourceNameList = empireContext.ResourceList.Select(r => r.Name).ToList();
            colResource.Items = resourceNameList;

            dgvResources.DataError += (s, ev) =>
            {
                Log.Warn("dgvResources DataError at [{0}, {1}]: {2}", ev.RowIndex, ev.ColumnIndex, ev.Exception?.Message);
                ev.ThrowException = false;
            };
            dgvStatistics.DataError += (s, ev) =>
            {
                Log.Warn("dgvStatistics DataError at [{0}, {1}]: {2}", ev.RowIndex, ev.ColumnIndex, ev.Exception?.Message);
                ev.ThrowException = false;
            };

            // Wire resources grid events
            dgvResources.CellValueChanged += DgvResources_CellValueChanged;
            dgvResources.CellValidating += DgvResources_CellValidating;
            btnAddResource.Click += BtnAddResource_Click;
            btnDeleteResource.Click += BtnDeleteResource_Click;
            btnAddStatistic.Click += BtnAddStatistic_Click;
            btnDeleteStatistic.Click += BtnDeleteStatistic_Click;

            // Wire context menu events
            tsmiAddStatistic.Click += BtnAddStatistic_Click;
            tsmiDeleteStatistic.Click += BtnDeleteStatistic_Click;
            tsmiAddResource.Click += BtnAddResource_Click;
            tsmiDeleteResource.Click += BtnDeleteResource_Click;

            // Configure pricing plan combo
            PopulatePricingPlanCombo();
            cmbPricingPlan.SelectedItemChanged += CmbPricingPlan_SelectedItemChanged;

            // Subscribe to data events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged += OnBlueprintDataChanged;
            playerContext.PricingDataChanged += OnPricingDataChanged;

            // Layout handlers
            flpSearchList.Layout += FlpSearchList_Layout;
            flpBlueprintData.Layout += FlpBlueprintData_Layout;

            // Initial population
            RefreshBlueprintList();
            UpdateTitleBarCounts();
        }

        // -----------------------------------------------------------------------
        // Delete Protection (Task 2.8)
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // Dynamic Title Bar (Task 2.9)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Formats the title bar text with global and player blueprint counts.
        /// </summary>
        public static string FormatTitleBar(int windowNumber, int globalCount, int playerCount)
        {
            return $"#{windowNumber} - Blueprints - Global: {globalCount} Player: {playerCount}";
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    viewModel.Save(chkGlobalBlueprint.Checked);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged -= OnBlueprintDataChanged;
            playerContext.PricingDataChanged -= OnPricingDataChanged;
            base.OnFormClosed(e);
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
            var buildPlans = pc?.BuildPlanList as IEnumerable<BuildPlan> ?? Enumerable.Empty<BuildPlan>();
            var shipTemplates = pc?.ShipTemplateList as IEnumerable<ShipTemplate> ?? Enumerable.Empty<ShipTemplate>();
            var ships = pc?.ShipList as IEnumerable<Ship> ?? Enumerable.Empty<Ship>();
            var stations = pc?.StationList as IEnumerable<Station> ?? Enumerable.Empty<Station>();

            var marketListings = pc?.MarketListingList as IEnumerable<MarketListing> ?? Enumerable.Empty<MarketListing>();

            return new BlueprintReferenceCounter(colonies, allBlueprints, surveys, buildPlans, shipTemplates, ships, stations, marketListings);
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
            var filterTypeItems = new List<string>();
            filterTypeItems.Add(string.Empty);
            foreach (BlueprintType bt in empireContext.BlueprintTypeList)
                filterTypeItems.Add(bt.Name);
            cmbFilterType.SetItems(filterTypeItems, string.Empty);

            // Class filter
            cmbFilterClass.Items.Add(string.Empty);
            foreach (ShipClass sc in empireContext.ShipClassList)
                cmbFilterClass.Items.Add(sc.Name);
            cmbFilterClass.SelectedIndex = 0;

            // TechLevel filter
            cmbFilterTechLevel.Items.Add(string.Empty);
            foreach (TechLevel tl in empireContext.TechLevelList)
                cmbFilterTechLevel.Items.Add(tl.Name);
            cmbFilterTechLevel.SelectedIndex = 0;

            // Evolution filter
            cmbFilterEvolution.Items.Add(string.Empty);
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

            var bpTypeItems = new List<string>();
            foreach (BlueprintType bt in empireContext.BlueprintTypeList)
                bpTypeItems.Add(bt.Name);
            cmbBlueprintType.SetItems(bpTypeItems, null);

            cmbShipClass.DisplayMember = "Name";
            cmbShipClass.ValueMember = "Id";
            cmbShipClass.DataSource = empireContext.ShipClassList.ToList();
            cmbShipClass.SelectedIndex = -1;

            cmbTechLevel.DisplayMember = "Name";
            cmbTechLevel.ValueMember = "Name";
            cmbTechLevel.DataSource = empireContext.TechLevelList.ToList();
            cmbTechLevel.SelectedIndex = -1;

            cmbEvolution.DisplayMember = string.Empty;
            cmbEvolution.ValueMember = string.Empty;
            cmbEvolution.DataSource = empireContext.EvolutionList.ToList();
            cmbEvolution.SelectedIndex = 0;
        }

        // -----------------------------------------------------------------------
        // Blueprint List (Task 2.5)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Reads all filter controls, builds a BlueprintFilterCriteria, and populates the list.
        /// </summary>
        private void RefreshBlueprintList()
        {
            var sw = Stopwatch.StartNew();
            string nameFilter = txtFilter.Text;

            var criteria = new BlueprintFilterCriteria();

            string filterTypeName = cmbFilterType.SelectedItem;
            if (!string.IsNullOrEmpty(filterTypeName))
            {
                string typeName = filterTypeName;
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

            long t1 = sw.ElapsedMilliseconds;
            var results = viewModel.GetFilteredBlueprints(nameFilter, criteria);
            long t2 = sw.ElapsedMilliseconds;
            PopulateListView(results);
            UpdateTitleBarCounts();
            sw.Stop();
            Log.Info(
                "RefreshBlueprintList PERF: total={0}ms filter={1}ms populate={2}ms results={3}",
                sw.ElapsedMilliseconds,
                t2 - t1,
                sw.ElapsedMilliseconds - t2,
                results.Count);
            sw.Stop();
            Log.Info("PERF RefreshBlueprintList: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Populates the ListView with the provided blueprints.
        /// </summary>
        private void PopulateListView(IReadOnlyList<ReadOnlyBlueprint> blueprints)
        {
            if (blueprints == null) return;

            var sw = Stopwatch.StartNew();
            var counter = CreateReferenceCounter();
            long t1 = sw.ElapsedMilliseconds;
            lvwBlueprints.BeginUpdate();
            lvwBlueprints.Items.Clear();

            foreach (ReadOnlyBlueprint bp in blueprints)
            {
                var item = new ListViewItem(bp.BluePrintType ?? string.Empty);
                item.Tag = bp;
                item.SubItems.Add(bp.Name ?? string.Empty);
                item.SubItems.Add(bp.TechLevel ?? string.Empty);
                item.SubItems.Add(bp.Evolution.ToString());
                item.SubItems.Add(bp.NickName ?? string.Empty);
                item.SubItems.Add(counter.CountReferences(bp.UUID).TotalCount.ToString());
                lvwBlueprints.Items.Add(item);
            }

            lvwBlueprints.EndUpdate();
            sw.Stop();
            Log.Info(
                "PopulateListView PERF: total={0}ms refCounter={1}ms listBuild={2}ms items={3}",
                sw.ElapsedMilliseconds,
                t1,
                sw.ElapsedMilliseconds - t1,
                blueprints.Count);
            sw.Stop();
            Log.Info("PERF PopulateListView: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Selects the blueprint with the given UUID in the list view and scrolls it into view.
        /// </summary>
        private void SelectBlueprintInList(string uuid)
        {
            foreach (ListViewItem item in lvwBlueprints.Items)
            {
                if ((item.Tag as ReadOnlyBlueprint)?.UUID == uuid)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private void LvwBlueprints_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
            {
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }

            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
        }

        private void BtnClearFilters_Click(object sender, EventArgs e)
        {
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                cmbFilterType.SetItems(cmbFilterType.Items, string.Empty);
                cmbFilterClass.SelectedIndex = 0;
                cmbFilterTechLevel.SelectedIndex = 0;
                cmbFilterEvolution.SelectedIndex = 0;
                chkFilterEvoAndAbove.Checked = false;
            }

            RefreshBlueprintList();
        }

        // -----------------------------------------------------------------------
        // Dirty Tracking / Unsaved Changes (Tasks 8â€“9)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Enables the Save button only when the ViewModel has unsaved changes.
        /// </summary>
        private void UpdateSaveButtonState()
        {
            btnSave.Enabled = viewModel.IsDirty;
        }

        /// <summary>
        /// Prompts the user to save, discard, or cancel when there are unsaved changes.
        /// Returns Yes (save), No (discard), or Cancel.
        /// </summary>
        private DialogResult PromptUnsavedChanges()
        {
            return MessageBox.Show(
                $"Save changes to '{viewModel.Name}'?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        // -----------------------------------------------------------------------
        // List Selection -> Populate Form
        // -----------------------------------------------------------------------

        private void LvwBlueprints_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwBlueprints.SelectedItems.Count == 1)
            {
                var readOnly = lvwBlueprints.SelectedItems[0].Tag as ReadOnlyBlueprint;
                string uuid = readOnly?.UUID;

                // Prompt for unsaved changes before switching
                if (viewModel.IsDirty)
                {
                    var result = PromptUnsavedChanges();
                    if (result == DialogResult.Yes)
                    {
                        viewModel.Save(chkGlobalBlueprint.Checked);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Restore previous selection
                        lvwBlueprints.SelectedIndexChanged -= LvwBlueprints_SelectedIndexChanged;
                        lvwBlueprints.SelectedItems.Clear();
                        if (!string.IsNullOrEmpty(_previousSelectedUUID))
                        {
                            foreach (ListViewItem item in lvwBlueprints.Items)
                            {
                                if ((item.Tag as ReadOnlyBlueprint)?.UUID == _previousSelectedUUID)
                                {
                                    item.Selected = true;
                                    item.EnsureVisible();
                                    break;
                                }
                            }
                        }

                        lvwBlueprints.SelectedIndexChanged += LvwBlueprints_SelectedIndexChanged;
                        return;
                    }

                    // DialogResult.No â€” discard, fall through to load new
                }

                if (readOnly != null)
                {
                    viewModel.LoadFrom(readOnly);
                }
                else
                {
                    viewModel.Reset();
                }

                _previousSelectedUUID = uuid;

                // Update delete button state
                var counter = CreateReferenceCounter();
                var report = counter.CountReferences(uuid);
                var (enabled, text) = GetDeleteButtonState(report);
                btnDelete.Enabled = enabled;
                btnDelete.Text = text;

                PopulateForm();
                UpdateSaveButtonState();
                RefreshEvolutionGraph();
                RefreshPriceEvolutionGraph();
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

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    viewModel.Save(chkGlobalBlueprint.Checked);
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            ClearForm();
            lvwBlueprints.SelectedItems.Clear();
            _previousSelectedUUID = null;
            UpdateSaveButtonState();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(viewModel.Name))
            {
                MessageBox.Show(
                    "Blueprint name is required.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            bool isGlobal = chkGlobalBlueprint.Checked;

            Log.Info(
                "BtnSave: persisting bp='{0}' UUID={1} type='{2}' class={3} tech='{4}' isNew={5}",
                viewModel.Name,
                viewModel.UUID,
                viewModel.BluePrintType ?? "(null)",
                viewModel.Class,
                viewModel.TechLevel ?? "(null)",
                viewModel.IsNew);

            ReadOnlyBlueprint result;
            if (viewModel.IsNew)
            {
                var request = viewModel.BuildCreateRequest();
                result = _blueprintService.Create(request, isGlobal);
            }
            else
            {
                // Handle global toggle: move between lists if needed
                bool wasGlobal = viewModel.IsGlobal;
                if (wasGlobal && !isGlobal)
                    _blueprintService.MoveToPlayer(viewModel.UUID);
                else if (!wasGlobal && isGlobal)
                    _blueprintService.MoveToGlobal(viewModel.UUID);

                var request = viewModel.BuildUpdateRequest();
                result = _blueprintService.Update(viewModel.UUID, request);
            }

            viewModel.LoadFrom(result);
            RefreshBlueprintList();
            SelectBlueprintInList(viewModel.UUID);
            _previousSelectedUUID = viewModel.UUID;
            UpdateSaveButtonState();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (!btnDelete.Enabled) return;
            if (viewModel.UUID == null) return;

            var result = MessageBox.Show(
                $"Delete blueprint '{viewModel.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _blueprintService.Delete(viewModel.UUID);
            viewModel.Reset();
            RefreshBlueprintList();
            lvwBlueprints.SelectedItems.Clear();
            ClearForm();
        }

        // -----------------------------------------------------------------------
        // Individual Import (Task 4.1)
        // -----------------------------------------------------------------------

        private void BtnImport_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show(
                    "No HTML found on clipboard. Copy the blueprint page from the game first.",
                    "Import",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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
                    MessageBox.Show(
                        $"The clipboard contains {found}, not blueprint data.\n\nCopy the blueprint page from the game browser first.",
                        "Wrong Content",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var scanner = new BlueprintScanner();

                Log.Info("Blueprint clipboard data length (temp parse): {0}", clipboardData.Length);

                var tempBP = new Blueprint();
                scanner.ProcessHtml(tempBP, htmlFragment);

                if (tempBP == null)
                    return;

                BlueprintImportHandler.LogParsedBlueprint(tempBP);

                // Classify the import via the shared service
                var importType = BlueprintImportHandler.ClassifyImport(tempBP);
                Log.Info("  ImportType: {0}", importType);

                // Resources-only import: merge into ViewModel local state, mark dirty
                if (importType == BlueprintImportHandler.ImportType.ResourcesOnly)
                {
                    if (string.IsNullOrEmpty(viewModel.UUID))
                    {
                        MessageBox.Show(
                            "Please select or import a blueprint first, then import the resources tab.",
                            "Import",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    // Merge parsed resources into ViewModel local state
                    if (tempBP.Resources != null)
                    {
                        foreach (var kvp in tempBP.Resources)
                            viewModel.SetResource(kvp.Key, kvp.Value);
                    }

                    // Merge parsed properties (if any) into ViewModel local state
                    if (tempBP.Properties != null && tempBP.Properties.Count > 0)
                    {
                        foreach (var kvp in tempBP.Properties.Properties)
                            viewModel.SetProperty(kvp.Key, kvp.Value);
                    }

                    // Refresh form to show merged data, update dirty state
                    PopulateForm();
                    UpdateSaveButtonState();
                    Log.Info(
                        "Resources-only import merged into ViewModel local state: {0} UUID={1}",
                        viewModel.Name,
                        viewModel.UUID);
                    return;
                }

                // NoName fallback: re-parse clipboard HTML into a temp Blueprint
                if (importType == BlueprintImportHandler.ImportType.NoName)
                {
                    Log.Warn("  No name parsed from clipboard -- using fallback direct import");
                    Blueprint fallbackBp = new Blueprint();

                    // Copy current ViewModel state into temp for ProcessHtml to merge into
                    if (!string.IsNullOrEmpty(viewModel.UUID))
                    {
                        fallbackBp.UUID = viewModel.UUID;
                        fallbackBp.Name = viewModel.Name;
                        fallbackBp.BluePrintType = viewModel.BluePrintType;
                        fallbackBp.Evolution = viewModel.Evolution;
                        fallbackBp.Class = viewModel.Class;
                        fallbackBp.TechLevel = viewModel.TechLevel;
                    }

                    scanner.ProcessHtml(fallbackBp, htmlFragment);
                    if (string.IsNullOrEmpty(fallbackBp.UUID))
                    {
                        bool fallbackGlobal = fallbackBp.Evolution == 0
                            && string.IsNullOrEmpty(fallbackBp.OwnerUUID);
                        fallbackBp.UUID = fallbackGlobal
                            ? DeterministicUUID.Generate(fallbackBp)
                            : Guid.NewGuid().ToString();
                    }

                    viewModel.LoadFrom(new ReadOnlyBlueprint(fallbackBp));
                    PopulateForm();
                    Log.Info("Blueprint imported from clipboard (fallback, no name parsed)");
                    return;
                }

                // Full import â€” delegate to BlueprintService
                ReadOnlyBlueprint selectedTarget = viewModel.Original;
                var importResult = _blueprintService.Import(tempBP, selectedTarget);

                // Refresh UI, select imported blueprint
                viewModel.LoadFrom(importResult);
                RefreshBlueprintList();
                SelectBlueprintInList(importResult.UUID);
                PopulateForm();

                // Auto-select best base blueprint match
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    if (string.IsNullOrEmpty(importResult.BaseBlueprintUUID) && _baseBlueprintList.Count > 1)
                    {
                        viewModel.BaseBlueprintUUID = _baseBlueprintList[1].UUID ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing blueprint from clipboard");
                MessageBox.Show(
                    "Failed to import blueprint: " + ex.Message,
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // -----------------------------------------------------------------------
        // Market Import (Task 4.2)
        // -----------------------------------------------------------------------

        private void BtnImportMarket_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show(
                    "No market HTML found on clipboard.",
                    "Import Market",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string clipboardData = Clipboard.GetText(TextDataFormat.Html);
            string html = ClipboardHelper.ExtractHtmlFragment(clipboardData);
            if (string.IsNullOrEmpty(html) || html.StartsWith("ERROR:"))
            {
                MessageBox.Show(
                    "No market HTML found on clipboard.",
                    "Import Market",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Validate clipboard contains market listing data
            var detected = ClipboardContentDetector.Detect(html);
            if (detected != ClipboardContentDetector.ContentType.MarketListing &&
                detected != ClipboardContentDetector.ContentType.Unknown)
            {
                string found = ClipboardContentDetector.GetDescription(detected);
                MessageBox.Show(
                    $"The clipboard contains {found}, not market listing data.\n\nCopy the market page from the game browser first.",
                    "Wrong Content",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Parse market HTML
            var scanner = new BlueprintScanner();
            var parsed = scanner.ProcessMarketHtml(html);
            if (parsed == null || parsed.Count == 0)
            {
                MessageBox.Show(
                    "No blueprint listings found in clipboard data.",
                    "Import Market",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Import
            var result = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Scan all imported blueprints for unknown properties
            var unknownPropWarnings = new List<string>();
            foreach (var entry in result.Entries)
            {
                if (entry.Action == ImportAction.Skipped || string.IsNullOrEmpty(entry.UUID))
                    continue;

                var bp = playerContext.FindBlueprint(entry.UUID)
                    ?? empireContext.FindGlobalBlueprint(entry.UUID);
                if (bp?.Properties == null || bp.Properties.Count == 0)
                    continue;

                BlueprintType bt = empireContext.FindBlueprintType(bp.BluePrintType);
                if (bt?.Properties == null)
                    continue;

                var knownProps = new HashSet<string>(bt.Properties, StringComparer.OrdinalIgnoreCase);
                knownProps.Add("_IconPosition");

                foreach (var propKey in bp.Properties.Keys)
                {
                    if (!knownProps.Contains(propKey))
                    {
                        bp.Properties.GetString(propKey, string.Empty, out string propVal);
                        unknownPropWarnings.Add($"  {bp.Name}: '{propKey}' = '{propVal}'");
                        Log.Warn("Market import: unknown property '{0}' on {1} ({2})", propKey, bp.Name, bt.Name);
                    }
                }
            }

            // Build summary
            var sb = new StringBuilder();
            sb.AppendLine($"Created: {result.CreatedCount}  Updated: {result.UpdatedCount}  Skipped: {result.SkippedCount}");
            sb.AppendLine();
            int shown = 0;
            foreach (var entry in result.Entries)
            {
                if (shown >= 15)
                {
                    sb.AppendLine($"  ... and {result.Entries.Count - 15} more");
                    break;
                }

                string key = $"{entry.Name} Ev{entry.Evolution} {entry.BluePrintType} C{entry.Class}";
                if (entry.Action == ImportAction.Skipped)
                    sb.AppendLine($"  [{entry.Storage ?? "?"}] SKIP  {key} -- {entry.SkipReason}");
                else
                    sb.AppendLine($"  [{entry.Storage}] {entry.Action}  {key}");
                shown++;
            }

            if (unknownPropWarnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Unknown properties detected ({unknownPropWarnings.Count}) â€” details in the log.");
            }

            MessageBox.Show(
                sb.ToString(),
                "Import Market Results",
                MessageBoxButtons.OK,
                unknownPropWarnings.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            // Notify if player blueprints changed
            bool playerChanged = result.Entries.Any(e2 =>
                e2.Storage == "Player" && (e2.Action == ImportAction.Created || e2.Action == ImportAction.Updated));
            if (playerChanged)
            {
                playerContext.OnBlueprintDataChanged(null);
            }

            // Refresh the blueprint list
            RefreshBlueprintList();
        }

        private void BtnImportCrate_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Import Blueprints from Crate Scraper JSON";
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                ofd.FilterIndex = 1;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    var result = CrateImporter.ImportFromFile(ofd.FileName, playerContext, empireContext);

                    if (result.Created > 0 || result.Updated > 0)
                    {
                        BlueprintService.PersistCrateImport(playerContext, empireContext);
                    }

                    if (result.Errors.Count > 0 && result.Created == 0 && result.Updated == 0)
                    {
                        MessageBox.Show(
                            string.Join("\n", result.Errors),
                            "Import Crate Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"File: {Path.GetFileName(ofd.FileName)}");
                    sb.AppendLine($"Total in file: {result.TotalInFile}");
                    sb.AppendLine($"Created: {result.Created}  Updated: {result.Updated}  Skipped: {result.Skipped}  Failed: {result.Failed}");

                    if (result.Errors.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Errors:");
                        foreach (var err in result.Errors.Take(10))
                            sb.AppendLine($"  {err}");
                    }

                    MessageBox.Show(
                        sb.ToString(),
                        "Import Crate Results",
                        MessageBoxButtons.OK,
                        result.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                    RefreshBlueprintList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error importing crate JSON file");
                    MessageBox.Show(
                        "Failed to import crate file: " + ex.Message,
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Identity Field Write-Through (Task 2.7)
        // -----------------------------------------------------------------------

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Name = txtName.Text;
            UpdateSaveButtonState();
        }

        private void TxtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.NickName = txtNickName.Text;
            UpdateSaveButtonState();
        }

        private void TxtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Description = txtDescription.Text;
            UpdateSaveButtonState();
        }

        private void TxtCopyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int.TryParse(txtCopyCost.Text, out int copyCost);
            viewModel.CopyCost = copyCost;
            UpdateSaveButtonState();
        }

        private void CmbBlueprintType_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string selectedName = cmbBlueprintType.SelectedItem;
            var bt = selectedName != null ? empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == selectedName) : null;
            string oldType = viewModel?.BluePrintType;
            string newType = bt?.Id;
            if (bt != null) viewModel.BluePrintType = bt.Id;
            Log.Info(
                "CmbBlueprintType changed: old='{0}' new='{1}' selectedIndex={2} bp='{3}' UUID={4}",
                oldType ?? "(null)",
                newType ?? "(null)",
                cmbBlueprintType.SelectedFullIndex,
                viewModel?.Name ?? "(null)",
                viewModel?.UUID ?? "(null)");

            // Hide ShipClass and TechLevel for Universal types
            UpdateUniversalVisibility(bt);

            // Rebuild statistics grid for the new type
            RefreshStatisticsGrid();
            UpdateSaveButtonState();
        }

        private void CmbShipClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var sc = cmbShipClass.SelectedItem as ShipClass;
            viewModel.Class = sc != null ? sc.Id : 0;
            UpdateSaveButtonState();
        }

        private void CmbTechLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var tl = cmbTechLevel.SelectedItem as TechLevel;
            viewModel.TechLevel = tl?.Name;
            UpdateSaveButtonState();
        }

        private void CmbEvolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string evo = cmbEvolution.SelectedItem as string ?? cmbEvolution.Text ?? "0";
            int.TryParse(evo, out int ev);
            viewModel.Evolution = ev;
            UpdateSaveButtonState();
        }

        private void CmbBaseBlueprint_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int fullIndex = cmbBaseBlueprint.SelectedFullIndex;
            if (fullIndex >= 0 && fullIndex < _baseBlueprintList.Count)
                viewModel.BaseBlueprintUUID = _baseBlueprintList[fullIndex].UUID ?? string.Empty;
            else
                viewModel.BaseBlueprintUUID = string.Empty;
            UpdateSaveButtonState();
        }

        private void ChkGlobalBlueprint_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Global flag is applied at save time via BlueprintService.MoveToGlobal/MoveToPlayer.
            // Changing the checkbox makes the form "dirty" for save purposes.
            viewModel.IsGlobal = chkGlobalBlueprint.Checked;
            UpdateSaveButtonState();
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
        /// Updates the base blueprint combo with candidates matching the current blueprint fields.
        /// Maintains a parallel list of Blueprint objects for UUID lookup via SelectedFullIndex.
        /// </summary>
        private void UpdateBaseBlueprintList()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var candidates = new List<ReadOnlyBlueprint>(viewModel.GetBaseBlueprintCandidates());

            // Insert empty entry at top to allow deselecting
            candidates.Insert(0, new ReadOnlyBlueprint(new Blueprint()));

            _baseBlueprintList = candidates;
            var names = candidates.Select(b => b.ExtendedName ?? string.Empty).ToList();
            string currentValue = string.Empty;
            if (!string.IsNullOrEmpty(viewModel.BaseBlueprintUUID))
            {
                var match = candidates.FirstOrDefault(b => b.UUID == viewModel.BaseBlueprintUUID);
                if (match != null) currentValue = match.ExtendedName ?? string.Empty;
            }

            cmbBaseBlueprint.SetItems(names, currentValue);
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

            int windowNumber = this.Tag is int n ? n : 0;
            this.Text = FormatTitleBar(windowNumber, globalCount, playerCount);
        }

        // -----------------------------------------------------------------------
        // Statistics Grid (Tasks 3.1â€“3.4)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the statistics grid structure if the BlueprintType or extra PropertyBag
        /// keys have changed, then populates values. Uses caching key "{typeId}|{extraKeys}"
        /// to avoid unnecessary rebuilds when only values change.
        /// </summary>
        private void RefreshStatisticsGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string btName = cmbBlueprintType.SelectedItem;
            var bt = btName != null ? empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == btName) : null;
            string[] definedProps = bt?.Properties ?? Array.Empty<string>();

            Log.Info(
                "RefreshStatisticsGrid: blueprint='{0}' type='{1}' definedProps={2} bagCount={3}",
                viewModel.Name ?? "(null)",
                bt?.Id ?? "(null)",
                definedProps.Length,
                viewModel.PropertyCount);

            // Find extra properties in PropertyBag not in the type definition
            var definedSet = new HashSet<string>(definedProps, StringComparer.Ordinal);
            var extraProps = viewModel.PropertyKeys
                .Where(k => !definedSet.Contains(k) && !k.StartsWith("_"))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (extraProps.Length > 0)
                Log.Info("RefreshStatisticsGrid: extraProps=[{0}]", string.Join(", ", extraProps));

            string gridKey = (bt?.Id ?? string.Empty) + "|" + string.Join(", ", extraProps);

            Log.Info(
                "RefreshStatisticsGrid: gridKey='{0}' cachedKey='{1}' rebuild={2}",
                gridKey,
                _cachedGridKey ?? "(null)",
                gridKey != _cachedGridKey);

            if (gridKey != _cachedGridKey)
            {
                RebuildStatisticsGrid(definedProps, extraProps);
                _cachedGridKey = gridKey;
            }

            PopulateStatisticsValues();
            sw.Stop();
            Log.Info("PERF RefreshStatisticsGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Rebuilds the statistics grid rows from scratch. Defined properties first,
        /// then extra/unknown properties. Handles CheckBox, ComboBox, and text cell types.
        /// </summary>
        private void RebuildStatisticsGrid(string[] definedProps, string[] extraProps)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            dgvStatistics.CellValidating -= DgvStatistics_CellValidating;
            try
            {
                dgvStatistics.EndEdit();
            }
            catch
            {
            }

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

            // CurrentValue column â€” placeholder, cells are swapped per-row below
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
                Log.Warn(
                    "Extra property '{0}' on '{1}' (not in {2} type definition)",
                    property,
                    viewModel.Name ?? "(new)",
                    cmbBlueprintType.SelectedItem ?? "unknown");
                AddStatisticsRow(property);
            }

            dgvStatistics.CellValidating += DgvStatistics_CellValidating;
            sw.Stop();
            Log.Info("PERF RebuildStatisticsGrid: {0}ms", sw.ElapsedMilliseconds);
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            Log.Info(
                "PopulateStatisticsValues: rows={0} blueprint='{1}' bagCount={2}",
                dgvStatistics.Rows.Count,
                viewModel.Name ?? "(null)",
                viewModel.PropertyCount);

            // Dump actual bag keys for diagnosis
            if (viewModel.PropertyCount > 0)
            {
                foreach (var kvp in viewModel.Properties)
                {
                    Log.Info(
                        "  BAG KEY: [{0}] = '{1}' (len={2}, chars={3})",
                        kvp.Key,
                        kvp.Value,
                        kvp.Key.Length,
                        string.Join(", ", kvp.Key.Select(c => ((int)c).ToString("X4"))));
                }
            }

            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string property = row.Cells["Property"].Tag as string;
                if (string.IsNullOrEmpty(property)) continue;

                viewModel.GetProperty(property, string.Empty, out string value);
                if (value == null) value = string.Empty;

                Log.Info(
                    "PopulateStatisticsValues: '{0}' = '{1}' (from bag: {2})",
                    property,
                    value,
                    viewModel.PropertyContainsKey(property));

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

            sw.Stop();
            Log.Info("PERF PopulateStatisticsValues: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Write-through: non-empty values written to PropertyBag, empty clears the key.
        /// </summary>
        private void DgvStatistics_CellValueChanged(object sender, DataGridViewCellEventArgs e)
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
                viewModel.RemoveProperty(propName);
            else
                viewModel.SetProperty(propName, strValue);

            UpdateSaveButtonState();
        }

        /// <summary>
        /// Validates cell input using BlueprintPropertyValidation patterns.
        /// </summary>
        private void DgvStatistics_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
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
            if (pattern == null) return; // Unknown â€” no validation

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
                dgvStatistics.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        /// <summary>
        /// Commits CheckBox and ComboBox edits immediately so CellValueChanged fires.
        /// </summary>
        private void DgvStatistics_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvStatistics.IsCurrentCellDirty)
                dgvStatistics.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // -----------------------------------------------------------------------
        // Resources Grid (Tasks 3.5â€“3.6)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates the resources grid from Blueprint.Resources.
        /// </summary>
        private void PopulateResourcesGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            dgvResources.CellValidating -= DgvResources_CellValidating;
            try
            {
                dgvResources.EndEdit();
            }
            catch
            {
            }

            dgvResources.Rows.Clear();
            dgvResources.CellValidating += DgvResources_CellValidating;

            foreach (var resource in viewModel.GetResources())
            {
                int rowIndex = dgvResources.Rows.Add();
                var row = dgvResources.Rows[rowIndex];
                row.Cells["Resource"].Value = resource.Key;
                row.Cells["Amount"].Value = resource.Value;
            }

            sw.Stop();
            Log.Info("PERF PopulateResourcesGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Write-through: resource edits written to Blueprint.Resources immediately.
        /// </summary>
        private void DgvResources_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;

            string resourceName = dgvResources.Rows[e.RowIndex].Cells["Resource"].Value as string;
            string amount = dgvResources.Rows[e.RowIndex].Cells["Amount"].Value as string;

            if (!string.IsNullOrEmpty(resourceName))
                viewModel.SetResource(resourceName, amount ?? "0");

            UpdateSaveButtonState();
        }

        /// <summary>
        /// Validates the Amount column as integer.
        /// </summary>
        private void DgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var col = dgvResources.Columns[e.ColumnIndex];
            if (col.Name != "Amount") return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = string.Empty;
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
                dgvResources.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        /// <summary>
        /// Adds an empty row and a placeholder entry in Blueprint.Resources.
        /// </summary>
        private void BtnAddResource_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvResources.Rows.Add();
            dgvResources.Rows[rowIndex].Cells["Amount"].Value = "0";
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Removes the selected row and its entry from Blueprint.Resources.
        /// </summary>
        private void BtnDeleteResource_Click(object sender, EventArgs e)
        {
            if (dgvResources.CurrentRow == null) return;
            int rowIndex = dgvResources.CurrentRow.Index;

            string resourceName = dgvResources.Rows[rowIndex].Cells["Resource"].Value as string;
            if (!string.IsNullOrEmpty(resourceName))
                viewModel.RemoveResource(resourceName);

            dgvResources.Rows.RemoveAt(rowIndex);
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Adds a new empty row to the statistics grid for a user-defined property.
        /// </summary>
        private void BtnAddStatistic_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvStatistics.Rows.Add();
            dgvStatistics.Rows[rowIndex].Cells["Property"].ReadOnly = false;
            dgvStatistics.Rows[rowIndex].Cells["Property"].Value = string.Empty;
            dgvStatistics.Rows[rowIndex].Cells["CurrentValue"].Value = string.Empty;
            _cachedGridKey = null;
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Deletes the selected statistics row. Type-defined properties cannot be deleted.
        /// </summary>
        private void BtnDeleteStatistic_Click(object sender, EventArgs e)
        {
            if (dgvStatistics.CurrentRow == null) return;
            int rowIndex = dgvStatistics.CurrentRow.Index;

            string propertyName = dgvStatistics.Rows[rowIndex].Cells["Property"].Value as string;
            if (string.IsNullOrEmpty(propertyName))
            {
                dgvStatistics.Rows.RemoveAt(rowIndex);
                _cachedGridKey = null;
                UpdateSaveButtonState();
                return;
            }

            // Check if this is a type-defined property
            string selectedTypeName = cmbBlueprintType.SelectedItem;
            var bt = selectedTypeName != null
                ? empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == selectedTypeName)
                : null;
            string[] definedProps = bt?.Properties ?? Array.Empty<string>();
            if (definedProps.Contains(propertyName))
            {
                MessageBox.Show(
                    string.Format("Cannot delete type-defined property '{0}'", propertyName),
                    "Delete Property",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            viewModel.RemoveProperty(propertyName);
            dgvStatistics.Rows.RemoveAt(rowIndex);
            _cachedGridKey = null;
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Clears the resources grid.
        /// </summary>
        private void ClearResourcesGrid()
        {
            dgvResources.CellValidating -= DgvResources_CellValidating;
            try
            {
                dgvResources.EndEdit();
            }
            catch
            {
            }

            dgvResources.Rows.Clear();
            dgvResources.CellValidating += DgvResources_CellValidating;
        }

        // -----------------------------------------------------------------------
        // Evolution Graph (Tasks 5.1â€“5.2)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Resolves the evolution chain, builds graph data, and renders series with solid/dashed segments.
        /// Called on selection change and BlueprintDataChanged.
        /// </summary>
        private void RefreshEvolutionGraph()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (viewModel == null)
            {
                ClearEvolutionGraph();
                return;
            }

            var blueprint = !string.IsNullOrEmpty(viewModel.UUID)
                ? (playerContext.FindBlueprint(viewModel.UUID)
                   ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(viewModel.UUID))
                : null;
            if (blueprint == null || string.IsNullOrEmpty(viewModel.UUID))
            {
                ClearEvolutionGraph();
                return;
            }

            // Resolve the evolution chain
            var chain = EvolutionChainService.ResolveChain(
                blueprint,
                uuid => playerContext.FindBlueprint(uuid) ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(uuid));

            // Look up BlueprintType to get Properties array
            var blueprintType = empireContext.BlueprintTypeList?
                .FirstOrDefault(bt => bt.Id == blueprint.BluePrintType);
            string[] blueprintTypeProperties = blueprintType?.Properties ?? new string[0];

            // Build graph data
            var graphData = EvolutionChainService.BuildGraphData(chain, blueprintTypeProperties);

            if (graphData.NoChanges)
            {
                lblNoChanges.Visible = true;
                chartEvolution.Visible = false;
                pnlPropertyCheckboxes.Visible = false;
                return;
            }

            // Show chart and checkbox panel, hide no-changes label
            lblNoChanges.Visible = false;
            chartEvolution.Visible = true;
            pnlPropertyCheckboxes.Visible = true;

            // Clear existing series and checkboxes, preserving unchecked state
            var uncheckedProperties = new HashSet<string>();
            foreach (Control ctrl in pnlPropertyCheckboxes.Controls)
            {
                if (ctrl is CheckBox cb && !cb.Checked && cb.Tag is string propName)
                {
                    uncheckedProperties.Add(propName);
                }
            }

            chartEvolution.Series.Clear();
            pnlPropertyCheckboxes.Controls.Clear();

            // Add evolution guide lines: expected +/- 50% range from Evo 0 to Evo 15
            var guideUpper = new Series("_GuideUpper")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.LightGray,
                BorderWidth = 2,
                BorderDashStyle = ChartDashStyle.Dash,
                IsVisibleInLegend = false
            };

            guideUpper.Points.AddXY(0, 100);
            guideUpper.Points.AddXY(15, 150);
            chartEvolution.Series.Add(guideUpper);

            var guideLower = new Series("_GuideLower")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.LightGray,
                BorderWidth = 2,
                BorderDashStyle = ChartDashStyle.Dash,
                IsVisibleInLegend = false
            };

            guideLower.Points.AddXY(0, 100);
            guideLower.Points.AddXY(15, 50);
            chartEvolution.Series.Add(guideLower);

            int colorIndex = 0;
            foreach (var kvp in graphData.Series)
            {
                string propertyName = kvp.Key;
                var points = kvp.Value;
                Color lineColor = WongPalette[colorIndex % WongPalette.Length];

                // Sort points by evolution level
                points.Sort((a, b) => a.Evolution.CompareTo(b.Evolution));

                // Build segments: solid for consecutive evolution levels, dashed for gaps
                int segmentIndex = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    if (i == 0 && points.Count == 1)
                    {
                        // Single point â€” create a series with just one data point
                        var singleSeries = new Series($"{propertyName}_{segmentIndex}")
                        {
                            ChartType = SeriesChartType.Line,
                            Color = lineColor,
                            BorderWidth = 2,
                            MarkerStyle = MarkerStyle.Circle,
                            MarkerSize = 6
                        };

                        singleSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                        chartEvolution.Series.Add(singleSeries);
                        segmentIndex++;
                        continue;
                    }

                    if (i == 0) continue; // Start processing pairs from index 1

                    int evDiff = points[i].Evolution - points[i - 1].Evolution;
                    bool isGap = evDiff > 1;
                    ChartDashStyle dashStyle = isGap ? ChartDashStyle.Dash : ChartDashStyle.Solid;

                    // Check if we can extend the previous segment (same dash style and connects)
                    bool extendPrevious = false;
                    if (segmentIndex > 0)
                    {
                        string prevSeriesName = $"{propertyName}_{segmentIndex - 1}";
                        var prevSeries = chartEvolution.Series.FindByName(prevSeriesName);
                        if (prevSeries != null && prevSeries.BorderDashStyle == dashStyle)
                        {
                            prevSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                            extendPrevious = true;
                        }
                    }

                    if (!extendPrevious)
                    {
                        var segmentSeries = new Series($"{propertyName}_{segmentIndex}")
                        {
                            ChartType = SeriesChartType.Line,
                            Color = lineColor,
                            BorderWidth = 2,
                            BorderDashStyle = dashStyle,
                            MarkerStyle = MarkerStyle.Circle,
                            MarkerSize = 6
                        };

                        segmentSeries.Points.AddXY(points[i - 1].Evolution, points[i - 1].Percent);
                        segmentSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                        chartEvolution.Series.Add(segmentSeries);
                        segmentIndex++;
                    }
                }

                // Add checkbox for this property (preserve previous unchecked state)
                bool isChecked = !uncheckedProperties.Contains(propertyName);
                var checkbox = new CheckBox
                {
                    Text = propertyName,
                    Checked = isChecked,
                    ForeColor = lineColor,
                    AutoSize = true,
                    Tag = propertyName
                };

                // Apply initial visibility based on preserved state
                if (!isChecked)
                {
                    foreach (var series in chartEvolution.Series)
                    {
                        if (series.Name.StartsWith(propertyName + "_"))
                        {
                            series.Enabled = false;
                        }
                    }
                }

                checkbox.CheckedChanged += (s, ev) =>
                {
                    string propTag = (string)((CheckBox)s).Tag;
                    bool visible = ((CheckBox)s).Checked;
                    foreach (var series in chartEvolution.Series)
                    {
                        if (series.Name.StartsWith(propTag + "_"))
                        {
                            series.Enabled = visible;
                        }
                    }
                };

                pnlPropertyCheckboxes.Controls.Add(checkbox);

                colorIndex++;
            }

            sw.Stop();
            Log.Info("PERF RefreshEvolutionGraph: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Clears the evolution graph chart series, checkbox panel, and hides the no-changes label.
        /// </summary>
        private void ClearEvolutionGraph()
        {
            chartEvolution.Series.Clear();
            pnlPropertyCheckboxes.Controls.Clear();
            lblNoChanges.Visible = false;
        }

        // -----------------------------------------------------------------------
        // Price Evolution Graph
        // -----------------------------------------------------------------------

        /// <summary>
        /// <summary>
        /// Resolves the evolution chain, computes the price at each evolution level,
        /// and plots a line chart of evolution level vs price.
        /// </summary>
        private void RefreshPriceEvolutionGraph()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (viewModel == null || string.IsNullOrEmpty(viewModel.UUID))
            {
                ClearPriceEvolutionGraph();
                return;
            }

            int planIdx = cmbPricingPlan.SelectedFullIndex;
            string planUUID = planIdx >= 0 && planIdx < _pricingPlanList.Count ? _pricingPlanList[planIdx] : string.Empty;
            if (string.IsNullOrEmpty(planUUID))
            {
                chartPriceEvolution.Visible = false;
                lblPriceEvoNoPlan.Text = "Select a pricing plan above";
                lblPriceEvoNoPlan.Visible = true;
                return;
            }

            var plan = playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (plan == null)
            {
                chartPriceEvolution.Visible = false;
                lblPriceEvoNoPlan.Text = "Select a pricing plan above";
                lblPriceEvoNoPlan.Visible = true;
                return;
            }

            // Resolve the evolution chain
            var priceBlueprint = !string.IsNullOrEmpty(viewModel.UUID)
                ? (playerContext.FindBlueprint(viewModel.UUID)
                   ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(viewModel.UUID))
                : null;
            if (priceBlueprint == null)
            {
                chartPriceEvolution.Visible = false;
                lblPriceEvoNoPlan.Text = "No evolution data";
                lblPriceEvoNoPlan.Visible = true;
                return;
            }

            var chain = EvolutionChainService.ResolveChain(
                priceBlueprint,
                uuid => playerContext.FindBlueprint(uuid) ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(uuid));

            if (chain.Count <= 1)
            {
                chartPriceEvolution.Visible = false;
                lblPriceEvoNoPlan.Text = "No evolution data";
                lblPriceEvoNoPlan.Visible = true;
                return;
            }

            // Compute price at each evolution level
            var dataPoints = new List<(int Evolution, decimal Price)>();
            foreach (var bp in chain)
            {
                decimal mfgHours = 0m;
                if (bp.Properties != null)
                {
                    bp.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, null, out string mfgTimeStr);
                    if (!string.IsNullOrEmpty(mfgTimeStr))
                    {
                        decimal seconds = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
                        mfgHours = seconds / 3600m;
                    }
                }

                var result = PriceCalculator.ComputeBlueprintPrice(plan, bp, mfgHours);
                dataPoints.Add((bp.Evolution, result.Price));
            }

            // Show chart, hide label
            lblPriceEvoNoPlan.Visible = false;
            chartPriceEvolution.Visible = true;

            // Clear and rebuild series
            chartPriceEvolution.Series.Clear();

            var series = new Series("Price")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(0, 114, 178), // blue
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6
            };

            dataPoints.Sort((a, b) => a.Evolution.CompareTo(b.Evolution));
            foreach (var pt in dataPoints)
            {
                series.Points.AddXY(pt.Evolution, (double)pt.Price);
            }

            chartPriceEvolution.Series.Add(series);

            // Auto-scale Y axis to data range
            var chartArea = chartPriceEvolution.ChartAreas[0];
            if (dataPoints.Count > 0)
            {
                decimal minPrice = dataPoints.Min(p => p.Price);
                decimal maxPrice = dataPoints.Max(p => p.Price);
                decimal range = maxPrice - minPrice;
                decimal margin = range > 0 ? range * 0.1m : Math.Max(minPrice * 0.1m, 1m);
                chartArea.AxisY.Minimum = (double)Math.Max(0, minPrice - margin);
                chartArea.AxisY.Maximum = (double)(maxPrice + margin);
                chartArea.AxisY.IsStartedFromZero = false;
            }

            sw.Stop();
            Log.Info("PERF RefreshPriceEvolutionGraph: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Clears the price evolution graph chart series and hides the label.
        /// </summary>
        private void ClearPriceEvolutionGraph()
        {
            chartPriceEvolution.Series.Clear();
            lblPriceEvoNoPlan.Visible = false;
        }

        // -----------------------------------------------------------------------
        // Pricing Plan (Tasks 5.3â€“5.4)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates the pricing plan combo with the current player's pricing plans.
        /// Preserves the previous selection if it still exists.
        /// </summary>
        private void PopulatePricingPlanCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedName = cmbPricingPlan.SelectedItem;

            var plans = playerContext.GetCurrentPlayerPricingPlans();
            var planNames = new List<string>();
            _pricingPlanList = new List<string>();
            planNames.Add("(none)");
            _pricingPlanList.Add(string.Empty);
            foreach (var p in CollectionSortHelper.OrderPricingPlans(plans))
            {
                planNames.Add(p.Name);
                _pricingPlanList.Add(p.UUID);
            }

            cmbPricingPlan.SetItems(planNames, selectedName ?? "(none)");
            sw.Stop();
            Log.Info("PERF PopulatePricingPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Handles pricing plan selection changes â€” recomputes the displayed price.
        /// </summary>
        private void CmbPricingPlan_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            UpdateCalculatedPrice();
            RefreshPriceEvolutionGraph();
        }

        /// <summary>
        /// Computes the blueprint price using PriceCalculator and displays it in lblComputedPrice.
        /// Shows an asterisk (*) indicator when the price is incomplete (missing resource prices).
        /// </summary>
        private void UpdateCalculatedPrice()
        {
            if (viewModel.UUID == null || viewModel.ResourceCount == 0)
            {
                lblComputedPrice.Text = string.Empty;
                return;
            }

            int planIdx2 = cmbPricingPlan.SelectedFullIndex;
            string planUUID = planIdx2 >= 0 && planIdx2 < _pricingPlanList.Count ? _pricingPlanList[planIdx2] : string.Empty;
            if (string.IsNullOrEmpty(planUUID))
            {
                lblComputedPrice.Text = string.Empty;
                return;
            }

            var plan = playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (plan == null)
            {
                lblComputedPrice.Text = string.Empty;
                return;
            }

            // Look up mutable entity for PriceCalculator (will be replaced by service in future)
            var priceBp = playerContext.FindBlueprint(viewModel.UUID)
                ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(viewModel.UUID);
            if (priceBp == null)
            {
                lblComputedPrice.Text = string.Empty;
                return;
            }

            // Parse manufacturing hours from blueprint properties
            decimal mfgHours = 0m;
            viewModel.GetProperty(BlueprintPropertyKeys.ManufactureRunTime, null, out string mfgTimeStr);
            if (!string.IsNullOrEmpty(mfgTimeStr))
            {
                decimal seconds = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
                mfgHours = seconds / 3600m;
            }

            var result = PriceCalculator.ComputeBlueprintPrice(plan, priceBp, mfgHours);

            // Divide by Amount Manufactured to get per-unit cost (for munitions, etc.)
            decimal amountMfg = 1m;
            priceBp.Properties.GetDecimal(BlueprintPropertyKeys.AmountManufactured, 1m, out amountMfg);
            if (amountMfg < 1m) amountMfg = 1m;

            decimal perUnitPrice = result.Price / amountMfg;
            string priceText = perUnitPrice.ToString("N2");
            if (amountMfg > 1m)
                priceText += " (x" + amountMfg.ToString("G0") + " = " + result.Price.ToString("N2") + ")";
            if (!result.IsComplete)
                priceText += " *";
            lblComputedPrice.Text = priceText;
        }

        /// <summary>
        /// Refreshes pricing data: repopulates the plan combo and recomputes the price.
        /// Called on PricingDataChanged events.
        /// </summary>
        private void RefreshPricing()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            PopulatePricingPlanCombo();
            UpdateCalculatedPrice();
            RefreshPriceEvolutionGraph();
            sw.Stop();
            Log.Info("PERF RefreshPricing: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Form Population / Clear
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates all form fields from the current viewModel.
        /// </summary>
        private void PopulateForm()
        {
            if (viewModel.UUID == null) return;

            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            // Identity fields
            txtName.Text = viewModel.Name ?? string.Empty;
            txtNickName.Text = viewModel.NickName ?? string.Empty;
            txtDescription.Text = viewModel.Description ?? string.Empty;
            txtCopyCost.Text = viewModel.CopyCost.ToString();

            // Blueprint type
            string dataType = viewModel.BluePrintType;
            var foundBt = empireContext.FindBlueprintType(dataType);
            if (foundBt != null)
                cmbBlueprintType.SetItems(cmbBlueprintType.Items, foundBt.Name);

            var bt = foundBt;
            Log.Debug(
                "PopulateForm type: data='{0}' found={1} comboSelected='{2}' comboIndex={3} bp='{4}'",
                dataType ?? "(null)",
                foundBt != null ? foundBt.Id : "(not found)",
                bt?.Id ?? "(null)",
                cmbBlueprintType.SelectedFullIndex,
                viewModel.Name ?? "(null)");
            UpdateUniversalVisibility(bt);

            // Dependent combos
            cmbShipClass.SelectedItem = empireContext.FindShipClass(viewModel.Class);
            cmbTechLevel.SelectedItem = empireContext.FindTechLevel(viewModel.TechLevel);
            cmbEvolution.SelectedItem = empireContext.FindEvolution(viewModel.Evolution);

            // Base blueprint
            UpdateBaseBlueprintList();

            // Global checkbox
            chkGlobalBlueprint.Checked = viewModel.IsGlobal;

            long t1 = sw.ElapsedMilliseconds;

            // Statistics and Resources grids
            RefreshStatisticsGrid();
            PopulateResourcesGrid();

            long t2 = sw.ElapsedMilliseconds;

            // Pricing
            PopulatePricingPlanCombo();
            UpdateCalculatedPrice();

            sw.Stop();
            Log.Info(
                "PopulateForm PERF: total={0}ms fields={1}ms grids={2}ms pricing={3}ms",
                sw.ElapsedMilliseconds,
                t1,
                t2 - t1,
                sw.ElapsedMilliseconds - t2);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Clears all form controls and resets the viewModel.
        /// </summary>
        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            txtName.Text = string.Empty;
            txtNickName.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtCopyCost.Text = string.Empty;

            cmbBlueprintType.SetItems(cmbBlueprintType.Items, null);
            cmbShipClass.SelectedIndex = -1;
            cmbTechLevel.SelectedIndex = -1;
            cmbEvolution.SelectedIndex = 0;

            _baseBlueprintList.Clear();
            cmbBaseBlueprint.SetItems(new List<string>(), string.Empty);

            chkGlobalBlueprint.Checked = false;

            // Clear grids
            dgvStatistics.CellValidating -= DgvStatistics_CellValidating;
            try
            {
                dgvStatistics.EndEdit();
            }
            catch
            {
            }

            dgvStatistics.Rows.Clear();
            dgvStatistics.Columns.Clear();
            dgvStatistics.CellValidating += DgvStatistics_CellValidating;
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
                try
                {
                    BeginInvoke(new Action(() => OnBlueprintDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            if (viewModel.UUID == e.BlueprintUUID)
            {
                PopulateForm();
            }

            RefreshEvolutionGraph();
            RefreshPriceEvolutionGraph();
            RefreshBlueprintList();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            lvwBlueprints.Items.Clear();
            viewModel.Reset();
            ClearForm();
            PopulatePricingPlanCombo();
            RefreshBlueprintList();
        }

        private void OnPricingDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnPricingDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            RefreshPricing();
            RefreshPriceEvolutionGraph();
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
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

        private void FlpBlueprintData_Layout(object sender, LayoutEventArgs e)
        {
            // TabControl fills remaining height after identity and commands
            int usedHeight = flpIdentity.Height + flpIdentity.Margin.Top + flpIdentity.Margin.Bottom
                           + flpCommands.Height + flpCommands.Margin.Top + flpCommands.Margin.Bottom;
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
