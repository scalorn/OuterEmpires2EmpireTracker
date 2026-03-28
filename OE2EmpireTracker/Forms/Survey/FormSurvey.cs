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
    /// <summary>
    /// FormSurvey - Main form for managing survey data in the OE2 Empire Tracker.
    /// </summary>
    /// <remarks>
    /// This form allows users to:
    /// - View all recorded surveys in a list view
    /// - Create new survey entries by scanning planets
    /// - Edit existing survey entries
    /// - Delete survey records
    ///
    /// Surveys contain information about planet scans including resources discovered,
    /// scanner blueprint used, and various sensor readings.
    /// </remarks>
    public partial class FormSurvey : Form
    {
        /// <summary>
        /// Gets or sets the empire context instance for accessing empire-wide data.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// Gets or sets the player context instance for accessing player-specific data.
        /// </summary>
        private PlayerContext playerContext;

        /// <summary>
        /// Gets or sets the currently selected survey in the list view.
        /// Used when editing an existing survey.
        /// </summary>
        private OE2EmpireTracker.Baseline.Survey selectedSurvey;

        /// <summary>
        /// Initializes a new instance of the FormSurvey class.
        /// </summary>
        /// <remarks>
        /// Performs the following initialization tasks:
        /// - Obtains empire and player context instances
        /// - Configures the scanner blueprint combo box with available scanners
        /// - Sets up the survey list view with appropriate columns
        /// - Populates the list view with existing surveys from player context
        /// - Configures resource data grid with resource types and purity levels
        /// </remarks>
        public FormSurvey()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            // Configure scanner blueprint combo box
            cmbScannerBlueprint.DisplayMember = "ExtendedName";
            cmbScannerBlueprint.ValueMember = "UUID";
            updateScannerBlueprintList();
            cmbScannerBlueprint.SelectedIndex = -1;

            // Set up survey list view with columns
            lvwSurveys.View = View.Details;
            lvwSurveys.Columns.Add("UUID", 0);
            lvwSurveys.Columns.Add("PlanetName", 100);
            lvwSurveys.Columns.Add("NickName", 100);
            lvwSurveys.Columns.Add("DateTime", 100);
            populateListView(new List<OE2EmpireTracker.Baseline.Survey>(playerContext.surveyList));

            // Configure resource data grid
            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.bindingSourceResource;

            DataGridViewComboBoxColumn cmbPurity = (DataGridViewComboBoxColumn)dgvResources.Columns["Purity"];
            cmbPurity.DisplayMember = "Name";
            cmbPurity.ValueMember = "Name";
            cmbPurity.DataSource = empireContext.bindingSourceResourcePurity;
        }

        /// <summary>
        /// Updates the scanner blueprint list with available system object scanners.
        /// </summary>
        /// <remarks>
        /// Filters blueprints to only include SystemObjectScanner types and applies
        /// any text filter from the search box. An empty Blueprint item is inserted
        /// at the beginning to allow deselecting a scanner.
        ///
        /// The method respects any text entered in txtFilterScannerBlueprint,
        /// performing case-insensitive filtering on the ExtendedName property.
        /// </remarks>
        private void updateScannerBlueprintList()
        {
            string searchText = txtFilterScannerBlueprint.Text;
            List<Data.Blueprint> filteredList = new List<Data.Blueprint>(playerContext.blueprintList);

            // Get the ScannerObject blueprint type ID
            BlueprintType scanners = empireContext.findBlueprintType("SystemObjectScanner");
            filteredList = filteredList
                .Where(item => item.BluePrintType == scanners.Id)
                .ToList();

            // Apply text filter if specified
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // Add an empty entry to allow selecting no base blueprint.
            filteredList.Insert(0, new Data.Blueprint());

            // Create a BindingSource with the filtered list as data source
            BindingSource filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;

            cmbScannerBlueprint.DataSource = filteredItemsBindingList;
        }

        /// <summary>
        /// Populates the survey list view with the provided list of surveys.
        /// </summary>
        /// <param name="surveys">The list of surveys to populate the view with.</param>
        /// <remarks>
        /// Creates a dictionary keyed by UUID for efficient lookup of existing items
        /// in the ListView to avoid duplicates. For each survey in the input list:
        /// - Creates a new ListView item if not already present
        /// - Updates existing item's subitems with current survey data
        /// - Maintages reference to Survey object in Tag property for editing
        ///
        /// After processing all items, removes any remaining ListView items whose
        /// corresponding surveys were deleted or filtered out from the player context.
        /// </remarks>
        void populateListView(List<OE2EmpireTracker.Baseline.Survey> surveys)
        {
            if (surveys == null)
            {
                return;
            }

            // Dictionary for efficient duplicate detection by UUID
            Dictionary<string, ListViewItem> viewableSurveys = new Dictionary<string, ListViewItem>();

            // Index currently viewable items to detect duplicates
            foreach (ListViewItem item in lvwSurveys.Items)
            {
                viewableSurveys[(item.Tag as OE2EmpireTracker.Baseline.Survey).UUID] = item;
            }

            // Process each survey - add or update ListView items
            foreach (OE2EmpireTracker.Baseline.Survey survey in surveys)
            {
                ListViewItem item;
                bool found = viewableSurveys.TryGetValue(survey.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(survey.UUID); // Main item text (first column - UUID)
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

            // Remove items that no longer have corresponding surveys (deleted/filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableSurvey in viewableSurveys)
            {
                lvwSurveys.Items.Remove(viewableSurvey.Value);
            }
        }

        /// <summary>
        /// Handles the click event for the Save button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Saves a new survey or updates an existing one based on whether selectedSurvey is null.
        ///
        /// For new surveys:
        /// - Generates a unique UUID using Guid.NewGuid()
        /// - Initializes with default empty values
        ///
        /// For existing surveys:
        /// - Uses the currently selected Survey object
        ///
        /// Populates the survey with:
        /// - Planet name and ID from text boxes
        /// - Scanner blueprint UUID from combo box selection
        /// - Resource data from the resources grid (name, purity, amount)
        /// - Sensor readings (abundance, purity modifier, scan level)
        ///
        /// After saving, the form is cleared and list view is updated.
        /// </remarks>
        private void btnSave_Click(object sender, EventArgs e)
        {
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            OE2EmpireTracker.Baseline.Survey survey;

            // Determine whether creating new or updating existing survey
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

            // Populate basic survey properties from text inputs
            survey.PlanetName = txtPlanetName.Text;
            survey.SurveyID = txtSurveyID.Text;
            survey.ScannedBy = txtScannedBy.Text;
            survey.DateTime = txtScanDateTime.Text;
            survey.Properties["SensorAbundance"] = txtSensorAbundance.Text;
            survey.Properties["PurityModifier"] = txtPurityModifier.Text;
            survey.Properties["ScanLevel"] = txtScanLevel.Text;

            // Get scanner blueprint UUID if one is selected
            if (cmbScannerBlueprint.SelectedItem != null)
            {
                Data.Blueprint baseBlueprint = cmbScannerBlueprint.SelectedItem as Data.Blueprint;
                survey.ScannerBlueprintUUID = baseBlueprint.UUID;
            }
            else
            {
                survey.ScannerBlueprintUUID = "";
            }

            // Map resources from grid to survey.Resources collection
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

            // Add to player context or update existing survey
            if (selectedSurvey == null)
            {
                playerContext.surveyList.Add(survey);
            }

            // Persist changes to player context
            playerContext.writeContext();

            // Refresh UI state
            //populateListView(new List<Blueprint>(playerContext.blueprintList));
            //selectedBlueprint = null;
            clearForm();
            //txtBlueprintListFilter.Focus();
        }

        /// <summary>
        /// Clears all form controls to prepare for a new survey entry.
        /// </summary>
        /// <remarks>
        /// Resets all text boxes and combo boxes to their default/empty states:
        /// - Clears the selected survey reference
        /// - Empties all text input fields (planet name, ID, nickname, etc.)
        /// - Deselects the scanner blueprint combo box
        /// - Clears the resources grid
        /// </remarks>
        private void clearForm()
        {
            selectedSurvey = null;

            // Clear all text input fields
            txtPlanetName.Text = "";
            txtSurveyID.Text = "";
            txtNickName.Text = "";
            txtScannedBy.Text = "";
            txtScanDateTime.Text = "";
            txtSensorAbundance.Text = "";
            txtPurityModifier.Text = "";
            txtScanLevel.Text = "";

            // Reset combo box and grid
            cmbScannerBlueprint.SelectedItem = null;
            dgvResources.Rows.Clear();
        }

        /// <summary>
        /// Handles the click event for the Delete button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Currently implemented as a placeholder with no functionality.
        /// To implement deletion, this should:
        /// - Validate that an item is selected in lvwSurveys
        /// - Get the selected survey's UUID from ListViewItem.Tag
        /// - Remove from playerContext.surveyList collection
        /// - Call playerContext.writeContext() to persist changes
        /// - Clear the form after deletion
        /// </remarks>
        private void cmdDelete_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the click event for the Cancel button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Currently implemented as a placeholder with no functionality.
        /// Typically should reset form state and close/reset focus appropriately.
        /// </remarks>
        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles changes in the survey list view item selection.
        /// </summary>
        /// <param name="sender">The ListView object that triggered the event.</param>
        /// <param name="e">Event data containing selection change information.</param>
        /// <remarks>
        /// When a user selects an item in the survey list, this method:
        /// - Validates that exactly one item is selected
        /// - Retrieves the Survey object from the ListView item's Tag property
        /// - Populates all form fields with the selected survey's data
        ///
        /// Uses Debug.Print for logging; should be replaced with proper logging in production.
        /// </remarks>
        private void lvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Debug.Print("lvwSurveys.SelectedItems.Count = " + lvwSurveys.SelectedItems.Count);

            if (lvwSurveys.SelectedItems.Count == 1)
            {
                Debug.Print("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Text);
                Debug.Print("Selected item = " + lvwSurveys.SelectedItems[0].SubItems[0].Tag);

                // Get the Survey object from the ListView item tag
                selectedSurvey = lvwSurveys.SelectedItems[0].SubItems[0].Tag as OE2EmpireTracker.Baseline.Survey;

                // Populate form fields with selected survey data
                populateForm();
            }
        }

        /// <summary>
        /// Populates the form fields with data from the currently selected survey.
        /// </summary>
        /// <remarks>
        /// If no survey is selected (selectedSurvey is null), this method returns immediately.
        /// Otherwise, it:
        /// - Sets all text input fields from the survey's properties and sub-properties
        /// - Loads the scanner blueprint into the combo box
        /// - Populates the resources grid with the survey's resource data
        ///
        /// The resources grid displays: Resource Name, Purity Level, and Amount.
        /// </remarks>
        private void populateForm()
        {
            if (selectedSurvey == null)
            {
                return;
            }

            // Populate text input fields
            txtPlanetName.Text = selectedSurvey.PlanetName;
            txtSurveyID.Text = selectedSurvey.SurveyID;
            txtNickName.Text = selectedSurvey.NickName;
            txtScannedBy.Text = selectedSurvey.ScannedBy;
            txtScanDateTime.Text = selectedSurvey.DateTime;
            txtSensorAbundance.Text = selectedSurvey.Properties["SensorAbundance"];
            txtPurityModifier.Text = selectedSurvey.Properties["PurityModifier"];
            txtScanLevel.Text = selectedSurvey.Properties["ScanLevel"];

            // Clear filter text
            txtFilterScannerBlueprint.Text = "";

            // Load the scanner blueprint into combo box
            //updateScannerBlueprintList();
            cmbScannerBlueprint.SelectedItem = playerContext.findBlueprint(selectedSurvey.ScannerBlueprintUUID);

            // Populate resources grid with survey's resource data
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

        /// <summary>
        /// Handles text changes in the scanner blueprint filter text box.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// When users type in the search box, this method:
        /// - Re-regenerates the filtered scanner blueprint list
        /// - Automatically drops down the combo box to show the updated results
        ///
        /// Provides real-time filtering of available scanners as the user types.
        /// </remarks>
        private void txtFilterScannerBlueprint_TextChanged(object sender, EventArgs e)
        {
            updateScannerBlueprintList();
            cmbScannerBlueprint.DroppedDown = true;
        }
    }
}