using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
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

namespace OE2EmpireTracker.Forms.Survey
{
    /// <summary>
    /// FormSurvey - Main form for managing survey data in the OE2 Empire Tracker.
    /// </summary>
    public partial class FormSurvey : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private SurveyViewModel viewModel;

        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new SurveyViewModel(new OE2EmpireTracker.Baseline.Survey(), playerContext, empireContext);

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

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            lvwSurveys.Items.Clear();
            viewModel.Reset();
            ClearForm();
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

        void PopulateListView(IReadOnlyList<OE2EmpireTracker.Baseline.Survey> surveys)
        {
            if (surveys == null) return;

            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                viewableSurveys[(item.Tag as OE2EmpireTracker.Baseline.Survey).UUID] = item;
            }

            foreach (OE2EmpireTracker.Baseline.Survey survey in surveys)
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.CellValidating += dgvResources_CellValidating;

            // Populate viewModel from form fields
            viewModel.PlanetName = txtPlanetName.Text;
            viewModel.SurveyID = txtSurveyID.Text;
            viewModel.ScannedBy = txtScannedBy.Text;
            viewModel.DateTime = txtScanDateTime.Text;
            viewModel.SensorAbundance = txtSensorAbundance.Text;
            viewModel.PurityModifier = txtPurityModifier.Text;
            viewModel.ScanLevel = txtScanLevel.Text;

            // Scanner blueprint
            if (cmbScannerBlueprint.SelectedItem != null)
            {
                Data.Blueprint baseBlueprint = cmbScannerBlueprint.SelectedItem as Data.Blueprint;
                viewModel.ScannerBlueprintUUID = baseBlueprint.UUID;
            }
            else
            {
                viewModel.ScannerBlueprintUUID = "";
            }

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
            viewModel.Reset();
            ClearForm();
        }

        private void ClearForm()
        {
            viewModel.Reset();

            txtPlanetName.Text = "";
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
            if (!string.IsNullOrEmpty(viewModel.UUID))
            {
                viewModel.Delete();
                viewModel.Reset();
                PopulateListView(viewModel.GetFilteredSurveys(null));
                lvwSurveys.SelectedItems.Clear();
                ClearForm();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
        }

        private void lvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Log.Debug("lvwSurveys.SelectedItems.Count = " + lvwSurveys.SelectedItems.Count);

            if (lvwSurveys.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Text);
                viewModel.SelectSurvey(lvwSurveys.SelectedItems[0].SubItems[0].Tag as OE2EmpireTracker.Baseline.Survey);
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
            txtPlanetName.Text = viewModel.PlanetName ?? "";
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
    }
}
