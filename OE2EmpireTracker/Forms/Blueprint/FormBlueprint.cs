using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using NLog;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker
{
    /// <summary>
    /// FormBlueprint - Main form for managing blueprint data in the OE2 Empire Tracker.
    /// </summary>
    /// <remarks>
    /// This form allows users to:
    /// - View all recorded blueprints in a list view
    /// - Create new blueprint entries with various properties
    /// - Edit existing blueprint entries
    /// - Delete blueprint records
    /// 
    /// Blueprints represent discovered technology including system object scanners,
    /// ship classes, and resource-related items. Each blueprint contains type information,
    /// evolution level, properties, resources, and copy costs.
    /// </remarks>
    public partial class FormBlueprint : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        /// <summary>
        /// Gets or sets the empire context instance for accessing empire-wide data.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// Gets or sets the player context instance for accessing player-specific data.
        /// </summary>
        private PlayerContext playerContext;

        /// <summary>
        /// Gets or sets the currently selected blueprint in the list view.
        /// Used when editing an existing blueprint.
        /// </summary>
        private BlueprintViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the FormBlueprint class.
        /// </summary>
        /// <remarks>
        /// Performs the following initialization tasks:
        /// - Obtains empire and player context instances
        /// - Configures blueprint type combo box with available types
        /// - Configures ship class combo box for non-universal blueprints
        /// - Configures tech level combo box for applicable blueprints
        /// - Sets up evolution dropdown with Evolution 0 selected by default
        /// - Configures resource data grid with resource types
        /// - Configures base blueprint combo box showing all available blueprints
        /// - Sets up the blueprint list view with appropriate columns (UUID, Type, Name, Tech Level, Evolution, Nick Name)
        /// - Populates the list view with existing blueprints from player context
        /// </remarks>
        public FormBlueprint()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new BlueprintViewModel(new Blueprint(), playerContext);

            // Configure blueprint type combo box
            cmbBlueprintType.DisplayMember = "Name";
            cmbBlueprintType.ValueMember = "Id";
            cmbBlueprintType.DataSource = empireContext.bindingSourceBlueprintType;
            cmbBlueprintType.SelectedIndex = -1;

            // Configure ship class combo box (used for non-universal blueprints)
            cmbShipClass.DisplayMember = "Name";
            cmbShipClass.ValueMember = "Id";
            cmbShipClass.DataSource = empireContext.bindingSourceShipClass;
            cmbShipClass.SelectedIndex = -1;

            // Configure tech level combo box
            cmbTechLevel.DisplayMember = "Name";
            cmbTechLevel.ValueMember = "Name";
            cmbTechLevel.DataSource = empireContext.bindingSourceTechLevel;
            cmbTechLevel.SelectedIndex = -1;

            // Set focus for statistics grid to previous control (tab navigation)
            dgvStatistics.previousControl = tabDetailedData;

            // Configure evolution dropdown with default value
            cmbEvolution.DisplayMember = "Name";
            cmbEvolution.ValueMember = "Name";
            cmbEvolution.DataSource = empireContext.bindingSourceEvolution;
            cmbEvolution.SelectedIndex = 0;

            // Configure resource data grid
            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.bindingSourceResource;

            // Configure base blueprint combo box
            cmbBaseBlueprint.DisplayMember = "ExtendedName";
            cmbBaseBlueprint.ValueMember = "UUID";
            cmbBaseBlueprint.DataSource = playerContext.bindingSourceBlueprint;
            cmbBaseBlueprint.SelectedIndex = -1;

            // Set up blueprint list view with columns
            lvwBlueprints.View = View.Details;
            lvwBlueprints.Columns.Add("UUID", 0);
            lvwBlueprints.Columns.Add("Type", 50);
            lvwBlueprints.Columns.Add("Name", 100);
            lvwBlueprints.Columns.Add("Tech Level", 60);
            lvwBlueprints.Columns.Add("Evolution", 30);
            lvwBlueprints.Columns.Add("Nick Name", 100);
            PopulateListView(viewModel.GetFilteredBlueprints(null));

            // Wire write-through handlers
            txtName.TextChanged += txtName_TextChanged;
            txtNickName.TextChanged += txtNickName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            txtCopyCost.TextChanged += txtCopyCost_TextChanged;
            cmbShipClass.SelectedIndexChanged += cmbShipClass_SelectedIndexChanged;
            cmbTechLevel.SelectedIndexChanged += cmbTechLevel_SelectedIndexChanged;
            cmbEvolution.SelectedIndexChanged += cmbEvolution_SelectedIndexChanged;
            cmbBaseBlueprint.SelectedIndexChanged += cmbBaseBlueprint_SelectedIndexChanged;
            chkGlobalBlueprint.CheckedChanged += chkGlobalBlueprint_CheckedChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged += OnBlueprintDataChanged;
        }

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
            PopulateListView(viewModel.GetFilteredBlueprints(txtBlueprintListFilter.Text));
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
            PopulateListView(viewModel.GetFilteredBlueprints(null));
        }

        /// <summary>
        /// Handles text changes in the copy target rich text box.
        /// </summary>
        /// <param name="sender">The RichTextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void rtbCopyTarget_TextChanged(object sender, EventArgs e)
        {
            // Log.Debug(e.ToString());
        }

        /// <summary>
        /// Handles the click event for the Import button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Checks if clipboard contains HTML text and retrieves it for import operations.
        /// The HTML fragment is extracted from clipboard data which typically includes
        // start/end fragment markers. Currently commented out - can be re-enabled when needed.
        /// </remarks>
        private void btnImport_Click(object sender, EventArgs e)
        {
            String returnHtmlText = null;
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                returnHtmlText = Clipboard.GetText(TextDataFormat.Html);
                string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);
                //rtbCopyTarget.Text = html;
                //ProcessHTML(html);
            }
        }

        /// <summary>
        /// Extracts selected HTML fragment string from clipboard data by parsing header information.
        /// </summary>
        /// <param name="htmlDataString">String representing HTML clipboard data. This includes HTML header.</param>
        /// <returns>String containing only the HTML selection part of htmlDataString, without header. Returns error message if parsing fails.</returns>
        /// <remarks>
        /// Uses Microsoft's standard clipboard HTML format which wraps fragments with:
        /// - <!--StartFragment--> marker followed by byte count to fragment start
        /// - <!--EndFragment--> marker followed by byte count to fragment end
        /// 
        /// The method extracts the content between these markers to isolate just the selected fragment.
        /// 
        /// Reference: https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx
        /// 
        /// TODO: Current implementation assumes 10-digit indices which may be brittle for non-standard cases.
        /// More flexible parsing should be implemented to handle edge cases.
        /// </remarks>
        internal static string ExtractHtmlFragmentFromClipboardData(string htmlDataString)
        {
            // HTML Clipboard Format:
            // (https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx)
            // - Fragment contains valid HTML representing the selected area
            // - Includes opening tags and attributes for elements with end tags within selection
            // - End tags that match included opening tags
            // - Wrapped with <!--StartFragment--> and <!--EndFragment--> markers

            // Byte count from beginning of clipboard to start of fragment
            int startFragmentIndex = htmlDataString.IndexOf("StartFragment:");
            if (startFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }
            
            // Parse the byte offset for fragment start
            startFragmentIndex = Int32.Parse(htmlDataString.Substring(startFragmentIndex + "StartFragment:".Length, 10));
            if (startFragmentIndex < 0 || startFragmentIndex > htmlDataString.Length)
            {
                return "ERROR: Unrecognized html header";
            }

            // Byte count from beginning of clipboard to end of fragment
            int endFragmentIndex = htmlDataString.IndexOf("EndFragment:");
            if (endFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }

            // Parse the byte offset for fragment end
            endFragmentIndex = Int32.Parse(htmlDataString.Substring(endFragmentIndex + "EndFragment:".Length, 10));
            if (endFragmentIndex > htmlDataString.Length)
            {
                endFragmentIndex = htmlDataString.Length;
            }

            // Convert bytes to string using UTF-8 encoding
            byte[] bytes = Encoding.UTF8.GetBytes(htmlDataString);
            return Encoding.UTF8.GetString(bytes, startFragmentIndex, endFragmentIndex - startFragmentIndex);
        }

        /// <summary>
        /// Processes HTML content by parsing with SgmlReader and debugging child nodes.
        /// </summary>
        /// <param name="inputText">The HTML string to parse.</param>
        /// <remarks>
        /// Currently used for debugging - prints inner text of each node to Debug window.
        /// Uses SgmlReader for HTML parsing with whitespace handling preserved.
        /// </remarks>
        private void ProcessHTML(string inputText)
        {
            StringReader reader = new StringReader(inputText);

            // Setup SgmlReader with HTML document type and settings
            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };

            // Create document with whitespace preservation
            XmlDocument doc = new XmlDocument()
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            doc.Load(sgmlReader);

            // Debug: Print inner text of each node
            foreach (XmlNode item in doc)
            {
                Log.Debug("T = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children(0, item.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Recursively processes child nodes and prints their inner text to debug output.
        /// </summary>
        /// <param name="depth">Current recursion depth for indentation.</param>
        /// <param name="nodes">List of child nodes to process.</param>
        /// <remarks>
        /// Used by ProcessHTML() to traverse and debug HTML node structure.
        /// Increments depth parameter for recursive calls to show nesting level.
        /// </remarks>
        private void children(int depth, XmlNodeList nodes)
        {
            foreach (XmlNode item in nodes)
            {
                Log.Debug("C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Handles changes in the blueprint type combo box selection.
        /// </summary>
        /// <param name="sender">The ComboBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Triggers property grid update to reflect properties of selected blueprint type.
        /// Universal blueprint types hide ship class and tech level options.
        /// </remarks>
        private void cmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null) viewModel.BluePrintType = bt.Id;
            UpdatePropertyGrid();
        }

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
            int copyCost = 0;
            int.TryParse(txtCopyCost.Text, out copyCost);
            viewModel.CopyCost = copyCost;
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

        private void chkGlobalBlueprint_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Global flag is read at save time — no viewModel field to write.
        }

        /// <summary>
        /// Updates the base blueprint list with filtered available blueprints based on current filters.
        /// </summary>
        /// <remarks>
        /// Applies multiple filter conditions:
        /// - Filters by blueprint type if a type is selected
        /// - Applies text filter from txtFilterBaseBlueprint for name-based search
        /// - Filters by ship class if a class is selected (for non-universal types)
        /// - Excludes the currently selected blueprint to allow selecting different base blueprints
        /// 
        /// An empty Blueprint entry is inserted at the beginning to allow deselecting a base blueprint.
        /// </remarks>
        public void UpdateBlueprintTypeListBase()
        {
            string searchText = txtFilterBlueprintType.Text;
            List<BlueprintType> filteredList = new List<BlueprintType>(empireContext.blueprintTypeList);

            // Apply text filter if specified
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            
            filteredList.Insert(0, new BlueprintType());
            
            var filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;

            cmbBlueprintType.DataSource = filteredItemsBindingList;
        }

        /// <summary>
        /// Updates the property grid based on the selected blueprint type.
        /// </summary>
        /// <remarks>
        /// For universal blueprints (Universal == true):
        /// - Hides the ship class dropdown panel
        /// - Resets ship class selection
        /// - Hides the tech level dropdown panel
        /// - Resets tech level selection
        /// 
        /// For non-universal blueprints:
        /// - Shows both ship class and tech level panels
        /// 
        /// Then populates the statistics grid (dgvStatistics) with blueprint type properties:
        /// - Clears existing rows if no properties defined
        /// - Adds rows for each property defined by the blueprint type
        /// - Sets property name in "Property" column
        /// - Stores property tag for later value retrieval
        /// - Removes excess rows if fewer than defined properties
        /// </remarks>
        public void UpdatePropertyGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            BlueprintType bt = cmbBlueprintType.SelectedItem as BlueprintType;
            
            // Handle universal vs non-universal blueprints
            if (bt != null && bt.Universal == true)
            {
                flpClass.Visible = false;
                cmbShipClass.SelectedIndex = -1;
                flpTechLevel.Visible = false;
                cmbTechLevel.SelectedIndex = -1;
            }
            else
            {
                flpClass.Visible = true;
                flpTechLevel.Visible = true;
            }

            // Populate property grid rows
            if (bt != null && bt.Properties != null)
            {
                int row = 0;
                foreach (string property in bt.Properties)
                {
                    int rowIndex = row;
                    // Add row if needed
                    if (dgvStatistics.Rows.Count <= row)
                    {
                        rowIndex = dgvStatistics.Rows.Add();
                    }
                    
                    DataGridViewRow newRow = dgvStatistics.Rows[rowIndex];
                    newRow.Cells["Property"].Value = property;
                    newRow.Cells["Property"].Tag = property;

                    // Swap the CurrentValue cell based on property type
                    var propType = BlueprintPropertyValidation.GetPropertyType(property);
                    if (propType == PropertyValueType.ComboBox)
                    {
                        var comboCell = new DataGridViewComboBoxCell();
                        comboCell.DataSource = BlueprintPropertyValidation.GetComboBoxDataSource(property);
                        comboCell.FlatStyle = FlatStyle.Flat;
                        newRow.Cells["CurrentValue"] = comboCell;
                    }
                    else if (propType == PropertyValueType.CheckBox)
                    {
                        var checkCell = new DataGridViewCheckBoxCell();
                        newRow.Cells["CurrentValue"] = checkCell;
                    }
                    else
                    {
                        // Ensure it's a text cell (may have been swapped previously)
                        if (!(newRow.Cells["CurrentValue"] is DataGridViewTextBoxCell))
                        {
                            newRow.Cells["CurrentValue"] = new DataGridViewTextBoxCell();
                        }
                    }

                    row++;
                }

                // Remove excess rows if there are more than defined properties
                if (bt.Properties.Length == 0)
                {
                    dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
                    try { dgvStatistics.EndEdit(); } catch { }
                    dgvStatistics.Rows.Clear();
                    dgvStatistics.CellValidating += dgvStatistics_CellValidating;
                }
                else while (dgvStatistics.Rows.Count > bt.Properties.Length)
                {
                    dgvStatistics.Rows.RemoveAt(dgvStatistics.Rows.Count - 1);
                }
            }
        }

        /// <summary>
        /// Handles selection changes in the statistics data grid.
        /// </summary>
        /// <param name="sender">The DataGridView object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void dgvStatistics_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            Log.Debug("dgvStatistics_SelectionChanged Sender = " + sender + " Event Args " + e);
        }

        /// <summary>
        /// Updates the base blueprint list with filtered results.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.DroppedDown = true;
        }

        /// <summary>
        /// Updates the blueprint list view with filtered blueprints based on current search filters.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtBlueprintListFilter_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtBlueprintListFilter.Text;
            PopulateListView(viewModel.GetFilteredBlueprints(searchText));
        }

        /// <summary>
        /// Updates the base blueprint list with filtered results based on current selection state.
        /// </summary>
        /// <remarks>
        /// Applies multiple filter conditions:
        /// - Filters by blueprint type if a type is selected in cmbBlueprintType
        /// - Applies text filter from txtFilterBaseBlueprint for extended name search
        /// - Filters by ship class if a class is selected (for non-universal types)
        /// - Excludes the currently selected blueprint UUID to allow selecting different base blueprints
        /// 
        /// An empty Blueprint entry is inserted at the beginning to allow deselecting a base blueprint.
        /// </remarks>
        private void UpdateBaseBlueprintList()
        {
            string searchText = txtFilterBaseBlueprint.Text;
            List<Blueprint> filteredList = new List<Blueprint>(playerContext.GetAllBlueprints());

            // Filter by blueprint type if selected
            if (cmbBlueprintType.SelectedItem != null)
            {
                filteredList = filteredList
                    .Where(item => item.BluePrintType == (cmbBlueprintType.SelectedItem as BlueprintType).Id)
                    .ToList();
            }

            // Apply text filter for extended name search
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // Filter by ship class if selected (for non-universal blueprints)
            if (cmbShipClass.SelectedItem != null)
            {
                int shipClass = (cmbShipClass.SelectedItem as ShipClass).Id;
                filteredList = filteredList
                    .Where(item => item.Class == shipClass)
                    .ToList();
            }

            // Exclude currently selected blueprint to allow selecting a different base blueprint
            if (viewModel.Data.UUID != null)
            {
                filteredList = filteredList
                    .Where(item => item.UUID != viewModel.UUID)
                    .ToList();
            }

            // Add an empty entry to allow selecting no base blueprint.
            filteredList.Insert(0, new Blueprint());
            
            BindingSource filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;

            cmbBaseBlueprint.DataSource = filteredItemsBindingList;
        }

        /// <summary>
        /// Populates the blueprint list view with the provided list of blueprints.
        /// </summary>
        /// <param name="blueprints">The list of blueprints to populate the view with.</param>
        /// <remarks>
        /// Clears the existing ListView and creates a dictionary keyed by UUID for efficient 
        /// lookup of existing items to avoid duplicates. For each blueprint in the input list:
        /// - Creates a new ListView item if not already present
        /// - Updates existing item's subitems with current blueprint data
        /// - Maintains reference to Blueprint object in Tag property for editing and deletion operations
        /// 
        /// Displays the following columns:
        /// - UUID (first column, uses full width remaining)
        /// - Type (blueprint type ID/name)
        /// - Name
        /// - Tech Level
        /// - Evolution level (as string)
        /// - Nick Name
        /// 
        /// After processing all items, removes any remaining ListView items whose 
        /// corresponding blueprints were deleted or filtered out.
        /// </remarks>
        void PopulateListView(IReadOnlyList<Blueprint> blueprints)
        {
            if (blueprints == null)
            {
                return;
            }

            // Clear and rebuild the list view
            lvwBlueprints.Items.Clear();

            // Dictionary for efficient duplicate detection by UUID
            Dictionary<string, ListViewItem> viewableBlueprints = new Dictionary<string, ListViewItem>();

            // Index currently viewable items to detect duplicates
            foreach (ListViewItem item in lvwBlueprints.Items)
            {
                viewableBlueprints[(item.Tag as Blueprint).UUID] = item;
            }

            // Process each blueprint - add or update ListView items
            foreach (Blueprint blueprint in blueprints)
            {
                ListViewItem item;
                bool found = viewableBlueprints.TryGetValue(blueprint.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(blueprint.UUID); // Main item text (first column - UUID)
                }
                item.Tag = blueprint;
                item.SubItems[0].Tag = blueprint;
                item.SubItems.Add(blueprint.BluePrintType); // Type
                item.SubItems.Add(blueprint.Name); // Name
                item.SubItems.Add(blueprint.TechLevel); // Tech Level
                item.SubItems.Add("" + blueprint.Evolution); // Evolution
                item.SubItems.Add(blueprint.NickName); // Nick Name

                if (!found)
                {
                    lvwBlueprints.Items.Add(item); // Add the item to the ListView
                }
                else
                {
                    viewableBlueprints.Remove(blueprint.UUID);
                }
            }

            // Remove items that no longer have corresponding blueprints (deleted/filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableBlueprint in viewableBlueprints)
            {
                lvwBlueprints.Items.Remove(viewableBlueprint.Value);
            }
        }

        /// <summary>
        /// Handles changes in the blueprint list view item selection.
        /// </summary>
        /// <param name="sender">The ListView object that triggered the event.</param>
        /// <param name="e">Event data containing selection change information.</param>
        /// <remarks>
        /// When a user selects an item in the blueprint list, this method:
        /// - Validates that exactly one item is selected
        /// - Retrieves the Blueprint object from the ListView item's Tag property
        /// - Populates all form fields with the selected blueprint's data
        /// 
        /// Uses NLog for logging selection changes.
        /// </remarks>
        private void lvwBlueprints_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Log.Debug("lvwBlueprints.SelectedItems.Count = " + lvwBlueprints.SelectedItems.Count);

            if (lvwBlueprints.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Text);
                Log.Debug("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Tag);

                // Get the Blueprint object from the ListView item tag
                viewModel.SelectBlueprint(lvwBlueprints.SelectedItems[0].SubItems[0].Tag as Blueprint);

                // Populate form fields with selected blueprint data
                PopulateForm();
            }
        }

        /// <summary>
        /// Handles text changes in the blueprint list filter text box.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtFilterBlueprintType_TextChanged(object sender, EventArgs e)
        {
            UpdateBlueprintTypeListBase();
            cmbBlueprintType.DroppedDown = true;
        }

        /// <summary>
        /// Handles entering text in the blueprint type filter text box.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Automatically drops down the combo box when user clicks in filter text box.
        /// </remarks>
        private void txtFilterBlueprintType_Enter(object sender, EventArgs e)
        {
            cmbBlueprintType.DroppedDown = true;
        }

        /// <summary>
        /// Handles the click event for the Delete button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Deletes the currently selected blueprint:
        /// - Delegates removal to viewModel.Delete()
        /// - Resets viewModel state
        /// - Updates the list view with remaining blueprints
        /// - Clears all form controls
        /// </remarks>
        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (viewModel.Data.UUID == null) return;
            var result = MessageBox.Show(
                $"Delete blueprint '{viewModel.Data.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            viewModel.Delete();
            viewModel.Reset();
            PopulateListView(viewModel.GetFilteredBlueprints(null));
            lvwBlueprints.SelectedItems.Clear();
            ClearForm();
        }

        /// <summary>
        /// Handles the click event for the New button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Clears all form controls to prepare for entering a new blueprint.
        /// Called from both New button and Save button after successful save.
        /// </remarks>
        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwBlueprints.SelectedItems.Clear();
        }

        /// <summary>
        /// Handles the click event for the Save button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Saves a new blueprint or updates an existing one based on whether selectedBlueprint is null.
        /// 
        /// For new blueprints:
        /// - Generates a unique UUID using Guid.NewGuid()
        /// - Initializes with default empty values
        /// 
        /// For existing blueprints:
        /// - Uses the currently selected Blueprint object
        /// 
        /// Populates the blueprint with:
        /// - Blueprint type ID from combo box selection
        /// - Ship class ID (for non-universal types)
        /// - Tech level name (if applicable)
        /// - Evolution level as integer
        /// - Base blueprint UUID from combo box selection
        /// - Name, nickname, and description from text inputs
        /// - Copy cost parsed from text input
        /// - Properties from statistics grid rows
        /// - Resources from resources grid
        /// 
        /// After saving:
        /// - Adds to player context or updates existing blueprint
        /// - Persists changes via writeContext()
        /// - Refreshes list view with updated data
        /// - Clears form controls
        /// - Restores focus to blueprint list filter
        /// </remarks>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Temporarily suppress cell validation so we can exit edit mode
            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try
            {
                dgvStatistics.EndEdit();
                dgvResources.EndEdit();
            }
            catch { }
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            dgvResources.CellValidating += dgvResources_CellValidating;

            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            // Map properties from grid via viewModel
            viewModel.ClearProperties();
            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string propName = row.Cells[0].Tag as string;
                object cellValue = row.Cells[2].Value;
                string strValue = cellValue is bool ? cellValue.ToString() : cellValue as string;
                viewModel.SetProperty(propName, strValue);
            }

            // Map resources from grid via viewModel
            viewModel.ClearResources();
            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                string resourceName = row.Cells[0].Value as string;
                string resourceAmount = row.Cells[1].Value as string;
                if (resourceName != null)
                {
                    viewModel.SetResource(resourceName, resourceAmount);
                }
            }

            // Persist via viewModel
            viewModel.Save(chkGlobalBlueprint.Checked);
            
            // Refresh list view
            PopulateListView(viewModel.GetFilteredBlueprints(null));
            
            // Restore focus
            txtBlueprintListFilter.Focus();
        }

        /// <summary>
        /// Populates the form fields with data from the currently selected blueprint.
        /// </summary>
        /// <remarks>
        /// If no blueprint is selected (selectedBlueprint is null), this method returns immediately.
        /// Otherwise, it:
        /// - Clears and regenerates blueprint type list
        /// - Loads the blueprint type into the combo box
        /// - Updates property grid based on blueprint type
        /// - Loads ship class, tech level, and evolution dropdown selections
        /// - Clears and regenerates base blueprint list
        /// - Loads the base blueprint (if any) into the combo box
        /// - Populates all text input fields from the blueprint's properties and sub-properties
        /// - Populates the statistics grid with property values
        /// - Populates the resources grid with the blueprint's resource data
        /// </remarks>
        private void PopulateForm()
        {
            if (viewModel.Data.UUID == null)
            {
                return;
            }

            // Clear and regenerate blueprint type list
            txtFilterBlueprintType.Text = "";
            UpdateBlueprintTypeListBase();

            // Load blueprint type selection
            cmbBlueprintType.SelectedItem = empireContext.FindBlueprintType(viewModel.Data.BluePrintType);

            // Update property grid based on selected type
            UpdatePropertyGrid();

            // Load dependent dropdowns
            cmbShipClass.SelectedItem = empireContext.FindShipClass(viewModel.Data.Class);
            cmbTechLevel.SelectedItem = empireContext.FindTechLevel(viewModel.Data.TechLevel);
            cmbEvolution.SelectedItem = empireContext.FindEvolution(viewModel.Data.Evolution);

            // Clear and regenerate base blueprint list
            txtFilterBaseBlueprint.Text = "";
            UpdateBaseBlueprintList();

            // Load base blueprint selection (if this is not the base blueprint)
            cmbBaseBlueprint.SelectedItem = playerContext.FindBlueprint(viewModel.Data.baseBlueprintUUID);

            // Populate text input fields
            txtName.Text = viewModel.Data.Name;
            txtNickName.Text = viewModel.Data.NickName;
            txtDescription.Text = viewModel.Data.Description;
            txtCopyCost.Text = "" + viewModel.Data.CopyCost;

            // Populate statistics grid with property values
            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string property = row.Cells["Property"].Tag as string;
                string value = "";
                bool found = viewModel.GetProperty(property, "", out value);
                if (!found || value == null)
                {
                    value = "";
                }

                var propType = BlueprintPropertyValidation.GetPropertyType(property);
                if (propType == PropertyValueType.CheckBox)
                {
                    bool boolVal = false;
                    bool.TryParse(value, out boolVal);
                    row.Cells["CurrentValue"].Value = boolVal;
                }
                else
                {
                    row.Cells["CurrentValue"].Value = value;
                }
            }
            PopulateResources();

            chkGlobalBlueprint.Checked = viewModel.IsGlobal;
        }
        private void PopulateResources()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            // Populate resources grid with blueprint's resource data
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;
            foreach (KeyValuePair<string, string> resource in viewModel.GetResources())
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value;
            }
        }

        /// <summary>
        /// Clears all form controls to prepare for a new blueprint entry.
        /// </summary>
        /// <remarks>
        /// Resets all controls to their default/empty states:
        /// - Clears the selected blueprint reference
        /// - Resets all text inputs (name, nickname, description, copy cost)
        /// - Resets all combo boxes and dropdowns
        /// - Regenerates filtered lists for type and base blueprint selectors
        /// - Clears both statistics and resources grids
        /// </remarks>
        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            // Clear filters
            txtFilterBlueprintType.Text = "";
            txtFilterBaseBlueprint.Text = "";

            // Regenerate type list with empty selection
            UpdateBlueprintTypeListBase();
            cmbBlueprintType.SelectedItem = null;
            cmbBlueprintType.Text = "";
            
            // Reset ship class and tech level dropdowns
            cmbShipClass.SelectedItem = null;
            cmbShipClass.Text = "";
            cmbTechLevel.SelectedItem = null;
            cmbTechLevel.Text = "";
            
            // Reset evolution dropdown
            cmbEvolution.SelectedItem = null;
            cmbEvolution.Text = "";

            // Regenerate base blueprint list with empty selection
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.SelectedItem = null;

            // Clear text inputs
            txtName.Text = "";
            txtNickName.Text = "";
            txtDescription.Text = "";
            txtCopyCost.Text = "";

            // Clear grids
            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvStatistics.EndEdit(); } catch { }
            try { dgvResources.EndEdit(); } catch { }
            dgvStatistics.Rows.Clear();
            dgvResources.Rows.Clear();
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            dgvResources.CellValidating += dgvResources_CellValidating;

            chkGlobalBlueprint.Checked = false;
        }

        /// <summary>
        /// Handles size changes to the search list flow panel.
        /// </summary>
        /// <param name="sender">The FlowLayoutPanel object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Previously used to adjust ListView height based on parent panel size.
        /// Commented out - may be re-enabled when needed.
        /// </remarks>
        private void flpSearchList_SizeChanged(object sender, EventArgs e)
        {
            //lvwBlueprints.Height = flpSearchList.Height - flpBlueprintSearch.Height;
        }

        private void cmdImport_Click(object sender, EventArgs e)
        {
            BlueprintScanner scanner = new BlueprintScanner();
            scanner.processClipboard(viewModel.Data);
            PopulateForm();
        }

        private void dgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the Amount column (index 1)
            if (e.ColumnIndex != 1) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!int.TryParse(value, out _))
            {
                e.Cancel = true;
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                dgvResources.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
            }
            else
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void dgvStatistics_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the CurrentValue column (index 2)
            if (e.ColumnIndex != 2) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(value)) return; // Allow empty

            // Get the property name from the Property cell's Tag
            string propertyName = dgvStatistics.Rows[e.RowIndex].Cells[0].Tag as string;
            if (string.IsNullOrEmpty(propertyName)) return;

            string pattern = Constants.BlueprintPropertyValidation.GetValidationPattern(propertyName);
            if (pattern == null)
            {
                // Unknown property Ã¢â‚¬â€ log and allow free-form
                Log.Warn("Unknown blueprint property for validation: {0}", propertyName);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
            {
                e.Cancel = true;
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                var propType = Constants.BlueprintPropertyValidation.GetPropertyType(propertyName);
                dgvStatistics.Rows[e.RowIndex].ErrorText = $"{propertyName} must be a valid {propType}";
            }
            else
            {
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvStatistics.Rows[e.RowIndex].ErrorText = "";
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged -= OnBlueprintDataChanged;
            base.OnFormClosed(e);
        }
    }
}