using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
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
            lvwSurveys.Columns.Add("NickName", 100);
            lvwSurveys.Columns.Add("DateTime", 100);
            PopulateListView(viewModel.GetFilteredSurveys(null));

            // Configure resource data grid
            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.bindingSourceResource;

            DataGridViewComboBoxColumn cmbPurity = (DataGridViewComboBoxColumn)dgvResources.Columns["Purity"];
            cmbPurity.DisplayMember = "Name";
            cmbPurity.ValueMember = "Name";
            cmbPurity.DataSource = empireContext.bindingSourceResourcePurity;

            // Wire write-through handlers
            txtPlanetName.TextChanged += txtPlanetName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;
            txtSurveyID.TextChanged += txtSurveyID_TextChanged;
            txtNickName.TextChanged += txtNickName_TextChanged;
            txtScannedBy.TextChanged += txtScannedBy_TextChanged;
            txtScanDateTime.TextChanged += txtScanDateTime_TextChanged;
            txtSensorAbundance.TextChanged += txtSensorAbundance_TextChanged;
            txtPurityModifier.TextChanged += txtPurityModifier_TextChanged;
            txtScanLevel.TextChanged += txtScanLevel_TextChanged;
            cmbScannerBlueprint.SelectedIndexChanged += cmbScannerBlueprint_SelectedIndexChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.SurveyDataChanged += OnSurveyDataChanged;

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
                flpSearchList.Size.Height - flpBlueprintSearch.Size.Height - flpBlueprintSearch.Margin.Top - flpBlueprintSearch.Margin.Bottom - flpResource.Size.Height - flpResource.Margin.Top - flpResource.Margin.Bottom - lvwSurveys.Margin.Top - lvwSurveys.Margin.Bottom);
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
            PopulateListView(viewModel.GetFilteredSurveys(null));
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
            PopulateListView(viewModel.GetFilteredSurveys(null));
        }

        private void UpdateScannerBlueprintList()
        {
            string searchText = txtFilterScannerBlueprint.Text;
            var filteredList = viewModel.GetFilteredScannerBlueprints(searchText);

            BindingSource filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;
            cmbScannerBlueprint.DataSource = filteredItemsBindingList;
        }

        void PopulateListView(IReadOnlyList<OE2EmpireTracker.Models.Survey> surveys)
        {
            if (surveys == null) return;

            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                viewableSurveys[(item.Tag as OE2EmpireTracker.Models.Survey).UUID] = item;
            }

            foreach (OE2EmpireTracker.Models.Survey survey in surveys)
            {
                ListViewItem item;
                bool found = viewableSurveys.TryGetValue(survey.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(survey.UUID);
                }
                item.Tag = survey;
                item.SubItems[0].Tag = survey;
                item.SubItems.Add(survey.PlanetName);
                item.SubItems.Add(survey.NickName);
                item.SubItems.Add(survey.DateTime);

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

        private void txtScanDateTime_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.DateTime = txtScanDateTime.Text;
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

            PopulateListView(viewModel.GetFilteredSurveys(null));
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            txtPlanetName.Text = "";
            txtSystemName.Text = "";
            txtSurveyID.Text = "";
            txtNickName.Text = "";
            txtScannedBy.Text = "";
            txtScanDateTime.Text = "";
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
            var result = MessageBox.Show(
                $"Delete survey '{viewModel.Data.PlanetName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            viewModel.Delete();
            viewModel.Reset();
            PopulateListView(viewModel.GetFilteredSurveys(null));
            lvwSurveys.SelectedItems.Clear();
            ClearForm();
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
            SurveyParser parser = new SurveyParser();
            parser.processClipboard(viewModel.Data);
            PopulateFormFromViewModel();
        }

        /// <summary>
        /// Populates form fields from the current viewModel state.
        /// Unlike PopulateForm(), this does not require a UUID (works for unsaved/imported surveys).
        /// </summary>
        private void PopulateFormFromViewModel()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanetName.Text = viewModel.PlanetName ?? "";
            txtSystemName.Text = viewModel.SystemName ?? "";
            txtSurveyID.Text = viewModel.SurveyID ?? "";
            txtNickName.Text = viewModel.NickName ?? "";
            txtScannedBy.Text = viewModel.ScannedBy ?? "";
            txtScanDateTime.Text = viewModel.DateTime ?? "";
            txtSensorAbundance.Text = viewModel.SensorAbundance ?? "";
            txtPurityModifier.Text = viewModel.PurityModifier ?? "";
            txtScanLevel.Text = viewModel.ScanLevel ?? "";

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
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.SurveyDataChanged -= OnSurveyDataChanged;
            base.OnFormClosed(e);
        }
    }
}
