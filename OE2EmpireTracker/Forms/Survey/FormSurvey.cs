using Microsoft.Extensions.Logging.Abstractions;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OE2EmpireTracker.Forms.Survey
{
    public partial class FormSurvey : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private OE2EmpireTracker.Baseline.Survey selectedSurvey;

        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            cmbScannerBlueprint.DisplayMember = "ExtendedName";
            cmbScannerBlueprint.ValueMember = "UUID";
            updateScannerBlueprintList();
            cmbScannerBlueprint.SelectedIndex = -1;

            lvwSurveys.View = View.Details;
            lvwSurveys.Columns.Add("UUID", 0);
            lvwSurveys.Columns.Add("PlanetName", 100);
            lvwSurveys.Columns.Add("NickName", 100);
            lvwSurveys.Columns.Add("DateTime", 100);
            populateListView(new List<OE2EmpireTracker.Baseline.Survey>(playerContext.surveyList));

            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.bindingSourceResource;

            DataGridViewComboBoxColumn cmbPurity = (DataGridViewComboBoxColumn)dgvResources.Columns["Purity"];
            cmbPurity.DisplayMember = "Name";
            cmbPurity.ValueMember = "Name";
            cmbPurity.DataSource = empireContext.bindingSourceResourcePurity;
        }
        private void updateScannerBlueprintList()
        {
            string searchText = txtFilterScannerBlueprint.Text;
            List<Blueprint> filteredList = new List<Blueprint>(playerContext.blueprintList);

            BlueprintType scanners = empireContext.findBlueprintType("SystemObjectScanner");
            filteredList = filteredList
                .Where(item => item.BluePrintType == scanners.Id)
                .ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // Add an empty to allow to select no base blueprint.

            filteredList.Insert(0, new Blueprint());
            BindingSource filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = filteredList;


            cmbScannerBlueprint.DataSource = filteredItemsBindingList;
        }

        void populateListView(List<OE2EmpireTracker.Baseline.Survey> surveys)
        {
            if (surveys == null)
            {
                return;
            }
            //lvwSurveys.Items.Clear();

            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();

            // First index what is viewable.
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                viewableSurveys[(item.Tag as OE2EmpireTracker.Baseline.Survey).UUID] = item;
            }

            // Now add or update what is viewable.
            foreach (OE2EmpireTracker.Baseline.Survey survey in surveys)
            {
                ListViewItem item;
                bool found = viewableSurveys.TryGetValue(survey.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(survey.UUID); // Main item text (first column)
                }
                item.Tag = survey;
                item.SubItems[0].Tag = survey;
                item.SubItems.Add(survey.PlanetName);
                item.SubItems.Add(survey.NickName);
                item.SubItems.Add(survey.DateTime);

                if (!found)
                {
                    lvwSurveys.Items.Add(item); // Add the item to the ListView
                }
                else
                {
                    viewableSurveys.Remove(survey.UUID);
                }
            }

            // Remove what is left over (deleted or filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableSurvey in viewableSurveys)
            {
                lvwSurveys.Items.Remove(viewableSurvey.Value);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            empireContext = EmpireContext.getInstance();

            playerContext = EmpireContext.PlayerContext;

            OE2EmpireTracker.Baseline.Survey survey;
            if (selectedSurvey != null)
            {
                survey = selectedSurvey;
            }
            else
            {
                survey = new OE2EmpireTracker.Baseline.Survey();
                Guid myUuid = Guid.NewGuid();
                survey.UUID = myUuid.ToString();
            }

            survey.PlanetName = txtPlanetName.Text;
            survey.SurveyID = txtSurveyID.Text;
            survey.ScannedBy = txtScannedBy.Text;
            survey.DateTime = txtScanDateTime.Text;
            survey.Properties["SensorAbundance"] = txtSensorAbundance.Text;
            survey.Properties["PurityModifier"] = txtPurityModifier.Text;
            survey.Properties["ScanLevel"] = txtScanLevel.Text;

            if (cmbScannerBlueprint.SelectedItem != null)
            {
                Blueprint baseBlueprint = cmbScannerBlueprint.SelectedItem as Blueprint;
                survey.ScannerBlueprintUUID = baseBlueprint.UUID;
            }
            else
            {
                survey.ScannerBlueprintUUID = "";
            }

            // Now to map grid fields.

            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                string resourceName = row.Cells[0].Value as string;
                string resourcePurity = row.Cells[1].Value as string;
                string resourceAmount = row.Cells[2].Value as string;
                SurveyResource surveyResource = new SurveyResource();
                surveyResource.Purity = resourcePurity;
                surveyResource.Amount = resourceAmount;
                if (resourceName != null)
                {
                    survey.Resources[resourceName] = surveyResource;
                }
            }

            if (selectedSurvey == null)
            {
                playerContext.surveyList.Add(survey);
            }
            playerContext.writeContext();
            //populateListView(new List<Blueprint>(playerContext.blueprintList));

            //selectedBlueprint = null;
            clearForm();
            //txtBlueprintListFilter.Focus();
        }

        private void clearForm()
        {
            selectedSurvey = null;
            txtPlanetName.Text = "";
            txtSurveyID.Text = "";
            txtNickName.Text = "";
            txtScannedBy.Text = "";
            txtScanDateTime.Text = "";
            txtSensorAbundance.Text = "";
            txtPurityModifier.Text = "";
            txtScanLevel.Text = "";

            cmbScannerBlueprint.SelectedItem = null;
            dgvResources.Rows.Clear();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void lvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Debug.Print("lvwBlueprints.SelectedItems.Count = " + lvwSurveys.SelectedItems.Count);
            if (lvwSurveys.SelectedItems.Count == 1)
            {
                Debug.Print("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Text);
                Debug.Print("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Tag);
                selectedSurvey = lvwSurveys.SelectedItems[0].SubItems[0].Tag as OE2EmpireTracker.Baseline.Survey;
                populateForm();
            }
        }
        private void populateForm()
        {
            if (selectedSurvey == null)
            {
                return;
            }
            txtPlanetName.Text = selectedSurvey.PlanetName;
            txtSurveyID.Text = selectedSurvey.SurveyID;
            txtNickName.Text = selectedSurvey.NickName;
            txtScannedBy.Text = selectedSurvey.ScannedBy;
            txtScanDateTime.Text = selectedSurvey.DateTime;
            txtSensorAbundance.Text = selectedSurvey.Properties["SensorAbundance"];
            txtPurityModifier.Text = selectedSurvey.Properties["PurityModifier"];
            txtScanLevel.Text = selectedSurvey.Properties["ScanLevel"];

            txtFilterScannerBlueprint.Text = "";
            //updateScannerBlueprintList();
            cmbScannerBlueprint.SelectedItem = playerContext.findBlueprint(selectedSurvey.ScannerBlueprintUUID);

            dgvResources.Rows.Clear();
            foreach (KeyValuePair<string, SurveyResource> resource in selectedSurvey.Resources)
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value.Purity;
                row.Cells[2].Value = resource.Value.Amount;
            }

        }
        private void txtFilterScannerBlueprint_TextChanged(object sender, EventArgs e)
        {
            updateScannerBlueprintList();
            cmbScannerBlueprint.DroppedDown = true;
        }
    }
}
