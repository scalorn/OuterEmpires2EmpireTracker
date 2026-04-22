using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using NLog;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Forms.Survey
{
    /// <summary>
    /// FormSurvey - Main form for managing survey data in the OE2 Empire Tracker.
    /// </summary>
    public partial class FormSurvey : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private SurveyViewModel viewModel;
        private int _sortColumn = 1; // PlanetName
        private SortOrder _sortOrder = SortOrder.Ascending;

        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new SurveyViewModel(new OE2EmpireTracker.Models.Survey(), playerContext, empireContext);

            // Configure scanner blueprint combo box
            cmbScannerBlueprint.DisplayMember = "ExtendedName";
            cmbScannerBlueprint.ValueMember = "UUID";
            UpdateScannerBlueprintList();
            cmbScannerBlueprint.SelectedIndex = -1;

            // Set up survey list view with columns
            lvwSurveys.View = View.Details;
            lvwSurveys.Columns.Add("UUID", 0);
            lvwSurveys.Columns.Add("PlanetName", 100);
            lvwSurveys.Columns.Add("SurveyID", 70);
            lvwSurveys.Columns.Add("NickName", 100);
            lvwSurveys.Columns.Add("DateTime", 100);
            lvwSurveys.Columns.Add("Refs", 35);
            lvwSurveys.ColumnClick += lvwSurveys_ColumnClick;
            lvwSurveys.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName()));
            UpdateTitle();

            // Wire survey list filter
            txtSurveyFilter.TextChanged += txtSurveyFilter_TextChanged;

            // Populate resource filter combo
            cmbResource.DisplayMember = "Name";
            cmbResource.Items.Add(new Models.Resource { Name = "(all)" });
            foreach (var r in empireContext.ResourceList)
                cmbResource.Items.Add(r);
            cmbResource.SelectedIndex = 0;
            cmbResource.SelectedIndexChanged += cmbResource_SelectedIndexChanged;

            // Wire additional filters (task 40.2)
            cmbSurveyType.Items.Add("All");
            cmbSurveyType.Items.Add("Planet");
            cmbSurveyType.Items.Add("Asteroid");
            cmbSurveyType.SelectedIndex = 0;
            cmbSurveyType.SelectedIndexChanged += cmbSurveyType_SelectedIndexChanged;

            cmbPurityFilter.Items.Add("(any)");
            foreach (var p in Models.ResourcePurity.Purities)
            {
                if (p.ID != Models.ResourcePurity.PurityEnum.None)
                    cmbPurityFilter.Items.Add(p.Name);
            }
            cmbPurityFilter.SelectedIndex = 0;
            cmbPurityFilter.SelectedIndexChanged += cmbPurityFilter_SelectedIndexChanged;

            txtMinAmount.TextChanged += txtMinAmount_TextChanged;

            // Configure resource data grid
            DataGridViewComboBoxColumn colResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            colResource.DisplayMember = "Name";
            colResource.ValueMember = "Name";
            colResource.DataSource = empireContext.BindingSourceResource;

            DataGridViewComboBoxColumn cmbPurity = (DataGridViewComboBoxColumn)dgvResources.Columns["Purity"];
            cmbPurity.DisplayMember = "Name";
            cmbPurity.ValueMember = "Name";
            cmbPurity.DataSource = empireContext.BindingSourceResourcePurity;

            dgvResources.DataError += (s, ev) => { Log.Warn("dgvResources DataError at [{0},{1}]: {2}", ev.RowIndex, ev.ColumnIndex, ev.Exception?.Message); ev.ThrowException = false; };

            // Add read-only Max Reserve column (programmatic — not in Designer)
            var colMaxReserve = new DataGridViewTextBoxColumn();
            colMaxReserve.HeaderText = "Max Reserve";
            colMaxReserve.Name = "MaxReserve";
            colMaxReserve.ReadOnly = true;
            colMaxReserve.Width = 100;
            dgvResources.Columns.Add(colMaxReserve);

            // Wire write-through handlers
            txtPlanetName.TextChanged += txtPlanetName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;
            cmbSurveyTypeEdit.Items.Add(SurveyType.Planet);
            cmbSurveyTypeEdit.Items.Add(SurveyType.Asteroid);
            cmbSurveyTypeEdit.SelectedIndex = 0;
            cmbSurveyTypeEdit.SelectedIndexChanged += cmbSurveyTypeEdit_SelectedIndexChanged;
            txtSurveyID.TextChanged += txtSurveyID_TextChanged;
            txtNickName.TextChanged += txtNickName_TextChanged;
            txtScannedBy.TextChanged += txtScannedBy_TextChanged;
            dtpScanDateTime.ValueChanged += dtpScanDateTime_ValueChanged;
            txtSensorAbundance.TextChanged += txtSensorAbundance_TextChanged;
            txtPurityModifier.TextChanged += txtPurityModifier_TextChanged;
            txtScanLevel.TextChanged += txtScanLevel_TextChanged;
            cmbScannerBlueprint.SelectedIndexChanged += cmbScannerBlueprint_SelectedIndexChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.SurveyDataChanged += OnSurveyDataChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpSurveyData.Layout += flpSurveyData_Layout;
        }

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpSurveyData.Size = new System.Drawing.Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpSurveyData.Margin.Left - flpSurveyData.Margin.Right,
                flpBase.Size.Height - flpSurveyData.Margin.Top - flpSurveyData.Margin.Bottom);
            flpSearchList.Size = new System.Drawing.Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwSurveys.Size = new System.Drawing.Size(
                lvwSurveys.Size.Width,
                flpSearchList.Size.Height - flpSurveyFilter.Size.Height - flpSurveyFilter.Margin.Top - flpSurveyFilter.Margin.Bottom - flpResource.Size.Height - flpResource.Margin.Top - flpResource.Margin.Bottom - lvwSurveys.Margin.Top - lvwSurveys.Margin.Bottom);
        }

        private void flpSurveyData_Layout(object sender, LayoutEventArgs e)
        {
            flpSurveyDetails.Size = new System.Drawing.Size(
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
            if (gridHeight < 50) gridHeight = 50;
            dgvResources.Size = new System.Drawing.Size(
                flpSurveyDetails.Size.Width - dgvResources.Margin.Left - dgvResources.Margin.Right,
                gridHeight);
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
            lvwSurveys.Items.Clear();
            viewModel.Reset();
            ClearForm();
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName()));
            UpdateTitle();
        }

        private void OnSurveyDataChanged(object sender, SurveyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnSurveyDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            if (viewModel.UUID == e.SurveyUUID)
            {
                PopulateFormFromViewModel();
            }
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName()));
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnColonyDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            // Refresh list to update Refs column (miner survey assignments may have changed)
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName()));
            UpdateDeleteButtonState();
        }

        private void UpdateScannerBlueprintList()
        {
            string searchText = txtFilterScannerBlueprint.Text;
            var filteredList = viewModel.GetFilteredScannerBlueprints(searchText);

            BindingSource filteredSource = new BindingSource();
            filteredSource.DataSource = filteredList;
            cmbScannerBlueprint.DataSource = filteredSource;
        }

        void PopulateListView(IReadOnlyList<OE2EmpireTracker.Models.Survey> surveys)
        {
            if (surveys == null) return;

            var sw = Stopwatch.StartNew();
            var counter = new SurveyReferenceCounter(playerContext.ColonyList);

            // Track which surveys are currently in the list
            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                viewableSurveys[(item.Tag as OE2EmpireTracker.Models.Survey).UUID] = item;
            }

            foreach (OE2EmpireTracker.Models.Survey survey in surveys)
            {
                ListViewItem item;
                bool found = viewableSurveys.TryGetValue(survey.UUID, out item);
                string refCount = counter.CountReferences(survey.UUID).TotalCount.ToString();
                if (!found)
                {
                    item = new ListViewItem(survey.UUID);
                    item.SubItems.Add(survey.PlanetName);
                    item.SubItems.Add(survey.SurveyID);
                    item.SubItems.Add(survey.NickName);
                    var dtSubItem = item.SubItems.Add(SurveyDateTimeParser.FormatForDisplay(survey.DateTime));
                    dtSubItem.Tag = survey.DateTime;
                    item.SubItems.Add(refCount);
                }
                else
                {
                    item.SubItems[1].Text = survey.PlanetName;
                    item.SubItems[2].Text = survey.SurveyID;
                    item.SubItems[3].Text = survey.NickName;
                    item.SubItems[4].Text = SurveyDateTimeParser.FormatForDisplay(survey.DateTime);
                    item.SubItems[4].Tag = survey.DateTime;
                    if (item.SubItems.Count > 5)
                        item.SubItems[5].Text = refCount;
                    else
                        item.SubItems.Add(refCount);
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
            Log.Info("PopulateListView PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds, surveys.Count);
        }

        private void txtSurveyFilter_TextChanged(object sender, EventArgs e)
        {
            lvwSurveys.Items.Clear();
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName(),
                GetSelectedSurveyType(), GetSelectedPurityFilter(), GetMinAmount()));
        }

        private void cmbResource_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvwSurveys.Items.Clear();
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName(),
                GetSelectedSurveyType(), GetSelectedPurityFilter(), GetMinAmount()));
        }

        private string GetSelectedResourceName()
        {
            var selected = cmbResource.SelectedItem as Models.Resource;
            if (selected == null || selected.Name == "(all)") return "";
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
            return sel == "(any)" ? "" : sel;
        }

        private int GetMinAmount()
        {
            int.TryParse(txtMinAmount.Text.Trim(), out int val);
            return val;
        }

        private void RefreshSurveyList()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            lvwSurveys.Items.Clear();
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName(),
                GetSelectedSurveyType(), GetSelectedPurityFilter(), GetMinAmount()));
            sw.Stop(); Log.Info("PERF RefreshSurveyList: {0}ms", sw.ElapsedMilliseconds);
        }

        private void cmbSurveyType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            RefreshSurveyList();
        }

        private void cmbPurityFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            RefreshSurveyList();
        }

        private void txtMinAmount_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            RefreshSurveyList();
        }

        private void txtPlanetName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PlanetName = txtPlanetName.Text;
        }

        private void txtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.SystemName = txtSystemName.Text;
        }

        private void cmbSurveyTypeEdit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (cmbSurveyTypeEdit.SelectedItem is SurveyType st)
            {
                viewModel.SurveyTypeValue = st;
                UpdateNameLabel(st);
            }
        }

        private void UpdateNameLabel(SurveyType surveyType)
        {
            lblPlanetName.Text = surveyType == SurveyType.Asteroid ? "Asteroid Name" : "Planet Name";
        }

        private void txtSurveyID_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.SurveyID = txtSurveyID.Text;
        }

        private void txtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.NickName = txtNickName.Text;
        }

        private void txtScannedBy_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.ScannedBy = txtScannedBy.Text;
        }

        private void dtpScanDateTime_ValueChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.DateTime = SurveyDateTimeParser.ToIsoString(dtpScanDateTime.Value.ToUniversalTime());
            using var guard = new ProgrammaticUpdateGuard(this);
            txtScanDateTime.Text = SurveyDateTimeParser.ToGameFormat(dtpScanDateTime.Value);
        }

        private void txtSensorAbundance_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.SensorAbundance = txtSensorAbundance.Text;
        }

        private void txtPurityModifier_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PurityModifier = txtPurityModifier.Text;
        }

        private void txtScanLevel_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.ScanLevel = txtScanLevel.Text;
        }

        private void cmbScannerBlueprint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bp = cmbScannerBlueprint.SelectedItem as Models.Blueprint;
            viewModel.ScannerBlueprintUUID = bp?.UUID ?? "";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.CellValidating += dgvResources_CellValidating;

            // Map resources from grid
            viewModel.ClearResources();
            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                string resourceName = row.Cells[0].Value as string;
                string resourcePurity = row.Cells[1].Value as string;
                string resourceAmount = row.Cells[2].Value as string;
                if (resourceName != null)
                {
                    var surveyResource = new SurveyResource(resourceName, resourcePurity, resourceAmount);
                    viewModel.SetResource(resourceName, surveyResource);
                }
            }

            viewModel.Save();

            RefreshSurveyList();
            UpdateTitle();
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            txtPlanetName.Text = "";
            txtSystemName.Text = "";
            cmbSurveyTypeEdit.SelectedIndex = 0;
            UpdateNameLabel(SurveyType.Planet);
            txtSurveyID.Text = "";
            txtNickName.Text = "";
            txtScannedBy.Text = "";
            dtpScanDateTime.Value = DateTime.Now;
            txtScanDateTime.Text = SurveyDateTimeParser.ToGameFormat(DateTime.Now);
            txtSensorAbundance.Text = "";
            txtPurityModifier.Text = "";
            txtScanLevel.Text = "";

            cmbScannerBlueprint.SelectedItem = null;
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.UUID)) return;

            var counter = new SurveyReferenceCounter(playerContext.ColonyList);
            var report = counter.CountReferences(viewModel.UUID);
            if (report.TotalCount > 0)
            {
                var msg = $"Cannot delete '{viewModel.Data.PlanetName}' -- it is assigned to {report.MinerCount} mining rig(s).";
                MessageBox.Show(msg, "Survey In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete survey '{viewModel.Data.PlanetName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            viewModel.Delete();
            viewModel.Reset();
            PopulateListView(viewModel.GetFilteredSurveys(txtSurveyFilter.Text, GetSelectedResourceName()));
            lvwSurveys.SelectedItems.Clear();
            ClearForm();
            UpdateTitle();
        }

        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwSurveys.SelectedItems.Clear();
        }

        private void lvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Log.Debug("lvwSurveys.SelectedItems.Count = " + lvwSurveys.SelectedItems.Count);

            if (lvwSurveys.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Text);
                viewModel.SelectSurvey(lvwSurveys.SelectedItems[0].SubItems[0].Tag as OE2EmpireTracker.Models.Survey);
                PopulateForm();
                UpdateDeleteButtonState();
            }
        }

        private void PopulateForm()
        {
            if (string.IsNullOrEmpty(viewModel.UUID)) return;
            PopulateFormFromViewModel();
        }

        private void txtFilterScannerBlueprint_TextChanged(object sender, EventArgs e)
        {
            UpdateScannerBlueprintList();
            cmbScannerBlueprint.DroppedDown = true;
        }

        private void dgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the Amount column (index 2)
            if (e.ColumnIndex != 2) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(value,
                OE2EmpireTracker.Constants.BlueprintPropertyValidation.DECIMAL_PATTERN))
            {
                e.Cancel = true;
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                dgvResources.Rows[e.RowIndex].ErrorText = "Amount must be a decimal number";
            }
            else
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void cmdImport_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML content found on the clipboard.\n\nCopy survey data from the game browser first.",
                    "No HTML", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                MessageBox.Show("No player selected. Select a player profile first.",
                    "No Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    MessageBox.Show($"The clipboard contains {found}, not survey data.\n\nCopy the survey page from the game browser first.",
                        "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Log.Info("Survey import started from clipboard");
                var parser = new SurveyParser();
                var tempSurvey = parser.ParseClipboardToTemp(out string extractedHtml);

                if (tempSurvey == null)
                    return;

                // Fallback: if no PlanetName or no SurveyID parsed, use current behavior
                if (string.IsNullOrEmpty(tempSurvey.PlanetName) || string.IsNullOrEmpty(tempSurvey.SurveyID))
                {
                    parser.ProcessClipboard(viewModel.Data);
                    PopulateFormFromViewModel();
                    Log.Info("Survey imported from clipboard (fallback): {0}", viewModel.Data.PlanetName);
                    return;
                }

                var existingSurvey = SurveyImportHelper.FindByKey(
                    playerContext.GetCurrentPlayerSurveys(), tempSurvey.PlanetName, tempSurvey.SurveyID);

                Log.Info("Survey dedup: {0} for planet '{1}', surveyID '{2}'",
                    existingSurvey != null ? "found existing survey UUID=" + existingSurvey.UUID : "no existing survey, creating new",
                    tempSurvey.PlanetName, tempSurvey.SurveyID);

                OE2EmpireTracker.Models.Survey importedSurvey;

                if (existingSurvey != null)
                {
                    SurveyImportHelper.MergeData(existingSurvey, tempSurvey);
                    importedSurvey = existingSurvey;
                    Log.Info("Survey updated via dedup: {0} ({1})", existingSurvey.PlanetName, existingSurvey.SurveyID);
                }
                else
                {
                    var newSurvey = SurveyImportHelper.CreateFromTemp(tempSurvey, playerContext.CurrentPlayerUUID);
                    playerContext.AddSurvey(newSurvey);
                    importedSurvey = newSurvey;
                    Log.Info("New survey created via dedup: {0} ({1})", newSurvey.PlanetName, newSurvey.SurveyID);
                }

                // Auto-create asteroid if this is an asteroid survey (Flow 12)
                SurveyImportHelper.LinkOrCreateAsteroid(importedSurvey, playerContext);

                playerContext.WriteContext();
                playerContext.OnSurveyDataChanged(importedSurvey.UUID);

                Log.Info("Survey import complete: UUID={0}, PlanetName='{1}', SurveyID='{2}'",
                    importedSurvey.UUID, importedSurvey.PlanetName, importedSurvey.SurveyID);

                // Refresh list view
                RefreshSurveyList();

                // Select the imported survey in the list view
                foreach (ListViewItem item in lvwSurveys.Items)
                {
                    if ((item.Tag as OE2EmpireTracker.Models.Survey)?.UUID == importedSurvey.UUID)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }

                viewModel.SelectSurvey(importedSurvey);
                PopulateFormFromViewModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing survey from clipboard");
                MessageBox.Show("Failed to import survey: " + ex.Message,
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Populates form fields from the current viewModel state.
        /// Unlike PopulateForm(), this does not require a UUID (works for unsaved/imported surveys).
        /// </summary>
        private void PopulateFormFromViewModel()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanetName.Text = viewModel.PlanetName ?? "";
            txtSystemName.Text = viewModel.SystemName ?? "";
            for (int i = 0; i < cmbSurveyTypeEdit.Items.Count; i++)
            {
                if ((SurveyType)cmbSurveyTypeEdit.Items[i] == viewModel.SurveyTypeValue)
                { cmbSurveyTypeEdit.SelectedIndex = i; break; }
            }
            UpdateNameLabel(viewModel.SurveyTypeValue);
            txtSurveyID.Text = viewModel.SurveyID ?? "";
            txtNickName.Text = viewModel.NickName ?? "";
            txtScannedBy.Text = viewModel.ScannedBy ?? "";
            txtScanDateTime.Text = viewModel.DisplayDateTime ?? "";
            if (SurveyDateTimeParser.TryParseIso(viewModel.DateTime, out DateTime parsedDt))
                dtpScanDateTime.Value = parsedDt.ToLocalTime();
            else
                dtpScanDateTime.Value = DateTime.Now;
            txtSensorAbundance.Text = viewModel.SensorAbundance ?? "";
            txtPurityModifier.Text = viewModel.PurityModifier ?? "";
            txtScanLevel.Text = viewModel.ScanLevel ?? "";

            long t1 = sw.ElapsedMilliseconds;

            txtFilterScannerBlueprint.Text = "";
            cmbScannerBlueprint.SelectedItem = viewModel.FindScannerBlueprint();

            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;
            foreach (KeyValuePair<string, SurveyResource> resource in viewModel.GetResources())
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value.Purity;
                row.Cells[2].Value = resource.Value.Amount;
            }

            // Populate Max Reserve column from linked asteroid (asteroid surveys only)
            var linkedAsteroid = viewModel.FindLinkedAsteroid();
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
            sw.Stop();
            Log.Info("PopulateFormFromViewModel PERF: total={0}ms fields={1}ms grid={2}ms",
                sw.ElapsedMilliseconds, t1, sw.ElapsedMilliseconds - t1);
            sw.Stop(); Log.Info("PERF PopulateFormFromViewModel: {0}ms", sw.ElapsedMilliseconds);
        }

        private void UpdateTitle()
        {
            var player = playerContext.CurrentPlayer;
            string playerName = player != null ? player.Name : "No Player";
            int surveyCount = playerContext.GetCurrentPlayerSurveys()?.Count ?? 0;
            string prefix = Tag != null ? "#" + Tag + " - " : "";
            Text = $"{prefix}Manage Surveys - {playerName} : {surveyCount}";
        }

        private void UpdateDeleteButtonState()
        {
            if (string.IsNullOrEmpty(viewModel?.UUID))
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = "Delete";
                return;
            }

            var counter = new SurveyReferenceCounter(playerContext.ColonyList);
            var report = counter.CountReferences(viewModel.UUID);
            if (report.TotalCount > 0)
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = $"In Use ({report.TotalCount})";
            }
            else
            {
                cmdDelete.Enabled = true;
                cmdDelete.Text = "Delete";
            }
        }

        private void lvwSurveys_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }
            lvwSurveys.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            lvwSurveys.Sort();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.SurveyDataChanged -= OnSurveyDataChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }
    }
}
