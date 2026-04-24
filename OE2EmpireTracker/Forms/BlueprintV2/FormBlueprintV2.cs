using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
    /// FormBlueprintV2 — Clean rewrite of the blueprint management form.
    /// Built around write-through: the data model (PropertyBag, Resources) is always
    /// the source of truth. The grid is a view, not a store.
    /// </summary>
    public partial class FormBlueprintV2 : Form, IProgrammaticUpdateSource
    {
        /// <summary>
        /// Extended 16-color Wong palette (8 base + 8 lighter tints) for colorblind-friendly chart lines.
        /// </summary>
        internal static readonly Color[] WongPalette = new Color[]
        {
            // Base Wong palette (8 colors)
            ColorTranslator.FromHtml("#000000"), // black
            ColorTranslator.FromHtml("#E69F00"), // orange
            ColorTranslator.FromHtml("#56B4E9"), // sky blue
            ColorTranslator.FromHtml("#009E73"), // bluish green
            ColorTranslator.FromHtml("#B8860B"), // dark goldenrod
            ColorTranslator.FromHtml("#0072B2"), // blue
            ColorTranslator.FromHtml("#D55E00"), // vermillion
            ColorTranslator.FromHtml("#CC79A7"), // reddish purple
            // 50% lighter tints for properties 9-16
            ColorTranslator.FromHtml("#808080"), // light black (grey)
            ColorTranslator.FromHtml("#F2CF80"), // light orange
            ColorTranslator.FromHtml("#ABD9F4"), // light sky blue
            ColorTranslator.FromHtml("#80CEB9"), // light bluish green
            ColorTranslator.FromHtml("#DAA520"), // goldenrod
            ColorTranslator.FromHtml("#80B8D8"), // light blue
            ColorTranslator.FromHtml("#EAAF80"), // light vermillion
            ColorTranslator.FromHtml("#E5BCD3"), // light reddish purple
        };

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

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
            lvwBlueprints.ColumnClick += LvwBlueprints_ColumnClick;
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);

            // Wire filter events
            txtFilter.TextChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
            cmbFilterType.SelectedIndexChanged += (s, e) => { if (_isProgrammaticUpdate == 0) RefreshBlueprintList(); };
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
            cmbBlueprintType.SelectedIndexChanged += CmbBlueprintType_SelectedIndexChanged;
            cmbShipClass.SelectedIndexChanged += CmbShipClass_SelectedIndexChanged;
            cmbTechLevel.SelectedIndexChanged += CmbTechLevel_SelectedIndexChanged;
            cmbEvolution.SelectedIndexChanged += CmbEvolution_SelectedIndexChanged;
            cmbBaseBlueprint.SelectedIndexChanged += CmbBaseBlueprint_SelectedIndexChanged;
            txtFilterBaseBlueprint.TextChanged += TxtFilterBaseBlueprint_TextChanged;
            chkGlobalBlueprint.CheckedChanged += ChkGlobalBlueprint_CheckedChanged;

            // Wire command buttons
            btnNew.Click += BtnNew_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnImport.Click += BtnImport_Click;
            btnImportMarket.Click += BtnImportMarket_Click;

            // Wire statistics grid events
            dgvStatistics.CellValueChanged += DgvStatistics_CellValueChanged;
            dgvStatistics.CellValidating += DgvStatistics_CellValidating;
            dgvStatistics.CurrentCellDirtyStateChanged += DgvStatistics_CurrentCellDirtyStateChanged;

            // Configure resources grid combo
            colResource.DisplayMember = "Name";
            colResource.ValueMember = "Name";
            colResource.DataSource = empireContext.BindingSourceResource;

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

            // Configure pricing plan combo
            cmbPricingPlan.DisplayMember = "Name";
            cmbPricingPlan.ValueMember = "UUID";
            PopulatePricingPlanCombo();
            cmbPricingPlan.SelectedIndexChanged += CmbPricingPlan_SelectedIndexChanged;

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
        public static string FormatTitleBar(int globalCount, int playerCount)
        {
            return $"Blueprints - Global: {globalCount} Player: {playerCount}";
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
            cmbFilterType.Items.Add(string.Empty);
            foreach (BlueprintType bt in empireContext.BlueprintTypeList)
                cmbFilterType.Items.Add(bt.Name);
            cmbFilterType.SelectedIndex = 0;

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
            var sw = Stopwatch.StartNew();
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
        private void PopulateListView(IReadOnlyList<Blueprint> blueprints)
        {
            if (blueprints == null) return;

            var sw = Stopwatch.StartNew();
            var counter = CreateReferenceCounter();
            long t1 = sw.ElapsedMilliseconds;
            lvwBlueprints.BeginUpdate();
            lvwBlueprints.Items.Clear();

            foreach (Blueprint bp in blueprints)
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
                if ((item.Tag as Blueprint)?.UUID == uuid)
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

        private void LvwBlueprints_SelectedIndexChanged(object sender, EventArgs e)
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
                RefreshEvolutionGraph();
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
            ClearForm();
            lvwBlueprints.SelectedItems.Clear();
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

            // All data is already in the model via write-through — just persist
            viewModel.Save(chkGlobalBlueprint.Checked);

            RefreshBlueprintList();
            SelectBlueprintInList(viewModel.Data.UUID);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
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
                            "Import",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
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
                    Log.Info(
                        "Resources-only import merged into selected blueprint: {0} UUID={1}",
                        viewModel.Data.Name,
                        viewModel.Data.UUID);
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
                        viewModel.BaseBlueprintUUID = bp?.UUID ?? string.Empty;
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
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboardData);
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

                Blueprint bp = playerContext.FindBlueprint(entry.UUID)
                    ?? empireContext.GlobalBlueprintList.FirstOrDefault(b => b.UUID == entry.UUID);
                if (bp?.Properties == null || bp.Properties.Count == 0)
                    continue;

                BlueprintType bt = empireContext.FindBlueprintType(bp.BluePrintType);
                if (bt?.Properties == null)
                    continue;

                var knownProps = new HashSet<string>(bt.Properties, StringComparer.OrdinalIgnoreCase);
                knownProps.Add("_IconPosition");

                foreach (var propKey in bp.Properties.Properties.Keys)
                {
                    if (!knownProps.Contains(propKey))
                    {
                        unknownPropWarnings.Add($"  {bp.Name}: '{propKey}' = '{bp.Properties.Properties[propKey]}'");
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
                sb.AppendLine($"Unknown properties detected ({unknownPropWarnings.Count}) — details in the log.");
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

        // -----------------------------------------------------------------------
        // Identity Field Write-Through (Task 2.7)
        // -----------------------------------------------------------------------

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Name = txtName.Text;
        }

        private void TxtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.NickName = txtNickName.Text;
        }

        private void TxtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Description = txtDescription.Text;
        }

        private void TxtCopyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int.TryParse(txtCopyCost.Text, out int copyCost);
            viewModel.CopyCost = copyCost;
        }

        private void CmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null) viewModel.BluePrintType = bt.Id;

            // Hide ShipClass and TechLevel for Universal types
            UpdateUniversalVisibility(bt);

            // Rebuild statistics grid for the new type
            RefreshStatisticsGrid();
        }

        private void CmbShipClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var sc = cmbShipClass.SelectedItem as ShipClass;
            viewModel.Class = sc != null ? sc.Id : 0;
        }

        private void CmbTechLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var tl = cmbTechLevel.SelectedItem as TechLevel;
            viewModel.TechLevel = tl?.Name;
        }

        private void CmbEvolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string evo = cmbEvolution.SelectedItem as string ?? cmbEvolution.Text ?? "0";
            int.TryParse(evo, out int ev);
            viewModel.Evolution = ev;
        }

        private void CmbBaseBlueprint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bp = cmbBaseBlueprint.SelectedItem as Blueprint;
            viewModel.BaseBlueprintUUID = bp?.UUID ?? string.Empty;
        }

        private void TxtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.DroppedDown = true;
        }

        private void ChkGlobalBlueprint_CheckedChanged(object sender, EventArgs e)
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            string[] definedProps = bt?.Properties ?? Array.Empty<string>();

            Log.Info(
                "RefreshStatisticsGrid: blueprint='{0}' type='{1}' definedProps={2} bagCount={3}",
                viewModel.Data.Name ?? "(null)",
                bt?.Id ?? "(null)",
                definedProps.Length,
                viewModel.Data.Properties?.Count ?? 0);

            // Find extra properties in PropertyBag not in the type definition
            var definedSet = new HashSet<string>(definedProps, StringComparer.Ordinal);
            var extraProps = viewModel.Data.Properties.Properties.Keys
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
                Log.Warn(
                    "Extra property '{0}' on '{1}' (not in {2} type definition)",
                    property,
                    viewModel.Data.Name ?? "(new)",
                    (cmbBlueprintType.SelectedItem as BlueprintType)?.Id ?? "unknown");
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
                viewModel.Data.Name ?? "(null)",
                viewModel.Data.Properties?.Count ?? 0);

            // Dump actual bag keys for diagnosis
            if (viewModel.Data.Properties?.Count > 0)
            {
                foreach (var kvp in viewModel.Data.Properties.Properties)
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
                    viewModel.Data.Properties.ContainsKey(property));

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
                viewModel.Data.Properties.Remove(propName);
            else
                viewModel.SetProperty(propName, strValue);
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
        // Resources Grid (Tasks 3.5–3.6)
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
                viewModel.Data.Resources.Remove(resourceName);

            dgvResources.Rows.RemoveAt(rowIndex);
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
        // Evolution Graph (Tasks 5.1–5.2)
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

            var blueprint = viewModel.Data;
            if (blueprint == null || string.IsNullOrEmpty(blueprint.UUID))
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
                        // Single point — create a series with just one data point
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
        // Pricing Plan (Tasks 5.3–5.4)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Populates the pricing plan combo with the current player's pricing plans.
        /// Preserves the previous selection if it still exists.
        /// </summary>
        private void PopulatePricingPlanCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = cmbPricingPlan.SelectedValue as string;
            cmbPricingPlan.DataSource = null;

            var plans = playerContext.GetCurrentPlayerPricingPlans();
            var items = new List<object>();
            items.Add(new { Name = "(none)", UUID = string.Empty });
            foreach (var p in plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                items.Add(new { Name = p.Name, UUID = p.UUID });

            cmbPricingPlan.DisplayMember = "Name";
            cmbPricingPlan.ValueMember = "UUID";
            cmbPricingPlan.DataSource = items;

            if (!string.IsNullOrEmpty(selectedUUID) && items.Any(i => ((dynamic)i).UUID == selectedUUID))
                cmbPricingPlan.SelectedValue = selectedUUID;
            else
                cmbPricingPlan.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulatePricingPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Handles pricing plan selection changes — recomputes the displayed price.
        /// </summary>
        private void CmbPricingPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            UpdateCalculatedPrice();
        }

        /// <summary>
        /// Computes the blueprint price using PriceCalculator and displays it in lblComputedPrice.
        /// Shows an asterisk (*) indicator when the price is incomplete (missing resource prices).
        /// </summary>
        private void UpdateCalculatedPrice()
        {
            if (viewModel.Data.UUID == null || viewModel.Data.Resources == null || viewModel.Data.Resources.Count == 0)
            {
                lblComputedPrice.Text = string.Empty;
                return;
            }

            string planUUID = cmbPricingPlan.SelectedValue as string;
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

            // Parse manufacturing hours from blueprint properties
            decimal mfgHours = 0m;
            if (viewModel.Data.Properties != null)
            {
                viewModel.Data.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, null, out string mfgTimeStr);
                if (!string.IsNullOrEmpty(mfgTimeStr))
                {
                    decimal seconds = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
                    mfgHours = seconds / 3600m;
                }
            }

            var result = PriceCalculator.ComputeBlueprintPrice(plan, viewModel.Data, mfgHours);
            string priceText = result.Price.ToString("N2");
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
            PopulatePricingPlanCombo();
            UpdateCalculatedPrice();
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

            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            // Identity fields
            txtName.Text = viewModel.Data.Name ?? string.Empty;
            txtNickName.Text = viewModel.Data.NickName ?? string.Empty;
            txtDescription.Text = viewModel.Data.Description ?? string.Empty;
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
            txtFilterBaseBlueprint.Text = string.Empty;
            UpdateBaseBlueprintList();
            if (!string.IsNullOrEmpty(viewModel.Data.BaseBlueprintUUID))
                cmbBaseBlueprint.SelectedValue = viewModel.Data.BaseBlueprintUUID;
            else
                cmbBaseBlueprint.SelectedIndex = 0;

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

            cmbBlueprintType.SelectedIndex = -1;
            cmbShipClass.SelectedIndex = -1;
            cmbTechLevel.SelectedIndex = -1;
            cmbEvolution.SelectedIndex = 0;

            txtFilterBaseBlueprint.Text = string.Empty;
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.SelectedIndex = -1;

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

            if (viewModel.Data.UUID == e.BlueprintUUID)
            {
                PopulateForm();
            }

            RefreshEvolutionGraph();
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
