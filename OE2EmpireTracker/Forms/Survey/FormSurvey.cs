using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.Survey
{
    /// <summary>
    /// FormSurvey - Main form for managing survey data in the OE2 Empire Tracker.
    /// </summary>
    public partial class FormSurvey : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private EmpireContext empireContext;

        private PlayerContext playerContext;

        private SurveyViewModel _viewModel = new SurveyViewModel();

        private SurveyService _surveyService;

        private int _sortColumn = 2; // PlanetName
        private SortOrder _sortOrder = SortOrder.Ascending;

        private List<Blueprint> _scannerBlueprintItems = new List<Blueprint>();

        // Tracks the previously selected survey UUID for unsaved-changes cancel/restore
        private string _previousSelectedUUID;
        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            _surveyService = new SurveyService(playerContext);

            // Configure scanner blueprint combo box
            PopulateScannerBlueprintList(null);
            cmbScannerBlueprint.SelectedItemChanged += CmbScannerBlueprint_SelectedItemChanged;

            // Set up survey list view with columns
            lvwSurveys.View = View.Details;
            lvwSurveys.Columns.Add("UUID", 0);
            lvwSurveys.Columns.Add("System", 70);
            lvwSurveys.Columns.Add("PlanetName", 100);
            lvwSurveys.Columns.Add("SurveyID", 70);
            lvwSurveys.Columns.Add("NickName", 100);
            lvwSurveys.Columns.Add("DateTime", 100);
            lvwSurveys.Columns.Add("Refs", 35);
            lvwSurveys.ColumnClick += LvwSurveys_ColumnClick;
            lvwSurveys.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            RefreshSurveyList();
            UpdateTitle();

            // Wire survey list filter
            txtSurveyFilter.TextChanged += TxtSurveyFilter_TextChanged;

            // Populate resource filter combo
            cmbResource.DisplayMember = "Name";
            cmbResource.Items.Add(new Models.Resource { Name = "(all)" });
            foreach (var r in empireContext.ResourceList)
            {
                cmbResource.Items.Add(r);
            }

            cmbResource.SelectedIndex = 0;
            cmbResource.SelectedIndexChanged += CmbResource_SelectedIndexChanged;

            // Wire additional filters
            cmbSurveyType.Items.Add("All");
            cmbSurveyType.Items.Add("Planet");
            cmbSurveyType.Items.Add("Asteroid");
            cmbSurveyType.SelectedIndex = 0;
            cmbSurveyType.SelectedIndexChanged += CmbSurveyType_SelectedIndexChanged;

            cmbPurityFilter.Items.Add("(any)");
            foreach (var p in Models.ResourcePurity.Purities)
            {
                if (p.ID != Models.ResourcePurity.PurityEnum.None)
                {
                    cmbPurityFilter.Items.Add(p.Name);
                }
            }

            cmbPurityFilter.SelectedIndex = 0;
            cmbPurityFilter.SelectedIndexChanged += CmbPurityFilter_SelectedIndexChanged;

            txtMinAmount.TextChanged += TxtMinAmount_TextChanged;
            // Configure resource data grid
            var resourceNameList = empireContext.ResourceList.Select(r => r.Name).ToList();
            Resource.Items = resourceNameList;

            DataGridViewComboBoxColumn cmbPurity = (DataGridViewComboBoxColumn)dgvResources.Columns["Purity"];
            cmbPurity.DisplayMember = "Name";
            cmbPurity.ValueMember = "Name";
            cmbPurity.DataSource = empireContext.BindingSourceResourcePurity;

            dgvResources.DataError += (s, ev) =>
            {
                Log.Warn("dgvResources DataError at [{0}, {1}]: {2}", ev.RowIndex, ev.ColumnIndex, ev.Exception?.Message);
                ev.ThrowException = false;
            };

            // Add read-only Max Reserve column (programmatic - not in Designer)
            var colMaxReserve = new DataGridViewTextBoxColumn();
            colMaxReserve.HeaderText = "Max Reserve";
            colMaxReserve.Name = "MaxReserve";
            colMaxReserve.ReadOnly = true;
            colMaxReserve.Width = 100;
            dgvResources.Columns.Add(colMaxReserve);

            // Wire control change handlers (local-only ViewModel updates)
            txtPlanetName.TextChanged += TxtPlanetName_TextChanged;
            txtSystemName.TextChanged += TxtSystemName_TextChanged;
            cmbSurveyTypeEdit.Items.Add(SurveyType.Planet);
            cmbSurveyTypeEdit.Items.Add(SurveyType.Asteroid);
            cmbSurveyTypeEdit.SelectedIndex = 0;
            cmbSurveyTypeEdit.SelectedIndexChanged += CmbSurveyTypeEdit_SelectedIndexChanged;
            txtSurveyID.TextChanged += TxtSurveyID_TextChanged;
            txtNickName.TextChanged += TxtNickName_TextChanged;
            txtScannedBy.TextChanged += TxtScannedBy_TextChanged;
            dtpScanDateTime.ValueChanged += DtpScanDateTime_ValueChanged;
            txtSensorAbundance.TextChanged += TxtSensorAbundance_TextChanged;
            txtPurityModifier.TextChanged += TxtPurityModifier_TextChanged;
            txtScanLevel.TextChanged += TxtScanLevel_TextChanged;
            dgvResources.CellValueChanged += DgvResources_CellValueChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.SurveyDataChanged += OnSurveyDataChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpSurveyData.Layout += FlpSurveyData_Layout;

            // Context menu event wiring
            tsmiAddResource.Click += TsmiAddResource_Click;
            tsmiRemoveResource.Click += TsmiRemoveResource_Click;
            dgvResources.CellMouseClick += DgvResources_CellMouseClick;
            cmsResources.Opening += CmsResources_Opening;

            UpdateSaveButtonState();
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // -----------------------------------------------------------------------
        // Form Lifecycle
        // -----------------------------------------------------------------------

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentSurvey();
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
            playerContext.SurveyDataChanged -= OnSurveyDataChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpSurveyData.Size = new Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpSurveyData.Margin.Left - flpSurveyData.Margin.Right,
                flpBase.Size.Height - flpSurveyData.Margin.Top - flpSurveyData.Margin.Bottom);
            flpSearchList.Size = new Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwSurveys.Size = new Size(
                lvwSurveys.Size.Width,
                flpSearchList.Size.Height - flpSurveyFilter.Size.Height - flpSurveyFilter.Margin.Top - flpSurveyFilter.Margin.Bottom - flpResource.Size.Height - flpResource.Margin.Top - flpResource.Margin.Bottom - lvwSurveys.Margin.Top - lvwSurveys.Margin.Bottom);
        }

        private void FlpSurveyData_Layout(object sender, LayoutEventArgs e)
        {
            flpSurveyDetails.Size = new Size(
                flpSurveyData.Size.Width - flpSurveyDetails.Margin.Left - flpSurveyDetails.Margin.Right,
                flpSurveyData.Size.Height - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom - flpSurveyDetails.Margin.Top - flpSurveyDetails.Margin.Bottom);

            // Size the resources grid to fill remaining space in flpSurveyDetails
            int usedHeight = 0;
            foreach (Control c in flpSurveyDetails.Controls)
            {
                if (c != dgvResources)
                {
                    usedHeight += c.Size.Height + c.Margin.Top + c.Margin.Bottom;
                }
            }

            int gridHeight = flpSurveyDetails.Size.Height - usedHeight - dgvResources.Margin.Top - dgvResources.Margin.Bottom;
            if (gridHeight < 50)
            {
                gridHeight = 50;
            }

            dgvResources.Size = new Size(
                flpSurveyDetails.Size.Width - dgvResources.Margin.Left - dgvResources.Margin.Right,
                gridHeight);
        }

        // -----------------------------------------------------------------------
        // Event Handlers (PlayerContext)
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

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

            lvwSurveys.Items.Clear();
            _viewModel.Reset();
            _previousSelectedUUID = null;
            ClearForm();
            RefreshSurveyList();
            UpdateTitle();
        }

        private void OnSurveyDataChanged(object sender, SurveyDataChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnSurveyDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            RefreshSurveyList();
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnColonyDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            // Refresh list to update Refs column (miner survey assignments may have changed)
            RefreshSurveyList();
            UpdateDeleteButtonState();
        }

        // -----------------------------------------------------------------------
        // Scanner Blueprint List
        // -----------------------------------------------------------------------

        private void PopulateScannerBlueprintList(string currentValue)
        {
            var sw = Stopwatch.StartNew();
            BlueprintType scanners = empireContext.FindBlueprintType("SystemObjectScanner");
            var list = playerContext.GetAllBlueprints()
                .Where(b => b.BluePrintType == scanners.Id)
                .ToList();
            list.Insert(0, new Blueprint());
            _scannerBlueprintItems = list;
            var displayNames = _scannerBlueprintItems.Select(b => b.ExtendedName ?? string.Empty).ToList();
            cmbScannerBlueprint.SetItems(displayNames, currentValue);
            sw.Stop();
            Log.Info("PERF PopulateScannerBlueprintList: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Survey List View (ReadOnlySurvey wrappers)
        // -----------------------------------------------------------------------

        private void PopulateListView(List<ReadOnlySurvey> surveys)
        {
            if (surveys == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            var counter = new SurveyReferenceCounter(playerContext.ColonyList);

            // Track which surveys are currently in the list
            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                var ro = item.Tag as ReadOnlySurvey;
                if (ro != null)
                {
                    viewableSurveys[ro.UUID] = item;
                }
            }

            foreach (ReadOnlySurvey survey in surveys)
            {
                ListViewItem item;
                bool found = viewableSurveys.TryGetValue(survey.UUID, out item);
                string refCount = counter.CountReferences(survey.UUID).TotalCount.ToString();
                if (!found)
                {
                    item = new ListViewItem(survey.UUID);
                    item.SubItems.Add(survey.SystemName);
                    item.SubItems.Add(survey.PlanetName);
                    item.SubItems.Add(survey.SurveyID);
                    item.SubItems.Add(survey.NickName);
                    var dtSubItem = item.SubItems.Add(SurveyDateTimeParser.FormatForDisplay(survey.DateTime));
                    dtSubItem.Tag = survey.DateTime;
                    item.SubItems.Add(refCount);
                }
                else
                {
                    item.SubItems[1].Text = survey.SystemName;
                    item.SubItems[2].Text = survey.PlanetName;
                    item.SubItems[3].Text = survey.SurveyID;
                    item.SubItems[4].Text = survey.NickName;
                    item.SubItems[5].Text = SurveyDateTimeParser.FormatForDisplay(survey.DateTime);
                    item.SubItems[5].Tag = survey.DateTime;
                    if (item.SubItems.Count > 6)
                    {
                        item.SubItems[6].Text = refCount;
                    }
                    else
                    {
                        item.SubItems.Add(refCount);
                    }
                }

                item.Tag = survey;
                item.SubItems[0].Tag = survey;

                if (!found)
                {
                    lvwSurveys.Items.Add(item);
                }
                else
                {
                    viewableSurveys.Remove(survey.UUID);
                }
            }

            foreach (KeyValuePair<string, ListViewItem> viewableSurvey in viewableSurveys)
            {
                lvwSurveys.Items.Remove(viewableSurvey.Value);
            }

            sw.Stop();
            Log.Info("PERF PopulateListView: total={0}ms items={1}", sw.ElapsedMilliseconds, surveys.Count);
        }

        // -----------------------------------------------------------------------
        // Filtering
        // -----------------------------------------------------------------------

        private List<ReadOnlySurvey> GetFilteredSurveys()
        {
            var list = playerContext.GetCurrentPlayerReadOnlySurveys();
            string nameFilter = txtSurveyFilter.Text;
            string resourceFilter = GetSelectedResourceName();
            SurveyType? typeFilter = GetSelectedSurveyType();
            string purityFilter = GetSelectedPurityFilter();
            int minAmount = GetMinAmount();

            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(s => s.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0
                             || (s.SystemName != null && s.SystemName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            if (typeFilter.HasValue)
            {
                list = list.Where(s => s.SurveyType == typeFilter.Value).ToList();
            }

            // Resource, purity, and amount filters must match the SAME resource record
            bool hasResourceFilter = !string.IsNullOrEmpty(resourceFilter);
            bool hasPurityFilter = !string.IsNullOrEmpty(purityFilter);
            bool hasAmountFilter = minAmount > 0;

            if (hasResourceFilter || hasPurityFilter || hasAmountFilter)
            {
                list = list
                    .Where(s => s.Resources.Values.Any(r =>
                    {
                        if (hasResourceFilter &&
                            !string.Equals(r.Resource, resourceFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        if (hasPurityFilter &&
                            !string.Equals(r.Purity, purityFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        if (hasAmountFilter)
                        {
                            if (!decimal.TryParse(r.Amount, out decimal amt) || amt < minAmount)
                            {
                                return false;
                            }
                        }

                        return true;
                    }))
                    .ToList();
            }

            return list;
        }

        private void TxtSurveyFilter_TextChanged(object sender, EventArgs e)
        {
            lvwSurveys.Items.Clear();
            PopulateListView(GetFilteredSurveys());
        }

        private void CmbResource_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvwSurveys.Items.Clear();
            PopulateListView(GetFilteredSurveys());
        }

        private string GetSelectedResourceName()
        {
            var selected = cmbResource.SelectedItem as Models.Resource;
            if (selected == null || selected.Name == "(all)")
            {
                return string.Empty;
            }

            return selected.Name;
        }

        private SurveyType? GetSelectedSurveyType()
        {
            string sel = cmbSurveyType.SelectedItem?.ToString() ?? "All";
            switch (sel)
            {
                case "Planet": return SurveyType.Planet;
                case "Asteroid": return SurveyType.Asteroid;
                default: return null;
            }
        }

        private string GetSelectedPurityFilter()
        {
            string sel = cmbPurityFilter.SelectedItem?.ToString() ?? "(any)";
            return sel == "(any)" ? string.Empty : sel;
        }

        private int GetMinAmount()
        {
            int.TryParse(txtMinAmount.Text.Trim(), out int val);
            return val;
        }

        private void RefreshSurveyList()
        {
            var sw = Stopwatch.StartNew();
            lvwSurveys.Items.Clear();
            PopulateListView(GetFilteredSurveys());
            sw.Stop();
            Log.Info("PERF RefreshSurveyList: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbSurveyType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            RefreshSurveyList();
        }

        private void CmbPurityFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            RefreshSurveyList();
        }

        private void TxtMinAmount_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            RefreshSurveyList();
        }

        // -----------------------------------------------------------------------
        // Control Change Handlers (local-only ViewModel updates)
        // -----------------------------------------------------------------------

        private void TxtPlanetName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.PlanetName = txtPlanetName.Text;
            UpdateSaveButtonState();
        }

        private void TxtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.SystemName = txtSystemName.Text;
            UpdateSaveButtonState();
        }

        private void CmbSurveyTypeEdit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (cmbSurveyTypeEdit.SelectedItem is SurveyType st)
            {
                _viewModel.SurveyTypeValue = st;
                UpdateNameLabel(st);
                UpdateSaveButtonState();
            }
        }

        private void UpdateNameLabel(SurveyType surveyType)
        {
            lblPlanetName.Text = surveyType == SurveyType.Asteroid ? "Asteroid Name" : "Planet Name";
        }

        private void TxtSurveyID_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.SurveyID = txtSurveyID.Text;
            UpdateSaveButtonState();
        }

        private void TxtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.NickName = txtNickName.Text;
            UpdateSaveButtonState();
        }

        private void TxtScannedBy_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.ScannedBy = txtScannedBy.Text;
            UpdateSaveButtonState();
        }

        private void DtpScanDateTime_ValueChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.DateTime = SurveyDateTimeParser.ToIsoString(dtpScanDateTime.Value.ToUniversalTime());
            using var guard = new ProgrammaticUpdateGuard(this);
            txtScanDateTime.Text = SurveyDateTimeParser.ToGameFormat(dtpScanDateTime.Value);
            UpdateSaveButtonState();
        }

        private void TxtSensorAbundance_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.SensorAbundance = txtSensorAbundance.Text;
            UpdateSaveButtonState();
        }

        private void TxtPurityModifier_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.PurityModifier = txtPurityModifier.Text;
            UpdateSaveButtonState();
        }

        private void TxtScanLevel_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.ScanLevel = txtScanLevel.Text;
            UpdateSaveButtonState();
        }

        private void CmbScannerBlueprint_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int idx = cmbScannerBlueprint.SelectedFullIndex;
            var bp = (idx >= 0 && idx < _scannerBlueprintItems.Count) ? _scannerBlueprintItems[idx] : null;
            _viewModel.ScannerBlueprintUUID = bp?.UUID ?? string.Empty;
            UpdateSaveButtonState();
        }

        private void DgvResources_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;

            // Sync the entire grid to _viewModel.Resources
            _viewModel.Resources.Clear();
            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                if (row.IsNewRow) continue;
                string resourceName = row.Cells[0].Value as string;
                string resourcePurity = row.Cells[1].Value as string;
                string resourceAmount = row.Cells[2].Value as string;
                if (resourceName != null)
                {
                    _viewModel.Resources[resourceName] = new SurveyResource(resourceName, resourcePurity, resourceAmount);
                }
            }

            UpdateSaveButtonState();
        }

        // -----------------------------------------------------------------------
        // Selection Handler
        // -----------------------------------------------------------------------

        private void LvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlySurvey plan)
            {
                // Prompt for unsaved changes before switching
                if (_viewModel.IsDirty)
                {
                    var result = PromptUnsavedChanges();
                    if (result == DialogResult.Yes)
                    {
                        SaveCurrentSurvey();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Restore previous selection
                        lvwSurveys.ItemSelectionChanged -= LvwSurveys_ItemSelectionChanged;
                        lvwSurveys.SelectedItems.Clear();
                        if (!string.IsNullOrEmpty(_previousSelectedUUID))
                        {
                            foreach (ListViewItem item in lvwSurveys.Items)
                            {
                                if ((item.Tag as ReadOnlySurvey)?.UUID == _previousSelectedUUID)
                                {
                                    item.Selected = true;
                                    item.EnsureVisible();
                                    break;
                                }
                            }
                        }

                        lvwSurveys.ItemSelectionChanged += LvwSurveys_ItemSelectionChanged;
                        return;
                    }

                    // DialogResult.No - discard, fall through to load new
                }

                _viewModel.LoadFrom(plan);
                _previousSelectedUUID = plan.UUID;
                PopulateFormFromViewModel();
                UpdateDeleteButtonState();
                UpdateSaveButtonState();
            }
            else if (!e.IsSelected && lvwSurveys.SelectedItems.Count == 0)
            {
                ClearForm();
            }
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateFormFromViewModel()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanetName.Text = _viewModel.PlanetName ?? string.Empty;
            txtSystemName.Text = _viewModel.SystemName ?? string.Empty;
            for (int i = 0; i < cmbSurveyTypeEdit.Items.Count; i++)
            {
                if ((SurveyType)cmbSurveyTypeEdit.Items[i] == _viewModel.SurveyTypeValue)
                {
                    cmbSurveyTypeEdit.SelectedIndex = i;
                    break;
                }
            }

            UpdateNameLabel(_viewModel.SurveyTypeValue);
            txtSurveyID.Text = _viewModel.SurveyID ?? string.Empty;
            txtNickName.Text = _viewModel.NickName ?? string.Empty;
            txtScannedBy.Text = _viewModel.ScannedBy ?? string.Empty;
            txtScanDateTime.Text = _viewModel.DisplayDateTime ?? string.Empty;
            if (SurveyDateTimeParser.TryParseIso(_viewModel.DateTime, out DateTime parsedDt))
            {
                dtpScanDateTime.Value = parsedDt.ToLocalTime();
            }
            else
            {
                dtpScanDateTime.Value = DateTime.Now;
            }

            txtSensorAbundance.Text = _viewModel.SensorAbundance ?? string.Empty;
            txtPurityModifier.Text = _viewModel.PurityModifier ?? string.Empty;
            txtScanLevel.Text = _viewModel.ScanLevel ?? string.Empty;

            long t1 = sw.ElapsedMilliseconds;

            // Scanner blueprint lookup
            string scannerBpName = string.Empty;
            if (!string.IsNullOrEmpty(_viewModel.ScannerBlueprintUUID))
            {
                var scannerBp = playerContext.FindBlueprint(_viewModel.ScannerBlueprintUUID);
                scannerBpName = scannerBp?.ExtendedName ?? string.Empty;
            }

            PopulateScannerBlueprintList(scannerBpName);

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
            foreach (KeyValuePair<string, SurveyResource> resource in _viewModel.Resources)
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value.Purity;
                row.Cells[2].Value = resource.Value.Amount;
            }

            // Populate Max Reserve column from linked asteroid (asteroid surveys only)
            if (!string.IsNullOrEmpty(_viewModel.AsteroidUUID))
            {
                var linkedAsteroid = playerContext.AsteroidList.FirstOrDefault(a => a.UUID == _viewModel.AsteroidUUID);
                if (linkedAsteroid != null && linkedAsteroid.Reserves != null && linkedAsteroid.Reserves.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvResources.Rows)
                    {
                        if (row.IsNewRow) continue;
                        string resName = row.Cells[0].Value as string;
                        string resPurity = row.Cells[1].Value as string;
                        if (string.IsNullOrEmpty(resName)) continue;

                        var reserve = linkedAsteroid.Reserves.FirstOrDefault(r =>
                            string.Equals(r.ResourceName, resName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Purity, resPurity, StringComparison.OrdinalIgnoreCase));
                        if (reserve != null)
                        {
                            row.Cells["MaxReserve"].Value = reserve.MaxReserve.ToString("N0");
                        }
                    }
                }
            }

            sw.Stop();
            Log.Info(
                "PopulateFormFromViewModel PERF: total={0}ms fields={1}ms grid={2}ms",
                sw.ElapsedMilliseconds,
                t1,
                sw.ElapsedMilliseconds - t1);
            sw.Stop();
            Log.Info("PERF PopulateFormFromViewModel: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            _viewModel.Reset();

            txtPlanetName.Text = string.Empty;
            txtSystemName.Text = string.Empty;
            cmbSurveyTypeEdit.SelectedIndex = 0;
            UpdateNameLabel(SurveyType.Planet);
            txtSurveyID.Text = string.Empty;
            txtNickName.Text = string.Empty;
            txtScannedBy.Text = string.Empty;
            dtpScanDateTime.Value = DateTime.Now;
            txtScanDateTime.Text = SurveyDateTimeParser.ToGameFormat(DateTime.Now);
            txtSensorAbundance.Text = string.Empty;
            txtPurityModifier.Text = string.Empty;
            txtScanLevel.Text = string.Empty;

            PopulateScannerBlueprintList(null);
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
            UpdateSaveButtonState();
        }

        private void DgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the Amount column (index 2)
            if (e.ColumnIndex != 2) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = string.Empty;
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                value,
                OE2EmpireTracker.Constants.BlueprintPropertyValidation.DecimalPattern))
            {
                e.Cancel = true;
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                dgvResources.Rows[e.RowIndex].ErrorText = "Amount must be a decimal number";
            }
            else
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        // -----------------------------------------------------------------------
        // Context Menu Handlers
        // -----------------------------------------------------------------------

        private void TsmiAddResource_Click(object sender, EventArgs e)
        {
            dgvResources.Rows.Add();
        }

        private void TsmiRemoveResource_Click(object sender, EventArgs e)
        {
            if (dgvResources.CurrentRow == null || dgvResources.CurrentRow.IsNewRow)
            {
                return;
            }

            dgvResources.Rows.RemoveAt(dgvResources.CurrentRow.Index);
        }

        private void DgvResources_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            if (e.RowIndex >= 0)
            {
                dgvResources.ClearSelection();
                dgvResources.Rows[e.RowIndex].Selected = true;
                dgvResources.CurrentCell = dgvResources.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvResources.ClearSelection();
            }
        }

        private void CmsResources_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvResources.CurrentRow != null && !dgvResources.CurrentRow.IsNewRow;
            tsmiRemoveResource.Enabled = hasSelection;
        }

        // -----------------------------------------------------------------------
        // Dirty Tracking / Unsaved Changes
        // -----------------------------------------------------------------------

        /// <summary>
        /// Enables the Save button only when the ViewModel has unsaved changes.
        /// </summary>
        private void UpdateSaveButtonState()
        {
            btnSave.Enabled = _viewModel.IsDirty;
        }

        /// <summary>
        /// Prompts the user to save, discard, or cancel when there are unsaved changes.
        /// Returns Yes (save), No (discard), or Cancel.
        /// </summary>
        private DialogResult PromptUnsavedChanges()
        {
            return MessageBox.Show(
                string.Format("Save changes to '{0}'?", _viewModel.PlanetName),
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        /// <summary>
        /// Saves the current survey via the service (Create or Update) and reloads the ViewModel.
        /// </summary>
        private void SaveCurrentSurvey()
        {
            // Sync resources from grid before saving
            SyncResourcesFromGrid();

            ReadOnlySurvey saved;
            if (_viewModel.IsNew)
            {
                saved = _surveyService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _surveyService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _viewModel.LoadFrom(saved);
            _previousSelectedUUID = saved.UUID;
            RefreshSurveyList();
            SelectSurveyInList(saved.UUID);
            PopulateFormFromViewModel();
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Syncs the resource grid contents into the ViewModel Resources dictionary.
        /// </summary>
        private void SyncResourcesFromGrid()
        {
            dgvResources.CellValidating -= DgvResources_CellValidating;
            try
            {
                dgvResources.EndEdit();
            }
            catch
            {
            }

            dgvResources.CellValidating += DgvResources_CellValidating;

            _viewModel.Resources.Clear();
            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                if (row.IsNewRow) continue;
                string resourceName = row.Cells[0].Value as string;
                string resourcePurity = row.Cells[1].Value as string;
                string resourceAmount = row.Cells[2].Value as string;
                if (resourceName != null)
                {
                    _viewModel.Resources[resourceName] = new SurveyResource(resourceName, resourcePurity, resourceAmount);
                }
            }
        }

        /// <summary>
        /// Selects the survey with the given UUID in the list view and scrolls it into view.
        /// </summary>
        private void SelectSurveyInList(string uuid)
        {
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                if ((item.Tag as ReadOnlySurvey)?.UUID == uuid)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        // -----------------------------------------------------------------------
        // CRUD Operations
        // -----------------------------------------------------------------------

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SyncResourcesFromGrid();
            SaveCurrentSurvey();
            UpdateTitle();
        }

        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentSurvey();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            _viewModel.Reset();
            _previousSelectedUUID = null;
            lvwSurveys.SelectedItems.Clear();
            ClearForm();
            UpdateSaveButtonState();
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.UUID))
            {
                return;
            }

            var counter = new SurveyReferenceCounter(playerContext.ColonyList);
            var report = counter.CountReferences(_viewModel.UUID);
            if (report.TotalCount > 0)
            {
                var msg = string.Format(
                    "Cannot delete '{0}' -- it is assigned to {1} mining rig(s).",
                    _viewModel.PlanetName,
                    report.MinerCount);
                MessageBox.Show(msg, "Survey In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete survey '{0}'?", _viewModel.PlanetName),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            _surveyService.Delete(_viewModel.UUID);
            _viewModel.Reset();
            _previousSelectedUUID = null;
            RefreshSurveyList();
            lvwSurveys.SelectedItems.Clear();
            ClearForm();
            UpdateTitle();
        }

        private void CmdImport_Click(object sender, EventArgs e)
        {
            // Check for unsaved changes before import
            if (_viewModel.IsDirty)
            {
                var dirtyResult = PromptUnsavedChanges();
                if (dirtyResult == DialogResult.Yes)
                {
                    SaveCurrentSurvey();
                }
                else if (dirtyResult == DialogResult.Cancel)
                {
                    return;
                }
            }

            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show(
                    "No HTML content found on the clipboard.\n\nCopy survey data from the game browser first.",
                    "No HTML",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                MessageBox.Show(
                    "No player selected. Select a player profile first.",
                    "No Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Validate clipboard contains survey data
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string htmlFragment = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                var detected = Parsers.ClipboardContentDetector.Detect(htmlFragment);
                if (detected != Parsers.ClipboardContentDetector.ContentType.Survey &&
                    detected != Parsers.ClipboardContentDetector.ContentType.Unknown)
                {
                    string found = Parsers.ClipboardContentDetector.GetDescription(detected);
                    MessageBox.Show(
                        string.Format(
                            "The clipboard contains {0}, not survey data.\n\nCopy the survey page from the game browser first.",
                            found),
                        "Wrong Content",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Log.Info("Survey import started from clipboard");
                var parser = new SurveyParser();
                var tempSurvey = parser.ParseClipboardToTemp(out string extractedHtml);

                if (tempSurvey == null)
                {
                    return;
                }

                // Import through service
                ReadOnlySurvey imported = _surveyService.Import(tempSurvey);

                Log.Info(
                    "Survey import complete: UUID={0}, PlanetName='{1}', SurveyID='{2}'",
                    imported.UUID,
                    imported.PlanetName,
                    imported.SurveyID);

                // Refresh list view and select imported survey
                RefreshSurveyList();
                SelectSurveyInList(imported.UUID);
                _viewModel.LoadFrom(imported);
                _previousSelectedUUID = imported.UUID;
                PopulateFormFromViewModel();
                UpdateSaveButtonState();
                UpdateTitle();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing survey from clipboard");
                MessageBox.Show(
                    "Failed to import survey: " + ex.Message,
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // -----------------------------------------------------------------------
        // Utility
        // -----------------------------------------------------------------------

        private void UpdateTitle()
        {
            var player = playerContext.CurrentPlayer;
            string playerName = player != null ? player.Name : "No Player";
            int surveyCount = playerContext.GetCurrentPlayerSurveys()?.Count ?? 0;
            string prefix = Tag != null ? "#" + Tag + " - " : string.Empty;
            Text = string.Format("{0}Manage Surveys - {1} : {2}", prefix, playerName, surveyCount);
        }

        private void UpdateDeleteButtonState()
        {
            if (string.IsNullOrEmpty(_viewModel?.UUID))
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = "Delete";
                return;
            }

            var counter = new SurveyReferenceCounter(playerContext.ColonyList);
            var report = counter.CountReferences(_viewModel.UUID);
            if (report.TotalCount > 0)
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = string.Format("In Use ({0})", report.TotalCount);
            }
            else
            {
                cmdDelete.Enabled = true;
                cmdDelete.Text = "Delete";
            }
        }

        private void LvwSurveys_ColumnClick(object sender, ColumnClickEventArgs e)
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

            lvwSurveys.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            lvwSurveys.Sort();
        }
    }
}
